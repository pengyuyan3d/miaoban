// 喵伴 · 手绘像素猫（32×36，逐行定义剪影 + 细节，自动白描边）
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace MiaoBan
{
    // 像素画布：char 表示颜色
    class Sprite
    {
        public const int W = 32, H = 36;

        // 调色板
        static readonly Color C_BODY = Color.FromArgb(255, 43, 40, 50);
        static readonly Color C_LIGHT = Color.FromArgb(255, 74, 70, 84);
        static readonly Color C_DARK = Color.FromArgb(255, 30, 28, 36);
        static readonly Color C_EYE = Color.FromArgb(255, 252, 250, 255);
        static readonly Color C_PUPIL = Color.FromArgb(255, 10, 9, 14);
        static readonly Color C_PINK = Color.FromArgb(255, 232, 140, 148);
        static readonly Color C_RED = Color.FromArgb(255, 214, 58, 54);
        static readonly Color C_YELL = Color.FromArgb(255, 255, 198, 70);
        static readonly Color C_WHITE = Color.FromArgb(255, 236, 232, 236);

        // 画布
        char[,] px = new char[W, H];

        public static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>
        {
            { '#', Color.FromArgb(255, 43, 40, 50) },
            { 'o', Color.FromArgb(255, 74, 70, 84) },
            { 'd', Color.FromArgb(255, 30, 28, 36) },
            { 'E', Color.FromArgb(255, 252, 250, 255) },
            { 'B', Color.FromArgb(255, 10, 9, 14) },
            { 'p', Color.FromArgb(255, 232, 140, 148) },
            { 'r', Color.FromArgb(255, 214, 58, 54) },
            { 'y', Color.FromArgb(255, 255, 198, 70) },
            { 'w', Color.FromArgb(255, 236, 232, 236) },
            { 'k', Color.FromArgb(255, 205, 205, 215) },
            { 'K', Color.FromArgb(255, 55, 55, 66) }
        };

        void Clear()
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    px[x, y] = '.';
        }

        void Fill(int x0, int y0, int x1, int y1, char c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && x < W && y >= 0 && y < H) px[x, y] = c;
        }

        void P(int x, int y, char c)
        {
            if (x >= 0 && x < W && y >= 0 && y < H) px[x, y] = c;
        }

        // 剪影：每行 [x0, x1]
        void Rows(int[][] spans, int yStart, char c)
        {
            for (int i = 0; i < spans.Length; i++)
            {
                if (spans[i] == null) continue;
                Fill(spans[i][0], yStart + i, spans[i][1], yStart + i, c);
            }
        }

        // ---------- 坐姿基础剪影 ----------
        static readonly int[][] HEAD = new int[][]
        {
            new int[]{10,10},              // y1  耳尖
            new int[]{9,10}, new int[]{21,22},
            new int[]{8,11}, new int[]{20,23},
            new int[]{7,12}, new int[]{19,24},
            new int[]{7,24},               // y5 头顶
            new int[]{6,25},
            new int[]{6,25}, new int[]{6,25}, new int[]{6,25}, new int[]{6,25}, new int[]{6,25},
            new int[]{6,25}, new int[]{6,25}, new int[]{6,25}, new int[]{6,25}, new int[]{6,25},
            new int[]{6,25},
            new int[]{7,24},               // y18 下巴
            new int[]{8,23},               // y19
        };

        public enum Frame { Sit, Blink, Happy, KneadA, KneadB, Walk1, Walk2, Walk3, Walk4, Sleep, Drag }

        static Sprite Make(Frame f, float heat)
        {
            Sprite s = new Sprite();
            if (f == Frame.Walk1 || f == Frame.Walk2 || f == Frame.Walk3 || f == Frame.Walk4)
                s.BuildWalk((int)f - (int)Frame.Walk1);
            else if (f == Frame.Sleep)
                s.BuildSleep();
            else if (f == Frame.Drag)
                s.BuildDrag();
            else
                s.Build(f, heat);
            return s;
        }

        public static Bitmap Render(Frame f, float heat) { return Make(f, heat).ToBitmap(); }

        public static string[] DumpRows(Frame f)
        {
            Sprite s = Make(f, 0f);
            string[] rows = new string[H];
            for (int y = 0; y < H; y++)
            {
                char[] line = new char[W];
                for (int x = 0; x < W; x++) line[x] = s.px[x, y];
                rows[y] = new string(line);
            }
            return rows;
        }

        // ---------- 侧面走路（朝右，向左时外部镜像） ----------
        void BuildWalk(int phase)
        {
            Clear();
            // 尾巴（左侧向后翘）
            Fill(3, 16, 5, 17, '#');
            Fill(4, 14, 6, 15, '#');
            Fill(6, 12, 8, 13, '#');
            Fill(7, 11, 8, 11, 'o');

            // 身体
            Fill(8, 18, 19, 27, '#');
            Fill(7, 20, 8, 26, '#');
            Fill(18, 19, 20, 26, '#');
            Fill(9, 19, 15, 20, 'o');   // 背上高光
            Fill(9, 25, 19, 26, 'd');   // 腹部暗部

            // 头（大头，右侧）
            Fill(17, 8, 30, 20, '#');
            Fill(18, 7, 29, 7, '#');
            Fill(23, 8, 29, 9, 'o');

            // 耳朵
            Fill(19, 5, 20, 6, '#');
            Fill(18, 6, 21, 7, '#');
            Fill(26, 4, 27, 5, '#');
            Fill(25, 6, 28, 6, '#');

            // 眼睛（侧脸一只）
            Fill(24, 12, 27, 15, 'E');
            Fill(25, 13, 26, 14, 'B');
            P(25, 13, 'E');

            // 鼻子与嘴
            Fill(30, 15, 30, 15, 'p');
            P(29, 17, 'w');

            // 四条腿：phase 决定前后摆动
            int[] back = { 0, 1, 2, 1 };
            int[] front = { 2, 1, 0, 1 };
            int b = back[phase], f2 = front[phase];
            Fill(9 + b, 27, 11 + b, 32, '#');
            Fill(15 - b, 27, 17 - b, 32, '#');
            Fill(18 + f2, 27, 20 + f2, 32, '#');
            Fill(22 - f2, 27, 24 - f2, 32, '#');

            // 项圈
            Fill(19, 20, 24, 21, 'r');
        }

        // ---------- 趴着睡 ----------
        void BuildSleep()
        {
            Clear();
            // 身体（横向趴着）
            Fill(6, 20, 24, 30, '#');
            Fill(7, 19, 22, 19, '#');
            Fill(8, 31, 22, 31, '#');
            Fill(10, 21, 20, 22, 'o');

            // 尾巴绕到前面
            Fill(5, 24, 7, 25, '#');
            Fill(4, 27, 6, 29, '#');
            Fill(7, 30, 16, 31, '#');

            // 头（右侧枕在前爪上）
            Fill(15, 14, 28, 26, '#');
            Fill(16, 13, 27, 13, '#');
            Fill(23, 14, 27, 15, 'o');

            // 耳朵
            Fill(23, 12, 24, 13, '#');
            Fill(22, 14, 25, 14, '#');

            // 闭眼
            Fill(20, 19, 23, 19, 'E');
            Fill(24, 19, 27, 19, 'E');

            // 鼻子
            Fill(27, 21, 27, 21, 'p');

            // 前爪
            Fill(16, 26, 19, 28, '#');
            Fill(15, 27, 15, 28, 'o');

            // 项圈
            Fill(18, 26, 22, 27, 'r');
        }

        // ---------- 被拎起来 ----------
        void BuildDrag()
        {
            Clear();
            // 拉长的身体
            Fill(9, 20, 22, 31, '#');
            Fill(10, 19, 21, 19, '#');
            Fill(10, 32, 21, 32, '#');
            Fill(12, 22, 19, 23, 'o');

            // 尾巴垂下
            Fill(22, 28, 24, 29, '#');
            Fill(24, 31, 25, 33, '#');

            // 头
            Fill(7, 4, 24, 20, '#');
            Fill(8, 3, 23, 3, '#');
            Fill(11, 5, 20, 5, 'o');

            // 耳朵
            Fill(9, 1, 11, 2, '#');
            Fill(12, 3, 14, 3, '#');
            Fill(20, 1, 22, 2, '#');
            Fill(18, 3, 20, 3, '#');

            // 大眼睛（被拎起来有点慌）
            Fill(10, 9, 14, 13, 'E');
            Fill(17, 9, 21, 13, 'E');
            Fill(11, 10, 13, 12, 'B');
            Fill(18, 10, 20, 12, 'B');
            P(11, 10, 'E'); P(18, 10, 'E');

            // 鼻子嘴
            Fill(15, 15, 16, 15, 'p');
            Fill(14, 17, 17, 17, 'w');

            // 悬空的四条腿
            Fill(9, 33, 11, 35, '#');
            Fill(13, 33, 15, 35, '#');
            Fill(17, 33, 19, 35, '#');
            Fill(21, 33, 23, 35, '#');

            // 项圈
            Fill(10, 19, 21, 20, 'r');
        }

        void Build(Frame f, float heat)
        {
            Clear();

            // ---- 耳朵 ----
            Fill(10, 1, 11, 1, '#');
            Fill(9, 2, 12, 2, '#');
            Fill(8, 3, 13, 3, '#');
            Fill(7, 4, 14, 4, '#');
            Fill(10, 2, 11, 2, 'p');

            Fill(20, 1, 21, 1, '#');
            Fill(19, 2, 22, 2, '#');
            Fill(18, 3, 23, 3, '#');
            Fill(17, 4, 24, 4, '#');
            Fill(20, 2, 21, 2, 'p');

            // ---- 头 ----
            Fill(7, 5, 24, 5, '#');
            Fill(6, 6, 25, 20, '#');
            Fill(7, 21, 24, 21, '#');

            // 头顶高光 + 左侧轮廓光
            Fill(12, 6, 19, 6, 'o');
            Fill(7, 8, 8, 9, 'o');

            // ---- 身体 ----
            Fill(9, 22, 22, 31, '#');
            Fill(10, 32, 21, 33, '#');
            Fill(9, 24, 9, 31, 'd');
            Fill(22, 24, 22, 31, 'd');


            // ---- 尾巴 ----
            Fill(21, 30, 24, 30, '#');
            Fill(24, 30, 25, 30, '#') ;
            Fill(24, 29, 25, 29, '#');
            Fill(25, 26, 26, 28, '#');
            Fill(26, 24, 27, 25, '#');

            // ---- 项圈 + 铃铛 ----
            Fill(12, 25, 19, 25, 'o');
            Fill(11, 22, 20, 23, 'r');
            Fill(15, 24, 16, 25, 'y');

            // ---- 爪子 ----
            bool knead = (f == Frame.KneadA || f == Frame.KneadB);
            int ly = (f == Frame.KneadA) ? 31 : 33;
            int ry = (f == Frame.KneadB) ? 31 : 33;
            Paws(11, ly, 17, ry);
            if (knead)
            {
                Fill(10, 34, 21, 35, 'K');
                Fill(12, 34, 13, 34, 'k');
                Fill(15, 34, 16, 34, 'k');
                Fill(18, 34, 19, 34, 'k');
            }

            Face(f);
        }
        void Paws(int lx, int ly, int rx, int ry)
        {
            Fill(lx, ly, lx + 3, ly + 1, '#');
            Fill(rx, ry, rx + 3, ry + 1, '#');
            Fill(lx, ly, lx + 3, ly, 'o');
            Fill(rx, ry, rx + 3, ry, 'o');
            P(lx + 1, ly + 1, 'd');
            Fill(lx + 4, ly, lx + 4, ly + 1, 'd');
            P(rx + 2, ly + 1, 'd');
        }

        void Face(Frame f)
        {
            // 眼睛：白框 + 黑瞳（5×6）
            int ex1 = 9, ex2 = 18, ey = 11;
            if (f == Frame.Blink)
            {
                Fill(ex1, ey + 3, ex1 + 4, ey + 3, 'E');
                Fill(ex2, ey + 3, ex2 + 4, ey + 3, 'E');
            }
            else if (f == Frame.Happy)
            {
                P(ex1, ey + 3, 'E'); P(ex1 + 1, ey + 2, 'E'); P(ex1 + 2, ey + 1, 'E'); P(ex1 + 3, ey + 2, 'E'); P(ex1 + 4, ey + 3, 'E');
                P(ex2, ey + 3, 'E'); P(ex2 + 1, ey + 2, 'E'); P(ex2 + 2, ey + 1, 'E'); P(ex2 + 3, ey + 2, 'E'); P(ex2 + 4, ey + 3, 'E');
            }
            else
            {
                Fill(ex1, ey, ex1 + 4, ey + 5, 'E');
                Fill(ex2, ey, ex2 + 4, ey + 5, 'E');
                Fill(ex1 + 1, ey + 1, ex1 + 3, ey + 4, 'B');
                Fill(ex2 + 1, ey + 1, ex2 + 3, ey + 4, 'B');
                // 高光
                P(ex1 + 1, ey + 1, 'E');
                P(ex2 + 1, ey + 1, 'E');
            }

            // 鼻子 + 嘴
            Fill(15, ey + 6, 16, ey + 6, 'p');
            P(14, ey + 8, 'w');
            P(17, ey + 8, 'w');
            Fill(15, ey + 9, 16, ey + 9, 'w');
        }

        // ---------- 输出：加白描边 ----------
        Bitmap ToBitmap()
        {
            Color[] pal = new Color[128];
            pal['#'] = C_BODY;
            pal['o'] = C_LIGHT;
            pal['d'] = C_DARK;
            pal['E'] = C_EYE;
            pal['B'] = C_PUPIL;
            pal['p'] = C_PINK;
            pal['r'] = C_RED;
            pal['y'] = C_YELL;
            pal['w'] = C_WHITE;
            pal['k'] = Color.FromArgb(255, 205, 205, 215);
            pal['K'] = Color.FromArgb(255, 55, 55, 66);

            Bitmap bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                using (SolidBrush halo = new SolidBrush(Color.White))
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
                        Color col = pal[c];
                        if (col.A == 0) col = C_BODY;
                        using (SolidBrush b = new SolidBrush(col)) g.FillRectangle(b, x, y, 1, 1);
                    }
            }
            return bmp;
        }
    }
}









