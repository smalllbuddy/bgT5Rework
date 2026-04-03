using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace bgT5Launcher;

public class Settings : Form
{
	private IContainer components = null;

	private Label label1;

	private TextBox tBgameID;

	private Button bChange;

	[DllImport("kernel32")]
	private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

	public Settings()
	{
		InitializeComponent();
	}

	private void GameID_Load(object sender, EventArgs e)
	{
		StringBuilder stringBuilder = new StringBuilder(500);
		GetPrivateProfileString("Config", "GameID", "", stringBuilder, 256u, ".\\bgset.ini");
		tBgameID.Text = stringBuilder.ToString();
		label1.Select();
	}

	private void bChange_Click(object sender, EventArgs e)
	{
		if (tBgameID.Text != "")
		{
			if (int.TryParse(tBgameID.Text, out var _))
			{
				WritePrivateProfileString("Config", "GameID", tBgameID.Text, ".\\bgset.ini");
				Close();
			}
			else
			{
				MessageBox.Show("ID must be in Int32 format!", "Invalid!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		else
		{
			MessageBox.Show("No GameID inserted! If you came here buy mistake, just close the window.", "Invalid!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
		this.label1 = new System.Windows.Forms.Label();
		this.tBgameID = new System.Windows.Forms.TextBox();
		this.bChange = new System.Windows.Forms.Button();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 9);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(49, 13);
		this.label1.TabIndex = 0;
		this.label1.Text = "GameID:";
		this.tBgameID.BorderStyle = System.Windows.Forms.BorderStyle.None;
		this.tBgameID.Location = new System.Drawing.Point(67, 9);
		this.tBgameID.MaxLength = 10;
		this.tBgameID.Name = "tBgameID";
		this.tBgameID.Size = new System.Drawing.Size(197, 13);
		this.tBgameID.TabIndex = 1;
		this.bChange.BackColor = System.Drawing.Color.Black;
		this.bChange.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
		this.bChange.Font = new System.Drawing.Font("Consolas", 9.75f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
		this.bChange.ForeColor = System.Drawing.Color.FromArgb(0, 192, 0);
		this.bChange.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
		this.bChange.Location = new System.Drawing.Point(270, 4);
		this.bChange.Name = "bChange";
		this.bChange.Size = new System.Drawing.Size(19, 23);
		this.bChange.TabIndex = 2;
		this.bChange.Text = "✔";
		this.bChange.UseVisualStyleBackColor = false;
		this.bChange.Click += new System.EventHandler(bChange_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = System.Drawing.Color.Silver;
		base.ClientSize = new System.Drawing.Size(301, 32);
		base.Controls.Add(this.bChange);
		base.Controls.Add(this.tBgameID);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
		base.Name = "Settings";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Set GameID";
		base.TopMost = true;
		base.Load += new System.EventHandler(GameID_Load);
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
