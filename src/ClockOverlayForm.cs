using System;
using System.Drawing;
using System.Windows.Forms;

namespace iOS6Clock
{
    internal sealed class ClockOverlayForm : Form
    {
        public event EventHandler ClockClicked;
        public event EventHandler MenuRequested;

        public ClockOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Black;
            Size = new Size(8, 8);
            Location = new Point(-100, -100);
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 1/255 alpha: invisible, but the window still receives mouse input.
            Native.SetLayeredWindowAttributes(Handle, 0, 1, Native.LWA_ALPHA);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_LBUTTONUP)
            {
                if (ClockClicked != null) ClockClicked(this, EventArgs.Empty);
                return;
            }
            if (m.Msg == Native.WM_RBUTTONUP)
            {
                if (MenuRequested != null) MenuRequested(this, EventArgs.Empty);
                return;
            }
            base.WndProc(ref m);
        }

        public void Place(Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            Native.SetWindowPos(Handle, new IntPtr(-1), r.X, r.Y, r.Width, r.Height, 0x0010);
        }

        public void RaiseTopmost()
        {
            Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }
    }
}
