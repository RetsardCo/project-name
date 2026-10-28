package com.projectname.android.controller

import com.projectname.android.input.CoordinateConverter

enum class MouseAction {
    MOVE,
    BUTTON_DOWN,
    BUTTON_UP,
    BUTTON_CLICK
}

data class MappedPointerEvent(
    val action: MouseAction,
    val xNorm: Float,
    val yNorm: Float,
    val button: String = "left"
)

/**
 * Translates Android touch events into normalized Windows mouse pointer and button events.
 */
class TouchToMouseMapper(
    private var viewWidth: Int = 1,
    private var viewHeight: Int = 1
) {

    fun updateDimensions(width: Int, height: Int) {
        viewWidth = maxOf(1, width)
        viewHeight = maxOf(1, height)
    }

    /**
     * Maps a touch down event to pointer move + left button down.
     */
    fun onTouchDown(touchX: Float, touchY: Float): List<MappedPointerEvent> {
        val (xNorm, yNorm) = CoordinateConverter.toNormalized(touchX, touchY, viewWidth, viewHeight)
        return listOf(
            MappedPointerEvent(MouseAction.MOVE, xNorm, yNorm),
            MappedPointerEvent(MouseAction.BUTTON_DOWN, xNorm, yNorm, "left")
        )
    }

    /**
     * Maps a touch move / drag event to pointer move.
     */
    fun onTouchMove(touchX: Float, touchY: Float): MappedPointerEvent {
        val (xNorm, yNorm) = CoordinateConverter.toNormalized(touchX, touchY, viewWidth, viewHeight)
        return MappedPointerEvent(MouseAction.MOVE, xNorm, yNorm)
    }

    /**
     * Maps a touch up event to pointer left button up.
     */
    fun onTouchUp(touchX: Float, touchY: Float): MappedPointerEvent {
        val (xNorm, yNorm) = CoordinateConverter.toNormalized(touchX, touchY, viewWidth, viewHeight)
        return MappedPointerEvent(MouseAction.BUTTON_UP, xNorm, yNorm, "left")
    }

    /**
     * Maps an explicit right-click event.
     */
    fun onRightClick(touchX: Float, touchY: Float): List<MappedPointerEvent> {
        val (xNorm, yNorm) = CoordinateConverter.toNormalized(touchX, touchY, viewWidth, viewHeight)
        return listOf(
            MappedPointerEvent(MouseAction.MOVE, xNorm, yNorm),
            MappedPointerEvent(MouseAction.BUTTON_CLICK, xNorm, yNorm, "right")
        )
    }
}
