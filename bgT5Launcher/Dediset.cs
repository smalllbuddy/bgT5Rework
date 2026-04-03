
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace bgT5Launcher;

public class Dediset : Form
{
    private IContainer components = null;
    private Button button1;

    public Dediset()
    {
        InitializeComponent();
    }

    private static string? FindExecutableBySize(long expectedSize, params string[] preferredNames)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        foreach (string name in preferredNames)
        {
            string path = Path.Combine(baseDir, name);
            if (File.Exists(path) && new FileInfo(path).Length == expectedSize)
                return path;
        }
        foreach (string exe in Directory.GetFiles(baseDir, "*.exe"))
        {
            try
            {
                if (new FileInfo(exe).Length == expectedSize)
                    return exe;
            }
            catch { }
        }
        return null;
    }

    private void button1_Click(object sender, EventArgs e)
    {
        string? exe = FindExecutableBySize(8607832L, "BGamerT5MP.exe", "BlackOpsMP.exe");
        if (exe == null)
        {
            MessageBox.Show("Could not find a matching MP executable.", "Error");
            return;
        }

        string cfg = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.cfg")) ? "server.cfg" :
                     File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgserver.cfg")) ? "bgserver.cfg" :
                     "server.cfg";
        Process.Start(exe, $"+set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec {cfg}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        button1 = new Button();
        SuspendLayout();
        button1.Text = "Start Dedicated";
        button1.Dock = DockStyle.Fill;
        button1.Click += button1_Click;
        Controls.Add(button1);
        Text = "Dedicated";
        ClientSize = new System.Drawing.Size(260, 60);
        ResumeLayout(false);
    }
}
