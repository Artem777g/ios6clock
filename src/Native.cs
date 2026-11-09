using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace iOS6Clock
{
    internal static class Native
    {
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int LWA_ALPHA = 2;
        public const int ABM_GETTASKBARPOS = 5;
        public const int ABM_GETSTATE = 4;
        public const int ABS_AUTOHIDE = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
            public Rectangle ToRect() { return Rectangle.FromLTRB(Left, Top, Right, Bottom); }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string className, string windowName);

        public delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr parent, EnumChildProc proc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hwnd, StringBuilder sb, int max);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint key, byte alpha, uint flags);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr icon);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT p);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHAppBarMessage(int message, ref APPBARDATA data);

        [DllImport("shell32.dll")]
        public static extern int SHQueryUserNotificationState(out int state);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);
    }

    internal sealed class TaskbarInfo
    {
        public Native.RECT Rect;
        public int Edge;          // 0 left, 1 top, 2 right, 3 bottom
        public bool AutoHide;

        public static TaskbarInfo Get()
        {
            var info = new TaskbarInfo();

            IntPtr hwnd = Native.FindWindow("Shell_TrayWnd", null);
            if (hwnd != IntPtr.Zero)
            {
                Native.RECT r;
                if (Native.GetWindowRect(hwnd, out r))
                {
                    info.Rect = r;
                    Rectangle b = Screen.FromHandle(hwnd).Bounds;
                    if (r.Width >= r.Height)
                        info.Edge = r.Top + r.Height / 2 < b.Top + b.Height / 2 ? 1 : 3;
                    else
                        info.Edge = r.Left + r.Width / 2 < b.Left + b.Width / 2 ? 0 : 2;
                }
            }

            var state = new Native.APPBARDATA();
            state.cbSize = Marshal.SizeOf(typeof(Native.APPBARDATA));
            IntPtr flags = Native.SHAppBarMessage(Native.ABM_GETSTATE, ref state);
            info.AutoHide = (flags.ToInt32() & Native.ABS_AUTOHIDE) != 0;
            return info;
        }
    }

    internal static class ClockLocator
    {
        // Windows 7 keeps the tray clock in a small child window of the taskbar.
        // If it cannot be found (or the user hid the clock), there is nothing to
        // intercept — the app is then used through its tray icon.
        public static Rectangle Find(float scale)
        {
            IntPtr tray = Native.FindWindow("Shell_TrayWnd", null);
            IntPtr clock = IntPtr.Zero;
            if (tray != IntPtr.Zero)
            {
                Native.EnumChildWindows(tray, delegate(IntPtr h, IntPtr l)
                {
                    var sb = new StringBuilder(128);
                    Native.GetClassName(h, sb, sb.Capacity);
                    if (string.Equals(sb.ToString(), "TrayClockWClass", StringComparison.OrdinalIgnoreCase))
                    {
                        clock = h;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            if (clock != IntPtr.Zero && Native.IsWindowVisible(clock))
            {
                Native.RECT r;
                if (Native.GetWindowRect(clock, out r) && r.Width > 4 && r.Height > 4)
                    return r.ToRect();
            }
            return Rectangle.Empty;
        }
    }

    internal static class Fullscreen
    {
        public static bool IsBusy()
        {
            int state;
            if (Native.SHQueryUserNotificationState(out state) != 0)
                return false;
            return state == 2 || state == 3 || state == 4; // busy, d3d fullscreen, presentation
        }
    }
}
