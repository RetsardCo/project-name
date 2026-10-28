# \[PROJECT_NAME\] --- Master TODO Checklist

**Document type:** Implementation backlog and release checklist\
**Project name:** Intentionally undecided; use `[PROJECT_NAME]` as a
placeholder\
**Status:** Planning baseline\
**Version:** 0.1\
**Last updated:** 2026-10-28

> Use this file as the working checklist. Check an item only when it is
> implemented and verified. Keep platform limitations and test evidence
> in the related task notes. Do not mark a task complete merely because
> code compiles.

------------------------------------------------------------------------

## 0. Project Governance and Scope

-   [x] Keep the product name undecided until naming and availability
    checks are completed (Universal placeholder `[PROJECT_NAME]`).
-   [x] Create a repository and define branch/commit conventions (`main`, `feature/*`, Conventional Commits in `CONTRIBUTING.md`).
-   [x] Add a README with project purpose, scope, current status, and
    links to the specification (`README.md`).
-   [x] Add a LICENSE after choosing the intended distribution model (`LICENSE` - MIT License).
-   [x] Add a security reporting contact/process before public
    distribution (`SECURITY.md`).
-   [x] Define issue labels: `platform`, `security`, `performance`,
    `bug`, `docs`, `blocked` (Created on GitHub repo).
-   [x] Define a decision log for architecture and platform decisions (`docs/decisions/ADR-0001-baseline-architecture-and-governance.md`).
-   [x] Keep the full product specification and this TODO list in
    version control (`docs/PROJECT_NAME_Project_Documentation.md` and `docs/PROJECT_NAME_Master_TODO.md`).
-   [x] Confirm the first supported Windows version (Windows 11 Build 26200+ confirmed on host).
-   [x] Confirm the Android minimum SDK and target SDK (Min SDK: 28, Target SDK: 34).
-   [x] Record the Android phone make/model and OS version used for
    baseline testing (Xiaomi Redmi Note 10 `M2101K7AG`, Android 12, API 31).
-   [x] Record the Google TV make/model, Android TV/Google TV version,
    and available developer settings (Xiaomi Mi TV `MiTV_MOSR2`, Android TV 11, API 30 at `192.168.254.104`).
-   [x] Define the first release as LAN-only (Direct subnet discovery and encrypted session, no cloud dependency).
-   [x] Explicitly exclude locked-screen Android control from the first
    release (Active unlocked user sessions only).
-   [x] Explicitly exclude recording, replay, and automatic screenshot
    storage (In-memory bounded frame buffers only, discarded on render/transmit).
-   [x] Explicitly exclude hidden monitoring and unauthorized access (Consent prompts, active indicators, local instant disconnect).
-   [x] Explicitly state that ADB is not a runtime requirement (Standard platform APIs only: MediaProjection & Accessibility).
-   [x] Define the one-controller-at-a-time policy for v1 (Single active controller per endpoint).
-   [x] Define which capabilities are required for the first usable
    release (LAN discovery, pairing, Android->Windows live view, Windows->Android control, Google TV remote navigation).

## 1. Phase 0 --- Feasibility and Compatibility Audit

### 1.1 Android live-view feasibility

-   [ ] Create a minimal Kotlin Android Studio test project.
-   [ ] Request and start Android's supported screen-capture consent
    flow.
-   [ ] Display captured frames locally without writing them to disk.
-   [ ] Confirm capture stops when the user stops sharing.
-   [ ] Confirm behavior when capture permission is denied.
-   [ ] Confirm behavior when capture permission is revoked.
-   [ ] Test app backgrounding and foreground-service requirements on
    the selected Android version.
-   [ ] Test portrait-to-landscape rotation.
-   [ ] Test screen resolution and density changes.
-   [ ] Test whether the screen locks or turns off during a session and
    document expected behavior.
-   [ ] Verify protected surfaces and secure screens are not captured.
-   [ ] Measure baseline CPU, memory, battery, and frame rate.
-   [x] Write a short feasibility report with device and OS details (`docs/FEASIBILITY_REPORT.md`).

