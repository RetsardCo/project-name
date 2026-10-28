package com.projectname.android.capture

import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.atomic.AtomicLong

/**
 * Represents an ephemeral uncompressed video frame held exclusively in volatile RAM.
 * Under Non-Negotiable Requirement 3.1: Zero screen recording, no disk writes, no caching to non-volatile storage.
 */
data class VideoFrame(
    val data: ByteArray,
    val width: Int,
    val height: Int,
    val timestampMs: Long
)

/**
 * Thread-safe bounded in-memory ring buffer for live Android screen video streaming.
 * Capacity is strictly bounded (default: 2 frames); when network or encoder backpressure occurs,
 * stale frames are dropped immediately to preserve the < 100 ms interaction latency target.
 */
class BoundedFrameBuffer(val maxCapacity: Int = 2) {

    init {
        require(maxCapacity > 0) { "Buffer capacity must be strictly positive." }
    }

    private val frameQueue = ConcurrentLinkedQueue<VideoFrame>()
    private val droppedFramesCount = AtomicLong(0)

    val queuedFramesCount: Int
        get() = frameQueue.size

    val totalDroppedFrames: Long
        get() = droppedFramesCount.get()

    /**
     * Pushes a new frame into the in-memory queue.
     * Drops oldest frame if capacity limit is reached.
     */
    fun pushFrame(data: ByteArray, width: Int, height: Int, timestampMs: Long) {
        if (data.isEmpty()) return

        while (frameQueue.size >= maxCapacity) {
            if (frameQueue.poll() != null) {
                droppedFramesCount.incrementAndGet()
            }
        }

        frameQueue.offer(VideoFrame(data, width, height, timestampMs))
    }

    /**
     * Retrieves and dequeues the next frame for transmission.
     */
    fun tryGetNextFrame(): VideoFrame? {
        return frameQueue.poll()
    }

    /**
     * Immediately clears all frame buffers when a session stops or disconnects.
     */
    fun clear() {
        frameQueue.clear()
    }
}
