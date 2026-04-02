using System;
using System.IO;

namespace bgT5Launcher
{
    internal static class Logger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bglauncher-v23.log");

        public static void Log(string s)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {s}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}