using System;
using System.Collections.Generic;

namespace ProjectName.Shared.Protocol
{
    [Flags]
    public enum DeviceCapabilityFlags
    {
        None = 0,
        ScreenView = 1 << 0,
        PointerControl = 1 << 1,
        KeyboardControl = 1 << 2,
        TouchGestures = 1 << 3,
        TvNavigation = 1 << 4
    }

    public enum SessionScope
    {
        ViewOnly = 0,
        ViewAndControl = 1
    }

    public static class CapabilityNames
    {
        public const string ScreenView = "screen_view";
        public const string PointerControl = "pointer_control";
        public const string KeyboardControl = "keyboard_control";
        public const string TouchGestures = "touch_gestures";
        public const string TvNavigation = "tv_navigation";

        public static IEnumerable<string> ToNames(DeviceCapabilityFlags flags)
        {
            var list = new List<string>();
            if (flags.HasFlag(DeviceCapabilityFlags.ScreenView)) list.Add(ScreenView);
            if (flags.HasFlag(DeviceCapabilityFlags.PointerControl)) list.Add(PointerControl);
            if (flags.HasFlag(DeviceCapabilityFlags.KeyboardControl)) list.Add(KeyboardControl);
            if (flags.HasFlag(DeviceCapabilityFlags.TouchGestures)) list.Add(TouchGestures);
            if (flags.HasFlag(DeviceCapabilityFlags.TvNavigation)) list.Add(TvNavigation);
            return list;
        }

        public static DeviceCapabilityFlags FromNames(IEnumerable<string>? names)
        {
            if (names == null) return DeviceCapabilityFlags.None;
            DeviceCapabilityFlags flags = DeviceCapabilityFlags.None;
            foreach (var rawName in names)
            {
                if (string.IsNullOrWhiteSpace(rawName)) continue;
                var name = rawName.Trim().ToLowerInvariant();
                switch (name)
                {
                    case ScreenView:
                    case "view_screen":
                        flags |= DeviceCapabilityFlags.ScreenView;
                        break;
                    case PointerControl:
                    case "mouse_control":
                        flags |= DeviceCapabilityFlags.PointerControl;
                        break;
                    case KeyboardControl:
                        flags |= DeviceCapabilityFlags.KeyboardControl;
                        break;
                    case TouchGestures:
                    case "touch_input":
                        flags |= DeviceCapabilityFlags.TouchGestures;
                        break;
                    case TvNavigation:
                        flags |= DeviceCapabilityFlags.TvNavigation;
                        break;
                }
            }
            return flags;
        }
    }
}
