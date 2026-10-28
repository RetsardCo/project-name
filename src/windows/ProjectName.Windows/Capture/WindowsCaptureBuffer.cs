using System;
using System.Collections.Concurrent;

namespace ProjectName.Windows.Capture
{
    public record VideoFrame(
        byte[] Data,
        int Width,
        int Height,
        long TimestampMs
    );

    /// <summary>
    /// Thread-safe in-memory bounded ring buffer for live screen video streaming.
    /// Strictly adheres to Non-Negotiable Requirement 3.1: Zero screen recording, no disk writes,
    /// bounded buffer capacity with immediate stale frame drop on backpressure.
    /// </summary>
    public class WindowsCaptureBuffer
    {
        private readonly ConcurrentQueue<VideoFrame> _frameQueue = new();
        private readonly int _maxCapacity;
        private long _droppedFramesCount;
        private readonly object _dropLock = new();

        public int MaxCapacity => _maxCapacity;
        public int QueuedFramesCount => _frameQueue.Count;
        public long DroppedFramesCount => Interlocked.Read(ref _droppedFramesCount);

        public WindowsCaptureBuffer(int maxCapacity = 2)
        {
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Capacity must be positive.");
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Pushes a new frame into the in-memory buffer. Drops oldest frames if capacity is exceeded.
        /// Under no circumstances is frame data ever written to non-volatile disk storage.
        /// </summary>
        public void PushFrame(byte[] frameData, int width, int height, long timestampMs)
        {
            if (frameData == null || frameData.Length == 0) return;

            // Discard stale frames when buffer reaches max capacity
            while (_frameQueue.Count >= _maxCapacity)
            {
                if (_frameQueue.TryDequeue(out _))
                {
                    Interlocked.Increment(ref _droppedFramesCount);
                }
            }

            _frameQueue.Enqueue(new VideoFrame(frameData, width, height, timestampMs));
        }

        /// <summary>
        /// Attempts to dequeue the next frame for transmission.
        /// </summary>
        public bool TryGetNextFrame(out VideoFrame? frame)
        {
            return _frameQueue.TryDequeue(out frame);
        }

        /// <summary>
        /// Immediately clears all queued frames when a session ends or disconnects.
        /// </summary>
        public void Clear()
        {
            while (_frameQueue.TryDequeue(out _)) { }
        }
    }
}
