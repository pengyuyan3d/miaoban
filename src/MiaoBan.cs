// 喵伴 MiaoBan v2 · 桌面陪伴猫（主程序）
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MiaoBan
{
    // ---------- Win32 ----------
    static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; public POINT(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int CX; public int CY; public SIZE(int x, int y) { CX = x; CY = y; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public int biSize; public int biWidth; public int biHeight; public short biPlanes;
            public short biBitCount; public int biCompression; public int biSizeImage;
            public int biXPelsPerMeter; public int biYPelsPerMeter; public int biClrUsed; public int biClrImportant;
        }

        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x02;

        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObj);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr hSection, uint offset);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public const int WH_KEYBOARD_LL = 13;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);
        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
        [DllImport("user32.dll")]
        public static extern bool CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);
        public static bool GetCursorPos(out POINT p) { return GetCursorPosNative(out p); }
        [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
        public static extern bool GetCursorPosNative(out POINT p);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        public static void KeepTop(IntPtr h)
        {
            try { SetWindowPos(h, (IntPtr)(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010); } catch { }
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
        public static void PostMessageNull(IntPtr h) { try { PostMessage(h, 0, IntPtr.Zero, IntPtr.Zero); } catch { } }
        [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int value);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();

        public static void EnableDpi()
        {
            try { if (SetProcessDpiAwareness(2) != 0) SetProcessDPIAware(); }
            catch { try { SetProcessDPIAware(); } catch { } }
        }
    }

    // ---------- 配置 ----------
    class Config
    {
        public int Size = 1;          // 0 小 1 中 2 大
        public int X = int.MinValue, Y = int.MinValue;
        public bool TopMost = true;
        public bool AutoStart = false;
        public bool Water = true;
        public bool Stretch = true;
        public string Name = "";

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiaoBan"); }
        }
        static string FilePath { get { return Path.Combine(Dir, "config.ini"); } }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                if (!File.Exists(FilePath)) return c;
                foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                    if (k == "size") { int n; if (int.TryParse(v, out n)) c.Size = Math.Max(0, Math.Min(2, n)); }
                    else if (k == "x") { int n; if (int.TryParse(v, out n)) c.X = n; }
                    else if (k == "y") { int n; if (int.TryParse(v, out n)) c.Y = n; }
                    else if (k == "topmost") c.TopMost = v == "1";
                    else if (k == "autostart") c.AutoStart = v == "1";
                    else if (k == "water") c.Water = v == "1";
                    else if (k == "stretch") c.Stretch = v == "1";
                    else if (k == "name") c.Name = v;
                }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("size=" + Size);
                sb.AppendLine("x=" + X);
                sb.AppendLine("y=" + Y);
                sb.AppendLine("topmost=" + (TopMost ? "1" : "0"));
                sb.AppendLine("autostart=" + (AutoStart ? "1" : "0"));
                sb.AppendLine("water=" + (Water ? "1" : "0"));
                sb.AppendLine("stretch=" + (Stretch ? "1" : "0"));
                sb.AppendLine("name=" + Name);
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }

    // ---------- 主窗口 ----------
    class PetForm : Form
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int WM_NCHITTEST = 0x0084, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
        const int WM_MOUSEMOVE = 0x0200, WM_RBUTTONUP = 0x0205, WM_LBUTTONDBLCLK = 0x0203;

        Config cfg;
        float dpiScale = 1f;
        float sizeFactor { get { return cfg.Size == 0 ? 0.82f : (cfg.Size == 1 ? 1f : 1.3f); } }

        Bitmap frame;          // 窗口画面
        Bitmap catBmp;         // 48x52 小图
        int P = 2;             // 放大倍数（整数，保证像素清晰）

        DateTime t0 = DateTime.Now;
        int tick = 0;

        // 行为
        PetState state = PetState.Idle;
        DateTime stateUntil = DateTime.MinValue;
        DateTime lastTyping = DateTime.MinValue;
        DateTime lastActivity = DateTime.MinValue;
        DateTime nextIdleAct = DateTime.Now.AddSeconds(6);
        DateTime nextWander = DateTime.Now.AddSeconds(14);
        DateTime nextBlink = DateTime.Now.AddSeconds(2);
        DateTime blinkEnd = DateTime.MinValue;
        DateTime huntUntil = DateTime.MinValue;
        DateTime nextHuntHop = DateTime.MinValue;
        DateTime wobbleEnd = DateTime.MinValue;
        DateTime focusEnd = DateTime.MinValue;
        bool focusRunning;
        DateTime nextWater = DateTime.MinValue;
        DateTime nextStretch = DateTime.MinValue;
        DateTime helloUntil = DateTime.MinValue;

        // 跳跃
        bool hopping;
        DateTime hopStart;
        float hopDur = 0.26f, hopFromX, hopToX, hopFromY, hopToY, hopHeight = 6f;

        // 打字热度
        float heat;
        int keyRate;
        DateTime rateWindow = DateTime.Now;
        int keysInWindow;

        // 拖动
        bool dragging, dragMoved;
        Point dragOffset;
        DateTime dragRelease = DateTime.MinValue;

        // 提示气泡
        string bubble = null;
        DateTime bubbleUntil = DateTime.MinValue;

        NotifyIcon tray;

        bool[] keyDown = new bool[256];
        System.Windows.Forms.Timer animTimer, pollTimer, secTimer;

        public PetForm(Config c)
        {
            cfg = c;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = cfg.TopMost;
            StartPosition = FormStartPosition.Manual;
            Text = "喵伴";

            BuildTray();

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 16;
            animTimer.Tick += delegate { SafeTick(); };

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 60;
            pollTimer.Tick += delegate { PollKeys(); };

            secTimer = new System.Windows.Forms.Timer();
            secTimer.Interval = 1000;
            secTimer.Tick += delegate { EverySecond(); };

            Layout();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00080000; // WS_EX_LAYERED
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        int CatW { get { return CatArt.W * P; } }
        int CatH { get { return CatArt.H * P; } }
        int CanvasW { get { return Math.Max((int)(132 * dpiScale), CatW + (int)(16 * dpiScale)); } }
        int CanvasH { get { return CatH + (int)(34 * dpiScale); } }

        new void Layout()
        {
            dpiScale = 1f;
            try { uint d = Native.GetDpiForWindow(Handle); if (d > 0) dpiScale = d / 96f; } catch { }
            P = Math.Max(2, (int)Math.Round(1.6f * dpiScale)) + cfg.Size;
            int w = CanvasW, h = CanvasH;
            if (frame == null || frame.Width != w || frame.Height != h)
            {
                if (frame != null) frame.Dispose();
                frame = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            }
            if (catBmp == null)
            {
                catBmp = new Bitmap(CatArt.W, CatArt.H, PixelFormat.Format32bppArgb);
            }
            Width = w; Height = h;
        }

        List<MenuItemDef> BuildMenuItems()
        {
            List<MenuItemDef> list = new List<MenuItemDef>();
            list.Add(new MenuItemDef { Text = "摸摸头", Act = delegate { Pet(); } });
            list.Add(new MenuItemDef { Text = focusRunning ? "结束专注" : "开始专注 25 分钟", Act = delegate { ToggleFocus(); } });
            list.Add(new MenuItemDef { Text = "起来动一动", Act = delegate { DoStretch(true); } });
            list.Add(new MenuItemDef { Text = "去追鼠标", Act = delegate { StartHunt(TimeSpan.FromSeconds(6)); } });
            list.Add(new MenuItemDef { Sep = true });
            list.Add(new MenuItemDef { Text = "小一点", Check = cfg.Size == 0, Act = delegate { SetSize(0); } });
            list.Add(new MenuItemDef { Text = "中号", Check = cfg.Size == 1, Act = delegate { SetSize(1); } });
            list.Add(new MenuItemDef { Text = "大一点", Check = cfg.Size == 2, Act = delegate { SetSize(2); } });
            list.Add(new MenuItemDef { Sep = true });
            list.Add(new MenuItemDef { Text = "总在最前", Check = cfg.TopMost, Act = delegate { cfg.TopMost = !cfg.TopMost; TopMost = cfg.TopMost; cfg.Save(); Apply(); } });
            list.Add(new MenuItemDef { Text = "喝水提醒", Check = cfg.Water, Act = delegate { cfg.Water = !cfg.Water; nextWater = DateTime.Now.AddMinutes(45); cfg.Save(); } });
            list.Add(new MenuItemDef { Text = "久坐提醒", Check = cfg.Stretch, Act = delegate { cfg.Stretch = !cfg.Stretch; nextStretch = DateTime.Now.AddMinutes(45); cfg.Save(); } });
            list.Add(new MenuItemDef { Text = "开机自动出现", Check = cfg.AutoStart, Act = delegate { cfg.AutoStart = !cfg.AutoStart; SetAutoStart(cfg.AutoStart); cfg.Save(); } });
            list.Add(new MenuItemDef { Sep = true });
            list.Add(new MenuItemDef { Text = "关于喵伴", Act = delegate { ShowAbout(); } });
            list.Add(new MenuItemDef { Text = "退出", Act = delegate { Quit(); } });
            return list;
        }

        void ShowMenu()
        {
            List<MenuItemDef> items = BuildMenuItems();
            IntPtr h = Native.CreatePopupMenu();
            try
            {
                List<MenuItemDef> map = new List<MenuItemDef>();
                foreach (MenuItemDef it in items)
                {
                    if (it.Sep)
                    {
                        Native.AppendMenuW(h, 0x800u, IntPtr.Zero, null);
                        continue;
                    }
                    int id = map.Count + 1;
                    Native.AppendMenuW(h, 0u, (IntPtr)id, it.Text);
                    if (it.Check) Native.CheckMenuItem(h, (uint)id, 0x8u);
                    map.Add(it);
                }
                Native.SetForegroundWindow(Handle);
                Point pos = Cursor.Position;
                int cmd = Native.TrackPopupMenu(h, 0x100u | 0x2u | 0x80u, pos.X, pos.Y, 0, Handle, IntPtr.Zero);
                Native.PostMessageNull(Handle);
                if (cmd > 0 && cmd <= map.Count)
                {
                    try { map[cmd - 1].Act(); } catch { }
                }
            }
            finally { Native.DestroyMenu(h); }
        }
        void BuildTray()
        {
            tray = new NotifyIcon();
            tray.Icon = MakeIcon();
            tray.Text = "喵伴 · 桌面陪伴猫";
            tray.Visible = true;
            tray.MouseClick += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) { Show(); Apply(); Pet(); }
            };
            ContextMenuStrip tm = new ContextMenuStrip();
            tm.Font = new Font("Microsoft YaHei UI", 9.5f);
            ToolStripMenuItem show = new ToolStripMenuItem("把小猫叫出来");
            show.Click += delegate { Show(); Layout(); PlaceDefault(); Say("我在这儿", 2.5); };
            tm.Items.Add(show);
            tm.Items.Add("退出", null, delegate { Quit(); });
            tray.ContextMenuStrip = tm;
        }

        Icon MakeIcon()
        {
            using (Bitmap bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    using (Bitmap cat = RenderCat(new Pose()))
                        g.DrawImage(cat, new Rectangle(0, 0, 64, 64));
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        void SetSize(int idx)
        {
            cfg.Size = idx;
            cfg.Save();
            int cx = Left + Width / 2, bottom = Top + Height;
            Layout();
            SetBounds(cx - Width / 2, bottom - Height, Width, Height);
            Apply();
        }

        void SetAutoStart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k == null) return;
                    if (on) k.SetValue("MiaoBan", "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue("MiaoBan", false);
                }
            }
            catch { }
        }

        void ShowAbout()
        {
            MessageBox.Show("喵伴 MiaoBan 2.0\n\n一只住在桌面上的小猫。\n你打字它就敲键盘，鼠标一快它就追，闲着会睡觉。\n\n左键摸头 / 拖动拎起来 / 右键菜单\n配置在 %APPDATA%\\MiaoBan", "关于喵伴",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void Quit()
        {
            try { cfg.X = Left; cfg.Y = Top; cfg.Save(); } catch { }
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            Application.Exit();
        }

        // ---- 位置 ----
        void PlaceDefault()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            SetBounds(wa.Right - Width - (int)(30 * dpiScale), wa.Bottom - CatH + (int)(4 * dpiScale), Width, Height);
        }

        void PlaceInitial()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            bool valid = cfg.X != int.MinValue && cfg.Y != int.MinValue &&
                SystemInformation.VirtualScreen.Contains(new Point(cfg.X + Width / 2, cfg.Y + Height / 2));
            if (valid) SetBounds(cfg.X, cfg.Y, Width, Height);
            else PlaceDefault();
        }

        // ---- 行为 ----
        void Say(string text, double seconds)
        {
            bubble = text;
            bubbleUntil = DateTime.Now.AddSeconds(seconds);
        }

        void Pet()
        {
            state = PetState.Happy;
            stateUntil = DateTime.Now.AddSeconds(1.8);
            Say(cfg.Name.Length > 0 ? cfg.Name + "~" : "喵~", 1.6);
        }

        void ToggleFocus()
        {
            if (focusRunning) { focusRunning = false; Say("专心结束", 3); }
            else
            {
                focusRunning = true;
                focusEnd = DateTime.Now.AddMinutes(25);
                Say("25 分钟，我陪你", 3);
            }
        }

        void DoStretch(bool byUser)
        {
            state = PetState.Stretch;
            stateUntil = DateTime.Now.AddSeconds(2.4);
            Say("伸个懒腰", 2.4);
        }

        void StartHunt(TimeSpan dur)
        {
            if (state == PetState.Sleep) state = PetState.Idle;
            huntUntil = DateTime.Now.Add(dur);
            nextHuntHop = DateTime.MinValue;
        }

        void SafeTick()
        {
            try { Tick(); }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetTempPath(), "miaoban-err.txt"),
                        DateTime.Now.ToString("HH:mm:ss") + " " + ex + "\r\n");
                }
                catch { }
            }
        }

        // ---- 主循环 ----
        void Tick()
        {
            DateTime now = DateTime.Now;
            float t = (float)(now - t0).TotalSeconds;
            tick++;

            // 眨眼
            if (now >= nextBlink && now > blinkEnd)
            {
                blinkEnd = now.AddMilliseconds(130);
                nextBlink = now.AddSeconds(2.2 + Rnd.NextDouble() * 4.5);
            }
            bool blink = now < blinkEnd;

            // 状态回归
            if ((state == PetState.Happy || state == PetState.Stretch) && now > stateUntil) state = PetState.Idle;

            // 打字 / 活动（钩子拿不到按键时，用系统活动兜底）
            bool active = IdleMilliseconds() < 1200;
            if (active) lastActivity = now;
            bool keyTyping = (now - lastTyping).TotalMilliseconds < 700;
            bool activeTyping = (now - lastActivity).TotalMilliseconds < 900;
            bool typing = keyTyping || activeTyping;
            heat = Math.Max(0f, Math.Min(1f, heat + (keyTyping ? 0.014f : -0.010f)));
            if (typing && state != PetState.Happy && state != PetState.Sleep && state != PetState.Stretch && now > huntUntil)
                state = PetState.Knead;
            else if (state == PetState.Knead) state = PetState.Idle;

            // 空闲睡觉
            uint idleMs = IdleMilliseconds();
            if (state != PetState.Happy && state != PetState.Stretch && idleMs > 120000 && state != PetState.Sleep)
                state = PetState.Sleep;
            if (state == PetState.Sleep && idleMs < 2500) state = PetState.Idle;

            // 追鼠标
            Native.POINT cur; Native.GetCursorPos(out cur);
            float catCx = Left + Width / 2f, catCy = Top + Height - CatH / 2f;
            float dx = cur.X - catCx, dy = cur.Y - catCy;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (!dragging && state != PetState.Sleep && now < huntUntil && dist > 70 && !hopping && now >= nextHuntHop)
            {
                float stepX = Math.Sign(dx) * Math.Min(52f, Math.Abs(dx));
                float stepY = Math.Sign(dy) * Math.Min(44f, Math.Abs(dy));
                StartHopTo(Left + stepX, Top + stepY, 0.26f, 6f);
                nextHuntHop = now.AddMilliseconds(330);
            }

            bool still = Environment.GetEnvironmentVariable("MIAOBAN_STILL") == "1";

            // 随机小动作
            if (!still && state == PetState.Idle && !hopping && now > nextIdleAct)
            {
                nextIdleAct = now.AddSeconds(5 + Rnd.NextDouble() * 8);
                double r = Rnd.NextDouble();
                if (r < 0.35) nextBlink = now;
                else if (r < 0.55) { state = PetState.Stretch; stateUntil = now.AddSeconds(1.6); }
                else if (r < 0.7) StartHopTo(Left + (Rnd.NextDouble() < 0.5 ? -30 : 30), Top, 0.24f, 5f);
            }

            // 闲逛
            if (!still && state == PetState.Idle && !hopping && now > nextWander && now > huntUntil)
            {
                nextWander = now.AddSeconds(18 + Rnd.NextDouble() * 30);
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                float target = wa.Left + 40 + (float)Rnd.NextDouble() * Math.Max(1, wa.Width - Width - 80);
                float dir = Math.Sign(target - Left);
                StartHopTo(Left + dir * Math.Min(60, Math.Abs(target - Left)), Top, 0.3f, 6f);
            }

            UpdateHop(now);

            // 绘制姿态
            Pose p = BuildPose(now, t, blink, cur);
            Render(p);
        }
        void StartHopTo(float targetX, float targetY, float dur, float height)
        {
            Rectangle vs = SystemInformation.VirtualScreen;
            targetX = Math.Max(vs.Left - Width / 2f + 10, Math.Min(vs.Right - Width / 2f - 10, targetX));
            targetY = Math.Max(vs.Top - Height / 2f + 10, Math.Min(vs.Bottom - Height / 2f - 10, targetY));
            hopping = true;
            hopStart = DateTime.Now;
            hopDur = dur;
            hopFromX = Left; hopToX = targetX;
            hopFromY = Top; hopToY = targetY;
            hopHeight = height;
        }

        void UpdateHop(DateTime now)
        {
            if (!hopping) return;
            float u = (float)(now - hopStart).TotalSeconds / hopDur;
            if (u >= 1f)
            {
                hopping = false;
                SetBounds((int)Math.Round(hopToX), (int)Math.Round(hopToY), Width, Height);
                hopBob = 0f;
                return;
            }
            SetBounds((int)Math.Round(hopFromX + (hopToX - hopFromX) * u),
                      (int)Math.Round(hopFromY + (hopToY - hopFromY) * u), Width, Height);
            hopBob = -(float)Math.Sin(u * Math.PI) * hopHeight * P;
            hopLand = u > 0.82f ? (u - 0.82f) / 0.18f : 0f;
            hopTake = u < 0.18f ? 1f - u / 0.18f : 0f;
        }

        float hopBob, hopLand, hopTake;

        Pose BuildPose(DateTime now, float t, bool blink, Native.POINT cur)
        {
            Pose p = new Pose();
            p.T = t;
            p.State = state;
            p.Moving = hopping && state != PetState.Sleep && !dragging;
            p.FaceLeft = hopping && hopToX < hopFromX;
            p.Blink = blink;
            p.Sleep = state == PetState.Sleep;
            p.Happy = state == PetState.Happy;

            // 视线
            float ccx = Left + Width / 2f, ccy = Top + Height - CatH * 0.55f;
            p.LookX = Math.Max(-1f, Math.Min(1f, (cur.X - ccx) / 200f));
            p.LookY = Math.Max(-1f, Math.Min(1f, (cur.Y - ccy) / 200f));

            // 呼吸
            float breath = (float)Math.Sin(t * 2 * Math.PI * 0.5);
            p.Bob = breath * 0.35f + hopBob / P;

            // 跳跃挤压
            p.ScaleY = 1f - hopLand * 0.14f + hopTake * 0.1f;
            p.ScaleX = 1f + hopLand * 0.12f - hopTake * 0.08f;

            // 追猎
            if (now < huntUntil && state != PetState.Sleep && !dragging)
            {
                p.Crouch = 1f;
                p.State = PetState.Hunt;
            }

            // 踩奶
            if (state == PetState.Knead) p.Knead = (float)Math.Sin(t * 2 * Math.PI * 2.1);

            // 伸懒腰：整体放大 + 抬头
            if (state == PetState.Stretch)
            {
                float k = (float)Math.Sin((1f - (float)(stateUntil - now).TotalSeconds / 2.4f) * Math.PI);
                p.ScaleY = 1f + k * 0.12f;
                p.ScaleX = 1f + k * 0.06f;
                p.Bob -= k * 1.2f;
            }

            // 被拎起来：拉长、爪子悬空
            if (dragging)
            {
                p.Drag = 1f;
                p.ScaleY = 1.34f; p.ScaleX = 0.82f;
                p.Think = false;
            }
            else if (now < wobbleEnd)
            {
                float k = (float)(wobbleEnd - now).TotalSeconds / 0.5f;
                p.ScaleY = 1f + 0.1f * k * (float)Math.Sin(t * 26);
                p.ScaleX = 1f - 0.08f * k * (float)Math.Sin(t * 26);
            }

            // 过热
            p.Heat = heat > 0.55f ? (heat - 0.55f) / 0.45f : 0f;
            p.Steam = p.Heat > 0.2f ? t * 0.8f : 0f;

            // 睡觉：慢慢晃
            if (state == PetState.Sleep)
            {
                p.TailAngle = (float)Math.Sin(t * 1.2f) * 3f;
                p.ZzzPhase = (t * 0.5f) % 1.4f;
            }
            else
            {
                p.TailAngle = (float)Math.Sin(t * 2 * Math.PI * 0.4) * 10f;
            }

            // 爱心
            if (state == PetState.Happy) p.HeartPhase = (t * 1.1f) % 1f;
            else p.HeartPhase = -1f;

            // 刚启动打招呼
            if (now < helloUntil) { p.Happy = true; p.HeartPhase = (t * 1.2f) % 1f; }

            return p;
        }

        static Bitmap RenderCat(Pose p)
        {
            Bitmap bmp = new Bitmap(CatArt.W, CatArt.H, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                CatArt.Draw(g, p);
            }
            return bmp;
        }

        void Render(Pose p)
        {
            using (Graphics g = Graphics.FromImage(frame))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;

                using (Bitmap cat = RenderCat(p))
                {
                    int cx = (frame.Width - CatW) / 2;
                    int cy = frame.Height - CatH;
                    g.DrawImage(cat, new Rectangle(cx, cy, CatW, CatH));
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                DrawOverlay(g);
            }
            Apply();
        }

        void DrawOverlay(Graphics g)
        {
            float s = dpiScale;
            // 番茄钟
            if (focusRunning)
            {
                TimeSpan left = focusEnd - DateTime.Now;
                if (left.TotalSeconds > 0)
                    DrawTimer(g, left, (int)(6 * s));
            }
            // 气泡
            if (bubble != null && DateTime.Now < bubbleUntil)
                DrawBubble(g, bubble);
        }

        void DrawTimer(Graphics g, TimeSpan left, int y)
        {
            string txt = string.Format("{0:00}:{1:00}", (int)left.TotalMinutes, left.Seconds);
            using (Font f = new Font("Microsoft YaHei UI", 9f * dpiScale, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(txt, f);
                float w = sz.Width + 8 * dpiScale, h = sz.Height + 4 * dpiScale;
                float x = (frame.Width - w) / 2f;
                using (GraphicsPath path = Rounded(new RectangleF(x, y, w, h), 4 * dpiScale))
                {
                    g.FillPath(new SolidBrush(Color.FromArgb(235, 60, 48, 44)), path);
                }
                g.DrawString(txt, f, Brushes.White, x + 4 * dpiScale, y + 2 * dpiScale);
            }
        }

        void DrawBubble(Graphics g, string text)
        {
            using (Font f = new Font("Microsoft YaHei UI", 8.5f * dpiScale, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                SizeF sz = g.MeasureString(text, f);
                float w = sz.Width + 10 * dpiScale, h = sz.Height + 6 * dpiScale;
                float x = (frame.Width - w) / 2f, y = 2 * dpiScale;
                if (x < 1) x = 1;
                using (GraphicsPath path = Rounded(new RectangleF(x, y, w, h), 5 * dpiScale))
                {
                    g.FillPath(new SolidBrush(Color.FromArgb(248, 255, 250, 242)), path);
                    using (Pen pen = new Pen(Color.FromArgb(230, 226, 168, 142), 1.2f * dpiScale)) g.DrawPath(pen, path);
                }
                g.DrawString(text, f, new SolidBrush(Color.FromArgb(255, 92, 64, 52)), x + 5 * dpiScale, y + 3 * dpiScale);
            }
        }

        static GraphicsPath Rounded(RectangleF r, float rad)
        {
            GraphicsPath path = new GraphicsPath();
            float d = rad * 2;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ---- 分层窗口 ----
        IntPtr dibHandle = IntPtr.Zero, dibBits = IntPtr.Zero, dibDC = IntPtr.Zero;
        int dibW, dibH;

        void EnsureDib(int w, int h)
        {
            if (dibBits != IntPtr.Zero && dibW == w && dibH == h) return;
            ReleaseDib();
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            dibDC = Native.CreateCompatibleDC(screenDc);
            Native.BITMAPINFOHEADER bmi = new Native.BITMAPINFOHEADER();
            bmi.biSize = Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER));
            bmi.biWidth = w;
            bmi.biHeight = -h;
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = 0;
            dibHandle = Native.CreateDIBSection(dibDC, ref bmi, 0, out dibBits, IntPtr.Zero, 0);
            Native.ReleaseDC(IntPtr.Zero, screenDc);
            dibW = w; dibH = h;
        }

        void ReleaseDib()
        {
            if (dibHandle != IntPtr.Zero) { Native.DeleteObject(dibHandle); dibHandle = IntPtr.Zero; }
            if (dibDC != IntPtr.Zero) { Native.DeleteDC(dibDC); dibDC = IntPtr.Zero; }
            dibBits = IntPtr.Zero; dibW = dibH = 0;
        }

        void Apply()
        {
            if (frame == null || !IsHandleCreated) return;
            EnsureDib(frame.Width, frame.Height);
            if (dibBits == IntPtr.Zero) return;

            Rectangle rect = new Rectangle(0, 0, frame.Width, frame.Height);
            BitmapData bd = frame.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    byte* src = (byte*)bd.Scan0;
                    byte* dst = (byte*)dibBits;
                    for (int y = 0; y < frame.Height; y++)
                    {
                        byte* row = src + y * bd.Stride;
                        byte* outRow = dst + y * frame.Width * 4;
                        for (int x = 0; x < frame.Width; x++)
                        {
                            byte a = row[x * 4 + 3];
                            outRow[x * 4 + 0] = (byte)(row[x * 4 + 0] * a / 255);
                            outRow[x * 4 + 1] = (byte)(row[x * 4 + 1] * a / 255);
                            outRow[x * 4 + 2] = (byte)(row[x * 4 + 2] * a / 255);
                            outRow[x * 4 + 3] = a;
                        }
                    }
                }
            }
            finally { frame.UnlockBits(bd); }

            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr old = Native.SelectObject(dibDC, dibHandle);
            try
            {
                Native.SIZE size = new Native.SIZE(frame.Width, frame.Height);
                Native.POINT src = new Native.POINT(0, 0);
                Native.POINT dst = new Native.POINT(Left, Top);
                Native.BLENDFUNCTION blend = new Native.BLENDFUNCTION();
                blend.BlendOp = Native.AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = Native.AC_SRC_ALPHA;
                Native.UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, dibDC, ref src, 0, ref blend, Native.ULW_ALPHA);
            }
            finally
            {
                Native.SelectObject(dibDC, old);
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        static uint IdleMilliseconds()
        {
            Native.LASTINPUTINFO info = new Native.LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(Native.LASTINPUTINFO));
            if (!Native.GetLastInputInfo(ref info)) return 0;
            return (uint)Environment.TickCount - info.dwTime;
        }

        int pollTicks;
        IntPtr keyHook = IntPtr.Zero;
        Native.LowLevelKeyboardProc keyProc;

        void InstallHook()
        {
            keyProc = delegate(int nCode, IntPtr wParam, IntPtr lParam)
            {
                try
                {
                    if (nCode >= 0 && (wParam == (IntPtr)0x100 || wParam == (IntPtr)0x104))
                    {
                        lastTyping = DateTime.Now;
                        keysInWindow++;
                        if (state == PetState.Sleep) state = PetState.Idle;
                    }
                }
                catch { }
                return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            };
            keyHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, keyProc, Native.GetModuleHandle(null), 0);
        }
        void PollKeys()
        {
            pollTicks++;
            DateTime now = DateTime.Now;
            for (int vk = 8; vk < 255; vk++)
            {
                short st = Native.GetAsyncKeyState(vk);
                bool down = (st & 0x8000) != 0;
                bool tapped = (st & 0x0001) != 0;
                if ((tapped || (down && !keyDown[vk])) && vk != 0x01 && vk != 0x02 && vk != 0x04 && vk != 0x05 && vk != 0x06)
                {
                    lastTyping = now;
                    keysInWindow++;
                    if (state == PetState.Sleep) state = PetState.Idle;
                }
                keyDown[vk] = down;
            }
        }

        void EverySecond()
        {
            DateTime now = DateTime.Now;
            keyRate = keysInWindow;
            keysInWindow = 0;

            if (focusRunning && now >= focusEnd)
            {
                focusRunning = false;
                Say("时间到，起来动动", 6);
                state = PetState.Happy;
                stateUntil = now.AddSeconds(3);
                tray.ShowBalloonTip(4000, "喵伴", "25 分钟到了，站起来活动一下吧。", ToolTipIcon.Info);
                }

            if (cfg.Water)
            {
                if (nextWater == DateTime.MinValue) nextWater = now.AddMinutes(45);
                if (now >= nextWater) { nextWater = now.AddMinutes(45); Say("喝口水吧", 6); DoStretch(false); }
            }
            else nextWater = DateTime.MinValue;

            if (cfg.Stretch)
            {
                if (nextStretch == DateTime.MinValue) nextStretch = now.AddMinutes(45);
                if (now >= nextStretch) { nextStretch = now.AddMinutes(45); DoStretch(false); }
            }
            else nextStretch = DateTime.MinValue;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Layout();
            PlaceInitial();
            Apply();
            InstallHook();
            animTimer.Start();
            pollTimer.Start();
            secTimer.Start();
            helloUntil = DateTime.Now.AddSeconds(4);
            Say(cfg.Name.Length > 0 ? "你好，" + cfg.Name : "右键有菜单", 4);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ReleaseDib();
            base.OnFormClosed(e);
        }

        bool IsOpaquePoint(Point client)
        {
            if (frame == null) return false;
            if (client.X < 0 || client.Y < 0 || client.X >= frame.Width || client.Y >= frame.Height) return false;
            return frame.GetPixel(client.X, client.Y).A > 24;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)3; // MA_NOACTIVATE
                return;
            }
            if (m.Msg == WM_NCHITTEST)
            {
                int lp = m.LParam.ToInt32();
                int sx = (short)(lp & 0xFFFF);
                int sy = (short)((lp >> 16) & 0xFFFF);
                m.Result = (IntPtr)(IsOpaquePoint(PointToClient(new Point(sx, sy))) ? 1 : -1);
                return;
            }
            if (m.Msg == WM_LBUTTONDOWN)
            {
                dragging = true;
                dragMoved = false;
                dragOffset = PointToClient(Cursor.Position);
                hopping = false;
                return;
            }
            if (m.Msg == WM_MOUSEMOVE && dragging)
            {
                Point p = Cursor.Position;
                int nx = p.X - dragOffset.X, ny = p.Y - dragOffset.Y;
                if (Math.Abs(p.X - (Left + dragOffset.X)) > 3 || Math.Abs(p.Y - (Top + dragOffset.Y)) > 3) dragMoved = true;
                Rectangle vs = SystemInformation.VirtualScreen;
                nx = Math.Max(vs.Left - Width / 2, Math.Min(vs.Right - Width / 2, nx));
                ny = Math.Max(vs.Top - Height / 2, Math.Min(vs.Bottom - Height / 2, ny));
                SetBounds(nx, ny, Width, Height);
                Apply();
                return;
            }
            if (m.Msg == WM_LBUTTONUP)
            {
                if (dragging)
                {
                    dragging = false;
                    cfg.X = Left; cfg.Y = Top; cfg.Save();
                    if (!dragMoved)
                    {
                        Pet();
                    }
                    else
                    {
                        wobbleEnd = DateTime.Now.AddMilliseconds(520);
                        hopping = false;
                        hopBob = 0f;
                    }
                }
                return;
            }
            if (m.Msg == WM_RBUTTONUP)
            {
                ShowMenu();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e) { }
        protected override void OnPaintBackground(PaintEventArgs e) { }
    }

    class CuteMenuRenderer : ToolStripProfessionalRenderer
    {
        public CuteMenuRenderer() : base(new CuteColors()) { }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(255, 92, 62, 50);
            base.OnRenderItemText(e);
        }
    }

    class CuteColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected { get { return Color.FromArgb(255, 255, 232, 220); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(255, 246, 178, 108); } }
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(255, 255, 251, 246); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(255, 255, 251, 246); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(255, 255, 251, 246); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(255, 255, 251, 246); } }
        public override Color MenuBorder { get { return Color.FromArgb(255, 228, 200, 182); } }
        public override Color SeparatorDark { get { return Color.FromArgb(255, 236, 214, 200); } }
        public override Color SeparatorLight { get { return Color.FromArgb(255, 255, 251, 246); } }
    }

    static class Rnd
    {
        static System.Random r = new System.Random();
        public static double NextDouble() { lock (r) { return r.NextDouble(); } }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--info") { Info(args[1]); return; }
            if (args.Length >= 2 && args[0] == "--export-pack") { ExportPack(args[1]); return; }
            if (args.Length >= 3 && args[0] == "--preview-pack") { PreviewPack(args[1], args[2]); return; }
            if (args.Length >= 2 && args[0] == "--sprite") { SpriteSheet(args[1]); return; }
            if (args.Length >= 2 && args[0] == "--poses") { PoseFiles(args[1]); return; }
            if (args.Length >= 2 && args[0] == "--pose")
            {
                PoseSheet(args[1]);
                return;
            }
            Native.EnableDpi();
            bool created;
            using (Mutex mtx = new Mutex(true, "MiaoBan_SingleInstance_v1", out created))
            {
                if (!created) return;
                try
                {
                    string packPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "sprite.cat");
                    if (File.Exists(packPath)) CatArt.UsePack(SpritePack.Load(packPath).RenderAll());
                }
                catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PetForm(Config.Load()));
                GC.KeepAlive(mtx);
            }
        }

        static void Info(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("W=" + CatArt.W + " H=" + CatArt.H);
            sb.AppendLine("dpi=" + (Environment.GetEnvironmentVariable("MIAOBAN_DPI") ?? "?") );
            File.WriteAllText(path, sb.ToString());
        }

        static void ExportPack(string path)
        {
            Dictionary<string, string[]> frames = new Dictionary<string, string[]>();
            frames["sit"] = Sprite.DumpRows(Sprite.Frame.Sit);
            frames["blink"] = Sprite.DumpRows(Sprite.Frame.Blink);
            frames["happy"] = Sprite.DumpRows(Sprite.Frame.Happy);
            frames["kneadA"] = Sprite.DumpRows(Sprite.Frame.KneadA);
            frames["kneadB"] = Sprite.DumpRows(Sprite.Frame.KneadB);
            frames["walk1"] = Sprite.DumpRows(Sprite.Frame.Walk1);
            frames["walk2"] = Sprite.DumpRows(Sprite.Frame.Walk2);
            frames["walk3"] = Sprite.DumpRows(Sprite.Frame.Walk3);
            frames["walk4"] = Sprite.DumpRows(Sprite.Frame.Walk4);
            frames["sleep"] = Sprite.DumpRows(Sprite.Frame.Sleep);
            frames["drag"] = Sprite.DumpRows(Sprite.Frame.Drag);
            SpritePack.Export(path, frames, Sprite.Pal, "喵伴", Sprite.W, Sprite.H);
        }

        static void PreviewPack(string packPath, string outPng)
        {
            SpritePack pack = SpritePack.Load(packPath);
            Dictionary<string, Bitmap> all = pack.RenderAll();
            int z = 5, cols = 6;
            using (Bitmap sheet = new Bitmap(pack.W * z * cols, pack.H * z * 2, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(Color.FromArgb(255, 214, 226, 238));
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    int i = 0;
                    foreach (string k in all.Keys)
                    {
                        Bitmap b = all[k];
                        if (b == null) continue;
                        g.DrawImage(b, new Rectangle((i % cols) * pack.W * z, (i / cols) * pack.H * z, pack.W * z, pack.H * z));
                        i++;
                    }
                }
                sheet.Save(outPng, ImageFormat.Png);
            }
        }

        static void SpriteSheet(string path)
        {
            Sprite.Frame[] frames = { Sprite.Frame.Sit, Sprite.Frame.Blink, Sprite.Frame.Happy, Sprite.Frame.KneadA, Sprite.Frame.KneadB, Sprite.Frame.Walk1, Sprite.Frame.Walk2, Sprite.Frame.Walk3, Sprite.Frame.Walk4, Sprite.Frame.Sleep, Sprite.Frame.Drag };
            int z = 6, cols = 6;
            using (Bitmap sheet = new Bitmap(Sprite.W * z * cols, Sprite.H * z * 2, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(Color.FromArgb(255, 214, 226, 238));
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    for (int i = 0; i < frames.Length; i++)
                    {
                        using (Bitmap b = Sprite.Render(frames[i], 0f))
                            g.DrawImage(b, new Rectangle((i % cols) * Sprite.W * z, (i / cols) * Sprite.H * z, Sprite.W * z, Sprite.H * z));
                    }
                }
                sheet.Save(path, ImageFormat.Png);
            }
        }

        static void PoseFiles(string dir)
        {
            Directory.CreateDirectory(dir);
            string[] names = { "idle", "happy", "sleep", "knead", "hunt", "heat", "drag", "stretch" };
            for (int i = 0; i < names.Length; i++)
            {
                Pose p = new Pose();
                p.T = 0.5f;
                if (names[i] == "happy") { p.Happy = true; p.HeartPhase = 0.4f; }
                if (names[i] == "sleep") { p.Sleep = true; p.ZzzPhase = 0.45f; }
                if (names[i] == "knead") { p.Knead = 0.8f; }
                if (names[i] == "hunt") { p.Crouch = 1f; }
                if (names[i] == "heat") { p.Heat = 1f; p.Steam = 0.3f; }
                if (names[i] == "drag") { p.Drag = 1f; p.ScaleY = 1.2f; p.ScaleX = 0.86f; }
                if (names[i] == "stretch") { p.ScaleY = 1.1f; p.ScaleX = 1.05f; p.Happy = true; }
                using (Bitmap cat = new Bitmap(CatArt.W, CatArt.H, PixelFormat.Format32bppArgb))
                {
                    using (Graphics cg = Graphics.FromImage(cat)) { cg.Clear(Color.Transparent); CatArt.Draw(cg, p); }
                    using (Bitmap big = new Bitmap(CatArt.W * 8, CatArt.H * 8, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics bg = Graphics.FromImage(big))
                        {
                            bg.InterpolationMode = InterpolationMode.NearestNeighbor;
                            bg.PixelOffsetMode = PixelOffsetMode.Half;
                            bg.DrawImage(cat, new Rectangle(0, 0, big.Width, big.Height));
                        }
                        big.Save(Path.Combine(dir, names[i] + ".png"), ImageFormat.Png);
                    }
                }
            }
        }

        static void PoseSheet(string path)
        {
            string[] names = { "idle", "happy", "sleep", "knead", "hunt", "heat", "drag", "stretch" };
            int z = 8, cols = 4, rows = 2;
            int w = CatArt.W * z, h = CatArt.H * z;
            using (Bitmap sheet = new Bitmap(w * cols, h * rows, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(sheet))
                {
                    g.Clear(Color.FromArgb(255, 236, 240, 245));
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    for (int i = 0; i < names.Length; i++)
                    {
                        Pose p = new Pose();
                        p.T = 0.5f;
                        if (names[i] == "happy") { p.Happy = true; p.HeartPhase = 0.4f; }
                        if (names[i] == "sleep") { p.Sleep = true; p.ZzzPhase = 0.45f; p.Bob = 0.5f; }
                        if (names[i] == "knead") { p.Knead = 0.8f; }
                        if (names[i] == "hunt") { p.Crouch = 1f; }
                        if (names[i] == "heat") { p.Heat = 1f; p.Steam = 0.3f; }
                        if (names[i] == "drag") { p.Drag = 1f; p.ScaleY = 1.34f; p.ScaleX = 0.82f; }
                        if (names[i] == "stretch") { p.ScaleY = 1.12f; p.ScaleX = 1.06f; p.Happy = true; }
                        using (Bitmap cat = new Bitmap(CatArt.W, CatArt.H, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics cg = Graphics.FromImage(cat))
                            {
                                cg.Clear(Color.Transparent);
                                CatArt.Draw(cg, p);
                            }
                            int cx = (i % cols) * w, cy = (i / cols) * h;
                            g.DrawImage(cat, new Rectangle(cx, cy, w, h));
                        }
                    }
                }
                sheet.Save(path, ImageFormat.Png);
            }
        }
    }
}






























