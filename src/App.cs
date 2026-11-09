using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace iOS6Clock
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Native.SetProcessDPIAware();

            if (args.Length >= 2 && args[0] == "--shot")
            {
                float sc = 1f;
                if (args.Length >= 3)
                    float.TryParse(args[2], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out sc);
                bool dark = args.Length >= 4 && args[3] == "dark";
                Shot(args[1], sc, dark);
                return;
            }
            if (args.Length >= 1 && args[0] == "--info") { Info(args.Length >= 2 ? args[1] : "info.txt"); return; }

            bool created;
            using (var mutex = new Mutex(true, "iOS6Clock.SingleInstance", out created))
            {
                if (!created) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { TrayApp.Log(e.Exception); };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    TrayApp.Log(e.ExceptionObject as Exception ?? new Exception("unknown"));
                };
                using (var app = new TrayApp())
                    Application.Run(app);
            }
        }

        static void Shot(string path, float scale, bool dark)
        {
            try
            {
                using (var form = new ClockPopupForm(scale, ClockTheme.Get(dark)))
                {
                    IntPtr forced = form.Handle; // exercise window / region creation
                    using (Bitmap bmp = form.RenderBitmap())
                        bmp.Save(path, ImageFormat.Png);
                    GC.KeepAlive(forced);
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(path + ".error.txt", ex.ToString());
            }
        }

        static void Info(string path)
        {
            TaskbarInfo tb = TaskbarInfo.Get();
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("DPI=" + g.DpiX);
                sb.AppendLine("taskbar=" + tb.Rect.Left + "," + tb.Rect.Top + "," + tb.Rect.Right + "," + tb.Rect.Bottom);
                sb.AppendLine("edge=" + tb.Edge + " autohide=" + tb.AutoHide);
                Rectangle c = ClockLocator.Find(g.DpiX / 96f);
                sb.AppendLine("clock=" + c.Left + "," + c.Top + "," + c.Right + "," + c.Bottom);
                sb.AppendLine("state=" + Fullscreen.IsBusy());
                File.WriteAllText(path, sb.ToString());
            }
        }
    }

    internal sealed class TrayApp : ApplicationContext, IDisposable
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValue = "iOS6Clock";
        const string SettingsKey = @"Software\iOS6Clock";

        readonly NotifyIcon _icon;
        readonly ClockOverlayForm _overlay;
        readonly ClockPopupForm _popup;
        readonly System.Windows.Forms.Timer _watch;
        readonly float _scale;
        readonly ContextMenuStrip _menu;

        Rectangle _clockRect = Rectangle.Empty;
        bool _overlayShown;

        internal static void Log(Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iOS6Clock");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "error.log"),
                    DateTime.Now.ToString("s") + "  " + ex + Environment.NewLine);
            }
            catch { }
        }

        public TrayApp()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
                _scale = g.DpiX / 96f;

            _popup = new ClockPopupForm(_scale, ClockTheme.Get(IsDarkTheme()));
            _popup.ThemeToggled += delegate { SaveDarkTheme(_popup.IsDark); };

            _overlay = new ClockOverlayForm();
            _overlay.ClockClicked += delegate { OnClockClicked(); };
            _overlay.MenuRequested += delegate { ShowMenu(Cursor.Position); };
            _overlay.Show();

            _menu = BuildMenu();
            _icon = new NotifyIcon();
            _icon.Icon = IconFactory.Create(32);
            _icon.Text = "Часы iOS 6";
            _icon.Visible = true;
            _icon.ContextMenuStrip = _menu;
            _icon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) OnClockClicked();
            };

            _watch = new System.Windows.Forms.Timer();
            _watch.Interval = 400;
            _watch.Tick += delegate { UpdateOverlay(); };
            _watch.Start();
            UpdateOverlay();
        }

        ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;

            var open = new ToolStripMenuItem("Открыть часы");
            open.Click += delegate { OnClockClicked(); };
            menu.Items.Add(open);

            menu.Items.Add(new ToolStripSeparator());

            var theme = new ToolStripMenuItem("Тёмная тема");
            theme.Name = "theme";
            theme.CheckOnClick = true;
            theme.Checked = IsDarkTheme();
            theme.Click += delegate(object s, EventArgs e)
            {
                bool dark = ((ToolStripMenuItem)s).Checked;
                SaveDarkTheme(dark);
                _popup.SetTheme(ClockTheme.Get(dark));
                if (!Fullscreen.IsBusy())
                {
                    Rectangle r = _clockRect.Width > 0 ? _clockRect : ClockLocator.Find(_scale);
                    _popup.ShowAt(r);
                }
            };
            menu.Items.Add(theme);
            menu.Opening += delegate
            {
                var item = menu.Items["theme"] as ToolStripMenuItem;
                if (item != null) item.Checked = _popup.IsDark;
            };

            var auto = new ToolStripMenuItem("Запускать при входе");
            auto.CheckOnClick = true;
            auto.Checked = IsAutoStart();
            auto.Click += delegate(object s, EventArgs e)
            {
                SetAutoStart(((ToolStripMenuItem)s).Checked);
            };
            menu.Items.Add(auto);

            menu.Items.Add(new ToolStripSeparator());

            var exit = new ToolStripMenuItem("Выход");
            exit.Click += delegate { ExitThread(); };
            menu.Items.Add(exit);

            return menu;
        }

        void ShowMenu(Point p)
        {
            _menu.Show(p);
        }

        void UpdateOverlay()
        {
            TaskbarInfo tb = TaskbarInfo.Get();
            bool allowed = !tb.AutoHide && !Fullscreen.IsBusy();
            Rectangle r = allowed ? ClockLocator.Find(_scale) : Rectangle.Empty;

            if (!allowed || r.Width <= 0 || r.Height <= 0)
            {
                if (_overlayShown) { _overlay.Hide(); _overlayShown = false; }
                return;
            }

            if (!_overlayShown)
            {
                _overlay.Show();
                _overlayShown = true;
                _clockRect = Rectangle.Empty;
            }

            if (r != _clockRect)
            {
                _clockRect = r;
                _overlay.Place(r);
            }
            else
            {
                _overlay.RaiseTopmost();
            }
        }

        void OnClockClicked()
        {
            if (_popup.Visible)
            {
                _popup.Hide();
                return;
            }
            if ((DateTime.Now - _popup.LastHidden).TotalMilliseconds < 220)
                return;

            Rectangle r = _clockRect;
            if (r.Width <= 0) r = ClockLocator.Find(_scale);
            _popup.ShowAt(r);
        }

        static bool IsDarkTheme()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(SettingsKey))
                    return k != null && Convert.ToInt32(k.GetValue("Dark", 0)) == 1;
            }
            catch { return false; }
        }

        static void SaveDarkTheme(bool dark)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(SettingsKey))
                {
                    if (k != null) k.SetValue("Dark", dark ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        static bool IsAutoStart()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue(RunValue) != null;
            }
            catch { return false; }
        }

        static void SetAutoStart(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (k == null) return;
                    if (on) k.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue(RunValue, false);
                }
            }
            catch { }
        }

        public new void Dispose()
        {
            if (_watch != null) _watch.Dispose();
            if (_icon != null) { _icon.Visible = false; _icon.Dispose(); }
            if (_overlay != null) _overlay.Dispose();
            if (_popup != null) _popup.Dispose();
            base.Dispose();
        }
    }
}

