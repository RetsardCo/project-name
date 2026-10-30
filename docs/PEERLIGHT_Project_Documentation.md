# \[PROJECT_NAME\] --- Product Requirements and Technical Design

**Document type:** Product requirements, architecture, security, and
implementation specification\
**Project name:** Intentionally undecided; use `Peerlight` as a
placeholder\
**Document status:** Initial baseline for review; not yet
implementation-approved\
**Version:** 0.1\
**Last updated:** 2026-10-28

------------------------------------------------------------------------

## 1. Executive Summary

`Peerlight` is a cross-platform remote device viewing and control
system. It is intended to let an authorized user view a device's live
screen and interact with it from another authorized device. Initial
target endpoints are:

-   **Windows desktop/laptop:** controller and remotely controlled
    endpoint.
-   **Android phone:** controller and remotely controlled endpoint.
-   **Google TV / Android TV:** remote-control target first; optional
    companion TV app and screen viewing later if device capabilities
    permit.

The product is **LAN-first**: devices on a trusted local network should
connect directly whenever possible. Optional WAN access may be added
later through a private VPN or a carefully designed secure relay. The
product must not require a cloud account or internet connection for a
direct LAN session.

The product is for **live viewing and interaction, not screen
recording**. Recording, screenshots saved automatically, session replay,
and persistent video archives are out of scope. Temporary in-memory
frames needed to transport a live stream are permitted, but they must be
discarded after delivery and must not be written to disk.

The first release focuses on an unlocked Android phone, Windows desktop
control, and Google TV navigation. Locked-screen phone control is
explicitly deferred. The system must use explicit user authorization and
supported operating-system capabilities. ADB may be used during
development or optional advanced setup, but must not be a runtime
dependency for the ordinary product experience.

### 1.1 Product principles

1.  **Permission before control:** No device can be viewed or controlled
    until its owner authorizes pairing and the required OS permissions.
2.  **LAN first:** Prefer direct local connections and avoid unnecessary
    internet/cloud dependencies.
3.  **No recording:** No recording feature, automatic screenshots, or
    persistent screen-video storage.
4.  **Capability honesty:** The UI must state what each endpoint can and
    cannot do. Do not promise identical control on all platforms.
5.  **Low latency over unnecessary complexity:** Keep capture, encoding,
    transport, decoding, and input paths short.
6.  **Secure by default:** Authenticate peers, encrypt traffic, minimize
    exposed services, and provide an obvious disconnect/revoke path.
7.  **Incremental delivery:** Validate one platform and capability at a
    time before adding WAN, TV streaming, or advanced features.

------------------------------------------------------------------------

## 2. Product Scope

### 2.1 In scope

-   Windows desktop application.
-   Android phone application.
-   Google TV / Android TV integration.
-   Device discovery and pairing on a local network.
-   Live screen viewing where supported.
-   Remote input: pointer/touch gestures, keyboard, navigation buttons,
    and media controls where supported.
-   Direct LAN sessions without cloud relay.
-   Capability discovery and per-device status.
-   Explicit consent and OS permission flows.
-   Secure session setup, disconnect, revoke, and audit-friendly local
    status.
-   Adaptive video quality and frame rate.
-   Diagnostics for connection, latency, frame rate, and permission
    state.
-   Optional WAN architecture in a later phase.
-   Future possibility of a TV companion application if built-in remote
    APIs are insufficient.

### 2.2 Out of scope for initial release

-   Control while the phone is locked or the display is off.
-   Screen recording, session recording, replay, or video archive.
-   Silent or hidden monitoring.
-   Bypassing OS security prompts, DRM, secure windows, or protected
    content restrictions.
-   Rooting devices or exploiting firmware vulnerabilities.
-   Unattended remote access without visible, revocable authorization.
-   Publicly exposing ADB or device-control ports.
-   Guaranteed screen capture of every streaming application or
    protected media surface.
-   Guaranteed full control of every Google TV model.
-   Multi-user enterprise management, central fleet administration, and
    role-based corporate policy in v1.
-   File transfer unless separately approved for a later release.

### 2.3 Terminology

-   **Controller:** Device from which the user views or controls another
    device.
-   **Agent/endpoint:** Device that provides a screen stream, accepts
    supported input, or exposes remote commands.
-   **Pairing:** Explicit process that establishes trust between two
    devices.
-   **Session:** Authenticated period of live viewing and/or control.
-   **LAN:** Local network where endpoints can communicate directly.
-   **WAN:** Remote connection across different networks.
-   **Control channel:** Low-volume messages for input and commands.
-   **Media channel:** Live screen video and, if explicitly added later,
    audio.
-   **Capability:** A supported feature exposed by an endpoint, such as
    `view_screen`, `touch_input`, or `tv_navigation`.

------------------------------------------------------------------------

## 3. Users and Primary Scenarios

### 3.1 Primary user

A single user who owns or administers their own Windows laptop, Android
phone, and Google TV, and wants to use one device to view/control
another on the same network.

### 3.2 Core scenarios

**Scenario A --- Laptop controls phone**

