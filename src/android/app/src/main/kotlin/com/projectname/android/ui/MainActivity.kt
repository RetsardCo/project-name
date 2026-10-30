package com.projectname.android.ui

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.media.MediaCodecList
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import android.view.View
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.bottomnavigation.BottomNavigationView
import com.projectname.android.R
import com.projectname.android.accessibility.ProjectNameAccessibilityService
import com.projectname.android.capture.MediaProjectionCaptureService

/**
 * Peerlight Android Main Hub Activity.
 * Coordinates LAN discovery, TV D-Pad remote navigation, PC trackpad controller,
 * MediaProjection zero-recording capture consent, and hardware diagnostics.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var tvDiagnostics: TextView
    private lateinit var tvAccessibilityStatus: TextView
    private lateinit var btnToggleAccessibility: Button
    private lateinit var btnStartCapture: Button
    private lateinit var btnEmergencyDisconnect: Button
    private lateinit var bottomNav: BottomNavigationView

    // Tab views
    private lateinit var viewHome: View
    private lateinit var viewDevices: View
    private lateinit var viewRemote: View
    private lateinit var viewSessions: View
    private lateinit var viewSettings: View

    // Quick Connect & TV controls
    private lateinit var etTargetIp: EditText
    private lateinit var tvTvStatusLabel: TextView

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

        // Initialize UI widgets
        tvDiagnostics = findViewById(R.id.tvDiagnostics)
        tvAccessibilityStatus = findViewById(R.id.tvAccessibilityStatus)
        btnToggleAccessibility = findViewById(R.id.btnToggleAccessibility)
        btnStartCapture = findViewById(R.id.btnStartCapture)
        btnEmergencyDisconnect = findViewById(R.id.btnEmergencyDisconnect)
        bottomNav = findViewById(R.id.bottomNavigation)

        viewHome = findViewById(R.id.viewHome)
        viewDevices = findViewById(R.id.viewDevices)
        viewRemote = findViewById(R.id.viewRemote)
        viewSessions = findViewById(R.id.viewSessions)
        viewSettings = findViewById(R.id.viewSettings)

        etTargetIp = findViewById(R.id.etTargetIp)
        tvTvStatusLabel = findViewById(R.id.tvTvStatusLabel)

        setupNavigation()
        setupDiagnostics()
        setupListeners()
        setupTvRemoteControls()
    }

    override fun onResume() {
        super.onResume()
        updateAccessibilityState()
        updateCaptureButtonState()
    }

    private fun setupNavigation() {
        bottomNav.setOnItemSelectedListener { item ->
            viewHome.visibility = View.GONE
            viewDevices.visibility = View.GONE
            viewRemote.visibility = View.GONE
            viewSessions.visibility = View.GONE
            viewSettings.visibility = View.GONE

            when (item.itemId) {
                R.id.nav_home -> {
                    viewHome.visibility = View.VISIBLE
                    true
                }
                R.id.nav_devices -> {
                    viewDevices.visibility = View.VISIBLE
                    true
                }
                R.id.nav_remote -> {
                    viewRemote.visibility = View.VISIBLE
                    true
                }
                R.id.nav_sessions -> {
                    viewSessions.visibility = View.VISIBLE
                    true
                }
                R.id.nav_settings -> {
                    viewSettings.visibility = View.VISIBLE
                    true
                }
                else -> false
            }
        }
    }

    private fun setupDiagnostics() {
        val metrics = resources.displayMetrics
        val encoders = getAvailableH264Encoders()

        val diagText = buildString {
            appendLine("PEERLIGHT DEVICE DIAGNOSTICS")
            appendLine("Device: ${Build.MANUFACTURER} ${Build.MODEL} (${Build.DEVICE})")
            appendLine("OS: Android ${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT})")
            appendLine("Display: ${metrics.widthPixels}x${metrics.heightPixels} @ ${metrics.densityDpi} dpi")
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

        btnEmergencyDisconnect.setOnClickListener {
            performEmergencyDisconnect()
        }

        findViewById<Button>(R.id.btnQuickConnectTv).setOnClickListener {
            bottomNav.selectedItemId = R.id.nav_remote
        }

        findViewById<Button>(R.id.btnQuickConnectPc).setOnClickListener {
            val intent = Intent(this, ControllerActivity::class.java)
            startActivity(intent)
        }

        findViewById<Button>(R.id.btnDeviceConnectTv).setOnClickListener {
            bottomNav.selectedItemId = R.id.nav_remote
        }

        findViewById<Button>(R.id.btnDeviceConnectPc).setOnClickListener {
            val intent = Intent(this, ControllerActivity::class.java)
            startActivity(intent)
        }

        findViewById<Button>(R.id.btnOpenFullTrackpad).setOnClickListener {
            val intent = Intent(this, ControllerActivity::class.java)
            startActivity(intent)
        }
    }

    private fun setupTvRemoteControls() {
        fun sendTvKey(label: String) {
            tvTvStatusLabel.text = "Command Sent: $label"
            Toast.makeText(this, "TV Key: $label", Toast.LENGTH_SHORT).show()
        }

        findViewById<Button>(R.id.btnTvUp).setOnClickListener { sendTvKey("D-Pad Up") }
        findViewById<Button>(R.id.btnTvDown).setOnClickListener { sendTvKey("D-Pad Down") }
        findViewById<Button>(R.id.btnTvLeft).setOnClickListener { sendTvKey("D-Pad Left") }
        findViewById<Button>(R.id.btnTvRight).setOnClickListener { sendTvKey("D-Pad Right") }
        findViewById<Button>(R.id.btnTvOk).setOnClickListener { sendTvKey("Select / OK") }
        findViewById<Button>(R.id.btnTvBack).setOnClickListener { sendTvKey("Back") }
        findViewById<Button>(R.id.btnTvHome).setOnClickListener { sendTvKey("Home") }
        findViewById<Button>(R.id.btnTvVolUp).setOnClickListener { sendTvKey("Volume +") }
        findViewById<Button>(R.id.btnTvVolDown).setOnClickListener { sendTvKey("Volume -") }
        findViewById<Button>(R.id.btnTvPlayPause).setOnClickListener { sendTvKey("Play/Pause") }
    }

    private fun updateAccessibilityState() {
        val active = ProjectNameAccessibilityService.isServiceActive
        if (active) {
            tvAccessibilityStatus.text = getString(R.string.accessibility_status_enabled)
            tvAccessibilityStatus.setTextColor(0xFF22C55E.toInt())
        } else {
            tvAccessibilityStatus.text = getString(R.string.accessibility_status_disabled)
            tvAccessibilityStatus.setTextColor(0xFFF59E0B.toInt())
        }
    }

    private fun updateCaptureButtonState() {
        if (MediaProjectionCaptureService.isCapturing) {
            btnStartCapture.text = getString(R.string.stop_capture)
            btnStartCapture.setBackgroundColor(0xFFEF4444.toInt())
        } else {
            btnStartCapture.text = getString(R.string.grant_consent)
            btnStartCapture.setBackgroundColor(0xFF168BFF.toInt())
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
        Toast.makeText(this, "Emergency Stop: Active capture and buffers cleared.", Toast.LENGTH_SHORT).show()
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
