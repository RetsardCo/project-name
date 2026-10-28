using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectName.Shared.Protocol
{
    public static class MessageTypes
    {
        public const string DiscoveryAnnounce = "DISCOVERY_ANNOUNCE";
        public const string DiscoveryQuery = "DISCOVERY_QUERY";
        public const string PairRequest = "PAIR_REQUEST";
        public const string PairChallenge = "PAIR_CHALLENGE";
        public const string PairConfirm = "PAIR_CONFIRM";
        public const string PairResult = "PAIR_RESULT";
        public const string SessionStart = "SESSION_START";
        public const string SessionAccept = "SESSION_ACCEPT";
        public const string SessionStop = "SESSION_STOP";
        public const string InputPointerMove = "INPUT_POINTER_MOVE";
        public const string InputPointerButton = "INPUT_POINTER_BUTTON";
        public const string InputKey = "INPUT_KEY";
        public const string InputTouchGesture = "INPUT_TOUCH_GESTURE";
        public const string InputTvCommand = "INPUT_TV_COMMAND";
        public const string DiagnosticsReport = "DIAGNOSTICS_REPORT";
        public const string Error = "ERROR";
    }

    public class ProtocolEnvelope
    {
        public const string CurrentProtocolVersion = "1.0";
        public const int MaxMessageSizeBytes = 65536; // 64 KB

        [JsonPropertyName("protocol_version")]
        public string ProtocolVersion { get; set; } = CurrentProtocolVersion;

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("sequence_number")]
        public long SequenceNumber { get; set; }

        [JsonPropertyName("timestamp_utc_ms")]
        public long TimestampUtcMs { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }

        public static ProtocolEnvelope Create(string type, object? payload, long sequenceNumber)
        {
            var envelope = new ProtocolEnvelope
            {
                ProtocolVersion = CurrentProtocolVersion,
                MessageId = Guid.NewGuid().ToString("D"),
                SequenceNumber = sequenceNumber,
                TimestampUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Type = type
            };

            if (payload != null)
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
                using var doc = JsonDocument.Parse(bytes);
                envelope.Payload = doc.RootElement.Clone();
            }

            return envelope;
        }

        public static ValidationResult Validate(string rawJson, long expectedNextSequence)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return ValidationResult.Fail("Message cannot be null or empty.");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(rawJson);
            return Validate(bytes, expectedNextSequence);
        }

        public static ValidationResult Validate(ReadOnlySpan<byte> utf8Json, long expectedNextSequence)
        {
            if (utf8Json.IsEmpty)
            {
                return ValidationResult.Fail("Message cannot be null or empty.");
            }

            if (utf8Json.Length > MaxMessageSizeBytes)
            {
                return ValidationResult.Fail($"Message exceeds maximum allowed size of {MaxMessageSizeBytes} bytes.");
            }

            ProtocolEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ProtocolEnvelope>(utf8Json);
            }
            catch (Exception ex)
            {
                return ValidationResult.Fail($"Malformed JSON envelope: {ex.Message}");
            }

            if (envelope == null)
            {
                return ValidationResult.Fail("Parsed envelope is null.");
            }

            if (envelope.ProtocolVersion != CurrentProtocolVersion)
            {
                return ValidationResult.Fail($"Incompatible protocol version '{envelope.ProtocolVersion}'. Expected '{CurrentProtocolVersion}'.");
            }

            if (string.IsNullOrWhiteSpace(envelope.MessageId))
            {
                return ValidationResult.Fail("Message ID cannot be null or blank.");
            }

            if (envelope.SequenceNumber < 0)
            {
                return ValidationResult.Fail("Sequence number cannot be negative.");
            }

            if (envelope.SequenceNumber < expectedNextSequence)
            {
                return ValidationResult.Fail($"Replay or out-of-order sequence: received {envelope.SequenceNumber}, expected at least {expectedNextSequence}.");
            }

            if (envelope.TimestampUtcMs <= 0)
            {
                return ValidationResult.Fail("Timestamp must be a positive integer.");
            }

            if (string.IsNullOrWhiteSpace(envelope.Type))
            {
                return ValidationResult.Fail("Message type cannot be blank.");
            }

            return ValidationResult.Success(envelope);
        }
    }

    public class SequenceTracker
    {
        private long _lastSequence = -1;
        private readonly object _lock = new();

        public long NextExpectedSequence
        {
            get
            {
                lock (_lock)
                {
                    return _lastSequence < 0 ? 0 : _lastSequence + 1;
                }
            }
        }

        public bool TryAccept(long sequenceNumber, out string? error)
        {
            lock (_lock)
            {
                if (sequenceNumber < 0)
                {
                    error = "Sequence number cannot be negative.";
                    return false;
                }
                if (_lastSequence >= 0 && sequenceNumber <= _lastSequence)
                {
                    error = $"Replay or duplicate sequence: received {sequenceNumber}, must be greater than {_lastSequence}.";
                    return false;
                }
                _lastSequence = sequenceNumber;
                error = null;
                return true;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _lastSequence = -1;
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string? Error { get; }
        public ProtocolEnvelope? Envelope { get; }

        private ValidationResult(bool isValid, string? error, ProtocolEnvelope? envelope)
        {
            IsValid = isValid;
            Error = error;
            Envelope = envelope;
        }

        public static ValidationResult Success(ProtocolEnvelope envelope) => new(true, null, envelope);
        public static ValidationResult Fail(string error) => new(false, error, null);
    }
}
