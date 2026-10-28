using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    }
}
