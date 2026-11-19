// 喵伴 · 姿态合成（手绘像素帧 + 动效）
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MiaoBan
{
    enum PetState { Idle, Knead, Sleep, Happy, Stretch, Hunt, Drag, Heat, Think }

    class Pose
    {
        public float T;
        public float Bob;
        public float ScaleX = 1f, ScaleY = 1f;
        public float TailAngle;
        public float LookX, LookY;
        public bool Blink, Happy, Sleep, Think;
        public float Crouch;
        public float Heat;
        public float Steam;
        public float Drag;
        public float Knead;
        public float HeartPhase = -1f;
        public float ZzzPhase = -1f;
        public PetState State;
        public bool Moving, FaceLeft;
    }

    static class CatArt
    {
        public const int W = Sprite.W, H = Sprite.H;

        static Dictionary<string, Bitmap> pack;
        public static void UsePack(Dictionary<string, Bitmap> p) { pack = p; }

        static Bitmap PackFrame(string name)
        {
            if (pack == null) return null;
            Bitmap b;
            return pack.TryGetValue(name, out b) ? b : null;
        }

        public static void Draw(Graphics g, Pose p)
        {
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            Sprite.Frame f;
            string name;
            if (p.Drag > 0.4f) { f = Sprite.Frame.Drag; name = "drag"; }
            else if (p.Sleep) { f = Sprite.Frame.Sleep; name = "sleep"; }
            else if (p.Moving) { f = (Sprite.Frame)((int)Sprite.Frame.Walk1 + ((int)(p.T * 8f) % 4)); name = "walk" + (1 + (int)(p.T * 8f) % 4); }
            else if (p.Happy) { f = Sprite.Frame.Happy; name = "happy"; }
            else if (p.Blink) { f = Sprite.Frame.Blink; name = "blink"; }
            else if (p.State == PetState.Knead) { f = ((int)(p.T * 6f) % 2 == 0) ? Sprite.Frame.KneadA : Sprite.Frame.KneadB; name = ((int)(p.T * 6f) % 2 == 0) ? "kneadA" : "kneadB"; }
            else { f = Sprite.Frame.Sit; name = "sit"; }

            Bitmap fromPack = PackFrame(name);
            using (Bitmap bmp = fromPack != null ? new Bitmap(fromPack) : Sprite.Render(f, 0f))
            {
                if (p.Heat > 0.02f) Tint(bmp, p.Heat);

                GraphicsState st = g.Save();
                g.TranslateTransform(16f, 34f + p.Bob);
                g.ScaleTransform((p.FaceLeft && p.Moving) ? -p.ScaleX : p.ScaleX, p.ScaleY * (1f - p.Crouch * 0.08f));
                g.TranslateTransform(-16f, -34f);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(bmp, 0, 0, W, H);
                g.Restore(st);
            }

            if (p.HeartPhase >= 0f) Hearts(g, p);
            if (p.ZzzPhase >= 0f) Zzz(g, p);
            if (p.Steam > 0f) Steam(g, p);
            if (p.Think) Think(g, p);
        }

        static void Tint(Bitmap bmp, float heat)
        {
            Color tint = Color.FromArgb(255, 226, 52, 44);
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    if (c.A == 0) continue;
                    if (c.R > 200 && c.G > 200 && c.B > 200) continue;
                    int r = (int)(c.R + (tint.R - c.R) * heat * 0.85f);
                    int gg = (int)(c.G * (1f - heat * 0.75f));
                    int b = (int)(c.B * (1f - heat * 0.75f));
                    bmp.SetPixel(x, y, Color.FromArgb(c.A, Math.Min(255, r), Math.Max(0, gg), Math.Max(0, b)));
                }
        }

        // ---------- 粒子 ----------
        static void Hearts(Graphics g, Pose p)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = ((p.HeartPhase * 0.9f + i * 0.33f) % 1f);
                int alpha = (int)(Math.Sin(a * Math.PI) * 240);
                if (alpha < 10) continue;
                int x = 23 + (int)(Math.Sin(a * 5f + i) * 3);
                int y = 6 - (int)(a * 5);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, 255, 84, 112)))
                {
                    g.FillRectangle(b, x, y, 2, 2);
                    g.FillRectangle(b, x + 1, y + 2, 1, 1);
                }
            }
        }

        static void Zzz(Graphics g, Pose p)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = Math.Max(0f, (p.ZzzPhase - i * 0.28f) / 1f);
                if (a <= 0f || a >= 1f) continue;
                int alpha = (int)(Math.Sin(a * Math.PI) * 235);
                int x = 25 + i * 2 + (int)(a * 2);
                int y = 6 - (int)(a * 4) - i;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                {
                    g.FillRectangle(b, x, y, 2, 1);
                    g.FillRectangle(b, x + 1, y + 1, 1, 1);
                    g.FillRectangle(b, x, y + 2, 2, 1);
                }
            }
        }

        static void Steam(Graphics g, Pose p)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = ((p.Steam + i * 0.33f) % 1f);
                int alpha = (int)((1f - a) * 220);
                int x = 9 + i * 5 + (int)(Math.Sin(a * 6f + i) * 1);
                int y = 1 - (int)(a * 4);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, 245, 245, 250)))
                {
                    g.FillRectangle(b, x, y, 2, 2);
                    g.FillRectangle(b, x, y - 1, 1, 1);
                }
            }
        }

        static void Think(Graphics g, Pose p)
        {
            int y = 2 + (int)(Math.Sin(p.T * 4f) * 1);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
            {
                g.FillRectangle(b, 27, y, 2, 1);
                g.FillRectangle(b, 28, y + 1, 1, 1);
                g.FillRectangle(b, 27, y + 2, 2, 1);
            }
        }
    }
}