1.  User opens the desktop client.
2.  The client discovers a previously paired Android phone or lets the
    user pair it.
3.  User selects "View and control."
4.  The phone shows or has already received the required OS permission
    for screen capture.
5.  The user approves the session.
6.  The laptop displays a live stream and sends supported input.
7.  User disconnects; streaming and input stop immediately.

**Scenario B --- Phone controls laptop**

1.  User opens the Android client.
2.  It discovers the Windows agent on the LAN.
3.  User authenticates and starts a session.
4.  The phone displays the live desktop and provides mouse, keyboard,
    and touch-to-pointer controls.
5.  The Windows agent accepts input only in the authorized interactive
    session and within the current user's permitted desktop/session
    context.
6.  User disconnects.

**Scenario C --- Phone or laptop controls Google TV**

1.  Controller discovers or manually adds a compatible TV.
2.  It connects using a supported TV remote protocol or companion
    integration.
3.  User navigates the TV interface using a directional pad, Back, Home,
    Select, and supported playback/volume commands.
4.  Live TV screen viewing is shown only if the model and supported
    software permit it.
5.  If the TV cannot provide a supported screen stream, the UI must
    still offer remote commands and clearly mark screen viewing as
    unsupported.

**Scenario D --- LAN with no internet**

1.  All endpoints connect to a trusted router/access point.
2.  The router provides local addressing but has no internet access.
3.  Device discovery, pairing (if already possible locally), and direct
    LAN sessions continue to work.
4.  No mandatory cloud authentication or cloud relay is attempted.

**Scenario E --- WAN access, future phase**

1.  User connects devices through a private VPN or an approved relay.
2.  The system authenticates the peer and establishes encrypted
    media/control sessions.
3.  WAN is disabled by default until the security review and threat
    model are complete.
4.  No inbound router port forwarding is required by the default design.

------------------------------------------------------------------------

## 4. Functional Requirements

Priority notation: **P0** = required for the first usable release;
**P1** = next; **P2** = future.

### 4.1 Device discovery and identity

-   **FR-DISC-001 (P0):** Discover compatible endpoints on the local
    network using an appropriate local discovery mechanism, such as
    DNS-SD/mDNS, with manual IP/hostname entry as a fallback.
-   **FR-DISC-002 (P0):** Do not treat discovery as authentication. A
    discovered device is untrusted until paired.
-   **FR-DISC-003 (P0):** Display device name, platform, online status,
    advertised capabilities, and last-seen time.
-   **FR-DISC-004 (P0):** Permit a user to rename a paired device
    locally.
-   **FR-DISC-005 (P0):** Provide an explicit revoke/unpair action that
    invalidates the local trust relationship.
-   **FR-DISC-006 (P1):** Handle network changes and changing IP
    addresses without treating IP address alone as identity.

### 4.2 Pairing and authorization

-   **FR-AUTH-001 (P0):** Pairing requires explicit user action on both
    sides or a secure, clearly displayed confirmation process.
-   **FR-AUTH-002 (P0):** Use a short-lived pairing code, QR code, or
    equivalent authenticated key exchange; codes must expire and resist
    guessing.
-   **FR-AUTH-003 (P0):** Persist only the minimum credentials needed to
    remember paired devices, using platform secure storage.
-   **FR-AUTH-004 (P0):** Each session must authenticate the peer, not
    merely trust its IP address.
-   **FR-AUTH-005 (P0):** Show an obvious active-session indicator on
    the controlled device when the platform allows it.
-   **FR-AUTH-006 (P0):** The controlled device can terminate a session
    immediately.
-   **FR-AUTH-007 (P1):** Offer separate view-only and view-plus-control
    permissions.
-   **FR-AUTH-008 (P1):** Support per-device revocation and a
    "disconnect all sessions" action.
-   **FR-AUTH-009 (P2):** Add time-limited unattended access only after
    a dedicated threat-model review; not part of initial scope.

### 4.3 Live screen viewing

-   **FR-VIEW-001 (P0):** Stream the current screen live where the
    operating system and endpoint permit it.
-   **FR-VIEW-002 (P0):** Do not implement recording or persist media
    frames to disk.
-   **FR-VIEW-003 (P0):** Use bounded in-memory buffers and discard
    stale frames to prevent latency accumulation.
-   **FR-VIEW-004 (P0):** Display connection status, current resolution,
    approximate frame rate, and stream quality where available.
-   **FR-VIEW-005 (P0):** Allow the controller to request a lower
    resolution or frame rate.
-   **FR-VIEW-006 (P0):** Respect OS screen-capture consent and
    secure/protected surfaces.
-   **FR-VIEW-007 (P0):** Clearly indicate when screen viewing is
    unavailable, denied, paused, or blocked by the OS.
-   **FR-VIEW-008 (P1):** Adapt bitrate and frame rate based on packet
    loss, congestion, and decoder performance.
-   **FR-VIEW-009 (P1):** Support reconnecting the media session without
    requiring a full application restart.
-   **FR-VIEW-010 (P2):** Consider audio only after video and input work
    reliably and only with explicit scope/consent.

