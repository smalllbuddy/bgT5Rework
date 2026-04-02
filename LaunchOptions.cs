using System;
using System.Text.RegularExpressions;

namespace bgT5Launcher
{
    internal sealed class LaunchOptions
    {
        public bool DirectLaunchRequested { get; set; }
        public string Mode { get; set; } = "";
        public string HostKind { get; set; } = "";
        public string CustomIp { get; set; } = "";

        public static LaunchOptions Parse(string[] args)
        {
            var o = new LaunchOptions();
            if (args == null || args.Length == 0) return o;

            foreach (var raw in args)
            {
                string a = (raw ?? "").Trim();
                if (a.Length == 0) continue;

                if (a.Equals("-zm", StringComparison.OrdinalIgnoreCase)) { o.Mode = "zm"; o.DirectLaunchRequested = true; }
                else if (a.Equals("-mp", StringComparison.OrdinalIgnoreCase)) { o.Mode = "mp"; o.DirectLaunchRequested = true; }
                else if (a.Equals("-server", StringComparison.OrdinalIgnoreCase)) { o.Mode = "server"; o.DirectLaunchRequested = true; }
                else if (a.Equals("-local", StringComparison.OrdinalIgnoreCase)) { o.HostKind = "local"; o.DirectLaunchRequested = true; }
                else if (a.Equals("-savedip", StringComparison.OrdinalIgnoreCase)) { o.HostKind = "savedip"; o.DirectLaunchRequested = true; }
                else if (Regex.IsMatch(a, @"^-\d{1,3}(\.\d{1,3}){3}$")) { o.HostKind = "custom"; o.CustomIp = a.Substring(1); o.DirectLaunchRequested = true; }
            }
            return o;
        }
    }
}