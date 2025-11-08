using System.Runtime.InteropServices;

namespace Sonosk.TrayIcon
{
    public static class User32
    {
        public static void ActivateWindow(nint hwnd)
        {
            var threadId1 = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var threadId2 = GetWindowThreadProcessId(hwnd, IntPtr.Zero);

            if (threadId1 != threadId2)
            {
                AttachThreadInput(threadId1, threadId2, true);
                SetForegroundWindow(hwnd);
                AttachThreadInput(threadId1, threadId2, false);
            }
            else
                SetForegroundWindow(hwnd);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public  static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    }
}
