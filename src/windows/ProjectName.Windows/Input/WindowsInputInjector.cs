using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProjectName.Shared.Protocol;

namespace ProjectName.Windows.Input
{
    public class WindowsInputInjector : IWindowsInputInjector
    {
        private readonly ConcurrentDictionary<string, byte> _heldKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _heldButtons = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> HeldKeys => _heldKeys.Keys as IReadOnlyCollection<string> ?? new List<string>(_heldKeys.Keys);
        public IReadOnlyCollection<string> HeldButtons => _heldButtons.Keys as IReadOnlyCollection<string> ?? new List<string>(_heldButtons.Keys);

        #region Native Methods & Constants

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(8)]
            public MOUSEINPUT mi;
            [FieldOffset(8)]
            public KEYBDINPUT ki;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        #endregion

        public void InjectPointerMove(double xNorm, double yNorm)
        {
            if (double.IsNaN(xNorm) || double.IsInfinity(xNorm)) return;
            if (double.IsNaN(yNorm) || double.IsInfinity(yNorm)) return;

            int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int vWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int vHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (vWidth <= 0 || vHeight <= 0) return;

            // Normalized to absolute mouse event coordinate range [0, 65535] across the virtual desktop
            double clampedX = Math.Clamp(xNorm, 0.0, 1.0);
            double clampedY = Math.Clamp(yNorm, 0.0, 1.0);

            int absX = (int)Math.Round(clampedX * 65535.0);
            int absY = (int)Math.Round(clampedY * 65535.0);

            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = absX,
                    dy = absY,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        public void InjectPointerButton(string button, string action)
        {
            if (string.IsNullOrWhiteSpace(button) || string.IsNullOrWhiteSpace(action)) return;

            uint downFlag = 0;
            uint upFlag = 0;

            switch (button.ToLowerInvariant())
            {
                case "left":
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
                case "right":
                    downFlag = MOUSEEVENTF_RIGHTDOWN;
                    upFlag = MOUSEEVENTF_RIGHTUP;
                    break;
                case "middle":
                    downFlag = MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    return;
            }

            var inputs = new List<INPUT>();

            switch (action.ToLowerInvariant())
            {
                case "down":
                    _heldButtons[button] = 1;
                    inputs.Add(CreateMouseInput(downFlag));
                    break;

                case "up":
                    _heldButtons.TryRemove(button, out _);
                    inputs.Add(CreateMouseInput(upFlag));
                    break;

                case "click":
                    inputs.Add(CreateMouseInput(downFlag));
                    inputs.Add(CreateMouseInput(upFlag));
                    _heldButtons.TryRemove(button, out _);
                    break;
            }

            if (inputs.Count > 0)
            {
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            }
        }

        public void InjectKey(string keyCode, string action, IEnumerable<string>? modifiers = null)
        {
            if (string.IsNullOrWhiteSpace(keyCode) || string.IsNullOrWhiteSpace(action)) return;

            ushort vk = MapKeyCodeToVirtualKey(keyCode);
            if (vk == 0) return;

            var inputs = new List<INPUT>();

            // Apply modifiers if any
            if (modifiers != null && action.Equals("down", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var mod in modifiers)
                {
                    ushort modVk = MapKeyCodeToVirtualKey(mod);
                    if (modVk != 0)
                    {
                        _heldKeys[mod] = 1;
                        inputs.Add(CreateKeyboardInput(modVk, false));
                    }
                }
            }

            if (action.Equals("down", StringComparison.OrdinalIgnoreCase))
            {
                _heldKeys[keyCode] = 1;
                inputs.Add(CreateKeyboardInput(vk, false));
            }
            else if (action.Equals("up", StringComparison.OrdinalIgnoreCase))
            {
                _heldKeys.TryRemove(keyCode, out _);
                inputs.Add(CreateKeyboardInput(vk, true));
            }

            if (inputs.Count > 0)
            {
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            }
        }

        public void ReleaseAllHeldInputs()
        {
            var inputs = new List<INPUT>();

            // Release held buttons
            foreach (var button in _heldButtons.Keys)
            {
                uint upFlag = button.ToLowerInvariant() switch
                {
                    "left" => MOUSEEVENTF_LEFTUP,
                    "right" => MOUSEEVENTF_RIGHTUP,
                    "middle" => MOUSEEVENTF_MIDDLEUP,
                    _ => 0
                };
                if (upFlag != 0) inputs.Add(CreateMouseInput(upFlag));
            }
            _heldButtons.Clear();

            // Release held keys
            foreach (var key in _heldKeys.Keys)
            {
                ushort vk = MapKeyCodeToVirtualKey(key);
                if (vk != 0) inputs.Add(CreateKeyboardInput(vk, true));
            }
            _heldKeys.Clear();

            if (inputs.Count > 0)
            {
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            }
        }

        private static INPUT CreateMouseInput(uint flag) =>
            new()
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT { dwFlags = flag | MOUSEEVENTF_VIRTUALDESK }
            };

        private static INPUT CreateKeyboardInput(ushort vk, bool keyUp) =>
            new()
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = keyUp ? KEYEVENTF_KEYUP : 0
                }
            };

        private static ushort MapKeyCodeToVirtualKey(string key)
        {
            return key.ToLowerInvariant() switch
            {
                "enter" or "return" => 0x0D,
                "escape" or "esc" => 0x1B,
                "backspace" or "back" => 0x08,
                "tab" => 0x09,
                "space" => 0x20,
                "ctrl" or "control" => 0x11,
                "shift" => 0x10,
                "alt" => 0x12,
                "up" => 0x26,
                "down" => 0x28,
                "left" => 0x25,
                "right" => 0x27,
                "home" => 0x24,
                "end" => 0x23,
                "pageup" => 0x21,
                "pagedown" => 0x22,
                "delete" or "del" => 0x2E,
                var singleChar when singleChar.Length == 1 => (ushort)(VkKeyScan(singleChar[0]) & 0xFF),
                _ => 0
            };
        }
    }
}
