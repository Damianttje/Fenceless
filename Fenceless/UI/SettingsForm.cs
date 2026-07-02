using Fenceless.Model;
using Fenceless.Util;
using Fenceless.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Fenceless.UI
{
    public class SettingsForm : ThemedForm
    {
        private readonly Logger logger;
        private List<FenceInfo> fenceInfos;
        private FenceInfo selectedFenceInfo;
        private bool isUpdatingControls = false;
        private System.Threading.Timer _globalSettingsDebounce;
        private System.Threading.Timer _fenceSettingsDebounce;
        private SaveStatusIndicator _saveIndicator;

        private SidebarNavigation sidebar;
        private AnimatedPagePanel pagePanel;
        private Panel footerPanel;

        private const string PageGeneral = "general";
        private const string PageAppearance = "appearance";
        private const string PageHotkeys = "hotkeys";
        private const string PageFences = "fences";

        #region General Page Controls
        private ToggleSwitch chkAutoSave;
        private NumericUpDown nudAutoSaveInterval;
        private ToggleSwitch chkShowTooltips;
        private ToggleSwitch chkEnableAnimations;
        private ToggleSwitch chkStartWithWindows;
        private ComboBox cmbLogLevel;
        private ToggleSwitch chkEnableFileLogging;
        #endregion

        #region Appearance Page Controls
        private NumericUpDown nudDefaultFenceWidth;
        private NumericUpDown nudDefaultFenceHeight;
        private NumericUpDown nudDefaultTitleHeight;
        private NumericUpDown nudDefaultTransparency;
        private ToggleSwitch chkDefaultAutoHide;
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
        private ToggleSwitch chkDefaultShowShadow;
        private ComboBox cmbDefaultIconSize;
        private NumericUpDown nudDefaultItemSpacing;
        private FencePreviewControl _appearancePreview;
        #endregion

        #region Hotkey Page Controls
        private HotkeyCaptureBox txtToggleTransparencyShortcut;
        private HotkeyCaptureBox txtToggleAutoHideShortcut;
        private HotkeyCaptureBox txtShowAllFencesShortcut;
        private HotkeyCaptureBox txtCreateNewFenceShortcut;
        private HotkeyCaptureBox txtOpenSettingsShortcut;
        private HotkeyCaptureBox txtToggleLockShortcut;
        private HotkeyCaptureBox txtMinimizeAllFencesShortcut;
        private HotkeyCaptureBox txtRefreshFencesShortcut;
        #endregion

        #region Fences Page Controls
        private ListBox lstFences;
        private FlowLayoutPanel fenceSettingsPanel;
        private ComboBox cmbFenceType;
        private TextBox txtFenceName;
        private NumericUpDown nudFenceTransparency;
        private ToggleSwitch chkFenceAutoHide;
        private NumericUpDown nudFenceAutoHideDelay;
        private ToggleSwitch chkFenceLocked;
        private ToggleSwitch chkFenceCanMinify;
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
        private ToggleSwitch chkFenceShowShadow;
        private ComboBox cmbFenceIconSize;
        private NumericUpDown nudFenceItemSpacing;
        private Button btnResetToDefaults;
        private Button btnSetAsDefaults;
        #endregion

        #region Type-Specific Controls
        private SettingsSection liveFolderPanel;
        private TextBox txtWatchPath;
        private ToggleSwitch chkWatchRecursive;
        private TextBox txtFileFilter;
        private NumericUpDown nudLiveFolderMaxItems;

        private SettingsSection runningTasksPanel;
        private NumericUpDown nudUpdateInterval;
        private ToggleSwitch chkShowMinimizedWindows;
        private TextBox txtProcessFilter;
        private NumericUpDown nudRunningTasksMaxItems;

        private SettingsSection clipboardHistoryPanel;
        private NumericUpDown nudClipboardMaxItems;
        private ToggleSwitch chkCaptureImages;
        #endregion

        public SettingsForm()
        {
            logger = Logger.Instance;
            logger.Debug("Creating settings form", "SettingsForm");

            InitializeComponent();
            LoadSettings();
            LoadFences();
            WireAppearancePreview();
            _appearancePreview?.Invalidate();

            this.Shown += (s, e) =>
            {
                if (AppSettings.Instance.EnableAnimations)
                    AnimationHelper.FadeIn(this, 200);
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            SetupThemedForm("Fenceless Settings", showMinimize: true, showMaximize: true, sizable: true, showInTaskbar: true);
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(900, 600);

            CreateLayout();
            SetupEventHandlers();

            this.ResumeLayout(false);
        }

        private void CreateLayout()
        {
            sidebar = new SidebarNavigation();
            sidebar.AddItem("General", "\uE713");
            sidebar.AddItem("Fences", "\uE8FD");
            sidebar.AddItem("Appearance", "\uE771");
            sidebar.AddItem("Hotkeys", "\uE7DF");
            sidebar.PageChanged += (s, index) =>
            {
                var keys = new[] { PageGeneral, PageFences, PageAppearance, PageHotkeys };
                if (index < keys.Length)
                    pagePanel.SwitchTo(keys[index]);
            };

            pagePanel = new AnimatedPagePanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid
            };

            pagePanel.AddPage(PageGeneral, CreateGeneralPage());
            pagePanel.AddPage(PageFences, CreateFencesPage());
            pagePanel.AddPage(PageAppearance, CreateAppearancePage());
            pagePanel.AddPage(PageHotkeys, CreateHotkeysPage());

            footerPanel = new Panel
            {
                Height = 48,
                Dock = DockStyle.Bottom,
                BackColor = Theme.Colors.BackgroundDark,
                Padding = new Padding(12, 0, 12, 0)
            };

            _saveIndicator = new SaveStatusIndicator
            {
                Dock = DockStyle.Left,
                Width = 240
            };

            var btnOK = Theme.CreateFlatButton("OK", Theme.ButtonRole.Accent);
            btnOK.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += BtnOK_Click;

            var btnCancel = Theme.CreateFlatButton("Cancel");
            btnCancel.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            btnCancel.DialogResult = DialogResult.Cancel;

            var btnExport = Theme.CreateFlatButton("Export");
            btnExport.Size = new Size(84, Theme.Sizes.ButtonHeight);
            btnExport.Click += BtnExport_Click;

            var btnImport = Theme.CreateFlatButton("Import");
            btnImport.Size = new Size(84, Theme.Sizes.ButtonHeight);
            btnImport.Click += BtnImport_Click;

            var buttonFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false
            };
            buttonFlow.Controls.Add(btnCancel);
            buttonFlow.Controls.Add(btnOK);
            buttonFlow.Controls.Add(btnImport);
            buttonFlow.Controls.Add(btnExport);
            footerPanel.Controls.Add(buttonFlow);
            footerPanel.Controls.Add(_saveIndicator);

            this.Controls.Add(pagePanel);
            this.Controls.Add(footerPanel);
            this.Controls.Add(sidebar);

            BringChromeToFront();

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        #region Page Creation

        private FlowLayoutPanel CreateScrollPage()
        {
            var page = new FlowLayoutPanel
            {
                BackColor = Theme.Colors.BackgroundMid,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(20, 8, 20, 20),
                Margin = Padding.Empty
            };

            page.Resize += (s, e) =>
            {
                foreach (Control c in page.Controls)
                    if (c is RoundedPanel section)
                        section.Width = page.ClientSize.Width - page.Padding.Horizontal;
            };

            return page;
        }

        private ScrollableControl CreateGeneralPage()
        {
            var page = CreateScrollPage();

            var autoSaveSection = new SettingsSection("Auto Save", 700);
            autoSaveSection.AddRow(CreateSettingsRow("Auto Save", chkAutoSave = new ToggleSwitch { Checked = true },
                "Automatically save fence layout changes at a regular interval."));
            autoSaveSection.AddRow(CreateSettingsRow("Interval (seconds)", nudAutoSaveInterval = Theme.CreateNumericUpDown(5, 300, 30)));

            var behaviorSection = new SettingsSection("Behavior", 700);
            behaviorSection.AddRow(CreateSettingsRow("Show Tooltips", chkShowTooltips = new ToggleSwitch { Checked = true }));
            behaviorSection.AddRow(CreateSettingsRow("Enable Animations", chkEnableAnimations = new ToggleSwitch { Checked = true },
                "Fade and slide transitions for the settings window and pages."));
            behaviorSection.AddRow(CreateSettingsRow("Start with Windows", chkStartWithWindows = new ToggleSwitch { Checked = false }));

            var loggingSection = new SettingsSection("Logging", 700);
            loggingSection.AddRow(CreateSettingsRow("Log Level", cmbLogLevel = Theme.CreateComboBox(new[] { "Debug", "Info", "Warning", "Error", "Critical" })));
            loggingSection.AddRow(CreateSettingsRow("File Logging", chkEnableFileLogging = new ToggleSwitch { Checked = true },
                "Persist log messages to disk in addition to the in-app viewer."));

            page.Controls.Add(autoSaveSection);
            page.Controls.Add(behaviorSection);
            page.Controls.Add(loggingSection);

            return page;
        }

        private ScrollableControl CreateAppearancePage()
        {
            var page = CreateScrollPage();

            var previewHost = new Panel
            {
                Height = 150,
                Width = 700,
                BackColor = Theme.Colors.BackgroundMid,
                Margin = new Padding(0, 0, 0, Theme.Sizes.SectionSpacing)
            };
            var preview = new FencePreviewControl
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid
            };
            var previewCaption = Theme.CreateLabel("Live preview", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            previewCaption.Dock = DockStyle.Top;
            previewCaption.Padding = new Padding(4, 0, 0, 4);
            previewHost.Controls.Add(preview);
            previewHost.Controls.Add(previewCaption);
            _appearancePreview = preview;
            page.Controls.Add(previewHost);

            var sizeSection = new SettingsSection("Default Size", 700);
            sizeSection.AddRow(CreateSettingsRow("Width", nudDefaultFenceWidth = Theme.CreateNumericUpDown(200, 2000, 524)));
            sizeSection.AddRow(CreateSettingsRow("Height", nudDefaultFenceHeight = Theme.CreateNumericUpDown(200, 2000, 517)));
            sizeSection.AddRow(CreateSettingsRow("Title Height", nudDefaultTitleHeight = Theme.CreateNumericUpDown(16, 100, 25)));

            var appearanceSection = new SettingsSection("Default Appearance", 700);
            appearanceSection.AddRow(CreateSettingsRow("Transparency (%)", nudDefaultTransparency = Theme.CreateNumericUpDown(25, 100, 80)));
            appearanceSection.AddRow(CreateSettingsRow("Auto Hide", chkDefaultAutoHide = new ToggleSwitch()));
            appearanceSection.AddRow(CreateSettingsRow("Auto Hide Delay (ms)", nudDefaultAutoHideDelay = Theme.CreateNumericUpDown(500, 10000, 2000)));

            var colorsSection = new SettingsSection("Default Colors", 700);
            colorsSection.AddRow(CreateColorRow("Background", out btnDefaultBackgroundColor, out nudDefaultBackgroundTransparency, 0));
            colorsSection.AddRow(CreateColorRow("Title Background", out btnDefaultTitleBackgroundColor, out nudDefaultTitleBackgroundTransparency, 50));
            colorsSection.AddRow(CreateColorRow("Text", out btnDefaultTextColor, out nudDefaultTextTransparency, 100));
            colorsSection.AddRow(CreateColorRow("Border", out btnDefaultBorderColor, out nudDefaultBorderTransparency, 150));

            var styleSection = new SettingsSection("Default Style", 700);
            styleSection.AddRow(CreateSettingsRow("Item Spacing", nudDefaultItemSpacing = Theme.CreateNumericUpDown(5, 50, 15)));
            styleSection.AddRow(CreateSettingsRow("Icon Size", cmbDefaultIconSize = Theme.CreateComboBox(new[] { "16", "24", "32", "48", "64" })));
            styleSection.AddRow(CreateSettingsRow("Show Shadow", chkDefaultShowShadow = new ToggleSwitch { Checked = true }));
            styleSection.AddRow(CreateSettingsRow("Corner Radius", nudDefaultCornerRadius = Theme.CreateNumericUpDown(0, 50, 0)));
            styleSection.AddRow(CreateSettingsRow("Border Width", nudDefaultBorderWidth = Theme.CreateNumericUpDown(0, 10, 0)));

            page.Controls.Add(sizeSection);
            page.Controls.Add(appearanceSection);
            page.Controls.Add(colorsSection);
            page.Controls.Add(styleSection);

            return page;
        }

        private ScrollableControl CreateHotkeysPage()
        {
            var page = CreateScrollPage();

            var section = new SettingsSection("Global Hotkeys", 700);

            var infoRow = new Panel { Height = 36, BackColor = Color.Transparent };
            var infoLabel = Theme.CreateLabel("Click a field and press the desired key combination. Press Escape to clear.",
                Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            infoLabel.Location = new Point(0, 8);
            infoLabel.MaximumSize = new Size(676, 0);
            infoLabel.AutoSize = true;
            infoRow.Controls.Add(infoLabel);
            section.AddRow(infoRow);

            section.AddRow(CreateHotkeyRow("Toggle Transparency", out txtToggleTransparencyShortcut));
            section.AddRow(CreateHotkeyRow("Toggle Auto-Hide", out txtToggleAutoHideShortcut));
            section.AddRow(CreateHotkeyRow("Show All Fences", out txtShowAllFencesShortcut));
            section.AddRow(CreateHotkeyRow("Create New Fence", out txtCreateNewFenceShortcut));
            section.AddRow(CreateHotkeyRow("Open Settings", out txtOpenSettingsShortcut));
            section.AddRow(CreateHotkeyRow("Toggle Lock", out txtToggleLockShortcut));
            section.AddRow(CreateHotkeyRow("Minimize All Fences", out txtMinimizeAllFencesShortcut));
            section.AddRow(CreateHotkeyRow("Refresh Fences", out txtRefreshFencesShortcut));

            page.Controls.Add(section);

            return page;
        }

        private ScrollableControl CreateFencesPage()
        {
            var page = new Panel
            {
                BackColor = Theme.Colors.BackgroundMid,
                Dock = DockStyle.Fill,
                Padding = new Padding(Theme.Sizes.PanelPadding)
            };

            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Colors.BackgroundMid
            };
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                Padding = new Padding(0, 0, 10, 0)
            };

            var listCard = new CardPanel("Active Fences") { Dock = DockStyle.Fill };

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Height = Theme.Sizes.ButtonHeight + 8,
                Dock = DockStyle.Top,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 6)
            };

            var btnRefresh = Theme.CreateFlatButton("Refresh");
            btnRefresh.Width = 78;
            btnRefresh.Click += (s, e) => LoadFences();

            var btnHighlight = Theme.CreateFlatButton("Highlight");
            btnHighlight.Width = 78;
            btnHighlight.Click += BtnHighlight_Click;

            var btnAdd = Theme.CreateFlatButton("Add");
            btnAdd.Width = 64;
            btnAdd.Click += BtnAdd_Click;

            var btnDelete = Theme.CreateFlatButton("Delete", Theme.ButtonRole.Danger);
            btnDelete.Width = 68;
            btnDelete.Click += BtnDelete_Click;

            buttonPanel.Controls.AddRange(new Control[] { btnRefresh, btnHighlight, btnAdd, btnDelete });

            lstFences = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.InputBackground,
                ForeColor = Theme.Colors.InputText,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                Font = Theme.Fonts.Body,
                ItemHeight = 30
            };
            lstFences.DisplayMember = "Name";

            listCard.Content.Controls.Add(lstFences);
            listCard.Content.Controls.Add(buttonPanel);
            leftPanel.Controls.Add(listCard);

            fenceSettingsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Enabled = false,
                Padding = new Padding(0),
                Margin = Padding.Empty
            };

            fenceSettingsPanel.Resize += (s, e) =>
            {
                foreach (Control c in fenceSettingsPanel.Controls)
                    if (c is RoundedPanel section)
                        section.Width = fenceSettingsPanel.ClientSize.Width;
            };

            CreateFenceSettingsEditor();

            mainContainer.Controls.Add(leftPanel, 0, 0);
            mainContainer.Controls.Add(fenceSettingsPanel, 1, 0);
            page.Controls.Add(mainContainer);

            return page;
        }

        private void CreateFenceSettingsEditor()
        {
            var typeSection = new SettingsSection("Fence Type", 500);
            cmbFenceType = Theme.CreateComboBox(new[] { "Standard", "Live Folder", "Running Tasks", "Clipboard History" });
            cmbFenceType.Width = 200;
            typeSection.AddRow(CreateSettingsRow("Type", cmbFenceType));
            cmbFenceType.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) UpdateTypeSpecificVisibility(); };

            var basicSection = new SettingsSection("Basic Settings", 500);
            txtFenceName = Theme.CreateTextBox();
            txtFenceName.Width = 200;
            basicSection.AddRow(CreateSettingsRow("Name", txtFenceName));
            basicSection.AddRow(CreateSettingsRow("Transparency (%)", nudFenceTransparency = Theme.CreateNumericUpDown(25, 100, 100)));
            basicSection.AddRow(CreateSettingsRow("Auto Hide", chkFenceAutoHide = new ToggleSwitch()));
            basicSection.AddRow(CreateSettingsRow("Auto Hide Delay (ms)", nudFenceAutoHideDelay = Theme.CreateNumericUpDown(500, 10000, 2000)));
            basicSection.AddRow(CreateSettingsRow("Can Minify", chkFenceCanMinify = new ToggleSwitch { Checked = true }));
            basicSection.AddRow(CreateSettingsRow("Locked", chkFenceLocked = new ToggleSwitch()));

            var sizeSection = new SettingsSection("Size Settings", 500);
            sizeSection.AddRow(CreateSettingsRow("Width", nudFenceWidth = Theme.CreateNumericUpDown(200, 2000, 524)));
            sizeSection.AddRow(CreateSettingsRow("Height", nudFenceHeight = Theme.CreateNumericUpDown(200, 2000, 517)));
            sizeSection.AddRow(CreateSettingsRow("Title Height", nudFenceTitleHeight = Theme.CreateNumericUpDown(16, 100, 25)));

            var colorsSection = new SettingsSection("Colors", 500);
            colorsSection.AddRow(CreateColorRow("Background", out btnFenceBackgroundColor, out nudFenceBackgroundTransparency, 0));
            colorsSection.AddRow(CreateColorRow("Title Background", out btnFenceTitleBackgroundColor, out nudFenceTitleBackgroundTransparency, 50));
            colorsSection.AddRow(CreateColorRow("Text", out btnFenceTextColor, out nudFenceTextTransparency, 100));
            colorsSection.AddRow(CreateColorRow("Border", out btnFenceBorderColor, out nudFenceBorderTransparency, 150));

            var styleSection = new SettingsSection("Style", 500);
            styleSection.AddRow(CreateSettingsRow("Item Spacing", nudFenceItemSpacing = Theme.CreateNumericUpDown(5, 50, 15)));
            styleSection.AddRow(CreateSettingsRow("Icon Size", cmbFenceIconSize = Theme.CreateComboBox(new[] { "16", "24", "32", "48", "64" })));
            styleSection.AddRow(CreateSettingsRow("Show Shadow", chkFenceShowShadow = new ToggleSwitch { Checked = true }));
            styleSection.AddRow(CreateSettingsRow("Corner Radius", nudFenceCornerRadius = Theme.CreateNumericUpDown(0, 50, 0)));
            styleSection.AddRow(CreateSettingsRow("Border Width", nudFenceBorderWidth = Theme.CreateNumericUpDown(0, 10, 0)));

            var actionCard = new CardPanel("Actions");
            var actionFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnResetToDefaults = Theme.CreateFlatButton("Reset to Defaults", Theme.ButtonRole.Danger);
            btnResetToDefaults.Size = new Size(150, Theme.Sizes.ButtonHeight);
            btnResetToDefaults.Click += BtnResetToDefaults_Click;

            btnSetAsDefaults = Theme.CreateFlatButton("Set as Defaults");
            btnSetAsDefaults.Size = new Size(150, Theme.Sizes.ButtonHeight);
            btnSetAsDefaults.Click += BtnSetAsDefaults_Click;

            actionFlow.Controls.AddRange(new Control[] { btnResetToDefaults, btnSetAsDefaults });
            actionCard.Content.Controls.Add(actionFlow);
            actionCard.Height = 34 + Theme.Sizes.ButtonHeight + 16;

            fenceSettingsPanel.Controls.Add(actionCard);
            fenceSettingsPanel.Controls.Add(typeSection);
            fenceSettingsPanel.Controls.Add(basicSection);
            fenceSettingsPanel.Controls.Add(sizeSection);
            fenceSettingsPanel.Controls.Add(colorsSection);
            fenceSettingsPanel.Controls.Add(styleSection);

            CreateTypeSpecificPanels();
        }

        private void CreateTypeSpecificPanels()
        {
            liveFolderPanel = new SettingsSection("Live Folder Settings", 500);

            var browseBtn = Theme.CreateFlatButton("Browse");
            browseBtn.Size = new Size(76, Theme.Sizes.ButtonHeight);
            browseBtn.Click += BtnBrowseWatchPath_Click;

            var watchPathInput = new Panel { Height = Theme.Sizes.InputHeight + 2 };
            txtWatchPath = Theme.CreateTextBox();
            txtWatchPath.Width = 220;
            txtWatchPath.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            browseBtn.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            browseBtn.Location = new Point(228, (watchPathInput.Height - browseBtn.Height) / 2);
            txtWatchPath.Location = new Point(0, (watchPathInput.Height - txtWatchPath.Height) / 2);
            watchPathInput.Controls.Add(browseBtn);
            watchPathInput.Controls.Add(txtWatchPath);
            liveFolderPanel.AddRow(new SettingsRow("Watch Path", watchPathInput, "Folder whose contents appear live in the fence."));

            liveFolderPanel.AddRow(CreateSettingsRow("Recursive", chkWatchRecursive = new ToggleSwitch()));
            txtFileFilter = Theme.CreateTextBox();
            txtFileFilter.Width = 200;
            liveFolderPanel.AddRow(CreateSettingsRow("File Filter", txtFileFilter));
            liveFolderPanel.AddRow(CreateSettingsRow("Max Items", nudLiveFolderMaxItems = Theme.CreateNumericUpDown(1, 500, 50)));

            runningTasksPanel = new SettingsSection("Running Tasks Settings", 500);
            runningTasksPanel.AddRow(CreateSettingsRow("Update Interval (ms)", nudUpdateInterval = Theme.CreateNumericUpDown(500, 30000, 3000)));
            runningTasksPanel.AddRow(CreateSettingsRow("Show Minimized", chkShowMinimizedWindows = new ToggleSwitch { Checked = true }));
            txtProcessFilter = Theme.CreateTextBox();
            txtProcessFilter.Width = 200;
            runningTasksPanel.AddRow(CreateSettingsRow("Process Filter", txtProcessFilter));
            runningTasksPanel.AddRow(CreateSettingsRow("Max Items", nudRunningTasksMaxItems = Theme.CreateNumericUpDown(1, 100, 20)));

            clipboardHistoryPanel = new SettingsSection("Clipboard History Settings", 500);
            clipboardHistoryPanel.AddRow(CreateSettingsRow("Max Items", nudClipboardMaxItems = Theme.CreateNumericUpDown(5, 200, 50)));
            clipboardHistoryPanel.AddRow(CreateSettingsRow("Capture Images", chkCaptureImages = new ToggleSwitch { Checked = true }));

            fenceSettingsPanel.Controls.Add(liveFolderPanel);
            fenceSettingsPanel.Controls.Add(runningTasksPanel);
            fenceSettingsPanel.Controls.Add(clipboardHistoryPanel);
        }

        private void BtnBrowseWatchPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder to watch";
                if (!string.IsNullOrEmpty(txtWatchPath.Text) && System.IO.Directory.Exists(txtWatchPath.Text))
                    dialog.SelectedPath = txtWatchPath.Text;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    txtWatchPath.Text = dialog.SelectedPath;
                    DebounceFenceSettingsSave();
                }
            }
        }

        private void UpdateTypeSpecificVisibility()
        {
            if (cmbFenceType.SelectedIndex < 0) return;
            var type = (FenceType)cmbFenceType.SelectedIndex;

            liveFolderPanel.Visible = type == FenceType.LiveFolder;
            runningTasksPanel.Visible = type == FenceType.RunningTasks;
            clipboardHistoryPanel.Visible = type == FenceType.ClipboardHistory;

            // Re-flow the host so hidden sections don't leave gaps.
            fenceSettingsPanel.PerformLayout();
        }

        #endregion

        #region Helper Methods

        private SettingsRow CreateSettingsRow(string labelText, Control input, string description = null)
        {
            return new SettingsRow(labelText, input, description);
        }

        private SettingsRow CreateHotkeyRow(string labelText, out HotkeyCaptureBox hotkeyBox)
        {
            hotkeyBox = new HotkeyCaptureBox();
            var box = hotkeyBox;

            var input = new Panel { Height = Theme.Sizes.InputHeight + 4 };
            var btnClear = Theme.CreateFlatButton("Clear");
            btnClear.Size = new Size(64, Theme.Sizes.ButtonHeight);

            hotkeyBox.Width = 200;
            hotkeyBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnClear.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            btnClear.Location = new Point(208, (input.Height - btnClear.Height) / 2);
            hotkeyBox.Location = new Point(0, (input.Height - hotkeyBox.Height) / 2);
            btnClear.Click += (s, e) => box.Text = "";

            input.Controls.Add(btnClear);
            input.Controls.Add(hotkeyBox);
            return new SettingsRow(labelText, input);
        }

        private SettingsRow CreateColorRow(string labelText, out Button colorButton, out NumericUpDown transparency, int defaultValue)
        {
            var input = new Panel { Height = Theme.Sizes.InputHeight + 4 };
            var cpb = new ColorPickerButton();
            cpb.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            colorButton = cpb;

            var transLabel = Theme.CreateLabel("Opacity:", Theme.Fonts.Caption, Theme.Colors.TextSecondary);
            transLabel.AutoSize = true;
            transLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            var trans = Theme.CreateNumericUpDown(0, 100, 100);
            trans.Width = 60;
            trans.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            transparency = trans;

            cpb.AlphaSource = () => (int)trans.Value;

            int x = 0;
            cpb.Location = new Point(x, (input.Height - cpb.Height) / 2);
            x += cpb.Width + 10;
            transLabel.Location = new Point(x, (input.Height - transLabel.Height) / 2);
            x += transLabel.Width + 6;
            trans.Location = new Point(x, (input.Height - trans.Height) / 2);
            trans.ValueChanged += (s, e) => cpb.Invalidate();

            input.Controls.Add(trans);
            input.Controls.Add(transLabel);
            input.Controls.Add(cpb);
            return new SettingsRow(labelText, input);
        }

        private void WireAppearancePreview()
        {
            if (_appearancePreview == null) return;
            _appearancePreview.GetState = BuildPreviewState;

            EventHandler invalidate = (s, e) => _appearancePreview?.Invalidate();
            nudDefaultFenceWidth.ValueChanged += invalidate;
            nudDefaultFenceHeight.ValueChanged += invalidate;
            nudDefaultTitleHeight.ValueChanged += invalidate;
            nudDefaultTransparency.ValueChanged += invalidate;
            chkDefaultAutoHide.CheckedChanged += invalidate;
            nudDefaultBackgroundTransparency.ValueChanged += invalidate;
            nudDefaultTitleBackgroundTransparency.ValueChanged += invalidate;
            nudDefaultTextTransparency.ValueChanged += invalidate;
            nudDefaultBorderTransparency.ValueChanged += invalidate;
            nudDefaultBorderWidth.ValueChanged += invalidate;
            nudDefaultCornerRadius.ValueChanged += invalidate;
            chkDefaultShowShadow.CheckedChanged += invalidate;
            cmbDefaultIconSize.SelectedIndexChanged += invalidate;
            nudDefaultItemSpacing.ValueChanged += invalidate;
        }

        private FencePreviewControl.PreviewState BuildPreviewState()
        {
            return new FencePreviewControl.PreviewState
            {
                Background = WithAlpha(btnDefaultBackgroundColor.BackColor, nudDefaultBackgroundTransparency.Value),
                TitleBackground = WithAlpha(btnDefaultTitleBackgroundColor.BackColor, nudDefaultTitleBackgroundTransparency.Value),
                Text = WithAlpha(btnDefaultTextColor.BackColor, nudDefaultTextTransparency.Value),
                Border = WithAlpha(btnDefaultBorderColor.BackColor, nudDefaultBorderTransparency.Value),
                BorderWidth = (float)nudDefaultBorderWidth.Value,
                CornerRadius = (int)nudDefaultCornerRadius.Value,
                ShowShadow = chkDefaultShowShadow.Checked,
                IconSize = int.TryParse(cmbDefaultIconSize.SelectedItem?.ToString(), out var s) ? s : 16,
                ItemSpacing = (int)nudDefaultItemSpacing.Value,
                Title = "Preview Fence"
            };
        }

        private static Color WithAlpha(Color c, decimal transparencyPct)
        {
            int a = (int)Math.Round(255m * (Math.Max(0m, Math.Min(100m, transparencyPct)) / 100m));
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        #endregion

        #region Event Handlers

        private void DebounceGlobalSettingsSave()
        {
            _saveIndicator?.SetPending();
            _globalSettingsDebounce?.Dispose();
            _globalSettingsDebounce = new System.Threading.Timer(_ =>
            {
                try { this.Invoke(new Action(ApplyGlobalSettings)); } catch { }
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void DebounceFenceSettingsSave()
        {
            _saveIndicator?.SetPending();
            _fenceSettingsDebounce?.Dispose();
            _fenceSettingsDebounce = new System.Threading.Timer(_ =>
            {
                try { this.Invoke(new Action(() => ApplyFenceSettings())); } catch { }
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void SetupEventHandlers()
        {
            chkAutoSave.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudAutoSaveInterval.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkShowTooltips.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkEnableAnimations.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkStartWithWindows.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            cmbLogLevel.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkEnableFileLogging.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };

            txtToggleTransparencyShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtToggleAutoHideShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtShowAllFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtCreateNewFenceShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtOpenSettingsShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtToggleLockShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtMinimizeAllFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            txtRefreshFencesShortcut.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };

            nudDefaultFenceWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultFenceHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultTitleHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkDefaultAutoHide.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultAutoHideDelay.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultTitleBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultTextTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultBorderTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultBorderWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultCornerRadius.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            chkDefaultShowShadow.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            cmbDefaultIconSize.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };
            nudDefaultItemSpacing.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceGlobalSettingsSave(); };

            btnDefaultBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultBackgroundColor, nudDefaultBackgroundTransparency, false);
            btnDefaultTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultTitleBackgroundColor, nudDefaultTitleBackgroundTransparency, false);
            btnDefaultTextColor.Click += (s, e) => ShowColorDialog(btnDefaultTextColor, nudDefaultTextTransparency, false);
            btnDefaultBorderColor.Click += (s, e) => ShowColorDialog(btnDefaultBorderColor, nudDefaultBorderTransparency, false);

            btnFenceBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceBackgroundColor, nudFenceBackgroundTransparency, true);
            btnFenceTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceTitleBackgroundColor, nudFenceTitleBackgroundTransparency, true);
            btnFenceTextColor.Click += (s, e) => ShowColorDialog(btnFenceTextColor, nudFenceTextTransparency, true);
            btnFenceBorderColor.Click += (s, e) => ShowColorDialog(btnFenceBorderColor, nudFenceBorderTransparency, true);

            lstFences.DrawItem += LstFences_DrawItem;
            lstFences.SelectedIndexChanged += LstFences_SelectedIndexChanged;

            txtFenceName.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkFenceAutoHide.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceAutoHideDelay.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkFenceLocked.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkFenceCanMinify.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceTitleHeight.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceTitleBackgroundTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceTextTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceBorderTransparency.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceBorderWidth.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceCornerRadius.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkFenceShowShadow.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            cmbFenceIconSize.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudFenceItemSpacing.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };

            txtWatchPath.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkWatchRecursive.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            txtFileFilter.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudLiveFolderMaxItems.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };

            nudUpdateInterval.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkShowMinimizedWindows.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            txtProcessFilter.TextChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            nudRunningTasksMaxItems.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };

            nudClipboardMaxItems.ValueChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
            chkCaptureImages.CheckedChanged += (s, e) => { if (!isUpdatingControls) DebounceFenceSettingsSave(); };
        }

        #endregion

        #region Load / Apply

        private void LoadSettings()
        {
            try
            {
                isUpdatingControls = true;
                logger.Debug("Loading settings into form", "SettingsForm");
                var settings = AppSettings.Instance;

                chkAutoSave.Checked = settings.AutoSave;
                nudAutoSaveInterval.Value = settings.AutoSaveInterval;
                chkShowTooltips.Checked = settings.ShowTooltips;
                chkEnableAnimations.Checked = settings.EnableAnimations;
                chkStartWithWindows.Checked = settings.StartWithWindows;
                cmbLogLevel.SelectedItem = settings.LogLevel;
                chkEnableFileLogging.Checked = settings.EnableFileLogging;

                txtToggleTransparencyShortcut.Text = settings.ToggleTransparencyShortcut;
                txtToggleAutoHideShortcut.Text = settings.ToggleAutoHideShortcut;
                txtShowAllFencesShortcut.Text = settings.ShowAllFencesShortcut;
                txtCreateNewFenceShortcut.Text = settings.CreateNewFenceShortcut;
                txtOpenSettingsShortcut.Text = settings.OpenSettingsShortcut;
                txtToggleLockShortcut.Text = settings.ToggleLockShortcut;
                txtMinimizeAllFencesShortcut.Text = settings.MinimizeAllFencesShortcut;
                txtRefreshFencesShortcut.Text = settings.RefreshFencesShortcut;

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

                cmbFenceType.SelectedIndex = (int)selectedFenceInfo.FenceType;
                UpdateTypeSpecificVisibility();

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

                txtWatchPath.Text = selectedFenceInfo.WatchPath;
                chkWatchRecursive.Checked = selectedFenceInfo.WatchRecursive;
                txtFileFilter.Text = selectedFenceInfo.FileFilter;
                nudLiveFolderMaxItems.Value = selectedFenceInfo.MaxItems > 0 ? selectedFenceInfo.MaxItems : 50;

                nudUpdateInterval.Value = selectedFenceInfo.UpdateInterval > 0 ? selectedFenceInfo.UpdateInterval : 3000;
                chkShowMinimizedWindows.Checked = selectedFenceInfo.ShowMinimizedWindows;
                txtProcessFilter.Text = selectedFenceInfo.ProcessFilter;
                nudRunningTasksMaxItems.Value = selectedFenceInfo.MaxItems > 0 ? selectedFenceInfo.MaxItems : 20;

                nudClipboardMaxItems.Value = selectedFenceInfo.MaxItems > 0 ? selectedFenceInfo.MaxItems : 50;
                chkCaptureImages.Checked = selectedFenceInfo.CaptureImages && selectedFenceInfo.ShowPreviews;
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

        private void ShowColorDialog(Button button, NumericUpDown alphaControl, bool isFenceProperty)
        {
            int initialAlpha = (int)(alphaControl?.Value ?? 100);
            Color initialColor = button.BackColor;

            using (var dialog = new ColorPickerDialog(initialColor, initialAlpha))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    SetColorButton(button, dialog.SelectedColor.ToArgb());
                    if (alphaControl != null)
                    {
                        if (isUpdatingControls == false)
                        {
                            try { alphaControl.Value = dialog.SelectedAlphaPercent; }
                            catch { }
                        }
                    }
                    button.Invalidate();
                    if (!isFenceProperty) _appearancePreview?.Invalidate();

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
            if (button is ColorPickerButton cpb) cpb.Invalidate();
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

                bool previousStartupSetting = settings.StartWithWindows;
                settings.StartWithWindows = chkStartWithWindows.Checked;

                if (previousStartupSetting != settings.StartWithWindows)
                {
                    if (settings.StartWithWindows)
                    {
                        if (!StartupManager.EnableStartup())
                        {
                            logger.Error("Failed to enable startup", "SettingsForm");
                            CustomMessageBox.Show("Failed to enable startup with Windows. Please check the logs for details.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            settings.StartWithWindows = false;
                            isUpdatingControls = true;
                            chkStartWithWindows.Checked = false;
                            isUpdatingControls = false;
                        }
                    }
                    else
                    {
                        if (!StartupManager.DisableStartup())
                        {
                            logger.Error("Failed to disable startup", "SettingsForm");
                            CustomMessageBox.Show("Failed to disable startup with Windows. Please check the logs for details.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            settings.StartWithWindows = true;
                            isUpdatingControls = true;
                            chkStartWithWindows.Checked = true;
                            isUpdatingControls = false;
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
                _saveIndicator?.SetSaved();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply global settings", "SettingsForm", ex);
                _saveIndicator?.SetError();
                CustomMessageBox.Show($"Failed to apply settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFenceSettings()
        {
            if (selectedFenceInfo == null || isUpdatingControls) return;

            try
            {
                selectedFenceInfo.FenceType = cmbFenceType.SelectedIndex >= 0
                    ? (FenceType)cmbFenceType.SelectedIndex
                    : FenceType.Standard;

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

                selectedFenceInfo.WatchPath = txtWatchPath.Text;
                selectedFenceInfo.WatchRecursive = chkWatchRecursive.Checked;
                selectedFenceInfo.FileFilter = txtFileFilter.Text;

                selectedFenceInfo.UpdateInterval = (int)nudUpdateInterval.Value;
                selectedFenceInfo.ShowMinimizedWindows = chkShowMinimizedWindows.Checked;
                selectedFenceInfo.ProcessFilter = txtProcessFilter.Text;

                selectedFenceInfo.CaptureImages = chkCaptureImages.Checked;
                selectedFenceInfo.ShowPreviews = chkCaptureImages.Checked;
                selectedFenceInfo.MaxItems = selectedFenceInfo.FenceType switch
                {
                    FenceType.RunningTasks => (int)nudRunningTasksMaxItems.Value,
                    FenceType.ClipboardHistory => (int)nudClipboardMaxItems.Value,
                    _ => (int)nudLiveFolderMaxItems.Value
                };

                FenceManager.Instance.UpdateFence(selectedFenceInfo);
                FenceManager.Instance.ApplySettingsToFence(selectedFenceInfo);

                logger.Info($"Applied settings to fence '{selectedFenceInfo.Name}'", "SettingsForm");
                _saveIndicator?.SetSaved();
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to apply fence settings for '{selectedFenceInfo?.Name}'", "SettingsForm", ex);
                _saveIndicator?.SetError();
            }
        }

        #endregion

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
                CustomMessageBox.Show($"Failed to apply settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (var nameDialog = new TextDialog("New Fence", "Enter fence name:"))
            {
                if (nameDialog.ShowDialog(this) != DialogResult.OK) return;

                var typeNames = new[] { "Standard", "Live Folder", "Running Tasks", "Clipboard History" };
                using (var typeDialog = new TextDialog("Fence Type", "Type (Standard/Live Folder/Running Tasks/Clipboard History):", "Standard"))
                {
                    if (typeDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var typeText = typeDialog.InputText;
                        FenceType type = FenceType.Standard;
                        for (int i = 0; i < typeNames.Length; i++)
                        {
                            if (typeNames[i].Equals(typeText, StringComparison.OrdinalIgnoreCase))
                            {
                                type = (FenceType)i;
                                break;
                            }
                        }

                        FenceManager.Instance.CreateFence(nameDialog.InputText, type);
                        LoadFences();
                    }
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedFenceInfo == null) return;

            var result = CustomMessageBox.Show(
                $"Delete fence '{selectedFenceInfo.Name}'?\n\nThis will permanently remove the fence and its configuration.",
                "Delete Fence",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var fenceWindow = Application.OpenForms.OfType<FenceWindow>()
                        .FirstOrDefault(f => f.GetFenceInfo().Id == selectedFenceInfo.Id);
                    fenceWindow?.Close();

                    FenceManager.Instance.RemoveFence(selectedFenceInfo);
                    selectedFenceInfo = null;
                    fenceSettingsPanel.Enabled = false;
                    LoadFences();
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to delete fence '{selectedFenceInfo?.Name}'", "SettingsForm", ex);
                    CustomMessageBox.Show($"Failed to delete fence: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.DefaultExt = "json";
                    dialog.FileName = $"Fenceless_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    dialog.Title = "Export Fence Configuration";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var json = FenceManager.Instance.ExportAllFences(false);
                        File.WriteAllText(dialog.FileName, json);
                        CustomMessageBox.Show("Fence configuration exported successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        logger.Info($"Exported fence configuration to {dialog.FileName}", "SettingsForm");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to export fence configuration", "SettingsForm", ex);
                CustomMessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.Title = "Import Fence Configuration";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var json = File.ReadAllText(dialog.FileName);
                        var result = CustomMessageBox.Show(
                            "Import fence configuration?\n\nExisting fences with the same name will be renamed.",
                            "Import Fences",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            var count = FenceManager.Instance.ImportFences(json, false);
                            LoadFences();
                            CustomMessageBox.Show($"Successfully imported {count} fence(s)!", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to import fence configuration", "SettingsForm", ex);
                CustomMessageBox.Show($"Failed to import: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnResetToDefaults_Click(object sender, EventArgs e)
        {
            if (selectedFenceInfo != null)
            {
                var result = CustomMessageBox.Show(
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
                var result = CustomMessageBox.Show(
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
                    LoadSettings();

                    CustomMessageBox.Show("Default settings updated successfully!", "Defaults Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LstFences_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var listBox = (ListBox)sender;
            var fence = (FenceInfo)listBox.Items[e.Index];
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color backgroundColor = selected ? Theme.Colors.SurfaceSelected : Theme.Colors.InputBackground;

            using (var backgroundBrush = new SolidBrush(backgroundColor))
                g.FillRectangle(backgroundBrush, e.Bounds);

            if (selected)
            {
                using (var accentBrush = new SolidBrush(Theme.Colors.Accent))
                    g.FillRectangle(accentBrush, new Rectangle(e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height));
            }

            var typeStyle = GetTypeStyle(fence.FenceType);
            int iconX = e.Bounds.X + 10;
            int iconY = e.Bounds.Y + (e.Bounds.Height - 16) / 2;
            if (!string.IsNullOrEmpty(typeStyle.glyph))
            {
                using (var iconFont = new Font(Theme.IconFontName, 10F))
                {
                    TextRenderer.DrawText(g, typeStyle.glyph, iconFont,
                        new Rectangle(iconX, iconY, 18, 18), typeStyle.color,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            using (var textBrush = new SolidBrush(selected ? Theme.Colors.TextBright : Theme.Colors.TextPrimary))
            {
                var nameRect = new Rectangle(e.Bounds.X + 34, e.Bounds.Y, e.Bounds.Width - 42, e.Bounds.Height);
                TextRenderer.DrawText(g, fence.Name, Theme.Fonts.Body, nameRect,
                    selected ? Theme.Colors.TextBright : Theme.Colors.TextPrimary,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private (string glyph, Color color) GetTypeStyle(FenceType type)
        {
            switch (type)
            {
                case FenceType.LiveFolder: return ("\uE8B7", Theme.Colors.Warning);
                case FenceType.RunningTasks: return ("\uE7A8", Theme.Colors.Accent);
                case FenceType.ClipboardHistory: return ("\uE77F", Theme.Colors.Success);
                default: return ("\uE8FD", Theme.Colors.TextSecondary);
            }
        }

        private void LstFences_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedFenceInfo = lstFences.SelectedItem as FenceInfo;
            fenceSettingsPanel.Enabled = selectedFenceInfo != null;
            LoadFenceSettings();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logger?.Debug("Disposing settings form", "SettingsForm");
                _globalSettingsDebounce?.Dispose();
                _fenceSettingsDebounce?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
