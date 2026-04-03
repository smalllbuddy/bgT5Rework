
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using bgT5Launcher.Properties;
using bgt5lms;

namespace bgT5Launcher;

public class LoadG : Form
{
    private IContainer components = null;
    private Label label1;

    public LoadG()
    {
        InitializeComponent();
        BackgroundImageLayout = ImageLayout.Stretch;
        BackgroundImage = Resources.BOlolgo;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
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
        label1 = new Label();
        SuspendLayout();
        label1.AutoSize = true;
        label1.BackColor = Color.Transparent;
        label1.Font = new Font("Consolas", 8.25f);
        label1.ForeColor = Color.White;
        label1.Location = new Point(12, 9);
        label1.Text = "Loading....";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(500, 226);
        Controls.Add(label1);
        FormBorderStyle = FormBorderStyle.None;
        Name = "LoadG";
        Text = "Load";
        ResumeLayout(false);
        PerformLayout();
    }
}
