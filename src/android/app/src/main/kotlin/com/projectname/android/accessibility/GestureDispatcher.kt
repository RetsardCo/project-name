package com.projectname.android.accessibility

import android.accessibilityservice.GestureDescription
import android.graphics.Path
import com.projectname.android.input.CoordinateConverter

/**
 * Builds programmatic GestureDescription strokes for Android AccessibilityService.dispatchGesture().
 * Converts normalized protocol coordinates into physical pixel Paths.
 */
object GestureDispatcher {

    /**
     * Builds a tap gesture at the specified normalized coordinates.
     */
    fun buildTapGesture(
        xNorm: Float,
        yNorm: Float,
        displayWidth: Int,
        displayHeight: Int,
        durationMs: Long = 50L
    ): GestureDescription {
        val (px, py) = CoordinateConverter.toScreenPixels(xNorm, yNorm, displayWidth, displayHeight)

        val path = Path().apply {
            moveTo(px.toFloat(), py.toFloat())
            lineTo(px.toFloat(), py.toFloat())
        }

        val stroke = GestureDescription.StrokeDescription(path, 0L, durationMs.coerceAtLeast(10L))
        return GestureDescription.Builder()
            .addStroke(stroke)
            .build()
    }

    /**
     * Builds a swipe/scroll gesture from start coordinates to end coordinates.
     */
    fun buildSwipeGesture(
        startXNorm: Float,
        startYNorm: Float,
        endXNorm: Float,
        endYNorm: Float,
        displayWidth: Int,
        displayHeight: Int,
        durationMs: Long = 300L
    ): GestureDescription {
        val (startX, startY) = CoordinateConverter.toScreenPixels(startXNorm, startYNorm, displayWidth, displayHeight)
        val (endX, endY) = CoordinateConverter.toScreenPixels(endXNorm, endYNorm, displayWidth, displayHeight)

        val path = Path().apply {
            moveTo(startX.toFloat(), startY.toFloat())
            lineTo(endX.toFloat(), endY.toFloat())
        }

        val stroke = GestureDescription.StrokeDescription(path, 0L, durationMs.coerceAtLeast(50L))
        return GestureDescription.Builder()
            .addStroke(stroke)
            .build()
    }
}