### 4.4 Remote input and commands

-   **FR-INPUT-001 (P0):** Send mouse movement, click, drag, and
    keyboard input to supported Windows endpoints.
-   **FR-INPUT-002 (P0):** Send supported touch gestures and keyboard
    input to Android endpoints through a clearly disclosed,
    user-authorized mechanism.
-   **FR-INPUT-003 (P0):** Support Google TV navigation commands such as
    Up, Down, Left, Right, Select, Back, and Home where the TV protocol
    permits them.
-   **FR-INPUT-004 (P0):** Map controller coordinates to the displayed
    remote screen resolution and account for rotation/scaling.
-   **FR-INPUT-005 (P0):** Handle press/release semantics to avoid stuck
    keys or buttons if a connection drops.
-   **FR-INPUT-006 (P0):** Reject input from unauthenticated or unpaired
    peers.
-   **FR-INPUT-007 (P0):** Provide a prominent stop-control action.
-   **FR-INPUT-008 (P0):** Do not claim full control where the OS
    permits only navigation or limited actions.
-   **FR-INPUT-009 (P1):** Add configurable keyboard mappings and
    accessibility-friendly controls.
-   **FR-INPUT-010 (P2):** Consider clipboard or file transfer
    separately; neither is implied by remote input.

### 4.5 Windows endpoint

-   **FR-WIN-001 (P0):** Run as a normal user process by default; do not
    require administrator privileges for ordinary viewing/control.
-   **FR-WIN-002 (P0):** Capture and control only the supported
    interactive desktop/session.
-   **FR-WIN-003 (P0):** Do not attempt to bypass Windows secure
    desktop, UAC prompts, login screen protections, or session
    isolation.
-   **FR-WIN-004 (P0):** Display a visible indicator while remote
    control is active.
-   **FR-WIN-005 (P0):** Provide a clear setting to disable incoming
    remote-control sessions.
-   **FR-WIN-006 (P1):** Handle monitor selection, display scaling,
    multi-monitor layouts, and resolution changes.
-   **FR-WIN-007 (P1):** Use hardware video encoding/decoding where
    available, with a tested fallback.
-   **FR-WIN-008 (P2):** Service-based startup or unattended access
    requires separate security review and is not required initially.

### 4.6 Android endpoint

-   **FR-AND-001 (P0):** Support current Android versions selected for
    the release; set the exact minimum SDK after a compatibility spike.
-   **FR-AND-002 (P0):** Use Android's supported screen-capture consent
    flow (for example, MediaProjection) for live viewing.
-   **FR-AND-003 (P0):** Explain that the OS may require the user to
    approve capture and may stop or revoke it.
-   **FR-AND-004 (P0):** Use a foreground service and visible
    notification where required by the target Android version and the
    chosen capture design.
-   **FR-AND-005 (P0):** For remote input, investigate Accessibility
    Service and other officially supported mechanisms; clearly disclose
    why permissions are requested.
-   **FR-AND-006 (P0):** Do not promise that an ordinary app can inject
    arbitrary touch input into all apps. Validate the exact interaction
    model on target devices.
-   **FR-AND-007 (P0):** Initial scope requires the phone to be unlocked
    and the capture session to be active.
-   **FR-AND-008 (P0):** Do not implement locked-screen control in the
    first release.
-   **FR-AND-009 (P0):** Handle orientation, display-size, and app
    foreground/background changes.
-   **FR-AND-010 (P1):** Explain battery optimization and background
    execution constraints without misleading the user or encouraging
    blanket disabling of safeguards.
-   **FR-AND-011 (P1):** Keep the active capture state visible and
    provide a one-tap stop action.

### 4.7 Google TV / Android TV endpoint

-   **FR-TV-001 (P0):** Determine whether the specific model supports a
    documented or compatible remote-control protocol.
-   **FR-TV-002 (P0):** Implement navigation controls through a
    supported protocol where feasible.
-   **FR-TV-003 (P0):** Treat remote-button control and screen capture
    as separate capabilities.
-   **FR-TV-004 (P0):** Do not require injecting code into the TV as the
    default approach.
-   **FR-TV-005 (P0):** Do not assume all TV models support a custom
    app, background agent, or remote screen capture.
-   **FR-TV-006 (P0):** If live screen viewing is unsupported, retain
    supported remote controls and show a clear capability limitation.
-   **FR-TV-007 (P1):** Evaluate a TV companion app if it provides a
    supported path to additional capabilities.
-   **FR-TV-008 (P1):** Verify behavior with protected video content; do
    not attempt to defeat DRM or protected-surface restrictions.
-   **FR-TV-009 (P2):** Consider audio streaming or richer TV
    integrations only after device-specific validation.

### 4.8 WAN connectivity (future)

-   **FR-WAN-001 (P2):** WAN support must be opt-in and disabled until
    security review is complete.
-   **FR-WAN-002 (P2):** Prefer a private VPN or secure relay with
    end-to-end authenticated encryption.
-   **FR-WAN-003 (P2):** Do not expose ADB or raw control ports to the
    public internet.
