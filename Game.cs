using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace bgT5Launcher
{
    internal static class Game
    {
        [Flags]
        private enum ProcessAccessFlags : uint { All = 0x1F0FFF }

        [DllImport("kernel32.dll")] private static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr address, byte[] buffer, uint size, out int written);
        [DllImport("kernel32.dll")] private static extern int CloseHandle(IntPtr hObject);

        private static void WriteMem(Process p, int addr, byte value)
        {
            var h = OpenProcess(ProcessAccessFlags.All, false, p.Id);
            if (h == IntPtr.Zero) return;
            try
            {
                int written;
                WriteProcessMemory(h, new IntPtr(addr), new byte[] { value }, 1, out written);
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public static void ApplyOriginalPatch(Process p)
        {
            if (p == null || p.HasExited) return;
            try
            {
                WriteMem(p, 94130980, 1);
                WriteMem(p, 94131056, 1);
                Logger.Log("Patch applied");
            }
            catch (Exception ex)
            {
                Logger.Log("Patch failed: " + ex);
            }
        }

        public static void ApplyPatchWithRetry(Process p)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                for (int i = 0; i < 22; i++)
                {
                    if (p == null || p.HasExited) return;
                    ApplyOriginalPatch(p);
                    Thread.Sleep(350);
                }
            });
        }
    }
}