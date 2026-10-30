# Peerlight — Cross-Platform Design System Specification

**Document Version:** 1.0.0  
**Status:** Approved Specification  
**Platforms Covered:** Windows 11 Desktop (WPF/WinForms) & Android 12+ (Material Components)

---

## 1. Design Philosophy

Peerlight bridges the desktop and mobile ecosystems with a unified visual language:
- **macOS-Inspired Desktop Elegance:** Minimalist aesthetic, generous breathing room, restrained shadows, clean card containers, and unified sidebar navigation.
- **Mobile-Native Touch Optimization:** Thumb-friendly touch targets (min 48dp), clear bottom navigation tabs, high-contrast dark surfaces, and immediate haptic/visual feedback.
- **Clarity and Focus:** Elimination of unnecessary ornamentation. Functional status indicators communicate device pairing, network discovery, and streaming throughput with zero ambiguity.

---

## 2. Design Tokens

Peerlight implements a 3-layer token architecture:
`Primitive (Raw Hex/Dimensions)` → `Semantic (Role & Purpose)` → `Component (Component-Specific Bounds)`

### 2.1 Primitive & Semantic Color Tokens

```
===================================================================================
TOKEN NAME                    VALUE       HEX       ROLE & APPLICATION
===================================================================================
--color-brand-navy           Navy 950    #071426   Master background & launcher canvas
--color-brand-surface        Navy 900    #0D1B2D   Elevated panels, sidebars, cards
--color-brand-surface-elev   Navy 800    #16273F   Card hover, input backgrounds, borders
--color-brand-surface-hover  Navy 700    #1E3454   Active item selection, button hover
--color-brand-blue           Blue 500    #168BFF   Primary action buttons, brand focus
--color-brand-blue-hover     Blue 600    #0284C7   Pressed state for primary buttons
--color-brand-cyan           Cyan 400    #20D9E8   Brand accent, glowing node indicators
--color-brand-cyan-light     Cyan 300    #67E8F9   Highlight star, active toggle glow
--color-status-success       Green 500   #22C55E   Verified pairing, live active stream
--color-status-warning       Amber 500   #F59E0B   PIN challenge waiting, reconnecting
--color-status-danger        Red 500     #EF4444   Emergency disconnect, revoked trust
--color-text-primary         White 950   #F4F7FB   Headings, high-contrast values, titles
--color-text-secondary       Slate 400   #9AAAC0   Subtitles, metadata, timestamps
--color-text-muted           Slate 500   #64748B   Placeholders, inactive tabs, captions
===================================================================================
```

### 2.2 Light Mode Surface Mapping

```
===================================================================================
TOKEN NAME                    VALUE       HEX       LIGHT THEME MAPPING
===================================================================================
--color-light-bg             Slate 50    #F5F7FA   Replaces --color-brand-navy
--color-light-surface        White 1000  #FFFFFF   Replaces --color-brand-surface
--color-light-border         Slate 200   #E2E8F0   Replaces --color-brand-surface-elev
--color-light-text-primary   Slate 900   #0F172A   Replaces --color-text-primary
--color-light-text-secondary Slate 600   #475569   Replaces --color-text-secondary
===================================================================================
```

---

## 3. Typography Scale

Clean system typography ensures crisp rendering across variable screen densities.

| Scale Token | Windows Desktop Font | Android Mobile Font | Size / Weight | Line Height |
| :--- | :--- | :--- | :--- | :--- |
| `type-display` | Segoe UI Variable Display | Google Sans / Roboto | 22pt / 28sp (Bold 700) | 1.25 |
| `type-heading` | Segoe UI Variable Text | Roboto Medium | 14pt / 18sp (SemiBold 600) | 1.3 |
| `type-subheading` | Segoe UI Variable Text | Roboto Medium | 11pt / 14sp (Medium 500) | 1.4 |
| `type-body` | Segoe UI Variable Text | Roboto Regular | 9.5pt / 13sp (Regular 400) | 1.5 |
| `type-caption` | Segoe UI Variable Text | Roboto Regular | 8.5pt / 11sp (Regular 400) | 1.4 |
| `type-code` | Consolas / Cascadia Mono | Roboto Mono | 14pt / 18sp (Bold 700) | 1.2 |

---

## 4. Spacing Scale & Corner Radii

### 4.1 Spacing
- `space-xs`: `4px` (inner padding, icon-text gap)
- `space-sm`: `8px` (button padding, compact element spacing)
- `space-md`: `12px` (card content padding, form field gaps)
- `space-lg`: `16px` (standard container margin, card gaps)
- `space-xl`: `24px` (section margins, header padding)
- `space-2xl`: `32px` (page gutters, modal padding)

### 4.2 Corner Radii
- `radius-sm`: `6px` (Status badges, pill indicators, small buttons)
- `radius-md`: `8px` (Standard action buttons, text inputs)
- `radius-lg`: `12px` (Content cards, device items, modals)
- `radius-xl`: `16px` (Main window surfaces)
- `radius-full`: `9999px` (Circular D-pad buttons, avatar icons)

---

## 5. Standard Component Specifications

### 5.1 Buttons
1. **Primary Button:** Background `#168BFF`, Text `#FFFFFF`, Height `36px` (desktop) / `44dp` (mobile), Radius `8px`. Used for "Connect", "Pair", "Start Capture".
2. **Secondary Button:** Background `#16273F`, Border `#1C304C`, Text `#F4F7FB`, Radius `8px`. Used for "Fit Window", "Generate PIN", "Refresh".
3. **Danger Button:** Background `#EF4444`, Text `#FFFFFF`, Radius `8px`. Dedicated exclusively to **EMERGENCY DISCONNECT**.
4. **Ghost / Tab Button:** Transparent background, Text `#9AAAC0`, Hover `#16273F`, Active `#20D9E8`.

### 5.2 Device Cards
- Dimensions: Width 100%, Height `76px` - `90px`.
- Left: 44px circular platform avatar (`💻` Windows, `📱` Android, `📺` Google TV).
- Center: Device Name (`type-heading`), Subtitle showing OS, IP address, and capability pills.
- Right: Status Pill (`Online` in `#22C55E`, `Discovered` in `#168BFF`) + "Connect" Action button.

### 5.3 Google TV D-Pad Remote
- Outer Ring Diameter: `220px` (desktop) / `220dp` (mobile).
- 4 Directional Segments: Up, Down, Left, Right with smooth triangular directional glyphs.
- Center Button: `70px` diameter circle in `#168BFF` with bold "OK" text.
- Aux Controls: Back, Home, Vol +, Vol -, Play/Pause positioned directly below the D-pad.

### 5.4 Safety Indicators
- **Emergency Stop:** Prominent button triggering immediate network severance, pipeline termination, and P/Invoke `SendInput` key/button reset within 1 millisecond.
- **Zero Disk Recording Pill:** Green badge `#22C55E` confirming active media stream operates entirely in RAM via bounded ring buffers (capacity = 2 frames) with zero bytes committed to storage.
