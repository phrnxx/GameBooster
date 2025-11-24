using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Management;
using System.IO;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;

namespace Game_Booster
{
    public partial class Form1 : Form
    {
        // Шлях до картинки
        private string imagePath = @"C:\Users\phrnxxx\source\repos\Game Booster\bg.jpg";

        // UI Елементи
        private Button btnBoost;

        // Наші кастомні вкладки (Кнопки)
        private Button tabGaming, tabSystem, tabVisuals, tabNet;
        private Panel activePanel; // Поточна видима панель

        // Панелі для кожної категорії (щоб були прозорими)
        private Panel pnlGaming, pnlSystem, pnlVisuals, pnlNet;

        // Чекбокси
        private CheckBox chkMouse, chkGameBar, chkHAGS, chkFSO;
        private CheckBox chkVisuals, chkMenu, chkNotifs;
        private CheckBox chkPower, chkHibern, chkThrottling, chkPriority;
        private CheckBox chkBloat, chkPrivacy, chkSearch, chkSysMain;
        private CheckBox chkNet, chkDNS, chkUpdate;

        // Моніторинг
        private Label lblCpuName, lblCpuLoad, lblCpuTemp;
        private Label lblGpuName, lblGpuLoad, lblGpuTemp;
        private Label lblRamInfo, lblMobo, lblPing;

        // Системні
        private Computer? computer;
        private System.Windows.Forms.Timer? updateTimer;
        private ManagementObjectSearcher? ramSearcher;
        private string motherboardName = "-";

        public Form1()
        {
            SetupWindow();
            SetupUI();
            GetMotherboardInfo();
            InitHardware();
        }

        private void SetupWindow()
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Text = "Game Booster (Transparent Core)";
            this.Size = new Size(816, 489);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            if (File.Exists(imagePath))
            {
                this.BackgroundImage = Image.FromFile(imagePath);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                this.BackColor = Color.Black;
            }
        }

