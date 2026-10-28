using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Transport;

namespace ProjectName.Shared.Media
{
    public record WebRtcSignalingPayload(
        [property: JsonPropertyName("session_id")] string SessionId,
        [property: JsonPropertyName("sdp")] string? Sdp,
        [property: JsonPropertyName("candidate")] string? Candidate,
        [property: JsonPropertyName("sdp_mid")] string? SdpMid,
        [property: JsonPropertyName("sdp_mline_index")] int? SdpMLineIndex
    );

    /// <summary>
    /// Manages WebRTC SDP Offer, SDP Answer, and ICE Candidate signaling over the TLS control channel.
    /// Enables peer-to-peer media pipeline setup while strictly maintaining zero persistent storage.
    /// </summary>
    public class WebRtcSignalingCoordinator
    {
        public const string TypeOffer = "WEBRTC_OFFER";
        public const string TypeAnswer = "WEBRTC_ANSWER";
        public const string TypeIceCandidate = "WEBRTC_ICE_CANDIDATE";

        public event EventHandler<WebRtcSignalingPayload>? OfferReceived;
        public event EventHandler<WebRtcSignalingPayload>? AnswerReceived;
        public event EventHandler<WebRtcSignalingPayload>? IceCandidateReceived;

        public void ProcessSignalingEnvelope(ProtocolEnvelope envelope)
        {
            if (envelope == null) return;

            var payload = envelope.Payload.Deserialize<WebRtcSignalingPayload>();
            if (payload == null) return;

            switch (envelope.Type)
            {
                case TypeOffer:
                    OfferReceived?.Invoke(this, payload);
                    break;
                case TypeAnswer:
                    AnswerReceived?.Invoke(this, payload);
                    break;
                case TypeIceCandidate:
                    IceCandidateReceived?.Invoke(this, payload);
                    break;
            }
        }

        public static ProtocolEnvelope CreateOfferEnvelope(string sessionId, string sdp, long sequenceNumber)
        {
            var payload = new WebRtcSignalingPayload(sessionId, sdp, null, null, null);
            return ProtocolEnvelope.Create(TypeOffer, payload, sequenceNumber);
        }

        public static ProtocolEnvelope CreateAnswerEnvelope(string sessionId, string sdp, long sequenceNumber)
        {
            var payload = new WebRtcSignalingPayload(sessionId, sdp, null, null, null);
            return ProtocolEnvelope.Create(TypeAnswer, payload, sequenceNumber);
        }

        public static ProtocolEnvelope CreateIceCandidateEnvelope(string sessionId, string candidate, string sdpMid, int sdpMLineIndex, long sequenceNumber)
        {
            var payload = new WebRtcSignalingPayload(sessionId, null, candidate, sdpMid, sdpMLineIndex);
            return ProtocolEnvelope.Create(TypeIceCandidate, payload, sequenceNumber);
        }
    }
}
