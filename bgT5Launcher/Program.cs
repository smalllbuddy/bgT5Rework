using System;
using System.Windows.Forms;

namespace bgT5Launcher;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		Application.Run(new MainWindow(args));
	}
}