        private void SetupUI()
        {
            Font titleFont = new Font("Tahoma", 16, FontStyle.Bold);
            Font itemFont = new Font("Tahoma", 9, FontStyle.Bold);
            Font statHeader = new Font("Tahoma", 9, FontStyle.Bold | FontStyle.Underline);
            Font statFont = new Font("Tahoma", 9, FontStyle.Bold);

            // === ЗАГОЛОВОК ===
            Label lblOpt = new Label() { Text = "TWEAKS", Font = titleFont, ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true, Location = new Point(30, 20) };
            this.Controls.Add(lblOpt);

            // === СТВОРЮЄМО КНОПКИ-ВКЛАДКИ ===
            int tabY = 60;
            int tabW = 100;
            int tabH = 30;

            tabGaming = CreateTabButton("GAMING", 25, tabY, tabW, tabH);
            tabSystem = CreateTabButton("SYSTEM", 130, tabY, tabW, tabH);
            tabVisuals = CreateTabButton("VISUALS", 235, tabY, tabW, tabH);
            tabNet = CreateTabButton("NETWORK", 340, tabY, tabW, tabH);

            // === СТВОРЮЄМО ПРОЗОРІ ПАНЕЛІ ===
            // Всі панелі в одному місці, одна над одною
            Point pnlLoc = new Point(25, 100);
            Size pnlSize = new Size(420, 270);

            pnlGaming = CreatePanel(pnlLoc, pnlSize);
            pnlSystem = CreatePanel(pnlLoc, pnlSize);
            pnlVisuals = CreatePanel(pnlLoc, pnlSize);
            pnlNet = CreatePanel(pnlLoc, pnlSize);

            // --- НАПОВНЕННЯ GAMING ---
            int y = 5;
            chkMouse = AddCheckToPanel(pnlGaming, "Input Lag Fix (No Mouse Accel)", ref y, itemFont);
            chkGameBar = AddCheckToPanel(pnlGaming, "Disable Game Bar & DVR", ref y, itemFont);
            chkHAGS = AddCheckToPanel(pnlGaming, "Enable HAGS (GPU Sched)", ref y, itemFont);
            chkFSO = AddCheckToPanel(pnlGaming, "Disable Fullscreen Optimization", ref y, itemFont);

            // --- НАПОВНЕННЯ SYSTEM ---
            y = 5;
            chkPower = AddCheckToPanel(pnlSystem, "Ultimate Performance Plan", ref y, itemFont);
            chkPriority = AddCheckToPanel(pnlSystem, "High Priority for Games", ref y, itemFont);
            chkThrottling = AddCheckToPanel(pnlSystem, "Disable Network Throttling", ref y, itemFont);
            chkHibern = AddCheckToPanel(pnlSystem, "Disable Hibernation", ref y, itemFont);
            chkBloat = AddCheckToPanel(pnlSystem, "Debloat (Copilot, StickyKeys)", ref y, itemFont);

            // --- НАПОВНЕННЯ VISUALS ---
            y = 5;
            chkVisuals = AddCheckToPanel(pnlVisuals, "Optimize Visual Effects", ref y, itemFont);
            chkMenu = AddCheckToPanel(pnlVisuals, "Classic Win10 Context Menu", ref y, itemFont);
            chkNotifs = AddCheckToPanel(pnlVisuals, "Disable Notifications", ref y, itemFont);
            chkPrivacy = AddCheckToPanel(pnlVisuals, "Disable Telemetry", ref y, itemFont);
            chkSearch = AddCheckToPanel(pnlVisuals, "Disable Search Indexing", ref y, itemFont);

            // --- НАПОВНЕННЯ NETWORK ---
            y = 5;
            chkNet = AddCheckToPanel(pnlNet, "Optimize TCP/IP (NoDelay)", ref y, itemFont);
            chkDNS = AddCheckToPanel(pnlNet, "Flush DNS Cache", ref y, itemFont);
            chkUpdate = AddCheckToPanel(pnlNet, "Disable P2P Updates", ref y, itemFont);
            chkSysMain = AddCheckToPanel(pnlNet, "Disable SysMain Service", ref y, itemFont);

            // Активація першої вкладки
            SwitchTab(tabGaming, pnlGaming);

            // === КНОПКА APPLY ===
            btnBoost = new Button();
            btnBoost.Text = "APPLY";
            btnBoost.Font = new Font("Tahoma", 11, FontStyle.Bold);
            btnBoost.BackColor = Color.Black;
            btnBoost.ForeColor = Color.White;
            btnBoost.FlatStyle = FlatStyle.Flat;
            btnBoost.FlatAppearance.BorderColor = Color.White;
            btnBoost.Size = new Size(150, 35);
            btnBoost.Location = new Point(30, 380);
            btnBoost.Cursor = Cursors.Hand;
            btnBoost.Click += BtnBoost_Click;
            btnBoost.MouseEnter += (s, e) => { btnBoost.BackColor = Color.White; btnBoost.ForeColor = Color.Black; };
            btnBoost.MouseLeave += (s, e) => { btnBoost.BackColor = Color.Black; btnBoost.ForeColor = Color.White; };
            this.Controls.Add(btnBoost);

            // === ПРАВА ЧАСТИНА (MONITOR) ===
            int rightX = 560;
            int statY = 60;

            Label lblStats = new Label() { Text = "MONITOR", Font = titleFont, ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true, Location = new Point(rightX, 20) };
            this.Controls.Add(lblStats);

            AddLabel("CPU", rightX, ref statY, statHeader);
            lblCpuName = AddLabel("-", rightX, ref statY, statFont);
            lblCpuLoad = AddLabel("Load: 0% | Temp: 0°C", rightX, ref statY, statFont);
            statY += 10;

            AddLabel("GPU", rightX, ref statY, statHeader);
            lblGpuName = AddLabel("-", rightX, ref statY, statFont);
            lblGpuLoad = AddLabel("Core: 0% | Temp: 0°C", rightX, ref statY, statFont);
            statY += 10;

            AddLabel("SYSTEM", rightX, ref statY, statHeader);
            lblRamInfo = AddLabel("RAM: 0/0 GB (0%)", rightX, ref statY, statFont);
            lblMobo = AddLabel("MB: -", rightX, ref statY, statFont);
            lblPing = AddLabel("Ping: ...", rightX, ref statY, statFont);
        }

        // === СИСТЕМА ВКЛАДОК (ЛОГІКА) ===

