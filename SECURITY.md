# Security Policy — [PROJECT_NAME]

## 1. Security Architecture Principles

`[PROJECT_NAME]` is an open, cross-platform, LAN-first remote device viewing and control system. Security, consent, and user privacy are foundational requirements:

1. **Permission Before Control:** No device may be viewed or controlled without explicit, cryptographic device pairing and explicit OS-level permission consent on the controlled endpoint.
2. **Strictly No Screen Recording:** The system provides live streaming only. It must never implement video recording, automated screenshots, session replays, or persistent video buffering on disk.
3. **No ADB Runtime Dependency:** Ordinary runtime operation must never require Android Debug Bridge (ADB), root privileges, or undocumented OS backdoors.
4. **LAN-First Isolation:** Local sessions do not touch or depend on third-party cloud relays or internet services. Unauthenticated WAN listeners and public port exposures are strictly prohibited.
5. **Immediate Revocation:** The controlled device maintains authority at all times and can terminate an active session or revoke paired trust immediately.

## 2. Supported Versions

| Version | Supported | Status |
| :--- | :--- | :--- |
| `0.1.x` (pre-release baseline) | :white_check_mark: | Active Development & Feasibility Validation |

## 3. Reporting a Vulnerability

We take the security of `[PROJECT_NAME]` seriously. If you discover a security vulnerability, we ask that you report it privately to allow remediation before public disclosure.

### Reporting Channels
- **GitHub Security Advisory:** Please submit a private advisory report via the GitHub repository's **Security** tab -> **Advisories**.
- **Email Contact:** security@retsard.co (or project maintainers via GitHub profile).

### What to Include in Your Report
Please provide:
- A descriptive summary of the vulnerability.
- Target platform(s) affected (Windows, Android, Google TV).
- Detailed steps to reproduce the issue (proof-of-concept code, packet traces, or minimal reproduction steps).
- Expected vs. actual behavior.
- Assessment of potential impact (e.g., unauthorized pairing bypass, remote code execution, replay attacks, input injection into unauthorized sessions).

### Response Process
1. **Acknowledgement:** We will acknowledge receipt of your report within 48 hours.
2. **Assessment:** Maintainers will verify the vulnerability and assess its severity.
3. **Remediation:** A patch or mitigation will be developed and tested across affected platforms.
4. **Coordinated Disclosure:** We will coordinate with the reporter on a public disclosure date once a fix is released.

## 4. Out-of-Scope Behaviors

The following are expected platform behaviors and are **not** considered security vulnerabilities:
- Android system dialogs appearing when requesting MediaProjection or Accessibility permissions (this is mandatory platform consent).
- Operating system blocking screen capture on DRM-protected surfaces or secure windows (e.g., banking apps, login password fields).
- Windows User Account Control (UAC) / Secure Desktop isolation pausing standard user-mode remote control.
