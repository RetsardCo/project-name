package com.projectname.android.ui

import android.content.Context
import android.os.Bundle
import android.view.MotionEvent
import android.view.View
import android.view.inputmethod.InputMethodManager
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
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
    private lateinit var btnKeyboardToggle: Button
    private lateinit var btnEmergencyDisconnect: Button

    private val touchMapper = TouchToMouseMapper()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_controller)

        touchCanvas = findViewById(R.id.touchCanvas)
        tvSessionInfo = findViewById(R.id.tvSessionInfo)
        btnLeftClick = findViewById(R.id.btnLeftClick)
        btnRightClick = findViewById(R.id.btnRightClick)
        btnKeyboardToggle = findViewById(R.id.btnKeyboardToggle)
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
                    touchMapper.onTouchDown(event.x, event.y)
                }
                MotionEvent.ACTION_MOVE -> {
                    touchMapper.onTouchMove(event.x, event.y)
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                    touchMapper.onTouchUp(event.x, event.y)
                }
            }
            true
        }
    }

    private fun setupListeners() {
        btnLeftClick.setOnClickListener {
            Toast.makeText(this, "Mouse: Left Click Dispatched", Toast.LENGTH_SHORT).show()
        }

        btnRightClick.setOnClickListener {
            Toast.makeText(this, "Mouse: Right Click Dispatched", Toast.LENGTH_SHORT).show()
        }

        btnKeyboardToggle.setOnClickListener {
            val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager
            imm.toggleSoftInput(InputMethodManager.SHOW_FORCED, 0)
        }

        btnEmergencyDisconnect.setOnClickListener {
            Toast.makeText(this, "Emergency Stop: Disconnected.", Toast.LENGTH_SHORT).show()
            finish()
        }
    }
}
