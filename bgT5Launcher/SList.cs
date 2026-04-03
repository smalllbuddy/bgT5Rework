using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace bgT5Launcher;

public class SList : Form
{
	private const int listenPort = 11000;

	private MainWindow mMainWindow = null;

	private IContainer components = null;

	private ListBox LBservers;

	private Button Brefresh;

	private Button Bconnect;

	private Label L1;

	private Label Lsnum;

	private TextBox textBox1;

	public SList(MainWindow f)
	{
		InitializeComponent();
		mMainWindow = f;
		new Thread((ThreadStart)delegate
		{
			try
			{
				StartListener();
			}
			catch (Exception)
			{
				Console.WriteLine("Error");
			}
		}).Start();
	}

	private void StartListener()
	{
		UdpClient udpClient = new UdpClient(11000);
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 11000);
		try
		{
			int num = 0;
			while (num < 1)
			{
				byte[] array = udpClient.Receive(ref remoteEP);
				string text = remoteEP.ToString().Remove(remoteEP.ToString().LastIndexOf(":"));
				string item = "HostIP: " + text + " | Connected Clients: " + Encoding.ASCII.GetString(array, 0, array.Length) + "\n";
				int num2 = LBservers.FindString("HostIP: " + text);
				if (num2 == -1)
				{
					Lsnum.Text = (Convert.ToInt32(Lsnum.Text) + 1).ToString();
					LBservers.Items.Add(item);
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
		finally
		{
			udpClient.Close();
		}
	}

	private void Brefresh_Click(object sender, EventArgs e)
	{
		Lsnum.Text = "0";
		LBservers.Items.Clear();
	}

	private void SList_Closing(object sender, EventArgs e)
	{
	}

	private void Bconnect_Click(object sender, EventArgs e)
	{
		try
		{
			string text = LBservers.SelectedItem.ToString();
			text = text.Remove(text.LastIndexOf("|") - 1);
			text = text.Remove(0, 8);
			mMainWindow.changeIP(text);
		}
		catch
		{
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.LBservers = new System.Windows.Forms.ListBox();
		this.Brefresh = new System.Windows.Forms.Button();
		this.Bconnect = new System.Windows.Forms.Button();
		this.L1 = new System.Windows.Forms.Label();
		this.Lsnum = new System.Windows.Forms.Label();
		this.textBox1 = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.LBservers.FormattingEnabled = true;
		this.LBservers.Location = new System.Drawing.Point(12, 12);
		this.LBservers.Name = "LBservers";
		this.LBservers.Size = new System.Drawing.Size(280, 95);
		this.LBservers.TabIndex = 0;
		this.Brefresh.Location = new System.Drawing.Point(136, 113);
		this.Brefresh.Name = "Brefresh";
		this.Brefresh.Size = new System.Drawing.Size(75, 23);
		this.Brefresh.TabIndex = 1;
		this.Brefresh.Text = "Refresh";
		this.Brefresh.UseVisualStyleBackColor = true;
		this.Brefresh.Click += new System.EventHandler(Brefresh_Click);
		this.Bconnect.Location = new System.Drawing.Point(217, 113);
		this.Bconnect.Name = "Bconnect";
		this.Bconnect.Size = new System.Drawing.Size(75, 23);
		this.Bconnect.TabIndex = 2;
		this.Bconnect.Text = "Connect";
		this.Bconnect.UseVisualStyleBackColor = true;
		this.Bconnect.Click += new System.EventHandler(Bconnect_Click);
		this.L1.AutoSize = true;
		this.L1.Location = new System.Drawing.Point(12, 113);
		this.L1.Name = "L1";
		this.L1.Size = new System.Drawing.Size(55, 13);
		this.L1.TabIndex = 3;
		this.L1.Text = "Servers:";
		this.Lsnum.AutoSize = true;
		this.Lsnum.Location = new System.Drawing.Point(64, 113);
		this.Lsnum.Name = "Lsnum";
		this.Lsnum.Size = new System.Drawing.Size(13, 13);
		this.Lsnum.TabIndex = 4;
		this.Lsnum.Text = "0";
		this.textBox1.Location = new System.Drawing.Point(97, -1);
		this.textBox1.Name = "textBox1";
		this.textBox1.Size = new System.Drawing.Size(100, 20);
		this.textBox1.TabIndex = 5;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.Silver;
		base.ClientSize = new System.Drawing.Size(300, 136);
		base.ControlBox = false;
		base.Controls.Add(this.textBox1);
		base.Controls.Add(this.Lsnum);
		base.Controls.Add(this.L1);
		base.Controls.Add(this.Bconnect);
		base.Controls.Add(this.Brefresh);
		base.Controls.Add(this.LBservers);
		this.Font = new System.Drawing.Font("Consolas", 8.25f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.ForeColor = System.Drawing.Color.White;
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "SList";
		base.Opacity = 0.9;
		base.ShowIcon = false;
		base.ShowInTaskbar = false;
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Server List";
		base.TopMost = true;
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(SList_Closing);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