### 1.2 Android input feasibility without ADB at runtime

-   [x] Define the exact intended input scope: tap, swipe, scroll,
    keyboard, Back/Home, and app navigation (`docs/FEASIBILITY_REPORT.md` & `docs/PROTOCOL_SPEC.md`).
-   [x] Investigate official Android input and accessibility
    capabilities for the target OS (`docs/decisions/ADR-0002-android-accessibility-input-injection.md`).
-   [ ] Build a separate Accessibility Service proof of concept only if
    appropriate to the use case and distribution policy.
-   [ ] Explain the requested Accessibility permission clearly in the
    test UI.
-   [ ] Test tapping and scrolling in ordinary apps.
-   [ ] Test keyboard input in text fields.
-   [ ] Test Back and Home behavior where permitted.
-   [ ] Test behavior on secure/protected apps and settings screens
    without attempting to bypass restrictions.
-   [ ] Verify behavior when Accessibility permission is revoked.
-   [ ] Verify whether the chosen approach can be distributed through
    the intended channel.
-   [x] Document actions that cannot be supported reliably (`docs/FEASIBILITY_REPORT.md`).
-   [x] Decide whether the product can meet its promise without ADB or
    privileged access (Yes, via `AccessibilityService.dispatchGesture`).
-   [x] Do not proceed with a claim of full Android control until this
    feasibility gate passes.

### 1.3 Windows feasibility

-   [x] Create a minimal Windows desktop application (`src/windows/ProjectName.Windows/`).
-   [ ] Test current interactive desktop capture using a supported
    Windows capture API.
-   [ ] Test displaying a live captured stream in a second window.
-   [ ] Test supported mouse and keyboard input delivery.
-   [ ] Verify operation in a normal, non-administrator user context.
-   [x] Document limitations around UAC, secure desktop, sign-in screen,
    and session switching (`docs/FEASIBILITY_REPORT.md`).
-   [ ] Test display scaling and multiple monitors.
-   [ ] Measure baseline CPU, GPU, memory, FPS, and capture latency.
-   [x] Decide whether Windows agent and controller initially ship as
    one application or separate processes (Single unified executable with mode toggle).

### 1.4 Google TV feasibility

-   [x] Record the exact TV model and OS version (`Xiaomi Mi TV 4A/4S MiTV_MOSR2`, Android TV 11, API 30).
-   [x] Check available supported remote-control mechanisms (Android TV Remote Service v2 on TLS 6466/6467).
-   [ ] Verify pairing and basic remote commands on the target TV.
-   [ ] Test Up, Down, Left, Right, Select, Back, and Home.
-   [ ] Test text entry and playback/volume commands if supported.
-   [ ] Verify behavior after TV sleep, restart, and network change.
-   [x] Determine whether screen viewing is possible using a supported
    method (Unsupported on TV in v1 due to lack of consent UI / HDCP DRM).
-   [x] Determine whether a custom TV companion app is needed (Not in v1; use native remote service).
-   [x] Do not assume code injection is possible, supported, or
    required.
-   [x] Do not bypass DRM or protected surfaces.
-   [x] Create a TV capability matrix with `supported`, `unsupported`,
    and `not tested` values (`docs/FEASIBILITY_REPORT.md`).
-   [x] Decide whether live TV screen viewing is a v1 requirement or a
    later capability (Deferred from v1; remote navigation only).

### 1.5 Feasibility exit gate

-   [ ] Android live view works with no recording.
-   [x] A viable Android remote-input approach is identified and its
    limits documented (`docs/FEASIBILITY_REPORT.md`).
-   [ ] Windows capture/control is validated in the intended session
    context.
-   [x] Google TV remote capability is validated on at least one named
    model, or TV integration is explicitly deferred (Deferred screen viewing, button navigation targeted).
-   [x] Architecture risks are documented before broad implementation
    begins (`docs/FEASIBILITY_REPORT.md`).

## 2. Phase 1 --- Architecture and Protocol

