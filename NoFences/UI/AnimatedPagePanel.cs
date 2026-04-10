using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Fenceless.Model;
using Fenceless.Util;

namespace Fenceless.UI
{
    public class AnimatedPagePanel : Panel
    {
        private Dictionary<string, Control> pages = new Dictionary<string, Control>();
        private string currentPageKey;
        private bool isTransitioning = false;

        public event EventHandler PageSwitched;

        public void AddPage(string key, Control page)
        {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            pages[key] = page;
            this.Controls.Add(page);

            if (pages.Count == 1)
            {
                currentPageKey = key;
                page.Visible = true;
            }
        }

        public void SwitchTo(string key)
        {
            if (key == currentPageKey || !pages.ContainsKey(key) || isTransitioning) return;

            var oldPage = pages[currentPageKey];
            var newPage = pages[key];

            if (AppSettings.Instance.EnableAnimations && oldPage != null && newPage != null)
            {
                isTransitioning = true;
                newPage.Visible = true;
                newPage.BringToFront();

                int slideDistance = 20;
                int originalX = newPage.Location.X;
                newPage.Location = new Point(originalX + slideDistance, newPage.Location.Y);

                var timer = new Timer { Interval = 16 };
                int steps = 8;
                int step = 0;

                timer.Tick += (s, e) =>
                {
                    step++;
                    double progress = (double)step / steps;
                    double eased = progress * (2.0 - progress);
                    int offset = (int)(slideDistance * (1.0 - eased));
                    newPage.Location = new Point(originalX + offset, newPage.Location.Y);

                    if (step >= steps)
                    {
                        timer.Stop();
                        timer.Dispose();
                        newPage.Location = new Point(originalX, newPage.Location.Y);
                        oldPage.Visible = false;
                        currentPageKey = key;
                        isTransitioning = false;
                        PageSwitched?.Invoke(this, EventArgs.Empty);
                    }
                };

                timer.Start();
            }
            else
            {
                oldPage.Visible = false;
                newPage.Visible = true;
                currentPageKey = key;
                PageSwitched?.Invoke(this, EventArgs.Empty);
            }
        }

        public Control GetCurrentPage()
        {
            if (currentPageKey != null && pages.ContainsKey(currentPageKey))
                return pages[currentPageKey];
            return null;
        }
    }
}
