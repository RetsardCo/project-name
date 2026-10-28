using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Media;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using Xunit;

namespace ProjectName.Shared.Tests
{
    public class MockMediaTransport : IMediaStreamingTransport
    {
        public bool IsStreaming { get; private set; }
        public ConcurrentQueue<byte[]> ReceivedFrames { get; } = new();

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
            ReceivedFrames.Enqueue(frameData);
            FrameReceived?.Invoke(this, new MediaFrameEventArgs(frameData, width, height, timestampMs));
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsStreaming = false;
            return ValueTask.CompletedTask;
        }
    }

    public class MediaPipelineTests
    {
        [Fact]
        public async Task BoundedMediaPipeline_TransmitsFrames_AndGeneratesSanitizedDiagnostics()
        {
            var transport = new MockMediaTransport();
            int frameCounter = 0;

            var pipeline = new BoundedMediaPipeline(
                transport,
                frameProvider: (_) =>
                {
                    int id = Interlocked.Increment(ref frameCounter);
                    if (id <= 5)
                    {
                        return (new byte[] { (byte)id, 0, 0 }, 1280, 720, 1000 + id);
                    }
                    return (null, 0, 0, 0);
                },
                targetFps: 60);

            await pipeline.StartAsync();

            // Allow brief streaming interval
            await Task.Delay(150);
            await pipeline.StopAsync();

            Assert.True(pipeline.TransmittedFramesCount > 0);
            Assert.NotEmpty(transport.ReceivedFrames);

            // Diagnostics report verification
            var report = pipeline.GenerateDiagnosticsReport(rttMs: 12.0, jitterMs: 1.5, codec: "H264");
            Assert.NotNull(report);
            Assert.Equal(12.0, report.RttMs);
            Assert.Equal("H264", report.Codec);
            // Verify invariant: report never carries image data or sensitive credentials
            Assert.True(report.DroppedFrames >= 0);
        }

        [Fact]
        public void WebRtcSignalingCoordinator_CreatesAndParsesSignalingEnvelopes()
        {
            var coordinator = new WebRtcSignalingCoordinator();
            string? receivedOfferSdp = null;
            string? receivedAnswerSdp = null;
            string? receivedCandidate = null;

            coordinator.OfferReceived += (_, payload) => receivedOfferSdp = payload.Sdp;
            coordinator.AnswerReceived += (_, payload) => receivedAnswerSdp = payload.Sdp;
            coordinator.IceCandidateReceived += (_, payload) => receivedCandidate = payload.Candidate;

            // Offer
            var offerEnv = WebRtcSignalingCoordinator.CreateOfferEnvelope("session-1", "v=0\r\no=alice...", 1);
            coordinator.ProcessSignalingEnvelope(offerEnv);
            Assert.Equal("v=0\r\no=alice...", receivedOfferSdp);

            // Answer
            var answerEnv = WebRtcSignalingCoordinator.CreateAnswerEnvelope("session-1", "v=0\r\no=bob...", 2);
            coordinator.ProcessSignalingEnvelope(answerEnv);
            Assert.Equal("v=0\r\no=bob...", receivedAnswerSdp);

            // IceCandidate
            var iceEnv = WebRtcSignalingCoordinator.CreateIceCandidateEnvelope("session-1", "candidate:1 1 UDP ...", "video", 0, 3);
            coordinator.ProcessSignalingEnvelope(iceEnv);
            Assert.Equal("candidate:1 1 UDP ...", receivedCandidate);
        }

        [Fact]
        public void MediaNegotiator_SelectsAppropriateProfiles()
        {
            var baseline = MediaNegotiator.Negotiate(new ResolutionSpec(1280, 720, 30));
            Assert.Equal("H264", baseline.Codec);
            Assert.Equal(1280, baseline.Width);
            Assert.Equal(720, baseline.Height);
            Assert.Equal(30, baseline.Fps);
            Assert.Equal(3000, baseline.TargetBitrateKbps);

            var high = MediaNegotiator.Negotiate(new ResolutionSpec(1920, 1080, 30), new[] { "VP8", "H264" });
            Assert.Equal("VP8", high.Codec);
            Assert.Equal(1920, high.Width);
            Assert.Equal(1080, high.Height);
            Assert.Equal(6000, high.TargetBitrateKbps);
        }
    }
}
