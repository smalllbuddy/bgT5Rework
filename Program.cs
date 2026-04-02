using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace bgT5Launcher
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var options = LaunchOptions.Parse(args);
            if (options.DirectLaunchRequested)
            {
                try
                {
                    Process launched;
                    if (DirectLauncher.RunAndGetProcess(options, out launched))
                    {
                        Application.Run(new MainWindow(true, launched));
                        return;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Direct launch error");
                    return;
                }
            }

            Application.Run(new MainWindow(false, null));
        }
    }
}