
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
    private readonly Timer hostEnsure = new Timer();
    private readonly List<Button> ipCopyButtons = new List<Button>();
    private readonly string[] startupArgs;
    private bool startupArgsConsumed;

    private Process currentGame;
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
    private Panel card;

    public MainWindow(string[] args = null)
    {
        startupArgs = args ?? Array.Empty<string>();
        InitializeComponent();

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

        textBoxHost.Text = GetIni("Config", "Host", "127.0.0.1");
        if (string.IsNullOrWhiteSpace(textBoxHost.Text))
            textBoxHost.Text = "127.0.0.1";
        textBoxNick.Text = GetIni("Config", "Nickname", Environment.UserName);

        EnsureGameId();
        textBoxGameId.Text = GetIni("Config", "GameID", "");
        RefreshGameIdLabel();
        BuildIpButtons();

        processWatch.Interval = 700;
        processWatch.Tick += ProcessWatch_Tick;

        hostEnsure.Interval = 1200;
        hostEnsure.Tick += (s, e) => EnsureHostMode();

        Shown += (s, e) =>
        {
            EnsureHostMode();
            hostEnsure.Start();
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

    private void WriteConfig(string gameMode, bool hosting)
    {
        string selectedLocalIp = GetPreferredLocalIPv4();
        string hostInput = textBoxHost.Text.Trim();
        string nick = textBoxNick.Text.Trim();
        if (string.IsNullOrWhiteSpace(nick))
            nick = Environment.UserName;

        bool useLoopback = hostInput == "127.0.0.1";
        string effectiveHostIp = useLoopback ? selectedLocalIp : hostInput;
        string hostModeValue = hosting ? "1" : "0";
        string gameId = (checkOverrideGameId != null && checkOverrideGameId.Checked) ? textBoxGameId.Text.Trim() : autoGameId;
        if (string.IsNullOrWhiteSpace(gameId) || !Int32.TryParse(gameId, out int gid) || gid < 0)
            gameId = autoGameId;

        string configHost = hostInput;
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

        statusIp.Text = string.IsNullOrWhiteSpace(selectedLocalIp) ? "No local IPv4" : selectedLocalIp;
        RefreshGameIdLabel();
    }


    private string GetLaunchOptionsText()
    {
        return "Launch options:\n-zm or -sp  Zombies/Campaign\n-mp  Multiplayer\n-server  Dedicated server\n-savedip  use saved Host IP\n-<IP>  override Host IP with a specific IPv4";
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

        bool wrap = this.card.Width < 720;

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

    private void EnsureHostMode()
    {
        try
        {
            BGT5LMS.Start(3074);
            hostStatus.ForeColor = Color.FromArgb(170, 230, 170);
            hostStatus.Text = "HOSTMODE ACTIVE";
            ApplyHostStatusLayout();
            WriteConfig(GetIni("Config", "Game", "Singleplayer"), true);
        }
        catch
        {
            hostStatus.Text = "HOSTMODE ERROR";
            hostStatus.ForeColor = Color.OrangeRed;
            ApplyHostStatusLayout();
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

    private void LaunchGame(string gameMode, string exePath)
    {
        if (launchInProgress)
            return;

        launchInProgress = true;
        try
        {
            WriteConfig(gameMode, true);
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

    private void LaunchDedicated()
    {
        try
        {
            WriteConfig("MULTIPLAYER", true);
            string? exe = FindMpExe();
            if (exe == null)
                throw new FileNotFoundException("Could not find the MP executable by expected file size.");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string cfgPath =
                File.Exists(Path.Combine(baseDir, "main", "bgserver.cfg")) ? Path.Combine(baseDir, "main", "bgserver.cfg") :
                File.Exists(Path.Combine(baseDir, "main", "server.cfg")) ? Path.Combine(baseDir, "main", "server.cfg") :
                File.Exists(Path.Combine(baseDir, "bgserver.cfg")) ? Path.Combine(baseDir, "bgserver.cfg") :
                File.Exists(Path.Combine(baseDir, "server.cfg")) ? Path.Combine(baseDir, "server.cfg") :
                Path.Combine(baseDir, "main", "bgserver.cfg");

            string cfgArg = cfgPath.Replace(baseDir + Path.DirectorySeparatorChar, "").Replace("\\", "/");
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

        LaunchGame("MULTIPLAYER", exe);
    }

    
    
    private void StartDedi_Click(object? sender, EventArgs e)
    {
        try
        {
            EnsureHostMode();

            string? exePath = FindMpExe();
            if (string.IsNullOrWhiteSpace(exePath))
            {
                MessageBox.Show("Could not find the multiplayer executable.", "Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            WriteConfig("Multiplayer", true);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string mainDir = Path.Combine(baseDir, "main");
            string cfgName = "";

            if (File.Exists(Path.Combine(mainDir, "bgserver.cfg")))
                cfgName = "bgserver.cfg";
            else if (File.Exists(Path.Combine(mainDir, "server.cfg")))
                cfgName = "server.cfg";
            else
            {
                MessageBox.Show("Server.cfg not found", "Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentGame = Process.Start(exePath, $"+set dedicated 2 +set sv_licensenum 0 +set net_port 27960 +exec {cfgName}");

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
            Button b = MakeSmallButton("Copy " + ip);
            b.Width = 130;
            b.Height = 24;
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
        b.Font = new Font("Consolas", 8.25f);
        return b;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try { hostEnsure.Stop(); processWatch.Stop(); } catch { }
        try { if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); } } catch { }
        try { BGT5LMS.Stop(); } catch { }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        ComponentResourceManager resources = new ComponentResourceManager(typeof(MainWindow));
        this.components = new Container();
        this.card = new Panel();
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
        this.ClientSize = new Size(680, 300);
        this.MinimumSize = new Size(560, 250);
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
        this.labelVersion.Text = "v3.2";
        this.labelVersion.Cursor = Cursors.Help;
        this.trayToolTip.SetToolTip(this.labelVersion, GetLaunchOptionsText());
        this.labelVersion.MouseEnter += (s, e) => this.trayToolTip.Show(GetLaunchOptionsText(), this.labelVersion, 0, this.labelVersion.Height + 2, 4000);
        this.labelVersion.MouseLeave += (s, e) => this.trayToolTip.Hide(this.labelVersion);
        this.labelVersion.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        this.labelVersion.Location = new Point(10, 8);


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
        this.labelNick.Font = new Font("Consolas", 9.75f);
        this.labelNick.Text = "Nickname:";
        this.labelNick.Location = new Point(14, 18);

        this.textBoxNick.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxNick.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxNick.ForeColor = Color.Gainsboro;
        this.textBoxNick.Location = new Point(100, 18);
        this.textBoxNick.Size = new Size(160, 20);
        this.textBoxNick.TextAlign = HorizontalAlignment.Center;
        this.textBoxNick.TextChanged += (s, e) => { RefreshAutoGameId(); ApplyGameIdUiState(); };

        this.labelHost.AutoSize = true;
        this.labelHost.BackColor = Color.FromArgb(14,14,14);
        this.labelHost.ForeColor = Color.White;
        this.labelHost.Font = new Font("Consolas", 9.75f);
        this.labelHost.Text = "Host IP:";
        this.labelHost.Location = new Point(280, 18);

        this.textBoxHost.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxHost.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxHost.ForeColor = Color.Gainsboro;
        this.textBoxHost.Location = new Point(350, 18);
        this.textBoxHost.Size = new Size(170, 20);
        this.textBoxHost.TextAlign = HorizontalAlignment.Center;

        this.buttonLoopback = MakeSmallButton("127.0.0.1");
        this.buttonLoopback.Location = new Point(this.textBoxHost.Left, this.textBoxHost.Bottom + 6);
        this.buttonLoopback.Click += (s, e) => { textBoxHost.Text = "127.0.0.1"; };
        this.buttonLoopback.Size = new Size(90, 24);
        this.labelGameId.AutoSize = true;
        this.labelGameId.BackColor = Color.FromArgb(14,14,14);
        this.labelGameId.ForeColor = Color.White;
        this.labelGameId.Font = new Font("Consolas", 9.75f);
        this.labelGameId.Text = "PlayerID:";
        this.labelGameId.Location = new Point(14, 50);

        this.textBoxGameId.BackColor = Color.FromArgb(30, 26, 36);
        this.textBoxGameId.BorderStyle = BorderStyle.FixedSingle;
        this.textBoxGameId.ForeColor = Color.Gainsboro;
        this.textBoxGameId.Location = new Point(100, 50);
        this.textBoxGameId.Size = new Size(160, 22);
        this.textBoxGameId.TextAlign = HorizontalAlignment.Center;

        this.checkOverrideGameId.Text = "Overwrite";
        this.checkOverrideGameId.AutoSize = true;
        this.checkOverrideGameId.BackColor = Color.FromArgb(14,14,14);
        this.checkOverrideGameId.ForeColor = Color.Gainsboro;
        this.checkOverrideGameId.Location = new Point(this.textBoxGameId.Left, this.textBoxGameId.Bottom + 4);
        this.checkOverrideGameId.CheckedChanged += (s, e) => ApplyGameIdUiState();


        this.hostStatus.AutoSize = true;
        this.hostStatus.BackColor = Color.Transparent;
        this.hostStatus.ForeColor = Color.LightGreen;
        this.hostStatus.Font = new Font("Consolas", 9.75f, FontStyle.Bold);
        this.hostStatus.Text = "HOSTMODE ACTIVE";
        this.hostStatus.Location = new Point(280, 52);

        this.ipPanel.BackColor = Color.Transparent;
        this.ipPanel.Location = new Point(14, 90);
        this.ipPanel.Size = new Size(640, 34);
        this.ipPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

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
        labelVersion.Location = new Point(10, 8);
        buttonTray.Location = new Point(card.Width - 34, 10);
        ipPanel.Width = card.Width - 28;
        ipPanel.Location = new Point(14, 90);
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
                    this.textBoxGameId.BackColor = enabled ? Color.FromArgb(26,26,30) : Color.FromArgb(38,38,44);
                    this.textBoxGameId.ForeColor = enabled ? Color.Gainsboro : Color.Gray;
                    if (!enabled)
                        this.textBoxGameId.Text = autoGameId;
                }
            }
            catch { }
        }


        
        
        
        
    
    
    private void UpdateResponsiveLayout()
    {
        try
        {
            bool compact = this.ClientSize.Width < 600 || this.ClientSize.Height < 225;
            bool tiny = this.ClientSize.Width < 590 || this.ClientSize.Height < 220;
            bool veryTiny = this.ClientSize.Width < 570 || this.ClientSize.Height < 200;

            this.labelVersion.Location = new Point(10, 8);

            if (this.textBoxHost != null && this.buttonLoopback != null)
            {
                int hostLeft = this.textBoxHost.Left;
                int rowRight = this.card.Width - 18;
                int reservedRight = 150;
                int hostWidth = Math.Max(120, rowRight - hostLeft - reservedRight);
                if (compact) hostWidth = Math.Max(110, hostWidth - 20);
                this.textBoxHost.Width = hostWidth;

                this.buttonLoopback.Size = new Size(compact ? 84 : 90, 24);
                this.buttonLoopback.Location = new Point(this.textBoxHost.Left, this.textBoxHost.Bottom + 6);
            }

            ApplyHostStatusLayout();

            if (this.checkOverrideGameId != null && this.textBoxGameId != null)
                this.checkOverrideGameId.Location = new Point(this.textBoxGameId.Left, this.textBoxGameId.Bottom + 4);

            if (this.buttonTray != null)
            {
                this.buttonTray.Location = new Point(this.card.Width - 34, 10);
                this.buttonTray.BringToFront();
            }

            if (this.startSP != null && this.startMP != null && this.startDedi != null)
            {
                int gap = veryTiny ? 6 : tiny ? 8 : compact ? 10 : 12;
                int bottomMargin = veryTiny ? 6 : tiny ? 8 : compact ? 10 : 12;
                int available = this.card.Width - 28;
                int h = veryTiny ? 40 : tiny ? 45 : compact ? 55 : 65;
                int w = Math.Min(160, Math.Max(90, (available - 2 * gap) / 3));
                int rowWidth = w * 3 + gap * 2;
                int left = Math.Max(14, (this.card.Width - rowWidth) / 2);
                int baseY = this.card.Height - h - bottomMargin;

                this.startSP.Size = new Size(w, h);
                this.startMP.Size = new Size(w, h);
                this.startDedi.Size = new Size(w, h);

                this.startSP.Location = new Point(left, baseY);
                this.startMP.Location = new Point(left + w + gap, baseY);
                this.startDedi.Location = new Point(left + (w + gap) * 2, baseY);

                Font f = new Font("Consolas", veryTiny ? 7.25f : tiny ? 7.75f : compact ? 8.25f : 8.75f, FontStyle.Bold);
                this.startSP.Font = f;
                this.startMP.Font = f;
                this.startDedi.Font = f;
            }
        }
        catch { }
    }
}