-   **FR-WAN-004 (P2):** Use short-lived credentials, rate limits,
    revocation, and abuse protection for any relay.
-   **FR-WAN-005 (P2):** Avoid sending media through a relay when a
    secure direct path is available and policy allows it.
-   **FR-WAN-006 (P2):** Clearly distinguish local direct, VPN, and
    relay connections in the UI.

------------------------------------------------------------------------

## 5. Non-Functional Requirements

### 5.1 Latency and responsiveness

Targets below are initial engineering goals, not guarantees. Measure on
real hardware and document results.

-   **NFR-PERF-001:** LAN command transport target: p95 under 10 ms on
    an uncongested test network, excluding OS scheduling and target-app
    response.
-   **NFR-PERF-002:** LAN end-to-end screen interaction target: p95
    under 100 ms on selected reference hardware and a good
    Wi-Fi/Ethernet network.
-   **NFR-PERF-003:** Target 30 FPS at 720p for the initial baseline;
    1080p/60 FPS is an optimization goal, not a universal promise.
-   **NFR-PERF-004:** Bound media queues and prefer dropping stale
    frames over adding delay.
-   **NFR-PERF-005:** Report capture-to-display latency where
    measurable; do not equate network ping with end-to-end latency.
-   **NFR-PERF-006:** Prefer hardware encoding/decoding where supported
    and stable.
-   **NFR-PERF-007:** Use adaptive resolution, bitrate, and frame rate
    to prevent congestion from creating long queues.

### 5.2 Preliminary memory budgets

These figures are **planning estimates only, not measurements or
platform requirements**. Confirm with profiling on target devices;
actual RSS/working set and graphics buffers can differ materially.

  -----------------------------------------------------------------------
  Component / mode               Initial additional-memory planning range
  ------------------------------ ----------------------------------------
  TV remote commands only                                       20--60 MB

  TV companion agent with live                                 60--150 MB
  capture/streaming, if          
  supported                      

  Android phone agent with                                      30--80 MB
  commands only                  

  Android phone agent with live                                60--180 MB
  capture/streaming              

  Windows viewer/controller                                   100--300 MB

  Windows viewer with recording       Not applicable; recording is out of
                                                                    scope
  -----------------------------------------------------------------------

Do not use the last row as a planned feature. It is listed only to make
clear that recording is excluded. Memory must be measured with and
without hardware codecs and at different resolutions.

### 5.3 Reliability

-   **NFR-REL-001:** A dropped connection must stop input delivery and
    clear pressed-key/button state.
-   **NFR-REL-002:** Recover gracefully from Wi-Fi changes, endpoint
    sleep, app termination, and permission revocation.
-   **NFR-REL-003:** Do not silently reconnect to a different endpoint
    based only on an IP address.
-   **NFR-REL-004:** Display meaningful errors rather than leaving a
    frozen screen that looks live.
-   **NFR-REL-005:** The user can disconnect or revoke pairing at any
    time.

### 5.4 Usability and accessibility

-   Provide clear pairing, permission, connecting, connected,
    reconnecting, and disconnected states.
-   Distinguish view-only from view-and-control sessions.
-   Keep the stop/disconnect control visible.
-   Provide keyboard navigation and accessible labels.
-   Show unsupported capabilities before a user attempts to start them.
-   Avoid dark patterns that pressure users to grant Accessibility or
    screen-capture permissions.

### 5.5 Portability and maintainability

-   Keep transport/protocol logic separate from platform-specific
    capture and input adapters.
-   Define versioned protocol messages.
-   Use capability negotiation instead of assuming every endpoint
    supports every feature.
-   Maintain a documented compatibility matrix.
-   Keep platform agents small and minimize dependencies.

------------------------------------------------------------------------

## 6. Proposed Architecture

### 6.1 Logical components

1.  **Desktop controller/viewer (Windows):** Device dashboard, pairing
    UI, session management, video rendering, keyboard/mouse capture,
    settings, diagnostics.
2.  **Windows endpoint agent:** Captures the authorized interactive
    desktop, encodes frames, receives authenticated input, and displays
    session status.
3.  **Android app:** Device discovery, pairing, MediaProjection-based
    live capture where approved, video transport, supported input
    integration, visible session state.
4.  **TV integration adapter:** TV discovery and remote-button control
    using a supported protocol. Optional TV app only if required and
    feasible.
5.  **Session/security layer:** Peer identity, pairing, authenticated
    key establishment, session authorization, revocation.
6.  **Media transport:** WebRTC is the preferred initial candidate for
    real-time video because it supports low-latency media transport and
    encrypted sessions. Validate platform integration and complexity in
    a prototype.
7.  **Control transport:** A separate reliable or low-latency channel
    selected according to message semantics. Use reliable ordered
    delivery for session setup and discrete key/button transitions; use
    latest-value semantics or carefully bounded delivery for high-rate
    pointer movement.
8.  **Discovery:** DNS-SD/mDNS or equivalent on LAN, plus manual
    connection fallback.
9.  **Optional WAN layer (future):** Private VPN or secure relay. No
    mandatory cloud dependency for LAN.

