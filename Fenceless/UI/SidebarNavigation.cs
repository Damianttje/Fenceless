using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class SidebarNavigation : UserControl
    {
        private struct SidebarItem
        {
            public string Text;
            public string Icon;
            public Panel ItemPanel;
            public Label IconLabel;
            public Label TextLabel;
        }

        private List<SidebarItem> items = new List<SidebarItem>();
        private int selectedIndex = 0;
        private Timer hoverTimer;
        private int hoverIndex = -1;
        private Color hoverOriginalColor;

        public event EventHandler<int> PageChanged;

        public SidebarNavigation()
        {
            this.Dock = DockStyle.Left;
            this.Width = Theme.Sizes.SidebarWidth;
            this.BackColor = Theme.Colors.SidebarBackground;
            this.Padding = new Padding(0, 8, 0, 8);

            hoverTimer = new Timer { Interval = 16 };
            hoverTimer.Tick += HoverTimer_Tick;
        }

        public void AddItem(string text, string icon)
        {
            var item = new SidebarItem { Text = text, Icon = icon };

            item.ItemPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2),
                Cursor = Cursors.Hand
            };

            var accentBar = new Panel
            {
                Width = 3,
                Dock = DockStyle.Left,
                BackColor = Color.Transparent
            };

            item.IconLabel = new Label
            {
                Text = icon,
                Font = new Font(Theme.IconFontName, 12F),
                ForeColor = Theme.Colors.SidebarItemText,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(36, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left,
                Margin = new Padding(0, 0, 0, 0)
            };

            item.TextLabel = new Label
            {
                Text = text,
                Font = Theme.Fonts.Body,
                ForeColor = Theme.Colors.SidebarItemText,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 0, 0, 0),
                Margin = new Padding(0)
            };

            item.ItemPanel.Controls.Add(item.TextLabel);
            item.ItemPanel.Controls.Add(item.IconLabel);
            item.ItemPanel.Controls.Add(accentBar);

            int index = items.Count;
            AttachItemHandlers(item.ItemPanel, index);

            items.Add(item);
            this.Controls.Add(item.ItemPanel);

            if (items.Count == 1)
                UpdateSelection();
        }

        private void AttachItemHandlers(Panel itemPanel, int index)
        {
            itemPanel.Click += (s, e) => SelectPage(index);
            itemPanel.MouseEnter += (s, e) => OnItemHover(index);
            itemPanel.MouseLeave += (s, e) => OnItemLeave(index);

            foreach (Control child in itemPanel.Controls)
            {
                child.Click += (s, e) => SelectPage(index);
                child.MouseEnter += (s, e) => OnItemHover(index);
                child.MouseLeave += (s, e) => OnItemLeave(index);
            }
        }

        public void SelectPage(int index)
        {
            if (index < 0 || index >= items.Count) return;
            selectedIndex = index;
            UpdateSelection();
            PageChanged?.Invoke(this, index);
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bool isSelected = i == selectedIndex;

                item.ItemPanel.BackColor = isSelected ? Theme.Colors.SurfaceSelected : Color.Transparent;
                item.IconLabel.ForeColor = isSelected ? Theme.Colors.SidebarItemTextSelected : Theme.Colors.SidebarItemText;
                item.TextLabel.ForeColor = isSelected ? Theme.Colors.SidebarItemTextSelected : Theme.Colors.SidebarItemText;

                var accentBar = item.ItemPanel.Controls[0] as Panel;
                if (accentBar != null)
                    accentBar.BackColor = isSelected ? Theme.Colors.SidebarItemSelected : Color.Transparent;
            }
        }

        private void OnItemHover(int index)
        {
            if (index == selectedIndex) return;
            hoverIndex = index;
            hoverOriginalColor = items[index].ItemPanel.BackColor;
            hoverTimer.Start();
        }

        private void OnItemLeave(int index)
        {
            if (hoverIndex == index)
            {
                hoverTimer.Stop();
                if (index != selectedIndex)
                    items[index].ItemPanel.BackColor = Color.Transparent;
                hoverIndex = -1;
            }
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (hoverIndex < 0 || hoverIndex >= items.Count)
            {
                hoverTimer.Stop();
                return;
            }

            var target = Theme.Colors.SidebarItemHover;
            var current = items[hoverIndex].ItemPanel.BackColor;

            int r = Math.Min(current.R + 4, target.R);
            int g = Math.Min(current.G + 4, target.G);
            int b = Math.Min(current.B + 4, target.B);

            items[hoverIndex].ItemPanel.BackColor = Color.FromArgb(r, g, b);

            if (r >= target.R && g >= target.G && b >= target.B)
                hoverTimer.Stop();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            foreach (var item in items)
            {
                item.TextLabel.Font = Theme.Fonts.Body;
            }
        }
    }
}
