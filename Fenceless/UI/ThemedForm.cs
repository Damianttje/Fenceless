using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Fenceless.Properties;
using Fenceless.Win32;

namespace Fenceless.UI
{
    public class ThemedForm : Form
    {
        private CustomTitleBar titleBar;
        private Panel bottomBorder;
        private bool chromeAdded;
        private bool _desiredTopMost = true;

        protected CustomTitleBar TitleBar => titleBar;

        public ThemedForm() : base()
        {
        }

        protected void SetupThemedForm(string title, bool showMinimize = true, bool showMaximize = true, bool sizable = true, bool topMost = true, bool showInTaskbar = false)
        {
            _desiredTopMost = topMost;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.MaximizeBox = showMaximize;
            this.MinimizeBox = showMinimize;
            this.ShowInTaskbar = showInTaskbar;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Colors.BackgroundMid;
            this.Font = Theme.Fonts.Body;
            this.TopMost = topMost;

            try
            {
                using (var ms = new MemoryStream(Resources.AppIconIco))
                    this.Icon = new Icon(ms);
            }
            catch { }

            titleBar = new CustomTitleBar(this, title, showMinimize, showMaximize);

            bottomBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Theme.Colors.SurfaceBorder
            };
        }

        protected void BringChromeToFront()
        {
            if (!chromeAdded)
            {
                this.Controls.Add(titleBar);
                this.Controls.Add(bottomBorder);
                chromeAdded = true;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Opaque backdrop (mica: false) — Mica would render the desktop
            // wallpaper layer (which includes fence windows glued to WorkerW)
            // through the translucent client area, causing widget repaints to
            // flicker through the dialog. Dark title bar + rounded corners
            // are still applied.
            try { WindowUtil.ApplyFluentBackdrop(this.Handle, true, false); } catch { }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Re-assert TopMost after show: changing ShowInTaskbar / the form
            // being presented can recreate the HWND and drop the topmost flag,
            // letting other windows push the dialog to the back.
            if (_desiredTopMost)
            {
                this.TopMost = false;
                this.TopMost = true;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTCLIENT = 1;

            if (m.Msg == WM_NCHITTEST && WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                {
                    Point screenPoint = new Point(m.LParam.ToInt32());
                    Point clientPoint = PointToClient(screenPoint);

                    if (titleBar != null && clientPoint.Y < titleBar.Height)
                        return;

                    int resizeBorder = 6;
                    int rightEdge = ClientRectangle.Right;
                    int bottomEdge = ClientRectangle.Bottom;

                    if (clientPoint.Y >= bottomEdge - resizeBorder && clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x11; // HTBOTTOMRIGHT
                    else if (clientPoint.Y >= bottomEdge - resizeBorder && clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x10; // HTBOTTOMLEFT
                    else if (clientPoint.Y <= resizeBorder && clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x0E; // HTTOPRIGHT
                    else if (clientPoint.Y <= resizeBorder && clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x0D; // HTTOPLEFT
                    else if (clientPoint.Y >= bottomEdge - resizeBorder)
                        m.Result = (IntPtr)0x0F; // HTBOTTOM
                    else if (clientPoint.X >= rightEdge - resizeBorder)
                        m.Result = (IntPtr)0x0B; // HTRIGHT
                    else if (clientPoint.X <= resizeBorder)
                        m.Result = (IntPtr)0x0A; // HTLEFT
                }
                return;
            }
            base.WndProc(ref m);
        }
    }
}
