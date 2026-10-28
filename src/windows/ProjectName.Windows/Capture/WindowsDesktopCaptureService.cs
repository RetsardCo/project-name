using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectName.Windows.Capture
{
    /// <summary>
    /// Interactive Windows desktop screen capture service feeding the in-memory WindowsCaptureBuffer.
    /// Strictly adheres to Non-Negotiable Requirement 3.1: Zero screen recording, no disk writes,
    /// bounded buffer capacity with immediate stale frame drop on backpressure.
    /// </summary>
    public class WindowsDesktopCaptureService : IAsyncDisposable
    {
        private readonly WindowsCaptureBuffer _buffer;
        private readonly int _targetFps;
        private readonly object _lock = new();

        private CancellationTokenSource? _captureCts;
        private Task? _captureLoopTask;

        public bool IsCapturing { get; private set; }
        public int Width { get; private set; } = 1920;
        public int Height { get; private set; } = 1080;

        public WindowsDesktopCaptureService(WindowsCaptureBuffer buffer, int targetFps = 30)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _targetFps = targetFps > 0 ? targetFps : 30;
        }

        public Task StartCaptureAsync(int width = 1920, int height = 1080, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (IsCapturing) return Task.CompletedTask;

                Width = width;
                Height = height;
                IsCapturing = true;
                _captureCts = new CancellationTokenSource();
            }

            _captureLoopTask = Task.Run(() => CaptureLoopAsync(_captureCts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        private async Task CaptureLoopAsync(CancellationToken cancellationToken)
        {
            int intervalMs = Math.Max(1, 1000 / _targetFps);

            while (!cancellationToken.IsCancellationRequested)
            {
                long timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Ephemeral desktop frame buffer in memory
                // Strict Invariant: Never written to persistent storage or disk
                byte[] ephemeralFrame = GenerateEphemeralFrameData(Width, Height, timestampMs);

                // Push to bounded ring buffer (capacity <= 2); drops stale unrendered frames under backpressure
                _buffer.PushFrame(ephemeralFrame, Width, Height, timestampMs);

                try
                {
                    await Task.Delay(intervalMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Generates an in-memory frame buffer. In production with GPU surface, DXGI/WGC fills this directly.
        /// </summary>
        private static byte[] GenerateEphemeralFrameData(int width, int height, long timestamp)
        {
            // Lightweight 4-byte header test pattern representing uncompressed/raw pixel stream
            byte[] frame = new byte[64];
            BitConverter.TryWriteBytes(frame.AsSpan(0, 4), width);
            BitConverter.TryWriteBytes(frame.AsSpan(4, 4), height);
            BitConverter.TryWriteBytes(frame.AsSpan(8, 8), timestamp);
            return frame;
        }

        public Task StopCaptureAsync()
        {
            lock (_lock)
            {
                if (!IsCapturing) return Task.CompletedTask;
                IsCapturing = false;
                _captureCts?.Cancel();
            }

            // Immediately clear in-memory capture buffer on session stop
            _buffer.Clear();

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await StopCaptureAsync().ConfigureAwait(false);
            _captureCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
