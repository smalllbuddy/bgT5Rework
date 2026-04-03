using System;
using System.IO;
using System.Reflection;

namespace bgt5lms
{
    public static class BGT5LMS
    {
        private static Type cachedType;
        private static MethodInfo startMethod;
        private static MethodInfo stopMethod;
        private static bool attemptedLoad;

        private static void EnsureLoaded()
        {
            if (attemptedLoad) return;
            attemptedLoad = true;

            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgT5lms.dll");
            if (!File.Exists(dllPath))
                throw new FileNotFoundException("bgT5lms.dll was not found.", dllPath);

            Assembly asm = Assembly.LoadFrom(dllPath);
            cachedType = asm.GetType("bgt5lms.BGT5LMS", throwOnError: false, ignoreCase: false);
            if (cachedType == null)
                throw new InvalidOperationException("Type bgt5lms.BGT5LMS was not found in bgT5lms.dll.");

            startMethod = cachedType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            stopMethod = cachedType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Static);
            if (startMethod == null || stopMethod == null)
                throw new InvalidOperationException("Expected Start(int) / Stop() methods were not found in bgT5lms.dll.");
        }

        public static void Start(int port)
        {
            EnsureLoaded();
            startMethod.Invoke(null, new object[] { port });
        }

        public static void Stop()
        {
            EnsureLoaded();
            stopMethod.Invoke(null, null);
        }
    }
}