### 6.2 Architecture sketch

``` text
                 Local dashboard / controller
              +-------------------------------+
              | Windows desktop client        |
              | Android controller UI         |
              +---------------+---------------+
                              |
                    Pairing + session layer
                              |
             +----------------+----------------+
             |                                 |
      Encrypted control channel          WebRTC media
             |                                 |
   +---------+----------+           +----------+----------+
   | Windows endpoint   |           | Android endpoint    |
   | capture + input    |           | capture + input     |
   +--------------------+           +---------------------+

   Google TV adapter:
   - supported remote protocol for buttons/navigation
   - optional companion app / screen stream only if validated
```

This is a logical diagram, not a commitment to a specific library or
final protocol layout.

### 6.3 Protocol separation

Use separate conceptual message categories:

-   **Discovery:** endpoint ID, display name, platform, protocol
    version, capability list, service endpoint.
-   **Pairing:** challenge, short-lived code, public-key exchange, user
    confirmation, result.
-   **Session:** session ID, requested permissions, negotiated
    capabilities, session expiry/renewal.
-   **Input:** pointer position, button down/up, key down/up, touch
    gesture, TV navigation command.
-   **Media negotiation:** codec capabilities, resolution, frame rate,
    bitrate limits, transport state.
-   **Diagnostics:** latency samples, packet loss, frame rate, decode
    errors, permission state.
-   **Disconnect/revoke:** reason, cleanup confirmation, token
    invalidation.

Never trust arbitrary fields received over the network. Validate message
sizes, ranges, message types, versions, and state transitions.

### 6.4 Suggested technology choices to evaluate

These are candidates to validate, not final decisions:

-   **Windows:** C#/.NET with WinUI 3 or another maintained desktop UI
    framework. Use Windows Graphics Capture or a supported capture API
    for the current interactive desktop; use supported input APIs.
    Investigate WebRTC bindings with active maintenance.
-   **Android:** Kotlin, Android Studio, MediaProjection for
    user-approved live capture, foreground service as required by the
    target API, and an explicitly disclosed input strategy. Evaluate
    WebRTC Android SDK.
-   **Google TV:** First test the built-in remote-control protocol and
    device compatibility. If needed, create a Kotlin TV companion app
    for supported Android TV versions. Screen capture feasibility must
    be proven on the exact target model.
-   **Transport:** WebRTC for video is a strong candidate; avoid writing
    a custom video transport before a proof of concept.
-   **Discovery:** DNS-SD/mDNS with manual address fallback.
-   **Secrets:** Windows credential protection and Android Keystore or
    equivalent platform-protected storage.
-   **Build/CI:** Version-controlled source, reproducible builds,
    automated unit tests, and dependency scanning.

Choose exact libraries only after checking maintenance status, license,
platform coverage, security history, and build complexity.

------------------------------------------------------------------------

## 7. Platform Constraints and Reality Checks

### 7.1 Android screen viewing is not the same as remote input

Android's screen-capture APIs require user consent and may be stopped or
revoked by the OS. Some protected surfaces may be blank or unavailable.
A live stream can be displayed without saving it, but the underlying
mechanism is still screen capture.

Remote input is a separate problem. An ordinary Android app cannot
assume it can inject arbitrary taps into all other apps. An
Accessibility Service may support some interactions but requires
explicit enablement and has policy and platform constraints. Other
methods may be limited to the app itself or require development/debug
privileges. The implementation must validate the precise input path on
real devices.

### 7.2 Windows capture/control limits

A Windows endpoint can generally capture and control its current
authorized interactive desktop, subject to permissions and session
state. Do not attempt to bypass secure desktop, UAC, sign-in
protections, or session isolation. Initial implementation should support
an unlocked, active user session only.

### 7.3 Google TV differences

TV remote navigation may be possible through a built-in remote
service/protocol without installing a custom app. Protocol availability
and pairing differ by model and OS version. Live TV screen capture is a
separate capability and cannot be assumed from remote-button support. A
custom TV app may help only where platform permissions and vendor
behavior allow it. Code injection is not the default solution and must
not be used to bypass security controls.

### 7.4 LAN does not guarantee connectivity

Devices on the same Wi-Fi name may still be isolated by guest mode,
client isolation, VLANs, firewall rules, or access-point configuration.
Discovery may fail even when manual IP connection works. The app needs
diagnostics and a manual fallback.

### 7.5 WAN needs a separate security design

NAT traversal is not guaranteed. A relay may be necessary, or the user
may use a private VPN. WAN must not be bolted on by opening arbitrary
ports. Threat model and security review are prerequisites.

------------------------------------------------------------------------

## 8. Security and Privacy Design

### 8.1 Threat model

Protect against:

-   Unpaired devices on the same Wi-Fi attempting to control an
    endpoint.
-   A malicious device impersonating a known IP address or display name.
-   Stolen pairing tokens.
-   Replay of control commands.
-   Unauthorized session continuation after revocation.
-   Malformed media/control messages causing crashes or resource
    exhaustion.
