# ADR-0003: WebRTC Real-Time Media Pipeline and Zero-Disk Storage Architecture

## Status
Accepted

## Context
Non-Negotiable Requirement 3.1 mandates that `[PROJECT_NAME]` is strictly a live viewing and remote control system. It must not implement screen recording, automatic screenshots, session replay, or persistent video archives. In-memory buffers must be strictly bounded and discarded promptly.

Furthermore, remote screen interaction requires sub-100ms end-to-end latency on local networks, which conventional streaming protocols (e.g. RTMP, HLS, or chunked TCP video streams) cannot achieve due to large buffering windows.

## Decision
We select WebRTC (Web Real-Time Communication) with hardware-accelerated H.264/VP8 encoding as the media transport layer for live video streaming, coupled with a bounded in-memory frame pipeline.

Key architectural rules:
1. **Zero Disk I/O Guarantee:** The video capture and transport pipeline operates exclusively in volatile memory (`SurfaceTexture` -> Hardware Encoder -> RTP/SRTP packetization). At no point in the capture or rendering pipeline is any frame written to flash storage or persistent disk.
2. **Bounded Frame Queue:** The capture buffer maintains a hard capacity of at most 2–3 unencoded frames (approx. 33–66 ms of video at 30 FPS).
3. **Stale Frame Drop:** If network backpressure or encoding stalls occur, the oldest queued frame is immediately dropped rather than buffered, guaranteeing that accumulated latency never builds up.
4. **Session Teardown Cleanup:** Disconnecting immediately flushes all video track references, clears in-memory frame queues, and releases encoder surfaces.

## Consequences
- **Positive:** Achieves p95 < 100 ms latency over local Wi-Fi with adaptive bitrate and resolution downscaling.
- **Positive:** Mathematically and architecturally prevents inadvertent screen recording or persistent data leaks.
- **Negative:** Native WebRTC libraries require platform-specific binaries on Windows and Android.
