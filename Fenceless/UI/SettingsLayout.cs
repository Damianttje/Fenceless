using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// A single settings row: label (left column) + input (right column),
    /// with an optional description/caption line beneath the label.
    /// Fixed height computed from content; positioned manually by its parent
    /// (no docking, no pixel math in callers).
    /// </summary>
    public class SettingsRow : Panel
    {
        private readonly Label _label;
        private readonly Label _description;
        public Control Input { get; }

        public SettingsRow(string labelText, Control input, string description = null)
        {
            BackColor = Color.Transparent;
            Margin = new Padding(0);

            _label = new Label
            {
                Text = labelText,
                Font = Theme.Fonts.Body,
                ForeColor = Theme.Colors.TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = false,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_label);

            if (!string.IsNullOrEmpty(description))
            {
                _description = new Label
                {
                    Text = description,
                    Font = Theme.Fonts.Caption,
                    ForeColor = Theme.Colors.TextSecondary,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    Height = 16,
                    TextAlign = ContentAlignment.TopLeft
                };
                Controls.Add(_description);
            }

            Input = input;
            Controls.Add(input);

            int h = Theme.Sizes.RowHeight;
            if (_description != null) h += 18;
            Height = h;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionChildren();
        }

        private void PositionChildren()
        {
            int labelW = Theme.Sizes.LabelColumnWidth - 8;
            _label.Location = new Point(0, 7);
            _label.Width = labelW;

            int inputX = Theme.Sizes.LabelColumnWidth;
            if (_description != null)
            {
                _description.Location = new Point(0, 7 + _label.Height + 2);
                _description.Width = labelW;
            }

            int availableH = Height;
            int iy = Math.Max(0, (availableH - Input.Height) / 2);
            Input.Location = new Point(inputX, iy);
        }
    }

    /// <summary>
    /// A rounded Fluent card containing a header + stacked settings rows.
    /// Height is computed automatically from its children — callers never
    /// pass a row count. Deterministic manual layout (no docking-order issues).
    /// </summary>
    public class SettingsSection : RoundedPanel
    {
        private readonly Label _header;
        private readonly Panel _accent;
        private readonly List<Control> _rows = new List<Control>();
        private bool _inLayout;
        private const int TopPad = 8;
        private const int BottomPad = 14;
        private const int SidePad = 16;

        public SettingsSection(string title, int width)
        {
            BackColor = Theme.Colors.Surface;
            CornerRadius = Theme.Sizes.CardRadius;
            BorderColor = Theme.Colors.StrokeControl;
            Margin = new Padding(0, 0, 0, Theme.Sizes.SectionSpacing);
            Padding = new Padding(0);

            _header = new Label
            {
                Text = title,
                Font = Theme.Fonts.SectionHeader,
                ForeColor = Theme.Colors.TextBright,
                BackColor = Color.Transparent,
                AutoSize = false,
                Height = 24
            };
            _accent = new Panel
            {
                Size = new Size(28, 2),
                BackColor = Theme.Colors.SectionBorder
            };

            Controls.Add(_header);
            Controls.Add(_accent);

            // Set width last — this triggers layout, so fields must exist first.
            Width = width;
        }

        public void AddRow(Control row)
        {
            _rows.Add(row);
            Controls.Add(row);
        }

        public void AddRows(params Control[] rows)
        {
            foreach (var r in rows) AddRow(r);
        }

        public IEnumerable<Control> Rows => _rows;

        public void ClearRows()
        {
            foreach (var r in _rows)
            {
                Controls.Remove(r);
                r.Dispose();
            }
            _rows.Clear();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_inLayout || Width <= 0 || _header == null || _accent == null) return;
            _inLayout = true;
            try
            {
                int x = SidePad;
                int y = TopPad;
                _header.Location = new Point(x, y);
                _header.Width = Width - SidePad * 2;
                y += _header.Height + 2;
                _accent.Location = new Point(x, y);
                y += _accent.Height + 6;

                int contentW = Width - SidePad * 2;
                foreach (var r in _rows)
                {
                    if (!r.Visible) continue;
                    r.Location = new Point(x, y);
                    r.Width = contentW;
                    y += r.Height + r.Margin.Vertical;
                }

                int desired = y + BottomPad;
                if (desired != Height)
                    Height = desired;
            }
            finally { _inLayout = false; }
        }
    }

    /// <summary>
    /// A rounded Fluent card with a header and a docked content area.
    /// Unlike <see cref="SettingsSection"/> (which stacks rows), children
    /// added to <see cref="Content"/> dock within it — suitable for hosting
    /// arbitrary controls (lists, toolbars, etc.).
    /// </summary>
    public class CardPanel : RoundedPanel
    {
        private readonly Label _header;
        private readonly Panel _headerBar;
        private readonly Panel _content;

        public Panel Content => _content;

        public CardPanel(string title)
        {
            BackColor = Theme.Colors.Surface;
            CornerRadius = Theme.Sizes.CardRadius;
            BorderColor = Theme.Colors.StrokeControl;
            Margin = new Padding(0, 0, 0, Theme.Sizes.SectionSpacing);
            Padding = new Padding(0);

            _content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 4, 12, 12)
            };

            _headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.Transparent
            };

            _header = new Label
            {
                Text = title,
                Font = Theme.Fonts.SectionHeader,
                ForeColor = Theme.Colors.TextBright,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            var accent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = Theme.Colors.SectionBorder
            };

            _headerBar.Controls.Add(_header);
            _headerBar.Controls.Add(accent);

            Controls.Add(_content);
            Controls.Add(_headerBar);
        }
    }
}
