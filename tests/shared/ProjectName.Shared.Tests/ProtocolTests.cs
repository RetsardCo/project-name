using System;
using System.Collections.Generic;
using System.Text.Json;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
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
            string raw = $"{{\"protocol_version\":\"1.0\",\"message_id\":\"msg-huge\",\"sequence_number\":1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":\"{hugePayload}\"}}";

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
        public void Envelope_NegativeSequenceNumber_IsRejected()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"message_id\":\"msg-neg\",\"sequence_number\":-1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 0);

            Assert.False(result.IsValid);
            Assert.Contains("cannot be negative", result.Error);
        }

        [Fact]
        public void Envelope_InvalidTimestamp_IsRejected()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"message_id\":\"msg-ts\",\"sequence_number\":1,\"timestamp_utc_ms\":0,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);

            Assert.False(result.IsValid);
            Assert.Contains("positive integer", result.Error);
        }

        [Fact]
        public void Envelope_IncompatibleVersion_IsRejected()
        {
            var raw = "{\"protocol_version\":\"2.0\",\"message_id\":\"msg-1\",\"sequence_number\":1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);

            Assert.False(result.IsValid);
            Assert.Contains("Incompatible protocol version", result.Error);
        }

        [Fact]
        public void Envelope_BlankMessageId_IsRejected()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"message_id\":\"\",\"sequence_number\":1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);
            Assert.False(result.IsValid);
            Assert.Contains("Message ID cannot be null or blank", result.Error);
        }

        [Fact]
        public void Envelope_MissingMessageId_IsRejected()
        {
            var raw = "{\"protocol_version\":\"1.0\",\"sequence_number\":1,\"timestamp_utc_ms\":1700000000000,\"type\":\"TEST\",\"payload\":{}}";
            var result = ProtocolEnvelope.Validate(raw, expectedNextSequence: 1);
            Assert.False(result.IsValid);
            Assert.Contains("Message ID cannot be null or blank", result.Error);
        }

        [Fact]
        public void Envelope_SpanByteValidation_Succeeds()
        {
            var envelope = ProtocolEnvelope.Create(MessageTypes.InputTvCommand, new TvCommandPayload("dpad_up"), 5);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(envelope);

            var result = ProtocolEnvelope.Validate(bytes.AsSpan(), expectedNextSequence: 5);
            Assert.True(result.IsValid);
            Assert.Equal(MessageTypes.InputTvCommand, result.Envelope!.Type);
        }

        [Fact]
        public void SequenceTracker_EnforcesStrictMonotonicityAndRejectsDuplicates()
        {
            var tracker = new SequenceTracker();
            Assert.Equal(0, tracker.NextExpectedSequence);

            Assert.True(tracker.TryAccept(0, out var err));
            Assert.Null(err);
            Assert.Equal(1, tracker.NextExpectedSequence);

            // Replay same sequence number: must reject
            Assert.False(tracker.TryAccept(0, out err));
            Assert.Contains("Replay or duplicate sequence", err);

            // Backwards sequence number: must reject
            Assert.False(tracker.TryAccept(-1, out err));
            Assert.Contains("cannot be negative", err);

            // Monotonic advancement: accept
            Assert.True(tracker.TryAccept(1, out err));
            Assert.Equal(2, tracker.NextExpectedSequence);

            // Forward jump: accept
            Assert.True(tracker.TryAccept(10, out err));
            Assert.Equal(11, tracker.NextExpectedSequence);
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
        public void CoordinateMapper_Landscape90_NonSquareRotation()
        {
            // Controller viewport sends normalized (x=0.0, y=1.0) on a 1920x1080 display rotated 90 degrees:
            // mappedX = (1 - 1.0) = 0.0 -> pixelX = 0
            // mappedY = 0.0 -> pixelY = 0
            var point1 = CoordinateMapper.MapNormalizedToPixel(0.0, 1.0, 1920, 1080, ScreenOrientation.LandscapeRight);
            Assert.Equal(0, point1.X);
            Assert.Equal(0, point1.Y);

            // Controller normalized (x=1.0, y=0.0):
            // mappedX = (1 - 0.0) = 1.0 -> pixelX = 1919
            // mappedY = 1.0 -> pixelY = 1079
            var point2 = CoordinateMapper.MapNormalizedToPixel(1.0, 0.0, 1920, 1080, ScreenOrientation.LandscapeRight);
            Assert.Equal(1919, point2.X);
            Assert.Equal(1079, point2.Y);
        }

        [Fact]
        public void CoordinateMapper_NaN_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CoordinateMapper.MapNormalizedToPixel(double.NaN, 0.5, 1920, 1080));
            Assert.Throws<ArgumentException>(() => CoordinateMapper.MapNormalizedToPixel(0.5, double.NaN, 1920, 1080));
        }

        [Fact]
        public void CoordinateMapper_Infinity_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CoordinateMapper.MapNormalizedToPixel(double.PositiveInfinity, 0.5, 1920, 1080));
            Assert.Throws<ArgumentException>(() => CoordinateMapper.MapNormalizedToPixel(0.5, double.NegativeInfinity, 1920, 1080));
        }

        [Fact]
        public void CoordinateMapper_InverseMapping_MapsCorrectly()
        {
            var norm = CoordinateMapper.MapPixelToNormalized(960, 540, 1921, 1081);
            Assert.Equal(0.5, norm.X, precision: 2);
            Assert.Equal(0.5, norm.Y, precision: 2);
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
        public void PairingManager_TracksDeviceLockoutAcrossNewSessions()
        {
            var manager = new PairingManager();
            var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var session = manager.CreateSession("device-rogue", baseTime);

            // 3 failed attempts
            manager.ValidateAttempt("device-rogue", "wrong1", baseTime);
            manager.ValidateAttempt("device-rogue", "wrong2", baseTime);
            var r3 = manager.ValidateAttempt("device-rogue", "wrong3", baseTime);

            Assert.Equal(PairingStatus.LockedOut, r3.Status);

            // Malicious attacker tries to bypass lockout by requesting a new session immediately
            Assert.Throws<InvalidOperationException>(() =>
                manager.CreateSession("device-rogue", baseTime.AddSeconds(10)));

            // After 5 minutes, lockout expires and new session is allowed
            var allowedSession = manager.CreateSession("device-rogue", baseTime.AddMinutes(5).AddSeconds(1));
            Assert.NotNull(allowedSession);
        }

        [Fact]
        public void PairingManager_PrunesExpiredSessions()
        {
            var manager = new PairingManager();
            var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            manager.CreateSession("device-old", baseTime);
            manager.PruneExpired(baseTime.AddMinutes(2));

            // Should be expired now
            var result = manager.ValidateAttempt("device-old", "123456", baseTime.AddMinutes(2));
            Assert.Equal(PairingStatus.Expired, result.Status);
        }

        [Fact]
        public void DeviceCapabilities_FromNames_RecognizesMouseControlAlias()
        {
            var names = new[] { "mouse_control", "view_screen", "touch_input" };
            var flags = CapabilityNames.FromNames(names);

            Assert.True(flags.HasFlag(DeviceCapabilityFlags.PointerControl));
            Assert.True(flags.HasFlag(DeviceCapabilityFlags.ScreenView));
            Assert.True(flags.HasFlag(DeviceCapabilityFlags.TouchGestures));
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

        [Fact]
        public void ProtocolPayloads_Serialization_RoundTripsCorrectly()
        {
            var move = new PointerMovePayload(0.42, 0.88);
            var envelope = ProtocolEnvelope.Create(MessageTypes.InputPointerMove, move, 42);

            string json = JsonSerializer.Serialize(envelope);
            var validation = ProtocolEnvelope.Validate(json, expectedNextSequence: 42);

            Assert.True(validation.IsValid);
            Assert.NotNull(validation.Envelope);

            var deserializedMove = JsonSerializer.Deserialize<PointerMovePayload>(validation.Envelope!.Payload.GetRawText());
            Assert.NotNull(deserializedMove);
            Assert.Equal(0.42, deserializedMove!.XNorm);
            Assert.Equal(0.88, deserializedMove.YNorm);
        }
    }
}
