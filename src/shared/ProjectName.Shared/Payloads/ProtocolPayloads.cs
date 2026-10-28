using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectName.Shared.Protocol.Payloads
{
    // Discovery
    public record DiscoveryPayload(
        [property: JsonPropertyName("device_id")] string DeviceId,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("platform")] string Platform,
        [property: JsonPropertyName("protocol_version")] string ProtocolVersion,
        [property: JsonPropertyName("control_port")] int ControlPort,
        [property: JsonPropertyName("capabilities")] List<string> Capabilities
    );

    // Pairing
    public record PairRequestPayload(
        [property: JsonPropertyName("controller_id")] string ControllerId,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("public_key")] string PublicKey
    );

    public record PairChallengePayload(
        [property: JsonPropertyName("endpoint_id")] string EndpointId,
        [property: JsonPropertyName("pin_code")] string PinCode,
        [property: JsonPropertyName("expires_in_seconds")] int ExpiresInSeconds,
        [property: JsonPropertyName("challenge_nonce")] string ChallengeNonce
    );

    public record PairConfirmPayload(
        [property: JsonPropertyName("controller_id")] string ControllerId,
        [property: JsonPropertyName("pin_code")] string PinCode,
        [property: JsonPropertyName("signed_nonce")] string SignedNonce
    );

    public record PairResultPayload(
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("auth_token")] string? AuthToken,
        [property: JsonPropertyName("error_code")] string? ErrorCode
    );

    // Session Management
    public record ResolutionSpec(
        [property: JsonPropertyName("width")] int Width,
        [property: JsonPropertyName("height")] int Height,
        [property: JsonPropertyName("fps")] int Fps
    );

    public record SessionStartPayload(
        [property: JsonPropertyName("session_id")] string SessionId,
        [property: JsonPropertyName("requested_scope")] string RequestedScope,
        [property: JsonPropertyName("requested_resolution")] ResolutionSpec RequestedResolution
    );

    public record SessionAcceptPayload(
        [property: JsonPropertyName("session_id")] string SessionId,
        [property: JsonPropertyName("granted_scope")] string GrantedScope,
        [property: JsonPropertyName("webrtc_offer")] string? WebRtcOffer
    );

    public record SessionStopPayload(
        [property: JsonPropertyName("session_id")] string SessionId,
        [property: JsonPropertyName("reason")] string Reason
    );

    // Input
    public record PointerMovePayload(
        [property: JsonPropertyName("x_norm")] double XNorm,
        [property: JsonPropertyName("y_norm")] double YNorm
    );

    public record PointerButtonPayload(
        [property: JsonPropertyName("button")] string Button,
        [property: JsonPropertyName("action")] string Action
    );

    public record KeyPayload(
        [property: JsonPropertyName("key_code")] string KeyCode,
        [property: JsonPropertyName("action")] string Action,
        [property: JsonPropertyName("modifiers")] List<string>? Modifiers
    );

    public record TouchGesturePayload(
        [property: JsonPropertyName("gesture")] string Gesture,
        [property: JsonPropertyName("x_norm")] double XNorm,
        [property: JsonPropertyName("y_norm")] double YNorm,
        [property: JsonPropertyName("end_x_norm")] double? EndXNorm,
        [property: JsonPropertyName("end_y_norm")] double? EndYNorm,
        [property: JsonPropertyName("duration_ms")] int DurationMs
    );

    public record TvCommandPayload(
        [property: JsonPropertyName("command")] string Command
    );

    // Diagnostics
    public record DiagnosticsReportPayload(
        [property: JsonPropertyName("rtt_ms")] double RttMs,
        [property: JsonPropertyName("jitter_ms")] double JitterMs,
        [property: JsonPropertyName("bitrate_kbps")] double BitrateKbps,
        [property: JsonPropertyName("dropped_frames")] long DroppedFrames,
        [property: JsonPropertyName("fps")] double Fps,
        [property: JsonPropertyName("codec")] string Codec
    );
}
