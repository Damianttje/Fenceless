using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class ToggleSwitch : UserControl
    {
        private bool checkedValue;
        private Timer animationTimer;
        private double thumbPosition;
        private double targetThumbPosition;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get => checkedValue;
            set
            {
                if (checkedValue != value)
                {
                    checkedValue = value;
                    targetThumbPosition = value ? 1.0 : 0.0;
                    StartAnimation();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public ToggleSwitch()
        {
            this.Size = new Size(44, 22);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;

            animationTimer = new Timer { Interval = 16 };
            animationTimer.Tick += AnimationTimer_Tick;

            this.Click += (s, e) => Checked = !Checked;
            this.Paint += ToggleSwitch_Paint;
        }

        private void ToggleSwitch_Paint(object sender, PaintEventArgs e)
        {
            int trackHeight = 22;
            int trackWidth = 44;
            int cornerRadius = trackHeight / 2;

            Color trackColor = checkedValue ? Theme.Colors.Accent : Theme.Colors.Surface;
            Color trackBorderColor = checkedValue ? Theme.Colors.AccentHover : Theme.Colors.SurfaceBorder;

            if (!Enabled)
            {
                trackColor = Theme.Colors.BackgroundLight;
                trackBorderColor = Theme.Colors.SurfaceBorder;
            }

            using (var trackBrush = new SolidBrush(trackColor))
            using (var trackBorderPen = new Pen(trackBorderColor))
            using (var thumbBrush = new SolidBrush(Enabled ? Color.White : Theme.Colors.TextDisabled))
            {
                var trackRect = new Rectangle(0, 0, trackWidth, trackHeight);
                using (var trackPath = CreateRoundedRectPath(trackRect, cornerRadius))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                    e.Graphics.DrawPath(trackBorderPen, trackPath);
                }

                int thumbDiameter = 16;
                int thumbX = (int)(4 + thumbPosition * (trackWidth - thumbDiameter - 8));
                int thumbY = (trackHeight - thumbDiameter) / 2;

                var thumbRect = new Rectangle(thumbX, thumbY, thumbDiameter, thumbDiameter);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(thumbBrush, thumbRect);
            }
        }

        private void StartAnimation()
        {
            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            double speed = 0.15;
            double diff = targetThumbPosition - thumbPosition;

            if (Math.Abs(diff) < 0.01)
            {
                thumbPosition = targetThumbPosition;
                animationTimer.Stop();
            }
            else
            {
                thumbPosition += diff * speed;
            }

            this.Invalidate();
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            this.Invalidate();
        }
    }
}
