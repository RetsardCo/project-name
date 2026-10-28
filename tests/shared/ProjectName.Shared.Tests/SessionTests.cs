using System;
using System.Text.Json;
using System.Threading.Tasks;
using ProjectName.Shared.Media;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Session;
using ProjectName.Shared.Transport;
using Xunit;

namespace ProjectName.Shared.Tests
{
    public class SessionTests
    {
        [Fact]
        public async Task SessionCoordinator_SuccessfulPairingFlow_GeneratesAuthToken()
        {
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);

            string? presentedPin = null;
            coordinator.PairingChallengePresented += (_, e) =>
            {
                presentedPin = e.PinCode;
            };

            // 1. Controller sends PAIR_REQUEST
            var pairReq = ProtocolEnvelope.Create(
                MessageTypes.PairRequest,
                new PairRequestPayload("ctrl-123", "Test Controller", "pubkey-xyz"),
                1);
            await coordinator.HandleEnvelopeAsync(pairReq);

            Assert.Equal(SessionStatus.PairingChallengePresented, coordinator.Status);
            Assert.NotNull(presentedPin);
            Assert.Equal(6, presentedPin!.Length);

            // 2. Controller submits matching PIN in PAIR_CONFIRM
            var pairConfirm = ProtocolEnvelope.Create(
                MessageTypes.PairConfirm,
                new PairConfirmPayload("ctrl-123", presentedPin, "signed-nonce-abc"),
                2);
            await coordinator.HandleEnvelopeAsync(pairConfirm);

            Assert.Equal(SessionStatus.Paired, coordinator.Status);
        }

        [Fact]
        public async Task SessionCoordinator_LockoutAfterThreeFailedAttempts()
        {
            var pairingManager = new PairingManager();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server, pairingManager);

            // Initiate pairing
            var pairReq = ProtocolEnvelope.Create(
                MessageTypes.PairRequest,
                new PairRequestPayload("attacker-device", "Attacker", "key"),
                1);
            await coordinator.HandleEnvelopeAsync(pairReq);

            // Submit 3 invalid PINs
            for (int i = 0; i < 3; i++)
            {
                var invalidConfirm = ProtocolEnvelope.Create(
                    MessageTypes.PairConfirm,
                    new PairConfirmPayload("attacker-device", "000000", "nonce"),
                    i + 2);
                await coordinator.HandleEnvelopeAsync(invalidConfirm);
            }

