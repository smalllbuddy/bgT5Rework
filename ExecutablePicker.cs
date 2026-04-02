using System.IO;

namespace bgT5Launcher
{
    internal static class ExecutablePicker
    {
        public const long SpPatchedSize = 8099928;
        public const long MpPatchedSize = 8607832;

        public static string PickBySize(string baseDir, long size)
        {
            foreach (var f in Directory.GetFiles(baseDir, "*.exe"))
            {
                try
                {
                    if (new FileInfo(f).Length == size) return f;
                }
                catch
                {
                }
            }
            return null;
        }
    }
}