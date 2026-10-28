using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Media;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Session;
using ProjectName.Shared.Transport;
using ProjectName.Windows.Capture;
using ProjectName.Windows.Endpoint;
using ProjectName.Windows.Input;
using Xunit;

namespace ProjectName.Windows.Tests
{
    public class MockInputInjector : IWindowsInputInjector
    {
        private readonly List<string> _heldKeys = new();
        private readonly List<string> _heldButtons = new();

        public IReadOnlyCollection<string> HeldKeys => _heldKeys;
        public IReadOnlyCollection<string> HeldButtons => _heldButtons;

        public int MoveCount { get; private set; }
        public int ButtonCount { get; private set; }
        public int KeyCount { get; private set; }
        public int ReleaseCount { get; private set; }

        public void InjectPointerMove(double xNorm, double yNorm)
        {
            MoveCount++;
        }

        public void InjectPointerButton(string button, string action)
        {
            ButtonCount++;
            if (action == "down") _heldButtons.Add(button);
            else if (action == "up") _heldButtons.Remove(button);
        }

        public void InjectKey(string keyCode, string action, IEnumerable<string>? modifiers = null)
        {
            KeyCount++;
            if (action == "down") _heldKeys.Add(keyCode);
            else if (action == "up") _heldKeys.Remove(keyCode);
        }

        public void ReleaseAllHeldInputs()
        {
            ReleaseCount++;
            _heldKeys.Clear();
            _heldButtons.Clear();
        }
    }

    public class WindowsDesktopEndpointTests
    {
        [Fact]
        public async Task WindowsDesktopEndpoint_AcceptsInput_InViewAndControlScope()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            // Start active session in view_and_control mode
            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-win-1", SessionScopes.ViewAndControl, new ResolutionSpec(1920, 1080, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);

            // Send move, button, and key inputs
            await coordinator.HandleEnvelopeAsync(ProtocolEnvelope.Create(
                MessageTypes.InputPointerMove,
                new PointerMovePayload(0.2, 0.4),
                2));

            await coordinator.HandleEnvelopeAsync(ProtocolEnvelope.Create(
                MessageTypes.InputPointerButton,
                new PointerButtonPayload("left", "click"),
                3));

            await coordinator.HandleEnvelopeAsync(ProtocolEnvelope.Create(
                MessageTypes.InputKey,
                new KeyPayload("Enter", "down", null),
                4));

            Assert.Equal(1, mockInjector.MoveCount);
            Assert.Equal(1, mockInjector.ButtonCount);
            Assert.Equal(1, mockInjector.KeyCount);
        }

        [Fact]
        public async Task WindowsDesktopEndpoint_RejectsInput_InViewOnlyScope()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            // Start active session in view_only mode
            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-win-2", SessionScopes.ViewOnly, new ResolutionSpec(1920, 1080, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);

            // Send inputs
            await coordinator.HandleEnvelopeAsync(ProtocolEnvelope.Create(
                MessageTypes.InputPointerMove,
                new PointerMovePayload(0.5, 0.5),
                2));

            await coordinator.HandleEnvelopeAsync(ProtocolEnvelope.Create(
                MessageTypes.InputKey,
                new KeyPayload("Space", "down", null),
                3));

            // Must NOT have injected any inputs
            Assert.Equal(0, mockInjector.MoveCount);
            Assert.Equal(0, mockInjector.KeyCount);
        }

        [Fact]
        public async Task WindowsDesktopEndpoint_DisconnectSafety_ReleasesAllHeldInputs()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-win-3", SessionScopes.ViewAndControl, new ResolutionSpec(1920, 1080, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);

            // Simulate holding a key down
            mockInjector.InjectKey("Shift", "down");
            Assert.NotEmpty(mockInjector.HeldKeys);

            // Stop session
            var stopEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStop,
                new SessionStopPayload("session-win-3", "Normal stop"),
                2);
            await coordinator.HandleEnvelopeAsync(stopEnv);

            // ReleaseAllHeldInputs must have been called
            Assert.True(mockInjector.ReleaseCount > 0);
            Assert.Empty(mockInjector.HeldKeys);
        }

        [Fact]
        public async Task WindowsDesktopEndpoint_EmergencyDisconnect_ImmediatelyAbortsSessionAndInputs()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            buffer.PushFrame(new byte[] { 1, 2, 3 }, 100, 100, 100);
            Assert.Equal(1, buffer.QueuedFramesCount);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            var startEnv = ProtocolEnvelope.Create(
                MessageTypes.SessionStart,
                new SessionStartPayload("session-win-4", SessionScopes.ViewAndControl, new ResolutionSpec(1920, 1080, 30)),
                1);
            await coordinator.HandleEnvelopeAsync(startEnv);

            mockInjector.InjectPointerButton("left", "down");

            // Host user hits emergency disconnect
            await endpoint.EmergencyDisconnectAsync("Host triggered emergency disconnect");

            Assert.True(mockInjector.ReleaseCount > 0);
            Assert.Empty(mockInjector.HeldButtons);
            Assert.Equal(0, buffer.QueuedFramesCount);
            Assert.Equal(SessionStatus.Idle, coordinator.Status);
        }

        [Fact]
        public async Task WindowsDesktopEndpoint_CreateMediaPipeline_StreamsFramesFromCaptureBuffer()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            var mockTransport = new MockEndpointMediaTransport();
            var pipeline = endpoint.CreateMediaPipeline(mockTransport, targetFps: 60);

            // Push frame into endpoint's buffer
            buffer.PushFrame(new byte[] { 0xAA, 0xBB, 0xCC }, 1920, 1080, 5000);

            await pipeline.StartAsync();
            await Task.Delay(100);
            await pipeline.StopAsync();

            Assert.True(mockTransport.FrameCount > 0);
            Assert.NotNull(endpoint.MediaPipeline);
        }

        [Fact]
        public async Task WindowsDesktopEndpoint_SignalingCoordinator_ReceivesWebRtcEnvelopes()
        {
            var mockInjector = new MockInputInjector();
            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server);
            var buffer = new WindowsCaptureBuffer(2);

            await using var endpoint = new WindowsDesktopEndpoint(
                server,
                coordinator,
                mockInjector,
                buffer);

            string? receivedOffer = null;
            endpoint.SignalingCoordinator.OfferReceived += (_, payload) => receivedOffer = payload.Sdp;

            var offerEnv = ProtocolEnvelope.Create(
                WebRtcSignalingCoordinator.TypeOffer,
                new WebRtcSignalingPayload("session-win-webrtc", "v=0\r\no=win...", null, null, null),
                1);

            await coordinator.HandleEnvelopeAsync(offerEnv);

            Assert.Equal("v=0\r\no=win...", receivedOffer);
        }
    }

    public class MockEndpointMediaTransport : IMediaStreamingTransport
    {
        public bool IsStreaming { get; private set; }
        public int FrameCount { get; private set; }

        public event EventHandler<MediaFrameEventArgs>? FrameReceived;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            IsStreaming = true;
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            IsStreaming = false;
            return Task.CompletedTask;
        }

        public Task SendVideoFrameAsync(byte[] frameData, int width, int height, long timestampMs, CancellationToken cancellationToken = default)
        {
            FrameCount++;
            FrameReceived?.Invoke(this, new MediaFrameEventArgs(frameData, width, height, timestampMs));
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsStreaming = false;
            return ValueTask.CompletedTask;
        }
    }
}
