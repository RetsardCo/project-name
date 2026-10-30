# Peerlight — Official Brand Guidelines & Visual Identity

**Version:** 1.0.0  
**Effective Date:** 2026-10-31  
**Status:** Approved  

---

## 1. Brand Identity Overview

**Peerlight** is a modern, high-performance cross-platform remote device viewing and control ecosystem. It enables seamless, low-latency interaction between Windows PCs, Android phones, and Google TV / Android TV devices over trusted local networks.

- **Product Name:** Peerlight
- **Tagline:** Connected devices made visible and accessible.
- **Short Description:** Cross-platform remote device viewing and control.
- **Brand Archetype:** Precision engineering, cryptographic clarity, and minimal design.

---

## 2. Core Color Palette

The Peerlight color system balances deep, focused navy surfaces with vivid electric blue and glowing cyan accents.

### 2.1 Primary Brand Colors

| Token Name | Hex Code | RGB | Role / Usage |
| :--- | :--- | :--- | :--- |
| `color-brand-navy` | `#071426` | `(7, 20, 38)` | Primary background, brand canvas, launcher background |
| `color-brand-surface` | `#0D1B2D` | `(13, 27, 45)` | Elevated cards, sidebars, toolbars, modal backgrounds |
| `color-brand-surface-light`| `#16273F` | `(22, 39, 63)` | Hover states, active selection, borders |
| `color-brand-blue` | `#168BFF` | `(22, 139, 255)` | Primary action buttons, active navigation, focus rings |
| `color-brand-cyan` | `#20D9E8` | `(32, 217, 232)` | Accents, glowing node connectors, status indicators |
| `color-brand-cyan-light` | `#67E8F9` | `(103, 232, 249)`| Star highlights, active toggle glow |

### 2.2 Semantic & Feedback Colors

| Token Name | Hex Code | Role / Usage |
| :--- | :--- | :--- |
| `color-status-success` | `#22C55E` | Connected status, verified pairing, active media stream |
| `color-status-warning` | `#F59E0B` | Pairing challenge waiting, reconnecting, network latency |
| `color-status-danger` | `#EF4444` | Emergency Disconnect, revoked permissions, fatal error |
| `color-text-primary` | `#F4F7FB` | Primary headers, high-contrast labels, active values |
| `color-text-secondary`| `#9AAAC0` | Subtitles, device details, telemetry units, timestamps |
| `color-text-muted` | `#64748B` | Disabled controls, placeholder hints, tertiary notes |

### 2.3 Light Mode Surface Tokens

| Token Name | Hex Code | Role / Usage |
| :--- | :--- | :--- |
| `color-light-bg` | `#F5F7FA` | Light theme window background |
| `color-light-surface` | `#FFFFFF` | Light theme card surface |
| `color-light-border` | `#E2E8F0` | Subtle card borders |
| `color-light-text-primary` | `#0F172A` | Primary text on light surface |
| `color-light-text-secondary` | `#475569` | Secondary text on light surface |

---

## 3. Brand Symbol & Logo Assets

The Peerlight logo combines a stylized letter **P**, connected network nodes, and a radiant center star.

```
       ●
      / 
   ●───(  ✦  )
      \       \
       ●   P   )
           │  /
           │─'
```

### 3.1 Asset Variants Inventory

1. **Full Logo with Wordmark (`assets/branding/peerlight-logo-full.png`):** High-resolution 1024x1024 master asset for banners, store listings, and splash screens.
2. **Vector Logo (`assets/branding/peerlight-logo.svg`):** Scalable vector format for web and high-DPI displays.
3. **Symbol-Only (`assets/branding/peerlight-symbol.png` & `peerlight-symbol.svg`):** Clean 512x512 square crop of the glowing P node icon.
4. **Windows Application Icon (`src/windows/ProjectName.Windows/peerlight.ico`):** Multi-resolution icon embedding 16x16, 24x24, 32x32, 48x48, 64x64, 128x128, and 256x256 mipmaps.
5. **Android Launcher Icons:**
   - Standard: `mipmap-{m,h,xh,xxh,xxxh}dpi/ic_launcher.png`
   - Circular: `mipmap-{m,h,xh,xxh,xxxh}dpi/ic_launcher_round.png`
   - Adaptive Foreground: `drawable/ic_launcher_foreground.png`
   - Adaptive Background: `drawable/ic_launcher_background.xml` (`#071426`)
6. **Small Navigation / Header Icon (`assets/branding/peerlight-small-nav.png`):** 64x64 icon optimized for sidebar headers and dialog crowns.
7. **About Page Asset (`assets/branding/peerlight-about.png`):** 256x256 display asset.
8. **Light/Dark Background Variants:** `peerlight-logo-light.png` and `peerlight-logo-dark.png`.

### 3.2 Clear Space & Proportions
- Keep a clear margin around the logo equal to at least 25% of the symbol width.
- Never stretch, skew, or rotate the symbol.
- Never separate the central star from the P counter loop.

---

## 4. Typography Hierarchy

Peerlight adheres to clean system fonts for maximum platform integration and legibility.

- **Windows Platform:** `Segoe UI Variable`, `Segoe UI`, `Consolas` (code/diagnostics).
- **Android Platform:** `Roboto`, `Google Sans`, `Roboto Mono`.
- **Cross-Platform Fallback:** System San Francisco (`-apple-system`), `Segoe UI`, `sans-serif`.

### Type Scale
- **Display 1 (Hero Title):** 24pt / 32sp, Bold (700), `#F4F7FB`
- **Heading 1 (Section Titles):** 18pt / 24sp, Semi-Bold (600), `#F4F7FB`
- **Heading 2 (Card Titles):** 14pt / 18sp, Semi-Bold (600), `#F4F7FB`
- **Body Text:** 10pt / 14sp, Regular (400), `#9AAAC0`
- **Monospace / PIN Code:** 20pt / 26sp, Bold (700), Monospace, `#20D9E8`

---

## 5. UI Spacing & Corner Radii

- **Base Unit:** 4px
- **Spacing Scale:** 4px (`xs`), 8px (`sm`), 12px (`md`), 16px (`lg`), 24px (`xl`), 32px (`2xl`)
- **Corner Radii:**
  - Small Buttons / Badges: `6px`
  - Cards / Dialogs: `12px`
  - Window Containers: `16px`
  - Pill Buttons / Circular Badges: `9999px`

---

## 6. Safety & Disconnect Visual Language

1. **EMERGENCY DISCONNECT:** Always rendered with high visual prominence in `#EF4444` (or `#D32F2F`), accompanied by an immediate key/mouse release indicator.
2. **Zero-Recording Invariant Badge:** Displayed in live session views: *"Zero Disk Recording: In-Memory Bounded Buffer (Active)"*.
3. **Session Status Badges:**
   - Active: Emerald Green circle `#22C55E` with *"Connected"*
   - Standby: Slate Blue circle `#64748B` with *"Discovered"*
   - Pairing: Amber circle `#F59E0B` with *"Awaiting PIN"*
