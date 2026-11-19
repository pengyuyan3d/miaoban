// 喵伴 · 素材包（sprite.cat）读写：把猫的形象从代码里剥离，做成可替换的数据
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace MiaoBan
{
    class SpritePack
    {
        public string Name = "喵伴";
        public int W = 32, H = 36;
        public Dictionary<char, Color> Palette = new Dictionary<char, Color>();
        public Dictionary<string, string[]> Frames = new Dictionary<string, string[]>();

        static readonly Color Halo = Color.White;

        // ---------- 读 ----------
        public static SpritePack Load(string path)
        {
            SpritePack p = new SpritePack();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string current = null;
            List<string> rows = null;

            foreach (string raw in lines)
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("#") && !line.StartsWith("#!")) continue;
                if (line.StartsWith("frame."))
                {
                    if (current != null && rows != null) p.Frames[current] = rows.ToArray();
                    current = line.Substring(6).Trim();
                    rows = new List<string>();
                    continue;
                }
                if (rows != null && current != null)
                {
                    if (line.Length == 0) continue;
                    rows.Add(line);
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim();
                if (k == "name") p.Name = v;
                else if (k == "size")
                {
                    string[] wh = v.Split('x');
                    if (wh.Length == 2) { p.W = int.Parse(wh[0]); p.H = int.Parse(wh[1]); }
                }
                else if (k.StartsWith("color."))
                {
                    char key = k.Substring(6)[0];
                    p.Palette[key] = ParseHex(v);
                }
            }
            if (current != null && rows != null) p.Frames[current] = rows.ToArray();
            return p;
        }

        static Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
                return Color.FromArgb(255,
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
            if (hex.Length == 8)
                return Color.FromArgb(
                    Convert.ToInt32(hex.Substring(6, 2), 16),
                    Convert.ToInt32(hex.Substring(0, 2), 16),
                    Convert.ToInt32(hex.Substring(2, 2), 16),
                    Convert.ToInt32(hex.Substring(4, 2), 16));
            return Color.Magenta;
        }

        // ---------- 渲染（含白描边） ----------
        public Bitmap Render(string frameName)
        {
            if (!Frames.ContainsKey(frameName)) return null;
            string[] rows = Frames[frameName];

            char[,] px = new char[W, H];
            for (int y = 0; y < H; y++)
            {
                string r = y < rows.Length ? rows[y] : "";
                for (int x = 0; x < W; x++)
                    px[x, y] = x < r.Length ? r[x] : '.';
            }

            Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                // 白色描边：外扩一圈
                using (SolidBrush halo = new SolidBrush(Halo))
                {
                    for (int y = 0; y < H; y++)
                        for (int x = 0; x < W; x++)
                        {
                            if (px[x, y] != '.') continue;
                            bool near = false;
                            for (int dy = -1; dy <= 1 && !near; dy++)
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    int nx = x + dx, ny = y + dy;
                                    if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                                    if (px[nx, ny] != '.') { near = true; break; }
                                }
                            if (near) g.FillRectangle(halo, x, y, 1, 1);
                        }
                }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        char c = px[x, y];
                        if (c == '.') continue;
                        Color col;
                        if (!Palette.TryGetValue(c, out col)) col = Color.Magenta;
                        using (SolidBrush b = new SolidBrush(col)) g.FillRectangle(b, x, y, 1, 1);
                    }
            }
            return bmp;
        }

        public Dictionary<string, Bitmap> RenderAll()
        {
            Dictionary<string, Bitmap> all = new Dictionary<string, Bitmap>();
            foreach (string k in Frames.Keys) all[k] = Render(k);
            return all;
        }

        // ---------- 写（从内置帧导出模板） ----------
        public static void Export(string path, Dictionary<string, string[]> frames, Dictionary<char, Color> palette, string name, int w, int h)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 喵伴素材包 v1");
            sb.AppendLine("# 换猫只要改这个文件，不用改代码、不用重新编译");
            sb.AppendLine("# 每个 frame 下面 36 行、每行 32 个字符；'.' 透明");
            sb.AppendLine("name=" + name);
            sb.AppendLine("size=" + w + "x" + h);
            foreach (KeyValuePair<char, Color> kv in palette)
                sb.AppendLine("color." + kv.Key + "=" + kv.Value.R.ToString("X2") + kv.Value.G.ToString("X2") + kv.Value.B.ToString("X2"));
            foreach (KeyValuePair<string, string[]> kv in frames)
            {
                sb.AppendLine("frame." + kv.Key);
                foreach (string row in kv.Value) sb.AppendLine(row);
            }
            File.WriteAllText(path, sb.ToString().TrimEnd() + Environment.NewLine, new UTF8Encoding(false));
        }
    }
}

