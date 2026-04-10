using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class ThemedForm : Form
    {
        private CustomTitleBar titleBar;
        private Panel bottomBorder;
        private bool chromeAdded;

        protected CustomTitleBar TitleBar => titleBar;

        public ThemedForm() : base()
        {
        }

        protected void SetupThemedForm(string title, bool showMinimize = true, bool showMaximize = true, bool sizable = true)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.MaximizeBox = showMaximize;
            this.MinimizeBox = showMinimize;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Theme.Colors.BackgroundMid;
            this.Font = Theme.Fonts.Body;

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
