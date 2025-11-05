using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace SpeechToTextTray.Core.Services
{
    /// <summary>
    /// Service for injecting text into the currently focused application
    /// </summary>
    public class TextInjectionService
    {
        #region Win32 API Declarations

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
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

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        #endregion

        private readonly bool _fallbackToClipboard;

        public TextInjectionService(bool fallbackToClipboard = true)
        {
            _fallbackToClipboard = fallbackToClipboard;
        }

        /// <summary>
        /// Inject text into the currently focused window
        /// </summary>
        public bool InjectText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            try
            {
                // Try SendInput first (most reliable)
                if (SendInputMethod(text))
                    return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendInput failed: {ex.Message}");
            }

            try
            {
                // Try SendKeys as fallback
                System.Windows.Forms.SendKeys.SendWait(EscapeForSendKeys(text));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendKeys failed: {ex.Message}");
            }

            // Last resort: copy to clipboard
            if (_fallbackToClipboard)
            {
                try
                {
                    Clipboard.SetText(text);
                    return false; // Indicate clipboard fallback was used
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clipboard fallback failed: {ex.Message}");
                }
            }

            return false;
        }

        /// <summary>
        /// Copy text to clipboard (explicit method)
        /// </summary>
        public void CopyToClipboard(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }

        /// <summary>
        /// Get the title of the currently focused window
        /// </summary>
        public string GetActiveWindowTitle()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    var sb = new System.Text.StringBuilder(256);
                    GetWindowText(hwnd, sb, 256);
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting window title: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Send text using Win32 SendInput API (supports Unicode)
        /// </summary>
        private bool SendInputMethod(string text)
        {
            // Create input array (key down + key up for each character)
            var inputs = new INPUT[text.Length * 2];
            int index = 0;

            foreach (char c in text)
            {
                // Key down
                inputs[index++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    union = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = c,
                            dwFlags = KEYEVENTF_UNICODE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                // Key up
                inputs[index++] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    union = new INPUTUNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = c,
                            dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
            }

            // Send the inputs
            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            return result == inputs.Length;
        }

        /// <summary>
        /// Escape special characters for SendKeys
        /// </summary>
        private string EscapeForSendKeys(string text)
        {
            // SendKeys has special meaning for: + ^ % ~ ( ) { } [ ]
            return text
                .Replace("+", "{+}")
                .Replace("^", "{^}")
                .Replace("%", "{%}")
                .Replace("~", "{~}")
                .Replace("(", "{(}")
                .Replace(")", "{)}")
                .Replace("{", "{{}")
                .Replace("}", "{}}")
                .Replace("[", "{[}")
                .Replace("]", "{]}");
        }
    }
}
