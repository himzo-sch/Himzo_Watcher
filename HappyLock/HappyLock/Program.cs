using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.IO; // Added for file logging

namespace HappyLock
{
    class Program
    {
        // --- CONFIGURATION ---
        // 1. VERIFY THIS NAME (Task Manager -> Details -> Name column)
        const string TargetProcessName = "happylan";

        // 2. VERIFY THIS PATH (Shift + Right Click the real .exe -> Copy as path)
        const string TargetPath = @"C:\Program Files (x86)\HAPPY\Happy Lan\happylan.exe";

        // Log file location
        const string LogPath = @"C:\HimzoNoti\happylock_log.txt";

        // --- STATE ---
        private static bool _isShowingAlert = false;
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        [STAThread] // Important for Windows Forms
        public static void Main()
        {
            try
            {
                // Simple file logging to prove it started
                Log("HappyLock starting...");

                // 1. Fix DPI
                try { SetProcessDPIAware(); } catch { Log("Warning: Could not set DPI awareness."); }

                // 2. Start Watchdog (Check if app is missing)
                Task.Run(() => AppWatchdogLoop());

                // 3. Install Hook
                _hookID = SetHook(_proc);
                Log("Hook installed.");

                // 4. Run loop
                Application.Run();

                // 5. Cleanup
                UnhookWindowsHookEx(_hookID);
            }
            catch (Exception ex)
            {
                // THIS IS THE CRITICAL PART
                // If it crashes, it will write the reason here:
                Log("CRITICAL ERROR: " + ex.Message + "\n" + ex.StackTrace);
                MessageBox.Show("HappyLock Crashed:\n" + ex.Message);
            }
        }

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath, DateTime.Now + ": " + msg + Environment.NewLine);
            }
            catch { /* If we can't write a log, we are truly doomed */ }
        }

        private static async Task AppWatchdogLoop()
        {
            while (true)
            {
                try
                {
                    if (Process.GetProcessesByName(TargetProcessName).Length == 0)
                    {
                        Log("HappyLan not found. Attempting to launch...");

                        if (File.Exists(TargetPath))
                        {
                            Process.Start(TargetPath);
                            Log("Launch command sent.");

                            // Wait 10 seconds for it to start up
                            await Task.Delay(10000);
                        }
                        else
                        {
                            Log("ERROR: File not found at " + TargetPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Watchdog Error: " + ex.Message);
                }
                await Task.Delay(2000);
            }
        }

        // --- MOUSE HOOK LOGIC (UNCHANGED) ---
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (MouseMessages)wParam == MouseMessages.WM_LBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                IntPtr hWndUnderMouse = WindowFromPoint(new Point(hookStruct.pt.x, hookStruct.pt.y));

                if (IsClickingCloseButton(hWndUnderMouse, hookStruct.pt.x, hookStruct.pt.y))
                {
                    if (!_isShowingAlert)
                    {
                        _isShowingAlert = true;
                        Task.Run(() =>
                        {
                            EnableWindow(hWndUnderMouse, false);
                            MessageBox.Show(new WindowWrapper(hWndUnderMouse),
                                            "No you don't! The embroidery machine is connected.",
                                            "HappyLock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            EnableWindow(hWndUnderMouse, true);
                            SetForegroundWindow(hWndUnderMouse);
                            _isShowingAlert = false;
                        });
                    }
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static bool IsClickingCloseButton(IntPtr hWnd, int mouseX, int mouseY)
        {
            if (hWnd == IntPtr.Zero) return false;
            GetWindowThreadProcessId(hWnd, out uint pid);
            try
            {
                Process p = Process.GetProcessById((int)pid);
                if (p.ProcessName.Equals(TargetProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    // Check 1: System HitTest
                    int hitTest = SendMessage(hWnd, 0x0084, IntPtr.Zero, MakeLParam(mouseX, mouseY));
                    if (hitTest == 20) return true;

                    // Check 2: Top-Right Corner Fallback
                    RECT windowRect;
                    GetWindowRect(hWnd, out windowRect);
                    int localX = mouseX - windowRect.Left;
                    int localY = mouseY - windowRect.Top;
                    int width = windowRect.Right - windowRect.Left;

                    // Danger Zone: Top Right 60x40 pixels
                    if (localX > (width - 60) && localY >= 0 && localY < 40) return true;
                }
            }
            catch { }
            return false;
        }

        // --- BOILERPLATE (UNCHANGED) ---
        public class WindowWrapper : IWin32Window
        {
            public WindowWrapper(IntPtr handle) { _hwnd = handle; }
            public IntPtr Handle { get { return _hwnd; } }
            private IntPtr _hwnd;
        }

        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(14, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        private enum MouseMessages { WM_LBUTTONDOWN = 0x0201 }
        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int x; public int y; }
        [StructLayout(LayoutKind.Sequential)] private struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData; public uint flags; public uint time; public IntPtr dwExtraInfo; }
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point Point);
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, int lParam);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        static int MakeLParam(int x, int y) => (y << 16) | (x & 0xFFFF);
    }
}