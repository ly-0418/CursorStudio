using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

        // 整体字号系数：所有字号都从这里过一遍，缩小界面上全部文字而保持原有层级比例。
        // 只影响字体，不影响控件尺寸与间距（Px() 仍走 K）。
        internal const float TextScale = 0.85f;

        internal static int Px(int v)
        {
            return (int)Math.Round(v * K);
        }

        internal static Font F(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size * K * TextScale, style);
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
        private static readonly Dictionary<int, Cursor> handCache = new Dictionary<int, Cursor>();
        private static readonly Dictionary<int, Cursor> arrowCache = new Dictionary<int, Cursor>();
        private static Cursor hand;
        private static int handPx = -1;
        private static Cursor arrow;
        private static int arrowPx = -1;

        internal static void Reset()
        {
            handCache.Clear();
            arrowCache.Clear();
            handPx = -1;
            arrowPx = -1;
            hand = null;
            arrow = null;
        }

        internal static Cursor Hand()
        {
            if (hand != null) return hand;
            return HandForPx(32 + (CursorWorks.CurrentSize() - 1) * 8);
        }

        internal static Cursor Arrow()
        {
            if (arrow != null) return arrow;
            return ArrowForPx(32 + (CursorWorks.CurrentSize() - 1) * 8);
        }

        internal static Cursor HandForPx(int px)
        {
            px = Clamp(px);
            if (hand != null && handPx == px) return hand;
            Cursor c;
            if (handCache.ContainsKey(px)) c = handCache[px];
            else { c = Build("Hand", px); handCache[px] = c; }
            handPx = px;
            hand = c;
            return c;
        }

        internal static Cursor ArrowForPx(int px)
        {
            px = Clamp(px);
            if (arrow != null && arrowPx == px) return arrow;
            Cursor c;
            if (arrowCache.ContainsKey(px)) c = arrowCache[px];
            else { c = Build("Arrow", px); arrowCache[px] = c; }
            arrowPx = px;
            arrow = c;
            return c;
        }

        private static int Clamp(int px)
        {
            if (px < 24) px = 24;
            // 窗口内的手形/箭头封顶 40 逻辑像素：档位调很大时，
            // 原尺寸的手形会把下拉列表整行盖住，反而不好点。
            int max = (int)Math.Round(40 * Theme.K);
            if (px > max) px = max;
            return px;
        }

        private static Cursor Build(string role, int px)
        {
            try
            {
                string p = CursorWorks.GetRolePath(role);
                if (string.IsNullOrEmpty(p) || !File.Exists(p)) return null;
                IntPtr h = CursorWorks.BuildHdCursor(p, px);
                if (h != IntPtr.Zero) return new Cursor(h);
            }
            catch { }
            return null;
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
        internal const int HTLEFT = 10;
        internal const int HTRIGHT = 11;
        internal const int HTTOP = 12;
        internal const int HTTOPLEFT = 13;
        internal const int HTTOPRIGHT = 14;
        internal const int HTBOTTOM = 15;
        internal const int HTBOTTOMLEFT = 16;
        internal const int HTBOTTOMRIGHT = 17;
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
            dlg.Cursor = AppCursors.Arrow() ?? Cursors.Default;
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
            itemH = Theme.Px(30);     // 字号整体缩小后按同比例收紧行高，文字不会显得悬在空行里
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
        private bool hover;
        private float knobT;                  // 悬停/拖动时滑块放大与光晕的进度 0..1
        private Timer anim;
        internal Action<int> OnChanged;
        internal Action<int> OnDragEnd;

        internal ModernSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.F(9f, FontStyle.Regular);
            BackColor = Theme.Card;      // 卡片是白的，滑块自己别再把卡片底色画成一块灰
            Height = 38;
            Cursor = AppCursors.Hand() ?? Cursors.Hand;
            anim = new Timer();
            anim.Interval = 15;
            anim.Tick += AnimTick;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && anim != null)
            {
                anim.Stop();
                anim.Dispose();
                anim = null;
            }
            base.Dispose(disposing);
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

        private int Pad() { return (int)Math.Round(13 * Theme.K); }

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

        private void Wake()
        {
            if (anim != null && !anim.Enabled) anim.Start();
        }

        private void AnimTick(object sender, EventArgs e)
        {
            float target = (hover || dragging) ? 1f : 0f;
            float d = target - knobT;
            if (Math.Abs(d) <= 0.04f)
            {
                knobT = target;
                if (anim != null) anim.Stop();
            }
            else
            {
                knobT += d * 0.34f;
            }
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Wake();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            Wake();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                Wake();
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
            Wake();
            base.OnMouseUp(e);
            if (OnDragEnd != null) OnDragEnd(value);
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
                if (OnDragEnd != null) OnDragEnd(value);
            }
            else if (e.KeyCode == Keys.Right && value < max)
            {
                value++;
                Invalidate();
                if (OnChanged != null) OnChanged(value);
                if (OnDragEnd != null) OnDragEnd(value);
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float k = Theme.K;
            int left = Pad();
            int right = Width - Pad();
            if (right <= left + 4) return;
            int cy = Height / 2;
            int th = (int)Math.Max(4, Math.Round(6 * k));      // 轨道比原来细一点
            float r = 9.5f * k + 2f * k * knobT;               // 悬停时滑块略微长大

            int knobX = left + (int)Math.Round((double)(value - min) / (double)(max - min) * (right - left));

            // 轨道底：上深下浅一点点，看着像条浅槽而不是一条实心色带
            Rectangle track = new Rectangle(left, cy - th / 2, right - left, th);
            using (GraphicsPath p = Theme.Round(track, th / 2))
            using (LinearGradientBrush tb = new LinearGradientBrush(
                       new Rectangle(track.X, track.Y, Math.Max(2, track.Width), Math.Max(2, track.Height)),
                       Color.FromArgb(211, 219, 233), Color.FromArgb(232, 237, 245), LinearGradientMode.Vertical))
                g.FillPath(tb, p);

            // 已选段：左深右浅的横向渐变，末端藏在滑块底下
            int fillW = knobX - left;
            if (fillW > 1)
            {
                Rectangle fill = new Rectangle(left, cy - th / 2, fillW, th);
                using (GraphicsPath p = Theme.Round(fill, th / 2))
                using (LinearGradientBrush lb = new LinearGradientBrush(
                           new Rectangle(left, cy - th, Math.Max(2, fillW), th * 2),
                           Theme.Primary, Color.FromArgb(104, 166, 248), LinearGradientMode.Horizontal))
                    g.FillPath(lb, p);
            }

            // 投影：两层很淡的同心圆贴着滑块走，别铺太宽，否则会像一圈灰边
            for (int i = 2; i >= 1; i--)
            {
                float sr = r + i * 1.5f * k;
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(i == 1 ? 16 : 7, 28, 54, 104)))
                    g.FillEllipse(sb, knobX - sr, cy - sr + 1.2f * k, sr * 2, sr * 2);
            }

            // 悬停 / 拖动光晕
            if (knobT > 0.01f)
            {
                float hr = r + 5.5f * k * knobT;
                using (SolidBrush hb = new SolidBrush(Color.FromArgb((int)(30 * knobT), Theme.Primary)))
                    g.FillEllipse(hb, knobX - hr, cy - hr, hr * 2, hr * 2);
            }

            // 滑块本体
            using (SolidBrush b = new SolidBrush(Color.White))
                g.FillEllipse(b, knobX - r, cy - r, r * 2, r * 2);
            Color ring = knobT > 0.5f ? Theme.Primary : Color.FromArgb(183, 197, 218);
            using (Pen pen = new Pen(ring, Math.Max(1.1f, 1.25f * k)))
                g.DrawEllipse(pen, knobX - r, cy - r, r * 2, r * 2);
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
        private Bitmap img;
        private Bitmap cache;      // 整块格子的离屏快照：只在悬停/尺寸变化时重画，逐帧只做一次贴图
        private bool hover;
        private int icon;

        internal CursorTile(string title, string cursorPath)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = title;
            icon = Theme.Px(46);
            Size = new Size(Theme.Px(108), Theme.Px(86));
            Font = Theme.F(8.5f, FontStyle.Regular);
            if (!string.IsNullOrEmpty(cursorPath) && File.Exists(cursorPath))
            {
                // 直接按最终绘制尺寸解码，画的时候 1:1 贴图，不再每帧做一次高质量重采样
                img = CursorWorks.LoadCursorBitmap(cursorPath, icon);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { SetHover(true); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { SetHover(false); base.OnMouseLeave(e); }

        private void SetHover(bool on)
        {
            if (hover == on) return;
            hover = on;
            DropCache();
            Invalidate();
        }

        private void DropCache()
        {
            if (cache != null)
            {
                Bitmap b = cache;
                cache = null;
                try { b.Dispose(); } catch { }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DropCache();
                if (img != null)
                {
                    try { img.Dispose(); } catch { }
                    img = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (cache == null || cache.Width != Width || cache.Height != Height) BuildCache();
            if (cache != null) e.Graphics.DrawImageUnscaled(cache, 0, 0);
        }

        private void BuildCache()
        {
            DropCache();
            if (Width <= 0 || Height <= 0) return;
            Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Theme.Card);                       // 卡片底色，四角不留透明残留
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath p = Theme.Round(r, Theme.Px(10)))
                {
                    using (SolidBrush b = new SolidBrush(hover ? Color.FromArgb(248, 251, 255) : Color.White)) g.FillPath(b, p);
                    using (Pen pen = new Pen(hover ? Theme.BorderFocus : Theme.Border, 1f)) g.DrawPath(pen, p);
                }
                if (img != null)
                {
                    int ix = (Width - icon) / 2;
                    int iy = Theme.Px(7);
                    g.DrawImage(img, ix, iy, icon, icon);
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
            cache = bmp;
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

    internal class TrafficDots : Control
    {
        internal Action<int> OnDot;    // 0 = 黄（最小化）、1 = 绿（最大化/还原）、2 = 红（关闭）
        internal Action<int> OnHover;  // 悬停的圆点序号，-1 = 移出
        internal bool Maximized;

        private int hover = -1;
        private int dot, gap, pad;
        private readonly float[] appear = new float[3];   // 每个圆点字形的浮现进度 0..1
        private Timer anim;

        private static readonly Color Green = Color.FromArgb(40, 205, 65);
        private static readonly Color GreenHot = Color.FromArgb(52, 217, 79);
        private static readonly Color Yellow = Color.FromArgb(254, 189, 46);
        private static readonly Color YellowHot = Color.FromArgb(255, 200, 68);
        private static readonly Color Red = Color.FromArgb(255, 96, 89);
        private static readonly Color RedHot = Color.FromArgb(255, 113, 105);

        internal TrafficDots()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            float k = Theme.K;
            dot = (int)Math.Round(20 * k);
            gap = (int)Math.Round(11 * k);
            pad = (int)Math.Round(5 * k);
            BackColor = Theme.Bg;
            Size = new Size(dot * 3 + gap * 2 + pad * 2, dot + pad * 2);
            anim = new Timer();
            anim.Interval = 15;
            anim.Tick += AnimTick;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && anim != null)
            {
                anim.Stop();
                anim.Dispose();
                anim = null;
            }
            base.Dispose(disposing);
        }

        private int DotX(int i) { return (Width - (dot * 3 + gap * 2)) / 2 + i * (dot + gap); }
        private int DotY { get { return (Height - dot) / 2; } }

        private int IndexAt(Point p)
        {
            int oy = DotY;
            if (p.Y < oy - gap / 2 || p.Y > oy + dot + gap / 2) return -1;
            for (int i = 0; i < 3; i++)
            {
                int x = DotX(i);
                if (p.X >= x - gap / 2 && p.X <= x + dot + gap / 2) return i;
            }
            return -1;
        }

        private void SetHover(int idx)
        {
            if (idx == hover) return;
            hover = idx;
            if (anim != null) anim.Start();
            if (OnHover != null) OnHover(idx);
        }

        internal Rectangle DotRect(int i)
        {
            return new Rectangle(DotX(i), DotY, dot, dot);
        }

        private void AnimTick(object sender, EventArgs e)
        {
            bool busy = false;
            for (int i = 0; i < 3; i++)
            {
                float target = (hover == i) ? 1f : 0f;
                float d = target - appear[i];
                if (Math.Abs(d) <= 0.03f) appear[i] = target;
                else { appear[i] += d * 0.34f; busy = true; }
            }
            Invalidate();
            if (!busy && anim != null) anim.Stop();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetHover(IndexAt(e.Location));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetHover(-1);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            SetHover(IndexAt(e.Location));
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int idx = IndexAt(e.Location);
                if (idx >= 0 && idx == hover && OnDot != null) OnDot(idx);
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Color[] baseCols = new Color[] { Yellow, Green, Red };
            Color[] hotCols = new Color[] { YellowHot, GreenHot, RedHot };
            for (int i = 0; i < 3; i++)
            {
                float t = appear[i];
                RectangleF r = new RectangleF(DotX(i), DotY, dot, dot);
                Color fill = t <= 0.001f ? baseCols[i] : Mix(baseCols[i], hotCols[i], t);
                using (SolidBrush b = new SolidBrush(fill)) g.FillEllipse(b, r);
                if (t <= 0.02f) continue;
                using (Pen ring = new Pen(Color.FromArgb((int)Math.Round(52 * t), 18, 18, 18), 1f))
                    g.DrawEllipse(ring, r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f);
                DrawGlyph(g, i, t, fill, r);
            }
        }

        // 字形一律用矢量笔画手绘：以圆心为基准对称展开，天然居中，
        // 尺寸按圆点直径的固定比例算，换 DPI 时自动跟着圆点一起放大缩小。
        private void DrawGlyph(Graphics g, int idx, float t, Color dotFill, RectangleF r)
        {
            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            cx = (float)Math.Round(cx * 2f) / 2f;      // 半像素对齐，笔画不发虚
            cy = (float)Math.Round(cy * 2f) / 2f;
            float grow = 0.76f + 0.24f * t;            // 浮现瞬间略小，落到 1.0 时正好铺满
            float s = dot * 0.215f * grow;             // 字形半边长：留出约 28% 的边距，不贴圆点外沿
            float w = Math.Max(1.25f, dot * 0.082f);   // 线宽随圆点缩放
            int alpha = (int)Math.Round(212 * t);
            using (Pen pen = new Pen(Color.FromArgb(alpha, 26, 26, 28), w))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (idx == 2)
                {
                    float d = s * 0.78f;               // 叉号的对角线最占地方，收得比横竖更小一点
                    g.DrawLine(pen, cx - d, cy - d, cx + d, cy + d);
                    g.DrawLine(pen, cx - d, cy + d, cx + d, cy - d);
                }
                else if (idx == 0)
                {
                    g.DrawLine(pen, cx - s, cy, cx + s, cy);
                }
                else if (!Maximized)
                {
                    float d = s * 0.88f;
                    g.DrawLine(pen, cx - d, cy, cx + d, cy);
                    g.DrawLine(pen, cx, cy - d, cx, cy + d);
                }
                else
                {
                    DrawRestore(g, pen, dotFill, cx, cy, s);
                }
            }
        }

        // 还原：两个叠放的方框（后框先描，前框用圆点底色盖住再描，小尺寸也不会糊成一团）
        private static void DrawRestore(Graphics g, Pen pen, Color dotFill, float cx, float cy, float s)
        {
            float s2 = s * 0.86f;              // 方框的笔画落在包围盒最外沿，比横竖更显大，所以整体再收一点
            float o = s2 * 0.58f;
            float b = s2 - o / 2f;
            RectangleF back = new RectangleF(cx - b + o / 2f, cy - b - o / 2f, b * 2f, b * 2f);
            RectangleF front = new RectangleF(cx - b - o / 2f, cy - b + o / 2f, b * 2f, b * 2f);
            g.DrawRectangle(pen, back.X, back.Y, back.Width, back.Height);
            using (SolidBrush bf = new SolidBrush(dotFill)) g.FillRectangle(bf, front);
            g.DrawRectangle(pen, front.X, front.Y, front.Width, front.Height);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }

    // 悬停提示：贴在圆点下方的小黑气泡
    internal class TipBubble : Control
    {
        private string text = "";
        private Font font;

        internal TipBubble()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            font = Theme.F(8.5f, FontStyle.Regular);
            Visible = false;
        }

        internal void ShowTip(string t, Point anchor, int limitRight)
        {
            text = t;
            Size sz = TextRenderer.MeasureText(t, font);
            int w = sz.Width + Theme.Px(20);
            int h = sz.Height + Theme.Px(12);
            int x = anchor.X - w / 2;
            if (x + w > limitRight) x = limitRight - w;
            if (x < Theme.Px(8)) x = Theme.Px(8);
            Bounds = new Rectangle(x, anchor.Y, w, h);
            if (!Visible) Visible = true;
            BringToFront();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, Theme.Px(7)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 44, 50, 62)))
                g.FillPath(b, p);
            Rectangle tr = new Rectangle(Theme.Px(8), 0, Width - Theme.Px(16), Height);
            TextRenderer.DrawText(g, text, font, tr, Color.FromArgb(246, 248, 252),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // 最小化动画用的快照层：整个界面被拍成一张图，随窗口一起缩小
    internal class SnapshotView : Control
    {
        private Bitmap src;

        internal SnapshotView(Bitmap bmp)
        {
            src = bmp;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (src == null) return;
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.Bilinear;   // 缩小时用双线性就够，且便宜
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.DrawImage(src, new Rectangle(0, 0, Width, Height));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && src != null)
            {
                try { src.Dispose(); } catch { }
                src = null;
            }
            base.Dispose(disposing);
        }
    }

    // 细进度条：圆角 + 渐变填充。下载进度本身更新很密，直接按真实值画就足够平滑，
    // 不再做定时器追赶（那套在这里不稳定，反而会出现「有值但不填充」）。
    internal class ThinProgress : Control
    {
        private float value;

        internal ThinProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Visible = false;
        }

        internal float Value
        {
            get { return value; }
            set
            {
                float v = value;
                if (v < 0f) v = 0f;
                if (v > 1f) v = 1f;
                if (Math.Abs(v - this.value) < 0.0005f) return;
                this.value = v;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int h = Height;
            if (h < 2 || Width < 2) return;
            Rectangle track = new Rectangle(0, 0, Width, h);
            using (GraphicsPath p = Theme.Round(track, h / 2))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(214, 222, 234)))
                g.FillPath(b, p);

            int w = (int)Math.Round(Width * value);
            if (w >= h)
            {
                Rectangle fill = new Rectangle(0, 0, w, h);
                using (GraphicsPath p = Theme.Round(fill, h / 2))
                using (LinearGradientBrush lb = new LinearGradientBrush(
                           new Rectangle(0, 0, Math.Max(2, w), h),
                           Theme.Primary, Color.FromArgb(104, 166, 248), LinearGradientMode.Horizontal))
                    g.FillPath(lb, p);
            }
            else if (w > 0)
            {
                using (SolidBrush b = new SolidBrush(Theme.Primary))
                    g.FillEllipse(b, 0, 0, h, h);
            }
        }
    }

    internal class OnlineSource
    {
        internal string Label;
        internal string Repo;      // "owner/repo"；Custom 时为空
        internal bool Custom;
        internal bool Mirror;      // 走 ghproxy 镜像前缀
    }

    internal class OnlinePack
    {
        internal string Name;
        internal string Url;
        internal long Size;
        internal string Display;
    }

    // 从链接安装：一个只有输入框的小弹窗
    internal class UrlPromptForm : Form
    {
        private TextBox box;
        internal string Url = "";

        private static int T(int v) { return Theme.Px(v); }

        internal UrlPromptForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.F(9f, FontStyle.Regular);
            DoubleBuffered = true;
            KeyPreview = true;
            ClientSize = new Size(T(560), T(196));

            Label title = new Label();
            title.Text = "从链接安装";
            title.Font = Theme.F(10.5f, FontStyle.Bold);
            title.ForeColor = Theme.TextMain;
            title.BackColor = Theme.Bg;
            title.SetBounds(T(24), T(20), T(320), T(24));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "粘贴一个 .zip 直链，工具会下载后自动解压到 custom\\ 目录";
            sub.Font = Theme.F(8.5f, FontStyle.Regular);
            sub.ForeColor = Theme.TextSub;
            sub.BackColor = Theme.Bg;
            sub.SetBounds(T(24), T(46), ClientSize.Width - T(48), T(20));
            Controls.Add(sub);

            box = new TextBox();
            box.BorderStyle = BorderStyle.None;
            box.Font = Theme.F(9.5f, FontStyle.Regular);
            box.BackColor = Color.White;
            box.ForeColor = Theme.TextMain;
            box.SetBounds(T(36), T(82), ClientSize.Width - T(72), T(22));
            Controls.Add(box);

            ModernButton ok = new ModernButton("下载并安装", true);
            ok.SetBounds(ClientSize.Width - T(24) - T(112), T(140), T(112), T(34));
            ok.Click += delegate
            {
                string t = box.Text.Trim();
                if (t.Length < 8 || !t.ToLowerInvariant().StartsWith("http"))
                {
                    box.Focus();
                    return;
                }
                Url = t;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);

            ModernButton cancel = new ModernButton("取消", false);
            cancel.SetBounds(ClientSize.Width - T(24) - T(112) - T(10) - T(80), T(140), T(80), T(34));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            MouseDown += Drag;
            title.MouseDown += Drag;
            KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
        }

        private void Drag(object o, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Shell.ReleaseCapture();
            Shell.SendMessage(Handle, Shell.WM_NCLBUTTONDOWN, (IntPtr)Shell.HTCAPTION, IntPtr.Zero);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, T(12)))
            using (Pen pen = new Pen(Theme.Border, 1f))
                g.DrawPath(pen, p);

            Rectangle f = new Rectangle(T(24), T(70), ClientSize.Width - T(48), T(44));
            using (GraphicsPath p = Theme.Round(f, T(8)))
            {
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, p);
                using (Pen pen = new Pen(Theme.BorderFocus, 1f)) g.DrawPath(pen, p);
            }
        }
    }

    // 在线光标包：从 GitHub Release（或自定义直链）拉取，选中即下载安装
    internal class OnlinePacksForm : Form
    {
        private List<OnlineSource> sources = new List<OnlineSource>();
        private List<OnlinePack> packs = new List<OnlinePack>();
        private ModernComboBox cmbSource;
        private ModernButton btnReload;
        private ModernButton btnInstall;
        private SmoothList list;
        private Label lblStatus;
        private System.Net.WebClient wc;
        private ThinProgress progress;
        private bool busy;
        private bool downloading;
        private bool closing;
        private bool mirror;

        internal bool Installed;
        internal string InstalledName = "";

        private static int T(int v) { return Theme.Px(v); }

        internal OnlinePacksForm()
        {
            Text = "在线获取光标包";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.F(9f, FontStyle.Regular);
            DoubleBuffered = true;
            KeyPreview = true;
            ClientSize = new Size(T(640), T(486));

            sources.Add(NewSource("GitHub · Bibata（ful1e5）", "ful1e5/Bibata_Cursor", false, false));
            sources.Add(NewSource("GitHub · BreezeX（ful1e5）", "ful1e5/BreezeX_Cursor", false, false));
            sources.Add(NewSource("Bibata（镜像加速）", "ful1e5/Bibata_Cursor", false, true));
            sources.Add(NewSource("BreezeX（镜像加速）", "ful1e5/BreezeX_Cursor", false, true));
            sources.Add(NewSource("自定义链接…", "", true, false));

            Label title = new Label();
            title.Text = "在线获取光标包";
            title.Font = Theme.F(11f, FontStyle.Bold);
            title.ForeColor = Theme.TextMain;
            title.BackColor = Theme.Bg;
            title.SetBounds(T(24), T(18), T(400), T(26));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "来自官方 GitHub Release 的 Windows 包，选中后安装到 custom\\ 目录";
            sub.Font = Theme.F(8.5f, FontStyle.Regular);
            sub.ForeColor = Theme.TextSub;
            sub.BackColor = Theme.Bg;
            sub.SetBounds(T(24), T(44), ClientSize.Width - T(40), T(20));
            Controls.Add(sub);

            Label lbSrc = new Label();
            lbSrc.Text = "来源";
            lbSrc.Font = Theme.F(9f, FontStyle.Regular);
            lbSrc.ForeColor = Theme.TextSub;
            lbSrc.BackColor = Theme.Bg;
            lbSrc.TextAlign = ContentAlignment.MiddleLeft;
            lbSrc.SetBounds(T(24), T(78), T(40), T(36));
            Controls.Add(lbSrc);

            cmbSource = new ModernComboBox();
            cmbSource.SetBounds(T(68), T(78), T(300), T(36));
            List<string> names = new List<string>();
            foreach (OnlineSource os in sources) names.Add(os.Label);
            cmbSource.SetItems(names, 0);
            cmbSource.OnChanged = delegate(int idx) { if (!busy) PickSource(idx); };
            Controls.Add(cmbSource);

            btnReload = new ModernButton("刷新", false);
            btnReload.SetBounds(T(378), T(78), T(80), T(36));
            btnReload.Click += delegate { if (!busy) PickSource(cmbSource.SelectedIndex); };
            Controls.Add(btnReload);

            CardPanel card = new CardPanel();
            card.SetBounds(T(24), T(128), ClientSize.Width - T(48), T(250));
            Controls.Add(card);

            list = new SmoothList(Font);
            list.SetBounds(T(10), T(10), card.Width - T(20), card.Height - T(20));
            list.OnPick = delegate(int idx) { ShowPackHint(idx); };
            card.Controls.Add(list);

            progress = new ThinProgress();
            progress.SetBounds(T(24), T(383), ClientSize.Width - T(48), T(8));
            Controls.Add(progress);

            lblStatus = new Label();
            lblStatus.Text = "";
            lblStatus.Font = Theme.F(8.5f, FontStyle.Regular);
            lblStatus.ForeColor = Theme.TextSub;
            lblStatus.BackColor = Theme.Bg;
            lblStatus.SetBounds(T(26), T(398), ClientSize.Width - T(52), T(20));
            Controls.Add(lblStatus);

            btnInstall = new ModernButton("下载并安装", true);
            btnInstall.SetBounds(ClientSize.Width - T(24) - T(68) - T(10) - T(124), T(430), T(124), T(34));
            btnInstall.Click += delegate { OnPrimary(); };
            Controls.Add(btnInstall);

            ModernButton btnClose = new ModernButton("关闭", false);
            btnClose.SetBounds(ClientSize.Width - T(24) - T(68), T(430), T(68), T(34));
            btnClose.Click += delegate { Close(); };
            Controls.Add(btnClose);

            MouseDown += Drag;
            title.MouseDown += Drag;
            sub.MouseDown += Drag;
            KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };

            PickSource(0);
        }

        private static OnlineSource NewSource(string label, string repo, bool custom, bool mirror)
        {
            OnlineSource s = new OnlineSource();
            s.Label = label;
            s.Repo = repo;
            s.Custom = custom;
            s.Mirror = mirror;
            return s;
        }

        private void Drag(object o, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Shell.ReleaseCapture();
            Shell.SendMessage(Handle, Shell.WM_NCLBUTTONDOWN, (IntPtr)Shell.HTCAPTION, IntPtr.Zero);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, T(12)))
            using (Pen pen = new Pen(Theme.Border, 1f))
                e.Graphics.DrawPath(pen, p);
        }

        private void SetStatus(string t)
        {
            lblStatus.Text = t;
        }

        private void ShowPackHint(int idx)
        {
            if (idx < 0 || idx >= packs.Count) return;
            SetStatus(packs[idx].Name + " · " + MB(packs[idx].Size) + " · 点「下载并安装」");
        }

        private static string MB(long b)
        {
            if (b <= 0) return "未知大小";
            return (b / 1048576.0).ToString("0.0") + " MB";
        }

        private void Cancel()
        {
            try { if (wc != null) wc.CancelAsync(); } catch { }
            if (wc != null) { try { wc.Dispose(); } catch { } wc = null; }
            busy = false;
        }

        private void PickSource(int idx)
        {
            if (idx < 0 || idx >= sources.Count) return;
            if (sources[idx].Custom)
            {
                AskUrl();
                return;
            }
            FetchRelease(sources[idx]);
        }

        private void AskUrl()
        {
            using (UrlPromptForm f = new UrlPromptForm())
            {
                if (f.ShowDialog(this) != DialogResult.OK)
                {
                    cmbSource.SelectedIndex = 0;
                    return;
                }
                string url = f.Url;
                string name = url;
                int q = name.IndexOf('?');
                if (q > 0) name = name.Substring(0, q);
                int slash = name.LastIndexOf('/');
                if (slash >= 0) name = name.Substring(slash + 1);

                packs.Clear();
                list.Items.Clear();
                OnlinePack p = new OnlinePack();
                p.Name = name;
                p.Url = url;
                p.Size = 0;
                p.Display = name + "    ·  自定义链接";
                packs.Add(p);
                list.Items.Add(p.Display);
                list.SelectedIndex = 0;
                list.Invalidate();
                SetStatus("已载入自定义链接，点「下载并安装」开始");
            }
        }

        private void FetchRelease(OnlineSource src)
        {
            Cancel();
            mirror = src.Mirror;
            packs.Clear();
            list.Items.Clear();
            list.SelectedIndex = -1;
            list.Invalidate();
            SetStatus("正在读取 " + src.Label + " 的最新 Release…");
            busy = true;

            wc = new System.Net.WebClient();
            wc.Headers.Add("User-Agent", "CursorStudio-PackBrowser");
            wc.Encoding = Encoding.UTF8;
            wc.DownloadStringCompleted += delegate(object o, System.Net.DownloadStringCompletedEventArgs e)
            {
                if (closing || IsDisposed) return;
                busy = false;
                if (e.Error != null) { SetStatus("读取失败：" + Short(e.Error.Message)); return; }
                try { ParseRelease(e.Result); }
                catch (Exception ex) { SetStatus("解析失败：" + Short(ex.Message)); }
            };
            try
            {
                wc.DownloadStringAsync(new Uri("https://api.github.com/repos/" + src.Repo + "/releases/latest"));
            }
            catch (Exception ex)
            {
                busy = false;
                SetStatus("读取失败：" + Short(ex.Message));
            }
        }

        private void ParseRelease(string json)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js =
                new System.Web.Script.Serialization.JavaScriptSerializer();
            js.MaxJsonLength = 16 * 1024 * 1024;
            Dictionary<string, object> root = js.DeserializeObject(json) as Dictionary<string, object>;
            if (root == null) { SetStatus("返回内容不是预期的 JSON。"); return; }
            object[] assets = null;
            if (root.ContainsKey("assets")) assets = root["assets"] as object[];
            if (assets == null) { SetStatus("这个仓库没有 Release 资源。"); return; }

            string tag = root.ContainsKey("tag_name") ? Convert.ToString(root["tag_name"]) : "";

            foreach (object o in assets)
            {
                Dictionary<string, object> a = o as Dictionary<string, object>;
                if (a == null) continue;
                string name = a.ContainsKey("name") ? Convert.ToString(a["name"]) : "";
                string url = a.ContainsKey("browser_download_url") ? Convert.ToString(a["browser_download_url"]) : "";
                if (name.Length == 0 || url.Length == 0) continue;
                if (!name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.IndexOf("windows", StringComparison.OrdinalIgnoreCase) < 0) continue;   // 只要 Windows 包

                long size = 0;
                if (a.ContainsKey("size")) { try { size = Convert.ToInt64(a["size"]); } catch { } }

                if (mirror) url = "https://ghproxy.net/" + url;   // 国内直连 GitHub 往往只有几十 KB/s

                OnlinePack p = new OnlinePack();
                p.Name = name;
                p.Url = url;
                p.Size = size;
                p.Display = name.Substring(0, name.Length - 4) + "    ·  " + MB(size);
                packs.Add(p);
                list.Items.Add(p.Display);
            }
            list.Invalidate();

            if (packs.Count == 0) SetStatus("没找到 Windows 的 zip 资源（" + tag + "）。");
            else SetStatus("共 " + packs.Count + " 个包" + (tag.Length > 0 ? "（" + tag + "）" : "") + "，选中一个再点「下载并安装」");
        }

        private void OnPrimary()
        {
            if (downloading) { CancelDownload(); return; }
            StartInstall();
        }

        private void CancelDownload()
        {
            if (wc == null) return;
            SetStatus("正在取消下载…");
            try { wc.CancelAsync(); } catch { }
        }

        private void ResetDownloadUi()
        {
            busy = false;
            downloading = false;
            if (btnInstall != null)
            {
                btnInstall.Text = "下载并安装";
                btnInstall.Invalidate();
            }
            if (progress != null)
            {
                progress.Value = 0;
                progress.Visible = false;
            }
        }

        private void StartInstall()
        {
            if (busy) return;
            int idx = list.SelectedIndex;
            if (idx < 0 || idx >= packs.Count)
            {
                SetStatus("先在上面的列表里选一个包。");
                return;
            }
            OnlinePack p = packs[idx];
            string tmp = Path.Combine(Path.GetTempPath(), "cs_dl_" + Guid.NewGuid().ToString("N") + ".zip");
            busy = true;
            downloading = true;
            btnInstall.Text = "取消下载";
            btnInstall.Invalidate();
            progress.Value = 0;
            progress.Visible = true;
            SetStatus("正在下载 " + p.Name + " …");

            wc = new System.Net.WebClient();
            wc.Headers.Add("User-Agent", "CursorStudio-PackBrowser");
            wc.DownloadProgressChanged += delegate(object o, System.Net.DownloadProgressChangedEventArgs e)
            {
                if (closing || IsDisposed) return;
                long total = e.TotalBytesToReceive;
                if (total > 0)
                {
                    progress.Value = (float)((double)e.BytesReceived / total);
                    SetStatus("正在下载 " + p.Name + " · " + e.ProgressPercentage + "%（" +
                        MB(e.BytesReceived) + " / " + MB(total) + "）");
                }
                else
                {
                    SetStatus("正在下载 " + p.Name + " · 已下载 " + MB(e.BytesReceived));
                }
            };
            wc.DownloadFileCompleted += delegate(object o, System.ComponentModel.AsyncCompletedEventArgs e)
            {
                if (closing || IsDisposed) return;
                bool cancelled = e.Cancelled;
                ResetDownloadUi();
                if (cancelled)
                {
                    try { File.Delete(tmp); } catch { }
                    SetStatus("已取消下载，临时文件已删除。");
                    return;
                }
                if (e.Error != null)
                {
                    try { File.Delete(tmp); } catch { }
                    SetStatus("下载失败：" + Short(e.Error.Message));
                    return;
                }
                try
                {
                    int matched;
                    string name = CursorWorks.InstallZip(tmp, out matched);
                    InstalledName = name;
                    Installed = true;
                    SetStatus("已安装「" + name + "」，匹配 " + matched + " 个角色，回到主界面就能选它。");
                }
                catch (Exception ex)
                {
                    SetStatus("安装失败：" + Short(ex.Message));
                }
                finally
                {
                    try { File.Delete(tmp); } catch { }
                }
            };
            try
            {
                wc.DownloadFileAsync(new Uri(p.Url), tmp);
            }
            catch (Exception ex)
            {
                ResetDownloadUi();
                SetStatus("下载失败：" + Short(ex.Message));
            }
        }

        private static string Short(string s)
        {
            if (string.IsNullOrEmpty(s)) return "未知错误";
            return s.Length > 90 ? s.Substring(0, 90) + "…" : s;
        }

        protected override void Dispose(bool disposing)
        {
            closing = true;
            if (disposing) Cancel();
            base.Dispose(disposing);
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
        private ModernButton btnCustom;
        private ModernSlider slider;
        private ModernButton btnApplySize;
        private Label lblSizeValue;
        private Label lblActual;
        private FlowLayoutPanel flow;
        private Label lblStatus;
        private Panel head;
        private Label lblHint;
        private TrafficDots dots;
        private TipBubble tip;
        private Timer tipTimer;
        private int tipIdx = -1;
        private CardPanel cardScheme;
        private CardPanel cardSize;
        private CardPanel cardPreview;
        private bool maximized;
        private Rectangle restoreRect;
        private Rectangle animFrom;
        private Rectangle animTo;
        private bool animToMax;
        private bool animating;
        private bool skipLayout;
        private Rectangle miniRestore;
        private System.Diagnostics.Stopwatch animClock;
        private double animDur;
        private int skinnedPx = -1;
        private int actualPx = 32;
        private Timer warmTimer;
        private int warmIdx = 1;
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
            LayoutAll();
            HookResize(this);
            LoadAll();
        }

        private static void ScaleTree(Control parent, float k)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is TrafficDots) continue;   // 红绿灯自绘且已按 DPI 自带换算，不再二次缩放
                c.Location = new Point((int)Math.Round(c.Left * k), (int)Math.Round(c.Top * k));
                c.Size = new Size((int)Math.Round(c.Width * k), (int)Math.Round(c.Height * k));
                c.Padding = new Padding((int)Math.Round(c.Padding.Left * k), (int)Math.Round(c.Padding.Top * k),
                    (int)Math.Round(c.Padding.Right * k), (int)Math.Round(c.Padding.Bottom * k));
                c.Margin = new Padding((int)Math.Round(c.Margin.Left * k), (int)Math.Round(c.Margin.Top * k),
                    (int)Math.Round(c.Margin.Right * k), (int)Math.Round(c.Margin.Bottom * k));
                ScaleTree(c, k);
            }
        }

        private int S(int v) { return (int)Math.Round(v * Theme.K); }


        private void LayoutAll()
        {
            if (skipLayout) return;            // 最小化动画期间只画快照，不重排
            HideTip();
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w < 200 || h < 200) return;
            int m = S(24);
            int pad = S(18);

            if (head != null) head.SetBounds(0, 0, w, S(68));
            if (dots != null)
            {
                Point dp = new Point(w - S(18) - dots.Width, S(18));
                if (dots.Location != dp) dots.Location = dp;
                if (dots.Maximized != maximized) { dots.Maximized = maximized; dots.Invalidate(); }
            }

            if (cardScheme != null)
            {
                cardScheme.SetBounds(m, S(84), w - m * 2, S(146));
                int bw1 = S(124), bw2 = S(96), gap = S(10);
                int refreshX = cardScheme.Width - pad - bw2;
                int applyX = refreshX - gap - bw1;
                btnRefresh.SetBounds(refreshX, S(46), bw2, S(38));
                btnApply.SetBounds(applyX, S(46), bw1, S(38));
                int cmbW = applyX - gap - pad;
                if (cmbW < S(140)) cmbW = S(140);
                cmb.SetBounds(pad, S(46), cmbW, S(38));

                btnImport.SetBounds(S(18), S(98), S(124), S(32));
                btnRestore.SetBounds(S(150), S(98), S(124), S(32));
                btnReload.SetBounds(S(282), S(98), S(132), S(32));
                btnCustom.SetBounds(cardScheme.Width - pad - S(152), S(98), S(152), S(32));
                int runRight = cardScheme.Width - pad - S(152) - gap;
                if (S(282) + S(132) > runRight)
                    btnReload.SetBounds(runRight - S(132), S(98), S(132), S(32));
                if (S(150) + S(124) > runRight - S(132) - gap)
                    btnRestore.SetBounds(runRight - S(132) - gap - S(124), S(98), S(124), S(32));
            }

            if (cardSize != null)
            {
                cardSize.SetBounds(m, S(242), w - m * 2, S(152));
                int btnW = S(114);
                int applyX = cardSize.Width - pad - btnW;
                btnApplySize.SetBounds(applyX, S(52), btnW, S(38));
                int sw = applyX - S(24) - pad;
                if (sw < S(140)) sw = S(140);
                slider.SetBounds(pad, S(52), sw, S(38));
                lblSizeValue.SetBounds(cardSize.Width - S(210), S(12), S(192), S(24));
                if (lblHint != null) lblHint.SetBounds(pad, S(96), cardSize.Width - S(200), S(20));
                lblActual.SetBounds(pad, S(120), cardSize.Width - S(40), S(20));
            }

            if (cardPreview != null)
            {
                int top = S(404);
                int bh = h - top - S(64);
                if (bh < S(140)) bh = S(140);
                cardPreview.SetBounds(m, top, w - m * 2, bh);
                flow.SetBounds(S(12), S(44), cardPreview.Width - S(24), cardPreview.Height - S(56));
            }

            if (lblStatus != null) lblStatus.SetBounds(S(28), h - S(44), w - S(56), S(24));
        }

        private void ToggleMaximize()
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            bool toMax = !maximized;
            Rectangle to;
            if (toMax)
            {
                if (!animating) restoreRect = Bounds;      // 动画中途再点，沿用最初的还原尺寸
                to = area;
            }
            else
            {
                to = restoreRect;
                if (to.Width < S(660) || to.Height < S(720))
                    to = new Rectangle(area.X + (area.Width - S(860)) / 2, area.Y + (area.Height - S(944)) / 2, S(860), S(944));
            }
            AnimWindow(to, toMax);
        }

        // 帧循环：动画与拖边缩放共用。原来靠 15 ms 定时器，每次都要「等定时器 + 干活」，
        // 实测满屏放大只跑出 13 帧、每帧跳 82 px；改成自己驱动帧，机器能画多快就跑多快。
        private bool inFrame;

        private bool RunFrames(Func<bool> step)
        {
            if (inFrame) return false;          // 已经在循环里了，交给外层
            inFrame = true;
            try
            {
                while (!IsDisposed && step()) { }
            }
            catch { }
            finally { inFrame = false; }
            return true;
        }

        private bool ApplyAnim(double k)
        {
            int x = (int)Math.Round(animFrom.X + (animTo.X - animFrom.X) * k);
            int y = (int)Math.Round(animFrom.Y + (animTo.Y - animFrom.Y) * k);
            int w = (int)Math.Round(animFrom.Width + (animTo.Width - animFrom.Width) * k);
            int h = (int)Math.Round(animFrom.Height + (animTo.Height - animFrom.Height) * k);
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            if (x == Left && y == Top && w == Width && h == Height) return false;
            Bounds = new Rectangle(x, y, w, h);
            return true;
        }

        private void AnimWindow(Rectangle to, bool toMax)
        {
            animFrom = Bounds;
            animTo = to;
            animToMax = toMax;
            maximized = toMax;
            if (dots != null) { dots.Maximized = toMax; dots.Invalidate(); }

            if (animFrom == animTo) { FinishAnim(); return; }

            if (animClock == null) animClock = new System.Diagnostics.Stopwatch();
            animClock.Reset();
            animClock.Start();

            // 实测这台机器满屏一帧要 25~35 ms（重排 + 3.9 MP 重绘），帧数只跟时长成正比：
            // 0.24~0.34 s 是「够快」和「帧够多」的折中，再拖长就成慢动作，再收短每帧跳得更狠。
            double span = Math.Max(Math.Abs(animTo.Width - animFrom.Width), Math.Abs(animTo.Height - animFrom.Height));
            animDur = 240.0 + Math.Min(100.0, span * 0.06);
            animating = true;

            bool owner = RunFrames(delegate
            {
                double p = animClock.Elapsed.TotalMilliseconds / animDur;
                if (p >= 1.0) return false;
                double k = p * p * (3.0 - 2.0 * p);      // smoothstep：起步轻、落点稳，不甩尾
                if (ApplyAnim(k)) Application.DoEvents();   // 泵消息，这一帧才会真正画出来
                return true;
            });
            if (!owner) return;                  // 动画途中又点了一下，已改目标，继续由外层跑
            animating = false;
            FinishAnim();
        }

        private void FinishAnim()
        {
            if (animClock != null) animClock.Reset();
            animating = false;
            maximized = animToMax;
            if (Bounds != animTo) Bounds = animTo;
            LayoutAll();
            if (dots != null) { dots.Maximized = maximized; dots.Invalidate(); }
        }

        // 最小化到任务栏：先把界面截成一张图，然后窗口一边缩一边用这张图重采样，
        // 看上去就是整个界面被收进任务栏，而不是「啪」一下消失。
        // 只动窗口 + 一次图片缩放，所以比重新排版便宜一个数量级。
        private void MinimizeAnimated()
        {
            if (WindowState != FormWindowState.Normal || inFrame || animating)
            {
                WindowState = FormWindowState.Minimized;
                return;
            }

            Rectangle from = Bounds;
            Rectangle scr = Screen.FromControl(this).Bounds;
            int tw = S(86), th = S(62);
            Rectangle to = new Rectangle(from.X + from.Width / 2 - tw / 2, scr.Bottom - th, tw, th);

            Bitmap snap = null;
            try
            {
                int cw = ClientSize.Width, ch = ClientSize.Height;
                if (cw > 0 && ch > 0)
                {
                    snap = new Bitmap(cw, ch, PixelFormat.Format32bppArgb);
                    DrawToBitmap(snap, new Rectangle(0, 0, cw, ch));
                }
            }
            catch { snap = null; }

            if (snap == null)
            {
                WindowState = FormWindowState.Minimized;
                return;
            }

            SnapshotView view = new SnapshotView(snap);
            view.Dock = DockStyle.Fill;
            Controls.Add(view);
            view.BringToFront();
            skipLayout = true;

            animFrom = from;
            animTo = to;
            if (animClock == null) animClock = new System.Diagnostics.Stopwatch();
            animClock.Reset();
            animClock.Start();
            animDur = 240.0;

            RunFrames(delegate
            {
                double p = animClock.Elapsed.TotalMilliseconds / animDur;
                if (p >= 1.0) return false;
                double k = p * p * (3.0 - 2.0 * p);
                if (ApplyAnim(k)) Application.DoEvents();
                return true;
            });

            try { Controls.Remove(view); view.Dispose(); } catch { }
            skipLayout = false;

            // 先最小化（窗口不可见），再把尺寸还原成原来的大小，
            // 这样从任务栏点回来时不会停在缩略尺寸上。
            WindowState = FormWindowState.Minimized;
            if (Bounds != from) Bounds = from;
            miniRestore = from;
            LayoutAll();
        }

        private int HitEdge(Point p)
        {
            if (maximized) return 0;
            if (animating || inFrame) return 0;
            if (WindowState != FormWindowState.Normal) return 0;
            int g = S(7);
            int w = ClientSize.Width, h = ClientSize.Height;
            bool left = p.X <= g, right = p.X >= w - g;
            bool top = p.Y <= g, bottom = p.Y >= h - g;
            if (left && top) return Shell.HTTOPLEFT;
            if (right && top) return Shell.HTTOPRIGHT;
            if (left && bottom) return Shell.HTBOTTOMLEFT;
            if (right && bottom) return Shell.HTBOTTOMRIGHT;
            if (left) return Shell.HTLEFT;
            if (right) return Shell.HTRIGHT;
            if (top) return Shell.HTTOP;
            if (bottom) return Shell.HTBOTTOM;
            return 0;
        }

        private static Cursor CursorForEdge(int edge)
        {
            switch (edge)
            {
                case Shell.HTTOPLEFT:
                case Shell.HTBOTTOMRIGHT:
                    return Cursors.SizeNWSE;
                case Shell.HTTOPRIGHT:
                case Shell.HTBOTTOMLEFT:
                    return Cursors.SizeNESW;
                case Shell.HTTOP:
                case Shell.HTBOTTOM:
                    return Cursors.SizeNS;
                default:
                    return Cursors.SizeWE;
            }
        }

        private void RestoreArrowCursor()
        {
            int lv = slider != null ? slider.Value : CursorWorks.CurrentSize();
            if (lv < 1) lv = 1;
            int px = 32 + (lv - 1) * 8;
            Cursor = AppCursors.ArrowForPx(px) ?? Cursors.Default;
        }

        private int dragEdge;
        private Point dragOrigin;
        private Rectangle dragRect;
        private bool resizing;

        private void HookResize(Control c)
        {
            c.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (resizing || inFrame) return;
                Point p = PointToClient(c.PointToScreen(e.Location));
                int edge = HitEdge(p);
                if (edge != 0) Cursor = CursorForEdge(edge);
                else RestoreArrowCursor();
            };
            c.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                if (inFrame) return;
                Point p = PointToClient(c.PointToScreen(e.Location));
                int edge = HitEdge(p);
                if (edge == 0) return;
                dragEdge = edge;
                dragOrigin = Cursor.Position;
                dragRect = Bounds;
                resizing = true;
                Capture = true;

                // 拖边期间不再「每个鼠标消息就重排一次」——鼠标消息一秒能来上百个，
                // 每次都排 15 ms 的版必然积压、越拖越滞后。这里改成按帧推进，只认最新光标位置。
                Point last = Point.Empty;
                RunFrames(delegate
                {
                    if (!resizing || dragEdge == 0) return false;
                    Point cur = Cursor.Position;
                    if (cur != last) { last = cur; ApplyDragResize(); }
                    Application.DoEvents();
                    return true;
                });
                try { if (dragEdge != 0) ApplyDragResize(); } catch { }   // 松手那一帧也跟上，别停在倒数第二个位置
                resizing = false;
                dragEdge = 0;
                Capture = false;
            };
            c.MouseUp += delegate
            {
                resizing = false;          // 帧循环会自己收尾
            };
            c.MouseLeave += delegate
            {
                if (resizing || inFrame || maximized) return;
                RestoreArrowCursor();
            };
        }

        private void ApplyDragResize()
        {
            if (dragEdge == 0) return;
            Point cur = Cursor.Position;
            int dx = cur.X - dragOrigin.X;
            int dy = cur.Y - dragOrigin.Y;
            Rectangle r = dragRect;
            int minW = S(660), minH = S(720);

            switch (dragEdge)
            {
                case Shell.HTLEFT:
                    r.X += dx; r.Width -= dx; break;
                case Shell.HTRIGHT:
                    r.Width += dx; break;
                case Shell.HTTOP:
                    r.Y += dy; r.Height -= dy; break;
                case Shell.HTBOTTOM:
                    r.Height += dy; break;
                case Shell.HTTOPLEFT:
                    r.X += dx; r.Y += dy; r.Width -= dx; r.Height -= dy; break;
                case Shell.HTTOPRIGHT:
                    r.Y += dy; r.Width += dx; r.Height -= dy; break;
                case Shell.HTBOTTOMLEFT:
                    r.X += dx; r.Width -= dx; r.Height += dy; break;
                case Shell.HTBOTTOMRIGHT:
                    r.Width += dx; r.Height += dy; break;
            }

            if (r.Width < minW)
            {
                if (dragEdge == Shell.HTLEFT || dragEdge == Shell.HTTOPLEFT || dragEdge == Shell.HTBOTTOMLEFT)
                    r.X = dragRect.Right - minW;
                r.Width = minW;
            }
            if (r.Height < minH)
            {
                if (dragEdge == Shell.HTTOP || dragEdge == Shell.HTTOPLEFT || dragEdge == Shell.HTTOPRIGHT)
                    r.Y = dragRect.Bottom - minH;
                r.Height = minH;
            }
            Bounds = r;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 从任务栏还原时，万一还停在收缩动画的尺寸上，把它拉回原尺寸
            if (!skipLayout && WindowState == FormWindowState.Normal && miniRestore.Width > 0)
            {
                if (Width < S(320) || Height < S(320))
                {
                    Rectangle r = miniRestore;
                    miniRestore = Rectangle.Empty;
                    Bounds = r;
                    return;
                }
                miniRestore = Rectangle.Empty;
            }
            LayoutAll();
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
            if (e.Button != MouseButtons.Left) return;
            if (maximized) return;    // 最大化时窗口不跟随拖动
            if (animating || inFrame) return;
            Control c = (Control)sender;
            Point p = PointToClient(c.PointToScreen(e.Location));
            if (HitEdge(p) != 0) return;      // 窗口边缘交给缩放
            Shell.ReleaseCapture();
            Shell.SendMessage(Handle, Shell.WM_NCLBUTTONDOWN, (IntPtr)Shell.HTCAPTION, IntPtr.Zero);
        }

        private void BuildHeader()
        {
            head = new Panel();
            head.SetBounds(0, 0, ClientSize.Width, 68);
            head.BackColor = Theme.Bg;
            head.MouseDown += DragWindow;
            Controls.Add(head);
            HookResize(head);

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

            // macOS 红绿灯：黄（最小化）· 绿（最大化/还原）· 红（关闭，最右）
            dots = new TrafficDots();
            dots.Location = new Point(ClientSize.Width - 160, 20);
            dots.OnDot = delegate(int idx)
            {
                HideTip();
                if (idx == 2) { Close(); return; }
                if (idx == 0) { MinimizeAnimated(); return; }
                ToggleMaximize();
            };
            dots.OnHover = delegate(int idx) { TipHover(idx); };
            head.Controls.Add(dots);

            tip = new TipBubble();
            Controls.Add(tip);
            tip.BringToFront();
            tipTimer = new Timer();
            tipTimer.Interval = 260;
            tipTimer.Tick += delegate { ShowTipForDot(); };
        }

        private void TipHover(int idx)
        {
            if (tipTimer == null) return;
            tipTimer.Stop();
            HideTip();
            tipIdx = idx;
            if (idx < 0) return;
            tipTimer.Start();
        }

        private void HideTip()
        {
            if (tip != null && tip.Visible) tip.Visible = false;
        }

        private void ShowTipForDot()
        {
            if (tipTimer != null) tipTimer.Stop();
            if (tipIdx < 0 || tip == null || dots == null) return;
            string text;
            if (tipIdx == 0) text = "最小化到任务栏";
            else if (tipIdx == 1) text = maximized ? "还原窗口" : "铺满屏幕";
            else text = "关闭窗口";
            Rectangle r = dots.DotRect(tipIdx);
            Point anchor = new Point(dots.Left + r.X + r.Width / 2, dots.Top + dots.Height + Theme.Px(2));
            tip.ShowTip(text, anchor, ClientSize.Width - Theme.Px(8));
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
            cardScheme = card;
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

            btnImport = new ModernButton("在线获取光标包", false);
            btnImport.SetBounds(18, 98, 124, 32);
            btnImport.Click += delegate { OpenOnlinePacks(); };
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

            btnCustom = new ModernButton("打开自定义文件夹", false);
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
            cardSize = card;
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
            slider.OnChanged = delegate(int v) { UpdateSizeLabel(); };
            slider.OnDragEnd = delegate(int v) { SkinHandForLevel(v); };
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
            lblHint = hint;

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
            cardPreview = card;
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
            skinnedPx = -1;
            ApplyAppCursors();
            StartWarmup();
            int packs = 0;
            foreach (Scheme s2 in schemes) if (s2.IsPack) packs++;
            SetStatus("已载入 " + schemes.Count + " 个方案（自定义 " + packs + " 个）。当前：" + (string.IsNullOrEmpty(current) ? "未知" : current));
        }

        private void ApplyAppCursors()
        {
            Cursor hc = AppCursors.Hand() ?? Cursors.Hand;
            Cursor ac = AppCursors.Arrow() ?? Cursors.Default;
            Cursor = ac;
            SkinCursors(this, hc);
        }

        private void SkinHandForLevel(int level)
        {
            int px = 32 + (level - 1) * 8;
            if (skinnedPx == px) return;      // 同一档位不重复重建、不重复遍历控件树
            skinnedPx = px;
            Cursor = AppCursors.ArrowForPx(px) ?? Cursors.Default;
            SkinCursors(this, AppCursors.HandForPx(px) ?? Cursors.Hand);
        }

        private void StartWarmup()
        {
            warmIdx = 1;
            if (warmTimer == null)
            {
                warmTimer = new Timer();
                warmTimer.Interval = 50;
                warmTimer.Tick += delegate
                {
                    if (slider == null || warmIdx > 15) { warmTimer.Stop(); return; }
                    int px = 32 + (warmIdx - 1) * 8;
                    AppCursors.ArrowForPx(px);
                    AppCursors.HandForPx(px);
                    warmIdx++;
                };
            }
            warmTimer.Stop();
            warmTimer.Start();
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
            UpdateActualLabel();
        }

        private void UpdateActualLabel()
        {
            int px = (slider != null ? slider.Value : 1);
            px = 32 + (px - 1) * 8;
            if (actualPx <= 0) lblActual.Text = "系统当前实际尺寸：读取失败（目标 " + px + " px）";
            else lblActual.Text = "系统当前实际尺寸：" + actualPx + " px（目标 " + px + " px）";
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
            actualPx = MeasureActual();
            UpdateActualLabel();
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
            // 不移动指针：按钮上的手形本身就是按当前档位重建的，尺寸与系统箭头一致，
            // 原地直接读 GetCursorInfo 即可，避免指针在屏幕上跳来跳去。
            int px = 0;
            for (int i = 0; i < 5; i++)
            {
                px = CursorWorks.GetCursorPixels();
                if (px > 0) break;
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
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
                SkinHandForLevel(slider.Value);
                actualPx = MeasureActual();
                UpdateSizeLabel();
                SetStatus("已按 " + slider.Value + " 档（约 " + (32 + (slider.Value - 1) * 8) + " px）重设 " + applied + " 个系统指针，立即生效。");
            }
            catch (Exception ex)
            {
                SetStatus("调整大小失败：" + ex.Message);
            }
        }

        private void OpenOnlinePacks()
        {
            using (OnlinePacksForm f = new OnlinePacksForm())
            {
                f.ShowDialog(this);
                if (!f.Installed) return;
                LoadAll();
                SelectSchemeByName(f.InstalledName);
                SetStatus("已在线安装「" + f.InstalledName + "」，已加入方案列表并选中。");
            }
        }

        private void SelectSchemeByName(string name)
        {
            if (string.IsNullOrEmpty(name) || schemes == null) return;
            for (int i = 0; i < schemes.Count; i++)
            {
                if (!string.Equals(schemes[i].Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                if (cmb.SelectedIndex == i) return;
                cmb.SelectedIndex = i;
                RefreshTiles(schemes[i], true);
                return;
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
            try
            {
                // GitHub 只走 TLS 1.2+，.NET 4.x 默认可能还是 SSL3/TLS1.0
                System.Net.ServicePointManager.SecurityProtocol =
                    (System.Net.SecurityProtocolType)3072 | (System.Net.SecurityProtocolType)768;
            }
            catch { }
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
