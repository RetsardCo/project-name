using System;
using System.Text.Json;
using ProjectName.Shared.Protocol;
using Xunit;

namespace ProjectName.Shared.Tests
{
    public class ProtocolTests
    {
        [Fact]
        public void Envelope_ValidMessage_PassesValidation()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"message_id\":\"msg-1\",\"sequence_number\":100,\"timestamp_utc_ms\":1700000000000,\"type\":\"INPUT_POINTER_MOVE\",\"payload\":{\"x_norm\":0.5,\"y_norm\":0.5}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 100);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Envelope);
            Assert.Equal("1.0", result.Envelope!.ProtocolVersion);
            Assert.Equal("INPUT_POINTER_MOVE", result.Envelope.Type);
            Assert.Equal(100, result.Envelope.SequenceNumber);
        }

        [Fact]
        public void Envelope_OversizedMessage_IsRejected()
        {
            string hugePayload = new string('A', ProtocolEnvelope.MaxMessageSizeBytes + 10);
            string raw = $"{{\"protocol_version\":\"1.0\",\"sequence_number\":1,\"type\":\"TEST\",\"payload\":\"{hugePayload}\"}}";

            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);

            Assert.False(result.IsValid);
            Assert.Contains("exceeds maximum allowed size", result.Error);
        }

        [Fact]
        public void Envelope_SequenceReplay_IsRejected()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"message_id\":\"msg-1\",\"sequence_number\":5,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 10);

            Assert.False(result.IsValid);
            Assert.Contains("Replay or out-of-order sequence", result.Error);
        }

        [Fact]
        public void Envelope_IncompatibleVersion_IsRejected()
        {
            var raw = "{\"protocol_version\":\"2.0\",\"message_id\":\"msg-1\",\"sequence_number\":1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);

            Assert.False(result.IsValid);
            Assert.Contains("Incompatible protocol version", result.Error);
        }

        [Theory]
        [InlineData(0.0, 0.0, 1920, 1080, 0, 0)]
        [InlineData(1.0, 1.0, 1920, 1080, 1919, 1079)]
        [InlineData(0.5, 0.5, 1920, 1080, 960, 540)]
        [InlineData(-0.5, 1.5, 1920, 1080, 0, 1079)] // Clamped
        public void CoordinateMapper_Portrait_MapsCorrectly(double xNorm, double yNorm, int w, int h, int expectedX, int expectedY)
        {
            var point = CoordinateMapper.MapNormalizedToPixel(xNorm, yNorm, w, h, ScreenOrientation.Portrait);
            Assert.Equal(expectedX, point.X);
            Assert.Equal(expectedY, point.Y);
        }

        [Fact]
        public void CoordinateMapper_Landscape90_RotatesCorrectly()
        {
            // Point at normalized (0.2, 0.8) rotated 90 degrees clockwise:
            // x_mapped = 1 - 0.8 = 0.2; y_mapped = 0.2
            var point = CoordinateMapper.MapNormalizedToPixel(0.2, 0.8, 1000, 1000, ScreenOrientation.LandscapeRight);
            Assert.Equal(200, point.X);
            Assert.Equal(200, point.Y);
        }

        [Fact]
        public void PairingSession_CorrectPin_Succeeds()
        {
            var session = new PairingSession("dev-1", "123456");
            var result = session.ValidateAttempt("123456");

            Assert.Equal(PairingStatus.Success, result.Status);
        }

        [Fact]
        public void PairingSession_ExpiredPin_IsRejected()
        {
            var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var session = new PairingSession("dev-1", "123456", baseTime);

            // Attempt at 61 seconds later
            var checkTime = baseTime.AddSeconds(61);
            var result = session.ValidateAttempt("123456", checkTime);

            Assert.Equal(PairingStatus.Expired, result.Status);
        }

        [Fact]
        public void PairingSession_RateLimiting_LocksOutAfterThreeFailures()
        {
            var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var session = new PairingSession("dev-1", "123456", baseTime);

            var r1 = session.ValidateAttempt("000000", baseTime);
            Assert.Equal(PairingStatus.InvalidPin, r1.Status);
            Assert.Equal(2, r1.RemainingAttempts);

            var r2 = session.ValidateAttempt("000001", baseTime);
            Assert.Equal(PairingStatus.InvalidPin, r2.Status);
            Assert.Equal(1, r2.RemainingAttempts);

            var r3 = session.ValidateAttempt("000002", baseTime);
            Assert.Equal(PairingStatus.LockedOut, r3.Status);
            Assert.NotNull(r3.LockedUntilUtc);
            Assert.Equal(baseTime.AddMinutes(5), r3.LockedUntilUtc!.Value);

            // Even correct PIN during lockout is rejected
            var r4 = session.ValidateAttempt("123456", baseTime.AddMinutes(1));
            Assert.Equal(PairingStatus.LockedOut, r4.Status);
        }

        [Fact]
        public void DeviceCapabilities_FlagsAndNames_RoundTrip()
        {
            var flags = DeviceCapabilityFlags.ScreenView | DeviceCapabilityFlags.PointerControl | DeviceCapabilityFlags.TvNavigation;
            var names = CapabilityNames.ToNames(flags);
            var roundTripped = CapabilityNames.FromNames(names);

            Assert.Equal(flags, roundTripped);
            Assert.True(roundTripped.HasFlag(DeviceCapabilityFlags.ScreenView));
            Assert.True(roundTripped.HasFlag(DeviceCapabilityFlags.PointerControl));
            Assert.True(roundTripped.HasFlag(DeviceCapabilityFlags.TvNavigation));
            Assert.False(roundTripped.HasFlag(DeviceCapabilityFlags.TouchGestures));
        }
    }
}