        private Button CreateTabButton(string text, int x, int y, int w, int h)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Tahoma", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            // При кліку визначаємо, яку панель показати
            if (text == "GAMING") btn.Click += (s, e) => SwitchTab(btn, pnlGaming);
            if (text == "SYSTEM") btn.Click += (s, e) => SwitchTab(btn, pnlSystem);
            if (text == "VISUALS") btn.Click += (s, e) => SwitchTab(btn, pnlVisuals);
            if (text == "NETWORK") btn.Click += (s, e) => SwitchTab(btn, pnlNet);

            this.Controls.Add(btn);
            return btn;
        }

        private Panel CreatePanel(Point loc, Size size)
        {
            Panel p = new Panel();
            p.Location = loc;
            p.Size = size;
            p.BackColor = Color.Transparent; // ПРОЗОРІСТЬ!
            p.Visible = false; // Спочатку прихована
            this.Controls.Add(p);
            return p;
        }

        private void SwitchTab(Button activeBtn, Panel targetPanel)
        {
            // Скидаємо стилі всіх кнопок
            ResetTabStyle(tabGaming);
            ResetTabStyle(tabSystem);
            ResetTabStyle(tabVisuals);
            ResetTabStyle(tabNet);

            // Активуємо натиснуту кнопку (білий текст, підкреслення можна імітувати кольором)
            activeBtn.ForeColor = Color.Cyan; // Активна вкладка - блакитна
            activeBtn.BackColor = Color.FromArgb(50, 0, 0, 0); // Легке затемнення

            // Перемикаємо панелі
            if (pnlGaming != null) pnlGaming.Visible = false;
            if (pnlSystem != null) pnlSystem.Visible = false;
            if (pnlVisuals != null) pnlVisuals.Visible = false;
            if (pnlNet != null) pnlNet.Visible = false;

            targetPanel.Visible = true;
            activePanel = targetPanel;
        }

        private void ResetTabStyle(Button btn)
        {
            btn.ForeColor = Color.Gray;
            btn.BackColor = Color.Transparent;
        }

        private CheckBox AddCheckToPanel(Panel p, string text, ref int y, Font font)
        {
            CheckBox cb = new CheckBox();
            cb.Text = "> " + text;
            cb.Font = font;
            cb.ForeColor = Color.White;
            cb.BackColor = Color.Transparent;
            cb.AutoSize = true;
            cb.Location = new Point(10, y);
            cb.Checked = true;
            p.Controls.Add(cb);
            y += 35;
            return cb;
        }

        // === ЛОГІКА OPTIMIZATION ===
        private async void BtnBoost_Click(object? sender, EventArgs e)
        {
            btnBoost.Enabled = false;
            btnBoost.Text = "WORKING...";
            btnBoost.BackColor = Color.DarkRed;

            await Task.Run(() =>
            {
                try
                {
                    // Gaming
                    if (chkMouse.Checked) { SetReg(@"HKCU\Control Panel\Mouse", "MouseSpeed", "0"); SetReg(@"HKCU\Control Panel\Mouse", "MouseThreshold1", "0"); SetReg(@"HKCU\Control Panel\Mouse", "MouseThreshold2", "0"); }
                    if (chkGameBar.Checked) { SetReg(@"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0); SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0); }
                    if (chkHAGS.Checked) SetReg(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
                    if (chkFSO.Checked) SetReg(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);

                    // System
                    if (chkPower.Checked) { RunCmd("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61"); RunCmd("powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61"); }
                    if (chkPriority.Checked) { SetReg(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", 38); }
                    if (chkThrottling.Checked) SetReg(@"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", 0xFFFFFFFF);
                    if (chkHibern.Checked) RunCmd("powercfg -h off");

                    // Visuals
                    if (chkVisuals.Checked) { SetReg(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0); SetReg(@"HKCU\Control Panel\Desktop", "MenuShowDelay", "0"); }
                    if (chkMenu.Checked) SetReg(@"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "");

                    // Network
                    if (chkNet.Checked) RunCmd("netsh int tcp set global autotuninglevel=normal");
                    if (chkDNS.Checked) RunCmd("ipconfig /flushdns");

                    // Debloat
                    if (chkBloat.Checked) { SetReg(@"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1); SetReg(@"HKCU\Control Panel\Accessibility\StickyKeys", "Flags", "506"); }
                    if (chkPrivacy.Checked) SetReg(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
                    if (chkSearch.Checked) { RunCmd("net stop WSearch"); RunCmd("net stop SysMain"); }
                }
                catch { }

                System.Threading.Thread.Sleep(2000);
            });

            MessageBox.Show("Optimizations Applied!", "LOG");
            btnBoost.Text = "APPLY";
            btnBoost.BackColor = Color.Black;
            btnBoost.Enabled = true;
        }

        private void SetReg(string path, string name, object value)
        {
            try
            {
                if (path.StartsWith("HKCU")) Registry.SetValue(path.Replace("HKCU", "HKEY_CURRENT_USER"), name, value);
                else if (path.StartsWith("HKLM")) Registry.SetValue(path.Replace("HKLM", "HKEY_LOCAL_MACHINE"), name, value);
            }
            catch { }
        }

        private void RunCmd(string cmd)
        {
            try { Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/C " + cmd, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }); } catch { }
        }

        private Label AddLabel(string text, int x, ref int y, Font font)
        {
            Label l = new Label() { Text = text, Location = new Point(x, y), Font = font, ForeColor = Color.White, BackColor = Color.Transparent, AutoSize = true };
            this.Controls.Add(l);
            y += 20;
            return l;
        }

        private void GetMotherboardInfo()
        {
            try
            {
                foreach (ManagementObject obj in new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard").Get())
                    motherboardName = obj["Product"].ToString();
            }
            catch { motherboardName = "Unknown"; }
        }

        private void InitHardware()
        {
            computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMemoryEnabled = true, IsMotherboardEnabled = true };
            try { computer.Open(); } catch { }
            ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000;
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            string ping = await Task.Run(() => {
                try { return new Ping().Send("8.8.8.8", 1000).RoundtripTime + " ms"; } catch { return "Timeout"; }
            });

            if (computer == null || ramSearcher == null) return;
            await Task.Run(() =>
            {
                string cName = "-", gName = "-";
                float cLoad = 0, cTemp = 0, gLoad = 0, gTemp = 0;
                try
                {
                    foreach (var hw in computer.Hardware)
                    {
                        hw.Update();
                        if (hw.HardwareType == HardwareType.Cpu)
                        {
                            cName = hw.Name;
                            foreach (var s in hw.Sensors)
                            {
                                if (s.SensorType == SensorType.Load && s.Name.Contains("Total")) cLoad = s.Value ?? 0;
                                if (s.SensorType == SensorType.Temperature)
                                    if (s.Name.Contains("Package") || s.Name.Contains("Tctl") || s.Name.Contains("Core"))
                                        if (s.Value > cTemp) cTemp = s.Value ?? 0;
                            }
                        }
                        if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd)
                        {
                            gName = hw.Name;
                            foreach (var s in hw.Sensors)
                            {
                                if (s.SensorType == SensorType.Load && s.Name.Contains("Core")) gLoad = s.Value ?? 0;
                                if (s.SensorType == SensorType.Temperature && s.Name.Contains("GPU")) gTemp = s.Value ?? 0;
                            }
                        }
                    }
                    double totalGB = 0, freeGB = 0;
                    foreach (ManagementObject obj in ramSearcher.Get())
                    {
                        totalGB = Math.Round(Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024 / 1024, 2);
                        freeGB = Math.Round(Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024 / 1024, 2);
                    }
                    double usedPerc = totalGB > 0 ? Math.Round((1 - freeGB / totalGB) * 100) : 0;
                    double usedGB = totalGB - freeGB;

                    this.BeginInvoke((MethodInvoker)(() => {
                        lblCpuName!.Text = cName;
                        lblCpuLoad!.Text = $"Load: {cLoad:F0}% | Temp: {cTemp:F0}°C";
                        lblGpuName!.Text = gName;
                        lblGpuLoad!.Text = $"Core: {gLoad:F0}% | Temp: {gTemp:F0}°C";
                        lblRamInfo!.Text = $"RAM: {usedGB:F2} / {totalGB} GB ({usedPerc}%)";
                        lblMobo!.Text = $"MB: {motherboardName}";
                        lblPing!.Text = $"Ping: {ping}";
                    }));
                }
                catch { }
            });
        }
        private void Form1_Load(object sender, EventArgs e) { }
    }
}