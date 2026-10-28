package com.projectname.android.accessibility

import android.accessibilityservice.AccessibilityService
import android.accessibilityservice.GestureDescription
import android.view.accessibility.AccessibilityEvent

/**
 * AccessibilityService implementation providing zero-ADB remote input injection.
 * Dispatches programmatic tap and swipe/scroll gestures across third-party applications
 * via dispatchGesture() with asynchronous completion callbacks.
 *
 * Privacy & Security Invariant:
 * Strictly does not inspect, scrape, or log screen contents or user keystrokes.
 */
class ProjectNameAccessibilityService : AccessibilityService() {

    companion object {
        var instance: ProjectNameAccessibilityService? = null
            private set

        val isServiceActive: Boolean
            get() = instance != null
    }

    override fun onServiceConnected() {
        super.onServiceConnected()
        instance = this
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        // Strict Invariant: Intentionally empty. No screen scraping or keylogging.
    }

    override fun onInterrupt() {
        // Handled on service interruption
    }

    override fun onDestroy() {
        super.onDestroy()
        if (instance == this) {
            instance = null
        }
    }

    /**
     * Dispatches a tap gesture at the specified normalized coordinates.
     */
    fun dispatchTap(
        xNorm: Float,
        yNorm: Float,
        durationMs: Long = 50L,
        onComplete: ((Boolean) -> Unit)? = null
    ): Boolean {
        val displayMetrics = resources.displayMetrics
        val gesture = GestureDispatcher.buildTapGesture(
            xNorm,
            yNorm,
            displayMetrics.widthPixels,
            displayMetrics.heightPixels,
            durationMs
        )

        return dispatchGesture(
            gesture,
            object : GestureResultCallback() {
                override fun onCompleted(gestureDescription: GestureDescription?) {
                    onComplete?.invoke(true)
                }

                override fun onCancelled(gestureDescription: GestureDescription?) {
                    onComplete?.invoke(false)
                }
            },
            null
        )
    }

    /**
     * Dispatches a scroll or swipe gesture from start to end normalized coordinates.
     */
    fun dispatchSwipe(
        startXNorm: Float,
        startYNorm: Float,
        endXNorm: Float,
        endYNorm: Float,
        durationMs: Long = 300L,
        onComplete: ((Boolean) -> Unit)? = null
    ): Boolean {
        val displayMetrics = resources.displayMetrics
        val gesture = GestureDispatcher.buildSwipeGesture(
            startXNorm,
            startYNorm,
            endXNorm,
            endYNorm,
            displayMetrics.widthPixels,
            displayMetrics.heightPixels,
            durationMs
        )

        return dispatchGesture(
            gesture,
            object : GestureResultCallback() {
                override fun onCompleted(gestureDescription: GestureDescription?) {
                    onComplete?.invoke(true)
                }

                override fun onCancelled(gestureDescription: GestureDescription?) {
                    onComplete?.invoke(false)
                }
            },
            null
        )
    }

    /**
     * Performs standard Android global system navigation actions without requiring root or ADB.
     */
    fun triggerGlobalAction(action: Int): Boolean {
        return performGlobalAction(action)
    }
}