            // Verify device is locked out in PairingManager
            Assert.True(pairingManager.IsDeviceLockedOut("attacker-device", DateTime.UtcNow, out var lockedUntil));
            Assert.NotNull(lockedUntil);
            Assert.True(lockedUntil > DateTime.UtcNow);
        }

        [Fact]
        public async Task SessionCoordinator_EnforcesScopeBoundaries_ViewOnlyRejectsInput()
        {
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);

            bool inputDispatched = false;
            coordinator.AuthorizedInputReceived += (_, _) => inputDispatched = true;

            // Start session in 'view_only' scope
            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-456", SessionScopes.ViewOnly, new ResolutionSpec(1920, 1080, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);

            Assert.Equal(SessionStatus.ActiveSession, coordinator.Status);
            Assert.Equal(SessionScopes.ViewOnly, coordinator.ActiveScope);

            // Attempt to send pointer move input
            var moveEnv = ProtocolEnvelope.Create(
                MessageTypes.InputPointerMove,
                new PointerMovePayload(0.5, 0.5),
                2);
            await coordinator.HandleEnvelopeAsync(moveEnv);

            // Must NOT dispatch input when in view-only scope
            Assert.False(inputDispatched);

            // Now start session in 'view_and_control' scope
            var controlEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-789", SessionScopes.ViewAndControl, new ResolutionSpec(1920, 1080, 30)),
                3);
            await coordinator.HandleEnvelopeAsync(controlEnv);

            Assert.Equal(SessionScopes.ViewAndControl, coordinator.ActiveScope);

            // Send pointer move input again
            var moveEnv2 = ProtocolEnvelope.Create(
                MessageTypes.InputPointerMove,
                new PointerMovePayload(0.5, 0.5),
                4);
            await coordinator.HandleEnvelopeAsync(moveEnv2);

            // Must dispatch input in view-and-control scope
            Assert.True(inputDispatched);
        }

        [Fact]
        public async Task SessionCoordinator_SessionStop_ResetsSessionStatus()
        {
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);

            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-abc", SessionScopes.ViewAndControl, new ResolutionSpec(1280, 720, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);
            Assert.Equal(SessionStatus.ActiveSession, coordinator.Status);

            var stopEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStop,
                new SessionStopPayload("session-abc", "User requested stop."),
                2);
            await coordinator.HandleEnvelopeAsync(stopEnv);

            Assert.Equal(SessionStatus.Idle, coordinator.Status);
            Assert.Null(coordinator.ActiveSessionId);
        }

        [Fact]
        public async Task SessionCoordinator_FullEndToEndPairingOverTls_SucceedsWithoutReplayRejection()
        {
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            await server.StartAsync();

            await using var client = new TlsControlClient();
            var challengeTcs = new TaskCompletionSource<PairChallengePayload>();
            var resultTcs = new TaskCompletionSource<PairResultPayload>();

            client.MessageReceived += (_, e) =>
            {
                if (e.Envelope.Type == MessageTypes.PairChallenge)
                {
                    var ch = e.Envelope.Payload.Deserialize<PairChallengePayload>();
                    if (ch != null) challengeTcs.TrySetResult(ch);
                }
                else if (e.Envelope.Type == MessageTypes.PairResult)
                {
                    var res = e.Envelope.Payload.Deserialize<PairResultPayload>();
                    if (res != null) resultTcs.TrySetResult(res);
                }
            };

            await client.ConnectAsync("127.0.0.1", server.Port);

            // 1. Send PAIR_REQUEST from client
            await client.SendMessageAsync(
                MessageTypes.PairRequest,
                new PairRequestPayload("controller-device-1", "Mobile App", "pubkey-123"));

            // Wait for challenge (server responds with PAIR_CHALLENGE)
            var challengeWait = await Task.WhenAny(challengeTcs.Task, Task.Delay(3000));
            Assert.Equal(challengeTcs.Task, challengeWait);
            var challenge = await challengeTcs.Task;
            Assert.NotNull(challenge.PinCode);
            Assert.Equal(6, challenge.PinCode.Length);

            // 2. Submit matching PIN in PAIR_CONFIRM
            await client.SendMessageAsync(
                MessageTypes.PairConfirm,
                new PairConfirmPayload("controller-device-1", challenge.PinCode, "signed-nonce-123"));

            // Wait for PAIR_RESULT from server
            var resultWait = await Task.WhenAny(resultTcs.Task, Task.Delay(3000));
            Assert.Equal(resultTcs.Task, resultWait);
            var result = await resultTcs.Task;

            Assert.Equal("SUCCESS", result.Status);
            Assert.NotNull(result.AuthToken);
            Assert.True(client.IsConnected); // Must NOT be terminated due to sequence replay!

            await client.DisconnectAsync();
            await server.StopAsync();
        }

        [Fact]
        public async Task SessionCoordinator_WebRtcSignaling_RoutesToSignalingEvent()
        {
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);

            ProtocolEnvelope? receivedSignalingEnv = null;
            coordinator.SignalingMessageReceived += (_, env) => receivedSignalingEnv = env;

            var offerEnv = ProtocolEnvelope.Create(
                WebRtcSignalingCoordinator.TypeOffer,
                new WebRtcSignalingPayload("sess-1", "v=0\r\no=controller...", null, null, null),
                1);

            await coordinator.HandleEnvelopeAsync(offerEnv);

            Assert.NotNull(receivedSignalingEnv);
            Assert.Equal(WebRtcSignalingCoordinator.TypeOffer, receivedSignalingEnv!.Type);
        }
    }
}
