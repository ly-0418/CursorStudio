using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CursorStudioApp
{
    internal static class Theme
    {
        internal static readonly Color Bg = Color.FromArgb(242, 244, 248);
        internal static readonly Color Card = Color.White;
        internal static readonly Color Border = Color.FromArgb(226, 231, 240);
        internal static readonly Color BorderFocus = Color.FromArgb(176, 199, 240);
        internal static readonly Color Primary = Color.FromArgb(45, 108, 223);
        internal static readonly Color PrimaryHover = Color.FromArgb(62, 123, 238);
        internal static readonly Color PrimaryDown = Color.FromArgb(36, 91, 192);
        internal static readonly Color PrimarySoft = Color.FromArgb(234, 241, 255);
        internal static readonly Color TextMain = Color.FromArgb(30, 41, 59);
        internal static readonly Color TextSub = Color.FromArgb(120, 129, 143);
        internal static readonly Color Track = Color.FromArgb(222, 228, 238);

        internal static float K = 1f;

        internal static int Px(int v)
        {
            return (int)Math.Round(v * K);
        }

        internal static Font F(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size * K, style);
        }

        internal static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    internal static class AppCursors
    {
        private static Cursor hand;
        private static int cachedPx = -1;

        internal static void Reset()
        {
            cachedPx = -1;
            hand = null;
        }

        internal static Cursor Hand()
        {
            if (hand != null) return hand;
            return HandForPx(32 + (CursorWorks.CurrentSize() - 1) * 8);
        }

        internal static Cursor HandForPx(int px)
        {
            if (px < 24) px = 24;
            if (px > 128) px = 128;
            if (hand != null && cachedPx == px) return hand;
            cachedPx = px;
            hand = null;
            try
            {
                string p = CursorWorks.GetRolePath("Hand");
                if (string.IsNullOrEmpty(p) || !File.Exists(p)) return null;
                IntPtr h = CursorWorks.BuildHdCursor(p, px);
                if (h != IntPtr.Zero) hand = new Cursor(h);
            }
            catch { }
            return hand;
        }
    }

    internal static class Shell
    {
        [DllImport("user32.dll")]
        internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("user32.dll")]
        internal static extern int ReleaseCapture();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        internal const int WM_NCLBUTTONDOWN = 0x00A1;
        internal const int HTCAPTION = 2;
        internal const int DI_NORMAL = 3;
    }

    internal class ModernButton : Control
    {
        internal bool Primary;
        private bool hover;
        private bool down;
        private int radius = 8;

        internal ModernButton(string text, bool primary)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Primary = primary;
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
            Font = Theme.F(9f, FontStyle.Regular);
            Height = 34;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill;
            Color fg;
            if (Primary)
            {
                fill = down ? Theme.PrimaryDown : (hover ? Theme.PrimaryHover : Theme.Primary);
                fg = Color.White;
            }
            else
            {
                fill = down ? Color.FromArgb(224, 232, 246) : (hover ? Color.FromArgb(240, 245, 253) : Color.White);
                fg = Theme.TextMain;
            }
            using (GraphicsPath p = Theme.Round(r, radius))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                if (!Primary)
                {
                    using (Pen pen = new Pen(hover ? Theme.BorderFocus : Theme.Border, 1f)) g.DrawPath(pen, p);
                }
            }
            TextRenderer.DrawText(g, Text, Font, r, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal class ModernComboBox : Control
    {
        private List<string> items = new List<string>();
        private int selected = -1;
        private bool hover;
        internal Action<int> OnChanged;

        internal ModernComboBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
            Font = Theme.F(9f, FontStyle.Regular);
            Height = 38;
        }

        internal void SetItems(List<string> list, int index)
        {
            items = list;
            selected = index;
            Invalidate();
        }

        internal int SelectedIndex
        {
            get { return selected; }
            set { if (value >= -1 && value < items.Count) { selected = value; Invalidate(); } }
        }

        internal string SelectedText
        {
            get { return (selected >= 0 && selected < items.Count) ? items[selected] : ""; }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) ShowDrop();
            base.OnMouseDown(e);
        }

        private void ShowDrop()
        {
            if (items.Count == 0) return;
            DropDownList dlg = new DropDownList(items, selected, Width, Font);
            Point p = PointToScreen(new Point(0, Height + 2));
            dlg.Location = p;
            dlg.OnPick = delegate(int idx)
            {
                selected = idx;
                Invalidate();
                if (OnChanged != null) OnChanged(idx);
            };
            dlg.Show();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 8))
            {
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, p);
                using (Pen pen = new Pen(hover ? Theme.BorderFocus : Theme.Border, 1f)) g.DrawPath(pen, p);
            }
            Rectangle textRect = new Rectangle(12, 0, Width - 44, Height);
            TextRenderer.DrawText(g, SelectedText, Font, textRect, Theme.TextMain,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            Point mid = new Point(Width - 22, Height / 2);
            using (Pen pen = new Pen(Theme.TextSub, 1.6f))
            {
                g.DrawLine(pen, mid.X - 4, mid.Y - 2, mid.X, mid.Y + 2);
                g.DrawLine(pen, mid.X, mid.Y + 2, mid.X + 4, mid.Y - 2);
            }
        }
    }

    internal class SmoothList : Control
    {
        internal List<string> Items = new List<string>();
        internal Action<int> OnPick;
        private int sel = -1;
        private float pos;
        private float target;
        private Timer anim;
        private int hoverIdx = -1;
        private bool dragging;
        private int dragStartY;
        private float dragStartPos;
        private int itemH;

        internal SmoothList(Font font)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Font = font;
            itemH = Theme.Px(34);
            BackColor = Color.White;
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
            anim = new Timer();
            anim.Interval = 15;
            anim.Tick += new EventHandler(AnimTick);
        }

        internal int SelectedIndex
        {
            get { return sel; }
            set
            {
                sel = value;
                EnsureVisible(sel);
                Invalidate();
            }
        }

        internal int PreferredHeight()
        {
            int h = Items.Count * itemH + Theme.Px(4);
            int max = Theme.Px(360);
            return h > max ? max : h;
        }

        private int ContentHeight { get { return Items.Count * itemH; } }

        private float MaxScroll
        {
            get
            {
                float m = ContentHeight - Height + Theme.Px(4);
                return m > 0 ? m : 0;
            }
        }

        private void Clamp()
        {
            if (target < 0) target = 0;
            if (target > MaxScroll) target = MaxScroll;
        }

        private void EnsureVisible(int idx)
        {
            if (idx < 0) return;
            float top = idx * itemH;
            float bottom = top + itemH;
            float view = Height - Theme.Px(4);
            if (top < target) target = top;
            else if (bottom > target + view) target = bottom - view;
            Clamp();
            anim.Start();
        }

        private void AnimTick(object sender, EventArgs e)
        {
            float d = target - pos;
            if (Math.Abs(d) < 0.7f) { pos = target; anim.Stop(); }
            else pos += d * 0.32f;
            Invalidate();
        }

        private void ScrollBy(float dy)
        {
            target += dy;
            Clamp();
            anim.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Focus();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollBy(-e.Delta / 120f * itemH * 2);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                float span = Height - Theme.Px(4) - ThumbHeight();
                if (span > 0)
                {
                    pos = dragStartPos + (e.Y - dragStartY) * (MaxScroll / span);
                    if (pos < 0) pos = 0;
                    if (pos > MaxScroll) pos = MaxScroll;
                    target = pos;
                    Invalidate();
                }
                return;
            }
            int idx = IndexAt(e.Y);
            if (idx != hoverIdx) { hoverIdx = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hoverIdx != -1) { hoverIdx = -1; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (ContentHeight > Height && e.X >= Width - Theme.Px(14))
                {
                    dragging = true;
                    dragStartY = e.Y;
                    dragStartPos = pos;
                }
                else
                {
                    int idx = IndexAt(e.Y);
                    if (idx >= 0 && idx < Items.Count)
                    {
                        sel = idx;
                        Invalidate();
                        if (OnPick != null) OnPick(idx);
                    }
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (sel < Items.Count - 1) { sel++; EnsureVisible(sel); Invalidate(); }
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (sel > 0) { sel--; EnsureVisible(sel); Invalidate(); }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (sel >= 0 && OnPick != null) OnPick(sel);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                Form f = FindForm();
                if (f != null) f.Close();
            }
            base.OnKeyDown(e);
        }

        private int IndexAt(int y)
        {
            int idx = (int)Math.Floor((y + pos) / itemH);
            if (idx < 0 || idx >= Items.Count) return -1;
            return idx;
        }

        private float ThumbHeight()
        {
            float trackH = Height - Theme.Px(4);
            float h = trackH * Height / ContentHeight;
            float min = Theme.Px(30);
            return h < min ? min : h;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(Color.White)) g.FillRectangle(b, ClientRectangle);

            int first = (int)Math.Floor(pos / itemH);
            int last = (int)Math.Ceiling((pos + Height) / itemH);
            for (int i = first; i <= last; i++)
            {
                if (i < 0 || i >= Items.Count) continue;
                int y = (int)(i * itemH - pos);
                Rectangle row = new Rectangle(Theme.Px(4), y, Width - Theme.Px(14), itemH);
                Rectangle clip = Rectangle.Intersect(new Rectangle(0, y, Width, itemH), ClientRectangle);
                if (clip.Height <= 0) continue;
                g.SetClip(clip);
                if (i == sel || i == hoverIdx)
                {
                    using (GraphicsPath p = Theme.Round(row, Theme.Px(6)))
                    {
                        using (SolidBrush b = new SolidBrush(i == sel ? Theme.PrimarySoft : Color.FromArgb(246, 249, 253)))
                            g.FillPath(b, p);
                    }
                }
                Color fg = i == sel ? Theme.Primary : Theme.TextMain;
                Rectangle tr = new Rectangle(row.X + Theme.Px(10), y, row.Width - Theme.Px(12), itemH);
                TextRenderer.DrawText(g, Items[i], Font, tr, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                g.ResetClip();
            }

            if (ContentHeight > Height)
            {
                float trackTop = Theme.Px(2);
                float trackH = Height - Theme.Px(4);
                float th = ThumbHeight();
                float ty = trackTop + (trackH - th) * (MaxScroll > 0 ? pos / MaxScroll : 0);
                Rectangle bar = new Rectangle(Width - Theme.Px(9), (int)ty, Theme.Px(5), (int)th);
                using (GraphicsPath p = Theme.Round(bar, Theme.Px(3)))
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(198, 208, 222))) g.FillPath(b, p);
                }
            }
        }
    }

    internal class DropDownList : Form
    {
        private SmoothList lb;
        internal Action<int> OnPick;

        internal DropDownList(List<string> items, int index, int width, Font font)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            DoubleBuffered = true;
            TopMost = true;

            lb = new SmoothList(font);
            foreach (string s in items) lb.Items.Add(s);
            lb.OnPick = delegate(int idx)
            {
                if (OnPick != null) OnPick(idx);
                Close();
            };
            Controls.Add(lb);

            int h = lb.PreferredHeight();
            ClientSize = new Size(width, h);
            lb.SetBounds(Theme.Px(2), Theme.Px(2), width - Theme.Px(4), h - Theme.Px(4));
            if (index >= 0) lb.SelectedIndex = index;
            Deactivate += delegate { Close(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            lb.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, Theme.Px(8)))
            {
                using (Pen pen = new Pen(Theme.BorderFocus, 1f)) e.Graphics.DrawPath(pen, p);
            }
        }
    }

    internal class ModernSlider : Control
    {
        private int min = 1;
        private int max = 15;
        private int value = 1;
        private bool dragging;
        internal Action<int> OnChanged;

        internal ModernSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.F(9f, FontStyle.Regular);
            Height = 36;
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
        }

        internal int Minimum { get { return min; } set { min = value; Invalidate(); } }
        internal int Maximum { get { return max; } set { max = value; Invalidate(); } }

        internal int Value
        {
            get { return value; }
            set
            {
                int v = value;
                if (v < min) v = min;
                if (v > max) v = max;
                this.value = v;
                Invalidate();
            }
        }

        private int Pad() { return 12; }

        private int ValueFromX(int x)
        {
            int left = Pad();
            int right = Width - Pad();
            if (right <= left) return min;
            double ratio = (double)(x - left) / (double)(right - left);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            return min + (int)Math.Round(ratio * (max - min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                SetByX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging) SetByX(e.X);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        private void SetByX(int x)
        {
            int v = ValueFromX(x);
            if (v != value)
            {
                value = v;
                Invalidate();
                if (OnChanged != null) OnChanged(value);
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left && value > min)
            {
                value--;
                Invalidate();
                if (OnChanged != null) OnChanged(value);
            }
            else if (e.KeyCode == Keys.Right && value < max)
            {
                value++;
                Invalidate();
                if (OnChanged != null) OnChanged(value);
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int left = Pad();
            int right = Width - Pad();
            int cy = Height / 2;
            Rectangle track = new Rectangle(left, cy - 3, right - left, 6);
            using (GraphicsPath p = Theme.Round(track, 3))
            {
                using (SolidBrush b = new SolidBrush(Theme.Track)) g.FillPath(b, p);
            }
            int knobX = left + (int)Math.Round(((double)(value - min) / (double)(max - min)) * (right - left));
            if (knobX > left)
            {
                Rectangle fill = new Rectangle(left, cy - 3, knobX - left, 6);
                using (GraphicsPath p = Theme.Round(fill, 3))
                {
                    using (SolidBrush b = new SolidBrush(Theme.Primary)) g.FillPath(b, p);
                }
            }
            Rectangle knob = new Rectangle(knobX - 8, cy - 8, 16, 16);
            using (GraphicsPath p = Theme.Round(knob, 8))
            {
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, p);
                using (Pen pen = new Pen(Theme.Primary, 3f)) g.DrawPath(pen, p);
            }
        }
    }

    internal class CardPanel : Panel
    {
        internal CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Font = Theme.F(9f, FontStyle.Regular);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, 12))
            {
                using (SolidBrush b = new SolidBrush(Theme.Card)) g.FillPath(b, p);
                using (Pen pen = new Pen(Theme.Border, 1f)) g.DrawPath(pen, p);
            }
            base.OnPaint(e);
        }
    }

    internal class CursorTile : Control
    {
        private string path;
        private Bitmap img;
        private bool hover;

        internal CursorTile(string title, string cursorPath)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = title;
            path = cursorPath;
            Size = new Size(Theme.Px(108), Theme.Px(86));
            Font = Theme.F(8.5f, FontStyle.Regular);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                img = CursorWorks.LoadCursorBitmap(path, Theme.Px(96));
            }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void Dispose(bool disposing)
        {
            if (img != null)
            {
                try { img.Dispose(); } catch { }
                img = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, Theme.Px(10)))
            {
                using (SolidBrush b = new SolidBrush(hover ? Color.FromArgb(248, 251, 255) : Color.White)) g.FillPath(b, p);
                using (Pen pen = new Pen(hover ? Theme.BorderFocus : Theme.Border, 1f)) g.DrawPath(pen, p);
            }
            if (img != null)
            {
                int icon = Theme.Px(46);
                int ix = (Width - icon) / 2;
                int iy = Theme.Px(7);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(img, new Rectangle(ix, iy, icon, icon), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
            }
            else
            {
                int ph2 = Theme.Px(32);
                Rectangle ph = new Rectangle((Width - ph2) / 2, Theme.Px(12), ph2, ph2);
                using (Pen pen = new Pen(Color.FromArgb(210, 216, 226), 1.4f)) g.DrawRectangle(pen, ph);
                using (Pen pen = new Pen(Color.FromArgb(210, 216, 226), 1.2f))
                {
                    g.DrawLine(pen, ph.Left, ph.Top, ph.Right, ph.Bottom);
                    g.DrawLine(pen, ph.Right, ph.Top, ph.Left, ph.Bottom);
                }
            }
            Rectangle tr = new Rectangle(Theme.Px(4), Theme.Px(54), Width - Theme.Px(8), Theme.Px(28));
            TextRenderer.DrawText(g, Text, Font, tr, hover ? Theme.Primary : Theme.TextSub,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }
    }

    internal class RoundIconButton : Control
    {
        private bool hover;
        internal string Glyph;
        internal Action OnClickAction;
        private bool isClose;

        internal RoundIconButton(string glyph, bool closeStyle)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Glyph = glyph;
            isClose = closeStyle;
            Size = new Size(32, 32);
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
            Font = Theme.F(9f, FontStyle.Regular);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && OnClickAction != null) OnClickAction();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (hover)
            {
                Color c = isClose ? Color.FromArgb(240, 90, 90) : Color.FromArgb(232, 237, 246);
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath p = Theme.Round(r, 8))
                {
                    using (SolidBrush b = new SolidBrush(c)) g.FillPath(b, p);
                }
            }
            Color fg = (hover && isClose) ? Color.White : Theme.TextSub;
            TextRenderer.DrawText(g, Glyph, Font, new Rectangle(0, 0, Width, Height), fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();
        private List<Scheme> schemes = new List<Scheme>();
        private ModernComboBox cmb;
        private ModernButton btnApply;
        private ModernButton btnRefresh;
        private ModernButton btnImport;
        private ModernButton btnRestore;
        private ModernButton btnReload;
        private ModernSlider slider;
        private ModernButton btnApplySize;
        private Label lblSizeValue;
        private Label lblActual;
        private FlowLayoutPanel flow;
        private Label lblStatus;
        private static Bitmap appIconCache;

        internal static Bitmap AppIconBitmap()
        {
            if (appIconCache != null) return appIconCache;
            try
            {
                string p = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "CursorStudio.ico");
                if (!File.Exists(p)) return null;
                using (Icon ico = new Icon(p, 128, 128))
                {
                    Bitmap src = ico.ToBitmap();
                    Bitmap dst = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(dst))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.DrawImage(src, 0, 0, 128, 128);
                    }
                    src.Dispose();
                    appIconCache = dst;
                }
            }
            catch { }
            return appIconCache;
        }

        internal MainForm()
        {
            Text = "光标工作室";
            try
            {
                string icoPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "CursorStudio.ico");
                if (File.Exists(icoPath)) Icon = new Icon(icoPath);
            }
            catch { }
            try
            {
                uint dpiSys = GetDpiForSystem();
                float s = dpiSys / 96f;
                if (s < 1f) s = 1f;
                Theme.K = s;
            }
            catch { Theme.K = 1f; }
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(860, 944);
            BackColor = Theme.Bg;
            Font = Theme.F(9f, FontStyle.Regular);
            DoubleBuffered = true;
            BuildHeader();
            BuildSchemeCard();
            BuildSizeCard();
            BuildPreviewCard();
            BuildStatus();
            float k = Theme.K;
            if (k > 1f)
            {
                ClientSize = new Size((int)Math.Round(860 * k), (int)Math.Round(944 * k));
                ScaleTree(this, k);
                try
                {
                    File.WriteAllText(Path.Combine(Path.GetTempPath(), "cs_scale.log"),
                        "k=" + k + " client=" + ClientSize.Width + "x" + ClientSize.Height + " children=" + Controls.Count);
                }
                catch { }
            }
            LoadAll();
        }

        private static void ScaleTree(Control parent, float k)
        {
            foreach (Control c in parent.Controls)
            {
                c.Location = new Point((int)Math.Round(c.Left * k), (int)Math.Round(c.Top * k));
                c.Size = new Size((int)Math.Round(c.Width * k), (int)Math.Round(c.Height * k));
                c.Padding = new Padding((int)Math.Round(c.Padding.Left * k), (int)Math.Round(c.Padding.Top * k),
                    (int)Math.Round(c.Padding.Right * k), (int)Math.Round(c.Padding.Bottom * k));
                c.Margin = new Padding((int)Math.Round(c.Margin.Left * k), (int)Math.Round(c.Margin.Top * k),
                    (int)Math.Round(c.Margin.Right * k), (int)Math.Round(c.Margin.Bottom * k));
                ScaleTree(c, k);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = 2;
                Shell.DwmSetWindowAttribute(Handle, 33, ref pref, 4);
            }
            catch { }
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Shell.ReleaseCapture();
                Shell.SendMessage(Handle, Shell.WM_NCLBUTTONDOWN, (IntPtr)Shell.HTCAPTION, IntPtr.Zero);
            }
        }

        private void BuildHeader()
        {
            Panel head = new Panel();
            head.SetBounds(0, 0, ClientSize.Width, 68);
            head.BackColor = Theme.Bg;
            head.MouseDown += DragWindow;
            Controls.Add(head);

            Panel logo = new Panel();
            logo.SetBounds(24, 16, 40, 40);
            logo.BackColor = Theme.Bg;
            logo.MouseDown += DragWindow;
            head.Controls.Add(logo);
            logo.Paint += delegate(object s, PaintEventArgs pe)
            {
                Graphics g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                Bitmap app = MainForm.AppIconBitmap();
                if (app != null)
                {
                    g.DrawImage(app, new Rectangle(0, 0, 40, 40));
                    return;
                }
                Rectangle r = new Rectangle(0, 0, 39, 39);
                using (GraphicsPath p = Theme.Round(r, 11))
                {
                    using (SolidBrush b = new SolidBrush(Theme.PrimarySoft)) g.FillPath(b, p);
                }
                IntPtr h = Native.LoadCursorFromFile(Path.Combine(Environment.SystemDirectory, "..\\Cursors\\aero_arrow.cur"));
                IntPtr hdc = g.GetHdc();
                try { Shell.DrawIconEx(hdc, 8, 8, h, 24, 24, 0, IntPtr.Zero, Shell.DI_NORMAL); }
                finally { g.ReleaseHdc(hdc); }
                if (h != IntPtr.Zero) Native.DestroyCursor(h);
            };

            Label title = new Label();
            title.Text = "光标工作室";
            title.Font = Theme.F(13f, FontStyle.Bold);
            title.ForeColor = Theme.TextMain;
            title.BackColor = Theme.Bg;
            title.SetBounds(76, 16, 300, 26);
            title.MouseDown += DragWindow;
            head.Controls.Add(title);

            Label sub = new Label();
            sub.Text = "切换第三方指针方案 · 调整显示大小 · 实时预览";
            sub.Font = Theme.F(8.5f, FontStyle.Regular);
            sub.ForeColor = Theme.TextSub;
            sub.BackColor = Theme.Bg;
            sub.SetBounds(76, 42, 420, 20);
            sub.MouseDown += DragWindow;
            head.Controls.Add(sub);

            RoundIconButton min = new RoundIconButton("—", false);
            min.SetBounds(ClientSize.Width - 80, 18, 32, 32);
            min.OnClickAction = delegate { WindowState = FormWindowState.Minimized; };
            head.Controls.Add(min);

            RoundIconButton close = new RoundIconButton("✕", true);
            close.SetBounds(ClientSize.Width - 44, 18, 32, 32);
            close.OnClickAction = delegate { Close(); };
            head.Controls.Add(close);
        }

        private Label CardTitle(CardPanel card, string text)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F(10f, FontStyle.Bold);
            l.ForeColor = Theme.TextMain;
            l.BackColor = Theme.Card;
            l.SetBounds(18, 14, 240, 22);
            card.Controls.Add(l);
            return l;
        }

        private void BuildSchemeCard()
        {
            CardPanel card = new CardPanel();
            card.SetBounds(24, 84, ClientSize.Width - 48, 146);
            Controls.Add(card);
            CardTitle(card, "指针方案");

            // 路径不再显示在界面上（已有「打开自定义文件夹」按钮）

            cmb = new ModernComboBox();
            cmb.SetBounds(18, 46, 396, 38);
            cmb.OnChanged = delegate(int idx)
            {
                if (idx < 0 || idx >= schemes.Count) return;
                RefreshTiles(schemes[idx], false);
                SetStatus("正在预览「" + schemes[idx].Name + "」。点「应用此方案」才会真正替换系统光标。");
            };
            card.Controls.Add(cmb);

            btnApply = new ModernButton("应用此方案", true);
            btnApply.SetBounds(424, 46, 124, 38);
            btnApply.Click += delegate { ApplyCurrentScheme(); };
            card.Controls.Add(btnApply);

            btnRefresh = new ModernButton("刷新列表", false);
            btnRefresh.SetBounds(556, 46, 96, 38);
            btnRefresh.Click += delegate { LoadAll(); };
            card.Controls.Add(btnRefresh);

            btnImport = new ModernButton("导入光标包", false);
            btnImport.SetBounds(18, 98, 124, 32);
            btnImport.Click += delegate { Import(); };
            card.Controls.Add(btnImport);

            btnRestore = new ModernButton("恢复系统默认", false);
            btnRestore.SetBounds(150, 98, 124, 32);
            btnRestore.Click += delegate { RestoreDefault(); };
            card.Controls.Add(btnRestore);

            btnReload = new ModernButton("重启资源管理器", false);
            btnReload.SetBounds(282, 98, 132, 32);
            btnReload.Click += delegate
            {
                SetStatus("正在重启资源管理器…");
                CursorWorks.RestartExplorer();
                LoadAll();
                SetStatus("资源管理器已重启。若光标仍未变化，请重新点击「应用此方案」。");
            };
            card.Controls.Add(btnReload);

            ModernButton btnCustom = new ModernButton("打开自定义文件夹", false);
            btnCustom.SetBounds(422, 98, 152, 32);
            btnCustom.Click += delegate
            {
                try { System.Diagnostics.Process.Start("explorer.exe", CursorWorks.CustomDir()); }
                catch (Exception ex) { SetStatus("打开失败：" + ex.Message); }
            };
            card.Controls.Add(btnCustom);
        }

        private void BuildSizeCard()
        {
            CardPanel card = new CardPanel();
            card.SetBounds(24, 242, ClientSize.Width - 48, 152);
            Controls.Add(card);
            CardTitle(card, "指针大小");

            lblSizeValue = new Label();
            lblSizeValue.Font = Theme.F(10f, FontStyle.Bold);
            lblSizeValue.ForeColor = Theme.Primary;
            lblSizeValue.BackColor = Theme.Card;
            lblSizeValue.SetBounds(card.Width - 210, 12, 192, 24);
            lblSizeValue.TextAlign = ContentAlignment.MiddleRight;
            card.Controls.Add(lblSizeValue);

            slider = new ModernSlider();
            slider.SetBounds(18, 52, card.Width - 160, 38);
            slider.Minimum = 1;
            slider.Maximum = 15;
            slider.OnChanged = delegate(int v) { UpdateSizeLabel(); SkinHandForLevel(v); };
            card.Controls.Add(slider);

            btnApplySize = new ModernButton("应用大小", true);
            btnApplySize.SetBounds(card.Width - 132, 52, 114, 38);
            btnApplySize.Click += delegate { ApplySize(); };
            card.Controls.Add(btnApplySize);

            Label hint = new Label();
            hint.Text = "1 档最小，15 档最大；每档 +8 px，按屏幕缩放自动换算";
            hint.Font = Theme.F(8.5f, FontStyle.Regular);
            hint.ForeColor = Theme.TextSub;
            hint.BackColor = Theme.Card;
            hint.SetBounds(18, 96, card.Width - 200, 20);
            card.Controls.Add(hint);

            lblActual = new Label();
            lblActual.Font = Theme.F(8.5f, FontStyle.Regular);
            lblActual.ForeColor = Theme.TextSub;
            lblActual.BackColor = Theme.Card;
            lblActual.SetBounds(18, 120, card.Width - 40, 20);
            card.Controls.Add(lblActual);
        }

        private void BuildPreviewCard()
        {
            int top = 404;
            int height = ClientSize.Height - top - 64;
            CardPanel card = new CardPanel();
            card.SetBounds(24, top, ClientSize.Width - 48, height);
            Controls.Add(card);
            CardTitle(card, "指针形态预览");

            flow = new FlowLayoutPanel();
            flow.SetBounds(12, 44, card.Width - 24, card.Height - 56);
            flow.BackColor = Theme.Card;
            flow.AutoScroll = true;
            flow.Padding = new Padding(4);
            card.Controls.Add(flow);
        }

        private void BuildStatus()
        {
            lblStatus = new Label();
            lblStatus.Font = Theme.F(8.5f, FontStyle.Regular);
            lblStatus.ForeColor = Theme.TextSub;
            lblStatus.BackColor = Theme.Bg;
            lblStatus.SetBounds(28, ClientSize.Height - 44, ClientSize.Width - 56, 24);
            Controls.Add(lblStatus);
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        private void LoadAll()
        {
            schemes = CursorWorks.LoadSchemes();
            List<string> items = new List<string>();
            string current = CursorWorks.CurrentSchemeName();
            int idx = -1;
            for (int i = 0; i < schemes.Count; i++)
            {
                items.Add(schemes[i].ToString());
                if (idx < 0 && !string.IsNullOrEmpty(current) &&
                    string.Equals(schemes[i].Name, current, StringComparison.OrdinalIgnoreCase)) idx = i;
            }
            if (idx < 0) idx = 0;
            cmb.SetItems(items, idx);

            int level = CursorWorks.CurrentSize();
            if (level < 1) level = 1;
            if (level > 15) level = 15;
            slider.Value = level;
            UpdateSizeLabel();
            RefreshTiles(SelectedScheme(), true);
            AppCursors.Reset();
            ApplyAppCursors();
            int packs = 0;
            foreach (Scheme s2 in schemes) if (s2.IsPack) packs++;
            SetStatus("已载入 " + schemes.Count + " 个方案（自定义 " + packs + " 个）。当前：" + (string.IsNullOrEmpty(current) ? "未知" : current));
        }

        private void ApplyAppCursors()
        {
            Cursor hc = AppCursors.Hand() ?? Cursors.Hand;
            SkinCursors(this, hc);
        }

        private void SkinHandForLevel(int level)
        {
            int px = 32 + (level - 1) * 8;
            Cursor hc = AppCursors.HandForPx(px) ?? Cursors.Hand;
            SkinCursors(this, hc);
        }

        private static void SkinCursors(Control parent, Cursor c)
        {
            foreach (Control x in parent.Controls)
            {
                if (x is ModernButton || x is RoundIconButton || x is ModernComboBox || x is ModernSlider)
                    x.Cursor = c;
                if (x.HasChildren) SkinCursors(x, c);
            }
        }

        private void UpdateSizeLabel()
        {
            int px = 32 + (slider.Value - 1) * 8;
            lblSizeValue.Text = "档位 " + slider.Value + " · 约 " + px + " px";
        }

        private void RefreshTiles(Scheme preview, bool measure)
        {
            flow.SuspendLayout();
            while (flow.Controls.Count > 0)
            {
                Control c = flow.Controls[0];
                flow.Controls.RemoveAt(0);
                c.Dispose();
            }
            for (int i = 0; i < Roles.Keys.Length; i++)
            {
                string p = RolePathFor(preview, i, Roles.Keys[i]);
                CursorTile tile = new CursorTile(Roles.Names[i], p);
                tile.Margin = new Padding(3, 3, 3, 3);
                flow.Controls.Add(tile);
            }
            for (int j = 0; j < Roles.ExtraKeys.Length; j++)
            {
                int idx = Roles.Keys.Length + j;
                string p2 = RolePathFor(preview, idx, Roles.ExtraKeys[j]);
                CursorTile tile2 = new CursorTile(Roles.Names[idx], p2);
                tile2.Margin = new Padding(3, 3, 3, 3);
                flow.Controls.Add(tile2);
            }
            flow.ResumeLayout();

            if (!measure) return;
            int actual = MeasureActual();
            int want = 32 + (slider.Value - 1) * 8;
            if (actual <= 0) lblActual.Text = "系统当前实际尺寸：读取失败";
            else lblActual.Text = "系统当前实际尺寸：" + actual + " px（目标 " + want + " px）";
        }

        private static string RolePathFor(Scheme s, int idx, string key)
        {
            if (s != null && s.Paths != null && idx >= 0 && idx < s.Paths.Length)
            {
                string p = s.Paths[idx];
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p;
            }
            return CursorWorks.GetRolePath(key);
        }

        private Scheme SelectedScheme()
        {
            int i = cmb.SelectedIndex;
            return (i >= 0 && i < schemes.Count) ? schemes[i] : null;
        }

        private int MeasureActual()
        {
            Point old = Cursor.Position;
            int px = 0;
            try
            {
                Cursor.Position = PointToScreen(new Point(ClientSize.Width / 2, ClientSize.Height - 46));
                Application.DoEvents();
                System.Threading.Thread.Sleep(220);
                px = CursorWorks.GetCursorPixels();
            }
            finally
            {
                Cursor.Position = old;
            }
            return px;
        }

        private void ApplyCurrentScheme()
        {
            int idx = cmb.SelectedIndex;
            if (idx < 0 || idx >= schemes.Count) return;
            try
            {
                CursorWorks.ApplyScheme(schemes[idx]);
                if (slider.Value > 1) CursorWorks.ApplySize(slider.Value);
                RefreshTiles(schemes[idx], true);
                LoadAll();
                SetStatus("已应用「" + schemes[idx].Name + "」，并按 " + slider.Value + " 档重设了大小。");
            }
            catch (Exception ex)
            {
                SetStatus("应用失败：" + ex.Message);
            }
        }

        private void ApplySize()
        {
            try
            {
                int applied = CursorWorks.ApplySize(slider.Value);
                AppCursors.Reset();
                ApplyAppCursors();
                int actual = MeasureActual();
                UpdateSizeLabel();
                lblActual.Text = "系统当前实际尺寸：" + (actual <= 0 ? "读取失败" : actual + " px");
                SetStatus("已按 " + slider.Value + " 档（约 " + (32 + (slider.Value - 1) * 8) + " px）重设 " + applied + " 个系统指针，立即生效。");
            }
            catch (Exception ex)
            {
                SetStatus("调整大小失败：" + ex.Message);
            }
        }

        private void Import()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "选择光标包文件夹（里面应包含 .cur / .ani 文件）";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    int matched;
                    int total;
                    string name = CursorWorks.ImportPack(dlg.SelectedPath, out matched, out total);
                    LoadAll();
                    SetStatus("已导入「" + name + "」到自定义目录：识别 " + matched + " / " + total + " 种指针，已加入方案列表。");
                }
                catch (Exception ex)
                {
                    SetStatus("导入失败：" + ex.Message);
                }
            }
        }

        private void RestoreDefault()
        {
            Scheme target = null;
            foreach (Scheme s in schemes)
            {
                if (s.IsSystem && string.Equals(s.Name, "Windows Aero", StringComparison.OrdinalIgnoreCase)) { target = s; break; }
            }
            if (target == null)
            {
                SetStatus("未找到「Windows Aero」方案。");
                return;
            }
            try
            {
                CursorWorks.ApplyScheme(target);
                LoadAll();
                SetStatus("已恢复为 Windows Aero（系统默认白色指针）。");
            }
            catch (Exception ex)
            {
                SetStatus("恢复失败：" + ex.Message);
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args != null && args.Length >= 2 && string.Equals(args[0], "--import", StringComparison.OrdinalIgnoreCase))
            {
                string log = Path.Combine(Path.GetTempPath(), "CursorStudio-import.log");
                try
                {
                    int matched;
                    int total;
                    string name = CursorWorks.ImportPack(args[1], out matched, out total);
                    File.WriteAllText(log, "name=" + name + "\nmatched=" + matched + "\ntotal=" + total);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(log, "ERROR " + ex.Message);
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
