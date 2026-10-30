# Peerlight — Comprehensive Project Audit & Discovery Report

**Document Version:** 1.0.0  
**Audit Date:** 2026-10-31  
**Project Workspace:** `C:\Users\Kurados\.gemini\antigravity\scratch\project-name`  
**GitHub Remote:** `https://github.com/RetsardCo/project-name.git`  
**Base Commit:** `7eb09d9 feat(apps): add executable entry points and publish standalone .exe binaries for desktop and TV remote`  
**Active Working Branch:** `feature/peerlight-rebrand-ui`  
**Recovery Branch:** `recovery-baseline` (Created before any modifications)

---

## 1. Repository Structure & Technology Stack

The repository is organized as a multi-platform solution supporting Windows desktop, Android mobile, and Google TV / Android TV targets:

```
project-name/
├── .github/
│   └── workflows/ci.yml                        # GitHub Actions CI build & test workflow
├── docs/
│   ├── decisions/                              # Architecture Decision Records (ADRs 0001-0003)
│   ├── FEASIBILITY_REPORT.md                   # Hardware & protocol feasibility report
│   ├── PROJECT_NAME_Master_TODO.md             # Planning & task checklist
│   ├── PROJECT_NAME_Project_Documentation.md   # Core requirements and PRD
│   └── PROTOCOL_SPEC.md                        # Network protocol & message envelope spec
├── publish/                                    # Existing published binaries
│   ├── standalone/                             # Self-contained single-file executables (.NET 8 win-x64)
│   ├── tv-remote/                              # Framework-dependent Google TV remote CLI
│   └── windows-app/                            # Framework-dependent Windows desktop CLI
├── src/
│   ├── android/                                # Android mobile client & endpoint (Kotlin, AGP 8.5, SDK 34)
│   │   ├── app/src/main/kotlin/                # MediaProjection, AccessibilityService, TouchToMouseMapper
│   │   └── app/src/main/res/                   # Layouts, values, strings, XML configs
│   ├── shared/
│   │   └── ProjectName.Shared/                 # C# .NET 8 core library (Protocol, TLS, mDNS, Pairing, Media)
│   ├── tv/
│   │   └── ProjectName.Tv/                     # C# .NET 8 Google TV remote navigation client
│   └── windows/
│       └── ProjectName.Windows/                # C# .NET 8 Windows desktop endpoint & input injector
├── tests/
│   ├── shared/ProjectName.Shared.Tests/        # 50 unit tests (Protocol, Transport, Session, Discovery, Media)
│   ├── tv/ProjectName.Tv.Tests/                # 3 unit tests (TvRemoteController commands & ping)
│   └── windows/ProjectName.Windows.Tests/      # 9 unit tests (WindowsDesktopEndpoint & CaptureBuffer)
├── CONTRIBUTING.md                             # Branching and commit standards
├── LICENSE                                     # MIT License
├── ProjectName.sln                             # Visual Studio / .NET solution
├── README.md                                   # Root project overview
└── SECURITY.md                                 # Security policy & vulnerability disclosures
```

### Technology Stack Summary
- **Windows Desktop:** .NET 8.0 SDK (`net8.0-windows`), Win32 `SendInput`, `Windows.Graphics.Capture` / DXGI Desktop Duplication, Direct3D 11.
- **Android Mobile:** Kotlin 1.9.20, Android Gradle Plugin 8.5.0, Android SDK Min 28 / Target 34, AndroidX, ViewBinding, `MediaProjection`, `AccessibilityService`.
- **Google TV / Android TV:** Android TV Remote Service protocol v2 over mutual TLS (ports 6466 & 6467).
- **Core Networking:** TCP with TLS 1.3 / 1.2, UDP multicast mDNS port 5353 (`224.0.0.251`), WebRTC bounded signaling.
- **Testing:** xUnit 2.5.3, JUnit 4.13.2, Kotlin Coroutines Test.

---

## 2. Existing Application Architectures