-   Accidental exposure of remote-control services to WAN.
-   A user believing a frozen frame is a live feed.
-   Unexpected screen capture or control after the user thinks a session
    ended.

### 8.2 Required controls

-   Mutual peer authentication after pairing.
-   Encrypted control and media transport.
-   Short-lived pairing codes with rate limiting.
-   Device identity based on cryptographic credentials, not IP alone.
-   Session authorization scoped to view-only or view-and-control.
-   Explicit session start and immediate disconnect.
-   No unauthenticated command endpoint.
-   Input state reset on disconnect.
-   Minimal local logs; never log screen pixels, typed content, or
    secrets.
-   No automatic media persistence.
-   Secure storage for credentials.
-   Dependency updates and vulnerability review.
-   Safe defaults: WAN disabled, remote control disabled until paired,
    and no auto-accept for unknown devices.

### 8.3 Privacy behavior

-   Live frames exist only in bounded memory buffers for
    transmission/rendering.
-   No recording, automatic screenshots, replay, or media archive.
-   Diagnostic logs must not contain frame content, passwords, keyboard
    text, or sensitive UI content.
-   The UI must identify which device is being viewed and which
    permissions are active.
-   The controlled endpoint must have a clear way to stop
    capture/control.
-   Revocation must invalidate future sessions and clear locally stored
    trust credentials.

### 8.4 Security questions to resolve before WAN

-   Is the session peer-to-peer or relay-mediated?
-   Who operates any relay and what metadata can it see?
-   How are device identity keys backed up or recovered?
-   How does revocation propagate to an offline endpoint?
-   What is the policy for unattended sessions?
-   What are the rate limits and lockout behavior for pairing?
-   How are protocol upgrades and downgrade attacks handled?

------------------------------------------------------------------------

## 9. UI/UX Requirements

### 9.1 Main dashboard

-   Paired devices list with platform icon, name, connection type, and
    status.
-   "Add/pair device" action.
-   Capability badges such as `Live view`, `Remote input`, `TV remote`,
    `LAN`.
-   Clear distinction between online, offline, unsupported, permission
    required, and busy.
-   No misleading "connected" state until authentication and session
    negotiation succeed.

### 9.2 Session viewer

-   Large live view with correct aspect ratio and rotation.
-   View-only or view-and-control mode visibly indicated.
-   Mouse/keyboard input for desktop endpoints.
-   Touch/gesture mode for phone endpoints.
-   Directional pad for TV.
-   Resolution/FPS/connection quality display.
-   Stop/disconnect button always accessible.
-   Frozen-frame warning if media stops advancing.
-   Permission and unsupported-surface messages that explain the
    limitation.

### 9.3 Pairing flow

1.  User chooses "Pair device."
2.  App discovers a candidate or accepts a manual connection.
3.  Both devices show a matching short-lived code or confirmation.
4.  User confirms trust on the controlled device.
5.  Required OS permissions are requested with contextual explanation.
6.  Pairing completes and the new device appears in the dashboard.
7.  User can revoke it later from either side where possible.

### 9.4 Settings

-   LAN discovery toggle.
-   View-only default / allow remote input.
-   Video quality preset.
-   Hardware acceleration status.
-   Device rename and revoke.
-   Diagnostics export that excludes sensitive content.
-   WAN/VPN settings only when the future WAN phase is implemented.

------------------------------------------------------------------------

## 10. Data Model (Initial Proposal)

-   **DeviceIdentity:** cryptographic device ID, display name, platform,
    protocol version, public-key fingerprint, capabilities, last-seen
    timestamp.
-   **PairingRecord:** peer device ID, credential reference, permissions
    granted, creation date, revocation status.
-   **SessionState:** session ID, peer ID, view/control scope, start
    time, connection mode, media state, permission state.
-   **CapabilitySet:** supported capture, input, codecs, resolution
    range, TV commands, and known limitations.
-   **DiagnosticsSnapshot:** connection mode, RTT estimate, packet loss,
    FPS, resolution, bitrate, capture/encode/decode timing, error codes.

Avoid storing personal content. Store timestamps and connection
diagnostics only as needed. Session metadata retention should be
documented and configurable if persistence is introduced.

------------------------------------------------------------------------

## 11. Error Handling

The UI must distinguish at least:

-   Device not found.
-   Device found but not paired.
-   Pairing expired or failed.
-   Peer identity changed.
-   OS permission not granted.
-   Screen capture stopped or revoked.
-   Remote input unavailable.
-   TV remote protocol unsupported.
-   TV screen viewing unsupported.
-   Network client isolation suspected.
-   Peer unreachable.
-   Authentication failed.
-   Unsupported codec.
-   Video stalled.
-   Endpoint busy or session already active.
-   Session disconnected by remote endpoint.
-   Remote control disabled by owner.

Errors should suggest safe next steps and must not silently fall back to
insecure transport.

------------------------------------------------------------------------

## 12. Testing Strategy

### 12.1 Unit tests

