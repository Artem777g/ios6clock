using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace iOS6Clock
{
    internal sealed class ClockPopupForm : Form
    {
        const int BaseW = 376;
        const int BaseH = 420;

        readonly float _scale;
        readonly Timer _timer;
        readonly Font _dateFont;
        readonly Font _linkFont;
        readonly CultureInfo _ru = new CultureInfo("ru-RU");

        Rectangle _clockRect;
        Rectangle _dateRect;
        Rectangle _themeLinkRect;
        Rectangle _timeLinkRect;
        int _lineY;
        int _lineLeft, _lineRight;

        int _hover = -1;      // -1 none, 0 theme, 1 time settings
        bool _mouseWasDown;
        int _bitmapSize;
        string _bitmapSuffix;
        string _lastDate;
        Bitmap _face, _hour, _minute, _second, _shadow;
        ClockTheme _theme;

        public DateTime LastHidden { get; private set; }

        public event EventHandler ThemeToggled;

        public bool IsDark { get { return _theme.Dark; } }

        public ClockPopupForm(float scale, ClockTheme theme)
        {
            _scale = scale;
            _theme = theme;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = theme.Bottom;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            ClientSize = new Size(S(BaseW), S(BaseH));
            _dateFont = Fonts.Light(17f);
            _linkFont = Fonts.Regular(10f);
            _timer = new Timer();
            _timer.Interval = 33;
            _timer.Tick += delegate
            {
                if (!Visible) return;

                bool down = (Native.GetAsyncKeyState(0x01) & 0x8000) != 0 ||
                            (Native.GetAsyncKeyState(0x02) & 0x8000) != 0;
                if (down && !_mouseWasDown)
                {
                    Native.POINT p;
                    if (Native.GetCursorPos(out p) && !Bounds.Contains(p.X, p.Y))
                    {
                        Hide();
                        return;
                    }
                }
                _mouseWasDown = down;

                string d = DateTime.Now.ToString("dddd, d MMMM yyyy", _ru);
                if (d != _lastDate) { _lastDate = d; Invalidate(); }
                else Invalidate(_clockRect);
            };
            ApplyLayout();
        }

        public void SetTheme(ClockTheme theme)
        {
            _theme = theme;
            BackColor = theme.Bottom;
            _bitmapSize = 0;
            _bitmapSuffix = null;
            Invalidate();
        }

        void ToggleTheme()
        {
            SetTheme(ClockTheme.Get(!_theme.Dark));
            if (ThemeToggled != null) ThemeToggled(this, EventArgs.Empty);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        int S(float v) { return (int)Math.Round(v * _scale); }

        void ApplyLayout()
        {
            int w = ClientSize.Width;
            _clockRect = new Rectangle(S(48), S(14), S(280), S(280));
            _dateRect = new Rectangle(0, S(304), w, S(36));
            _lineY = S(357);
            _lineLeft = S(44);
            _lineRight = w - S(44);
            _themeLinkRect = new Rectangle(S(44), S(364), S(124), S(32));
            _timeLinkRect = new Rectangle(w - S(44) - S(164), S(364), S(164), S(32));
        }

        void ApplyRegion()
        {
            using (var path = Rounded(ClientRectangle, S(7)))
                Region = new Region(path);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRegion();
            ApplyLayout();
        }

        static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        void EnsureBitmaps()
        {
            int size = _clockRect.Width;
            if (_bitmapSize == size && _bitmapSuffix == _theme.Suffix) return;
            _bitmapSize = size;
            _bitmapSuffix = _theme.Suffix;
            Assets.ClearScaled();
            _shadow = Assets.Get("shadow.png", size);
            _face = Assets.Get("face" + _theme.Suffix + ".png", size);
            _hour = Assets.Get("hour" + _theme.Suffix + ".png", size);
            _minute = Assets.Get("minute" + _theme.Suffix + ".png", size);
            _second = Assets.Get("second" + _theme.Suffix + ".png", size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render(e.Graphics);
        }

        public Bitmap RenderBitmap()
        {
            var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            bmp.SetResolution(96f * _scale, 96f * _scale);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(_theme.Bottom);
                Render(g);
            }
            return bmp;
        }

        void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var path = Rounded(new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1), S(7)))
            {
                using (var b = new LinearGradientBrush(
                    new Rectangle(0, 0, ClientSize.Width, ClientSize.Height),
                    _theme.Top, _theme.Bottom, 90f))
                    g.FillPath(b, path);
                using (var p = new Pen(_theme.Border, Math.Max(1f, _scale)))
                    g.DrawPath(p, path);
            }

            DrawClock(g);
            DrawText(g);
        }

        void DrawClock(Graphics g)
        {
            EnsureBitmaps();
            Rectangle r = _clockRect;
            if (!_theme.Dark)
                g.DrawImage(_shadow, r);
            g.DrawImage(_face, r);

            DateTime t = DateTime.Now;
            float hourAngle = (t.Hour % 12) * 30f + t.Minute * 0.5f + t.Second * (0.5f / 60f);
            float minuteAngle = t.Minute * 6f + t.Second * 0.1f + t.Millisecond * 0.0001f;
            float secondAngle = t.Second * 6f + t.Millisecond * 0.006f;

            float cx = r.X + r.Width / 2f;
            float cy = r.Y + r.Height / 2f;
            DrawHand(g, _hour, cx, cy, r.Width, hourAngle);
            DrawHand(g, _minute, cx, cy, r.Width, minuteAngle);
            DrawHand(g, _second, cx, cy, r.Width, secondAngle);
        }

        void DrawHand(Graphics g, Bitmap img, float cx, float cy, int size, float angle)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(angle);
            g.DrawImage(img, new RectangleF(-size / 2f, -size / 2f, size, size));
            g.Restore(state);
        }

        void DrawText(Graphics g)
        {
            string date = DateTime.Now.ToString("dddd, d MMMM yyyy", _ru);
            _lastDate = date;
            using (var fmt = new StringFormat())
            using (var brush = new SolidBrush(_theme.Text))
            {
                fmt.Alignment = StringAlignment.Center;
                fmt.LineAlignment = StringAlignment.Center;
                g.DrawString(date, _dateFont, brush, _dateRect, fmt);
            }

            using (var pen = new Pen(_theme.Separator, Math.Max(1f, _scale)))
                g.DrawLine(pen, _lineLeft, _lineY, _lineRight, _lineY);

            string themeText = _theme.Dark ? "Светлая тема" : "Тёмная тема";
            string timeText = "Изменить дату и время…";

            using (var fmt = new StringFormat())
            {
                fmt.LineAlignment = StringAlignment.Center;
                fmt.Alignment = StringAlignment.Near;
                using (var brush = new SolidBrush(_hover == 0 ? _theme.LinkHover : _theme.Link))
                    g.DrawString(themeText, _linkFont, brush, _themeLinkRect, fmt);

                fmt.Alignment = StringAlignment.Far;
                using (var brush = new SolidBrush(_hover == 1 ? _theme.LinkHover : _theme.Link))
                    g.DrawString(timeText, _linkFont, brush, _timeLinkRect, fmt);
            }

            if (_hover >= 0)
            {
                Rectangle r = _hover == 0 ? _themeLinkRect : _timeLinkRect;
                string text = _hover == 0 ? themeText : timeText;
                SizeF sz = g.MeasureString(text, _linkFont);
                float x = _hover == 0 ? r.X : r.Right - sz.Width;
                float y = r.Y + (r.Height + sz.Height) / 2f - S(2);
                using (var pen = new Pen(_theme.Link, Math.Max(1f, _scale)))
                    g.DrawLine(pen, x, y, x + sz.Width, y);
            }
        }

        int HitLink(Point p)
        {
            if (_themeLinkRect.Contains(p)) return 0;
            if (_timeLinkRect.Contains(p)) return 1;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitLink(e.Location);
            if (h != _hover)
            {
                _hover = h;
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate(new Rectangle(0, S(360), ClientSize.Width, S(40)));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hover != -1)
            {
                _hover = -1;
                Cursor = Cursors.Default;
                Invalidate(new Rectangle(0, S(360), ClientSize.Width, S(40)));
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int h = HitLink(e.Location);
                if (h == 0)
                {
                    ToggleTheme();
                    return;
                }
                if (h == 1)
                {
                    try { System.Diagnostics.Process.Start("control.exe", "timedate.cpl"); }
                    catch { }
                    Hide();
                    return;
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { Hide(); return; }
            base.OnKeyDown(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                LastHidden = DateTime.Now;
            }
        }

        public void ShowAt(Rectangle clockRect)
        {
            if (clockRect.Width <= 0 || clockRect.Height <= 0)
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                clockRect = new Rectangle(wa.Right - S(96), wa.Bottom - S(40), S(88), S(40));
            }
            TaskbarInfo tb = TaskbarInfo.Get();
            Screen scr = Screen.FromRectangle(clockRect);
            Rectangle b = scr.Bounds;
            int gap = S(8);
            int x, y;

            if (tb.Edge == 3) { x = clockRect.Right - Width + S(10); y = clockRect.Top - Height - gap; }
            else if (tb.Edge == 1) { x = clockRect.Right - Width + S(10); y = clockRect.Bottom + gap; }
            else if (tb.Edge == 0) { x = clockRect.Right + gap; y = clockRect.Bottom - Height; }
            else { x = clockRect.Left - Width - gap; y = clockRect.Bottom - Height; }

            x = Math.Max(b.Left + S(4), Math.Min(x, b.Right - Width - S(4)));
            y = Math.Max(b.Top + S(4), Math.Min(y, b.Bottom - Height - S(4)));

            Location = new Point(x, y);
            if (!Visible) Show();
            Activate();
            BringToFront();
        }
    }
}