### 2.1 Repository and modules

-   [x] Create a top-level repository structure (`src/`, `tests/`, `docs/`).
-   [x] Add `docs/` for product, protocol, security, testing, and
    release documentation.
-   [x] Add a Windows desktop project (`src/windows/ProjectName.Windows/`).
-   [ ] Add an Android phone project.
-   [ ] Add a TV integration module or project only after feasibility
    review.
-   [x] Separate UI, session logic, transport, platform capture, and
    input adapters.
-   [ ] Pin dependencies and document supported versions.
-   [ ] Review third-party licenses before selecting libraries.
-   [ ] Add dependency vulnerability scanning.
-   [x] Define supported protocol versions and compatibility behavior (`docs/PROTOCOL_SPEC.md`).

### 2.2 Protocol schema

-   [x] Define a versioned device discovery record (`DiscoveryPayload`).
-   [x] Define endpoint identity and capability fields (`DeviceCapabilities.cs`).
-   [x] Define pairing messages (`PairRequestPayload`, `PairChallengePayload`, `PairConfirmPayload`, `PairResultPayload`).
-   [x] Define session start and stop messages (`SessionStartPayload`, `SessionAcceptPayload`, `SessionStopPayload`).
-   [x] Define view-only and view-and-control permission scopes (`SessionScope`).
-   [x] Define input messages for pointer movement and click (`PointerMovePayload`, `PointerButtonPayload`).
-   [x] Define key down and key up messages (`KeyPayload`).
-   [x] Define touch gesture messages where supported (`TouchGesturePayload`).
-   [x] Define TV navigation messages (`TvCommandPayload`).
-   [ ] Define media negotiation and codec capability messages.
-   [x] Define diagnostics and error messages (`DiagnosticsReportPayload`, `MessageTypes.Error`).
-   [ ] Define revoke and disconnect messages.
-   [x] Validate message sizes and ranges (64 KB ceiling, [0.0, 1.0] coordinate clamping in `CoordinateMapper`).
-   [x] Reject unknown mandatory fields and unsupported protocol
    versions safely (`ProtocolEnvelope.Validate`).
-   [x] Add protocol unit tests and malformed-message tests (26 unit tests in `ProtocolTests.cs`).

### 2.3 Transport selection

-   [ ] Prototype WebRTC video on Windows and Android.
-   [ ] Validate available WebRTC bindings and maintenance status.
-   [ ] Confirm encryption defaults and configuration.
-   [x] Define the control channel separately from the video path (`docs/PROTOCOL_SPEC.md`).
-   [x] Choose reliable delivery for key/button state transitions and
    session commands (TLS-encrypted TCP control channel with sequence numbering).
-   [x] Prevent pointer-motion queues from accumulating stale events (`CoordinateMapper` + direct dispatch).
-   [x] Define maximum media buffer depth (Bounded ring buffer max capacity 2 in `WindowsCaptureBuffer`).
-   [x] Add a clear transport abstraction so libraries can be replaced
    if needed.
-   [x] Document why the chosen transport was selected (`docs/decisions/ADR-0003-webrtc-media-transport-zero-recording.md`).

## 3. Phase 2 --- Device Discovery, Pairing, and Trust

### 3.1 LAN discovery

-   [ ] Implement DNS-SD/mDNS or a validated equivalent.
-   [ ] Advertise only the minimum required endpoint metadata.
-   [ ] Do not include secrets or pairing tokens in discovery records.
-   [ ] Add manual hostname/IP connection as a fallback.
-   [ ] Handle changing IP addresses.
-   [ ] Handle client isolation and failed discovery with helpful
    diagnostics.
-   [ ] Show device name, platform, online status, and capabilities.
-   [ ] Do not treat discovery as authentication.

### 3.2 Pairing

-   [ ] Generate a cryptographic device identity.
-   [ ] Design short-lived pairing codes or QR-based pairing.
-   [ ] Enforce code expiration.
-   [ ] Rate-limit failed pairing attempts.
-   [ ] Require visible confirmation from the device being controlled.
-   [ ] Bind trust to a cryptographic identity, not IP address alone.
-   [ ] Store credentials using Windows-protected storage and Android
    Keystore or equivalent.
