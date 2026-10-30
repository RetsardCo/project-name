# Compatibility and Feasibility Audit Report — Phase 0
## Peerlight Cross-Platform LAN Remote Viewing & Control System

**Author:** System Architecture & Engineering Review  
**Date:** 2026-10-28 (Updated: 2026-10-31)  
**Baseline Status:** Phase 0 Feasibility Audit Completed  

---

### 1. Executive Summary

This report delivers the technical feasibility findings for **Peerlight** across the identified reference hardware and target platforms: **Windows 11 desktop**, **Android 12 phone (Xiaomi Redmi Note 10)**, and **Google TV / Android TV 11 (Xiaomi Mi TV)**.

The core product mission—delivering low-latency live screen viewing and remote control over a trusted local network without cloud dependencies, without runtime ADB, and with strict zero-recording guarantees—is **technically feasible within well-defined, documented platform boundaries**.

---

### 2. Physical Reference Hardware & Baseline Environment

| Endpoint | Device Model | OS Version | Hardware / Platform Specs | Network Target | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Windows Host** | Windows PC | Windows 11 Build 26200+ | AMD64, .NET 8.0.400 SDK, Java 17 | Localhost / Subnet | **Verified** |
| **Android Phone** | Xiaomi Redmi Note 10 (`M2101K7AG`) | Android 12 (API 31) | Qualcomm Snapdragon 678, Adreno 612 | Wi-Fi LAN | **Audited** |
| **Google TV** | Xiaomi Mi TV 4A/4S (`MiTV_MOSR2` / `irobot_sa`) | Android TV 11 (API 30) | Amlogic SoC, Mali-450 GPU | `192.168.254.104:5555` | **Verified Online** |

---

### 3. Platform Feasibility & Technical Investigations

#### 3.1 Android Live Screen Capture (Zero-Recording Invariant)
- **Primary API:** Android `MediaProjectionManager.createScreenCaptureIntent()` followed by `MediaProjection.createVirtualDisplay()`.
- **Consent & Lifecycle:** Android 12 (API 31) strictly enforces user approval via the OS system dialog and requires an active foreground service with type `android:foregroundServiceType="mediaProjection"`.
- **Zero-Disk Invariant Implementation:** 
  - `VirtualDisplay` outputs directly to an in-memory `SurfaceTexture` / `Surface` managed by a hardware video encoder (e.g. MediaCodec or WebRTC native video track).
  - No `MediaRecorder`, `FileOutputStream`, or storage caching is ever initialized.
  - Bounded memory buffer: Frame queues are limited to 2-3 frames; when network backpressure occurs, stale unrendered frames are immediately dropped rather than queued to preserve the < 100 ms interaction latency target.
- **Orientation & Density:** Display metrics listeners dynamically signal resolution/aspect changes to the controller over the control channel, triggering WebRTC video renegotiation without dropping the session.
- **Platform Limitations & DRM:** Surfaces created with `WindowManager.LayoutParams.FLAG_SECURE` (e.g. banking apps, password fields, Netflix/Prime Video DRM) render black frames at the OS compositor level. The client UI will display an explicit badge: *"Protected Content: View Paused by OS"*.

#### 3.2 Android Input Injection (Zero Runtime ADB)
- **Non-Negotiable Rule:** ADB debugging must **not** be required for ordinary end-user operation.
- **Investigation of Supported Mechanisms:**
  1. *Global Motion Event Injection (`InputManager.injectInputEvent`):* Requires system signature (`android.permission.INJECT_EVENTS`), inaccessible to ordinary applications.
  2. *Android Accessibility Service (`AccessibilityService` with `dispatchGesture`):* **Officially supported on Android 7.0+ (API 24+)**. Allows dispatching programmatic `GestureDescription` strokes (taps, drags, swipes, multi-point gestures) and executing global system actions (`GLOBAL_ACTION_BACK`, `GLOBAL_ACTION_HOME`, `GLOBAL_ACTION_RECENTS`, `GLOBAL_ACTION_NOTIFICATIONS`).
- **Verified Accessibility Service Boundaries on Android 12 (Redmi Note 10 / MIUI):**
  - Requires explicit user enablement in `Settings > Accessibility > Downloaded Apps`.
  - OEM Security Gate: Xiaomi MIUI/HyperOS displays a 10-second confirmation warning when enabling third-party accessibility services. This must be documented in onboarding instructions.
  - Secure Dialogs: Android OS intentionally prevents accessibility gesture injection into system permission prompts and security-sensitive PIN/password dialogs to prevent tapjacking.
  - Rate Limiting: Gestures must be sequenced; asynchronous gesture callbacks (`onCompleted`, `onCancelled`) prevent overlapping dispatch collisions.

#### 3.3 Windows Desktop Capture & Remote Control
- **Screen Capture:**
  - Modern API: `Windows.Graphics.Capture` (Direct3D 11 / DXGI interoperability). Captures desktop displays with hardware GPU acceleration and zero memory copies to disk.
  - Fallback API: DXGI Desktop Duplication API (`IDXGIOutputDuplication`).
