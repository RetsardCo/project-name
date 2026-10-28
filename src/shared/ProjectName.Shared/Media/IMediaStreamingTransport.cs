using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectName.Shared.Media
{
    /// <summary>
    /// Contract for real-time video streaming transport (e.g., WebRTC / ephemeral media channel).
    /// Under Non-Negotiable Requirement 3.1: Zero screen recording, no disk writes, only in-memory streaming.
    /// </summary>
    public interface IMediaStreamingTransport : IAsyncDisposable
    {
        bool IsStreaming { get; }
        event EventHandler<MediaFrameEventArgs>? FrameReceived;

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync();
        Task SendVideoFrameAsync(byte[] frameData, int width, int height, long timestampMs, CancellationToken cancellationToken = default);
    }

    public class MediaFrameEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public int Width { get; }
        public int Height { get; }
        public long TimestampMs { get; }

        public MediaFrameEventArgs(byte[] data, int width, int height, long timestampMs)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Width = width;
            Height = height;
            TimestampMs = timestampMs;
        }
    }
}
