using System;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public static class AnimationHelper
    {
        public static void FadeIn(Form form, int durationMs = 200, Action onComplete = null)
        {
            AnimateOpacity(form, 0.0, 1.0, durationMs, onComplete);
        }

        public static void FadeOut(Form form, int durationMs = 200, Action onComplete = null)
        {
            AnimateOpacity(form, 1.0, 0.0, durationMs, onComplete);
        }

        public static void AnimateOpacity(Form form, double from, double to, int durationMs, Action onComplete = null)
        {
            if (durationMs <= 0)
            {
                form.Opacity = to;
                onComplete?.Invoke();
                return;
            }

            var timer = new Timer { Interval = 16 };
            double startValue = from;
            double range = to - from;
            int totalSteps = durationMs / 16;
            int step = 0;

            timer.Tick += (s, e) =>
            {
                step++;
                double progress = Math.Min((double)step / totalSteps, 1.0);
                double eased = EaseOutQuad(progress);
                form.Opacity = startValue + range * eased;

                if (step >= totalSteps)
                {
                    timer.Stop();
                    timer.Dispose();
                    form.Opacity = to;
                    onComplete?.Invoke();
                }
            };

            form.Opacity = from;
            timer.Start();
        }

        public static void LerpColor(Control control, Color from, Color to, int durationMs, Action onComplete = null)
        {
            if (durationMs <= 0)
            {
                control.BackColor = to;
                onComplete?.Invoke();
                return;
            }

            var timer = new Timer { Interval = 16 };
            int totalSteps = durationMs / 16;
            int step = 0;

            timer.Tick += (s, e) =>
            {
                step++;
                double progress = Math.Min((double)step / totalSteps, 1.0);
                double eased = EaseOutQuad(progress);

                int r = (int)(from.R + (to.R - from.R) * eased);
                int g = (int)(from.G + (to.G - from.G) * eased);
                int b = (int)(from.B + (to.B - from.B) * eased);

                control.BackColor = Color.FromArgb(r, g, b);

                if (step >= totalSteps)
                {
                    timer.Stop();
                    timer.Dispose();
                    control.BackColor = to;
                    onComplete?.Invoke();
                }
            };

            control.BackColor = from;
            timer.Start();
        }

        private static double EaseOutQuad(double t)
        {
            return t * (2.0 - t);
        }
    }
}
