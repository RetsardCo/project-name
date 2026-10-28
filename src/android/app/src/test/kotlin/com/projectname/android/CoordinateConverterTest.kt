package com.projectname.android

import com.projectname.android.input.CoordinateConverter
import org.junit.Assert.assertEquals
import org.junit.Test

class CoordinateConverterTest {

    @Test
    fun clampNormalized_clampsValuesCorrectly() {
        assertEquals(0.0f, CoordinateConverter.clampNormalized(-0.5f), 0.0001f)
        assertEquals(1.0f, CoordinateConverter.clampNormalized(1.5f), 0.0001f)
        assertEquals(0.5f, CoordinateConverter.clampNormalized(0.5f), 0.0001f)
        assertEquals(0.0f, CoordinateConverter.clampNormalized(Float.NaN), 0.0001f)
        assertEquals(0.0f, CoordinateConverter.clampNormalized(Float.POSITIVE_INFINITY), 0.0001f)
    }

    @Test
    fun toScreenPixels_scalesToDisplayDimensions() {
        val width = 1080
        val height = 2400

        val (originX, originY) = CoordinateConverter.toScreenPixels(0.0f, 0.0f, width, height)
        assertEquals(0, originX)
        assertEquals(0, originY)

        val (maxX, maxY) = CoordinateConverter.toScreenPixels(1.0f, 1.0f, width, height)
        assertEquals(1079, maxX)
        assertEquals(2399, maxY)

        val (midX, midY) = CoordinateConverter.toScreenPixels(0.5f, 0.5f, width, height)
        assertEquals(540, midX)
        assertEquals(1200, midY)
    }

    @Test
    fun toNormalized_scalesPixelCoordinatesCorrectly() {
        val width = 1080
        val height = 2400

        val (normX0, normY0) = CoordinateConverter.toNormalized(0f, 0f, width, height)
        assertEquals(0.0f, normX0, 0.0001f)
        assertEquals(0.0f, normY0, 0.0001f)

        val (normXMax, normYMax) = CoordinateConverter.toNormalized(1080f, 2400f, width, height)
        assertEquals(1.0f, normXMax, 0.0001f)
        assertEquals(1.0f, normYMax, 0.0001f)
    }
}