-   [ ] Implement pair, unpair, and revoke flows.
-   [ ] Test expired codes and repeated failed attempts.
-   [ ] Test device identity changes.
-   [ ] Test revoked-device reconnect attempts.
-   [ ] Implement a "disconnect all sessions" control.
-   [ ] Document credential backup/recovery assumptions.

### 3.3 Session authorization

-   [ ] Require authentication before session setup.
-   [ ] Negotiate capabilities before showing control buttons.
-   [ ] Support view-only sessions.
-   [ ] Support view-and-control sessions only when granted.
-   [ ] Show the active peer and permission scope on the controlled
    endpoint.
-   [ ] Allow the controlled endpoint to stop a session immediately.
-   [ ] Invalidate session state on disconnect.
-   [ ] Reset held key/button state after disconnect.
-   [ ] Test simultaneous connection attempts and enforce the v1
    controller policy.

## 4. Phase 3 --- Android Agent

### 4.1 Application foundation

-   [ ] Build the Android app shell in Kotlin.
-   [ ] Add onboarding explaining live viewing and remote control.
-   [ ] Add a device identity screen.
-   [ ] Add LAN discovery and pairing UI.
-   [ ] Add a permission status screen.
-   [ ] Add active-session status and a prominent Stop button.
-   [ ] Handle Android lifecycle and configuration changes.
-   [ ] Provide clear permission-denied and unsupported-feature
    messages.

### 4.2 Live screen stream

-   [ ] Implement user-approved screen capture.
-   [ ] Keep frames in memory only for active streaming.
-   [ ] Ensure no frame is written to disk.
-   [ ] Implement a bounded frame pipeline.
-   [ ] Drop stale frames when necessary to preserve responsiveness.
-   [ ] Encode with hardware acceleration where supported.
-   [ ] Add a software fallback only if performance and battery are
    acceptable.
-   [ ] Negotiate resolution and frame rate.
-   [ ] Handle rotation and display-size changes.
-   [ ] Handle permission revocation and capture termination.
-   [ ] Stop capture and release encoder resources on disconnect.
-   [ ] Verify no capture continues after the session stops.
-   [ ] Add a stalled-stream signal to the controller protocol.
-   [ ] Measure capture-to-encode time and memory use.

### 4.3 Remote input

-   [ ] Implement the selected, approved input mechanism.
-   [ ] Provide clear explanations before requesting Accessibility
    permission, if used.
-   [ ] Never enable Accessibility silently.
-   [ ] Validate pointer-to-screen coordinate mapping.
-   [ ] Handle display rotation and scaling.
-   [ ] Implement supported tap and swipe gestures.
-   [ ] Implement keyboard input only where supported.
-   [ ] Validate Back/Home and navigation behavior.
-   [ ] Reject control when the session is view-only.
-   [ ] Stop accepting input after disconnect or revocation.
-   [ ] Test permission removal while a session is active.
-   [ ] Document unsupported apps and secure screens.

### 4.4 Android quality and privacy

-   [ ] Add a visible foreground notification when required.
-   [ ] Provide a one-tap stop action.
-   [ ] Do not implement locked-screen control in v1.
-   [ ] Do not add recording or replay.
-   [ ] Do not log screen contents or typed text.
-   [ ] Profile CPU, memory, battery, and thermal behavior.
-   [ ] Test 30-minute continuous live-view sessions.
-   [ ] Test low-memory conditions and app termination.

## 5. Phase 4 --- Windows Desktop Client and Endpoint

### 5.1 Desktop dashboard

-   [ ] Create the main device dashboard.
-   [ ] Show paired, online, offline, busy, permission-required, and
    unsupported states.
-   [ ] Add device pairing and revocation actions.
-   [ ] Add per-device capability indicators.
-   [ ] Add connection mode indicator: direct LAN, VPN, or relay when
    future WAN support exists.
