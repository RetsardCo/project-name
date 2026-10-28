package com.projectname.android.ui

import android.os.Bundle
import android.view.MotionEvent
import android.view.View
import android.widget.Button
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.projectname.android.R
import com.projectname.android.controller.TouchToMouseMapper

/**
 * Controller Activity providing touch-to-pointer mapping for Windows remote control.
 * Features an interactive touch canvas and a prominent Emergency Disconnect button.
 */
class ControllerActivity : AppCompatActivity() {

    private lateinit var touchCanvas: View
    private lateinit var tvSessionInfo: TextView
    private lateinit var btnLeftClick: Button
    private lateinit var btnRightClick: Button
    private lateinit var btnEmergencyDisconnect: Button

    private val touchMapper = TouchToMouseMapper()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_controller)

        touchCanvas = findViewById(R.id.touchCanvas)
        tvSessionInfo = findViewById(R.id.tvSessionInfo)
        btnLeftClick = findViewById(R.id.btnLeftClick)
        btnRightClick = findViewById(R.id.btnRightClick)
        btnEmergencyDisconnect = findViewById(R.id.btnControllerEmergencyDisconnect)

        setupTouchSurface()
        setupListeners()
    }

    private fun setupTouchSurface() {
        touchCanvas.post {
            touchMapper.updateDimensions(touchCanvas.width, touchCanvas.height)
        }

        touchCanvas.setOnTouchListener { v, event ->
            v.performClick()
            when (event.actionMasked) {
                MotionEvent.ACTION_DOWN -> {
                    // Map touch down to pointer move + click
                    touchMapper.onTouchDown(event.x, event.y)
                }
                MotionEvent.ACTION_MOVE -> {
                    // Map touch move
                    touchMapper.onTouchMove(event.x, event.y)
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                    // Map touch release
                    touchMapper.onTouchUp(event.x, event.y)
                }
            }
            true
        }
    }

    private fun setupListeners() {
        btnLeftClick.setOnClickListener {
            // Emits discrete left click
        }

        btnRightClick.setOnClickListener {
            // Emits discrete right click
        }

        btnEmergencyDisconnect.setOnClickListener {
            // Immediate local emergency disconnect
            finish()
        }
    }
}
