using Sonosk.ViewModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sonosk.TrayIcon
{
    public class TrayIconService
    {
        private readonly MainViewModel mainViewModel;
        private nint hwnd;
        private uint trayIconId = 1; // must match what you pass into AddTrayIcon
        private static nint hookId = nint.Zero;
        private static LowLevelMouseProc proc;

        public TrayIconService(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        public void Show(nint handle)
        {
            hwnd = handle;
            TrayIconHelper.AddTrayIcon(handle, "SONOSKontroller");
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);
            proc = HookCallback;
            hookId = SetWindowsHookEx(WH_MOUSE_LL, proc, Process.GetCurrentProcess().MainModule!.BaseAddress, 0);
        }

        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WH_MOUSE_LL = 14;

        private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == TrayIconHelper.WM_USER)
            {
                switch ((int)lParam)
                {
                    case WM_LBUTTONDOWN:
                        Clicked?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case WM_RBUTTONDOWN:
                        RightClicked?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                }

                return 1;
            }
            return nint.Zero;
        }

        private nint HookCallback(int nCode, nint wParam, nint lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == WM_MOUSEWHEEL)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam)!;
                int delta = (short)(hookStruct.mouseData >> 16 & 0xffff);

                var cursor = new Point(hookStruct.pt.x, hookStruct.pt.y);
                if (IsMouseOverTrayIcon(cursor))
                {
                    if (delta > 0) mainViewModel.IncreaseVolume(1);
                    else if (delta < 0) mainViewModel.DecreaseVolume(1);
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private bool IsMouseOverTrayIcon(Point cursor)
        {
            var nid = new NOTIFYICONIDENTIFIER
            {
                cbSize = Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
                hWnd = hwnd,
                uID = (int)trayIconId
            };

            if (Shell_NotifyIconGetRect(ref nid, out RECT rect) == 0)
            {
                return cursor.X >= rect.Left && cursor.X <= rect.Right &&
                       cursor.Y >= rect.Top && cursor.Y <= rect.Bottom;
            }
            return false;
        }

        public event EventHandler<EventArgs> Clicked;
        public event EventHandler<EventArgs> RightClicked;

        #region Native

        private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

        [DllImport("shell32.dll")]
        private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONIDENTIFIER
        {
            public int cbSize;
            public nint hWnd;
            public int uID;
            public Guid guidItem;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        #endregion
    }
}
