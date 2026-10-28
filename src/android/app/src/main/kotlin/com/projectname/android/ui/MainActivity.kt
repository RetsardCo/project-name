package com.projectname.android.ui

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.media.MediaCodecList
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.widget.Button
import android.widget.TextView
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import com.projectname.android.R
import com.projectname.android.accessibility.ProjectNameAccessibilityService
import com.projectname.android.capture.MediaProjectionCaptureService

/**
 * Diagnostic and Permission Consent Activity for Android Endpoint.
 * Reports display metrics, hardware encoder capabilities, and Accessibility status.
 * Coordinates user-approved MediaProjection screen capture with zero recording.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var tvDiagnostics: TextView
    private lateinit var tvAccessibilityStatus: TextView
    private lateinit var btnToggleAccessibility: Button
    private lateinit var btnStartCapture: Button
    private lateinit var btnOpenController: Button
    private lateinit var btnEmergencyDisconnect: Button

    private val captureConsentLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == Activity.RESULT_OK && result.data != null) {
            val metrics = resources.displayMetrics
            val serviceIntent = Intent(this, MediaProjectionCaptureService::class.java).apply {
                action = MediaProjectionCaptureService.ACTION_START
                putExtra(MediaProjectionCaptureService.EXTRA_RESULT_CODE, result.resultCode)
                putExtra(MediaProjectionCaptureService.EXTRA_RESULT_DATA, result.data)
                putExtra(MediaProjectionCaptureService.EXTRA_WIDTH, metrics.widthPixels)
                putExtra(MediaProjectionCaptureService.EXTRA_HEIGHT, metrics.heightPixels)
                putExtra(MediaProjectionCaptureService.EXTRA_DENSITY, metrics.densityDpi)
            }
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(serviceIntent)
            } else {
                startService(serviceIntent)
            }
            updateCaptureButtonState()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        tvDiagnostics = findViewById(R.id.tvDiagnostics)
        tvAccessibilityStatus = findViewById(R.id.tvAccessibilityStatus)
        btnToggleAccessibility = findViewById(R.id.btnToggleAccessibility)
        btnStartCapture = findViewById(R.id.btnStartCapture)
        btnOpenController = findViewById(R.id.btnOpenController)
        btnEmergencyDisconnect = findViewById(R.id.btnEmergencyDisconnect)

        setupDiagnostics()
        setupListeners()
    }

    override fun onResume() {
        super.onResume()
        updateAccessibilityState()
        updateCaptureButtonState()
    }

    private fun setupDiagnostics() {
        val metrics = resources.displayMetrics
        val encoders = getAvailableH264Encoders()

        val diagText = buildString {
            appendLine("DEVICE DIAGNOSTICS")
            appendLine("Model: ${Build.MANUFACTURER} ${Build.MODEL} (${Build.DEVICE})")
            appendLine("OS: Android ${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT})")
            appendLine("Screen: ${metrics.widthPixels}x${metrics.heightPixels} @ ${metrics.densityDpi} dpi")
            appendLine("Hardware H.264 Encoders: ${encoders.ifEmpty { listOf("None detected") }.joinToString(", ")}")
            appendLine("Zero-Recording Invariant: Enforced (In-memory ring buffer capacity = 2)")
        }
        tvDiagnostics.text = diagText
    }

    private fun setupListeners() {
        btnToggleAccessibility.setOnClickListener {
            val intent = Intent(Settings.ACTION_ACCESSIBILITY_SETTINGS)
            startActivity(intent)
        }

        btnStartCapture.setOnClickListener {
            if (MediaProjectionCaptureService.isCapturing) {
                stopScreenCapture()
            } else {
                val mpManager = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
                captureConsentLauncher.launch(mpManager.createScreenCaptureIntent())
            }
        }

        btnOpenController.setOnClickListener {
            val intent = Intent(this, ControllerActivity::class.java)
            startActivity(intent)
        }

        btnEmergencyDisconnect.setOnClickListener {
            performEmergencyDisconnect()
        }
    }

    private fun updateAccessibilityState() {
        val active = ProjectNameAccessibilityService.isServiceActive
        if (active) {
            tvAccessibilityStatus.text = getString(R.string.accessibility_status_enabled)
            tvAccessibilityStatus.setTextColor(0xFF4CAF50.toInt())
        } else {
            tvAccessibilityStatus.text = getString(R.string.accessibility_status_disabled)
            tvAccessibilityStatus.setTextColor(0xFFFFA726.toInt())
        }
    }

    private fun updateCaptureButtonState() {
        if (MediaProjectionCaptureService.isCapturing) {
            btnStartCapture.text = getString(R.string.stop_capture)
            btnStartCapture.setBackgroundColor(0xFFD32F2F.toInt())
        } else {
            btnStartCapture.text = getString(R.string.grant_consent)
            btnStartCapture.setBackgroundColor(0xFF1976D2.toInt())
        }
    }

    private fun stopScreenCapture() {
        val stopIntent = Intent(this, MediaProjectionCaptureService::class.java).apply {
            action = MediaProjectionCaptureService.ACTION_STOP
        }
        startService(stopIntent)
        updateCaptureButtonState()
    }

    private fun performEmergencyDisconnect() {
        stopScreenCapture()
        MediaProjectionCaptureService.frameBuffer.clear()
        finish()
    }

    private fun getAvailableH264Encoders(): List<String> {
        val encoderList = mutableListOf<String>()
        val codecList = MediaCodecList(MediaCodecList.REGULAR_CODECS)
        for (info in codecList.codecInfos) {
            if (!info.isEncoder) continue
            for (type in info.supportedTypes) {
                if (type.equals("video/avc", ignoreCase = true)) {
                    encoderList.add(info.name)
                }
            }
        }
        return encoderList
    }
}