-   [ ] Add settings for view-only default and video quality.
-   [ ] Add diagnostic status without collecting screen content.

### 5.2 Windows endpoint capture

-   [ ] Implement supported interactive desktop capture.
-   [ ] Ensure the endpoint captures only the intended user session.
-   [ ] Add hardware video encoding where supported.
-   [ ] Add codec capability negotiation.
-   [ ] Handle multiple monitors and monitor selection.
-   [ ] Handle DPI scaling and resolution changes.
-   [ ] Display an active remote-session indicator.
-   [ ] Add a setting to disable incoming control.
-   [ ] Handle UAC/secure desktop limitations safely.
-   [ ] Release capture/encoder resources after disconnect.
-   [ ] Verify no media is written to disk.

### 5.3 Windows input

-   [ ] Implement mouse movement and click.
-   [ ] Implement drag and scroll.
-   [ ] Implement key down/up.
-   [ ] Map remote coordinates correctly for scaling and multiple
    monitors.
-   [ ] Reset held keys/buttons after connection loss.
-   [ ] Reject unpaired or unauthorized input.
-   [ ] Respect view-only sessions.
-   [ ] Test keyboard layouts and modifier keys.
-   [ ] Test window focus changes.
-   [ ] Test session stop during a held key or drag.
-   [ ] Do not bypass secure desktop or sign-in protections.

### 5.4 Windows reliability

-   [ ] Recover from Wi-Fi changes.
-   [ ] Recover from viewer closure and endpoint restart.
-   [ ] Show a clear frozen-frame warning.
-   [ ] Prevent unbounded memory queues.
-   [ ] Test sleep/resume behavior.
-   [ ] Test standard-user operation.
-   [ ] Document any optional elevated feature separately rather than
    requiring elevation by default.

## 6. Phase 5 --- Android Controller

-   [ ] Display discovered and paired endpoints.
-   [ ] Add device detail and capability pages.
-   [ ] Implement secure session start/stop.
-   [ ] Render the Windows live stream.
-   [ ] Add touch-to-mouse mode for desktop control.
-   [ ] Add a virtual mouse/trackpad option.
-   [ ] Add keyboard input.
-   [ ] Add modifier keys and key-release cleanup.
-   [ ] Add pinch/zoom only if a well-defined mapping is useful.
-   [ ] Add connection-quality and FPS status.
-   [ ] Add reconnect and disconnect handling.
-   [ ] Ensure the phone does not save the remote screen.
-   [ ] Test different screen sizes and orientations.
-   [ ] Test backgrounding and return to the viewer.

## 7. Phase 6 --- Low-Latency Optimization

-   [ ] Define repeatable latency test scenarios.
-   [ ] Measure network RTT separately from end-to-end latency.
-   [ ] Measure capture, encode, transport, decode, render, and
    input-to-response times.
-   [ ] Add bounded frame buffers.
-   [ ] Prefer dropping stale frames over queueing them.
-   [ ] Test hardware encoder and decoder availability.
-   [ ] Add adaptive bitrate.
-   [ ] Add adaptive resolution.
-   [ ] Add adaptive frame rate.
-   [ ] Test 720p at 30 FPS on baseline hardware.
-   [ ] Test 1080p at 30 FPS where hardware permits.
-   [ ] Treat 1080p at 60 FPS as an optimization target, not a universal
    guarantee.
-   [ ] Test 2.4 GHz versus 5 GHz/6 GHz Wi-Fi where available.
-   [ ] Test Ethernet where available.
-   [ ] Test congested Wi-Fi and weak signal.
-   [ ] Add diagnostics for packet loss, jitter, FPS, bitrate, and
    stalled video.
-   [ ] Verify diagnostics do not contain screen pixels or typed
    content.
-   [ ] Record test device models, OS versions, and network topology.

## 8. Phase 7 --- Google TV Integration

### 8.1 Remote protocol

-   [ ] Implement TV discovery or manual TV registration.
-   [ ] Confirm the target TV's remote protocol and pairing
    requirements.
