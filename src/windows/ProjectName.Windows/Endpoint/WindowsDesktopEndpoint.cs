using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Session;
using ProjectName.Shared.Transport;
using ProjectName.Windows.Capture;
using ProjectName.Windows.Input;

namespace ProjectName.Windows.Endpoint
{
    /// <summary>
    /// Unified Windows Desktop Endpoint.
    /// Combines TLS control channel server, pairing verification, bounded video capture buffer,
    /// and WindowsInputInjector with automatic input release on disconnect and emergency abort.
    /// </summary>
    public class WindowsDesktopEndpoint : IAsyncDisposable
    {
        private readonly ITlsControlServer _controlServer;
        private readonly SessionCoordinator _sessionCoordinator;
        private readonly IWindowsInputInjector _inputInjector;
        private readonly WindowsCaptureBuffer _captureBuffer;
        private readonly WindowsDesktopCaptureService _captureService;
        private readonly object _lock = new();

        public ITlsControlServer ControlServer => _controlServer;
        public SessionCoordinator SessionCoordinator => _sessionCoordinator;
        public IWindowsInputInjector InputInjector => _inputInjector;
        public WindowsCaptureBuffer CaptureBuffer => _captureBuffer;
        public bool IsRunning { get; private set; }

        public event EventHandler<string>? EmergencyDisconnectTriggered;

        public WindowsDesktopEndpoint(
            ITlsControlServer? controlServer = null,
            SessionCoordinator? sessionCoordinator = null,
            IWindowsInputInjector? inputInjector = null,
            WindowsCaptureBuffer? captureBuffer = null)
        {
            _captureBuffer = captureBuffer ?? new WindowsCaptureBuffer(maxCapacity: 2);
            _inputInjector = inputInjector ?? new WindowsInputInjector();
            _controlServer = controlServer ?? new TlsControlServer();
            _sessionCoordinator = sessionCoordinator ?? new SessionCoordinator(_controlServer);
            _captureService = new WindowsDesktopCaptureService(_captureBuffer);

            // Wire input dispatch
            _sessionCoordinator.AuthorizedInputReceived += OnAuthorizedInputReceived;

            // Wire disconnect safety: immediately release all held inputs when session terminates
            _sessionCoordinator.SessionStopped += OnSessionStopped;
            _controlServer.ClientDisconnected += OnClientDisconnected;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (IsRunning) return;
                IsRunning = true;
            }

            await _controlServer.StartAsync(cancellationToken).ConfigureAwait(false);
            await _captureService.StartCaptureAsync(1920, 1080, cancellationToken).ConfigureAwait(false);
        }

        private void OnAuthorizedInputReceived(object? sender, ProtocolEnvelope envelope)
        {
            try
            {
                switch (envelope.Type)
                {
                    case MessageTypes.InputPointerMove:
                        var move = envelope.Payload.Deserialize<PointerMovePayload>();
                        if (move != null)
                        {
                            var (cx, cy) = CoordinateMapper.ClampNormalized(move.XNorm, move.YNorm);
                            _inputInjector.InjectPointerMove(cx, cy);
                        }
                        break;

                    case MessageTypes.InputPointerButton:
                        var btn = envelope.Payload.Deserialize<PointerButtonPayload>();
                        if (btn != null)
                        {
                            _inputInjector.InjectPointerButton(btn.Button, btn.Action);
                        }
                        break;

                    case MessageTypes.InputKey:
                        var key = envelope.Payload.Deserialize<KeyPayload>();
                        if (key != null)
                        {
                            _inputInjector.InjectKey(key.KeyCode, key.Action, key.Modifiers);
                        }
                        break;
                }
            }
            catch
            {
                // Ignore transient input errors
            }
        }

        private void OnSessionStopped(object? sender, string reason)
        {
            // Non-Negotiable Safety Invariant: Disconnect immediately clears all held keys and mouse buttons
            _inputInjector.ReleaseAllHeldInputs();
            _captureBuffer.Clear();
        }

        private void OnClientDisconnected(object? sender, PeerConnectionEventArgs e)
        {
            // Non-Negotiable Safety Invariant: Socket drop immediately clears all held keys and mouse buttons
            _inputInjector.ReleaseAllHeldInputs();
            _captureBuffer.Clear();
        }

        /// <summary>
        /// Immediately terminates the active session from the Windows host and resets all held inputs.
        /// </summary>
        public async Task EmergencyDisconnectAsync(string reason = "Emergency disconnect invoked by user.")
        {
            // Reset all inputs first
            _inputInjector.ReleaseAllHeldInputs();
            _captureBuffer.Clear();

            _sessionCoordinator.TerminateActiveSession(reason);
            await _controlServer.DisconnectCurrentClientAsync(reason).ConfigureAwait(false);

            EmergencyDisconnectTriggered?.Invoke(this, reason);
        }

        public async Task StopAsync()
        {
            lock (_lock)
            {
                if (!IsRunning) return;
                IsRunning = false;
            }

            _inputInjector.ReleaseAllHeldInputs();
            _captureBuffer.Clear();

            await _captureService.StopCaptureAsync().ConfigureAwait(false);
            await _controlServer.StopAsync().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            await _captureService.DisposeAsync().ConfigureAwait(false);
            await _controlServer.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
