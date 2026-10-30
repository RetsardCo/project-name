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
import androidx.lifecycle.lifecycleScope
import com.projectname.android.R
import com.projectname.android.client.PeerlightWindowsClient
import com.projectname.android.controller.TouchToMouseMapper
import kotlinx.coroutines.launch

/**
 * Controller Activity providing touch-to-pointer mapping for Windows remote control.
 * Features an interactive touch canvas and a prominent Emergency Disconnect button.
 */
class ControllerActivity : AppCompatActivity() {

    companion object {
        const val EXTRA_TARGET_HOST = "extra_target_host"
        const val EXTRA_TARGET_PORT = "extra_target_port"
    }

    private lateinit var touchCanvas: View
    private lateinit var tvSessionInfo: TextView
    private lateinit var btnLeftClick: Button
    private lateinit var btnRightClick: Button
    private lateinit var btnKeyboardToggle: Button
    private lateinit var btnEmergencyDisconnect: Button

    private val touchMapper = TouchToMouseMapper()
    private val windowsClient = PeerlightWindowsClient()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_controller)

        touchCanvas = findViewById(R.id.touchCanvas)
        tvSessionInfo = findViewById(R.id.tvSessionInfo)
        btnLeftClick = findViewById(R.id.btnLeftClick)
        btnRightClick = findViewById(R.id.btnRightClick)
        btnKeyboardToggle = findViewById(R.id.btnKeyboardToggle)
        btnEmergencyDisconnect = findViewById(R.id.btnControllerEmergencyDisconnect)

        val targetHost = intent.getStringExtra(EXTRA_TARGET_HOST) ?: "192.168.1.100"
        val targetPort = intent.getIntExtra(EXTRA_TARGET_PORT, PeerlightWindowsClient.DEFAULT_PORT)

        tvSessionInfo.text = "Connecting to Windows PC at $targetHost:$targetPort..."

        lifecycleScope.launch {
            val result = windowsClient.connect(targetHost, targetPort)
            if (result.isSuccess) {
                tvSessionInfo.text = "Connected to Windows PC at $targetHost:$targetPort (Active)"
            } else {
                tvSessionInfo.text = "Local Preview Mode (Offline: ${result.exceptionOrNull()?.message})"
            }
        }

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
                    val events = touchMapper.onTouchDown(event.x, event.y)
                    if (windowsClient.isConnected && events.isNotEmpty()) {
                        lifecycleScope.launch {
                            windowsClient.sendPointerMove(events[0].xNorm, events[0].yNorm)
                        }
                    }
                }
                MotionEvent.ACTION_MOVE -> {
                    val move = touchMapper.onTouchMove(event.x, event.y)
                    if (windowsClient.isConnected) {
                        lifecycleScope.launch {
                            windowsClient.sendPointerMove(move.xNorm, move.yNorm)
                        }
                    }
                }
                MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                    val up = touchMapper.onTouchUp(event.x, event.y)
                    if (windowsClient.isConnected) {
                        lifecycleScope.launch {
                            windowsClient.sendPointerMove(up.xNorm, up.yNorm)
                        }
                    }
                }
            }
            true
        }
    }

    private fun setupListeners() {
        btnLeftClick.setOnClickListener {
            if (windowsClient.isConnected) {
                lifecycleScope.launch {
                    windowsClient.sendPointerButton("left", "click")
                }
            }
            Toast.makeText(this, "Mouse: Left Click Dispatched", Toast.LENGTH_SHORT).show()
        }

        btnRightClick.setOnClickListener {
            if (windowsClient.isConnected) {
                lifecycleScope.launch {
                    windowsClient.sendPointerButton("right", "click")
                }
            }
            Toast.makeText(this, "Mouse: Right Click Dispatched", Toast.LENGTH_SHORT).show()
        }

        btnKeyboardToggle.setOnClickListener {
            val imm = getSystemService(Context.INPUT_METHOD_SERVICE) as InputMethodManager
            imm.toggleSoftInput(InputMethodManager.SHOW_FORCED, 0)
        }

        btnEmergencyDisconnect.setOnClickListener {
            windowsClient.disconnect()
            Toast.makeText(this, "Emergency Stop: Disconnected.", Toast.LENGTH_SHORT).show()
            finish()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        windowsClient.disconnect()
    }
}