-   [ ] Implement directional controls.
-   [ ] Implement Select/OK.
-   [ ] Implement Back and Home.
-   [ ] Implement text entry if supported.
-   [ ] Implement playback/volume commands only where supported.
-   [ ] Handle TV sleep and reconnect.
-   [ ] Handle protocol incompatibility with a clear message.
-   [ ] Never assume remote commands imply screen-view capability.

### 8.2 Optional TV companion app

-   [ ] Decide whether the TV needs a dedicated app based on feasibility
    results.
-   [ ] Confirm the app can be installed and supported on the target
    model.
-   [ ] Verify the supported APIs for background execution and network
    access.
-   [ ] Test live capture only through supported platform capabilities.
-   [ ] Verify protected video behavior and document limitations.
-   [ ] Profile TV memory and CPU.
-   [ ] Provide visible session state and a stop mechanism where the
    platform permits.
-   [ ] Avoid code injection or attempts to bypass TV security
    restrictions.
-   [ ] If screen streaming is unsupported, ship remote navigation
    separately rather than blocking the whole TV integration.

## 9. Phase 8 --- Security and Privacy Hardening

-   [ ] Write a threat model before WAN work.
-   [ ] Review all network listeners and bound interfaces.
-   [ ] Ensure the product does not expose ADB.
-   [ ] Ensure no remote-control port is publicly exposed by default.
-   [ ] Verify all sessions authenticate peers.
-   [ ] Verify control and media traffic are encrypted.
-   [ ] Verify pairing codes expire and rate limits work.
-   [ ] Verify revoked devices cannot reconnect.
-   [ ] Test replayed commands.
-   [ ] Fuzz protocol parsing and test malformed messages.
-   [ ] Test oversized messages and resource-exhaustion behavior.
-   [ ] Audit credential storage.
-   [ ] Audit logs for secrets, keystrokes, and screen data.
-   [ ] Confirm no recording or automatic screenshot path exists.
-   [ ] Confirm temporary media buffers are cleared/released on stop.
-   [ ] Verify permission revocation stops capture and input.
-   [ ] Add a privacy/security section to user documentation.
-   [ ] Perform dependency and license audits.
-   [ ] Fix all critical and high-severity findings before release.

## 10. Phase 9 --- WAN (Future, Not Initial Release)

-   [ ] Keep WAN disabled until the security review is approved.
-   [ ] Decide between private VPN, secure relay, or both.
-   [ ] Document metadata visible to a relay operator.
-   [ ] Define endpoint identity and key recovery.
-   [ ] Define session expiration and refresh.
-   [ ] Define revocation behavior for offline endpoints.
-   [ ] Add relay authentication and abuse/rate controls if a relay is
    used.
-   [ ] Add relay failover only if it does not weaken security.
-   [ ] Avoid raw inbound port exposure.
-   [ ] Test NAT and firewall cases.
-   [ ] Test direct LAN behavior remains available without internet.
-   [ ] Clearly label LAN, VPN, and relay connections.
-   [ ] Conduct a separate WAN security review.
-   [ ] Add WAN documentation only after tests pass.

## 11. Phase 10 --- Testing and Release

### 11.1 Automated tests

-   [ ] Unit tests for protocol parsing.
-   [ ] Unit tests for pairing expiry and revocation.
-   [ ] Unit tests for authorization scopes.
-   [ ] Unit tests for input coordinate transforms.
-   [ ] Unit tests for session state transitions.
-   [ ] Unit tests for disconnect cleanup.
-   [ ] Integration tests for Windows-to-Android live view.
-   [ ] Integration tests for Android-to-Windows control.
-   [ ] Integration tests for Google TV commands on named models.
-   [ ] Integration tests for LAN with internet disabled.
-   [ ] Integration tests for Wi-Fi loss/recovery.
-   [ ] Integration tests for permission denial/revocation.
-   [ ] Security tests for unauthorized peers and replayed messages.
-   [ ] Performance tests for FPS, latency, memory, and CPU.

