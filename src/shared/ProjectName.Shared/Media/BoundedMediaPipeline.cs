using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol.Payloads;

namespace ProjectName.Shared.Media
{
    /// <summary>
    /// Connects the in-memory capture buffer to the real-time media transport.
    /// Strictly enforces Non-Negotiable Requirement 3.1: Zero screen recording, no disk writes,
    /// bounded buffer capacity with immediate stale frame drop to preserve < 100 ms interaction latency.
    /// </summary>
    public class BoundedMediaPipeline : IAsyncDisposable
    {
        private readonly IMediaStreamingTransport _transport;
        private readonly Func<bool, (byte[]? Data, int Width, int Height, long TimestampMs)> _frameProvider;
        private readonly int _targetFps;
        private readonly object _stateLock = new();

        private CancellationTokenSource? _pipelineCts;
        private Task? _streamingLoopTask;
        private long _transmittedFramesCount;
        private long _droppedFramesCount;
        private long _totalBytesTransmitted;
        private long _lastReportTimestampMs;
        private long _framesSinceLastReport;
        private double _currentFps;
        private double _currentBitrateKbps;

        public bool IsRunning { get; private set; }
        public long TransmittedFramesCount => Interlocked.Read(ref _transmittedFramesCount);
        public long DroppedFramesCount => Interlocked.Read(ref _droppedFramesCount);
        public double CurrentFps => _currentFps;
        public double CurrentBitrateKbps => _currentBitrateKbps;

        public BoundedMediaPipeline(
            IMediaStreamingTransport transport,
            Func<bool, (byte[]? Data, int Width, int Height, long TimestampMs)> frameProvider,
            int targetFps = 30)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
            _targetFps = targetFps > 0 ? targetFps : 30;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                if (IsRunning) return Task.CompletedTask;

                IsRunning = true;
                _pipelineCts = new CancellationTokenSource();
                _lastReportTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            _streamingLoopTask = Task.Run(() => StreamingLoopAsync(_pipelineCts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        private async Task StreamingLoopAsync(CancellationToken cancellationToken)
        {
            int intervalMs = Math.Max(1, 1000 / _targetFps);
            var stopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                long startMs = stopwatch.ElapsedMilliseconds;

                // Dequeue next ephemeral frame from in-memory ring buffer
                var (frameData, width, height, timestampMs) = _frameProvider(false);

                if (frameData != null && frameData.Length > 0)
                {
                    // Strict Invariant Assertion: Under no circumstances is frameData written to disk
                    // Only transmitted directly over in-memory transport
                    try
                    {
                        await _transport.SendVideoFrameAsync(frameData, width, height, timestampMs, cancellationToken).ConfigureAwait(false);

                        Interlocked.Increment(ref _transmittedFramesCount);
                        Interlocked.Add(ref _totalBytesTransmitted, frameData.Length);
                        _framesSinceLastReport++;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Count failed transmission as a dropped frame under backpressure
                        Interlocked.Increment(ref _droppedFramesCount);
                    }
                }

                // Update real-time FPS and bitrate telemetry
                UpdateTelemetry();

                long elapsedMs = stopwatch.ElapsedMilliseconds - startMs;
                int delayMs = (int)(intervalMs - elapsedMs);
                if (delayMs > 0)
                {
                    try
                    {
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private void UpdateTelemetry()
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long deltaMs = nowMs - _lastReportTimestampMs;

            if (deltaMs >= 1000)
            {
                _currentFps = (_framesSinceLastReport * 1000.0) / deltaMs;
                long bytes = Interlocked.Exchange(ref _totalBytesTransmitted, 0);
                _currentBitrateKbps = (bytes * 8.0) / (deltaMs); // kilobits per sec

                _framesSinceLastReport = 0;
                _lastReportTimestampMs = nowMs;
            }
        }

        /// <summary>
        /// Generates a sanitized diagnostics report payload adhering strictly to Section 4:
        /// Diagnostics must never contain pixel data, image hashes, or keystroke information.
        /// </summary>
        public DiagnosticsReportPayload GenerateDiagnosticsReport(double rttMs = 15.0, double jitterMs = 2.5, string codec = "H264")
        {
            return new DiagnosticsReportPayload(
                RttMs: rttMs,
                JitterMs: jitterMs,
                BitrateKbps: _currentBitrateKbps,
                DroppedFrames: DroppedFramesCount,
                Fps: _currentFps,
                Codec: codec
            );
        }

        public async Task StopAsync()
        {
            lock (_stateLock)
            {
                if (!IsRunning) return;
                IsRunning = false;
                _pipelineCts?.Cancel();
            }

            if (_streamingLoopTask != null)
            {
                try { await _streamingLoopTask.ConfigureAwait(false); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _pipelineCts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
