# [PROJECT_NAME] — Cross-Platform, LAN-First Remote Device Viewing & Control System

[![CI](https://github.com/RetsardCo/project-name/actions/workflows/ci.yml/badge.svg)](https://github.com/RetsardCo/project-name/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Status: Planning & Feasibility](https://img.shields.io/badge/Status-Feasibility%20%26%20Baseline-blue.svg)](docs/PROJECT_NAME_Master_TODO.md)

> **Notice:** The product name is intentionally undecided. `[PROJECT_NAME]` is used as the universal placeholder across code, documentation, package metadata, and UI until a final name is chosen.

---

## 1. Executive Summary

`[PROJECT_NAME]` is a cross-platform remote viewing and interaction system designed for trusted local area networks (LAN-first). It enables an authorized device (controller) to view and interact with another authorized device (endpoint) with low latency, mutual cryptographic authentication, and uncompromising user privacy.

### Supported Platforms:
- **Windows Desktop / Laptop:** Controller and interactive desktop endpoint.
- **Android Phone:** Controller and mobile endpoint (unlocked display sessions).
- **Google TV / Android TV:** Remote navigation control endpoint (d-pad, back, home, media).

---

## 2. Non-Negotiable Core Principles

1. **Strictly No Screen Recording:** The system provides live streaming only. It never implements video capture to disk, automated screenshots, session recording, or replay archives. Bounded in-memory frame buffers are used solely for transmission and discarded immediately.
2. **No ADB Runtime Dependency:** Android Debug Bridge (ADB) may be used for developer benchmarking or initial diagnostics, but ordinary users do not need ADB, root, or privileged access at runtime.
3. **Explicit Consent & Platform Integrity:** All sessions require mutual pairing confirmation. Screen capture operates via official OS consent prompts (`MediaProjection` on Android, `Windows.Graphics.Capture` on Windows). Secure desktop surfaces and DRM-protected windows are respected and never bypassed.
4. **LAN-First Architecture:** Devices connect directly on the local subnet without requiring cloud accounts, external relays, or internet connectivity. WAN access is strictly deferred to a later milestone under a dedicated security model.
5. **Immediate Local Revocation:** The controlled device maintains absolute authority at all times and can terminate an active session or revoke paired trust with one tap.

---

## 3. Verified Baseline Hardware

To ground implementation in reality rather than theoretical assumptions, initial feasibility and testing are anchored to the following hardware:

| Role | Device / Model | Operating System | Connection Mode |
| :--- | :--- | :--- | :--- |
| **Windows Host / Controller** | PC / Laptop Workstation | Windows 11 Build 26200+ | Local Subnet (Ethernet/Wi-Fi) |
| **Android Endpoint / Controller** | Xiaomi Redmi Note 10 (`M2101K7AG`) | Android 12 (API Level 31) | Local Wi-Fi (LAN) |
| **Google TV Target** | Xiaomi Mi TV (`MiTV_MOSR2`) | Android TV 11 (API Level 30) | Local LAN (`192.168.254.104`) |

---

## 4. Repository Structure

```
project-name/
├── .github/
│   └── workflows/ci.yml       # Automated CI build and test pipeline
├── docs/
│   ├── PROJECT_NAME_Project_Documentation.md # Product Specification & Architecture
│   ├── PROJECT_NAME_Master_TODO.md           # Implementation Checklist & Release Gates
│   ├── PROTOCOL_SPEC.md                      # Network Protocol Specification v1.0
│   └── decisions/
│       └── ADR-0001-baseline-architecture-and-governance.md
├── src/
│   ├── shared/
│   │   └── ProjectName.Shared/               # Core protocol envelopes, coordinate mapping, pairing logic
│   ├── windows/                              # Windows desktop client and agent
│   ├── android/                              # Android mobile application
│   └── tv/                                   # Google TV / Android TV remote adapter
├── tests/
│   └── shared/
│       └── ProjectName.Shared.Tests/         # Protocol and coordinate mapping test suite
├── CONTRIBUTING.md                            # Branch conventions, commit specs, and issue labels
├── LICENSE                                   # MIT License
└── SECURITY.md                               # Vulnerability reporting process and privacy invariants
```

---

## 5. Getting Started & Testing

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Java 17 JDK
- Android Studio / Android SDK (API 31+)

### Building and Running Shared Tests
```powershell
dotnet restore ProjectName.sln
dotnet test ProjectName.sln
```

---

## 6. Project Roadmap & Documentation

- [Full Architecture & Requirements Specification](docs/PROJECT_NAME_Project_Documentation.md)
- [Master Implementation Checklist & TODO](docs/PROJECT_NAME_Master_TODO.md)
- [Network Protocol Specification v1.0](docs/PROTOCOL_SPEC.md)
- [Architecture Decision Records (ADRs)](docs/decisions/)
- [Security Policy & Disclosures](SECURITY.md)

---

## 7. License

Distributed under the [MIT License](LICENSE). Copyright (c) 2026 RetsardCo and [PROJECT_NAME] Contributors.
