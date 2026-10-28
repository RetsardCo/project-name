package com.projectname.android.capture

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.graphics.PixelFormat
import android.hardware.display.DisplayManager
import android.hardware.display.VirtualDisplay
import android.media.ImageReader
import android.media.projection.MediaProjection
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat

/**
 * Android Foreground Service executing ephemeral live screen capture via MediaProjection.
 * Strictly adheres to Non-Negotiable Requirement 3.1:
 * - Zero screen recording: No MediaRecorder, FileOutputStream, or disk caching is ever created.
 * - Bounded in-memory frame buffer (capacity <= 2).
 * - Immediate teardown on consent revocation or session termination.
 */
class MediaProjectionCaptureService : Service() {

    companion object {
        const val ACTION_START = "com.projectname.android.capture.START"
        const val ACTION_STOP = "com.projectname.android.capture.STOP"
        const val EXTRA_RESULT_CODE = "extra_result_code"
        const val EXTRA_RESULT_DATA = "extra_result_data"
        const val EXTRA_WIDTH = "extra_width"
        const val EXTRA_HEIGHT = "extra_height"
        const val EXTRA_DENSITY = "extra_density"

        private const val NOTIFICATION_CHANNEL_ID = "screen_capture_channel"
        private const val NOTIFICATION_ID = 4001

        val frameBuffer = BoundedFrameBuffer(maxCapacity = 2)
        var isCapturing: Boolean = false
            private set
    }

    private var mediaProjection: MediaProjection? = null
    private var virtualDisplay: VirtualDisplay? = null
    private var imageReader: ImageReader? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        val action = intent?.action ?: return START_NOT_STICKY

        when (action) {
            ACTION_START -> {
                val resultCode = intent.getIntExtra(EXTRA_RESULT_CODE, 0)
                val resultData = intent.getParcelableExtra<Intent>(EXTRA_RESULT_DATA)
                val width = intent.getIntExtra(EXTRA_WIDTH, 1280)
                val height = intent.getIntExtra(EXTRA_HEIGHT, 720)
                val density = intent.getIntExtra(EXTRA_DENSITY, 320)

                if (resultCode != 0 && resultData != null) {
                    startForeground(NOTIFICATION_ID, createForegroundNotification())
                    startMediaProjection(resultCode, resultData, width, height, density)
                }
            }
            ACTION_STOP -> {
                stopMediaProjection()
                stopForeground(STOP_FOREGROUND_REMOVE)
                stopSelf()
            }
        }

        return START_NOT_STICKY
    }

    private fun startMediaProjection(resultCode: Int, resultData: Intent, width: Int, height: Int, density: Int) {
        val mpManager = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
        val mp = mpManager.getMediaProjection(resultCode, resultData) ?: return
        mediaProjection = mp

        mp.registerCallback(object : MediaProjection.Callback() {
            override fun onStop() {
                stopMediaProjection()
                stopSelf()
            }
        }, null)

        // Ephemeral in-memory ImageReader surface with maxImages = 2
        val reader = ImageReader.newInstance(width, height, PixelFormat.RGBA_8888, 2)
        imageReader = reader

        reader.setOnImageAvailableListener({ ir ->
            try {
                val image = ir.acquireLatestImage()
                if (image != null) {
                    try {
                        val planes = image.planes
                        val buffer = planes[0].buffer
                        val pixelStride = planes[0].pixelStride
                        val rowStride = planes[0].rowStride
                        val rowPadding = rowStride - pixelStride * width

                        // Extract ephemeral raw bytes into memory buffer
                        val bytes = ByteArray(buffer.remaining())
                        buffer.get(bytes)

                        // Push directly to bounded in-memory buffer (zero disk writes)
                        frameBuffer.pushFrame(bytes, width, height, System.currentTimeMillis())
                    } finally {
                        image.close()
                    }
                }
            } catch (ex: Exception) {
                // Silently drop frame on backpressure/surface recreation
            }
        }, null)

        virtualDisplay = mp.createVirtualDisplay(
            "ProjectNameLiveDisplay",
            width,
            height,
            density,
            DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
            reader.surface,
            null,
            null
        )

        isCapturing = true
    }

    private fun stopMediaProjection() {
        isCapturing = false
        frameBuffer.clear()

        try {
            virtualDisplay?.release()
            virtualDisplay = null
        } catch (_: Exception) {}

        try {
            imageReader?.close()
            imageReader = null
        } catch (_: Exception) {}

        try {
            mediaProjection?.stop()
            mediaProjection = null
        } catch (_: Exception) {}
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                NOTIFICATION_CHANNEL_ID,
                "Live Screen Sharing",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "Active notification while live screen viewing is running."
            }
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }

    private fun createForegroundNotification(): Notification {
        return NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
            .setContentTitle("Screen Sharing Active")
            .setContentText("Streaming live display to authenticated controller over LAN.")
            .setSmallIcon(android.R.drawable.presence_video_online)
            .setOngoing(true)
            .build()
    }

    override fun onDestroy() {
        super.onDestroy()
        stopMediaProjection()
    }
}
