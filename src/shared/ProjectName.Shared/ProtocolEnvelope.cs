using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectName.Shared.Protocol
{
    public class ProtocolEnvelope
    {
        public const string CurrentProtocolVersion = "1.0";
        public const int MaxMessageSizeBytes = 65536; // 64 KB

        [JsonPropertyName("protocol_version")]
        public string ProtocolVersion { get; set; } = CurrentProtocolVersion;

        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString("D");

        [JsonPropertyName("sequence_number")]
        public long SequenceNumber { get; set; }

        [JsonPropertyName("timestamp_utc_ms")]
        public long TimestampUtcMs { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public JsonElement Payload { get; set; }

        public static ValidationResult Validate(string rawJson, long expectedNextSequence)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return ValidationResult.Fail("Message cannot be null or empty.");
            }

            if (System.Text.Encoding.UTF8.GetByteCount(rawJson) > MaxMessageSizeBytes)
            {
                return ValidationResult.Fail($"Message exceeds maximum allowed size of {MaxMessageSizeBytes} bytes.");
            }

            ProtocolEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ProtocolEnvelope>(rawJson);
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

            if (envelope.SequenceNumber < expectedNextSequence)
            {
                return ValidationResult.Fail($"Replay or out-of-order sequence: received {envelope.SequenceNumber}, expected at least {expectedNextSequence}.");
            }

            if (string.IsNullOrWhiteSpace(envelope.Type))
            {
                return ValidationResult.Fail("Message type cannot be blank.");
            }

            return ValidationResult.Success(envelope);
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