### 11.2 Manual test matrix

-   [ ] Pair new device.
-   [ ] Reconnect paired device.
-   [ ] Revoke paired device.
-   [ ] Attempt connection from an unknown device.
-   [ ] Deny screen-capture permission.
-   [ ] Stop screen capture during an active session.
-   [ ] Disconnect Wi-Fi during input.
-   [ ] Verify no stuck key or mouse button remains.
-   [ ] Rotate the Android phone.
-   [ ] Change Windows display scaling.
-   [ ] Restart the Windows endpoint.
-   [ ] Restart the phone.
-   [ ] Restart the TV.
-   [ ] Test TV without screen-view capability.
-   [ ] Test protected video surface behavior without attempting to
    bypass protection.
-   [ ] Run a 30-minute session.
-   [ ] Confirm no recording file or automatic screenshot is produced.
-   [ ] Confirm no cloud service is required for a direct LAN session.
-   [ ] Confirm WAN is not reachable by default.

### 11.3 Packaging and documentation

-   [ ] Add installation instructions for Windows.
-   [ ] Add installation instructions for Android.
-   [ ] Add TV setup instructions only for validated models.
-   [ ] Explain required Android permissions and their purpose.
-   [ ] Explain how to stop a session and revoke a device.
-   [ ] Explain LAN router/client-isolation troubleshooting.
-   [ ] Publish supported OS/device matrix.
-   [ ] Publish known limitations.
-   [ ] Publish measured performance results and test conditions.
-   [ ] Sign distributable binaries where practical.
-   [ ] Verify clean installation and uninstall behavior.
-   [ ] Verify uninstall removes local credentials according to the
    documented policy.
-   [ ] Prepare release notes.
-   [ ] Tag the first release only after acceptance criteria pass.

## 12. Release Acceptance Checklist

-   [ ] Secure pairing works on a trusted LAN.
-   [ ] Direct LAN session works with internet disabled.
-   [ ] Live viewing works without recording or persistent media
    storage.
-   [ ] The Android user approves screen capture.
-   [ ] Android input works only through the validated, explicitly
    authorized mechanism.
-   [ ] Windows can be viewed and controlled in the supported
    interactive session.
-   [ ] Google TV navigation works on at least one named, tested model,
    or TV support is clearly marked as deferred/limited.
-   [ ] Unknown devices cannot view or control endpoints.
-   [ ] Revoked devices cannot reconnect.
-   [ ] Disconnect stops video and input.
-   [ ] Permission revocation stops capture.
-   [ ] Frozen streams are clearly identified.
-   [ ] Latency and memory have been measured on real hardware.
-   [ ] No recording, replay, or automatic screenshot feature exists.
-   [ ] Locked-screen Android control is still out of scope.
-   [ ] WAN is disabled unless the separate WAN milestone and security
    review have passed.
-   [ ] User-facing documentation describes platform limitations
    accurately.

------------------------------------------------------------------------

## 13. Suggested First 10 Tasks

Complete these in order before building the full product:

1.  [ ] Record the phone model and Android version.
2.  [ ] Record the Google TV model and OS version.
3.  [ ] Choose the minimum Windows version.
4.  [ ] Prove Android live capture with explicit consent and no file
    output.
5.  [ ] Prove the desired Android input mechanism without ADB at
    runtime.
6.  [ ] Prove Windows screen capture and input in a normal user session.
7.  [ ] Prove TV remote navigation on the actual TV.
8.  [ ] Decide whether TV screen viewing is feasible and required for
    v1.
9.  [ ] Select the media transport after a small WebRTC prototype.
10. [ ] Freeze the v1 capability matrix and then implement pairing and
    secure LAN sessions.

## 14. Definition of Completion

A checkbox is complete only when: - code or configuration exists; - the
feature has been tested on the stated target platform; - known
limitations are documented; - security and privacy behavior has been
checked where applicable; and - evidence (test output, a short report,
or a reproducible manual procedure) is recorded in the issue or project
notes.
