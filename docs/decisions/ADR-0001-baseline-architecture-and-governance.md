# ADR-0001: Baseline Architecture, Governance, and Platform Constraints

## Status
Accepted

## Context
The project objective is to build `[PROJECT_NAME]`, a cross-platform remote device viewing and control system operating on local area networks (LAN-first). The system must bridge Windows desktop/laptop systems, Android mobile phones, and Google TV / Android TV endpoints.

Before starting implementation, clear architectural boundaries, platform constraints, and governance standards must be established to prevent technical debt, security regressions, and scope violations.

## Decisions

### 1. Placeholder Naming
The product name remains undecided. The identifier `[PROJECT_NAME]` is mandated across all documentation, metadata, packages, code namespaces, and UI until official branding is approved.

### 2. LAN-First Architecture
The system connects directly over trusted local networks (mDNS discovery + direct TCP/UDP control and media transport). No cloud accounts, external relays, or internet connections are required for LAN operation. WAN connectivity is deferred to Phase 8 and must use a dedicated VPN or end-to-end encrypted relay.

### 3. Strict Non-Negotiable Privacy Constraint: No Screen Recording
The application provides real-time live viewing and interaction only.
- In-memory frame buffers are bounded and discarded immediately upon transmission/rendering.
- Frame buffers are never written to disk, persistent caches, or local video stores.
- Automated screenshotting, video recording, session replays, and background monitoring are strictly prohibited.
- Diagnostic logs must never collect screen pixels, user input, keystrokes, or sensitive credentials.

### 4. No ADB Runtime Dependency
Ordinary end-users must not be required to enable developer options, install Android Debug Bridge (ADB), or execute shell commands to use the application. All Android capabilities must operate via official, user-consented platform APIs (MediaProjection for capture, Accessibility Services for input injection where permitted).

### 5. Verified Hardware Baseline
Feasibility testing is anchored on identified target hardware:
- **Windows Host:** Windows 11 Build 26200+ (Interactive user desktop, .NET 8, Windows Graphics Capture).
- **Android Phone Endpoint/Controller:** Xiaomi Redmi Note 10 (`M2101K7AG`), Android 12 (API level 31).
- **Google TV Target:** Xiaomi Mi TV (`MiTV_MOSR2`), Android TV 11 (API level 30) on LAN at `192.168.254.104`.

### 6. Decoupled Media & Control Transports
- **Control Channel:** Separate TLS/encrypted TCP transport for versioned protocol messages, device discovery, authenticated pairing, discrete key/button input, and TV navigation.
- **Media Transport:** WebRTC evaluated as the primary real-time media pipeline for bounded, low-latency live streaming with hardware acceleration.

## Consequences
- Requires upfront feasibility gates (Phase 0) to validate platform permissions and input injection limits before committing to release promises.
- Guarantees user privacy, system integrity, and platform compliance.
