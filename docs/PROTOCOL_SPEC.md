# Protocol Specification v1.0 — [PROJECT_NAME]

## 1. Overview & Transport Model

`[PROJECT_NAME]` employs a decoupled multi-channel architecture:
1. **Discovery Layer:** Multicast DNS (mDNS / DNS-SD) over UDP port `5353` with service type `_projectname._tcp.local.`, supplemented by direct IP/port fallback.
2. **Control & Signaling Channel:** Authenticated, TLS-encrypted TCP connection. Handles device identity exchange, challenge-response pairing, session lifecycle management, and discrete remote input (keyboard, mouse, touch, TV d-pad).
3. **Media Streaming Channel:** Bounded real-time WebRTC media pipeline utilizing hardware encoders/decoders (H.264/VP8/AV1) with zero persistent disk storage.

All control messages use structured JSON envelopes with strict size boundaries, schema validation, and sequence numbering to prevent replay attacks.

---

## 2. Base Message Envelope

All control and signaling messages must adhere to the base envelope format:

```json
{
  "protocol_version": "1.0",
  "message_id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "sequence_number": 1042,
  "timestamp_utc_ms": 1793203200000,
  "type": "MESSAGE_TYPE",
  "payload": {}
}
```

### Constraints:
- Maximum envelope size: `65,536` bytes (64 KB). Any message exceeding this limit is immediately dropped and results in channel termination.
- `sequence_number`: Monotonically increasing per active connection; duplicate or backwards sequence numbers trigger replay rejection.
- Unknown message types with unsupported versions are discarded with an explicit `ERROR` response.

---

## 3. Message Catalog

### 3.1 Discovery (`DISCOVERY_ANNOUNCE` / `DISCOVERY_QUERY`)
Advertised over mDNS TXT records and local beacons:
```json
{
  "type": "DISCOVERY_ANNOUNCE",
  "payload": {
    "device_id": "crypto-sha256-fingerprint",
    "display_name": "Living Room PC",
    "platform": "windows",
    "protocol_version": "1.0",
    "control_port": 8443,
    "capabilities": [
      "screen_view",
      "mouse_control",
      "keyboard_control"
    ]
  }
}
```

### 3.2 Pairing (`PAIR_REQUEST`, `PAIR_CHALLENGE`, `PAIR_CONFIRM`, `PAIR_RESULT`)
Explicit out-of-band user consent flow:
- `PAIR_REQUEST`: Initiated by controller containing client public identity key.
- `PAIR_CHALLENGE`: Endpoint displays short-lived 6-digit numeric pairing PIN (valid for 60 seconds, max 3 failed attempts before 5-minute lockout).
- `PAIR_CONFIRM`: Controller submits user-entered PIN and cryptographically signed challenge nonce.
- `PAIR_RESULT`: Success returns long-lived mutual authentication credential; failure returns error code (`PIN_EXPIRED`, `PIN_INVALID`, `RATE_LIMITED`).

### 3.3 Session Management (`SESSION_START`, `SESSION_ACCEPT`, `SESSION_STOP`)
```json
{
  "type": "SESSION_START",
  "payload": {
    "session_id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "requested_scope": "view_and_control",
    "requested_resolution": { "width": 1920, "height": 1080, "fps": 30 }
  }
}
```

`requested_scope` values:
- `view_only`: Screen viewing only. All input messages are rejected by the endpoint.
- `view_and_control`: Screen viewing with authorized remote input.

### 3.4 Remote Input Messages

#### Pointer Input (`INPUT_POINTER_MOVE`, `INPUT_POINTER_BUTTON`)
Normalized coordinates `[0.0, 1.0]` map across variable client/endpoint display resolutions and rotation:
```json
{
  "type": "INPUT_POINTER_MOVE",
  "payload": {
    "x_norm": 0.4521,
    "y_norm": 0.8124
  }
}
```

#### Keyboard Input (`INPUT_KEY`)
```json
{
  "type": "INPUT_KEY",
  "payload": {
    "key_code": "Enter",
    "action": "down",
    "modifiers": ["Ctrl"]
  }
}
```
*Safety Rule:* Disconnect immediately resets all active held keys and buttons.

#### Touch Gestures (`INPUT_TOUCH_GESTURE`)
For Android endpoints supporting Accessibility gesture dispatch:
```json
{
  "type": "INPUT_TOUCH_GESTURE",
  "payload": {
    "gesture": "tap",
    "x_norm": 0.5,
    "y_norm": 0.5,
    "duration_ms": 50
  }
}
```

#### Google TV Navigation (`INPUT_TV_COMMAND`)
Discrete remote navigation commands:
```json
{
  "type": "INPUT_TV_COMMAND",
  "payload": {
    "command": "dpad_up"
  }
}
```
Supported commands: `dpad_up`, `dpad_down`, `dpad_left`, `dpad_right`, `dpad_center`, `back`, `home`, `volume_up`, `volume_down`, `play_pause`.

---

## 4. Diagnostics & Safety

- Diagnostic snapshots (`DIAGNOSTICS_REPORT`) report RTT, jitter, bitrate, dropped frame count, and hardware codec status.
- **Strict Invariant:** Diagnostic payloads must never include image buffers, frame hashes that allow image reconstruction, keystroke values, or credentials.
