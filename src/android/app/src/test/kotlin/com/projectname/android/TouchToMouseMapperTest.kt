package com.projectname.android

import com.projectname.android.controller.MouseAction
import com.projectname.android.controller.TouchToMouseMapper
import org.junit.Assert.assertEquals
import org.junit.Test

class TouchToMouseMapperTest {

    @Test
    fun onTouchDown_emitsMoveAndButtonDown() {
        val mapper = TouchToMouseMapper(1000, 2000)
        val events = mapper.onTouchDown(500f, 1000f)

        assertEquals(2, events.size)
        assertEquals(MouseAction.MOVE, events[0].action)
        assertEquals(0.5f, events[0].xNorm, 0.001f)
        assertEquals(0.5f, events[0].yNorm, 0.001f)

        assertEquals(MouseAction.BUTTON_DOWN, events[1].action)
        assertEquals("left", events[1].button)
    }

    @Test
    fun onTouchMove_emitsMove() {
        val mapper = TouchToMouseMapper(1000, 2000)
        val event = mapper.onTouchMove(750f, 1500f)

        assertEquals(MouseAction.MOVE, event.action)
        assertEquals(0.75f, event.xNorm, 0.001f)
        assertEquals(0.75f, event.yNorm, 0.001f)
    }

    @Test
    fun onTouchUp_emitsButtonUp() {
        val mapper = TouchToMouseMapper(1000, 2000)
        val event = mapper.onTouchUp(500f, 1000f)

        assertEquals(MouseAction.BUTTON_UP, event.action)
        assertEquals("left", event.button)
    }
}
