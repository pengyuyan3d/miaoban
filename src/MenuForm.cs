// 喵伴 · 自绘右键菜单（不依赖 ToolStripDropDown，避免分层窗口下的焦点问题）
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiaoBan
{
    class MenuItemDef
    {
        public string Text;
        public Action Act;
        public bool Sep;
        public bool Check;
    }

    class MenuForm : Form
    {
        List<MenuItemDef> items;
        float dpi;
        int itemH, padX, padY;
        int hover = -1;

        public MenuForm(List<MenuItemDef> items, float dpiScale)
        {
            this.items = items;
            dpi = dpiScale;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            itemH = (int)(30 * dpi);
            padX = (int)(14 * dpi);
            padY = (int)(8 * dpi);
            Size = Measure();
        }

        Size Measure()
        {
            using (Font f = MenuFont())
            using (Graphics g = CreateGraphics())
            {
                int w = 120;
                foreach (MenuItemDef it in items)
                {
                    if (it.Sep) continue;
                    string t = (it.Check ? "✓ " : "  ") + it.Text;
                    SizeF sz = g.MeasureString(t, f);
                    if (sz.Width + padX * 2 + 18 > w) w = (int)(sz.Width + padX * 2 + 18);
                }
                int h = padY * 2;
                foreach (MenuItemDef it in items) h += it.Sep ? (int)(9 * dpi) : itemH;
                return new Size(w, h);
            }
        }

        Font MenuFont() { return new Font("Microsoft YaHei UI", 9.5f * dpi, FontStyle.Regular, GraphicsUnit.Pixel); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Round(r, (int)(8 * dpi)))
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(252, 255, 251, 245))) g.FillPath(b, path);
                using (Pen pen = new Pen(Color.FromArgb(255, 226, 196, 176), 1.2f)) g.DrawPath(pen, path);
            }

            int y = padY;
            using (Font f = MenuFont())
            using (StringFormat sf = new StringFormat())
            {
                sf.LineAlignment = StringAlignment.Center;
                for (int i = 0; i < items.Count; i++)
                {
                    MenuItemDef it = items[i];
                    if (it.Sep)
                    {
                        using (Pen pen = new Pen(Color.FromArgb(255, 236, 214, 200), 1f))
                            g.DrawLine(pen, padX, y + (int)(4 * dpi), Width - padX, y + (int)(4 * dpi));
                        y += (int)(9 * dpi);
                        continue;
                    }
                    Rectangle ir = new Rectangle(padX / 2, y, Width - padX, itemH);
                    if (i == hover)
                    {
                        using (GraphicsPath hp = Round(ir, (int)(6 * dpi)))
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 255, 232, 220))) g.FillPath(b, hp);
                    }
                    string t = (it.Check ? "✓ " : "  ") + it.Text;
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 92, 62, 50)))
                        g.DrawString(t, f, b, ir, sf);
                    y += itemH;
                }
            }
        }

        static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        int HitTest(Point p)
        {
            int y = padY;
            for (int i = 0; i < items.Count; i++)
            {
                int h = items[i].Sep ? (int)(9 * dpi) : itemH;
                if (!items[i].Sep && p.Y >= y && p.Y < y + h) return i;
                y += h;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int i = HitTest(e.Location);
            MenuItemDef def = (i >= 0) ? items[i] : null;
            Hide();
            if (def != null && def.Act != null)
            {
                try { def.Act(); } catch { }
            }
            Close();
        }

        protected override void OnDeactivate(EventArgs e) { Close(); }

        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); }

        public void ShowAt(Point p)
        {
            Rectangle wa = Screen.FromPoint(p).WorkingArea;
            int x = p.X, y = p.Y;
            if (x + Width > wa.Right) x = wa.Right - Width - 4;
            if (y + Height > wa.Bottom) y = wa.Bottom - Height - 4;
            if (x < wa.Left) x = wa.Left;
            if (y < wa.Top) y = wa.Top;
            SetBounds(x, y, Width, Height);
            Show();
            BringToFront();
            Activate();
        }

        protected override bool ShowWithoutActivation { get { return false; } }
    }
}

