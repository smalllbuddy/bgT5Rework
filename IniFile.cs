using System.Runtime.InteropServices;
using System.Text;

namespace bgT5Launcher
{
    internal static class IniFile
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(string app, string key, string def, StringBuilder ret, uint size, string path);

        public static void Write(string section, string key, string value, string path)
        {
            WritePrivateProfileString(section, key, value, path);
        }

        public static string Read(string section, string key, string path, string fallback = "")
        {
            var sb = new StringBuilder(512);
            GetPrivateProfileString(section, key, fallback, sb, (uint)sb.Capacity, path);
            return sb.ToString();
        }
    }
}