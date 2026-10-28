# ADR-0002: Android Input Injection Mechanism via AccessibilityService

## Status
Accepted

## Context
Non-Negotiable Requirement 3.2 mandates that the application must operate without requiring ADB as a runtime dependency. Furthermore, ordinary non-root Android applications cannot invoke `InputManager.injectInputEvent()` or access `/dev/input/event*` nodes due to Linux permission restrictions (`android.permission.INJECT_EVENTS` requires system signature).

## Decision
We select the standard Android `AccessibilityService` API (`dispatchGesture()` introduced in Android 7.0 / API 24) as the input injection mechanism for Android endpoints.

Key architectural rules:
1. The service exposes gesture synthesis (`GestureDescription.Builder`) for single taps, click-and-drag, multi-point swipes, and programmatic invocations of global actions (`GLOBAL_ACTION_BACK`, `GLOBAL_ACTION_HOME`, `GLOBAL_ACTION_RECENTS`).
2. The user must explicitly enable the accessibility service in Android Settings with a clear disclosure of why permissions are requested.
3. System security boundaries are strictly respected: gesture injection will not attempt to bypass OS-protected password fields, permission confirmation prompts, or device admin dialogs.
4. If the user revokes accessibility permissions in Android Settings, the application immediately updates its capability state, drops input handling, and transitions to View-Only mode without crashing.

## Consequences
- **Positive:** No ADB daemon, root access, or developer options required for ordinary users on runtime.
- **Positive:** Complies with platform security architecture and Google Play accessibility policy requirements for remote assistance tools.
- **Negative:** Certain OEM skins (such as Xiaomi MIUI/HyperOS on the Redmi Note 10 reference device) show a 10-second warning dialog when enabling accessibility services. This requires clear onboarding guidance.
- **Negative:** Full global touch injection across every conceivable system screen is not possible on secure/credential surfaces. This limitation is explicitly documented and accepted in v1.
