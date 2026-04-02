using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace bgT5Launcher
{
    internal static class DirectLauncher
    {
        public static bool RunAndGetProcess(LaunchOptions options, out Process launchedProcess)
        {
            launchedProcess = null;

            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgset.ini");
            string savedHost = IniFile.Read("Config", "Host", iniPath, "127.0.0.1");
            string nick = IniFile.Read("Config", "Nickname", iniPath, "Player");

            if (!Bgt5LmsBridge.StartHostMode(3074))
                Logger.Log("Direct launch: hostmode did not confirm active.");

            Thread.Sleep(250);

            string host = ResolveHost(options, savedHost);
            IniFile.Write("Config", "Host", host, iniPath);
            IniFile.Write("Config", "Nickname", nick, iniPath);

            if (options.Mode == "zm")
            {
                string exe = ExecutablePicker.PickBySize(AppDomain.CurrentDomain.BaseDirectory, ExecutablePicker.SpPatchedSize);
                if (exe == null) throw new FileNotFoundException("Could not find the patched SP executable by expected file size.");
                launchedProcess = Process.Start(exe);
            }
            else if (options.Mode == "mp")
            {
                string exe = ExecutablePicker.PickBySize(AppDomain.CurrentDomain.BaseDirectory, ExecutablePicker.MpPatchedSize);
                if (exe == null) throw new FileNotFoundException("Could not find the patched MP executable by expected file size.");
                launchedProcess = Process.Start(exe);
            }
            else if (options.Mode == "server")
            {
                string exe = ExecutablePicker.PickBySize(AppDomain.CurrentDomain.BaseDirectory, ExecutablePicker.MpPatchedSize);
                if (exe == null) throw new FileNotFoundException("Could not find the patched MP executable by expected file size.");
                string args = " +set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec bgserver.cfg";
                launchedProcess = Process.Start(exe, args);
            }
            else
            {
                throw new ArgumentException("Use -zm, -mp, or -server.");
            }

            if (launchedProcess != null)
                Game.ApplyPatchWithRetry(launchedProcess);

            return launchedProcess != null;
        }

        private static string ResolveHost(LaunchOptions options, string savedHost)
        {
            if (string.Equals(options.HostKind, "local", StringComparison.OrdinalIgnoreCase))
                return "127.0.0.1";
            if (string.Equals(options.HostKind, "savedip", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(options.HostKind))
                return savedHost;
            if (string.Equals(options.HostKind, "custom", StringComparison.OrdinalIgnoreCase))
                return options.CustomIp.Trim();
            return savedHost;
        }
    }
}