
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using bgT5Launcher.Properties;
using bgt5lms;

namespace bgT5Launcher;

public class MainWindow : Form
{
    private const long SpExpectedSize = 8099928L;
    private const long MpExpectedSize = 8607832L;

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetPrivateProfileString(string appName, string keyName, string defaultValue, StringBuilder returnedString, uint size, string fileName);

    private readonly string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bgset.ini");
    private readonly Timer processWatch = new Timer();
    private readonly Timer ambientDigitsTimer = new Timer();
    private readonly List<Button> ipCopyButtons = new List<Button>();
    private readonly List<FloatingDigit> ambientDigits = new List<FloatingDigit>();
    private readonly Random ambientDigitsRandom = new Random();
    private readonly string[] startupArgs;
    private bool startupArgsConsumed;

    private Process currentGame;
    private bool hostModeStarted;
    private NotifyIcon trayIcon;
        private CheckBox checkOverrideGameId;
        private string autoGameId = "0";
    private bool launchInProgress;
    private IContainer components;

    private Button startSP;
    private Button startMP;
    private Button startDedi;
    private Button buttonLoopback;
    private TextBox textBoxHost;
    private TextBox textBoxNick;
    private TextBox textBoxGameId;
    private Label labelHost;
    private Label labelNick;
    private Label labelGameId;
    private Label labelVersion;
    private Button buttonTray;
    private Label hostStatus;
    private ToolTip trayToolTip;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusYourIp;
    private ToolStripStatusLabel statusIp;
    private ToolStripStatusLabel statusGameId;
    private FlowLayoutPanel ipPanel;
    private AmbientDigitsPanel ambientLayer;
    private Panel card;
    private Bitmap ambientBackdropCache;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x02000000;
            return cp;
        }
    }

    private sealed class FloatingDigit
    {
        public float X;
        public float Y;
        public float Speed;
        public float Drift;
        public float FontSize;
        public int Alpha;
        public bool IsAccent;
        public int[] GroupLengths = Array.Empty<int>();
        public string Text = string.Empty;
    }

    private sealed class AmbientDigitsPanel : Panel
    {
        public Action<Graphics>? PaintDigits;

        public AmbientDigitsPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
            UpdateStyles();
            BackColor = Color.Black;
            TabStop = false;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintDigits?.Invoke(e.Graphics);
        }
    }

    public MainWindow(string[] args = null)
    {
        startupArgs = args ?? Array.Empty<string>();
        InitializeComponent();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        UpdateStyles();
        InitializeAmbientDigits();

        try
        {
            string savedGameId = GetIni("Config", "GameID", "");
            if (!string.IsNullOrWhiteSpace(savedGameId))
                this.textBoxGameId.Text = savedGameId;
            string savedOverwrite = GetIni("Config", "GameIDOverwrite", "0");
            this.checkOverrideGameId.Checked = (savedOverwrite == "1");
        }
        catch { }

        RefreshAutoGameId();
        ApplyGameIdUiState();
        WireGameIdValidation();
        UpdateResponsiveLayout();

        textBoxHost.Text = GetIni("Network", "TypedHostInput", GetIni("Config", "Host", "127.0.0.1"));
        if (string.IsNullOrWhiteSpace(textBoxHost.Text))
            textBoxHost.Text = "127.0.0.1";
        textBoxNick.Text = GetIni("Config", "Nickname", Environment.UserName);

        EnsureGameId();
        textBoxGameId.Text = GetIni("Config", "GameID", "");
        RefreshGameIdLabel();
        BuildIpButtons();
        ApplyGameIdUiState();
        SaveTypedHostInput();

        processWatch.Interval = 700;
        processWatch.Tick += ProcessWatch_Tick;

        Shown += (s, e) =>
        {
            try
            {
                StartHostModeOnce();
            }
            catch
            {
            }

            if (!startupArgsConsumed)
            {
                startupArgsConsumed = true;
                BeginInvoke((Action)(() => HandleStartupArgs()));
            }
        };
    }

    private static string GetIniStatic(string section, string key, string file, string fallback = "")
    {
        StringBuilder sb = new StringBuilder(512);
        GetPrivateProfileString(section, key, fallback, sb, 512u, file);
        return sb.ToString();
    }

    private string GetIni(string section, string key, string fallback = "") => GetIniStatic(section, key, iniPath, fallback);


    private void SaveTypedHostInput()
    {
        try
        {
            string typedHost = (textBoxHost?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(typedHost))
                typedHost = "127.0.0.1";

            WritePrivateProfileString("Network", "TypedHostInput", typedHost, iniPath);
        }
        catch
        {
        }
    }

    private void EnsureGameId()
    {
        if (!string.IsNullOrWhiteSpace(GetIni("Config", "GameID", "")))
            return;

        Random random = new Random();
        string gameId = random.Next(2000000000, 2099999999).ToString();
        WritePrivateProfileString("Config", "GameID", gameId, iniPath);
    }

    private void RefreshGameIdLabel()
    {
        string gid = GetIni("Config", "GameID", "");
        statusGameId.Text = "GameID: " + gid;
        if (textBoxGameId != null && !textBoxGameId.Focused)
            textBoxGameId.Text = gid;
    }


    public void changeIP(string ip)
    {
        if (textBoxHost == null)
            return;

        textBoxHost.Text = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        try
        {
            textBoxHost.Focus();
            textBoxHost.SelectionStart = textBoxHost.TextLength;
        }
        catch
        {
        }
    }

    private static List<string> GetLocalIPv4s()
    {
        List<string> result = new List<string>();
        try
        {
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                    continue;

                string s = ip.ToString();
                if (!result.Contains(s))
                    result.Add(s);
            }
        }
        catch
        {
        }

        return result;
    }

    private static string GetPreferredLocalIPv4()
    {
        List<string> ips = GetLocalIPv4s();
        if (ips.Count == 0)
            return "";

        foreach (string s in ips)
        {
            if (s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("172.") || s.StartsWith("100."))
                return s;
        }

        return ips[0];
    }

    private static string? FindExecutableBySize(long expectedSize, params string[] preferredNames)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        foreach (string name in preferredNames)
        {
            string path = Path.Combine(baseDir, name);
            if (File.Exists(path))
            {
                try
                {
                    if (new FileInfo(path).Length == expectedSize)
                        return path;
                }
                catch
                {
                }
            }
        }

        foreach (string exe in Directory.GetFiles(baseDir, "*.exe"))
        {
            try
            {
                if (new FileInfo(exe).Length == expectedSize)
                    return exe;
            }
            catch
            {
            }
        }

        return null;
    }

    private string? FindSpExe()
    {
        return FindExecutableBySize(SpExpectedSize, "BGamerT5.exe", "BlackOps.exe");
    }

    private string? FindMpExe()
    {
        return FindExecutableBySize(MpExpectedSize, "BGamerT5MP.exe", "BlackOpsMP.exe");
    }


    private void EnsureProfileTemplateForGameId(string gameId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameId))
                return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string templateDir = Path.Combine(baseDir, "bgData", "Template");
            string profilesRoot = Path.Combine(baseDir, "bgData", "Profiles");
            string profileDir = Path.Combine(profilesRoot, gameId);

            if (!Directory.Exists(templateDir))
                return;

            Directory.CreateDirectory(profilesRoot);
            Directory.CreateDirectory(profileDir);

            foreach (string src in Directory.GetFiles(templateDir))
            {
                string name = Path.GetFileName(src);
                string dstName = name.Replace("_ID", "_" + gameId).Replace("ID.", gameId + ".");
                string dst = Path.Combine(profileDir, dstName);
                if (!File.Exists(dst))
                    File.Copy(src, dst, false);
            }
        }
        catch
        {
        }
    }

    private void WriteConfig(string gameMode, bool hosting, string resolvedHostOverride = null)
    {
        string selectedLocalIp = GetPreferredLocalIPv4();
        string hostInput = textBoxHost.Text.Trim();
        string nick = textBoxNick.Text.Trim();
        if (string.IsNullOrWhiteSpace(nick))
            nick = Environment.UserName;

        bool useLoopback = hostInput == "127.0.0.1";
        string effectiveHostIp = string.IsNullOrWhiteSpace(resolvedHostOverride) ? (useLoopback ? selectedLocalIp : hostInput) : resolvedHostOverride;
        string hostModeValue = hosting ? "1" : "0";
        string gameId = (checkOverrideGameId != null && checkOverrideGameId.Checked) ? textBoxGameId.Text.Trim() : autoGameId;
        if (string.IsNullOrWhiteSpace(gameId) || !Int32.TryParse(gameId, out int gid) || gid < 0)
            gameId = autoGameId;

        string configHost = effectiveHostIp;
        if (gameMode == "MULTIPLAYER" && useLoopback && !string.IsNullOrWhiteSpace(selectedLocalIp))
            configHost = selectedLocalIp;

        WritePrivateProfileString("Config", "Host", configHost, iniPath);
        WritePrivateProfileString("Config", "Nickname", nick, iniPath);
        WritePrivateProfileString("Config", "GameID", gameId, iniPath);
        WritePrivateProfileString("Config", "Game", gameMode, iniPath);
        WritePrivateProfileString("Config", "HostMode", hostModeValue, iniPath);
        WritePrivateProfileString("Config", "HostStatus", hosting ? "1" : "0", iniPath);

        WritePrivateProfileString("Host", "Host", hosting ? "1" : "0", iniPath);
        WritePrivateProfileString("Host", "HostMode", hostModeValue, iniPath);
        WritePrivateProfileString("Host", "HostName", nick, iniPath);
        WritePrivateProfileString("Host", "HostIp", effectiveHostIp, iniPath);
        WritePrivateProfileString("Host", "netip", effectiveHostIp, iniPath);
        WritePrivateProfileString("Host", "IPAddress", effectiveHostIp, iniPath);
        WritePrivateProfileString("Host", "InternalIPAddress", effectiveHostIp, iniPath);
        WritePrivateProfileString("Host", "ipdetect", "1", iniPath);

        WritePrivateProfileString("Network", "SelectedLocalIp", selectedLocalIp, iniPath);
        WritePrivateProfileString("Network", "AllowLoopbackForManualConnect", useLoopback ? "1" : "0", iniPath);
        WritePrivateProfileString("Network", "TypedHostInput", hostInput, iniPath);

        statusIp.Text = string.IsNullOrWhiteSpace(selectedLocalIp) ? "No local IPv4" : selectedLocalIp;
        RefreshGameIdLabel();
    }


    private string GetLaunchOptionsText()
    {
        return "Launch Options\n-zm  Zombies\n-sp  Campaign\n-mp  Multiplayer\n-server  Dedicated Server\n-savedip  Launch with Saved IP\n-[IP]  Launch with Specific IP\nMade by Smalllbuddy";
    }

    private static bool TryParseIpArg(string arg, out string ip)
    {
        ip = "";
        if (string.IsNullOrWhiteSpace(arg) || !arg.StartsWith("-"))
            return false;

        string candidate = arg.Substring(1).Trim();
        string[] parts = candidate.Split('.');
        if (parts.Length != 4)
            return false;

        foreach (string part in parts)
        {
            if (!Int32.TryParse(part, out int value) || value < 0 || value > 255)
                return false;
        }

        ip = candidate;
        return true;
    }

    private void HandleStartupArgs()
    {
        try
        {
            string mode = "";
            string launchIp = "";

            foreach (string raw in startupArgs)
            {
                string arg = (raw ?? "").Trim();
                if (arg.Length == 0)
                    continue;

                if (arg.Equals("-zm", StringComparison.OrdinalIgnoreCase) || arg.Equals("-sp", StringComparison.OrdinalIgnoreCase))
                    mode = "sp";
                else if (arg.Equals("-mp", StringComparison.OrdinalIgnoreCase))
                    mode = "mp";
                else if (arg.Equals("-server", StringComparison.OrdinalIgnoreCase))
                    mode = "server";
                else if (arg.Equals("-savedip", StringComparison.OrdinalIgnoreCase))
                {
                    // default behavior already uses saved Host IP
                }
                else if (TryParseIpArg(arg, out string parsedIp))
                    launchIp = parsedIp;
            }

            if (!string.IsNullOrWhiteSpace(launchIp))
                this.textBoxHost.Text = launchIp;

            if (mode == "sp")
                StartSP_Click(this, EventArgs.Empty);
            else if (mode == "mp")
                StartMP_Click(this, EventArgs.Empty);
            else if (mode == "server")
                StartDedi_Click(this, EventArgs.Empty);
        }
        catch
        {
        }
    }

    
    
    private void ApplyHostStatusLayout()
    {
        if (this.hostStatus == null || this.buttonLoopback == null)
            return;

        bool wrap = this.card.Width < 620;

        this.hostStatus.AutoSize = false;
        this.hostStatus.Location = new Point(
            this.buttonLoopback.Right + 12,
            this.buttonLoopback.Top + (wrap ? -2 : 2)
        );

        string raw = (this.hostStatus.Text ?? "").Replace("\r", "").Replace("\n", " ").Trim();

        if (raw.Equals("HOSTMODE ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            this.hostStatus.Size = wrap ? new Size(96, 30) : new Size(128, 18);
            this.hostStatus.Text = wrap ? "HOSTMODE\r\nACTIVE" : "HOSTMODE ACTIVE";
        }
        else if (raw.Equals("HOSTMODE ERROR", StringComparison.OrdinalIgnoreCase))
        {
            this.hostStatus.Size = wrap ? new Size(96, 30) : new Size(128, 18);
            this.hostStatus.Text = wrap ? "HOSTMODE\r\nERROR" : "HOSTMODE ERROR";
        }
        else
        {
            this.hostStatus.Size = wrap ? new Size(110, 30) : new Size(140, 18);
        }
    }


    private string ResolveHostForGameLaunch(bool forMultiplayerOrDedicated)
    {
        string hostInput = (this.textBoxHost.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(hostInput))
            hostInput = "127.0.0.1";

        if (!forMultiplayerOrDedicated)
            return hostInput;

        if (hostInput == "127.0.0.1")
        {
            foreach (Control c in this.ipPanel.Controls)
            {
                string txt = c.Text ?? "";
                Match m = Regex.Match(txt, @"(\d{1,3}(?:\.\d{1,3}){3})");
                if (m.Success)
                    return m.Groups[1].Value;
            }

            string selected = GetIni("Network", "SelectedLocalIp", "");
            if (!string.IsNullOrWhiteSpace(selected))
                return selected;

            string hostIp = GetIni("Host", "HostIp", "");
            if (!string.IsNullOrWhiteSpace(hostIp))
                return hostIp;
        }

        return hostInput;
    }

    private bool IsLocalOrLoopbackHostTarget()
    {
        string hostInput = (this.textBoxHost.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(hostInput) || hostInput == "127.0.0.1")
            return true;

        if (string.Equals(hostInput, GetPreferredLocalIPv4(), StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (string ip in GetLocalIPv4s())
        {
            if (string.Equals(ip, hostInput, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void StartHostModeOnce()
    {
        if (hostModeStarted)
            return;

        try
        {
            BGT5LMS.Start(3074);
            hostModeStarted = true;
            hostStatus.ForeColor = Color.FromArgb(170, 230, 170);
            hostStatus.Text = "HOSTMODE ACTIVE";
            ApplyHostStatusLayout();
        }
        catch (Exception ex)
        {
            hostStatus.Text = "HOSTMODE ERROR";
            hostStatus.ForeColor = Color.OrangeRed;
            ApplyHostStatusLayout();
            MessageBox.Show("Failed to start bgT5lms.\n" + ex.Message, "Hostmode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw;
        }
    }

    private void ProcessWatch_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (currentGame == null)
                return;

            if (currentGame.HasExited)
            {
                processWatch.Stop();
                currentGame = null;
                ShowFromTray();
            }
        }
        catch
        {
            processWatch.Stop();
            currentGame = null;
            ShowFromTray();
        }
    }

    private void ShowInTray()
    {
        if (trayIcon == null)
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "bgT5Launcher";
            try { trayIcon.Icon = this.Icon; } catch { }
            trayIcon.DoubleClick += (s, e) => ShowFromTray();

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Show launcher", null, (s, e) => ShowFromTray());
            menu.Items.Add("Exit launcher", null, (s, e) => Close());
            trayIcon.ContextMenuStrip = menu;
        }

        trayIcon.Visible = true;
        Hide();
        ShowInTaskbar = false;
    }

    private void ShowFromTray()
    {
        if (trayIcon != null)
            trayIcon.Visible = false;

        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void LaunchGame(string gameMode, string exePath, bool hosting = false)
    {
        if (launchInProgress)
            return;

        launchInProgress = true;
        try
        {
            if (gameMode == "MULTIPLAYER")
            {
                try { StartHostModeOnce(); } catch { }
            }

            string resolvedHost = ResolveHostForGameLaunch(gameMode == "MULTIPLAYER");
            WriteConfig(gameMode, hosting, resolvedHost);
            if (gameMode == "Singleplayer" || gameMode == "MULTIPLAYER")
                EnsureProfileTemplateForGameId(GetIni("Config", "GameID", "0"));
            currentGame = Process.Start(exePath);
            if (currentGame == null)
                throw new InvalidOperationException("Could not start process.");

            Game.Start(currentGame);
            processWatch.Start();
            ShowInTray();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Can't start .exe!\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            currentGame = null;
        }
        finally
        {
            Timer unlock = new Timer();
            unlock.Interval = 1500;
            unlock.Tick += (s, e) =>
            {
                unlock.Stop();
                unlock.Dispose();
                launchInProgress = false;
            };
            unlock.Start();
        }
    }

    private static string? ResolveDedicatedCfgExecArg()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string mainBgServer = Path.Combine(baseDir, "main", "bgserver.cfg");
        if (File.Exists(mainBgServer))
            return "bgserver.cfg";

        string mainServer = Path.Combine(baseDir, "main", "server.cfg");
        if (File.Exists(mainServer))
            return "server.cfg";

        string rootBgServer = Path.Combine(baseDir, "bgserver.cfg");
        if (File.Exists(rootBgServer))
            return "bgserver.cfg";

        string rootServer = Path.Combine(baseDir, "server.cfg");
        if (File.Exists(rootServer))
            return "server.cfg";

        return null;
    }

    private void LaunchDedicated()
    {
        try
        {
            WriteConfig("MULTIPLAYER", true);
            string? exe = FindMpExe();
            if (exe == null)
                throw new FileNotFoundException("Could not find the MP executable by expected file size.");

            string? cfgArg = ResolveDedicatedCfgExecArg();
            if (string.IsNullOrWhiteSpace(cfgArg))
                throw new FileNotFoundException("bgserver.cfg / server.cfg not found in main or the game root.");

            currentGame = Process.Start(exe, $"+set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec {cfgArg}");
            if (currentGame == null)
                throw new InvalidOperationException("Could not start dedicated server.");

            processWatch.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Couldn't start dedicated server.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            currentGame = null;
        }
    }

    private void StartSP_Click(object? sender, EventArgs e)
    {
        string? exe = FindSpExe();
        if (exe == null)
        {
            MessageBox.Show("Could not find BGamerT5.exe / BlackOps.exe or another matching SP executable by file size.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }

        LaunchGame("Singleplayer", exe);
    }

    private void StartMP_Click(object? sender, EventArgs e)
    {
        string? exe = FindMpExe();
        if (exe == null)
        {
            MessageBox.Show("Could not find BGamerT5MP.exe / BlackOpsMP.exe or another matching MP executable by file size.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            return;
        }

        bool hosting = IsLocalOrLoopbackHostTarget();
        LaunchGame("MULTIPLAYER", exe, hosting);
    }

    
    
    private void StartDedi_Click(object? sender, EventArgs e)
    {
        try
        {
            StartHostModeOnce();

            string? exePath = FindMpExe();
            if (string.IsNullOrWhiteSpace(exePath))
            {
                MessageBox.Show("Could not find the multiplayer executable.", "Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string resolvedHost = ResolveHostForGameLaunch(true);
            WriteConfig("MULTIPLAYER", true, resolvedHost);

            string? cfgArg = ResolveDedicatedCfgExecArg();
            if (string.IsNullOrWhiteSpace(cfgArg))
            {
                MessageBox.Show("bgserver.cfg / server.cfg not found in main or the game root.", "Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentGame = Process.Start(exePath, $"+set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec {cfgArg}");

            if (currentGame != null)
                processWatch.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Loopback_Click(object? sender, EventArgs e)
    {
        textBoxHost.Text = "127.0.0.1";
    }

    private void MainWindow_Load(object? sender, EventArgs e)
    {
        string ip = GetPreferredLocalIPv4();
        statusIp.Text = string.IsNullOrWhiteSpace(ip) ? "No local IPv4" : ip;
        if (textBoxHost.Text == "")
            textBoxHost.Text = "127.0.0.1";
    }

    private void BuildIpButtons()
    {
        ipPanel.Controls.Clear();
        ipCopyButtons.Clear();

        foreach (string ip in GetLocalIPv4s())
        {
            string buttonText = "Copy " + ip;
            Button b = MakeSmallButton(buttonText);
            int measured = TextRenderer.MeasureText(buttonText, b.Font).Width;
            b.Width = Math.Max(116, measured + 16);
            b.Height = 24;
            b.Margin = new Padding(0, 0, 8, 0);
            b.Padding = new Padding(0);
            b.TextAlign = ContentAlignment.MiddleCenter;
            b.Click += (s, e) =>
            {
                try { Clipboard.SetText(ip); } catch { }
            };
            ipPanel.Controls.Add(b);
            ipCopyButtons.Add(b);
        }

        if (ipPanel.Controls.Count == 0)
        {
            Label noIp = new Label();
            noIp.Text = "No local IPv4 detected";
            noIp.ForeColor = Color.Gainsboro;
            noIp.BackColor = Color.Transparent;
            noIp.AutoSize = true;
            noIp.Margin = new Padding(0, 4, 0, 0);
            ipPanel.Controls.Add(noIp);
        }
    }

    private Button MakeSmallButton(string text)
    {
        Button b = new Button();
        b.Text = text;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.MediumPurple;
        b.FlatAppearance.BorderSize = 1;
        b.BackColor = Color.FromArgb(32, 22, 36);
        b.ForeColor = Color.White;
        b.Font = new Font("Consolas", 8.0f);
        b.Margin = new Padding(0);
        b.TextAlign = ContentAlignment.MiddleCenter;
        return b;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try { processWatch.Stop(); ambientDigitsTimer.Stop(); } catch { }
        try { ambientBackdropCache?.Dispose(); ambientBackdropCache = null; } catch { }
        try { if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); } } catch { }
        try { BGT5LMS.Stop(); } catch { }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ambientBackdropCache?.Dispose();
            if (components != null)
                components.Dispose();
        }
        base.Dispose(disposing);
    }


    private void UpdateAmbientLayerRegion()
    {
        try
        {
            if (this.ambientLayer == null || this.card == null || this.card.Width <= 1 || this.card.Height <= 1)
                return;

            int edgeBand = Math.Max(34, Math.Min(92, Math.Min(this.card.Width, this.card.Height) / 6));
            Region ring = new Region(new Rectangle(0, 0, this.card.Width, edgeBand));
            ring.Union(new Rectangle(0, Math.Max(0, this.card.Height - edgeBand), this.card.Width, edgeBand));
            ring.Union(new Rectangle(0, edgeBand, edgeBand, Math.Max(1, this.card.Height - edgeBand * 2)));
            ring.Union(new Rectangle(Math.Max(0, this.card.Width - edgeBand), edgeBand, edgeBand, Math.Max(1, this.card.Height - edgeBand * 2)));
            this.ambientLayer.Region?.Dispose();
            this.ambientLayer.Region = ring;
        }
        catch
        {
        }
    }

    private void InvalidateAmbientEdges()
    {
        try
        {
            if (this.ambientLayer == null || !this.ambientLayer.IsHandleCreated || this.card == null)
                return;

            int edgeBand = Math.Max(34, Math.Min(92, Math.Min(this.card.Width, this.card.Height) / 6));
            this.ambientLayer.Invalidate(new Rectangle(0, 0, this.card.Width, edgeBand + 2));
            this.ambientLayer.Invalidate(new Rectangle(0, Math.Max(0, this.card.Height - edgeBand - 2), this.card.Width, edgeBand + 2));
            this.ambientLayer.Invalidate(new Rectangle(0, edgeBand, edgeBand + 2, Math.Max(1, this.card.Height - edgeBand * 2)));
            this.ambientLayer.Invalidate(new Rectangle(Math.Max(0, this.card.Width - edgeBand - 2), edgeBand, edgeBand + 2, Math.Max(1, this.card.Height - edgeBand * 2)));
        }
        catch
        {
        }
    }

    private void InitializeAmbientDigits()
    {
        try
        {
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this.card, true, null);
            if (this.ambientLayer != null)
                this.ambientLayer.PaintDigits = DrawAmbientDigits;

            this.card.Resize += (s, e) =>
            {
                EnsureAmbientDigitsFilled();
                RebuildAmbientBackdropCache();
                UpdateAmbientLayerRegion();
                InvalidateAmbientEdges();
            };

            ambientDigitsTimer.Interval = 30;
            ambientDigitsTimer.Tick += (s, e) =>
            {
                StepAmbientDigits();
                if (!this.IsDisposed && this.ambientLayer != null && this.ambientLayer.IsHandleCreated && this.Visible && this.WindowState != FormWindowState.Minimized)
                    InvalidateAmbientEdges();
            };

            SeedAmbientDigits();
            RebuildAmbientBackdropCache();
            UpdateAmbientLayerRegion();
            ambientDigitsTimer.Start();
        }
        catch
        {
        }
    }

    private void SeedAmbientDigits()
    {
        ambientDigits.Clear();
        int target = Math.Max(34, Math.Min(82, Math.Max(1, this.card.Width / 12)));
        for (int i = 0; i < target; i++)
            ambientDigits.Add(CreateAmbientDigit(false));
    }

    private void EnsureAmbientDigitsFilled()
    {
        if (this.card == null || this.card.Width <= 0 || this.card.Height <= 0)
            return;

        int target = Math.Max(34, Math.Min(82, Math.Max(1, this.card.Width / 12)));
        while (ambientDigits.Count < target)
            ambientDigits.Add(CreateAmbientDigit(true));

        while (ambientDigits.Count > target && ambientDigits.Count > 0)
            ambientDigits.RemoveAt(ambientDigits.Count - 1);
    }

    private string GetAmbientDigitsFontName()
    {
        try
        {
            string[] candidates = new[] { "Agency FB", "Bahnschrift Condensed", "Arial Narrow", "Consolas" };
            InstalledFontCollection installed = new InstalledFontCollection();
            foreach (string family in candidates)
            {
                if (installed.Families.Any(f => string.Equals(f.Name, family, StringComparison.OrdinalIgnoreCase)))
                    return family;
            }
        }
        catch { }

        return "Consolas";
    }

    private float EstimateAmbientDigitWidth(FloatingDigit digit)
    {
        if (digit == null)
            return 32f;

        int charCount = string.IsNullOrEmpty(digit.Text) ? 1 : digit.Text.Length;
        float factor = GetAmbientDigitsFontName() == "Consolas" ? 0.72f : 0.58f;
        return Math.Max(20f, digit.FontSize * factor * charCount + 8f);
    }

    private FloatingDigit CreateAmbientDigit(bool fromBottom)
    {
        int width = Math.Max(120, this.card.Width - 16);
        int height = Math.Max(120, this.card.Height - 16);
        int[] pattern = CreateAmbientDigitPattern();
        bool accent = ambientDigitsRandom.NextDouble() < 0.42;
        float margin = 28f;
        float edgeBand = Math.Max(34f, Math.Min(92f, Math.Min(width, height) * 0.20f));
        int edge = ambientDigitsRandom.Next(0, 4);
        float x;
        float y;
        FloatingDigit digit = new FloatingDigit
        {
            Speed = 1.05f + (float)(ambientDigitsRandom.NextDouble() * 1.65),
            Drift = (float)(ambientDigitsRandom.NextDouble() * 0.56 - 0.28),
            FontSize = (accent ? 8.4f : 6.9f) + (float)(ambientDigitsRandom.NextDouble() * (accent ? 5.2 : 3.6)),
            Alpha = accent ? ambientDigitsRandom.Next(64, 102) : ambientDigitsRandom.Next(28, 58),
            IsAccent = accent,
            GroupLengths = pattern,
            Text = CreateAmbientDigitText(pattern)
        };
        float textWidth = EstimateAmbientDigitWidth(digit);

        switch (edge)
        {
            case 0:
                x = margin + (float)(ambientDigitsRandom.NextDouble() * Math.Max(40, width - margin * 2 - textWidth));
                y = fromBottom ? height + ambientDigitsRandom.Next(8, 40) : margin + (float)(ambientDigitsRandom.NextDouble() * edgeBand);
                break;
            case 1:
                x = margin + (float)(ambientDigitsRandom.NextDouble() * Math.Max(40, width - margin * 2 - textWidth));
                y = height - edgeBand + (float)(ambientDigitsRandom.NextDouble() * Math.Max(12, edgeBand - margin));
                if (fromBottom) y = height + ambientDigitsRandom.Next(8, 40);
                break;
            case 2:
                x = margin + (float)(ambientDigitsRandom.NextDouble() * Math.Max(12, edgeBand - 10f));
                y = margin + (float)(ambientDigitsRandom.NextDouble() * Math.Max(40, height - margin * 2));
                if (fromBottom) y = height + ambientDigitsRandom.Next(8, 40);
                break;
            default:
                x = Math.Max(margin, width - edgeBand + (float)(ambientDigitsRandom.NextDouble() * Math.Max(8, edgeBand - 12f)) - textWidth);
                y = margin + (float)(ambientDigitsRandom.NextDouble() * Math.Max(40, height - margin * 2));
                if (fromBottom) y = height + ambientDigitsRandom.Next(8, 40);
                break;
        }

        digit.X = Math.Max(margin, Math.Min(width - margin - textWidth, x));
        digit.Y = y;
        return digit;
    }

    private int[] CreateAmbientDigitPattern()
    {
        double roll = ambientDigitsRandom.NextDouble();
        if (roll < 0.50)
            return new[] { 1 };
        if (roll < 0.80)
            return new[] { ambientDigitsRandom.Next(2, 4) };
        if (roll < 0.96)
            return new[] { 3 };

        return new[] { ambientDigitsRandom.Next(1, 3), ambientDigitsRandom.Next(1, 3) };
    }

    private string CreateAmbientDigitText(int[] pattern)
    {
        StringBuilder sb = new StringBuilder();
        for (int g = 0; g < pattern.Length; g++)
        {
            for (int i = 0; i < pattern[g]; i++)
                sb.Append(ambientDigitsRandom.Next(0, 10));

            if (g < pattern.Length - 1)
                sb.Append(' ');
        }

        return sb.ToString();
    }

    private string ScrambleAmbientDigitText(FloatingDigit digit)
    {
        if (digit.GroupLengths == null || digit.GroupLengths.Length == 0)
            digit.GroupLengths = CreateAmbientDigitPattern();

        StringBuilder sb = new StringBuilder();
        for (int g = 0; g < digit.GroupLengths.Length; g++)
        {
            for (int i = 0; i < digit.GroupLengths[g]; i++)
                sb.Append(ambientDigitsRandom.Next(0, 10));

            if (g < digit.GroupLengths.Length - 1)
                sb.Append(' ');
        }

        return sb.ToString();
    }

    private void StepAmbientDigits()
    {
        if (ambientDigits.Count == 0 || this.card == null)
        {
            EnsureAmbientDigitsFilled();
            return;
        }

        float maxX = Math.Max(80, this.card.Width - 10);
        float maxY = Math.Max(80, this.card.Height + 36);
        float margin = 16f;
        float edgeBand = Math.Max(34f, Math.Min(92f, Math.Min(this.card.Width, this.card.Height) * 0.20f));

        foreach (FloatingDigit digit in ambientDigits)
        {
            digit.Y -= digit.Speed;
            digit.X += digit.Drift;
            digit.Text = ScrambleAmbientDigitText(digit);

            float textWidth = EstimateAmbientDigitWidth(digit);
            bool insideCenterX = digit.X > edgeBand && digit.X < this.card.Width - edgeBand - textWidth;
            bool insideCenterY = digit.Y > edgeBand && digit.Y < this.card.Height - edgeBand - 20;
            bool outOfBounds = digit.Y < -26 || digit.X < -textWidth || digit.X > maxX || digit.Y > maxY;

            if (outOfBounds || (insideCenterX && insideCenterY))
            {
                FloatingDigit reset = CreateAmbientDigit(true);
                digit.X = reset.X;
                digit.Y = reset.Y;
                digit.Speed = reset.Speed;
                digit.Drift = reset.Drift;
                digit.FontSize = reset.FontSize;
                digit.Alpha = reset.Alpha;
                digit.GroupLengths = reset.GroupLengths;
                digit.Text = reset.Text;
            }
            else
            {
                if (digit.X < margin) digit.X = margin;
                float maxDigitX = Math.Max(margin, this.card.Width - margin - textWidth);
                if (digit.X > maxDigitX) digit.X = maxDigitX;
            }
        }
    }

    private void RebuildAmbientBackdropCache()
    {
        try
        {
            if (this.card == null || this.card.Width <= 1 || this.card.Height <= 1)
                return;

            ambientBackdropCache?.Dispose();
            ambientBackdropCache = new Bitmap(this.card.Width, this.card.Height);
            using Graphics g = Graphics.FromImage(ambientBackdropCache);
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            if (this.BackgroundImage != null)
            {
                Rectangle dest = new Rectangle(0, 0, ambientBackdropCache.Width, ambientBackdropCache.Height);
                float scaleX = (float)this.BackgroundImage.Width / Math.Max(1, this.ClientSize.Width);
                float scaleY = (float)this.BackgroundImage.Height / Math.Max(1, this.ClientSize.Height);
                Rectangle src = new Rectangle(
                    Math.Max(0, (int)Math.Round(this.card.Left * scaleX)),
                    Math.Max(0, (int)Math.Round(this.card.Top * scaleY)),
                    Math.Max(1, (int)Math.Round(this.card.Width * scaleX)),
                    Math.Max(1, (int)Math.Round(this.card.Height * scaleY)));

                src.Width = Math.Min(src.Width, this.BackgroundImage.Width - src.X);
                src.Height = Math.Min(src.Height, this.BackgroundImage.Height - src.Y);
                g.DrawImage(this.BackgroundImage, dest, src, GraphicsUnit.Pixel);
            }
            else
            {
                g.Clear(Color.FromArgb(14, 14, 14));
            }

            using SolidBrush overlay = new SolidBrush(this.card.BackColor);
            g.FillRectangle(overlay, 0, 0, ambientBackdropCache.Width, ambientBackdropCache.Height);
        }
        catch
        {
        }
    }

    private void DrawAmbientDigits(Graphics graphics)
    {
        if (ambientBackdropCache != null)
            graphics.DrawImageUnscaled(ambientBackdropCache, 0, 0);

        if (ambientDigits.Count == 0 || this.card == null)
            return;

        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        float edgeBand = Math.Max(34f, Math.Min(92f, Math.Min(this.card.Width, this.card.Height) * 0.20f));
        RectangleF center = new RectangleF(edgeBand, edgeBand, Math.Max(1, this.card.Width - edgeBand * 2), Math.Max(1, this.card.Height - edgeBand * 2));

        foreach (FloatingDigit digit in ambientDigits)
        {
            using Font measureFont = new Font(GetAmbientDigitsFontName(), digit.FontSize, FontStyle.Bold, GraphicsUnit.Point);
            SizeF sz = graphics.MeasureString(digit.Text, measureFont);
            RectangleF bounds = new RectangleF(digit.X, digit.Y, sz.Width, sz.Height);
            if (bounds.IntersectsWith(center))
                continue;

            using Font font = new Font(GetAmbientDigitsFontName(), digit.FontSize, FontStyle.Bold, GraphicsUnit.Point);
            int shadowAlpha = digit.IsAccent ? Math.Max(10, digit.Alpha / 4) : Math.Max(5, digit.Alpha / 5);
            using SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(shadowAlpha, 0, 0, 0));
            using SolidBrush textBrush = new SolidBrush(Color.FromArgb(digit.Alpha, digit.IsAccent ? 124 : 102, digit.IsAccent ? 58 : 44, digit.IsAccent ? 132 : 118));

            graphics.DrawString(digit.Text, font, shadowBrush, digit.X + 1f, digit.Y + 1f);
            graphics.DrawString(digit.Text, font, textBrush, digit.X, digit.Y);
        }
    }

    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainWindow));
        this.components = new Container();
        this.card = new Panel();
        this.ambientLayer = new AmbientDigitsPanel();
        this.startSP = new Button();
        this.startMP = new Button();
        this.startDedi = new Button();
        this.buttonLoopback = new Button();
        this.textBoxHost = new TextBox();
        this.textBoxNick = new TextBox();
        this.textBoxGameId = new TextBox();
        this.labelHost = new Label();
        this.labelNick = new Label();
        this.labelGameId = new Label();
        this.checkOverrideGameId = new CheckBox();
        this.labelVersion = new Label();
        this.buttonTray = new Button();
        this.hostStatus = new Label();
        this.trayToolTip = new ToolTip();
        this.statusStrip = new StatusStrip();
        this.statusYourIp = new ToolStripStatusLabel();
        this.statusIp = new ToolStripStatusLabel();
        this.statusGameId = new ToolStripStatusLabel();
        this.ipPanel = new FlowLayoutPanel();

        this.statusStrip.SuspendLayout();
        this.card.SuspendLayout();
        SuspendLayout();

        this.BackColor = Color.FromArgb(14, 14, 14);
        this.BackgroundImage = Resources.BOlolgo;
        this.BackgroundImageLayout = ImageLayout.Stretch;
        this.ClientSize = new Size(670, 294);
        this.MinimumSize = new Size(466, 248);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = true;
        this.Icon = (Icon?)resources.GetObject("$this.Icon");
        this.Name = "MainWindow";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "BGamer: Black Ops";
        this.TopMost = false;
        this.Load += MainWindow_Load;
        this.Resize += (s, e) => UpdateResponsiveLayout();

        this.card.BackColor = Color.FromArgb(128, 18, 14, 22);
        this.card.BorderStyle = BorderStyle.FixedSingle;
        this.card.Location = new Point(10, 10);
        this.card.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        this.card.Size = new Size(this.ClientSize.Width - 20, this.ClientSize.Height - 45);

        this.labelVersion.AutoSize = true;
        this.labelVersion.BackColor = Color.Transparent;
        this.labelVersion.ForeColor = Color.Gainsboro;
        this.labelVersion.Font = new Font("Consolas", 8.25f);
        this.labelVersion.Text = "v0.4.0";
        this.labelVersion.Cursor = Cursors.Help;
        this.trayToolTip.SetToolTip(this.labelVersion, GetLaunchOptionsText());
        this.labelVersion.MouseEnter += (s, e) => this.trayToolTip.Show(GetLaunchOptionsText(), this.labelVersion, 0, this.labelVersion.Height + 2, 4000);
        this.labelVersion.MouseLeave += (s, e) => this.trayToolTip.Hide(this.labelVersion);
        this.labelVersion.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.labelVersion.Location = new Point(10, 4);


        this.buttonTray.Text = "◱";
        this.buttonTray.Font = new Font("Segoe UI Symbol", 9.5f, FontStyle.Bold);
        this.buttonTray.FlatStyle = FlatStyle.Flat;
        this.buttonTray.FlatAppearance.BorderSize = 1;
        this.buttonTray.FlatAppearance.BorderColor = Color.FromArgb(92, 92, 118);
        this.buttonTray.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 42, 54);
        this.buttonTray.BackColor = Color.FromArgb(24, 24, 30);
        this.buttonTray.ForeColor = Color.FromArgb(190, 190, 210);
        this.buttonTray.Size = new Size(22, 22);
        this.buttonTray.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this.buttonTray.TabStop = false;
        this.buttonTray.Click += (s, e) => ShowInTray();
        this.buttonTray.Cursor = Cursors.Help;
        this.trayToolTip.SetToolTip(this.buttonTray, "Minimize to tray");
        this.buttonTray.MouseEnter += (s, e) => this.trayToolTip.Show("Minimize to tray", this.buttonTray, 0, this.buttonTray.Height + 2, 2000);
        this.buttonTray.MouseLeave += (s, e) => this.trayToolTip.Hide(this.buttonTray);


        this.labelNick.AutoSize = true;
        this.labelNick.BackColor = Color.FromArgb(14,14,14);
        this.labelNick.ForeColor = Color.White;
        this.labelNick.Font = new Font("Consolas", 7.25f, FontStyle.Bold);
        this.labelNick.Text = "Nickname:";
        this.labelNick.Location = new Point(6, 13);

        this.textBoxNick.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxNick.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxNick.ForeColor = Color.Gainsboro;
        this.textBoxNick.Location = new Point(58, 18);
        this.textBoxNick.Size = new Size(160, 20);
        this.textBoxNick.TextAlign = HorizontalAlignment.Center;
        this.textBoxNick.TextChanged += (s, e) => { RefreshAutoGameId(); ApplyGameIdUiState(); };

        this.labelHost.AutoSize = true;
        this.labelHost.BackColor = Color.FromArgb(14,14,14);
        this.labelHost.ForeColor = Color.White;
        this.labelHost.Font = new Font("Consolas", 7.25f, FontStyle.Bold);
        this.labelHost.Text = "Host IP:";
        this.labelHost.Location = new Point(6, 45);

        this.textBoxHost.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxHost.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxHost.ForeColor = Color.Gainsboro;
        this.textBoxHost.Location = new Point(58, 50);
        this.textBoxHost.Size = new Size(160, 20);
        this.textBoxHost.TextAlign = HorizontalAlignment.Center;
        this.textBoxHost.TextChanged += (s, e) => SaveTypedHostInput();

        this.buttonLoopback = MakeSmallButton("127.0.0.1");
        this.buttonLoopback.Location = new Point(this.textBoxHost.Left, this.textBoxHost.Bottom + 6);
        this.buttonLoopback.Click += (s, e) => { textBoxHost.Text = "127.0.0.1"; };
        this.buttonLoopback.Size = new Size(90, 24);

        this.labelGameId.AutoSize = true;
        this.labelGameId.BackColor = Color.FromArgb(14,14,14);
        this.labelGameId.ForeColor = Color.FromArgb(58, 58, 68);
        this.labelGameId.Font = new Font("Consolas", 6.9f, FontStyle.Bold);
        this.labelGameId.Text = "PlayerID:";
        this.labelGameId.Location = new Point(478, 18);

        this.textBoxGameId.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxGameId.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxGameId.ForeColor = Color.FromArgb(62, 62, 72);
        this.textBoxGameId.Location = new Point(540, 18);
        this.textBoxGameId.Size = new Size(76, 20);
        this.textBoxGameId.TextAlign = HorizontalAlignment.Center;

        this.checkOverrideGameId.Text = "Overwrite";
        this.checkOverrideGameId.AutoSize = true;
        this.checkOverrideGameId.BackColor = Color.FromArgb(14,14,14);
        this.checkOverrideGameId.ForeColor = Color.FromArgb(70, 70, 80);
        this.checkOverrideGameId.Location = new Point(this.textBoxGameId.Left - 2, this.textBoxGameId.Bottom + 4);
        this.checkOverrideGameId.CheckedChanged += (s, e) =>
        {
            ApplyGameIdUiState();
            WritePrivateProfileString("Config", "GameIDOverwrite", this.checkOverrideGameId.Checked ? "1" : "0", iniPath);
        };


        this.hostStatus.AutoSize = true;
        this.hostStatus.BackColor = Color.Transparent;
        this.hostStatus.ForeColor = Color.LightGreen;
        this.hostStatus.Font = new Font("Consolas", 9.75f, FontStyle.Bold);
        this.hostStatus.Text = "HOSTMODE ACTIVE";
        this.hostStatus.Location = new Point(202, 76);

        this.ipPanel.BackColor = Color.Transparent;
        this.ipPanel.Location = new Point(58, 108);
        this.ipPanel.Size = new Size(640, 28);
        this.ipPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        this.ipPanel.FlowDirection = FlowDirection.LeftToRight;
        this.ipPanel.WrapContents = false;
        this.ipPanel.Padding = new Padding(0);
        this.ipPanel.Margin = new Padding(0);
        this.ipPanel.AutoScroll = false;

        this.startSP = CreateLaunchButton("ZOMBIES/CAMPAIGN");
        this.startSP.Location = new Point(14, 142);
        this.startSP.Click += StartSP_Click;

        this.startMP = CreateLaunchButton("MULTIPLAYER");
        this.startMP.Location = new Point(230, 142);
        this.startMP.Click += StartMP_Click;

        this.startDedi = CreateLaunchButton("DEDICATED SERVER");
        this.startDedi.Location = new Point(446, 142);
        this.startDedi.Click += StartDedi_Click;

        this.statusStrip.BackColor = Color.Transparent;
        this.statusStrip.Items.AddRange(new ToolStripItem[] { this.statusYourIp, this.statusIp, this.statusGameId });
        this.statusStrip.Location = new Point(0, card.Height - statusStrip.Height);
        this.statusStrip.SizingGrip = false;
        this.statusStrip.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        this.statusStrip.Dock = DockStyle.Bottom;
        this.statusStrip.Visible = false;

        this.statusYourIp.Font = new Font("Consolas", 9f);
        this.statusYourIp.ForeColor = Color.White;
        this.statusYourIp.Text = "";

        this.statusIp.Font = new Font("Consolas", 9f);
        this.statusIp.ForeColor = Color.White;
        this.statusIp.Text = "";

        this.statusGameId.ForeColor = Color.White;
        this.statusGameId.Text = "";

        this.ambientLayer.Location = new Point(0, 0);
        this.ambientLayer.Size = this.card.Size;
        this.ambientLayer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        this.card.Controls.Add(this.ambientLayer);
        this.card.Controls.Add(this.labelVersion);
        this.card.Controls.Add(this.labelNick);
        this.card.Controls.Add(this.textBoxNick);
        this.card.Controls.Add(this.labelHost);
        this.card.Controls.Add(this.textBoxHost);
        this.card.Controls.Add(this.labelGameId);
        this.card.Controls.Add(this.textBoxGameId);
        this.card.Controls.Add(this.checkOverrideGameId);
        this.card.Controls.Add(this.buttonLoopback);
        this.card.Controls.Add(this.buttonTray);
        this.card.Controls.Add(this.hostStatus);
        this.card.Controls.Add(this.ipPanel);
        this.card.Controls.Add(this.startSP);
        this.card.Controls.Add(this.startMP);
        this.card.Controls.Add(this.startDedi);
        this.card.Controls.Add(this.statusStrip);

        this.Controls.Add(this.card);

        this.Resize += (s, e) => LayoutCard();

        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ambientLayer.SendToBack();

        this.card.ResumeLayout(false);
        this.card.PerformLayout();
        ResumeLayout(false);

        LayoutCard();
    }

    private Button CreateLaunchButton(string text)
    {
        Button b = new Button();
        b.Text = text;
        b.BackColor = Color.FromArgb(28, 20, 36);
        b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.MediumPurple;
        b.FlatAppearance.BorderSize = 1;
        b.Font = new Font("Consolas", 10f, FontStyle.Bold);
        b.Size = new Size(200, 70);
        return b;
    }

    


    private void LayoutCard()
    {
        card.Size = new Size(ClientSize.Width - 20, ClientSize.Height - 20);
        labelVersion.Location = new Point(10, 4);
        if (ambientLayer != null) { ambientLayer.Bounds = new Rectangle(0, 0, card.Width, card.Height); UpdateAmbientLayerRegion(); }
        buttonTray.Location = new Point(card.Width - 34, 10);
        ipPanel.Width = Math.Max(150, card.Width - 120);
        ipPanel.Location = new Point(54, 104);
        statusStrip.Location = new Point(0, card.Height - statusStrip.Height);
        UpdateResponsiveLayout();
    }
        
        private void WireGameIdValidation()
        {
            try
            {
                this.textBoxGameId.MaxLength = 10;
                this.textBoxGameId.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                        e.Handled = true;
                };

                this.textBoxGameId.TextChanged += (s, e) =>
                {
                    if (this.checkOverrideGameId != null && this.checkOverrideGameId.Checked)
                    {
                        string t = this.textBoxGameId.Text.Trim();
                        string cleaned = new string(t.Where(char.IsDigit).ToArray());
                        if (cleaned.Length > 10) cleaned = cleaned.Substring(0, 10);
                        if (cleaned != t)
                        {
                            this.textBoxGameId.Text = cleaned;
                            this.textBoxGameId.SelectionStart = this.textBoxGameId.Text.Length;
                            return;
                        }
                        if (!string.IsNullOrWhiteSpace(cleaned))
                        {
                            if (!Int64.TryParse(cleaned, out long parsed) || parsed > Int32.MaxValue)
                            {
                                cleaned = cleaned.Substring(0, Math.Min(10, cleaned.Length));
                                while (cleaned.Length > 0 && (!Int64.TryParse(cleaned, out parsed) || parsed > Int32.MaxValue))
                                    cleaned = cleaned.Substring(0, cleaned.Length - 1);
                                this.textBoxGameId.Text = cleaned;
                                this.textBoxGameId.SelectionStart = this.textBoxGameId.Text.Length;
                            }
                        }
                    }
                };
            }
            catch { }
        }

private int StableInt32FromString(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in (s ?? "Player"))
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                int result = (int)(hash & 0x7FFFFFFF);
                if (result == 0) result = 1;
                return result;
            }
        }

        private void RefreshAutoGameId()
        {
            try
            {
                string name = this.textBoxNick != null ? this.textBoxNick.Text.Trim() : "Player";
                if (string.IsNullOrWhiteSpace(name)) name = "Player";
                autoGameId = StableInt32FromString(name).ToString();
                if (this.textBoxGameId != null && this.checkOverrideGameId != null && !this.checkOverrideGameId.Checked)
                {
                    this.textBoxGameId.Text = autoGameId;
                }
            }
            catch { }
        }

        private void ApplyGameIdUiState()
        {
            try
            {
                bool enabled = this.checkOverrideGameId != null && this.checkOverrideGameId.Checked;
                if (this.textBoxGameId != null)
                {
                    this.textBoxGameId.ReadOnly = !enabled;
                    this.textBoxGameId.Enabled = true;
                                this.textBoxGameId.BackColor = enabled ? Color.FromArgb(24, 24, 28) : Color.FromArgb(28, 25, 31);
                    this.textBoxGameId.ForeColor = enabled ? Color.FromArgb(118, 118, 128) : Color.FromArgb(32, 32, 40);
                    if (!enabled)
                        this.textBoxGameId.Text = autoGameId;
                }

                if (this.labelGameId != null)
                    this.labelGameId.ForeColor = enabled ? Color.FromArgb(78, 78, 90) : Color.FromArgb(28, 28, 36);

                if (this.checkOverrideGameId != null)
                    this.checkOverrideGameId.ForeColor = enabled ? Color.FromArgb(82, 82, 92) : Color.FromArgb(30, 30, 38);
            }
            catch { }
        }


        
        
        
        
    
    
    private void UpdateResponsiveLayout()
    {
        try
        {
            bool compact = this.ClientSize.Width < 590 || this.ClientSize.Height < 242;
            bool tiny = this.ClientSize.Width < 548 || this.ClientSize.Height < 228;
            bool veryTiny = this.ClientSize.Width < 520 || this.ClientSize.Height < 214;

            this.labelVersion.Location = new Point(8, 4);

            if (this.buttonTray != null)
            {
                this.buttonTray.Location = new Point(this.card.Width - 34, 10);
                this.buttonTray.BringToFront();
            }

            int leftInputX = veryTiny ? 44 : tiny ? 48 : compact ? 52 : 56;
            int leftWidth = veryTiny ? 126 : tiny ? 136 : compact ? 148 : 160;
            int topRowY = 18;
            int secondRowY = 50;
            int labelOffsetY = 2;

            if (this.labelNick != null)
            {
                this.labelNick.Font = new Font("Consolas", veryTiny ? 5.8f : 6.1f, FontStyle.Bold);
                this.labelNick.Location = new Point(6, topRowY - labelOffsetY);
            }

            if (this.textBoxNick != null)
            {
                this.textBoxNick.Location = new Point(leftInputX, topRowY);
                this.textBoxNick.Width = leftWidth;
            }

            if (this.labelHost != null)
            {
                this.labelHost.Font = new Font("Consolas", veryTiny ? 5.8f : 6.1f, FontStyle.Bold);
                this.labelHost.Location = new Point(6, secondRowY - labelOffsetY);
            }

            if (this.textBoxHost != null)
            {
                this.textBoxHost.Location = new Point(leftInputX, secondRowY);
                this.textBoxHost.Width = leftWidth;
            }

            if (this.buttonLoopback != null && this.textBoxHost != null)
            {
                this.buttonLoopback.Size = new Size(veryTiny ? 82 : compact ? 88 : 94, 22);
                this.buttonLoopback.Location = new Point(this.textBoxHost.Left, this.textBoxHost.Bottom + 4);
            }

            if (this.hostStatus != null && this.buttonLoopback != null)
            {
                this.hostStatus.Font = new Font("Consolas", veryTiny ? 8.0f : compact ? 8.4f : 8.8f, FontStyle.Bold);
                this.hostStatus.Location = new Point(this.buttonLoopback.Right + 8, this.buttonLoopback.Top + 3);
            }

            int trayLeft = this.buttonTray != null ? this.buttonTray.Left : this.card.Width - 34;
            int gameIdWidth = veryTiny ? 64 : tiny ? 70 : compact ? 74 : 82;
            int rightInputX = Math.Max(leftInputX + leftWidth + 122, trayLeft - gameIdWidth - 18);
            int rightLabelX = Math.Max(rightInputX - 52, trayLeft - gameIdWidth - 78);

            if (this.labelGameId != null)
            {
                this.labelGameId.Font = new Font("Consolas", veryTiny ? 5.7f : 6.0f, FontStyle.Bold);
                this.labelGameId.Location = new Point(rightLabelX, topRowY + 2);
                this.labelGameId.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            if (this.textBoxGameId != null)
            {
                this.textBoxGameId.Location = new Point(rightInputX, topRowY);
                this.textBoxGameId.Width = gameIdWidth;
                this.textBoxGameId.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            if (this.checkOverrideGameId != null && this.textBoxGameId != null)
            {
                this.checkOverrideGameId.Font = new Font("Consolas", veryTiny ? 6.1f : 6.5f);
                this.checkOverrideGameId.Location = new Point(this.textBoxGameId.Left - 2, this.textBoxGameId.Bottom + 2);
                this.checkOverrideGameId.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            }

            if (this.ipPanel != null)
            {
                this.ipPanel.Location = new Point(Math.Max(8, leftInputX - 2), this.buttonLoopback.Bottom + 8);
                this.ipPanel.Width = Math.Max(120, this.card.Width - Math.Max(8, leftInputX - 2) - 18);
                this.ipPanel.Height = 28;
            }

            if (this.startSP != null && this.startMP != null && this.startDedi != null)
            {
                int gap = veryTiny ? 6 : tiny ? 8 : compact ? 10 : 12;
                int bottomMargin = veryTiny ? 7 : tiny ? 9 : compact ? 11 : 14;
                int available = this.card.Width - 20;
                int h = veryTiny ? 34 : tiny ? 40 : compact ? 48 : 62;
                int w = Math.Min(160, Math.Max(86, (available - 2 * gap) / 3));
                int rowWidth = w * 3 + gap * 2;
                int left = Math.Max(10, (this.card.Width - rowWidth) / 2);
                int minButtonTop = this.ipPanel.Bottom + 10;
                int baseY = Math.Max(minButtonTop, this.card.Height - h - bottomMargin);

                this.startSP.Size = new Size(w, h);
                this.startMP.Size = new Size(w, h);
                this.startDedi.Size = new Size(w, h);

                this.startSP.Location = new Point(left, baseY);
                this.startMP.Location = new Point(left + w + gap, baseY);
                this.startDedi.Location = new Point(left + (w + gap) * 2, baseY);

                Font f = new Font("Consolas", veryTiny ? 6.8f : tiny ? 7.25f : compact ? 7.8f : 8.5f, FontStyle.Bold);
                this.startSP.Font = f;
                this.startMP.Font = f;
                this.startDedi.Font = f;
            }
        }
        catch { }
    }

}