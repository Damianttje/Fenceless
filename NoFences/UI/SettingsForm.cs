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
        private Panel liveFolderPanel;
        private TextBox txtWatchPath;
        private ToggleSwitch chkWatchRecursive;
        private TextBox txtFileFilter;
        private NumericUpDown nudLiveFolderMaxItems;

        private Panel runningTasksPanel;
        private NumericUpDown nudUpdateInterval;
        private ToggleSwitch chkShowMinimizedWindows;
        private TextBox txtProcessFilter;
        private NumericUpDown nudRunningTasksMaxItems;

        private Panel clipboardHistoryPanel;
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

            this.Shown += (s, e) =>
            {
                if (AppSettings.Instance.EnableAnimations)
                    AnimationHelper.FadeIn(this, 200);
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            SetupThemedForm("Fenceless Settings", showMinimize: true, showMaximize: true, sizable: true);
            this.Size = new Size(1200, 700);
            this.MinimumSize = new Size(900, 600);

            CreateLayout();
            SetupEventHandlers();

            this.ResumeLayout(false);
        }

        private void CreateLayout()
        {
            sidebar = new SidebarNavigation();
            sidebar.AddItem("General", "\uE80F");
            sidebar.AddItem("Fences", "\uE8FD");
            sidebar.AddItem("Appearance", "\uE790");
            sidebar.AddItem("Hotkeys", "\uE8C1");
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
                Padding = new Padding(0, 0, 12, 0)
            };

            var btnOK = Theme.CreateFlatButton("OK", Theme.ButtonRole.Accent);
            btnOK.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Click += BtnOK_Click;

            var btnCancel = Theme.CreateFlatButton("Cancel");
            btnCancel.Size = new Size(Theme.Sizes.ButtonWidth, Theme.Sizes.ButtonHeight);
            btnCancel.DialogResult = DialogResult.Cancel;

            var btnExport = Theme.CreateFlatButton("Export");
            btnExport.Size = new Size(80, Theme.Sizes.ButtonHeight);
            btnExport.Click += BtnExport_Click;

            var btnImport = Theme.CreateFlatButton("Import");
            btnImport.Size = new Size(80, Theme.Sizes.ButtonHeight);
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
                Padding = new Padding(16, 8, 16, 16),
                Margin = Padding.Empty
            };

            page.Resize += (s, e) =>
            {
                foreach (Control c in page.Controls)
                    if (c is Panel section)
                        section.Width = page.ClientSize.Width - page.Padding.Horizontal;
            };

            return page;
        }

        private void AddSection(FlowLayoutPanel page, Panel section, int rowCount)
        {
            section.Height = 28 + rowCount * 36 + 12;
            if (page.ClientSize.Width > 0)
                section.Width = page.ClientSize.Width - page.Padding.Horizontal;
            page.Controls.Add(section);
        }

        private void AddColorSection(FlowLayoutPanel page, Panel section, int rowCount)
        {
            section.Height = 28 + (rowCount - 1) * 50 + 38;
            if (page.ClientSize.Width > 0)
                section.Width = page.ClientSize.Width - page.Padding.Horizontal;
            page.Controls.Add(section);
        }

        private ScrollableControl CreateGeneralPage()
        {
            var page = CreateScrollPage();

            var autoSaveSection = Theme.CreateSection("Auto Save", 700);

            chkAutoSave = new ToggleSwitch { Checked = true };
            var autoSaveRow = CreateSettingsRow("Auto Save", chkAutoSave);

            nudAutoSaveInterval = Theme.CreateNumericUpDown(5, 300, 30);
            nudAutoSaveInterval.Width = 100;
            var intervalRow = CreateSettingsRow("Interval (seconds)", nudAutoSaveInterval);

            autoSaveSection.Controls.Add(intervalRow);
            autoSaveSection.Controls.Add(autoSaveRow);

            var behaviorSection = Theme.CreateSection("Behavior", 700);

            chkShowTooltips = new ToggleSwitch { Checked = true };
            chkEnableAnimations = new ToggleSwitch { Checked = true };
            chkStartWithWindows = new ToggleSwitch { Checked = false };

            var tooltipsRow = CreateSettingsRow("Show Tooltips", chkShowTooltips);
            var animRow = CreateSettingsRow("Enable Animations", chkEnableAnimations);
            var startupRow = CreateSettingsRow("Start with Windows", chkStartWithWindows);

            behaviorSection.Controls.Add(startupRow);
            behaviorSection.Controls.Add(animRow);
            behaviorSection.Controls.Add(tooltipsRow);

            var loggingSection = Theme.CreateSection("Logging", 700);

            cmbLogLevel = Theme.CreateComboBox(new[] { "Debug", "Info", "Warning", "Error", "Critical" });
            cmbLogLevel.Width = 120;
            var logLevelRow = CreateSettingsRow("Log Level", cmbLogLevel);

            chkEnableFileLogging = new ToggleSwitch { Checked = true };
            var fileLogRow = CreateSettingsRow("File Logging", chkEnableFileLogging);

            loggingSection.Controls.Add(fileLogRow);
            loggingSection.Controls.Add(logLevelRow);

            AddSection(page, loggingSection, 2);
            AddSection(page, behaviorSection, 3);
            AddSection(page, autoSaveSection, 2);

            return page;
        }

        private ScrollableControl CreateAppearancePage()
        {
            var page = CreateScrollPage();

            var sizeSection = Theme.CreateSection("Default Size", 700);

            nudDefaultFenceWidth = Theme.CreateNumericUpDown(200, 2000, 524);
            nudDefaultFenceWidth.Width = 100;
            nudDefaultFenceHeight = Theme.CreateNumericUpDown(200, 2000, 517);
            nudDefaultFenceHeight.Width = 100;
            nudDefaultTitleHeight = Theme.CreateNumericUpDown(16, 100, 25);
            nudDefaultTitleHeight.Width = 100;

            sizeSection.Controls.Add(CreateSettingsRow("Title Height", nudDefaultTitleHeight));
            sizeSection.Controls.Add(CreateSettingsRow("Height", nudDefaultFenceHeight));
            sizeSection.Controls.Add(CreateSettingsRow("Width", nudDefaultFenceWidth));

            var appearanceSection = Theme.CreateSection("Default Appearance", 700);

            nudDefaultTransparency = Theme.CreateNumericUpDown(25, 100, 80);
            nudDefaultTransparency.Width = 100;
            chkDefaultAutoHide = new ToggleSwitch();
            nudDefaultAutoHideDelay = Theme.CreateNumericUpDown(500, 10000, 2000);
            nudDefaultAutoHideDelay.Width = 100;

            appearanceSection.Controls.Add(CreateSettingsRow("Auto Hide Delay (ms)", nudDefaultAutoHideDelay));
            appearanceSection.Controls.Add(CreateSettingsRow("Auto Hide", chkDefaultAutoHide));
            appearanceSection.Controls.Add(CreateSettingsRow("Transparency (%)", nudDefaultTransparency));

            var colorsSection = Theme.CreateSection("Default Colors", 700);

            (btnDefaultBackgroundColor, nudDefaultBackgroundTransparency) = CreateColorSettingsRow(colorsSection, "Background", 0);
            (btnDefaultTitleBackgroundColor, nudDefaultTitleBackgroundTransparency) = CreateColorSettingsRow(colorsSection, "Title Background", 50);
            (btnDefaultTextColor, nudDefaultTextTransparency) = CreateColorSettingsRow(colorsSection, "Text", 100);
            (btnDefaultBorderColor, nudDefaultBorderTransparency) = CreateColorSettingsRow(colorsSection, "Border", 150);

            var styleSection = Theme.CreateSection("Default Style", 700);

            nudDefaultBorderWidth = Theme.CreateNumericUpDown(0, 10, 0);
            nudDefaultBorderWidth.Width = 100;
            nudDefaultCornerRadius = Theme.CreateNumericUpDown(0, 50, 0);
            nudDefaultCornerRadius.Width = 100;
            chkDefaultShowShadow = new ToggleSwitch { Checked = true };
            cmbDefaultIconSize = Theme.CreateComboBox(new[] { "16", "24", "32", "48", "64" });
            cmbDefaultIconSize.Width = 100;
            nudDefaultItemSpacing = Theme.CreateNumericUpDown(5, 50, 15);
            nudDefaultItemSpacing.Width = 100;

            styleSection.Controls.Add(CreateSettingsRow("Item Spacing", nudDefaultItemSpacing));
            styleSection.Controls.Add(CreateSettingsRow("Icon Size", cmbDefaultIconSize));
            styleSection.Controls.Add(CreateSettingsRow("Show Shadow", chkDefaultShowShadow));
            styleSection.Controls.Add(CreateSettingsRow("Corner Radius", nudDefaultCornerRadius));
            styleSection.Controls.Add(CreateSettingsRow("Border Width", nudDefaultBorderWidth));

            AddSection(page, styleSection, 5);
            AddColorSection(page, colorsSection, 4);
            AddSection(page, appearanceSection, 3);
            AddSection(page, sizeSection, 3);

            return page;
        }

        private ScrollableControl CreateHotkeysPage()
        {
            var page = CreateScrollPage();

            var section = Theme.CreateSection("Global Hotkeys", 700);

            var infoLabel = Theme.CreateLabel("Click a hotkey field and press the desired key combination. Press Escape to clear.", Theme.Fonts.Small, Theme.Colors.TextSecondary);
            infoLabel.Location = new Point(12, 32);
            infoLabel.MaximumSize = new Size(676, 0);
            section.Controls.Add(infoLabel);

            int y = 56;
            int hotkeyWidth = 200;
            int clearWidth = 60;

            txtToggleTransparencyShortcut = CreateHotkeyRow(section, "Toggle Transparency", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtToggleAutoHideShortcut = CreateHotkeyRow(section, "Toggle Auto-Hide", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtShowAllFencesShortcut = CreateHotkeyRow(section, "Show All Fences", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtCreateNewFenceShortcut = CreateHotkeyRow(section, "Create New Fence", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtOpenSettingsShortcut = CreateHotkeyRow(section, "Open Settings", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtToggleLockShortcut = CreateHotkeyRow(section, "Toggle Lock", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtMinimizeAllFencesShortcut = CreateHotkeyRow(section, "Minimize All Fences", 12, y, 700, hotkeyWidth, clearWidth); y += 36;
            txtRefreshFencesShortcut = CreateHotkeyRow(section, "Refresh Fences", 12, y, 700, hotkeyWidth, clearWidth); y += 36;

            section.Height = y + 8;

            if (page.ClientSize.Width > 0)
                section.Width = page.ClientSize.Width - page.Padding.Horizontal;
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
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Colors.BackgroundMid,
                Padding = new Padding(0, 0, 8, 0)
            };

            var listSection = Theme.CreateSection("Active Fences", 230);
            listSection.Dock = DockStyle.Fill;
            listSection.Padding = new Padding(8, 28, 8, 8);

            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Height = Theme.Sizes.ButtonHeight + 4,
                Dock = DockStyle.Top,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 4)
            };

            var btnRefresh = Theme.CreateFlatButton("Refresh");
            btnRefresh.Width = 70;
            btnRefresh.Click += (s, e) => LoadFences();

            var btnHighlight = Theme.CreateFlatButton("Highlight");
            btnHighlight.Width = 70;
            btnHighlight.Click += BtnHighlight_Click;

            var btnAdd = Theme.CreateFlatButton("Add");
            btnAdd.Width = 60;
            btnAdd.Click += BtnAdd_Click;

            var btnDelete = Theme.CreateFlatButton("Delete", Theme.ButtonRole.Danger);
            btnDelete.Width = 60;
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
                ItemHeight = 28
            };
            lstFences.DisplayMember = "Name";

            listSection.Controls.Add(lstFences);
            listSection.Controls.Add(buttonPanel);
            leftPanel.Controls.Add(listSection);

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
                    if (c is Panel section)
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
            var typeSection = Theme.CreateSection("Fence Type", 500);

            cmbFenceType = Theme.CreateComboBox(new[] { "Standard", "Live Folder", "Running Tasks", "Clipboard History" });
            cmbFenceType.Width = 200;
            typeSection.Controls.Add(CreateSettingsRow("Type", cmbFenceType));
            cmbFenceType.SelectedIndexChanged += (s, e) => { if (!isUpdatingControls) UpdateTypeSpecificVisibility(); };

            var basicSection = Theme.CreateSection("Basic Settings", 500);

            txtFenceName = Theme.CreateTextBox();
            txtFenceName.Width = 200;
            basicSection.Controls.Add(CreateSettingsRow("Name", txtFenceName));

            nudFenceTransparency = Theme.CreateNumericUpDown(25, 100, 100);
            nudFenceTransparency.Width = 100;
            basicSection.Controls.Add(CreateSettingsRow("Transparency (%)", nudFenceTransparency));

            chkFenceAutoHide = new ToggleSwitch();
            basicSection.Controls.Add(CreateSettingsRow("Auto Hide", chkFenceAutoHide));

            nudFenceAutoHideDelay = Theme.CreateNumericUpDown(500, 10000, 2000);
            nudFenceAutoHideDelay.Width = 100;
            basicSection.Controls.Add(CreateSettingsRow("Auto Hide Delay (ms)", nudFenceAutoHideDelay));

            chkFenceLocked = new ToggleSwitch();
            chkFenceCanMinify = new ToggleSwitch { Checked = true };
            basicSection.Controls.Add(CreateSettingsRow("Can Minify", chkFenceCanMinify));
            basicSection.Controls.Add(CreateSettingsRow("Locked", chkFenceLocked));

            var sizeSection = Theme.CreateSection("Size Settings", 500);

            nudFenceWidth = Theme.CreateNumericUpDown(200, 2000, 524);
            nudFenceWidth.Width = 100;
            nudFenceHeight = Theme.CreateNumericUpDown(200, 2000, 517);
            nudFenceHeight.Width = 100;
            nudFenceTitleHeight = Theme.CreateNumericUpDown(16, 100, 25);
            nudFenceTitleHeight.Width = 100;

            sizeSection.Controls.Add(CreateSettingsRow("Title Height", nudFenceTitleHeight));
            sizeSection.Controls.Add(CreateSettingsRow("Height", nudFenceHeight));
            sizeSection.Controls.Add(CreateSettingsRow("Width", nudFenceWidth));

            var colorsSection = Theme.CreateSection("Colors", 500);

            (btnFenceBackgroundColor, nudFenceBackgroundTransparency) = CreateColorSettingsRow(colorsSection, "Background", 0);
            (btnFenceTitleBackgroundColor, nudFenceTitleBackgroundTransparency) = CreateColorSettingsRow(colorsSection, "Title Background", 50);
            (btnFenceTextColor, nudFenceTextTransparency) = CreateColorSettingsRow(colorsSection, "Text", 100);
            (btnFenceBorderColor, nudFenceBorderTransparency) = CreateColorSettingsRow(colorsSection, "Border", 150);

            var styleSection = Theme.CreateSection("Style", 500);

            nudFenceBorderWidth = Theme.CreateNumericUpDown(0, 10, 0);
            nudFenceBorderWidth.Width = 100;
            nudFenceCornerRadius = Theme.CreateNumericUpDown(0, 50, 0);
            nudFenceCornerRadius.Width = 100;
            chkFenceShowShadow = new ToggleSwitch { Checked = true };
            cmbFenceIconSize = Theme.CreateComboBox(new[] { "16", "24", "32", "48", "64" });
            cmbFenceIconSize.Width = 100;
            nudFenceItemSpacing = Theme.CreateNumericUpDown(5, 50, 15);
            nudFenceItemSpacing.Width = 100;

            styleSection.Controls.Add(CreateSettingsRow("Item Spacing", nudFenceItemSpacing));
            styleSection.Controls.Add(CreateSettingsRow("Icon Size", cmbFenceIconSize));
            styleSection.Controls.Add(CreateSettingsRow("Show Shadow", chkFenceShowShadow));
            styleSection.Controls.Add(CreateSettingsRow("Corner Radius", nudFenceCornerRadius));
            styleSection.Controls.Add(CreateSettingsRow("Border Width", nudFenceBorderWidth));

            var actionPanel = new Panel
            {
                Height = 40,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };

            btnResetToDefaults = Theme.CreateFlatButton("Reset to Defaults", Theme.ButtonRole.Danger);
            btnResetToDefaults.Size = new Size(140, Theme.Sizes.ButtonHeight);
            btnResetToDefaults.Location = new Point(0, 4);
            btnResetToDefaults.Click += BtnResetToDefaults_Click;

            btnSetAsDefaults = Theme.CreateFlatButton("Set as Defaults");
            btnSetAsDefaults.Size = new Size(140, Theme.Sizes.ButtonHeight);
            btnSetAsDefaults.Location = new Point(150, 4);
            btnSetAsDefaults.Click += BtnSetAsDefaults_Click;

            actionPanel.Controls.AddRange(new Control[] { btnResetToDefaults, btnSetAsDefaults });

            fenceSettingsPanel.Controls.Add(actionPanel);
            AddSection(fenceSettingsPanel, typeSection, 1);
            AddSection(fenceSettingsPanel, basicSection, 6);
            AddSection(fenceSettingsPanel, sizeSection, 3);
            AddColorSection(fenceSettingsPanel, colorsSection, 4);
            AddSection(fenceSettingsPanel, styleSection, 5);

            CreateTypeSpecificPanels();
        }

        private void CreateTypeSpecificPanels()
        {
            liveFolderPanel = Theme.CreateSection("Live Folder Settings", 500);
            txtWatchPath = Theme.CreateTextBox();
            txtWatchPath.Width = 300;
            var btnBrowse = Theme.CreateFlatButton("Browse");
            btnBrowse.Size = new Size(70, Theme.Sizes.ButtonHeight);
            btnBrowse.Click += BtnBrowseWatchPath_Click;

            var watchPathRow = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2)
            };
            var watchPathLabel = Theme.CreateLabel("Watch Path");
            watchPathLabel.Location = new Point(0, (32 - watchPathLabel.Height) / 2);
            watchPathLabel.Width = 160;
            txtWatchPath.Location = new Point(170, (32 - txtWatchPath.Height) / 2);
            btnBrowse.Location = new Point(480, (32 - btnBrowse.Height) / 2);
            watchPathRow.Controls.Add(btnBrowse);
            watchPathRow.Controls.Add(txtWatchPath);
            watchPathRow.Controls.Add(watchPathLabel);
            liveFolderPanel.Controls.Add(watchPathRow);

            chkWatchRecursive = new ToggleSwitch();
            liveFolderPanel.Controls.Add(CreateSettingsRow("Recursive", chkWatchRecursive));

            txtFileFilter = Theme.CreateTextBox();
            txtFileFilter.Width = 200;
            liveFolderPanel.Controls.Add(CreateSettingsRow("File Filter", txtFileFilter));

            nudLiveFolderMaxItems = Theme.CreateNumericUpDown(1, 500, 50);
            nudLiveFolderMaxItems.Width = 100;
            liveFolderPanel.Controls.Add(CreateSettingsRow("Max Items", nudLiveFolderMaxItems));

            liveFolderPanel.Height = 28 + 4 * 36 + 12;
            fenceSettingsPanel.Controls.Add(liveFolderPanel);

            runningTasksPanel = Theme.CreateSection("Running Tasks Settings", 500);
            nudUpdateInterval = Theme.CreateNumericUpDown(500, 30000, 3000);
            nudUpdateInterval.Width = 100;
            runningTasksPanel.Controls.Add(CreateSettingsRow("Update Interval (ms)", nudUpdateInterval));

            chkShowMinimizedWindows = new ToggleSwitch { Checked = true };
            runningTasksPanel.Controls.Add(CreateSettingsRow("Show Minimized", chkShowMinimizedWindows));

            txtProcessFilter = Theme.CreateTextBox();
            txtProcessFilter.Width = 200;
            runningTasksPanel.Controls.Add(CreateSettingsRow("Process Filter", txtProcessFilter));

            nudRunningTasksMaxItems = Theme.CreateNumericUpDown(1, 100, 20);
            nudRunningTasksMaxItems.Width = 100;
            runningTasksPanel.Controls.Add(CreateSettingsRow("Max Items", nudRunningTasksMaxItems));

            runningTasksPanel.Height = 28 + 4 * 36 + 12;
            fenceSettingsPanel.Controls.Add(runningTasksPanel);

            clipboardHistoryPanel = Theme.CreateSection("Clipboard History Settings", 500);
            nudClipboardMaxItems = Theme.CreateNumericUpDown(5, 200, 50);
            nudClipboardMaxItems.Width = 100;
            clipboardHistoryPanel.Controls.Add(CreateSettingsRow("Max Items", nudClipboardMaxItems));

            chkCaptureImages = new ToggleSwitch { Checked = true };
            clipboardHistoryPanel.Controls.Add(CreateSettingsRow("Capture Images", chkCaptureImages));

            clipboardHistoryPanel.Height = 28 + 2 * 36 + 12;
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
        }

        #endregion

        #region Helper Methods

        private Panel CreateSettingsRow(string labelText, Control input)
        {
            var row = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 2)
            };

            var label = Theme.CreateLabel(labelText);
            label.AutoSize = true;
            label.Location = new Point(0, (32 - label.Height) / 2);
            label.Width = 160;

            if (input is ToggleSwitch toggleSwitch)
            {
                input.Dock = DockStyle.Right;
                input.Margin = new Padding(0, 5, 0, 5);
            }
            else
            {
                input.Location = new Point(170, (32 - input.Height) / 2);
            }

            row.Controls.Add(input);
            row.Controls.Add(label);
            return row;
        }

        private HotkeyCaptureBox CreateHotkeyRow(Control parent, string labelText, int x, int y, int sectionWidth, int hotkeyWidth, int clearWidth)
        {
            var label = Theme.CreateLabel(labelText);
            label.Location = new Point(x, y + 4);
            label.Width = 160;

            var hotkeyBox = new HotkeyCaptureBox
            {
                Location = new Point(x + 170, y),
                Width = hotkeyWidth
            };

            var btnClear = Theme.CreateFlatButton("Clear");
            btnClear.Size = new Size(clearWidth, Theme.Sizes.ButtonHeight);
            btnClear.Location = new Point(x + 170 + hotkeyWidth + 8, y);
            btnClear.Click += (s, e) => hotkeyBox.Text = "";

            parent.Controls.Add(btnClear);
            parent.Controls.Add(hotkeyBox);
            parent.Controls.Add(label);

            return hotkeyBox;
        }

        private (Button colorButton, NumericUpDown transparency) CreateColorSettingsRow(Control parent, string labelText, int yOffset)
        {
            int x = 12;
            int y = yOffset + 28;

            var label = Theme.CreateLabel(labelText);
            label.Location = new Point(x, y + 4);
            label.Width = 120;

            var colorButton = Theme.CreateColorSwatchButton();
            colorButton.Location = new Point(x + 130, y);

            var transLabel = Theme.CreateLabel("Opacity (%):", Theme.Fonts.Small, Theme.Colors.TextSecondary);
            transLabel.Location = new Point(x + 260, y + 4);
            transLabel.Width = 70;

            var transparency = Theme.CreateNumericUpDown(0, 100, 100);
            transparency.Width = 60;
            transparency.Location = new Point(x + 340, y);

            parent.Controls.Add(transparency);
            parent.Controls.Add(transLabel);
            parent.Controls.Add(colorButton);
            parent.Controls.Add(label);

            return (colorButton, transparency);
        }

        #endregion

        #region Event Handlers

        private void DebounceGlobalSettingsSave()
        {
            _globalSettingsDebounce?.Dispose();
            _globalSettingsDebounce = new System.Threading.Timer(_ =>
            {
                try { this.Invoke(new Action(ApplyGlobalSettings)); } catch { }
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void DebounceFenceSettingsSave()
        {
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

            btnDefaultBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultBackgroundColor, "DefaultBackgroundColor");
            btnDefaultTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnDefaultTitleBackgroundColor, "DefaultTitleBackgroundColor");
            btnDefaultTextColor.Click += (s, e) => ShowColorDialog(btnDefaultTextColor, "DefaultTextColor");
            btnDefaultBorderColor.Click += (s, e) => ShowColorDialog(btnDefaultBorderColor, "DefaultBorderColor");

            btnFenceBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceBackgroundColor, "BackgroundColor", true);
            btnFenceTitleBackgroundColor.Click += (s, e) => ShowColorDialog(btnFenceTitleBackgroundColor, "TitleBackgroundColor", true);
            btnFenceTextColor.Click += (s, e) => ShowColorDialog(btnFenceTextColor, "TextColor", true);
            btnFenceBorderColor.Click += (s, e) => ShowColorDialog(btnFenceBorderColor, "BorderColor", true);

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
            Theme.UpdateColorSwatch(button, color);
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
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply global settings", "SettingsForm", ex);
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
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to apply fence settings for '{selectedFenceInfo?.Name}'", "SettingsForm", ex);
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

            Color backgroundColor = (e.State & DrawItemState.Selected) != 0
                ? Theme.Colors.SurfaceSelected
                : Theme.Colors.InputBackground;

            using (var backgroundBrush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            var typeNames = new[] { "", "\u25CF ", "\u25B6 ", "\u2630 " };
            var typeLabel = fence.FenceType != FenceType.Standard && (int)fence.FenceType < typeNames.Length
                ? typeNames[(int)fence.FenceType]
                : "";

            using (var textBrush = new SolidBrush(Theme.Colors.TextPrimary))
            {
                e.Graphics.DrawString($"{typeLabel}{fence.Name}", Theme.Fonts.Body, textBrush,
                    new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 16, e.Bounds.Height),
                    StringFormat.GenericDefault);
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