### 2.1 Windows Desktop Architecture
- **Endpoint Service (`WindowsDesktopEndpoint.cs`):** Manages TLS server, pairing manager, session coordinator, desktop screen capture service, and input injector.
- **Capture Engine (`WindowsDesktopCaptureService.cs`):** Captures desktop surface with zero disk writing using bounded frame queues (capacity = 2). Drops stale frames under backpressure.
- **Input Injection (`WindowsInputInjector.cs`):** P/Invoke `SendInput` handling absolute pointer coordinates mapped to the virtual desktop space (`SM_CXVIRTUALSCREEN`, `SM_CYVIRTUALSCREEN`). Emits keyboard scan codes and mouse clicks.
- **Safety Features:** Disconnect safety resets all held keys and mouse buttons (`ReleaseAllHeldInputs()`). Detects Windows session lock and UAC isolation.
- **Current UI State:** A console terminal interface with ASCII menus, status displays, and keyboard shortcuts (`P`, `T`, `E`, `S`, `Q`). Needs a macOS-inspired, clean graphical desktop UI.

### 2.2 Android Application Architecture
- **Screen Capture Service (`MediaProjectionCaptureService.kt`):** Foreground service with `android:foregroundServiceType="mediaProjection"`. Outputs virtual display directly to an in-memory `BoundedFrameBuffer` (capacity = 2) with zero storage caching.
- **Input Injection Service (`ProjectNameAccessibilityService.kt`):** Leverages `AccessibilityService.dispatchGesture()` for taps and scrolls without requiring root or ADB.
- **Touch-to-Mouse Controller (`TouchToMouseMapper.kt`):** Normalizes touchscreen coordinates to `[0.0, 1.0]` for remote PC pointer control.
- **Current UI State:** Basic ScrollView layout (`MainActivity`) and RelativeLayout (`ControllerActivity`) with simple diagnostic text and buttons. Needs a modern, tabbed/bottom-navigation interface with Peerlight branding.

### 2.3 Google TV / Android TV Integration Architecture
- **Protocol Client (`GoogleTvRemoteClient.cs`):** Connects to Android TV Remote Service on TLS ports 6466/6467. Employs ephemeral device certificate generation and TLS handshake.
- **Controller API (`TvRemoteController.cs`):** Dispatches D-Pad navigation (`Up`, `Down`, `Left`, `Right`, `Select`), system keys (`Back`, `Home`), and media keys (`VolumeUp`, `VolumeDown`, `PlayPause`).
- **Screen Capture Feasibility:** Live TV screen streaming is unsupported in v1 due to lack of standard TV consent UI and HDCP/DRM restrictions on media apps. Integration is strictly focused on remote navigation.

---

## 3. Implementation Status of Device-Control Matrix

| Controller | Target | Screen Viewing | Remote Control | Status Classification |
| :--- | :--- | :--- | :--- | :--- |
| **Windows PC** | **Android Phone** | Yes (`MediaProjection`) | Yes (`AccessibilityService`) | **IMPLEMENTED AND VERIFIED** (Core logic & tests pass; UI enhancement needed) |
| **Windows PC** | **Google TV / Android TV** | No (OS / DRM restriction) | Yes (TLS 6466/6467 D-Pad) | **IMPLEMENTED AND VERIFIED** (Remote navigation verified; TV streaming blocked by platform) |
| **Android Phone** | **Windows PC** | Yes (`Windows.Graphics.Capture`) | Yes (`SendInput` via touch) | **IMPLEMENTED AND VERIFIED** (Backend endpoints & touch mappers verified; UI polish needed) |
| **Android Phone** | **Google TV / Android TV** | No | Yes (Remote D-Pad) | **PARTIALLY IMPLEMENTED** (Shared TV protocol client exists; Android TV remote UI needed) |

---

