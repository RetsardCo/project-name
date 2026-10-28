using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Media;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Transport;

namespace ProjectName.Shared.Session
{
    public enum SessionStatus
    {
        Idle,
        PairingChallengePresented,
        Paired,
        ActiveSession,
        Terminated
    }

    /// <summary>
    /// Event arguments for pairing challenge display (endpoint UI shows the 6-digit PIN).
    /// </summary>
    public class PairingChallengeEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string PinCode { get; }
        public int ExpiresInSeconds { get; }

        public PairingChallengeEventArgs(string deviceId, string pinCode, int expiresInSeconds)
        {
            DeviceId = deviceId;
            PinCode = pinCode;
            ExpiresInSeconds = expiresInSeconds;
        }
    }

    /// <summary>
    /// Coordinates pairing verification, mutual trust, and session authorization.
    /// Uses PairingManager to enforce 6-digit PIN expiry and 5-minute lockout on 3 failed attempts.
    /// Rejects unauthorized control inputs when session is configured in 'view_only' scope.
    /// </summary>
    public class SessionCoordinator
    {
        private readonly PairingManager _pairingManager;
        private readonly ITlsControlServer _controlServer;
        private readonly ConcurrentDictionary<string, string> _trustedAuthTokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _sessionLock = new();

        public SessionStatus Status { get; private set; } = SessionStatus.Idle;
        public string? ActiveSessionId { get; private set; }
        public string? ActivePeerDeviceId { get; private set; }
        public string? ActiveScope { get; private set; }
        public DiscoveryPayload LocalDevicePayload { get; set; }
        public PairingManager PairingManager => _pairingManager;

        public event EventHandler<PairingChallengeEventArgs>? PairingChallengePresented;
        public event EventHandler<string>? SessionStarted;
        public event EventHandler<string>? SessionStopped;
        public event EventHandler<ProtocolEnvelope>? AuthorizedInputReceived;
        public event EventHandler<ProtocolEnvelope>? SignalingMessageReceived;

        public SessionCoordinator(ITlsControlServer controlServer, PairingManager? pairingManager = null, DiscoveryPayload? localPayload = null)
        {
            _controlServer = controlServer ?? throw new ArgumentNullException(nameof(controlServer));
            _pairingManager = pairingManager ?? new PairingManager();
            LocalDevicePayload = localPayload ?? new DiscoveryPayload(
                DeviceId: Guid.NewGuid().ToString("N"),
                DisplayName: Environment.MachineName,
                Platform: OperatingSystem.IsWindows() ? "windows" : (OperatingSystem.IsAndroid() ? "android" : "unknown"),
                ProtocolVersion: ProtocolEnvelope.CurrentProtocolVersion,
                ControlPort: controlServer.Port,
                Capabilities: new List<string> { "screen_view", "mouse_control", "keyboard_control" }
            );

            _controlServer.MessageReceived += OnServerMessageReceived;
            _controlServer.ClientDisconnected += OnClientDisconnected;
        }

        private async void OnServerMessageReceived(object? sender, ProtocolMessageEventArgs e)
        {
            try
            {
                await HandleEnvelopeAsync(e.Envelope).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorEnvelope = ProtocolEnvelope.Create(
                    MessageTypes.Error,
                    new { error = "HANDLER_EXCEPTION", message = ex.Message },
                    0);
                await _controlServer.SendEnvelopeAsync(errorEnvelope).ConfigureAwait(false);
            }
        }

        public async Task HandleEnvelopeAsync(ProtocolEnvelope envelope)
        {
            switch (envelope.Type)
            {
                case MessageTypes.DiscoveryQuery:
                    await HandleDiscoveryQueryAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.PairRequest:
                    await HandlePairRequestAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.PairConfirm:
                    await HandlePairConfirmAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.SessionStart:
                    await HandleSessionStartAsync(envelope).ConfigureAwait(false);
                    break;

                case MessageTypes.SessionStop:
                    await HandleSessionStopAsync(envelope).ConfigureAwait(false);
                    break;

                case WebRtcSignalingCoordinator.TypeOffer:
                case WebRtcSignalingCoordinator.TypeAnswer:
                case WebRtcSignalingCoordinator.TypeIceCandidate:
                    SignalingMessageReceived?.Invoke(this, envelope);
                    break;

                case MessageTypes.InputPointerMove:
                case MessageTypes.InputPointerButton:
                case MessageTypes.InputKey:
                case MessageTypes.InputTouchGesture:
                case MessageTypes.InputTvCommand:
                    HandleIncomingInput(envelope);
                    break;
            }
        }

        private async Task HandleDiscoveryQueryAsync(ProtocolEnvelope envelope)
        {
            var announcePayload = LocalDevicePayload with { ControlPort = _controlServer.Port > 0 ? _controlServer.Port : LocalDevicePayload.ControlPort };
            var announceEnvelope = ProtocolEnvelope.Create(MessageTypes.DiscoveryAnnounce, announcePayload, 0);
            await TrySendResponseAsync(announceEnvelope).ConfigureAwait(false);
        }

        public async Task SendSignalingEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            await _controlServer.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandlePairRequestAsync(ProtocolEnvelope envelope)
        {
            var req = envelope.Payload.Deserialize<PairRequestPayload>();
            if (req == null || string.IsNullOrWhiteSpace(req.ControllerId))
            {
                var err = ProtocolEnvelope.Create(MessageTypes.Error, new { error = "INVALID_PAYLOAD" }, 0);
                await _controlServer.SendEnvelopeAsync(err).ConfigureAwait(false);
                return;
            }

            lock (_sessionLock)
            {
                // Check if device is currently locked out
                if (_pairingManager.IsDeviceLockedOut(req.ControllerId, DateTime.UtcNow, out var lockedUntil))
                {
                    var lockedResult = ProtocolEnvelope.Create(
                        MessageTypes.PairResult,
                        new PairResultPayload("LOCKED_OUT", null, $"Device locked out until {lockedUntil:o}"),
                        0);
                    _ = _controlServer.SendEnvelopeAsync(lockedResult);
                    return;
                }

                var session = _pairingManager.CreateSession(req.ControllerId);
                ActivePeerDeviceId = req.ControllerId;
                Status = SessionStatus.PairingChallengePresented;

                // Fire event so host UI can display the 6-digit numeric PIN
                PairingChallengePresented?.Invoke(this, new PairingChallengeEventArgs(req.ControllerId, session.PinCode, 60));

                var challengePayload = new PairChallengePayload(
                    EndpointId: "LocalHostEndpoint",
                    PinCode: session.PinCode,
                    ExpiresInSeconds: 60,
                    ChallengeNonce: Guid.NewGuid().ToString("N")
                );

                var challengeEnvelope = ProtocolEnvelope.Create(MessageTypes.PairChallenge, challengePayload, 0);
                _ = TrySendResponseAsync(challengeEnvelope);
            }
        }

        private async Task HandlePairConfirmAsync(ProtocolEnvelope envelope)
        {
            var confirm = envelope.Payload.Deserialize<PairConfirmPayload>();
            if (confirm == null || string.IsNullOrWhiteSpace(confirm.ControllerId))
            {
                var err = ProtocolEnvelope.Create(MessageTypes.Error, new { error = "INVALID_PAYLOAD" }, 0);
                await TrySendResponseAsync(err).ConfigureAwait(false);
                return;
            }

            var result = _pairingManager.ValidateAttempt(confirm.ControllerId, confirm.PinCode);

            ProtocolEnvelope resultEnvelope;
            lock (_sessionLock)
            {
                if (result.Status == PairingStatus.Success)
                {
                    string authToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    _trustedAuthTokens[confirm.ControllerId] = authToken;
                    Status = SessionStatus.Paired;

                    resultEnvelope = ProtocolEnvelope.Create(
                        MessageTypes.PairResult,
                        new PairResultPayload("SUCCESS", authToken, null),
                        0);
                }
                else if (result.Status == PairingStatus.LockedOut)
                {
                    Status = SessionStatus.Idle;
                    resultEnvelope = ProtocolEnvelope.Create(
                        MessageTypes.PairResult,
                        new PairResultPayload("LOCKED_OUT", null, "Max failed pairing attempts exceeded. Locked out for 5 minutes."),
                        0);
                }
                else if (result.Status == PairingStatus.Expired)
                {
                    Status = SessionStatus.Idle;
                    resultEnvelope = ProtocolEnvelope.Create(
                        MessageTypes.PairResult,
                        new PairResultPayload("EXPIRED", null, "PIN code has expired. Request a new pairing session."),
                        0);
                }
                else
                {
                    resultEnvelope = ProtocolEnvelope.Create(
                        MessageTypes.PairResult,
                        new PairResultPayload("INVALID_PIN", null, $"Invalid PIN. Remaining attempts: {result.RemainingAttempts}"),
                        0);
                }
            }

            await TrySendResponseAsync(resultEnvelope).ConfigureAwait(false);
        }

        private async Task HandleSessionStartAsync(ProtocolEnvelope envelope)
        {
            var start = envelope.Payload.Deserialize<SessionStartPayload>();
            if (start == null || string.IsNullOrWhiteSpace(start.SessionId))
            {
                var err = ProtocolEnvelope.Create(MessageTypes.Error, new { error = "INVALID_SESSION_PAYLOAD" }, 0);
                await TrySendResponseAsync(err).ConfigureAwait(false);
                return;
            }

            string scope = SessionScopes.IsValid(start.RequestedScope) ? start.RequestedScope : SessionScopes.ViewOnly;

            lock (_sessionLock)
            {
                ActiveSessionId = start.SessionId;
                ActiveScope = scope;
                Status = SessionStatus.ActiveSession;
            }

            var acceptPayload = new SessionAcceptPayload(
                SessionId: start.SessionId,
                GrantedScope: scope,
                WebRtcOffer: null // WebRTC negotiation follows
            );

            var acceptEnvelope = ProtocolEnvelope.Create(MessageTypes.SessionAccept, acceptPayload, 0);
            await TrySendResponseAsync(acceptEnvelope).ConfigureAwait(false);

            SessionStarted?.Invoke(this, start.SessionId);
        }

        private async Task HandleSessionStopAsync(ProtocolEnvelope envelope)
        {
            var stop = envelope.Payload.Deserialize<SessionStopPayload>();
            string reason = stop?.Reason ?? "Peer requested session stop.";

            TerminateActiveSession(reason);

            var ackEnvelope = ProtocolEnvelope.Create(MessageTypes.SessionStop, new { status = "TERMINATED", reason }, 0);
            await TrySendResponseAsync(ackEnvelope).ConfigureAwait(false);
        }

        private async Task TrySendResponseAsync(ProtocolEnvelope envelope)
        {
            if (_controlServer.HasActiveClient)
            {
                try
                {
                    await _controlServer.SendEnvelopeAsync(envelope).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore transient disconnection or send failures during reply
                }
            }
        }

        private void HandleIncomingInput(ProtocolEnvelope envelope)
        {
            lock (_sessionLock)
            {
                if (Status != SessionStatus.ActiveSession)
                {
                    // Ignore inputs when no active session exists
                    return;
                }

                // Enforce scope boundary: strictly reject input when in view-only mode
                if (string.Equals(ActiveScope, SessionScopes.ViewOnly, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            AuthorizedInputReceived?.Invoke(this, envelope);
        }

        private void OnClientDisconnected(object? sender, PeerConnectionEventArgs e)
        {
            TerminateActiveSession(e.Reason ?? "Network connection terminated.");
        }

        public void TerminateActiveSession(string reason)
        {
            string? endedSessionId;
            lock (_sessionLock)
            {
                endedSessionId = ActiveSessionId;
                ActiveSessionId = null;
                ActiveScope = null;
                Status = SessionStatus.Idle;
            }

            if (endedSessionId != null)
            {
                SessionStopped?.Invoke(this, reason);
            }
        }
    }
}
