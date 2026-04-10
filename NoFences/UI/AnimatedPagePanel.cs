using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class AnimatedPagePanel : Panel
    {
        private Dictionary<string, Control> pages = new Dictionary<string, Control>();
        private string currentPageKey;

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
            if (key == currentPageKey || !pages.ContainsKey(key)) return;

            var oldPage = pages[currentPageKey];
            var newPage = pages[key];

            oldPage.Visible = false;
            newPage.Visible = true;
            currentPageKey = key;

            PageSwitched?.Invoke(this, EventArgs.Empty);
        }

        public Control GetCurrentPage()
        {
            if (currentPageKey != null && pages.ContainsKey(currentPageKey))
                return pages[currentPageKey];
            return null;
        }
    }
}
