using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public static class AnimationHelper
    {
        private static readonly Dictionary<Form, Timer> _opacityAnimations = new Dictionary<Form, Timer>();
        private static readonly Dictionary<Control, Timer> _colorAnimations = new Dictionary<Control, Timer>();
        private static readonly object _lock = new object();

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

            CancelOpacityAnimation(form);

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
                    lock (_lock) { _opacityAnimations.Remove(form); }
                    form.Opacity = to;
                    onComplete?.Invoke();
                }
            };

            lock (_lock) { _opacityAnimations[form] = timer; }
            form.Opacity = from;
            timer.Start();
        }

        public static void CancelOpacityAnimation(Form form)
        {
            lock (_lock)
            {
                if (_opacityAnimations.TryGetValue(form, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _opacityAnimations.Remove(form);
                }
            }
        }

        public static void LerpColor(Control control, Color from, Color to, int durationMs, Action onComplete = null)
        {
            if (durationMs <= 0)
            {
                control.BackColor = to;
                onComplete?.Invoke();
                return;
            }

            CancelColorAnimation(control);

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
                    lock (_lock) { _colorAnimations.Remove(control); }
                    control.BackColor = to;
                    onComplete?.Invoke();
                }
            };

            lock (_lock) { _colorAnimations[control] = timer; }
            control.BackColor = from;
            timer.Start();
        }

        public static void CancelColorAnimation(Control control)
        {
            lock (_lock)
            {
                if (_colorAnimations.TryGetValue(control, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _colorAnimations.Remove(control);
                }
            }
        }

        private static double EaseOutQuad(double t)
        {
            return t * (2.0 - t);
        }
    }
}