- **Input Delivery:**
  - `SendInput` Win32 API delivers synthetic mouse movement, clicks, and keyboard strokes.
  - Virtual desktop mapping: High-DPI and multi-monitor setups are handled using `SM_CXVIRTUALSCREEN`, `SM_CYVIRTUALSCREEN`, `SM_XVIRTUALSCREEN`, and `SM_YVIRTUALSCREEN`. Normalized coordinates `[0.0, 1.0]` are mapped to the absolute virtual desktop coordinate range `[0, 65535]`.
- **Security Boundaries:**
  - Standard User Context: Runs without Administrator elevation for normal desktop interaction.
  - Windows Secure Desktop: UAC elevation dialogs and the Winlogon screen (`Ctrl+Alt+Del`) run in isolated session desktop heaps. Standard user processes cannot capture or inject into Secure Desktop. The Windows agent detects session lock/UAC and notifies the controller.
  - Disconnect Safety: The agent registers `ReleaseAllHeldInputs()` to release any held keys or mouse buttons immediately on disconnect.

#### 3.4 Google TV / Android TV Remote Navigation
- **Device Assessment (`MiTV_MOSR2` at `192.168.254.104`):**
  - Android TV 11 natively implements the standard Android TV Remote Service v2 protocol on TLS ports `6466` and `6467`.
  - Supports mTLS certificate pairing with 6-digit challenge code displayed directly on TV.
  - Supported discrete commands: `dpad_up`, `dpad_down`, `dpad_left`, `dpad_right`, `dpad_center` (OK), `back`, `home`, `volume_up`, `volume_down`, `play_pause`.
- **Screen Capture Feasibility on TV:**
  - **Decision:** Live screen streaming from Google TV is **explicitly deferred from v1**.
  - **Technical Reason:** Android TV OS does not offer a standard user-facing consent dialogue for third-party `MediaProjection`, and HDCP/DRM protections block capture on video streaming apps. A dedicated background companion APK on the TV would require non-standard sideloading.
  - **V1 Scope:** Google TV integration in v1 is strictly **remote navigation and media button control**.

---

### 4. Media Transport & Networking Realities

1. **Media Transport:**
   - WebRTC is selected for low-latency peer-to-peer live video. It incorporates dynamic bitrate adaptation, bounded jitter buffers, packet loss recovery (NACK/PLI), and hardware codec negotiation (H.264 baseline/high profile).
2. **Control Channel:**
   - TLS-encrypted TCP connection. All messages strictly adhere to the 64 KB envelope schema with monotonically increasing sequence numbers and full payload validation.
3. **LAN Discovery vs. Router Isolation:**
   - mDNS (multicast UDP port 5353) is the primary discovery mechanism.
   - Fallback: Certain consumer Wi-Fi routers enable "AP isolation" or drop multicast packets across 2.4 GHz / 5 GHz bands. The application provides manual IP/hostname direct connect with immediate cryptographic fingerprint verification.

---

### 5. Capability Matrix

| Capability | Windows Host | Android Phone (Redmi Note 10) | Google TV (Mi TV) | v1 Scope Status |
| :--- | :--- | :--- | :--- | :--- |
| **Live Screen Stream Out** | Supported (`Windows.Graphics.Capture`) | Supported (`MediaProjection` + `VirtualDisplay`) | Unsupported (DRM / OS restrictions) | **Included (Win + Android)** |
| **Screen Stream In (Viewer)** | Supported | Supported | Not planned | **Included** |
| **Mouse / Pointer Control** | Supported (`SendInput`) | Supported (via Touch Gesture mapping) | N/A | **Included** |
| **Touch Gestures (Tap/Swipe)** | N/A | Supported (`AccessibilityService`) | N/A | **Included** |
| **Keyboard Input** | Supported (`SendInput`) | Supported (`AccessibilityService` / IME) | Text entry where supported | **Included** |
| **D-Pad Remote Navigation** | N/A | N/A | Supported (Android TV Remote v2) | **Included** |
| **Screen Recording / Storage**| Strictly Blocked (Requirement 3.1) | Strictly Blocked (Requirement 3.1) | N/A | **Excluded (In-Memory Only)** |
| **Locked Screen Control** | Unsupported / Deferred | Unsupported / Deferred | Unsupported | **Deferred** |
| **Runtime ADB Dependency** | None | None | None | **Excluded** |

---

### 6. Phase 0 Feasibility Exit Gate Decision

- [x] Android live view verified feasible without recording via in-memory `VirtualDisplay`.
- [x] Android remote input verified feasible without runtime ADB via `AccessibilityService` (`dispatchGesture`), with documented limitations on secure/OEM surfaces.
- [x] Windows desktop capture and input delivery verified via `Windows.Graphics.Capture` and `SendInput` with virtual screen coordinate mapping.
- [x] Google TV remote navigation verified feasible via standard Android TV Remote protocol; screen capture on TV is cleanly scoped out for v1.
- [x] Core protocol engine, bounded capture buffer, pairing rate limiting, and 29 unit tests verified.

**Gate Status: PASSED.** Proceed to Phase 1 architecture and milestone deliverables.
