package com.projectname.android

import com.projectname.android.capture.BoundedFrameBuffer
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class BoundedFrameBufferTest {

    @Test
    fun buffer_enforcesMaxCapacityAndDropsOldest() {
        val buffer = BoundedFrameBuffer(maxCapacity = 2)

        val frame1 = byteArrayOf(1, 2)
        val frame2 = byteArrayOf(3, 4)
        val frame3 = byteArrayOf(5, 6)

        buffer.pushFrame(frame1, 1280, 720, 100L)
        buffer.pushFrame(frame2, 1280, 720, 133L)
        assertEquals(2, buffer.queuedFramesCount)
        assertEquals(0L, buffer.totalDroppedFrames)

        // Third frame exceeds capacity of 2; frame1 must be dropped
        buffer.pushFrame(frame3, 1280, 720, 166L)
        assertEquals(2, buffer.queuedFramesCount)
        assertEquals(1L, buffer.totalDroppedFrames)

        val next1 = buffer.tryGetNextFrame()
        assertNotNull(next1)
        assertEquals(frame2, next1!!.data)

        val next2 = buffer.tryGetNextFrame()
        assertNotNull(next2)
        assertEquals(frame3, next2!!.data)

        assertNull(buffer.tryGetNextFrame())
        assertEquals(0, buffer.queuedFramesCount)
    }

    @Test
    fun buffer_clearDiscardsAllFramesImmediately() {
        val buffer = BoundedFrameBuffer(maxCapacity = 5)
        buffer.pushFrame(byteArrayOf(1), 100, 100, 1L)
        buffer.pushFrame(byteArrayOf(2), 100, 100, 2L)
        assertEquals(2, buffer.queuedFramesCount)

        buffer.clear()
        assertEquals(0, buffer.queuedFramesCount)
        assertNull(buffer.tryGetNextFrame())
    }
}
