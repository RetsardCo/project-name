using System;
using System.Collections.Generic;
using System.Text.Json;
using ProjectName.Shared.Discovery;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Session;
using ProjectName.Shared.Transport;
using Xunit;

namespace ProjectName.Shared.Tests
{
    public class DiscoveryTests
    {
        [Fact]
        public void DiscoveryPayload_Serialization_PreservesAllRequiredFields()
        {
            var payload = new DiscoveryPayload(
                DeviceId: "device-xyz",
                DisplayName: "Living Room PC",
                Platform: "windows",
                ProtocolVersion: "1.0",
                ControlPort: 8443,
                Capabilities: new List<string> { "screen_view", "mouse_control", "keyboard_control" }
            );

            var env = ProtocolEnvelope.Create(MessageTypes.DiscoveryAnnounce, payload, 0);
            string json = JsonSerializer.Serialize(env);

            var validation = ProtocolEnvelope.Validate(json, 0);
            Assert.True(validation.IsValid);
            Assert.NotNull(validation.Envelope);

            var deserialized = validation.Envelope!.Payload.Deserialize<DiscoveryPayload>();
            Assert.NotNull(deserialized);
            Assert.Equal("device-xyz", deserialized!.DeviceId);
            Assert.Equal("Living Room PC", deserialized.DisplayName);
            Assert.Equal("windows", deserialized.Platform);
            Assert.Equal(8443, deserialized.ControlPort);
            Assert.Equal(3, deserialized.Capabilities.Count);
        }

        [Fact]
        public void DiscoveredDevice_RecordsEndpointMetadata()
        {
            var device = new DiscoveredDevice(
                DeviceId: "phone-1",
                DisplayName: "Redmi Note 10",
                Platform: "android",
                HostOrIp: "192.168.1.50",
                ControlPort: 8443,
                Capabilities: new List<string> { "screen_view", "touch_gesture" },
                LastSeenUtc: DateTime.UtcNow
            );

            Assert.Equal("phone-1", device.DeviceId);
            Assert.Equal("Redmi Note 10", device.DisplayName);
            Assert.Equal("android", device.Platform);
            Assert.Equal("192.168.1.50", device.HostOrIp);
            Assert.Equal(8443, device.ControlPort);
            Assert.Equal(2, device.Capabilities.Count);
        }

        [Fact]
        public async Task DirectConnectService_ProbeEndpointAsync_SuccessfullyQueriesDeviceOverTls()
        {
            var serverPayload = new DiscoveryPayload(
                DeviceId: "probe-target-device",
                DisplayName: "Living Room PC",
                Platform: "windows",
                ProtocolVersion: "1.0",
                ControlPort: 0,
                Capabilities: new List<string> { "screen_view", "mouse_control" }
            );

            await using var server = new TlsControlServer(port: 0);
            var coordinator = new SessionCoordinator(server, localPayload: serverPayload);
            await server.StartAsync();

            var discovered = await DirectConnectService.ProbeEndpointAsync("127.0.0.1", server.Port);

            Assert.NotNull(discovered);
            Assert.Equal("probe-target-device", discovered.DeviceId);
            Assert.Equal("Living Room PC", discovered.DisplayName);
            Assert.Equal("windows", discovered.Platform);
            Assert.Equal(server.Port, discovered.ControlPort);
            Assert.Contains("screen_view", discovered.Capabilities);
            Assert.Contains("mouse_control", discovered.Capabilities);

            await server.StopAsync();
        }

        [Fact]
        public async Task MdnsDiscoveryBeacon_StartsAndStopsCleanly()
        {
            var localPayload = new DiscoveryPayload(
                DeviceId: "beacon-device-1",
                DisplayName: "Beacon Test",
                Platform: "windows",
                ProtocolVersion: "1.0",
                ControlPort: 9000,
                Capabilities: new List<string> { "screen_view" }
            );

            await using var beacon = new MdnsDiscoveryBeacon(localPayload);
            await beacon.StartAsync();

            // Query should not throw
            await beacon.QueryPeersAsync();
            await beacon.AnnouncePresenceAsync();

            await beacon.StopAsync();
        }
    }
}