-   Protocol parsing and schema validation.
-   Pairing code expiration and rate limits.
-   Authorization checks for view-only versus control.
-   Input mapping and screen-coordinate transforms.
-   Session state transitions.
-   Disconnect cleanup and key/button release.
-   Capability negotiation and version compatibility.
-   Bounded buffer behavior and stale-frame dropping.

### 12.2 Integration tests

-   Windows-to-Android live view over USB during development and over
    LAN for product validation.
-   Android-to-Windows view/control.
-   Google TV remote commands on each supported model.
-   Permission denial, revocation, and regrant.
-   Wi-Fi disconnect/reconnect and IP changes.
-   Endpoint restart during a session.
-   No-internet LAN operation.
-   Multiple paired devices and one active session.
-   Direct LAN traffic verification to ensure no mandatory cloud calls.

### 12.3 Security tests

-   Unpaired peer cannot send input.
-   Pairing code expires and rate-limits failures.
-   Replayed or malformed messages are rejected.
-   Revoked peer cannot reconnect.
-   No ADB/debug ports are exposed by the product.
-   No control listener is reachable from WAN by default.
-   Credential storage is protected by platform facilities.
-   Logs do not contain screen content or typed secrets.
-   Media frames are not written to disk.
-   Disconnect clears pressed keys and pending input.

### 12.4 Performance tests

Measure on named reference devices and network conditions:

-   Capture-to-display latency.
-   Input-to-visible-response latency.
-   p50/p95/p99 control-channel delay.
-   FPS and dropped frames.
-   CPU, GPU, battery, and memory use.
-   Bitrate under 720p and 1080p profiles.
-   Wi-Fi congestion and weak-signal behavior.
-   Ethernet versus 5 GHz Wi-Fi.
-   Cold start, reconnect time, and 30-minute stability.

Do not report a performance target as achieved until measurements are
captured with a repeatable test setup.

------------------------------------------------------------------------

## 13. Acceptance Criteria

### Initial usable release

-   A Windows client and Android app can pair securely on a trusted LAN.
-   The Android user explicitly grants screen-capture permission.
-   The Windows client displays a live Android screen without saving
    recordings.
-   A supported input mechanism has been validated on the selected
    Android devices; limitations are documented.
-   A Windows endpoint can be viewed and controlled from the Android
    client in an authorized interactive session.
-   Google TV navigation works on at least one specifically tested
    model, or the TV limitation is explicitly documented before claiming
    support.
-   LAN sessions work with internet disconnected.
-   Unknown peers cannot view or control devices.
-   Disconnect stops media and input promptly.
-   Revoked devices cannot start a new session.
-   The application clearly reports permissions, unsupported
    capabilities, and stalled video.
-   Locked-screen phone control remains unimplemented.
-   WAN access remains disabled until its phase is completed and
    reviewed.
-   No recording feature or persistent screen-video storage exists.

### Release quality bar

-   No known critical security issue.
-   No unbounded media queues.
-   No persistent capture after permission revocation or disconnect.
-   No silent fallback to an insecure protocol.
-   Compatibility matrix and test results are included in the release
    notes.
-   RAM and latency measurements are collected from real hardware.

------------------------------------------------------------------------

## 14. Risks and Mitigations

  -----------------------------------------------------------------------
  Risk                    Impact                  Mitigation
  ----------------------- ----------------------- -----------------------
  Android blocks          Full phone control may  Prototype supported
  arbitrary remote input  not be possible through Accessibility/API
                          a normal app alone      options early; define
                                                  exact supported control
                                                  level

  Android capture consent Session interruption or Test target API levels
  or service behavior     compatibility issues    and devices; handle
  changes                                         revocation visibly

  TV model does not       TV can be controlled    Separate TV remote and
  expose screen capture   but not viewed          TV screen-view
                                                  capabilities; optional
                                                  companion app only if
                                                  viable

  Protected video is      Streaming apps may not  Respect DRM; disclose
  blank in stream         mirror                  limitation; do not
                                                  bypass protections

  LAN client isolation    Devices cannot          Add manual address,
                          discover/connect        diagnostics, and router
                                                  guidance

  High video latency      Poor usability          Hardware codecs,
                                                  bounded buffers,
                                                  adaptive bitrate,
                                                  measurement

  WAN adds attack surface Unauthorized access     Defer WAN; use
                          risk                    VPN/secure relay,
                                                  authenticated
                                                  encryption, and
                                                  security review

  Third-party WebRTC      Build or maintenance    Prove the stack early;
  dependency complexity   burden                  pin versions and audit
                                                  licenses

  User mistakes frozen    Privacy and control     Show frame freshness
  image for live view     confusion               and stalled-stream
                                                  warning

  Excessive               Phone or TV performance Profile, adapt
  battery/memory use      degradation             FPS/resolution, stop
                                                  resources on disconnect
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 15. Milestone Plan

### Milestone 0 --- Requirements and feasibility

-   Freeze v1 scope and supported reference devices.
-   Choose Windows and Android target versions.
-   Identify the exact Google TV model and version.
-   Test Android capture and input feasibility without ADB as a runtime
    requirement.
-   Test TV remote protocol compatibility.
-   Select the media transport after a proof of concept.
-   Document unresolved platform limitations.

