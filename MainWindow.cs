using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace bgT5Launcher
{

    internal sealed class NumberTickerControl : Control
    {
        private readonly Random rng = new Random();
        private readonly Timer timer;
        private readonly string[] lines = new string[4];
        private int shift;

        public NumberTickerControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Opaque, true);

            BackColor = Color.FromArgb(8, 8, 8);

            timer = new Timer();
            timer.Interval = 520;
            timer.Tick += (s, e) =>
            {
                Generate();
                shift = (shift + 1) % 4;
                Invalidate();
            };

            Generate();
        }

        public void StartTicker() => timer.Start();
        public void StopTicker() => timer.Stop();

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        private void Generate()
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int groups = 7 + rng.Next(3);
                string line = "";
                for (int g = 0; g < groups; g++)
                    line += rng.Next(0, 9999).ToString("0000") + " ";
                lines[i] = line.TrimEnd();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            using (var bg = new SolidBrush(Color.FromArgb(8, 8, 8)))
                g.FillRectangle(bg, ClientRectangle);

            using (var orange = new SolidBrush(Color.FromArgb(70, 255, 140, 0)))
            using (var dim = new SolidBrush(Color.FromArgb(34, 210, 120, 30)))
            using (var font = new Font("Consolas", 11f, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                int lineHeight = 26;
                int top = 10;
                int baseX = 10;

                for (int i = 0; i < lines.Length; i++)
                {
                    int x = baseX + (i * 10) + ((shift + i) % 4) * 3;
                    int y = top + i * lineHeight;
                    g.DrawString(lines[i], font, i % 2 == 0 ? orange : dim, x, y);
                }
            }
        }
    }

    internal sealed class T5LaunchButton : Control
    {
        private bool hovered;
        private bool pressed;

        public T5LaunchButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);

            BackColor = Color.FromArgb(20, 20, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Size = new Size(230, 68);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool wasPressed = pressed;
            pressed = false;
            Invalidate();
            if (wasPressed && ClientRectangle.Contains(e.Location))
                OnClick(EventArgs.Empty);
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle r = ClientRectangle;
            Color fill = pressed
                ? Color.FromArgb(34, 34, 34)
                : hovered ? Color.FromArgb(26, 26, 26) : Color.FromArgb(20, 20, 20);

            using (var bg = new SolidBrush(fill))
                g.FillRectangle(bg, r);

            Rectangle tex = new Rectangle(6, 6, Math.Max(0, Width - 12), Math.Max(0, Height - 12));
            using (var clip = new Region(tex))
            {
                Region oldClip = g.Clip;
                g.Clip = clip;

                using (var wash = new SolidBrush(Color.FromArgb(12, 255, 128, 20)))
                    g.FillRectangle(wash, tex);

                using (var font = new Font("Consolas", 8f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var orange = new SolidBrush(Color.FromArgb(18, 255, 140, 0)))
                using (var gray = new SolidBrush(Color.FromArgb(8, 170, 170, 170)))
                {
                    int y = tex.Top + 4;
                    int line = 0;
                    while (y < tex.Bottom - 8)
                    {
                        string txt = (line % 2 == 0)
                            ? "115 935 281 04 22 77 311 5 8 13 21"
                            : "9 11 3 17 52 07 21 5 86 40 27 44 12";
                        int x = tex.Left + 4;
                        while (x < tex.Right + 140)
                        {
                            g.DrawString(txt, font, line % 2 == 0 ? orange : gray, x, y);
                            x += 116;
                        }
                        y += 9;
                        line++;
                    }
                }

                g.Clip = oldClip;
            }

            using (var border = new Pen(Color.FromArgb(220, 255, 120, 0), 1))
                g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

            SizeF sz = g.MeasureString(Text, Font);
            float tx = (Width - sz.Width) / 2f;
            float ty = (Height - sz.Height) / 2f;
            using (var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.DrawString(Text, Font, shadow, tx + 1, ty + 1);
            using (var textBrush = new SolidBrush(ForeColor))
                g.DrawString(Text, Font, textBrush, tx, ty);
        }
    }

    public class MainWindow : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int WM_SETICON = 0x80;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private static readonly IntPtr ICON_SMALL = new IntPtr(0);
        private static readonly IntPtr ICON_BIG = new IntPtr(1);

        private readonly string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgset.ini");
        private Process launched;
        private Timer retry;
        private Timer gameWatchTimer;
        private NotifyIcon trayIcon;
        private bool startHiddenToTray;
        private bool isTrayMode;
        private bool launchInProgress;
        private string launchedProcessName = "";
        private Icon launcherIcon;

        private Panel card;
        private Label title;
        private Label subtitle;
        private Label hostLabel;
        private Label nickLabel;
        private Label statusLabel;
        private TextBox hostBox;
        private TextBox nickBox;
        private T5LaunchButton zcButton;
        private T5LaunchButton mpButton;
        private T5LaunchButton dediButton;
        private Button setLoopbackButton;
        private FlowLayoutPanel ipButtonsPanel;
        private Button launchOptionsButton;
        private NumberTickerControl ticker;

        public MainWindow(bool startHiddenToTray, Process existingGameProcess)
        {
            this.startHiddenToTray = startHiddenToTray;
            this.launched = existingGameProcess;

            try
            {
                launcherIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (launcherIcon == null)
                    launcherIcon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.ico"));
                this.Icon = launcherIcon;
            }
            catch
            {
                try
                {
                    launcherIcon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.ico"));
                    this.Icon = launcherIcon;
                }
                catch
                {
                }
            }

            InitializeComponent();

            hostBox.Text = IniFile.Read("Config", "Host", iniPath, "127.0.0.1");
            if (string.IsNullOrWhiteSpace(hostBox.Text)) hostBox.Text = "127.0.0.1";
            nickBox.Text = IniFile.Read("Config", "Nickname", iniPath, "Player");

            BuildIpButtons();

            Shown += (s, e) =>
            {
                ForceWindowIcons();
                ticker.StartTicker();
                StartHostmode();
                retry.Start();

                if (launched != null)
                {
                    try { launchedProcessName = launched.ProcessName; } catch { }
                    gameWatchTimer.Start();
                    if (this.startHiddenToTray)
                        HideToTray();
                }
                launchInProgress = false;
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ForceWindowIcons();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;

            if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_MINIMIZE)
            {
                HideToTray();
                return;
            }

            base.WndProc(ref m);
        }


        private bool TryReattachLaunchedProcess()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(launchedProcessName))
                    return false;

                Process[] procs = Process.GetProcessesByName(launchedProcessName);
                foreach (Process p in procs)
                {
                    try
                    {
                        if (p == null || p.HasExited) continue;
                        if (p.Id == Process.GetCurrentProcess().Id) continue;
                        launched = p;
                        return true;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private void ForceWindowIcons()
        {
            try
            {
                if (launcherIcon != null && IsHandleCreated)
                {
                    this.Icon = launcherIcon;
                    SendMessage(this.Handle, WM_SETICON, ICON_SMALL, launcherIcon.Handle);
                    SendMessage(this.Handle, WM_SETICON, ICON_BIG, launcherIcon.Handle);
                    if (trayIcon != null) trayIcon.Icon = launcherIcon;
                }
            }
            catch
            {
            }
        }

        private void InitializeComponent()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            UpdateStyles();

            Text = "bgT5Launcher";
            ClientSize = new Size(980, 620);
            MinimumSize = new Size(980, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            BackColor = Color.FromArgb(14, 14, 14);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            DoubleBuffered = true;
            ShowIcon = true;

            retry = new Timer();
            retry.Interval = 900;
            retry.Tick += (s, e) =>
            {
                if (statusLabel.Text != "HOSTMODE ACTIVE") StartHostmode();
                else retry.Stop();
            };

            gameWatchTimer = new Timer();
            gameWatchTimer.Interval = 750;
            gameWatchTimer.Tick += GameWatchTimer_Tick;

            trayIcon = new NotifyIcon();
            trayIcon.Text = "bgT5Launcher";
            trayIcon.Visible = false;
            try { trayIcon.Icon = launcherIcon ?? this.Icon; } catch { }
            trayIcon.DoubleClick += (s, e) => RestoreFromTray();

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show launcher", null, (s, e) => RestoreFromTray());
            trayMenu.Items.Add("Exit launcher", null, (s, e) => Close());
            trayIcon.ContextMenuStrip = trayMenu;

            card = new Panel();
            card.Location = new Point(24, 24);
            card.Size = new Size(932, 572);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            card.BackColor = Color.FromArgb(150, 8, 8, 8);
            card.BorderStyle = BorderStyle.FixedSingle;

            title = new Label();
            title.Text = "BLACK OPS // T5 LAUNCHER";
            title.ForeColor = Color.Orange;
            title.BackColor = Color.Transparent;
            title.Font = new Font("Consolas", 24f, FontStyle.Bold);
            title.AutoSize = true;

            subtitle = new Label();
            subtitle.Text = "session bridge online // reworked launcher for bgt5launcher v0.1.1";
            subtitle.ForeColor = Color.Gainsboro;
            subtitle.BackColor = Color.Transparent;
            subtitle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            subtitle.AutoSize = true;

            statusLabel = new Label();
            statusLabel.Text = "starting hostmode...";
            statusLabel.ForeColor = Color.FromArgb(230, 170, 96);
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            statusLabel.AutoSize = true;
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            launchOptionsButton = MakeSystemButton("Launch options", 120, 32);
            launchOptionsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            launchOptionsButton.Click += (s, e) => ShowLaunchOptions();

            hostLabel = new Label();
            hostLabel.Text = "Host IP";
            hostLabel.ForeColor = Color.White;
            hostLabel.BackColor = Color.Transparent;
            hostLabel.AutoSize = true;

            hostBox = new TextBox();
            hostBox.BackColor = Color.FromArgb(24, 24, 24);
            hostBox.ForeColor = Color.White;
            hostBox.BorderStyle = BorderStyle.FixedSingle;

            setLoopbackButton = MakeSystemButton("Set 127.0.0.1", 130, 32);
            setLoopbackButton.Click += (s, e) => hostBox.Text = "127.0.0.1";

            nickLabel = new Label();
            nickLabel.Text = "Nickname";
            nickLabel.ForeColor = Color.White;
            nickLabel.BackColor = Color.Transparent;
            nickLabel.AutoSize = true;

            nickBox = new TextBox();
            nickBox.BackColor = Color.FromArgb(24, 24, 24);
            nickBox.ForeColor = Color.White;
            nickBox.BorderStyle = BorderStyle.FixedSingle;

            ipButtonsPanel = new FlowLayoutPanel();
            ipButtonsPanel.BackColor = Color.Transparent;
            ipButtonsPanel.AutoScroll = true;
            ipButtonsPanel.WrapContents = true;

            ticker = new NumberTickerControl();

            zcButton = new T5LaunchButton { Text = "START ZOMBIES / CAMPAIGN" };
            mpButton = new T5LaunchButton { Text = "START MULTIPLAYER" };
            dediButton = new T5LaunchButton { Text = "START DEDICATED" };

            zcButton.Click += (s, e) => LaunchAndHide(ExecutablePicker.SpPatchedSize, false);
            mpButton.Click += (s, e) => LaunchAndHide(ExecutablePicker.MpPatchedSize, false);
            dediButton.Click += (s, e) => LaunchAndHide(ExecutablePicker.MpPatchedSize, true);

            card.Controls.Add(title);
            card.Controls.Add(subtitle);
            card.Controls.Add(statusLabel);
            card.Controls.Add(launchOptionsButton);
            card.Controls.Add(hostLabel);
            card.Controls.Add(hostBox);
            card.Controls.Add(setLoopbackButton);
            card.Controls.Add(nickLabel);
            card.Controls.Add(nickBox);
            card.Controls.Add(ipButtonsPanel);
            card.Controls.Add(ticker);
            card.Controls.Add(zcButton);
            card.Controls.Add(mpButton);
            card.Controls.Add(dediButton);

            Controls.Add(card);

            Resize += (s, e) => LayoutDynamic();
            FormClosing += OnFormClosing;
            LayoutDynamic();
        }

        private Button MakeSystemButton(string text, int w, int h)
        {
            var b = new Button();
            b.Text = text;
            b.Width = w;
            b.Height = h;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(220, 255, 120, 0);
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 120, 0);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(56, 255, 120, 0);
            b.BackColor = Color.FromArgb(20, 20, 20);
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            b.UseCompatibleTextRendering = true;
            return b;
        }

        private void GameWatchTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (launched == null)
                {
                    if (TryReattachLaunchedProcess())
                        return;

                    if (isTrayMode)
                    {
                        gameWatchTimer.Stop();
                        RestoreFromTray();
                        statusLabel.Text = "HOSTMODE ACTIVE";
                        statusLabel.ForeColor = Color.FromArgb(255, 190, 120);
                    }
                    return;
                }

                if (launched.HasExited)
                {
                    launched = null;
                    if (TryReattachLaunchedProcess())
                        return;

                    if (isTrayMode)
                    {
                        gameWatchTimer.Stop();
                        RestoreFromTray();
                        statusLabel.Text = "HOSTMODE ACTIVE";
                        statusLabel.ForeColor = Color.FromArgb(255, 190, 120);
                    }
                }
            }
            catch
            {
            }
        }

        private void HideToTray()
        {
            if (IsDisposed) return;

            isTrayMode = true;

            if (trayIcon != null)
            {
                try { trayIcon.Icon = launcherIcon ?? this.Icon; } catch { }
                trayIcon.Visible = true;
            }

            card.Visible = false;
            ShowInTaskbar = false;
            if (IsHandleCreated)
                ShowWindow(this.Handle, SW_HIDE);
            else
                Visible = false;
        }

        private void RestoreFromTray()
        {
            if (IsDisposed) return;
            isTrayMode = false;
            if (trayIcon != null) trayIcon.Visible = false;
            ShowInTaskbar = true;
            if (IsHandleCreated)
                ShowWindow(this.Handle, SW_SHOW);
            else
                Show();
            card.Visible = true;
            WindowState = FormWindowState.Normal;
            ForceWindowIcons();
            Activate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try { ticker.StopTicker(); } catch { }
            try { if (gameWatchTimer != null) { gameWatchTimer.Stop(); gameWatchTimer.Dispose(); } } catch { }
            try { if (retry != null) { retry.Stop(); retry.Dispose(); } } catch { }
            try { if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); } } catch { }
            try { if (launcherIcon != null) launcherIcon.Dispose(); } catch { }
            try { Bgt5LmsBridge.StopHostMode(); } catch { }
            try { Application.ExitThread(); } catch { }
        }

        private void BuildIpButtons()
        {
            ipButtonsPanel.SuspendLayout();
            ipButtonsPanel.Controls.Clear();
            foreach (var ip in NetUtil.GetLocalIPv4s())
            {
                var b = MakeSystemButton("Copy local IPv4: " + ip, 190, 32);
                b.AutoSize = true;
                b.Margin = new Padding(0, 0, 10, 10);
                b.Click += (s, e) => { try { Clipboard.SetText(ip); } catch { } };
                ipButtonsPanel.Controls.Add(b);
            }
            ipButtonsPanel.ResumeLayout();
        }

        private void LayoutDynamic()
        {
            int cw = card.ClientSize.Width;
            int ch = card.ClientSize.Height;

            title.Location = new Point(24, 18);
            subtitle.Location = new Point(28, 58);

            launchOptionsButton.Location = new Point(cw - 148, 20);
            statusLabel.Location = new Point(Math.Max(480, cw - 300), 58);

            hostLabel.Location = new Point(30, 112);
            hostBox.Location = new Point(154, 108);
            hostBox.Size = new Size(Math.Max(220, cw - 460), 28);
            setLoopbackButton.Location = new Point(hostBox.Right + 10, 106);

            nickLabel.Location = new Point(30, 154);
            nickBox.Location = new Point(154, 150);
            nickBox.Size = new Size(Math.Max(220, cw - 330), 28);

            ipButtonsPanel.Location = new Point(30, 202);
            ipButtonsPanel.Size = new Size(cw - 60, Math.Max(70, Math.Min(110, ch / 5)));

            ticker.Location = new Point(Math.Max(cw - 456, cw / 2 + 44), 86);
            ticker.Size = new Size(404, 124);

            int gap = 24;
            int buttonY = Math.Max(360, ch - 176);
            int available = cw - 60;

            if (cw >= 980)
            {
                int bw = (available - gap * 2) / 3;
                bw = Math.Max(180, Math.Min(250, bw));

                zcButton.Size = new Size(bw, 68);
                mpButton.Size = new Size(bw, 68);
                dediButton.Size = new Size(bw, 68);

                zcButton.Location = new Point(30, buttonY);
                mpButton.Location = new Point(30 + bw + gap, buttonY);
                dediButton.Location = new Point(30 + (bw + gap) * 2, buttonY);
            }
            else
            {
                int bw = (available - gap) / 2;
                bw = Math.Max(180, bw);
                int bh = 58;

                zcButton.Size = new Size(bw, bh);
                mpButton.Size = new Size(bw, bh);
                dediButton.Size = new Size(available, bh);

                zcButton.Location = new Point(30, buttonY - bh - 16);
                mpButton.Location = new Point(30 + bw + gap, buttonY - bh - 16);
                dediButton.Location = new Point(30, buttonY);
            }
        }

        private void StartHostmode()
        {
            if (Bgt5LmsBridge.StartHostMode(3074))
            {
                statusLabel.Text = "HOSTMODE ACTIVE";
                statusLabel.ForeColor = Color.FromArgb(255, 190, 120);
            }
            else
            {
                statusLabel.Text = "retrying hostmode...";
                statusLabel.ForeColor = Color.FromArgb(230, 170, 96);
            }
        }

        private void WriteIniForLaunch(string host)
        {
            IniFile.Write("Config", "Host", host, iniPath);
            IniFile.Write("Config", "Nickname", nickBox.Text, iniPath);
        }

        private void LaunchAndHide(long size, bool dedicated)
        {
            if (launchInProgress) return;
            launchInProgress = true;
            try
            {
                StartHostmode();
                var exe = ExecutablePicker.PickBySize(AppDomain.CurrentDomain.BaseDirectory, size);
                if (exe == null)
                    throw new FileNotFoundException("Could not find the patched executable by expected file size.");

                WriteIniForLaunch(hostBox.Text.Trim().Length == 0 ? "127.0.0.1" : hostBox.Text.Trim());

                launchedProcessName = Path.GetFileNameWithoutExtension(exe);

                if (dedicated)
                {
                    var args = " +set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec bgserver.cfg";
                    Logger.Log("Launching dedicated: " + exe + args);
                    launched = Process.Start(exe, args);
                }
                else
                {
                    launched = Process.Start(exe);
                }

                if (launched != null || !string.IsNullOrWhiteSpace(launchedProcessName))
                {
                    if (launched != null)
                        Game.ApplyPatchWithRetry(launched);
                    gameWatchTimer.Start();
                    if (!dedicated)
                        HideToTray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Launcher error");
                Logger.Log("Launch failed: " + ex);
            }
            finally
            {
                var guardTimer = new Timer();
                guardTimer.Interval = 1800;
                guardTimer.Tick += (s, e) =>
                {
                    try { guardTimer.Stop(); guardTimer.Dispose(); } catch { }
                    launchInProgress = false;
                };
                guardTimer.Start();
            }
        }

        private void ShowLaunchOptions()
        {
            string text =
@"Launch in campaign/zombie mode: -zm or -sp
Launch in multiplayer mode: -mp
Launch with specific IP: -savedip
Launch with 127.0.0.1: -local
Launch with a specific IP: -<IP> where <IP> is the IP
Launch dedicated server: -server";

            try { Clipboard.SetText(text); } catch { }
            MessageBox.Show(text, "Launch options");
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle r = ClientRectangle;

            using (var lg = new LinearGradientBrush(r, Color.FromArgb(8, 8, 8), Color.FromArgb(24, 24, 24), 90f))
                e.Graphics.FillRectangle(lg, r);

            using (var glowPath = new GraphicsPath())
            {
                glowPath.AddEllipse(Width - 330, 20, 260, 260);
                using (var pgb = new PathGradientBrush(glowPath))
                {
                    pgb.CenterColor = Color.FromArgb(42, 255, 110, 0);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, 255, 110, 0) };
                    e.Graphics.FillPath(pgb, glowPath);
                }
            }

            using (var brush = new SolidBrush(Color.FromArgb(16, 255, 120, 0)))
            {
                for (int x = -200; x < Width + 200; x += 94)
                {
                    Point[] pts = {
                        new Point(x, 0),
                        new Point(x + 38, 0),
                        new Point(x - 132, Height),
                        new Point(x - 170, Height)
                    };
                    e.Graphics.FillPolygon(brush, pts);
                }
            }

            using (var p = new Pen(Color.FromArgb(14, 255, 120, 0), 1))
            {
                for (int x = 0; x < Width; x += 72) e.Graphics.DrawLine(p, x, 0, x, Height);
                for (int y = 0; y < Height; y += 48) e.Graphics.DrawLine(p, 0, y, Width, y);
            }

            using (var accent = new Pen(Color.FromArgb(175, 255, 120, 0), 3))
            {
                int margin = 24;
                int len = 96;
                e.Graphics.DrawLine(accent, margin, margin, margin + len, margin);
                e.Graphics.DrawLine(accent, margin, margin, margin, margin + len);
            }
        }
    }
}