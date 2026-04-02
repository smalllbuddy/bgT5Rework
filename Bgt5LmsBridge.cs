using System;
using System.IO;
using System.Reflection;

namespace bgT5Launcher
{
    internal static class Bgt5LmsBridge
    {
        private static bool loaded;
        private static MethodInfo start;
        private static MethodInfo stop;

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            string dll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgT5lms.dll");
            if (!File.Exists(dll))
            {
                Logger.Log("bgT5lms.dll missing");
                return;
            }

            var asm = Assembly.LoadFrom(dll);
            var t = asm.GetType("bgt5lms.BGT5LMS", false);
            if (t == null)
            {
                Logger.Log("BGT5LMS type missing");
                return;
            }

            start = t.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            stop = t.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static);
        }

        public static bool StartHostMode(int port)
        {
            try
            {
                EnsureLoaded();
                if (start == null) return false;
                start.Invoke(null, new object[] { port });
                Logger.Log("Hostmode started");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Hostmode start failed: " + ex);
                return false;
            }
        }

        public static void StopHostMode()
        {
            try
            {
                EnsureLoaded();
                if (stop != null) stop.Invoke(null, null);
                Logger.Log("Hostmode stopped");
            }
            catch (Exception ex)
            {
                Logger.Log("Hostmode stop failed: " + ex);
            }
        }
    }
}