**Exit gate:** The team can demonstrate the required core capabilities
on selected devices, or explicitly reduce the supported feature set.

### Milestone 1 --- Local protocol and pairing prototype

-   Create device identity and protocol schema.
-   Implement pairing, mutual authentication, and local discovery.
-   Add a minimal connection status UI.
-   Validate no-internet LAN connectivity.
-   Add disconnect and revoke.

**Exit gate:** Only paired devices can establish a session.

### Milestone 2 --- Android to Windows live viewing

-   Implement Android capture consent and lifecycle.
-   Stream to Windows.
-   Display the stream with a stalled-frame warning.
-   Measure latency, FPS, CPU, memory, and battery.

**Exit gate:** Stable live viewing without recording.

### Milestone 3 --- Remote input

-   Implement Windows mouse/keyboard input.
-   Validate Android remote input strategy on target phone models.
-   Add view-only and control permissions.
-   Test disconnect cleanup and permission revocation.

**Exit gate:** Supported input works reliably and unsupported actions
are clearly communicated.

### Milestone 4 --- Windows endpoint and phone controller

-   Implement Windows capture and session agent.
-   Add Android controller UI.
-   Test phone-to-laptop sessions and active-session indicators.

**Exit gate:** Both directions work for the selected endpoint pair.

### Milestone 5 --- Google TV remote integration

-   Implement compatible TV discovery/pairing.
-   Add directional, Back, Home, Select, and supported media controls.
-   Test compatibility and reconnection.
-   Evaluate optional TV companion app and live TV screen viewing.

**Exit gate:** TV capabilities are accurately represented and validated
on named models.

### Milestone 6 --- Hardening and LAN release

-   Security review.
-   Performance and memory profiling.
-   Recovery and network-change tests.
-   Package/sign installers.
-   Documentation, known limitations, and release checklist.

**Exit gate:** Acceptance criteria pass.

### Milestone 7 --- Optional WAN

-   Threat model and architecture review.
-   Decide private VPN versus secure relay.
-   Implement identity, session expiry, revocation, abuse controls, and
    secure transport.
-   Conduct security and NAT/connectivity testing.
-   Keep WAN opt-in.

**Exit gate:** WAN passes a separate security review; it is not
automatically included in the LAN release.

------------------------------------------------------------------------

## 16. Decisions Still Open

These must be resolved before implementation is treated as stable:

1.  Exact minimum Windows version.
2.  Exact minimum Android version and supported phone models.
3.  Exact Google TV make/model and OS version.
4.  Whether the first release must support arbitrary Android touch
    injection or a narrower supported set.
5.  WebRTC library and packaging approach for Windows.
6.  Local pairing UX and cryptographic identity lifecycle.
7.  One-controller-at-a-time versus multi-controller policy.
8.  Whether session metadata persists locally and for how long.
9.  Whether the future WAN path uses a VPN, relay, or both.
10. Whether the TV companion app is necessary after testing built-in
    remote control.

### 16.1 Initial defaults recommended

-   Product name remains undecided.
-   Windows + Android are the first full remote-view/control pair.
-   Google TV remote navigation follows as a separate capability.
-   LAN only in the first release.
-   No recording, no replay, no automatic screenshots.
-   No ADB requirement at runtime.
-   Unlocked-screen Android sessions only.
-   Explicit pairing and user-approved permissions.
-   No code injection into the TV.
-   No WAN listeners or public port exposure.
-   Capability negotiation and transparent limitations are mandatory.

------------------------------------------------------------------------

## 17. Official References to Verify During Implementation

These references are starting points; verify current requirements for
the exact target OS versions before implementation.

-   Android Debug Bridge (development tool only):
    https://developer.android.com/tools/adb
-   Android MediaProjection / screen capture:
    https://developer.android.com/media/grow/media-projection
-   Android Accessibility services:
    https://developer.android.com/guide/topics/ui/accessibility/service
-   Android foreground services:
    https://developer.android.com/develop/background-work/services/foreground-services
-   WebRTC documentation: https://webrtc.org/
-   Windows Graphics Capture:
    https://learn.microsoft.com/windows/uwp/audio-video-camera/screen-capture
-   Windows SendInput:
    https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-sendinput
-   Android TV developer documentation: https://developer.android.com/tv
-   DNS-SD / mDNS overview (IETF RFC 6763):
    https://www.rfc-editor.org/rfc/rfc6763
-   Multicast DNS (IETF RFC 6762):
    https://www.rfc-editor.org/rfc/rfc6762

------------------------------------------------------------------------

## 18. Definition of Done for the Documentation Baseline

This document is ready to guide feasibility work when the team agrees
on:

-   v1 scope and non-goals;
-   reference Windows, Android, and TV devices;
-   capture and input feasibility;
-   LAN pairing and security model;
-   no-recording privacy requirements;
-   performance measurement plan;
-   phased acceptance criteria.

Do not begin broad implementation until the Android input feasibility
and Google TV capability checks are complete. These are the largest
platform-specific uncertainties.
