using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Fenceless.UI
{
    /// <summary>
    /// A Fluent-style color picker dialog: hex input, alpha slider,
    /// preset palette, and recent colors. Returns the chosen Color + alpha.
    /// </summary>
    public class ColorPickerDialog : ThemedForm
    {
        private Color _rgbColor;
        private int _alphaPercent;

        private ColorPickerButton _previewSwatch;
        private TextBox _hexBox;
        private TrackBar _alphaTrack;
        private Label _alphaLabel;
        private FlowLayoutPanel _presetFlow;
        private FlowLayoutPanel _recentFlow;

        private static readonly List<Color> _recentColors = new List<Color>();
        private static readonly Color[] _presets =
        {
            Color.FromArgb(0,0,0), Color.FromArgb(64,64,64), Color.FromArgb(128,128,128), Color.FromArgb(200,200,200), Color.FromArgb(255,255,255),
            Color.FromArgb(0,120,212), Color.FromArgb(0,153,76), Color.FromArgb(95,175,80), Color.FromArgb(210,167,0), Color.FromArgb(232,17,35),
            Color.FromArgb(120,16,180), Color.FromArgb(22,80,120), Color.FromArgb(40,40,40), Color.FromArgb(60,30,30), Color.FromArgb(30,50,40)
        };

        public Color SelectedColor => _rgbColor;
        public int SelectedAlphaPercent => _alphaPercent;

        public ColorPickerDialog(Color initialColor, int alphaPercent)
        {
            _rgbColor = Color.FromArgb(initialColor.R, initialColor.G, initialColor.B); // drop any incoming alpha
            _alphaPercent = Math.Max(0, Math.Min(100, alphaPercent));
            InitializeComponent();
            UpdatePreview();
            PopulateRecent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            SetupThemedForm("Choose Color", showMinimize: false, showMaximize: false, sizable: false);
            this.ClientSize = new Size(340, 460);
            this.StartPosition = FormStartPosition.CenterParent;

            var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 16), BackColor = Theme.Colors.BackgroundMid };

            var previewCaption = Theme.CreateLabel("Preview", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            previewCaption.Dock = DockStyle.Top;
            _previewSwatch = new ColorPickerButton { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 2, 0, 8) };

            var hexCaption = Theme.CreateLabel("Hex", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            hexCaption.Dock = DockStyle.Top;
            _hexBox = Theme.CreateTextBox();
            _hexBox.Dock = DockStyle.Top;
            _hexBox.Height = Theme.Sizes.InputHeight;
            _hexBox.TextChanged += HexBox_TextChanged;

            var alphaCaption = Theme.CreateLabel("Opacity", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            alphaCaption.Dock = DockStyle.Top;
            _alphaTrack = new TrackBar
            {
                Dock = DockStyle.Top,
                Minimum = 0, Maximum = 100, Value = _alphaPercent,
                TickStyle = TickStyle.None, Height = 28
            };
            _alphaLabel = Theme.CreateLabel($"{_alphaPercent}%", Theme.Fonts.Small, Theme.Colors.TextSecondary);
            _alphaLabel.Dock = DockStyle.Top;
            _alphaLabel.TextAlign = ContentAlignment.MiddleRight;
            _alphaTrack.ValueChanged += (s, e) =>
            {
                _alphaPercent = _alphaTrack.Value;
                _alphaLabel.Text = $"{_alphaPercent}%";
                UpdatePreview();
            };

            var presetCaption = Theme.CreateLabel("Presets", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            presetCaption.Dock = DockStyle.Top;
            presetCaption.Margin = new Padding(0, 8, 0, 2);
            _presetFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, WrapContents = true, BackColor = Color.Transparent };
            foreach (var p in _presets)
            {
                var sw = MakeSwatch(p, false);
                _presetFlow.Controls.Add(sw);
            }

            var recentCaption = Theme.CreateLabel("Recent", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            recentCaption.Dock = DockStyle.Top;
            recentCaption.Margin = new Padding(0, 8, 0, 2);
            _recentFlow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, WrapContents = true, BackColor = Color.Transparent };

            // Stack from bottom up so Dock=Top ordering is correct
            var footer = BuildFooter();
            content.Controls.Add(footer);
            content.Controls.Add(_recentFlow);
            content.Controls.Add(recentCaption);
            content.Controls.Add(_presetFlow);
            content.Controls.Add(presetCaption);
            content.Controls.Add(_alphaLabel);
            content.Controls.Add(_alphaTrack);
            content.Controls.Add(alphaCaption);
            content.Controls.Add(_hexBox);
            content.Controls.Add(hexCaption);
            content.Controls.Add(_previewSwatch);
            content.Controls.Add(previewCaption);

            this.Controls.Add(content);
            BringChromeToFront();
            this.AcceptButton = (Button)footer.Controls.Find("btnOK", true)[0];
            this.CancelButton = (Button)footer.Controls.Find("btnCancel", true)[0];

            this.ResumeLayout(false);
        }

        private Panel BuildFooter()
        {
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Theme.Colors.BackgroundDark, Padding = new Padding(12, 6, 12, 6) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
            var btnCancel = Theme.CreateFlatButton("Cancel");
            btnCancel.Name = "btnCancel";
            btnCancel.DialogResult = DialogResult.Cancel;
            var btnOK = Theme.CreateFlatButton("OK", Theme.ButtonRole.Accent);
            btnOK.Name = "btnOK";
            btnOK.DialogResult = DialogResult.OK;
            flow.Controls.Add(btnCancel);
            flow.Controls.Add(btnOK);
            footer.Controls.Add(flow);
            return footer;
        }

        private Control MakeSwatch(Color c, bool isRecent)
        {
            var btn = new Button
            {
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = c,
                Cursor = Cursors.Hand,
                Margin = new Padding(2)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.Colors.StrokeControl;
            btn.Click += (s, e) =>
            {
                _rgbColor = c;
                UpdatePreview();
            };
            return btn;
        }

        private void PopulateRecent()
        {
            _recentFlow.Controls.Clear();
            if (_recentColors.Count == 0)
            {
                var empty = Theme.CreateLabel("No recent colors", Theme.Fonts.Caption, Theme.Colors.TextDisabled);
                empty.AutoSize = true;
                _recentFlow.Controls.Add(empty);
                return;
            }
            foreach (var c in _recentColors)
                _recentFlow.Controls.Add(MakeSwatch(c, true));
        }

        private void HexBox_TextChanged(object sender, EventArgs e)
        {
            var text = _hexBox.Text.Trim();
            if (text.StartsWith("#")) text = text.Substring(1);
            if (text.Length == 6 && TryParseHex(text, out Color c))
            {
                _rgbColor = c;
                UpdatePreview(skipHex: true);
            }
        }

        private static bool TryParseHex(string hex, out Color c)
        {
            try
            {
                int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                c = Color.FromArgb(r, g, b);
                return true;
            }
            catch
            {
                c = Color.Empty;
                return false;
            }
        }

        private void UpdatePreview(bool skipHex = false)
        {
            _previewSwatch.BackColor = _rgbColor;
            _previewSwatch.AlphaPercent = _alphaPercent;
            _previewSwatch.Invalidate();
            if (!skipHex) _hexBox.Text = $"{_rgbColor.R:X2}{_rgbColor.G:X2}{_rgbColor.B:X2}";
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (this.DialogResult == DialogResult.OK)
            {
                if (!_recentColors.Contains(_rgbColor))
                {
                    _recentColors.Insert(0, _rgbColor);
                    if (_recentColors.Count > 10) _recentColors.RemoveAt(_recentColors.Count - 1);
                }
            }
        }
    }
}
