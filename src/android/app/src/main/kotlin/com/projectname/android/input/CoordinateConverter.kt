package com.projectname.android.input

import kotlin.math.max
import kotlin.math.min
import kotlin.math.roundToInt

/**
 * Coordinate mapping and conversion utilities for normalized protocol coordinates [0.0, 1.0].
 * Safely scales normalized coordinates to device screen pixel boundaries.
 */
object CoordinateConverter {

    /**
     * Clamps a normalized float to the valid range [0.0, 1.0].
     * Protects against out-of-bounds inputs, NaN, and infinities.
     */
    fun clampNormalized(value: Float): Float {
        if (value.isNaN() || value.isInfinite()) return 0.0f
        return max(0.0f, min(1.0f, value))
    }

    /**
     * Converts normalized coordinates to absolute screen pixel coordinates.
     * @param xNorm Normalized X coordinate in [0.0, 1.0].
     * @param yNorm Normalized Y coordinate in [0.0, 1.0].
     * @param displayWidth Device display width in pixels.
     * @param displayHeight Device display height in pixels.
     * @return Pair of integer screen pixel coordinates (x, y).
     */
    fun toScreenPixels(xNorm: Float, yNorm: Float, displayWidth: Int, displayHeight: Int): Pair<Int, Int> {
        val clampedX = clampNormalized(xNorm)
        val clampedY = clampNormalized(yNorm)

        val targetWidth = max(1, displayWidth)
        val targetHeight = max(1, displayHeight)

        val px = (clampedX * (targetWidth - 1)).roundToInt()
        val py = (clampedY * (targetHeight - 1)).roundToInt()

        return Pair(px, py)
    }

    /**
     * Converts absolute screen pixel coordinates to normalized coordinates [0.0, 1.0].
     */
    fun toNormalized(pixelX: Float, pixelY: Float, displayWidth: Int, displayHeight: Int): Pair<Float, Float> {
        val targetWidth = max(1, displayWidth).toFloat()
        val targetHeight = max(1, displayHeight).toFloat()

        val normX = clampNormalized(pixelX / targetWidth)
        val normY = clampNormalized(pixelY / targetHeight)

        return Pair(normX, normY)
    }
}
