using System;
using System.Collections.Generic;
using System.Text.Json;
using ProjectName.Shared.Discovery;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
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
    }
}
