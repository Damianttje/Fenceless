using Fenceless.Model;
using Fenceless.Util;
using Fenceless.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public partial class SettingsForm : Form
    {
        private readonly Logger logger;
        private List<FenceInfo> fenceInfos;
        private FenceInfo selectedFenceInfo;
        private bool isUpdatingControls = false;

        public SettingsForm()
        {
            logger = Logger.Instance;
            logger.Debug("Creating settings form", "SettingsForm");

            InitializeComponent();
            LoadSettings();
            LoadFences();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form setup
            this.Text = "Fenceless Settings";
            this.Size = new Size(1200, 700);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;
            this.MinimumSize = new Size(800, 600);

            CreateControls();
            SetupEventHandlers();

            this.ResumeLayout(false);
        }

        #region Control Declarations

        // Main layout
        private TabControl mainTabControl;
        private TabPage globalTab;
        private TabPage defaultsTab;
        private TabPage fencesTab;

        // Global Settings Controls
        private CheckBox chkAutoSave;
        private NumericUpDown nudAutoSaveInterval;
        private CheckBox chkShowTooltips;
        private CheckBox chkEnableAnimations;
        private CheckBox chkStartWithWindows;
        private ComboBox cmbLogLevel;
        private CheckBox chkEnableFileLogging;
        private TextBox txtToggleTransparencyShortcut;
        private TextBox txtToggleAutoHideShortcut;
        private TextBox txtShowAllFencesShortcut;
        private TextBox txtCreateNewFenceShortcut;
        private TextBox txtOpenSettingsShortcut;
        private TextBox txtToggleLockShortcut;
        private TextBox txtMinimizeAllFencesShortcut;
        private TextBox txtRefreshFencesShortcut;

        // Default Settings Controls
        private NumericUpDown nudDefaultFenceWidth;
        private NumericUpDown nudDefaultFenceHeight;
        private NumericUpDown nudDefaultTitleHeight;
        private NumericUpDown nudDefaultTransparency;
        private CheckBox chkDefaultAutoHide;
        private NumericUpDown nudDefaultAutoHideDelay;
        private Button btnDefaultBackgroundColor;
        private NumericUpDown nudDefaultBackgroundTransparency;
        private Button btnDefaultTitleBackgroundColor;
        private NumericUpDown nudDefaultTitleBackgroundTransparency;
        private Button btnDefaultTextColor;
        private NumericUpDown nudDefaultTextTransparency;
        private Button btnDefaultBorderColor;
        private NumericUpDown nudDefaultBorderTransparency;
        private NumericUpDown nudDefaultBorderWidth;
        private NumericUpDown nudDefaultCornerRadius;
        private CheckBox chkDefaultShowShadow;
        private ComboBox cmbDefaultIconSize;
        private NumericUpDown nudDefaultItemSpacing;

        // Fence Management Controls
        private ListBox lstFences;
        private GroupBox grpFenceSettings;
        private TextBox txtFenceName;
        private NumericUpDown nudFenceTransparency;
        private CheckBox chkFenceAutoHide;
        private NumericUpDown nudFenceAutoHideDelay;
        private CheckBox chkFenceLocked;
        private CheckBox chkFenceCanMinify;
        private NumericUpDown nudFenceWidth;
        private NumericUpDown nudFenceHeight;
        private NumericUpDown nudFenceTitleHeight;
        private Button btnFenceBackgroundColor;
        private NumericUpDown nudFenceBackgroundTransparency;
        private Button btnFenceTitleBackgroundColor;
        private NumericUpDown nudFenceTitleBackgroundTransparency;
        private Button btnFenceTextColor;
        private NumericUpDown nudFenceTextTransparency;
        private Button btnFenceBorderColor;
        private NumericUpDown nudFenceBorderTransparency;
        private NumericUpDown nudFenceBorderWidth;
        private NumericUpDown nudFenceCornerRadius;
        private CheckBox chkFenceShowShadow;
        private ComboBox cmbFenceIconSize;
        private NumericUpDown nudFenceItemSpacing;

        // Buttons
        private Button btnOK;
        private Button btnCancel;
        private Button btnApply;
        private Button btnRefreshFences;
        private Button btnHighlightFence;
        private Button btnAddFence;
        private Button btnResetToDefaults;
        private Button btnSetAsDefaults;

        #endregion

        private void CreateControls()
        {
            // Create main tab control
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            // Create tabs
            globalTab = new TabPage("Global Settings")
            {
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            defaultsTab = new TabPage("Default Settings")
            {
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            fencesTab = new TabPage("Fence Management")
            {
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220)
            };

            mainTabControl.TabPages.AddRange(new[] { globalTab, defaultsTab, fencesTab });

            CreateGlobalSettingsTab();
            CreateDefaultSettingsTab();
            CreateFenceManagementTab();
            CreateButtonPanel();

            this.Controls.Add(mainTabControl);
        }

        private void CreateGlobalSettingsTab()
        {
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(60, 63, 65)
            };

            // Application Settings Group
            var grpApp = new GroupBox
            {
                Text = "Application Settings",
                Location = new Point(10, 10),
                Size = new Size(450, 150),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            chkAutoSave = new CheckBox
            {
                Text = "Auto Save",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            var lblAutoSaveInterval = new Label
            {
                Text = "Auto Save Interval (seconds):",
                Location = new Point(10, 55),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            nudAutoSaveInterval = new NumericUpDown
            {
                Location = new Point(200, 52),
                Width = 100,
                Minimum = 5,
                Maximum = 300,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                BorderStyle = BorderStyle.FixedSingle
            };

            chkShowTooltips = new CheckBox
            {
                Text = "Show Tooltips",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            chkEnableAnimations = new CheckBox
            {
                Text = "Enable Animations",
                Location = new Point(10, 115),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            chkStartWithWindows = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(200, 25),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            grpApp.Controls.AddRange(new Control[] {
                chkAutoSave, lblAutoSaveInterval, nudAutoSaveInterval,
                chkShowTooltips, chkEnableAnimations, chkStartWithWindows
            });

            // Logging Settings Group
            var grpLogging = new GroupBox
            {
                Text = "Logging Settings",
                Location = new Point(10, 170),
                Size = new Size(450, 100),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            var lblLogLevel = new Label
            {
                Text = "Log Level:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            cmbLogLevel = new ComboBox
            {
                Location = new Point(100, 22),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                FlatStyle = FlatStyle.Flat
            };
            cmbLogLevel.Items.AddRange(new[] { "Debug", "Info", "Warning", "Error", "Critical" });

            chkEnableFileLogging = new CheckBox
            {
                Text = "Enable File Logging",
                Location = new Point(10, 55),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            grpLogging.Controls.AddRange(new Control[] {
                lblLogLevel, cmbLogLevel, chkEnableFileLogging
            });

            // Global Hotkeys Group
            var grpHotkeys = new GroupBox
            {
                Text = "Global Hotkeys",
                Location = new Point(480, 10),
                Size = new Size(400, 260),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateHotkeyControls(grpHotkeys);

            scrollPanel.Controls.AddRange(new Control[] { grpApp, grpLogging, grpHotkeys });
            globalTab.Controls.Add(scrollPanel);
        }

        private void CreateHotkeyControls(GroupBox parent)
        {
            var hotkeys = new[]
            {
                ("Toggle Transparency:", "txtToggleTransparencyShortcut"),
                ("Toggle Auto-Hide:", "txtToggleAutoHideShortcut"),
                ("Show All Fences:", "txtShowAllFencesShortcut"),
                ("Create New Fence:", "txtCreateNewFenceShortcut"),
                ("Open Settings:", "txtOpenSettingsShortcut"),
                ("Toggle Lock:", "txtToggleLockShortcut"),
                ("Minimize All Fences:", "txtMinimizeAllFencesShortcut"),
                ("Refresh Fences:", "txtRefreshFencesShortcut")
            };

            int yPos = 25;
            foreach (var (label, controlName) in hotkeys)
            {
                var lbl = new Label
                {
                    Text = label,
                    Location = new Point(10, yPos + 3),
                    Size = new Size(150, 20),
                    ForeColor = Color.FromArgb(220, 220, 220),
                    BackColor = Color.Transparent
                };

                var txt = new TextBox
                {
                    Location = new Point(170, yPos),
                    Width = 150,
                    ReadOnly = true,
                    ForeColor = Color.FromArgb(220, 220, 220),
                    BackColor = Color.FromArgb(50, 53, 55),
                    BorderStyle = BorderStyle.FixedSingle,
                    Text = ""
                };

                var btnClear = new Button
                {
                    Text = "Clear",
                    Location = new Point(330, yPos),
                    Size = new Size(50, 23),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(70, 73, 75),
                    ForeColor = Color.FromArgb(220, 220, 220)
                };
                btnClear.FlatAppearance.BorderSize = 1;
                btnClear.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

                btnClear.Click += (s, e) => txt.Text = "";

                // Assign to the appropriate field
                switch (controlName)
                {
                    case "txtToggleTransparencyShortcut": txtToggleTransparencyShortcut = txt; break;
                    case "txtToggleAutoHideShortcut": txtToggleAutoHideShortcut = txt; break;
                    case "txtShowAllFencesShortcut": txtShowAllFencesShortcut = txt; break;
                    case "txtCreateNewFenceShortcut": txtCreateNewFenceShortcut = txt; break;
                    case "txtOpenSettingsShortcut": txtOpenSettingsShortcut = txt; break;
                    case "txtToggleLockShortcut": txtToggleLockShortcut = txt; break;
                    case "txtMinimizeAllFencesShortcut": txtMinimizeAllFencesShortcut = txt; break;
                    case "txtRefreshFencesShortcut": txtRefreshFencesShortcut = txt; break;
                }

                parent.Controls.AddRange(new Control[] { lbl, txt, btnClear });
                yPos += 30;
            }
        }

        private void CreateDefaultSettingsTab()
        {
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(60, 63, 65)
            };

            // Size Settings Group
            var grpSize = new GroupBox
            {
                Text = "Default Size Settings",
                Location = new Point(10, 10),
                Size = new Size(400, 150),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateLabeledNumericUpDown(grpSize, "Width:", out nudDefaultFenceWidth, 200, 2000, 10, 25);
            CreateLabeledNumericUpDown(grpSize, "Height:", out nudDefaultFenceHeight, 200, 2000, 10, 55);
            CreateLabeledNumericUpDown(grpSize, "Title Height:", out nudDefaultTitleHeight, 16, 100, 10, 85);

            // Appearance Settings Group
            var grpAppearance = new GroupBox
            {
                Text = "Default Appearance Settings",
                Location = new Point(10, 170),
                Size = new Size(400, 150),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateLabeledNumericUpDown(grpAppearance, "Transparency (%):", out nudDefaultTransparency, 25, 100, 10, 25);

            chkDefaultAutoHide = new CheckBox
            {
                Text = "Auto Hide",
                Location = new Point(10, 55),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            CreateLabeledNumericUpDown(grpAppearance, "Auto Hide Delay (ms):", out nudDefaultAutoHideDelay, 500, 10000, 10, 85);

            grpAppearance.Controls.Add(chkDefaultAutoHide);

            // Color Settings Group
            var grpColors = new GroupBox
            {
                Text = "Default Color Settings",
                Location = new Point(420, 10),
                Size = new Size(450, 310),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateColorSetting(grpColors, "Background Color:", out btnDefaultBackgroundColor, out nudDefaultBackgroundTransparency, 10, 25);
            CreateColorSetting(grpColors, "Title Background Color:", out btnDefaultTitleBackgroundColor, out nudDefaultTitleBackgroundTransparency, 10, 85);
            CreateColorSetting(grpColors, "Text Color:", out btnDefaultTextColor, out nudDefaultTextTransparency, 10, 145);
            CreateColorSetting(grpColors, "Border Color:", out btnDefaultBorderColor, out nudDefaultBorderTransparency, 10, 205);

            // Style Settings Group
            var grpStyle = new GroupBox
            {
                Text = "Default Style Settings",
                Location = new Point(10, 330),
                Size = new Size(400, 200),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateLabeledNumericUpDown(grpStyle, "Border Width:", out nudDefaultBorderWidth, 0, 10, 10, 25);
            CreateLabeledNumericUpDown(grpStyle, "Corner Radius:", out nudDefaultCornerRadius, 0, 20, 10, 55);

            chkDefaultShowShadow = new CheckBox
            {
                Text = "Show Shadow",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            var lblIconSize = new Label
            {
                Text = "Icon Size:",
                Location = new Point(10, 118),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            cmbDefaultIconSize = new ComboBox
            {
                Location = new Point(120, 115),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                FlatStyle = FlatStyle.Flat
            };
            cmbDefaultIconSize.Items.AddRange(new[] { "16", "24", "32", "48", "64" });

            CreateLabeledNumericUpDown(grpStyle, "Item Spacing:", out nudDefaultItemSpacing, 5, 50, 10, 145);

            grpStyle.Controls.AddRange(new Control[] { chkDefaultShowShadow, lblIconSize, cmbDefaultIconSize });

            scrollPanel.Controls.AddRange(new Control[] { grpSize, grpAppearance, grpColors, grpStyle });
            defaultsTab.Controls.Add(scrollPanel);
        }

        private void CreateFenceManagementTab()
        {
            // Main container with proper layout
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(60, 63, 65)
            };
            
            // Set column styles - left column wider width, right column fills remaining space
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Left panel container for fence list and buttons
            var leftContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 63, 65),
                Padding = new Padding(5)
            };

            // Fence list group box
            var fenceListGroup = new GroupBox
            {
                Text = "Active Fences",
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            // Container for buttons and list with proper layout - buttons first (higher)
            var listContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(60, 63, 65)
            };
            
            // Set row styles - buttons at top with fixed height, list takes remaining space
            listContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            listContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            listContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Button panel with proper layout - moved to top
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
                BackColor = Color.FromArgb(60, 63, 65),
                Margin = new Padding(0, 0, 0, 5)
            };
            
            // Equal width columns for buttons
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Create buttons with proper sizing
            btnRefreshFences = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnRefreshFences.FlatAppearance.BorderSize = 1;
            btnRefreshFences.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnHighlightFence = new Button
            {
                Text = "Highlight",
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnHighlightFence.FlatAppearance.BorderSize = 1;
            btnHighlightFence.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnAddFence = new Button
            {
                Text = "Add",
                Dock = DockStyle.Fill,
                Margin = new Padding(1),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnAddFence.FlatAppearance.BorderSize = 1;
            btnAddFence.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            // Add buttons to button panel
            buttonPanel.Controls.Add(btnRefreshFences, 0, 0);
            buttonPanel.Controls.Add(btnHighlightFence, 1, 0);
            buttonPanel.Controls.Add(btnAddFence, 2, 0);

            // Fence list - moved below buttons
            lstFences = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 63, 65),
                ForeColor = Color.FromArgb(220, 220, 220),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                Margin = new Padding(0)
            };
            lstFences.DisplayMember = "Name";

            // Add buttons and list to list container - buttons first (row 0), then list (row 1)
            listContainer.Controls.Add(buttonPanel, 0, 0);
            listContainer.Controls.Add(lstFences, 0, 1);

            // Add list container to group box
            fenceListGroup.Controls.Add(listContainer);
            leftContainer.Controls.Add(fenceListGroup);

            // Right panel - Fence settings
            grpFenceSettings = new GroupBox
            {
                Text = "Fence Settings",
                Dock = DockStyle.Fill,
                Enabled = false,
                Margin = new Padding(5, 0, 0, 0),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateFenceSettingsControls();

            // Add panels to main container
            mainContainer.Controls.Add(leftContainer, 0, 0);
            mainContainer.Controls.Add(grpFenceSettings, 1, 0);
            
            // Add main container to tab
            fencesTab.Controls.Add(mainContainer);
        }

        private void CreateFenceSettingsControls()
        {
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(60, 63, 65),
                Padding = new Padding(10)
            };

            // Basic Settings Group
            var grpBasic = new GroupBox
            {
                Text = "Basic Settings",
                Location = new Point(0, 0),
                Size = new Size(400, 220),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            var lblName = new Label
            {
                Text = "Name:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            txtFenceName = new TextBox
            {
                Location = new Point(120, 22),
                Width = 200,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                BorderStyle = BorderStyle.FixedSingle
            };

            CreateLabeledNumericUpDown(grpBasic, "Transparency (%):", out nudFenceTransparency, 25, 100, 10, 55);

            chkFenceAutoHide = new CheckBox
            {
                Text = "Auto Hide",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            CreateLabeledNumericUpDown(grpBasic, "Auto Hide Delay (ms):", out nudFenceAutoHideDelay, 500, 10000, 10, 115);

            chkFenceLocked = new CheckBox
            {
                Text = "Locked",
                Location = new Point(10, 145),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            chkFenceCanMinify = new CheckBox
            {
                Text = "Can Minify",
                Location = new Point(150, 145),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            grpBasic.Controls.AddRange(new Control[] {
                lblName, txtFenceName, chkFenceAutoHide, chkFenceLocked, chkFenceCanMinify
            });

            // Size Settings Group
            var grpFenceSize = new GroupBox
            {
                Text = "Size Settings",
                Location = new Point(0, 230),
                Size = new Size(400, 120),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateLabeledNumericUpDown(grpFenceSize, "Width:", out nudFenceWidth, 200, 2000, 10, 25);
            CreateLabeledNumericUpDown(grpFenceSize, "Height:", out nudFenceHeight, 200, 2000, 10, 55);
            CreateLabeledNumericUpDown(grpFenceSize, "Title Height:", out nudFenceTitleHeight, 16, 100, 10, 85);

            // Color Settings Group
            var grpFenceColors = new GroupBox
            {
                Text = "Color Settings",
                Location = new Point(410, 0),
                Size = new Size(450, 350),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateColorSetting(grpFenceColors, "Background Color:", out btnFenceBackgroundColor, out nudFenceBackgroundTransparency, 10, 25);
            CreateColorSetting(grpFenceColors, "Title Background Color:", out btnFenceTitleBackgroundColor, out nudFenceTitleBackgroundTransparency, 10, 85);
            CreateColorSetting(grpFenceColors, "Text Color:", out btnFenceTextColor, out nudFenceTextTransparency, 10, 145);
            CreateColorSetting(grpFenceColors, "Border Color:", out btnFenceBorderColor, out nudFenceBorderTransparency, 10, 205);

            // Style Settings Group
            var grpFenceStyle = new GroupBox
            {
                Text = "Style Settings",
                Location = new Point(410, 360),
                Size = new Size(450, 200),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(60, 63, 65)
            };

            CreateLabeledNumericUpDown(grpFenceStyle, "Border Width:", out nudFenceBorderWidth, 0, 10, 10, 25);
            CreateLabeledNumericUpDown(grpFenceStyle, "Corner Radius:", out nudFenceCornerRadius, 0, 20, 10, 55);

            chkFenceShowShadow = new CheckBox
            {
                Text = "Show Shadow",
                Location = new Point(10, 85),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            var lblFenceIconSize = new Label
            {
                Text = "Icon Size:",
                Location = new Point(10, 118),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            cmbFenceIconSize = new ComboBox
            {
                Location = new Point(120, 115),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                FlatStyle = FlatStyle.Flat
            };
            cmbFenceIconSize.Items.AddRange(new[] { "16", "24", "32", "48", "64" });

            CreateLabeledNumericUpDown(grpFenceStyle, "Item Spacing:", out nudFenceItemSpacing, 5, 50, 10, 145);

            grpFenceStyle.Controls.AddRange(new Control[] { chkFenceShowShadow, lblFenceIconSize, cmbFenceIconSize });

            // Action buttons
            btnResetToDefaults = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(10, 570),
                Size = new Size(150, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnResetToDefaults.FlatAppearance.BorderSize = 1;
            btnResetToDefaults.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnSetAsDefaults = new Button
            {
                Text = "Set as Defaults",
                Location = new Point(170, 570),
                Size = new Size(150, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnSetAsDefaults.FlatAppearance.BorderSize = 1;
            btnSetAsDefaults.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            scrollPanel.Controls.AddRange(new Control[] {
                grpBasic, grpFenceSize, grpFenceColors, grpFenceStyle,
                btnResetToDefaults, btnSetAsDefaults
            });

            grpFenceSettings.Controls.Add(scrollPanel);
        }

        private void CreateButtonPanel()
        {
            var buttonPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(60, 63, 65)
            };

            btnApply = new Button
            {
                Text = "Apply",
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnApply.Location = new Point(buttonPanel.Width - 260, 10);
            btnApply.FlatAppearance.BorderSize = 1;
            btnApply.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnOK.Location = new Point(buttonPanel.Width - 170, 10);
            btnOK.FlatAppearance.BorderSize = 1;
            btnOK.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            btnCancel.Location = new Point(buttonPanel.Width - 80, 10);
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            buttonPanel.Controls.AddRange(new Control[] { btnApply, btnOK, btnCancel });
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void CreateLabeledNumericUpDown(Control parent, string labelText, out NumericUpDown numericUpDown,
            decimal min, decimal max, int x, int y)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(x, y + 3),
                Size = new Size(110, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            numericUpDown = new NumericUpDown
            {
                Location = new Point(x + 120, y),
                Width = 100,
                Minimum = min,
                Maximum = max,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                BorderStyle = BorderStyle.FixedSingle
            };

            parent.Controls.AddRange(new Control[] { label, numericUpDown });
        }

        private void CreateColorSetting(Control parent, string labelText, out Button colorButton,
            out NumericUpDown transparencyUpDown, int x, int y)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(x, y + 3),
                Size = new Size(130, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            colorButton = new Button
            {
                Text = "Choose Color",
                Location = new Point(x + 140, y),
                Size = new Size(120, 23),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 73, 75),
                ForeColor = Color.FromArgb(220, 220, 220)
            };
            colorButton.FlatAppearance.BorderSize = 1;
            colorButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);

            var transparencyLabel = new Label
            {
                Text = "Transparency (%):",
                Location = new Point(x + 270, y + 3),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.Transparent
            };

            transparencyUpDown = new NumericUpDown
            {
                Location = new Point(x + 380, y),
                Width = 60,
                Minimum = 0,
                Maximum = 100,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(50, 53, 55),
                BorderStyle = BorderStyle.FixedSingle
            };

            parent.Controls.AddRange(new Control[] { label, colorButton, transparencyLabel, transparencyUpDown });
        }

        private void SetupEventHandlers()
        {
            // Global settings auto-apply handlers
            chkAutoSave.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudAutoSaveInterval.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            chkShowTooltips.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            chkEnableAnimations.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            cmbLogLevel.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            chkEnableFileLogging.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };

            // Hotkey handlers
            txtToggleTransparencyShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtToggleAutoHideShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtShowAllFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtCreateNewFenceShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtOpenSettingsShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtToggleLockShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtMinimizeAllFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            txtRefreshFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };

            // Default settings auto-apply handlers
            nudDefaultFenceWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultFenceHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultTitleHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            chkDefaultAutoHide.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultAutoHideDelay.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultTitleBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultTextTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultBorderTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultBorderWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultCornerRadius.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            chkDefaultShowShadow.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            cmbDefaultIconSize.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };
            nudDefaultItemSpacing.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyGlobalSettings(); };

            // Color button handlers
            btnDefaultBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultBackgroundColor, "DefaultBackgroundColor");
            btnDefaultTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultTitleBackgroundColor, "DefaultTitleBackgroundColor");
            btnDefaultTextColor.Click += (s, e) => ShowColorDialog(btnDefaultTextColor, "DefaultTextColor");
            btnDefaultBorderColor.Click += (s, e) => ShowColorDialog(btnDefaultBorderColor, "DefaultBorderColor");

            btnFenceBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceBackgroundColor, "BackgroundColor", true);
            btnFenceTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceTitleBackgroundColor, "TitleBackgroundColor", true);
            btnFenceTextColor.Click += (s, e) => ShowColorDialog(btnFenceTextColor, "TextColor", true);
            btnFenceBorderColor.Click += (s, e) => ShowColorDialog(btnFenceBorderColor, "BorderColor", true);

            // Fence management handlers
            lstFences.DrawItem += LstFences_DrawItem;
            lstFences.SelectedIndexChanged += LstFences_SelectedIndexChanged;
            btnRefreshFences.Click += (s, e) => LoadFences();
            btnHighlightFence.Click += BtnHighlight_Click;
            btnAddFence.Click += BtnAdd_Click;
            btnResetToDefaults.Click += BtnResetToDefaults_Click;
            btnSetAsDefaults.Click += BtnSetAsDefaults_Click;

            // Fence settings auto-apply handlers
            txtFenceName.TextChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            chkFenceAutoHide.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceAutoHideDelay.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            chkFenceLocked.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            chkFenceCanMinify.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceTitleHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceTitleBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceTextTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceBorderTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceBorderWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceCornerRadius.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            chkFenceShowShadow.CheckedChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            cmbFenceIconSize.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };
            nudFenceItemSpacing.ValueChanged += (s, e) => { if (!isUpdatingControls) ApplyFenceSettings(); };

            // Main buttons
            btnOK.Click += BtnOK_Click;
            btnApply.Click += BtnApply_Click;
        }

        private void LoadSettings()
        {
            try
            {
                isUpdatingControls = true;
                logger.Debug("Loading settings into form", "SettingsForm");
                var settings = AppSettings.Instance;

                // Global settings
                chkAutoSave.Checked = settings.AutoSave;
                nudAutoSaveInterval.Value = settings.AutoSaveInterval;
                chkShowTooltips.Checked = settings.ShowTooltips;
                chkEnableAnimations.Checked = settings.EnableAnimations;
                chkStartWithWindows.Checked = settings.StartWithWindows;
                cmbLogLevel.SelectedItem = settings.LogLevel;
                chkEnableFileLogging.Checked = settings.EnableFileLogging;

                // Hotkeys
                txtToggleTransparencyShortcut.Text = settings.ToggleTransparencyShortcut;
                txtToggleAutoHideShortcut.Text = settings.ToggleAutoHideShortcut;
                txtShowAllFencesShortcut.Text = settings.ShowAllFencesShortcut;
                txtCreateNewFenceShortcut.Text = settings.CreateNewFenceShortcut;
                txtOpenSettingsShortcut.Text = settings.OpenSettingsShortcut;
                txtToggleLockShortcut.Text = settings.ToggleLockShortcut;
                txtMinimizeAllFencesShortcut.Text = settings.MinimizeAllFencesShortcut;
                txtRefreshFencesShortcut.Text = settings.RefreshFencesShortcut;

                // Default settings
                nudDefaultFenceWidth.Value = settings.DefaultFenceWidth;
                nudDefaultFenceHeight.Value = settings.DefaultFenceHeight;
                nudDefaultTitleHeight.Value = settings.DefaultTitleHeight;
                nudDefaultTransparency.Value = settings.DefaultTransparency;
                chkDefaultAutoHide.Checked = settings.DefaultAutoHide;
                nudDefaultAutoHideDelay.Value = settings.DefaultAutoHideDelay;

                SetColorButton(btnDefaultBackgroundColor, settings.DefaultBackgroundColor);
                nudDefaultBackgroundTransparency.Value = settings.DefaultBackgroundTransparency;
                SetColorButton(btnDefaultTitleBackgroundColor, settings.DefaultTitleBackgroundColor);
                nudDefaultTitleBackgroundTransparency.Value = settings.DefaultTitleBackgroundTransparency;
                SetColorButton(btnDefaultTextColor, settings.DefaultTextColor);
                nudDefaultTextTransparency.Value = settings.DefaultTextTransparency;
                SetColorButton(btnDefaultBorderColor, settings.DefaultBorderColor);
                nudDefaultBorderTransparency.Value = settings.DefaultBorderTransparency;

                nudDefaultBorderWidth.Value = settings.DefaultBorderWidth;
                nudDefaultCornerRadius.Value = settings.DefaultCornerRadius;
                chkDefaultShowShadow.Checked = settings.DefaultShowShadow;
                cmbDefaultIconSize.SelectedItem = settings.DefaultIconSize.ToString();
                nudDefaultItemSpacing.Value = settings.DefaultItemSpacing;

                logger.Info("Settings loaded successfully", "SettingsForm");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to load settings", "SettingsForm", ex);
            }
            finally
            {
                isUpdatingControls = false;
            }
        }

        private void LoadFences()
        {
            try
            {
                logger.Debug("Loading fences into list", "SettingsForm");

                fenceInfos = FenceManager.Instance.GetAllFenceInfos();

                Guid? selectedId = selectedFenceInfo?.Id;

                lstFences.Items.Clear();

                foreach (var fence in fenceInfos)
                {
                    lstFences.Items.Add(fence);
                }

                if (selectedId.HasValue)
                {
                    var fenceToSelect = fenceInfos.FirstOrDefault(f => f.Id == selectedId.Value);
                    if (fenceToSelect != null)
                    {
                        lstFences.SelectedItem = fenceToSelect;
                    }
                }

                if (lstFences.SelectedItem == null && fenceInfos.Any())
                {
                    lstFences.SelectedItem = fenceInfos.First();
                }

                logger.Info($"Loaded {fenceInfos.Count} fences", "SettingsForm");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to load fences", "SettingsForm", ex);
            }
        }

        private void LoadFenceSettings()
        {
            if (selectedFenceInfo == null) return;

            try
            {
                isUpdatingControls = true;
                logger.Debug($"Loading settings for fence '{selectedFenceInfo.Name}'", "SettingsForm");

                txtFenceName.Text = selectedFenceInfo.Name;
                nudFenceTransparency.Value = selectedFenceInfo.Transparency;
                chkFenceAutoHide.Checked = selectedFenceInfo.AutoHide;
                nudFenceAutoHideDelay.Value = selectedFenceInfo.AutoHideDelay;
                chkFenceLocked.Checked = selectedFenceInfo.Locked;
                chkFenceCanMinify.Checked = selectedFenceInfo.CanMinify;
                nudFenceWidth.Value = selectedFenceInfo.Width;
                nudFenceHeight.Value = selectedFenceInfo.Height;
                nudFenceTitleHeight.Value = selectedFenceInfo.TitleHeight;

                SetColorButton(btnFenceBackgroundColor, selectedFenceInfo.BackgroundColor);
                nudFenceBackgroundTransparency.Value = selectedFenceInfo.BackgroundTransparency;
                SetColorButton(btnFenceTitleBackgroundColor, selectedFenceInfo.TitleBackgroundColor);
                nudFenceTitleBackgroundTransparency.Value = selectedFenceInfo.TitleBackgroundTransparency;
                SetColorButton(btnFenceTextColor, selectedFenceInfo.TextColor);
                nudFenceTextTransparency.Value = selectedFenceInfo.TextTransparency;
                SetColorButton(btnFenceBorderColor, selectedFenceInfo.BorderColor);
                nudFenceBorderTransparency.Value = selectedFenceInfo.BorderTransparency;

                nudFenceBorderWidth.Value = selectedFenceInfo.BorderWidth;
                nudFenceCornerRadius.Value = selectedFenceInfo.CornerRadius;
                chkFenceShowShadow.Checked = selectedFenceInfo.ShowShadow;
                cmbFenceIconSize.SelectedItem = selectedFenceInfo.IconSize.ToString();
                nudFenceItemSpacing.Value = selectedFenceInfo.ItemSpacing;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load fence settings for '{selectedFenceInfo?.Name}'", "SettingsForm", ex);
            }
            finally
            {
                isUpdatingControls = false;
            }
        }

        private void ShowColorDialog(Button button, string propertyName, bool isFenceProperty = false)
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.FullOpen = true;

                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    SetColorButton(button, colorDialog.Color.ToArgb());

                    if (isFenceProperty)
                    {
                        ApplyFenceSettings();
                    }
                    else
                    {
                        ApplyGlobalSettings();
                    }
                }
            }
        }

        private void SetColorButton(Button button, int argbColor)
        {
            var color = Color.FromArgb(argbColor);
            button.BackColor = color;
            button.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void ApplyGlobalSettings()
        {
            if (isUpdatingControls) return;

            try
            {
                logger.Debug("Applying global settings", "SettingsForm");
                var settings = AppSettings.Instance;

                settings.AutoSave = chkAutoSave.Checked;
                settings.AutoSaveInterval = (int)nudAutoSaveInterval.Value;
                settings.ShowTooltips = chkShowTooltips.Checked;
                settings.EnableAnimations = chkEnableAnimations.Checked;
                settings.LogLevel = cmbLogLevel.SelectedItem?.ToString() ?? "Info";
                settings.EnableFileLogging = chkEnableFileLogging.Checked;
                
                // Handle startup with Windows setting
                bool previousStartupSetting = settings.StartWithWindows;
                settings.StartWithWindows = chkStartWithWindows.Checked;
                
                if (previousStartupSetting != settings.StartWithWindows)
                {
                    if (settings.StartWithWindows)
                    {
                        if (!StartupManager.EnableStartup())
                        {
                            logger.Error("Failed to enable startup", "SettingsForm");
                            MessageBox.Show("Failed to enable startup with Windows. Please check the logs for details.", 
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            settings.StartWithWindows = false;
                            chkStartWithWindows.Checked = false;
                        }
                    }
                    else
                    {
                        if (!StartupManager.DisableStartup())
                        {
                            logger.Error("Failed to disable startup", "SettingsForm");
                            MessageBox.Show("Failed to disable startup with Windows. Please check the logs for details.", 
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            settings.StartWithWindows = true;
                            chkStartWithWindows.Checked = true;
                        }
                    }
                }

                settings.ToggleTransparencyShortcut = txtToggleTransparencyShortcut.Text;
                settings.ToggleAutoHideShortcut = txtToggleAutoHideShortcut.Text;
                settings.ShowAllFencesShortcut = txtShowAllFencesShortcut.Text;
                settings.CreateNewFenceShortcut = txtCreateNewFenceShortcut.Text;
                settings.OpenSettingsShortcut = txtOpenSettingsShortcut.Text;
                settings.ToggleLockShortcut = txtToggleLockShortcut.Text;
                settings.MinimizeAllFencesShortcut = txtMinimizeAllFencesShortcut.Text;
                settings.RefreshFencesShortcut = txtRefreshFencesShortcut.Text;

                settings.DefaultFenceWidth = (int)nudDefaultFenceWidth.Value;
                settings.DefaultFenceHeight = (int)nudDefaultFenceHeight.Value;
                settings.DefaultTitleHeight = (int)nudDefaultTitleHeight.Value;
                settings.DefaultTransparency = (int)nudDefaultTransparency.Value;
                settings.DefaultAutoHide = chkDefaultAutoHide.Checked;
                settings.DefaultAutoHideDelay = (int)nudDefaultAutoHideDelay.Value;

                settings.DefaultBackgroundColor = btnDefaultBackgroundColor.BackColor.ToArgb();
                settings.DefaultBackgroundTransparency = (int)nudDefaultBackgroundTransparency.Value;
                settings.DefaultTitleBackgroundColor = btnDefaultTitleBackgroundColor.BackColor.ToArgb();
                settings.DefaultTitleBackgroundTransparency = (int)nudDefaultTitleBackgroundTransparency.Value;
                settings.DefaultTextColor = btnDefaultTextColor.BackColor.ToArgb();
                settings.DefaultTextTransparency = (int)nudDefaultTextTransparency.Value;
                settings.DefaultBorderColor = btnDefaultBorderColor.BackColor.ToArgb();
                settings.DefaultBorderTransparency = (int)nudDefaultBorderTransparency.Value;

                settings.DefaultBorderWidth = (int)nudDefaultBorderWidth.Value;
                settings.DefaultCornerRadius = (int)nudDefaultCornerRadius.Value;
                settings.DefaultShowShadow = chkDefaultShowShadow.Checked;
                if (int.TryParse(cmbDefaultIconSize.SelectedItem?.ToString(), out int iconSize))
                    settings.DefaultIconSize = iconSize;
                settings.DefaultItemSpacing = (int)nudDefaultItemSpacing.Value;

                settings.SaveSettings();

                logger.Info("Global settings applied successfully", "SettingsForm");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply global settings", "SettingsForm", ex);
                MessageBox.Show($"Failed to apply settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFenceSettings()
        {
            if (selectedFenceInfo == null || isUpdatingControls) return;

            try
            {
                selectedFenceInfo.Name = txtFenceName.Text;
                selectedFenceInfo.Transparency = (int)nudFenceTransparency.Value;
                selectedFenceInfo.AutoHide = chkFenceAutoHide.Checked;
                selectedFenceInfo.AutoHideDelay = (int)nudFenceAutoHideDelay.Value;
                selectedFenceInfo.Locked = chkFenceLocked.Checked;
                selectedFenceInfo.CanMinify = chkFenceCanMinify.Checked;
                selectedFenceInfo.Width = (int)nudFenceWidth.Value;
                selectedFenceInfo.Height = (int)nudFenceHeight.Value;
                selectedFenceInfo.TitleHeight = (int)nudFenceTitleHeight.Value;

                selectedFenceInfo.BackgroundColor = btnFenceBackgroundColor.BackColor.ToArgb();
                selectedFenceInfo.BackgroundTransparency = (int)nudFenceBackgroundTransparency.Value;
                selectedFenceInfo.TitleBackgroundColor = btnFenceTitleBackgroundColor.BackColor.ToArgb();
                selectedFenceInfo.TitleBackgroundTransparency = (int)nudFenceTitleBackgroundTransparency.Value;
                selectedFenceInfo.TextColor = btnFenceTextColor.BackColor.ToArgb();
                selectedFenceInfo.TextTransparency = (int)nudFenceTextTransparency.Value;
                selectedFenceInfo.BorderColor = btnFenceBorderColor.BackColor.ToArgb();
                selectedFenceInfo.BorderTransparency = (int)nudFenceBorderTransparency.Value;

                selectedFenceInfo.BorderWidth = (int)nudFenceBorderWidth.Value;
                selectedFenceInfo.CornerRadius = (int)nudFenceCornerRadius.Value;
                selectedFenceInfo.ShowShadow = chkFenceShowShadow.Checked;
                if (int.TryParse(cmbFenceIconSize.SelectedItem?.ToString(), out int iconSize))
                    selectedFenceInfo.IconSize = iconSize;
                selectedFenceInfo.ItemSpacing = (int)nudFenceItemSpacing.Value;

                FenceManager.Instance.UpdateFence(selectedFenceInfo);
                FenceManager.Instance.ApplySettingsToFence(selectedFenceInfo);



                logger.Info($"Applied settings to fence '{selectedFenceInfo.Name}'", "SettingsForm");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to apply fence settings for '{selectedFenceInfo?.Name}'", "SettingsForm", ex);
            }
        }

        #region Event Handlers

        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                ApplyGlobalSettings();
                if (selectedFenceInfo != null)
                {
                    ApplyFenceSettings();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply settings on OK", "SettingsForm", ex);
                MessageBox.Show($"Failed to apply settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            try
            {
                ApplyGlobalSettings();
                if (selectedFenceInfo != null)
                {
                    ApplyFenceSettings();
                }

                MessageBox.Show("Settings applied successfully!", "Settings Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply settings", "SettingsForm", ex);
                MessageBox.Show($"Failed to apply settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnHighlight_Click(object sender, EventArgs e)
        {
            if (selectedFenceInfo != null)
            {
                FenceManager.Instance.HighlightFence(selectedFenceInfo.Id);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog("New Fence", "Enter fence name:"))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    FenceManager.Instance.CreateFence(dialog.InputText);
                    LoadFences();
                }
            }
        }

        private void BtnResetToDefaults_Click(object sender, EventArgs e)
        {
            if (selectedFenceInfo != null)
            {
                var result = MessageBox.Show(
                    $"Reset fence '{selectedFenceInfo.Name}' to default settings?",
                    "Reset to Defaults",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var settings = AppSettings.Instance;

                    selectedFenceInfo.Transparency = settings.DefaultTransparency;
                    selectedFenceInfo.AutoHide = settings.DefaultAutoHide;
                    selectedFenceInfo.AutoHideDelay = settings.DefaultAutoHideDelay;
                    selectedFenceInfo.Width = settings.DefaultFenceWidth;
                    selectedFenceInfo.Height = settings.DefaultFenceHeight;
                    selectedFenceInfo.TitleHeight = settings.DefaultTitleHeight;
                    selectedFenceInfo.BackgroundColor = settings.DefaultBackgroundColor;
                    selectedFenceInfo.TitleBackgroundColor = settings.DefaultTitleBackgroundColor;
                    selectedFenceInfo.TextColor = settings.DefaultTextColor;
                    selectedFenceInfo.BorderColor = settings.DefaultBorderColor;
                    selectedFenceInfo.BackgroundTransparency = settings.DefaultBackgroundTransparency;
                    selectedFenceInfo.TitleBackgroundTransparency = settings.DefaultTitleBackgroundTransparency;
                    selectedFenceInfo.TextTransparency = settings.DefaultTextTransparency;
                    selectedFenceInfo.BorderTransparency = settings.DefaultBorderTransparency;
                    selectedFenceInfo.BorderWidth = settings.DefaultBorderWidth;
                    selectedFenceInfo.CornerRadius = settings.DefaultCornerRadius;
                    selectedFenceInfo.ShowShadow = settings.DefaultShowShadow;
                    selectedFenceInfo.IconSize = settings.DefaultIconSize;
                    selectedFenceInfo.ItemSpacing = settings.DefaultItemSpacing;

                    LoadFenceSettings();
                    ApplyFenceSettings();
                }
            }
        }

        private void BtnSetAsDefaults_Click(object sender, EventArgs e)
        {
            if (selectedFenceInfo != null)
            {
                var result = MessageBox.Show(
                    $"Set fence '{selectedFenceInfo.Name}' settings as new defaults?",
                    "Set as Defaults",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    var settings = AppSettings.Instance;

                    settings.DefaultTransparency = selectedFenceInfo.Transparency;
                    settings.DefaultAutoHide = selectedFenceInfo.AutoHide;
                    settings.DefaultAutoHideDelay = selectedFenceInfo.AutoHideDelay;
                    settings.DefaultFenceWidth = selectedFenceInfo.Width;
                    settings.DefaultFenceHeight = selectedFenceInfo.Height;
                    settings.DefaultTitleHeight = selectedFenceInfo.TitleHeight;
                    settings.DefaultBackgroundColor = selectedFenceInfo.BackgroundColor;
                    settings.DefaultTitleBackgroundColor = selectedFenceInfo.TitleBackgroundColor;
                    settings.DefaultTextColor = selectedFenceInfo.TextColor;
                    settings.DefaultBorderColor = selectedFenceInfo.BorderColor;
                    settings.DefaultBackgroundTransparency = selectedFenceInfo.BackgroundTransparency;
                    settings.DefaultTitleBackgroundTransparency = selectedFenceInfo.TitleBackgroundTransparency;
                    settings.DefaultTextTransparency = selectedFenceInfo.TextTransparency;
                    settings.DefaultBorderTransparency = selectedFenceInfo.BorderTransparency;
                    settings.DefaultBorderWidth = selectedFenceInfo.BorderWidth;
                    settings.DefaultCornerRadius = selectedFenceInfo.CornerRadius;
                    settings.DefaultShowShadow = selectedFenceInfo.ShowShadow;
                    settings.DefaultIconSize = selectedFenceInfo.IconSize;
                    settings.DefaultItemSpacing = selectedFenceInfo.ItemSpacing;

                    settings.SaveSettings();
                    LoadSettings(); // Reload default settings controls

                    MessageBox.Show("Default settings updated successfully!", "Defaults Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LstFences_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var listBox = (ListBox)sender;
            var fence = (FenceInfo)listBox.Items[e.Index];

            // Background color
            Color backgroundColor = (e.State & DrawItemState.Selected) != 0
                ? Color.FromArgb(81, 81, 81) // Selected color
                : Color.FromArgb(60, 63, 65); // Normal color

            using (var backgroundBrush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            // Text color
            using (var textBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
            {
                e.Graphics.DrawString(fence.Name, e.Font, textBrush, e.Bounds, StringFormat.GenericDefault);
            }

            e.DrawFocusRectangle();
        }

        private void LstFences_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedFenceInfo = lstFences.SelectedItem as FenceInfo;
            grpFenceSettings.Enabled = selectedFenceInfo != null;
            LoadFenceSettings();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logger?.Debug("Disposing settings form", "SettingsForm");
            }
            base.Dispose(disposing);
        }
    }
}