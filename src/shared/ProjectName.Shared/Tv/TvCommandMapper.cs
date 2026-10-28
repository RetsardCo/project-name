using System;
using System.Collections.Generic;

namespace ProjectName.Shared.Tv
{
    /// <summary>
    /// Maps protocol string commands (e.g., 'dpad_up', 'back', 'volume_up') to Android TV key codes.
    /// Strictly adheres to PROTOCOL_SPEC section 3.4 for INPUT_TV_COMMAND payloads.
    /// </summary>
    public static class TvCommandMapper
    {
        private static readonly Dictionary<string, TvKeyCode> CommandMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dpad_up"] = TvKeyCode.DpadUp,
            ["dpad_down"] = TvKeyCode.DpadDown,
            ["dpad_left"] = TvKeyCode.DpadLeft,
            ["dpad_right"] = TvKeyCode.DpadRight,
            ["dpad_center"] = TvKeyCode.DpadCenter,
            ["select"] = TvKeyCode.DpadCenter,
            ["ok"] = TvKeyCode.DpadCenter,
            ["back"] = TvKeyCode.Back,
            ["home"] = TvKeyCode.Home,
            ["volume_up"] = TvKeyCode.VolumeUp,
            ["volume_down"] = TvKeyCode.VolumeDown,
            ["mute"] = TvKeyCode.Mute,
            ["power"] = TvKeyCode.Power,
            ["play_pause"] = TvKeyCode.MediaPlayPause,
            ["media_play_pause"] = TvKeyCode.MediaPlayPause,
            ["media_stop"] = TvKeyCode.MediaStop,
            ["media_next"] = TvKeyCode.MediaNext,
            ["media_previous"] = TvKeyCode.MediaPrevious
        };

        /// <summary>
        /// Attempts to map a command string to its corresponding TvKeyCode.
        /// </summary>
        public static bool TryMapCommand(string? command, out TvKeyCode keyCode)
        {
            if (!string.IsNullOrWhiteSpace(command) && CommandMap.TryGetValue(command.Trim(), out keyCode))
            {
                return true;
            }

            keyCode = TvKeyCode.Unknown;
            return false;
        }

        /// <summary>
        /// Maps a command string to its corresponding TvKeyCode, throwing ArgumentException if unknown.
        /// </summary>
        public static TvKeyCode MapCommand(string command)
        {
            if (TryMapCommand(command, out var keyCode))
            {
                return keyCode;
            }

            throw new ArgumentException($"Unknown or unsupported TV command: '{command}'.", nameof(command));
        }
    }
}