## 4. Current GUI Status
- **Windows Desktop:** Functional CLI menu. Requires transformation into a clean, modern, macOS-inspired desktop GUI with sidebar navigation, device cards, pairing modals, viewing workspace, TV remote D-Pad, and settings.
- **Android Mobile:** Functional diagnostic screens. Requires transformation into a cohesive mobile interface with Peerlight branding, bottom navigation, device discovery list, TV D-Pad remote, and PC trackpad controller.

---

## 5. Build Environment & Verification Status
- **.NET SDK:** 8.0.400 installed. Solution compiles cleanly. All 62 unit tests pass across `ProjectName.Shared.Tests` (50), `ProjectName.Tv.Tests` (3), and `ProjectName.Windows.Tests` (9).
- **Android SDK:** Compile SDK 34, Min SDK 28, JDK 17. Gradle 8.5 assembleDebug succeeds and generates `app-debug.apk`. (Fixed minor `gradlew.bat` exit code fallthrough to ensure clean build scripts).
- **Standalone Binaries:** Pre-existing published executables present in `publish/`.

---

## 6. Risks, Blockers & Platform Limitations
1. **Android TV Screen Streaming:** Strictly blocked by platform limitations (no standard TV consent dialog, DRM/HDCP protection). Remote navigation (D-Pad, Select, Back, Home, Volume) is fully supported.
2. **Android Package Identifier:** Android `applicationId = "com.projectname.android"` must remain untouched to prevent breaking application upgrades, keystore signing continuity, and device permissions. User-facing app name, launcher label, activities, and branding must be renamed to Peerlight.
3. **Windows Secure Desktop & UAC:** P/Invoke `SendInput` cannot cross UAC elevation or Winlogon desktop boundaries without kernel/system service elevation. This is a deliberate OS security feature and is documented clearly.
4. **Zero-Recording Invariant:** Under no circumstances will screen capture write frames to disk or maintain persistent video buffers. Only transient in-memory ring buffers of capacity 2 are permitted.

---

## 7. Selected Antigravity Skills & Applied Principles
- **brand:** Visual identity guidelines, color palettes (#071426 deep navy, #168BFF primary blue, #20D9E8 cyan), typography rules, logo asset sizing.
- **design-system:** Three-layer token architecture (primitive → semantic → component), consistent spacing scales, restrained corner radii, accessible dark/light contrast.
- **create-game-assets / image processing:** High-fidelity icon generation from approved Peerlight logo (`media_1793383756613_46f4541e.jpg`) to generate Windows `.ico`, Android mipmap densities, and scalable vector assets.

---

## 8. Phase-by-Phase Execution Order
1. **Phase 1: Product Rebranding** — Update all product names, titles, displays, and metadata to Peerlight while preserving technical package identifiers.
2. **Phase 2: Brand Assets & Guidelines** — Generate all required icon and logo variants from official asset and produce `docs/BRAND_GUIDELINES.md`.
3. **Phase 3 & 4: Device-Control & Networking** — Align mDNS service type to `_peerlight._tcp.local.` and verify pairing/session pipelines.
4. **Phase 5: Windows Desktop GUI** — Build a polished, macOS-inspired desktop interface with sidebar navigation, device cards, TV remote, and settings.
5. **Phase 6: Android Application GUI** — Build a cohesive mobile-native interface with bottom navigation, TV remote, and PC controller.
6. **Phase 7: Shared Design System** — Create `docs/PEERLIGHT_DESIGN_SYSTEM.md`.
7. **Phase 8: Documentation Overhaul** — Update PRD, Master TODO, Feature Matrix, and Architecture docs.
8. **Phase 9: GitHub Synchronization** — Synchronize repo documentation, workflows, and branch `feature/peerlight-rebrand-ui`.
9. **Phase 10: Release Builds** — Compile Windows desktop application and Android APK.
10. **Phase 11 & 12: Quality Assurance & Performance** — Run all unit tests, verify zero-recording invariants, and validate release artifacts.
11. **Phase 13: Final Report** — Generate `PEERLIGHT_RELEASE_REPORT.md` and complete handoff.
