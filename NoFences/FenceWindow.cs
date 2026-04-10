using Fenceless.Model;
using Fenceless.UI;
using Fenceless.Util;
using Fenceless.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Fenceless.Win32.WindowUtil;
using FormsTimer = System.Windows.Forms.Timer;

namespace Fenceless
{
    public partial class FenceWindow : Form
    {
        private int logicalTitleHeight;
        private int titleHeight;
        private const int titleOffset = 3;
        private const int itemWidth = 75;
        private const int itemHeight = 32 + itemPadding + textHeight;
        private const int textHeight = 35;
        private const int itemPadding = 15;
        private const float shadowDist = 1.5f;

        private readonly FenceInfo fenceInfo;
        private readonly Logger logger;

        private Font titleFont;
        private Font iconFont;

        private string selectedItem;
        private string hoveringItem;
        private readonly HashSet<string> selectedItems = new HashSet<string>();
        private bool shouldUpdateSelection;
        private bool shouldRunDoubleClick;
        private bool hasSelectionUpdated;
        private bool hasHoverUpdated;
        private bool isMinified;
        private int prevHeight;

        private int scrollHeight;
        private int scrollOffset;

        private bool isSearchActive = false;
        private string searchQuery = "";
        private int searchMatchCount = 0;
        private TextBox searchBox;

        // New fields for transparency and autohide
        private bool isAutoHidden = false;
    private FormsTimer autoHideTimer;
        private double normalOpacity = 1.0;
        private bool isMouseInside = false;

        // Internal drag and drop fields
        private bool isDraggingItem = false;
        private string draggingItem = null;
        private Point dragStartPoint;
        private Point dragCurrentPoint;
        private int dragTargetIndex = -1;
        private const int DragThreshold = 5; // Minimum distance to start drag
        
        // Thread-safe icon cache with automatic memory management
        private readonly IconCache iconCache = new IconCache(50);
    private FormsTimer dragRefreshTimer;

        private readonly ThrottledExecution throttledMove = new ThrottledExecution(TimeSpan.FromSeconds(4));
        private readonly ThrottledExecution throttledResize = new ThrottledExecution(TimeSpan.FromSeconds(4));

        private readonly ShellContextMenu shellContextMenu = new ShellContextMenu();

        private readonly ThumbnailProvider thumbnailProvider = new ThumbnailProvider();

        private IFenceProvider fenceProvider;

        private readonly ToolTip itemToolTip = new ToolTip { InitialDelay = 500, ReshowDelay = 200, AutomaticDelay = 500 };

        // Override CreateParams to hide from Alt+Tab and prevent minimize on Show Desktop
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Add WS_EX_TOOLWINDOW to hide from Alt+Tab
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                // Add WS_EX_NOACTIVATE to prevent being minimized on Show Desktop
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                // Remove WS_EX_APPWINDOW to prevent Show Desktop from affecting this window
                cp.ExStyle &= ~0x00040000; // Remove WS_EX_APPWINDOW
                return cp;
            }
        }

        private void ReloadFonts()
        {
            var family = new FontFamily("Segoe UI");
            titleFont = new Font(family, (int)Math.Floor(logicalTitleHeight / 2.0));
            iconFont = new Font(family, 9);
        }

        public FenceWindow(FenceInfo fenceInfo)
        {
            logger = Logger.Instance;
            logger.Debug($"Creating fence window for '{fenceInfo.Name}'", "FenceWindow");
            
            // Set form properties to hide from Alt+Tab before initialization
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            
            InitializeComponent();
            SetupEventHandlers();
            DropShadow.ApplyShadows(this);
            BlurUtil.EnableBlur(Handle);
            
            logicalTitleHeight = (fenceInfo.TitleHeight < 16 || fenceInfo.TitleHeight > 100) ? 35 : fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            
            this.MouseWheel += FenceWindow_MouseWheel;
            thumbnailProvider.IconThumbnailLoaded += ThumbnailProvider_IconThumbnailLoaded;
            shellContextMenu.CustomMenuItemSelected += OnRemoveFromFence;

            ReloadFonts();

            AllowDrop = true;

            this.fenceInfo = fenceInfo;
            Text = fenceInfo.Name;
            Location = new Point(fenceInfo.PosX, fenceInfo.PosY);

            Width = fenceInfo.Width;
            Height = fenceInfo.Height;

            prevHeight = Height;
            lockedToolStripMenuItem.Checked = fenceInfo.Locked;
            minifyToolStripMenuItem.Checked = fenceInfo.CanMinify;

            // Initialize transparency and autohide
            SetTransparency(fenceInfo.Transparency);
            InitializeAutoHide();
            
            Minify();

            InitializeFenceProvider();
            
            logger.Info($"Fence window '{fenceInfo.Name}' created successfully at ({fenceInfo.PosX}, {fenceInfo.PosY})", "FenceWindow");
        }

        private void SetupEventHandlers()
        {
            removeItemToolStripMenuItem.Click += (sender, e) =>
            {
                if (hoveringItem != null)
                    RemoveItem(hoveringItem, confirm: true);
            };

            moveItemUpToolStripMenuItem.Click += (sender, e) =>
            {
                if (hoveringItem != null)
                {
                    try
                    {
                        var currentIndex = fenceInfo.Files.IndexOf(hoveringItem);
                        if (currentIndex > 0)
                        {
                            fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex - 1];
                            fenceInfo.Files[currentIndex - 1] = hoveringItem;
                            
                            Save();
                            Invalidate();
                            logger.Debug($"Moved item up in fence '{fenceInfo.Name}'", "FenceWindow");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to move item up in fence '{fenceInfo.Name}'", "FenceWindow", ex);
                    }
                }
            };

            moveItemDownToolStripMenuItem.Click += (sender, e) =>
            {
                if (hoveringItem != null)
                {
                    try
                    {
                        var currentIndex = fenceInfo.Files.IndexOf(hoveringItem);
                        if (currentIndex >= 0 && currentIndex < fenceInfo.Files.Count - 1)
                        {
                            fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex + 1];
                            fenceInfo.Files[currentIndex + 1] = hoveringItem;
                            
                            Save();
                            Invalidate();
                            logger.Debug($"Moved item down in fence '{fenceInfo.Name}'", "FenceWindow");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to move item down in fence '{fenceInfo.Name}'", "FenceWindow", ex);
                    }
                }
            };
        }

        // Add validation for file operations
        private bool ItemExists(string path)
        {
            try
            {
                var exists = File.Exists(path) || Directory.Exists(path);
                if (!exists)
                {
                    logger.Warning($"Item does not exist: {path}", "FenceWindow");
                }
                return exists;
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking if item exists: {path}", "FenceWindow", ex);
                return false;
            }
        }

        private void ClearOldCacheEntries()
        {
            try
            {
                // Clear the cache - the LRU cache will handle automatic cleanup
                iconCache.ClearCache();
                logger.Debug("Icon cache cleared", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error("Error clearing icon cache", "FenceWindow", ex);
            }
        }

        private void fenceSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.ShowGlobalSettings();
        }

        private void globalSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.ShowGlobalSettings();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.ShowGlobalSettings();
        }

        // Add methods for external control from FenceManager
        public void UpdateAutoHideState()
        {
            if (fenceInfo.AutoHide)
            {
                StartAutoHideTimer();
            }
            else
            {
                ShowFence();
                StopAutoHideTimer();
            }
        }

        public void ApplySettings()
        {
            // Apply transparency
            SetTransparency(fenceInfo.Transparency);

            // Apply auto-hide settings
            autoHideTimer.Interval = fenceInfo.AutoHideDelay;
            UpdateAutoHideState();

            // Apply other settings
            lockedToolStripMenuItem.Checked = fenceInfo.Locked;
            minifyToolStripMenuItem.Checked = fenceInfo.CanMinify;

            // Update title and size if changed
            Text = fenceInfo.Name;
            Width = fenceInfo.Width;
            Height = fenceInfo.Height;
            
            // Update title height if changed
            logicalTitleHeight = fenceInfo.TitleHeight;
            titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
            ReloadFonts();
            
            // Clear icon cache if icon size changed
            if (iconCache.CacheCount > 0)
            {
                ClearIconCache();
            }
            
            // Adjust height if minified
            if (isMinified)
            {
                prevHeight = Height;
                Height = titleHeight;
            }

            Refresh();
            Save();
        }

        private void ClearIconCache()
        {
            try
            {
                logger.Debug($"Clearing icon cache ({iconCache.CacheCount} entries)", "FenceWindow");
                
                iconCache.ClearCache();
            }
            catch (Exception ex)
            {
                logger.Error("Error clearing icon cache", "FenceWindow", ex);
            }
        }

        private void InitializeAutoHide()
        {
            autoHideTimer = new FormsTimer();
            autoHideTimer.Interval = fenceInfo.AutoHideDelay;
            autoHideTimer.Tick += AutoHideTimer_Tick;
        }

        private void SetTransparency(int transparencyPercent)
        {
            // Clamp transparency between 25 and 100
            transparencyPercent = Math.Max(25, Math.Min(100, transparencyPercent));
            fenceInfo.Transparency = transparencyPercent;
            
            normalOpacity = transparencyPercent / 100.0;
            if (!isAutoHidden)
            {
                this.Opacity = normalOpacity;
            }
            
            Save();
        }

        private void AutoHideTimer_Tick(object sender, EventArgs e)
        {
            if (fenceInfo.AutoHide && !isMouseInside && !isMinified)
            {
                HideFence();
            }
            autoHideTimer.Stop();
        }

        private void HideFence()
        {
            if (!isAutoHidden)
            {
                isAutoHidden = true;
                if (AppSettings.Instance.EnableAnimations)
                {
                    var startOpacity = this.Opacity;
                    var targetOpacity = 0.1;
                    var timer = new FormsTimer { Interval = 16 };
                    int steps = 12;
                    int step = 0;
                    double range = targetOpacity - startOpacity;
                    timer.Tick += (s, e) =>
                    {
                        step++;
                        double progress = (double)step / steps;
                        this.Opacity = startOpacity + range * progress;
                        if (step >= steps)
                        {
                            timer.Stop();
                            timer.Dispose();
                            this.Opacity = targetOpacity;
                        }
                    };
                    timer.Start();
                }
                else
                {
                    this.Opacity = 0.1;
                }
            }
        }

        private void ShowFence()
        {
            if (isAutoHidden)
            {
                isAutoHidden = false;
                if (AppSettings.Instance.EnableAnimations)
                {
                    var startOpacity = this.Opacity;
                    var targetOpacity = normalOpacity;
                    var timer = new FormsTimer { Interval = 16 };
                    int steps = 12;
                    int step = 0;
                    double range = targetOpacity - startOpacity;
                    timer.Tick += (s, e) =>
                    {
                        step++;
                        double progress = (double)step / steps;
                        this.Opacity = startOpacity + range * progress;
                        if (step >= steps)
                        {
                            timer.Stop();
                            timer.Dispose();
                            this.Opacity = targetOpacity;
                        }
                    };
                    timer.Start();
                }
                else
                {
                    this.Opacity = normalOpacity;
                }
            }
        }

        private void StartAutoHideTimer()
        {
            if (fenceInfo.AutoHide && !isAutoHidden)
            {
                autoHideTimer.Stop();
                autoHideTimer.Start();
            }
        }

        private void StopAutoHideTimer()
        {
            autoHideTimer.Stop();
        }

        private void InitializeFenceProvider()
        {
            try
            {
                switch (fenceInfo.FenceType)
                {
                    case FenceType.LiveFolder:
                        fenceProvider = new LiveFolderFence(fenceInfo);
                        break;
                    case FenceType.RunningTasks:
                        fenceProvider = new RunningTasksFence(fenceInfo);
                        break;
                    case FenceType.ClipboardHistory:
                        fenceProvider = new ClipboardHistoryFence(fenceInfo);
                        break;
                }

                if (fenceProvider != null)
                {
                    fenceProvider.ItemsChanged += () =>
                    {
                        try
                        {
                            if (IsHandleCreated && !IsDisposed)
                            {
                                BeginInvoke(new Action(() =>
                                {
                                    if (!IsDisposed) Invalidate();
                                }));
                            }
                        }
                        catch { }
                    };
                    logger.Info($"Initialized {fenceInfo.FenceType} provider for fence '{fenceInfo.Name}'", "FenceWindow");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to initialize fence provider for '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            
            // Additional protection: Hide from Alt+Tab after handle is created
            HideFromAltTab(Handle);
            
            // Prevent minimize to survive Show Desktop
            DesktopUtil.PreventMinimize(Handle);

            if (fenceProvider is ClipboardHistoryFence clipboardFence)
            {
                clipboardFence.StartListening(Handle);
            }
            
            logger?.Debug($"Fence window '{fenceInfo?.Name ?? "Unknown"}' configured to prevent minimize", "FenceWindow");
        }

        private void EnsureFenceVisible(bool triggeredByMonitor = false)
        {
            if (IsDisposed || !IsHandleCreated || isAutoHidden)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => EnsureFenceVisible(triggeredByMonitor)));
                }
                catch (ObjectDisposedException)
                {
                    // Window disposed while invoke was pending
                }
                return;
            }

            bool isHidden = !IsWindowVisible(Handle) ||
                            IsIconic(Handle) ||
                            !Visible ||
                            WindowState == FormWindowState.Minimized;

            if (isHidden)
            {
                Visible = true;
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }

                ShowWindow(Handle, SW_SHOWNOACTIVATE);
                SendToDesktopBack();

                if (!triggeredByMonitor)
                {
                    logger?.Debug($"Fence window '{fenceInfo?.Name ?? "Unknown"}' restored after Show Desktop", "FenceWindow");
                }
            }
        }

        public void CheckVisibility()
        {
            EnsureFenceVisible(true);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            DesktopUtil.GlueToDesktop(Handle);
            SendToDesktopBack();
            logger?.Debug($"Fence window '{fenceInfo?.Name ?? "Unknown"}' attached to desktop", "FenceWindow");
        }

        private void SendToDesktopBack()
        {
            SetWindowPos(Handle, HWND_BOTTOM, 0, 0, 0, 0,
                SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        protected override void SetVisibleCore(bool value)
        {
            // Prevent Show Desktop from hiding the window
            // Only allow hiding if we're auto-hiding or being disposed
            if (!value && !isAutoHidden && !this.IsDisposed && this.IsHandleCreated)
            {
                // Ignore hide requests from Show Desktop
                return;
            }
            base.SetVisibleCore(value);
        }

        protected override void WndProc(ref Message m)
        {
            const uint HideWindowFlag = 0x0080;

            // Remove border
            if (m.Msg == 0x0083)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            // Mouse leave
            var myrect = new Rectangle(Location, Size);
            if (m.Msg == 0x02a2 && !myrect.IntersectsWith(new Rectangle(MousePosition, new Size(1, 1))))
            {
                Minify();
            }

            // Prevent maximize/minimize
            if (m.Msg == WM_SYSCOMMAND)
            {
                var command = m.WParam.ToInt32() & 0xFFF0;
                if (command == SC_MAXIMIZE || command == SC_MINIMIZE)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            // Prevent window from being hidden (Show Desktop)
            if (m.Msg == WM_SHOWWINDOW && m.WParam == IntPtr.Zero)
            {
                // Ignore hide commands unless we're auto-hiding or user is closing
                if (!isAutoHidden && !this.IsDisposed)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            // Prevent window position changes that would hide the window (Show Desktop button)
            if (m.Msg == WM_WINDOWPOSCHANGING)
            {
                var wp = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);

                // Check if the window is being moved off-screen or hidden
                if ((wp.flags & HideWindowFlag) != 0)
                {
                    // Remove the hide flag unless we're auto-hiding
                    if (!isAutoHidden && !IsDisposed)
                    {
                        wp.flags &= ~HideWindowFlag;
                        Marshal.StructureToPtr(wp, m.LParam, false);
                    }
                }
            }

            if (m.Msg == WM_SIZE && m.WParam.ToInt32() == SIZE_MINIMIZED)
            {
                EnsureFenceVisible();
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WM_WINDOWPOSCHANGED)
            {
                var wp = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
                if ((wp.flags & HideWindowFlag) != 0 && !isAutoHidden && !IsDisposed)
                {
                    EnsureFenceVisible();
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            if (m.Msg == WM_COMMAND)
            {
                int commandId = m.WParam.ToInt32() & 0xFFFF;
                if ((commandId == MIN_ALL || commandId == MIN_ALL_UNDO) && !isAutoHidden)
                {
                    EnsureFenceVisible();
                    m.Result = IntPtr.Zero;
                    return;
                }
            }

            // Prevent foreground
            if (m.Msg == WM_SETFOCUS)
            {
                SendToDesktopBack();
                return;
            }

            if (m.Msg == 0x031D) // WM_CLIPBOARDUPDATE
            {
                if (fenceProvider is ClipboardHistoryFence clipboardFence)
                {
                    clipboardFence.OnClipboardChanged();
                }
            }

            // Handle DPI change when dragged between monitors with different DPI
            if (m.Msg == 0x02E0) // WM_DPICHANGED
            {
                try
                {
                    var suggestedRect = Marshal.PtrToStructure<Rectangle>(m.LParam);
                    Location = suggestedRect.Location;
                    fenceInfo.PosX = Location.X;
                    fenceInfo.PosY = Location.Y;
                    logicalTitleHeight = fenceInfo.TitleHeight;
                    titleHeight = LogicalToDeviceUnits(logicalTitleHeight);
                    ReloadFonts();
                    iconCache.ClearCache();
                    Save();
                    Invalidate();
                }
                catch (Exception ex)
                {
                    logger.Error($"Error handling DPI change: {ex.Message}", "FenceWindow", ex);
                }
                m.Result = IntPtr.Zero;
                return;
            }

            // Other messages
            base.WndProc(ref m);

            // If not locked and using the left mouse button
            if (MouseButtons == MouseButtons.Right || lockedToolStripMenuItem.Checked)
                return;

            // Then, allow dragging and resizing
            if (m.Msg == WM_NCHITTEST)
            {
                var pt = PointToClient(new Point(m.LParam.ToInt32()));

                // Don't allow form dragging if we're dragging an item
                if (isDraggingItem)
                {
                    m.Result = (IntPtr)HTCLIENT;
                    return;
                }

                if ((int)m.Result == HTCLIENT && pt.Y < titleHeight)     // drag the form
                {
                    m.Result = (IntPtr)HTCAPTION;
                    FenceWindow_MouseEnter(null, null);
                }

                if (pt.X < 10 && pt.Y < 10)
                    m.Result = new IntPtr(HTTOPLEFT);
                else if (pt.X > (Width - 10) && pt.Y < 10)
                    m.Result = new IntPtr(HTTOPRIGHT);
                else if (pt.X < 10 && pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOMLEFT);
                else if (pt.X > (Width - 10) && pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOMRIGHT);
                else if (pt.Y > (Height - 10))
                    m.Result = new IntPtr(HTBOTTOM);
                else if (pt.X < 10)
                    m.Result = new IntPtr(HTLEFT);
                else if (pt.X > (Width - 10))
                    m.Result = new IntPtr(HTRIGHT);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle Escape key to cancel dragging
            if (keyData == Keys.Escape && isDraggingItem)
            {
                CancelDrag();
                return true;
            }

            if (keyData == Keys.Escape && isSearchActive)
            {
                CloseSearch();
                return true;
            }

            if (keyData == (Keys.Control | Keys.F))
            {
                ToggleSearch();
                return true;
            }
            
            if (keyData == Keys.Delete && selectedItems.Count > 0 && !lockedToolStripMenuItem.Checked)
            {
                RemoveSelectedItems();
                return true;
            }
            else if (keyData == Keys.Delete && selectedItem != null && !lockedToolStripMenuItem.Checked)
            {
                RemoveSelectedItem();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Up) && selectedItem != null && !lockedToolStripMenuItem.Checked)
            {
                MoveSelectedItemUp();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Down) && selectedItem != null && !lockedToolStripMenuItem.Checked)
            {
                MoveSelectedItemDown();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RemoveItem(string itemPath, bool confirm = false)
        {
            if (string.IsNullOrEmpty(itemPath)) return;

            try
            {
                if (confirm)
                {
                    var result = MessageBox.Show(this,
                        $"Remove '{Path.GetFileName(itemPath)}' from this fence?\n\nThis will not delete the file, only remove it from the fence.",
                        "Remove Item",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes) return;
                }

                fenceInfo.Files.Remove(itemPath);
                if (hoveringItem == itemPath) hoveringItem = null;
                if (selectedItem == itemPath) selectedItem = null;
                iconCache.ClearCache();
                Save();
                Invalidate();
                logger.Info($"Removed item from fence '{fenceInfo.Name}'", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to remove item from fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        private void RemoveSelectedItem()
        {
            if (selectedItem != null)
                RemoveItem(selectedItem, confirm: false);
        }

        private void MoveSelectedItemUp()
        {
            if (selectedItem != null)
            {
                try
                {
                    var currentIndex = fenceInfo.Files.IndexOf(selectedItem);
                    if (currentIndex > 0)
                    {
                        fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex - 1];
                        fenceInfo.Files[currentIndex - 1] = selectedItem;
                        Save();
                        Refresh();
                        logger.Debug($"Moved selected item up in fence '{fenceInfo.Name}' via keyboard", "FenceWindow");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to move selected item up in fence '{fenceInfo.Name}'", "FenceWindow", ex);
                }
            }
        }

        private void MoveSelectedItemDown()
        {
            if (selectedItem != null)
            {
                try
                {
                    var currentIndex = fenceInfo.Files.IndexOf(selectedItem);
                    if (currentIndex >= 0 && currentIndex < fenceInfo.Files.Count - 1)
                    {
                        fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex + 1];
                        fenceInfo.Files[currentIndex + 1] = selectedItem;
                        Save();
                        Refresh();
                        logger.Debug($"Moved selected item down in fence '{fenceInfo.Name}' via keyboard", "FenceWindow");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to move selected item down in fence '{fenceInfo.Name}'", "FenceWindow", ex);
                }
            }
        }

        private void ToggleTransparency()
        {
            // Cycle through transparency levels: 100 -> 75 -> 50 -> 25 -> 100
            int newTransparency;
            switch (fenceInfo.Transparency)
            {
                case 100:
                    newTransparency = 75;
                    break;
                case 75:
                    newTransparency = 50;
                    break;
                case 50:
                    newTransparency = 25;
                    break;
                default:
                    newTransparency = 100;
                    break;
            }
            SetTransparency(newTransparency);
        }

        private void ShowAllFences()
        {
            // This will be implemented in FenceManager
            FenceManager.Instance.ShowAllFences();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Really remove this fence?", "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                FenceManager.Instance.RemoveFence(fenceInfo);
                Close();
            }
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hoveringItem != null)
                RemoveItem(hoveringItem, confirm: false);
        }

        private void sortByNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ascending = !fenceInfo.SortAscending && fenceInfo.SortColumn == "name";
            SortFiles("name", ascending);
        }

        private void sortByTypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ascending = !fenceInfo.SortAscending && fenceInfo.SortColumn == "type";
            SortFiles("type", ascending);
        }

        private void sortByDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ascending = !fenceInfo.SortAscending && fenceInfo.SortColumn == "date";
            SortFiles("date", ascending);
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleSearch();
        }

        private void resetPositionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetPosition();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var hasHoveringItem = hoveringItem != null;
            var itemIndex = hasHoveringItem ? fenceInfo.Files.IndexOf(hoveringItem) : -1;
            
            deleteItemToolStripMenuItem.Visible = hasHoveringItem;
            removeItemToolStripMenuItem.Visible = hasHoveringItem;
            moveItemUpToolStripMenuItem.Visible = hasHoveringItem && itemIndex > 0;
            moveItemDownToolStripMenuItem.Visible = hasHoveringItem && itemIndex < fenceInfo.Files.Count - 1;
            toolStripSeparator3.Visible = hasHoveringItem;

            sortByNameToolStripMenuItem.Text = fenceInfo.SortColumn == "name"
                ? (fenceInfo.SortAscending ? "Sort by Name  \u2191" : "Sort by Name  \u2193")
                : "Sort by Name";
            sortByTypeToolStripMenuItem.Text = fenceInfo.SortColumn == "type"
                ? (fenceInfo.SortAscending ? "Sort by Type  \u2191" : "Sort by Type  \u2193")
                : "Sort by Type";
            sortByDateToolStripMenuItem.Text = fenceInfo.SortColumn == "date"
                ? (fenceInfo.SortAscending ? "Sort by Date Modified  \u2191" : "Sort by Date Modified  \u2193")
                : "Sort by Date Modified";
            searchToolStripMenuItem.Text = isSearchActive ? "Close Search    Esc" : "Search...    Ctrl+F";
        }

        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (!lockedToolStripMenuItem.Checked)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent("FencelessItemPaths"))
                    e.Effect = DragDropEffects.Move;
            }
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var addedFiles = 0;

                if (e.Data.GetDataPresent("FencelessItemPaths"))
                {
                    var data = e.Data.GetData("FencelessItemPaths") as string;
                    if (!string.IsNullOrEmpty(data))
                    {
                        var paths = data.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var file in paths)
                        {
                            if (!fenceInfo.Files.Contains(file) && ItemExists(file))
                            {
                                fenceInfo.Files.Add(file);
                                addedFiles++;
                            }
                        }
                    }
                }
                else
                {
                    var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                    logger.Debug($"Processing {dropped.Length} dropped files", "FenceWindow");
                    
                    foreach (var file in dropped)
                    {
                        if (!fenceInfo.Files.Contains(file) && ItemExists(file))
                        {
                            fenceInfo.Files.Add(file);
                            addedFiles++;
                            logger.Debug($"Added file to fence: {file}", "FenceWindow");
                        }
                        else
                        {
                            logger.Debug($"Skipped file (already exists or invalid): {file}", "FenceWindow");
                        }
                    }
                }
                
                if (addedFiles > 0)
                {
                    logger.Info($"Added {addedFiles} files to fence '{fenceInfo.Name}'", "FenceWindow");
                    Save();
                    Refresh();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to process dropped files for fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        private void FenceWindow_Resize(object sender, EventArgs e)
        {
            throttledResize.Run(() =>
            {
                fenceInfo.Width = Width;
                fenceInfo.Height = isMinified ? prevHeight : Height;
                Save();
            });

            Refresh();
        }

        private void FenceWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // Handle internal item dragging
            if (isDraggingItem && !lockedToolStripMenuItem.Checked)
            {
                dragCurrentPoint = e.Location;
                
                // Update target position for drop indicator
                UpdateDragTarget(e.Location);
                
                // Use throttled refresh during drag to prevent excessive repainting
                if (dragRefreshTimer == null)
                {
                    dragRefreshTimer = new FormsTimer();
                    dragRefreshTimer.Interval = 16; // ~60 FPS max
                    dragRefreshTimer.Tick += (s, args) =>
                    {
                        if (isDraggingItem)
                        {
                            Invalidate();
                        }
                        else
                        {
                            dragRefreshTimer.Stop();
                            dragRefreshTimer.Dispose();
                            dragRefreshTimer = null;
                        }
                    };
                    dragRefreshTimer.Start();
                }
                return;
            }
            
            // Check if we should start dragging
            if (e.Button == MouseButtons.Left && !isDraggingItem && selectedItem != null && !lockedToolStripMenuItem.Checked)
            {
                // Only start drag if the item still exists
                if (ItemExists(selectedItem))
                {
                    var dragDistance = Math.Sqrt(Math.Pow(e.X - dragStartPoint.X, 2) + Math.Pow(e.Y - dragStartPoint.Y, 2));
                    if (dragDistance >= DragThreshold)
                    {
                        StartItemDrag(selectedItem, e.Location);
                        return;
                    }
                }
                else
                {
                    // Item no longer exists, clear selection
                    logger.Warning($"Selected item no longer exists: {selectedItem}", "FenceWindow");
                    fenceInfo.Files.Remove(selectedItem);
                    selectedItem = null;
                    Save();
                    Refresh();
                }
            }
            
            // Only refresh if not dragging to avoid excessive repaints
            if (!isDraggingItem)
            {
                Invalidate();
            }
        }

        private void FenceWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !lockedToolStripMenuItem.Checked)
            {
                dragStartPoint = e.Location;
                
                var itemPath = GetItemAtPosition(e.Location);
                if (itemPath != null && ItemExists(itemPath))
                {
                    if (ModifierKeys.HasFlag(Keys.Control))
                    {
                        if (selectedItems.Contains(itemPath))
                            selectedItems.Remove(itemPath);
                        else
                            selectedItems.Add(itemPath);
                        selectedItem = itemPath;
                    }
                    else if (ModifierKeys.HasFlag(Keys.Shift) && selectedItem != null)
                    {
                        var startIdx = fenceInfo.Files.IndexOf(selectedItem);
                        var endIdx = fenceInfo.Files.IndexOf(itemPath);
                        if (startIdx >= 0 && endIdx >= 0)
                        {
                            selectedItems.Clear();
                            var min = Math.Min(startIdx, endIdx);
                            var max = Math.Max(startIdx, endIdx);
                            for (int i = min; i <= max; i++)
                                selectedItems.Add(fenceInfo.Files[i]);
                        }
                        selectedItem = itemPath;
                    }
                    else
                    {
                        selectedItems.Clear();
                        selectedItems.Add(itemPath);
                        selectedItem = itemPath;
                    }
                    Refresh();
                }
                else if (itemPath != null)
                {
                    logger.Warning($"Item no longer exists, removing from fence: {itemPath}", "FenceWindow");
                    fenceInfo.Files.Remove(itemPath);
                    selectedItem = null;
                    selectedItems.Clear();
                    Save();
                    Refresh();
                }
                else
                {
                    selectedItem = null;
                    selectedItems.Clear();
                    Refresh();
                }
            }
        }

        private void FenceWindow_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDraggingItem && e.Button == MouseButtons.Left)
            {
                CompleteDrag(e.Location);
            }
        }

        private void FenceWindow_MouseEnter(object sender, EventArgs e)
        {
            isMouseInside = true;
            StopAutoHideTimer();
            ShowFence();

            if (minifyToolStripMenuItem.Checked && isMinified)
            {
                isMinified = false;
                Height = prevHeight;
            }
        }

        private void FenceWindow_MouseLeave(object sender, EventArgs e)
        {
            isMouseInside = false;
            StartAutoHideTimer();
            Minify();
            
            // Cancel drag operation if mouse leaves the window
            if (isDraggingItem)
            {
                CancelDrag();
            }
            
            selectedItem = null;
            selectedItems.Clear();
            Refresh();
        }

        private void CompleteDrag(Point dropLocation)
        {
            if (!isDraggingItem || draggingItem == null) return;
            
            try
            {
                // Verify the dragged item still exists
                if (!ItemExists(draggingItem))
                {
                    logger.Warning($"Dragged item no longer exists: {draggingItem}", "FenceWindow");
                    fenceInfo.Files.Remove(draggingItem);
                    selectedItem = null;
                    Save();
                    return;
                }
                
                var currentIndex = fenceInfo.Files.IndexOf(draggingItem);
                var targetIndex = GetGridPositionIndex(dropLocation);
                
                // Clamp target index to valid range
                targetIndex = Math.Max(0, Math.Min(targetIndex, fenceInfo.Files.Count - 1));
                
                if (currentIndex != targetIndex && currentIndex >= 0)
                {
                    // Remove item from current position
                    fenceInfo.Files.RemoveAt(currentIndex);
                    
                    // Adjust target index if we removed an item before it
                    if (targetIndex > currentIndex)
                        targetIndex--;
                    
                    // Insert item at new position
                    fenceInfo.Files.Insert(targetIndex, draggingItem);
                    
                    // Update selection to follow the moved item
                    selectedItem = draggingItem;
                    
                    Save();
                    logger.Info($"Moved item '{Path.GetFileName(draggingItem)}' from position {currentIndex} to {targetIndex} in fence '{fenceInfo.Name}'", "FenceWindow");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to complete drag operation in fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
            finally
            {
                // Reset drag state
                isDraggingItem = false;
                draggingItem = null;
                dragTargetIndex = -1;
                this.Cursor = Cursors.Default;
                
                // Stop drag refresh timer
                if (dragRefreshTimer != null)
                {
                    dragRefreshTimer.Stop();
                    dragRefreshTimer.Dispose();
                    dragRefreshTimer = null;
                }
                
                // Restore original title
                this.Text = fenceInfo.Name;
                
                // Force a final refresh
                Invalidate();
            }
        }

        private void CancelDrag()
        {
            if (isDraggingItem)
            {
                logger.Debug($"Cancelled drag operation for item '{Path.GetFileName(draggingItem)}' in fence '{fenceInfo.Name}'", "FenceWindow");
                
                isDraggingItem = false;
                draggingItem = null;
                dragTargetIndex = -1;
                this.Cursor = Cursors.Default;
                
                // Stop drag refresh timer
                if (dragRefreshTimer != null)
                {
                    dragRefreshTimer.Stop();
                    dragRefreshTimer.Dispose();
                    dragRefreshTimer = null;
                }
                
                // Restore original title
                this.Text = fenceInfo.Name;
                
                Refresh();
            }
        }

        private void Minify()
        {
            if (minifyToolStripMenuItem.Checked && !isMinified)
            {
                isMinified = true;
                prevHeight = Height;
                Height = titleHeight;
                Refresh();
            }
        }

        private void minifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isMinified)
            {
                Height = prevHeight;
                isMinified = false;
            }
            fenceInfo.CanMinify = minifyToolStripMenuItem.Checked;
            Save();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                logger?.Debug("Disposing fence window", "FenceWindow");
                
                // Dispose icon cache (handles all cached bitmaps)
                iconCache?.Dispose();
                
                // Dispose timers
                autoHideTimer?.Dispose();
                dragRefreshTimer?.Dispose();
                
                // Dispose fonts
                titleFont?.Dispose();
                iconFont?.Dispose();
                
                // Dispose other resources
                thumbnailProvider?.Dispose();
                itemToolTip?.Dispose();
                searchBox?.Dispose();
                throttledMove?.Dispose();
                throttledResize?.Dispose();
                fenceProvider?.Dispose();
                // Note: ShellContextMenu doesn't implement IDisposable
            }
            base.Dispose(disposing);
        }

        private void FenceWindow_Click(object sender, EventArgs e)
        {
            // Only handle selection if we're not dragging
            if (!isDraggingItem)
            {
                shouldUpdateSelection = true;
                Refresh();
            }
        }

        private void FenceWindow_DoubleClick(object sender, EventArgs e)
        {
            if (!isDraggingItem && selectedItem != null)
            {
                var mousePos = PointToClient(MousePosition);
                var itemPath = GetItemAtPosition(mousePos);
                
                if (itemPath != null && itemPath == selectedItem)
                {
                    if (itemPath.StartsWith("task:"))
                    {
                        if (RunningTasksFence.TryBringToFront(itemPath))
                        {
                            logger.Info($"Brought window to front in fence '{fenceInfo.Name}'", "FenceWindow");
                            return;
                        }
                    }
                    else if (itemPath.StartsWith("clip:"))
                    {
                        var parts = itemPath.Split(new[] { ':' }, 3);
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int index))
                        {
                            var clipboardFence = fenceProvider as ClipboardHistoryFence;
                            var text = clipboardFence?.GetClipboardText(index);
                            if (text != null)
                            {
                                try { Clipboard.SetText(text); }
                                catch { }
                                logger.Info($"Copied clipboard history item to clipboard in fence '{fenceInfo.Name}'", "FenceWindow");
                                return;
                            }
                        }
                    }
                    else if (ItemExists(itemPath))
                    {
                        var entry = FenceEntry.FromPath(itemPath);
                        if (entry != null)
                        {
                            logger.Info($"Double-clicked item '{System.IO.Path.GetFileName(itemPath)}' in fence '{fenceInfo.Name}'", "FenceWindow");
                            entry.Open();
                            return;
                        }
                    }
                    else if (!itemPath.StartsWith("task:") && !itemPath.StartsWith("clip:"))
                    {
                        logger.Warning($"Double-clicked item no longer exists, removing: {itemPath}", "FenceWindow");
                        fenceInfo.Files.Remove(itemPath);
                        selectedItem = null;
                        Save();
                        Refresh();
                    }
                }
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new UI.TextDialog("Edit Name", "New name:", Text);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Text = dialog.InputText;
                fenceInfo.Name = Text;
                Refresh();
                Save();
            }
        }

        private void newFenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("New Fence", FenceType.Standard);
        }

        private void newLiveFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("Live Folder", FenceType.LiveFolder);
        }

        private void newRunningTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("Running Tasks", FenceType.RunningTasks);
        }

        private void newClipboardHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FenceManager.Instance.CreateFence("Clipboard History", FenceType.ClipboardHistory);
        }

        private void FenceWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Application.OpenForms.Count == 0)
                Application.Exit();
        }

        // Add method to expose FenceInfo for manager
        public FenceInfo GetFenceInfo()
        {
            return fenceInfo;
        }

        // Method to update fence info from external source (like settings)
        public void UpdateFenceInfo(FenceInfo updatedInfo)
        {
            try
            {
                logger.Debug($"Updating fence info for '{fenceInfo.Name}' -> '{updatedInfo.Name}'", "FenceWindow");
                
                // Update the fence info properties
                fenceInfo.Name = updatedInfo.Name;
                fenceInfo.Transparency = updatedInfo.Transparency;
                fenceInfo.AutoHide = updatedInfo.AutoHide;
                fenceInfo.AutoHideDelay = updatedInfo.AutoHideDelay;
                fenceInfo.Locked = updatedInfo.Locked;
                fenceInfo.CanMinify = updatedInfo.CanMinify;
                fenceInfo.Width = updatedInfo.Width;
                fenceInfo.Height = updatedInfo.Height;
                fenceInfo.TitleHeight = updatedInfo.TitleHeight;
                fenceInfo.PosX = updatedInfo.PosX;
                fenceInfo.PosY = updatedInfo.PosY;
                
                // Update color and style properties
                fenceInfo.BackgroundColor = updatedInfo.BackgroundColor;
                fenceInfo.TitleBackgroundColor = updatedInfo.TitleBackgroundColor;
                fenceInfo.TextColor = updatedInfo.TextColor;
                fenceInfo.BorderColor = updatedInfo.BorderColor;
                fenceInfo.BackgroundTransparency = updatedInfo.BackgroundTransparency;
                fenceInfo.TitleBackgroundTransparency = updatedInfo.TitleBackgroundTransparency;
                fenceInfo.TextTransparency = updatedInfo.TextTransparency;
                fenceInfo.BorderTransparency = updatedInfo.BorderTransparency;
                fenceInfo.BorderWidth = updatedInfo.BorderWidth;
                fenceInfo.CornerRadius = updatedInfo.CornerRadius;
                fenceInfo.ShowShadow = updatedInfo.ShowShadow;
                fenceInfo.IconSize = updatedInfo.IconSize;
                fenceInfo.ItemSpacing = updatedInfo.ItemSpacing;
                
                fenceInfo.FenceType = updatedInfo.FenceType;
                fenceInfo.WatchPath = updatedInfo.WatchPath;
                fenceInfo.WatchRecursive = updatedInfo.WatchRecursive;
                fenceInfo.FileFilter = updatedInfo.FileFilter;
                fenceInfo.MaxItems = updatedInfo.MaxItems;
                fenceInfo.UpdateInterval = updatedInfo.UpdateInterval;
                fenceInfo.ShowMinimizedWindows = updatedInfo.ShowMinimizedWindows;
                fenceInfo.ProcessFilter = updatedInfo.ProcessFilter;
                fenceInfo.CaptureImages = updatedInfo.CaptureImages;
                
                logger.Info($"Fence info updated for '{fenceInfo.Name}'", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to update fence info for '{fenceInfo?.Name}'", "FenceWindow", ex);
            }
        }

        // Methods for external control
        public void ForceShow()
        {
            ShowFence();
            StopAutoHideTimer();
        }

        public void ForceHide()
        {
            HideFence();
        }

        public void CycleTransparency()
        {
            ToggleTransparency();
        }

        public void RefreshFence()
        {
            fenceProvider?.Refresh();
            Invalidate();
        }

        public void ClampToScreen()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;

                var rect = new Rectangle(Location, Size);
                var screenBounds = Screen.FromRectangle(rect).Bounds;

                if (!screenBounds.Contains(rect) && !screenBounds.IntersectsWith(rect))
                {
                    var primaryScreen = Screen.PrimaryScreen;
                    if (primaryScreen != null)
                    {
                        var bounds = primaryScreen.Bounds;
                        var newX = Math.Max(bounds.X, Math.Min(Location.X, bounds.Right - Width));
                        var newY = Math.Max(bounds.Y, Math.Min(Location.Y, bounds.Bottom - Height));
                        Location = new Point(newX, newY);
                        fenceInfo.PosX = Location.X;
                        fenceInfo.PosY = Location.Y;
                        Save();
                        logger.Info($"Clamped fence '{fenceInfo.Name}' to screen at ({Location.X}, {Location.Y})", "FenceWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error clamping fence to screen: {ex.Message}", "FenceWindow", ex);
            }
        }

        public void ResetPosition()
        {
            try
            {
                var primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    var bounds = primaryScreen.WorkingArea;
                    Location = new Point(bounds.X + 100 + (Width % 300), bounds.Y + 100 + (Height % 200));
                    fenceInfo.PosX = Location.X;
                    fenceInfo.PosY = Location.Y;
                    Save();
                    logger.Info($"Reset fence '{fenceInfo.Name}' position to ({Location.X}, {Location.Y})", "FenceWindow");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error resetting fence position: {ex.Message}", "FenceWindow", ex);
            }
        }

        public void HighlightFence()
        {
            try
            {
                logger.Debug($"Highlighting fence '{fenceInfo.Name}'", "FenceWindow");
                
                // Bring the fence to front and show it
                ForceShow();
                this.BringToFront();
                this.Focus();
                
                // Create a highlight effect by temporarily changing the border
                var originalOpacity = this.Opacity;
                var highlightTimer = new FormsTimer();
                var flashCount = 0;
                
                highlightTimer.Interval = 200;
                highlightTimer.Tick += (s, e) =>
                {
                    flashCount++;
                    if (flashCount % 2 == 0)
                    {
                        this.Opacity = originalOpacity;
                    }
                    else
                    {
                        this.Opacity = Math.Min(1.0, originalOpacity + 0.3);
                    }
                    
                    if (flashCount >= 6) // Flash 3 times
                    {
                        this.Opacity = originalOpacity;
                        highlightTimer.Stop();
                        highlightTimer.Dispose();
                    }
                };
                
                highlightTimer.Start();
                
                logger.Info($"Fence '{fenceInfo.Name}' highlighted", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to highlight fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        // Improve the Save method with better error handling
        private readonly object saveLock = new object();
        
        /// <summary>
        /// Validates all items in the fence and removes any that no longer exist
        /// </summary>
        /// <returns>Number of items removed</returns>
        private int ValidateAndCleanupItems()
        {
            try
            {
                var itemsToRemove = new List<string>();
                
                foreach (var file in fenceInfo.Files)
                {
                    if (!ItemExists(file))
                    {
                        itemsToRemove.Add(file);
                    }
                }
                
                if (itemsToRemove.Count > 0)
                {
                    foreach (var item in itemsToRemove)
                    {
                        fenceInfo.Files.Remove(item);
                        logger.Info($"Removed invalid item from fence '{fenceInfo.Name}': {item}", "FenceWindow");
                    }
                    
                    // Clear selection if it was removed
                    if (selectedItem != null && itemsToRemove.Contains(selectedItem))
                    {
                        selectedItem = null;
                    }
                    
                    return itemsToRemove.Count;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error($"Error validating items in fence '{fenceInfo.Name}'", "FenceWindow", ex);
                return 0;
            }
        }
        
        private void Save()
        {
            lock (saveLock)
            {
                try
                {
                    FenceManager.Instance.UpdateFence(fenceInfo);
                    logger.Debug($"Fence '{fenceInfo.Name}' saved successfully", "FenceWindow");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to save fence '{fenceInfo.Name}'", "FenceWindow", ex);
                }
            }
        }

        private void FenceWindow_LocationChanged(object sender, EventArgs e)
        {
            throttledMove.Run(() =>
            {
                fenceInfo.PosX = Location.X;
                fenceInfo.PosY = Location.Y;
                Save();
            });
        }

        private void lockedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fenceInfo.Locked = lockedToolStripMenuItem.Checked;
            Save();
        }

        private void FenceWindow_Load(object sender, EventArgs e)
        {
            // Validate items when the fence loads
            var removedCount = ValidateAndCleanupItems();
            if (removedCount > 0)
            {
                logger.Info($"Cleaned up {removedCount} invalid items from fence '{fenceInfo.Name}' on load", "FenceWindow");
                Save();
                Refresh();
            }
        }

        private void titleSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Redirect to global settings where title height can be configured
            FenceManager.Instance.ShowGlobalSettings();
        }

        private void FenceWindow_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (hoveringItem != null && !ModifierKeys.HasFlag(Keys.Shift))
            {
                shellContextMenu.ShowContextMenu(
                    new[] { new FileInfo(hoveringItem) }, 
                    MousePosition, 
                    (filePath) => "Remove from fence"
                );
            }
            else
            {
                appContextMenu.Show(this, e.Location);
            }
        }

        private void OnRemoveFromFence(object sender, CustomMenuEventArgs e)
        {
            try
            {
                var filePath = e.FilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    logger.Warning("Remove from fence called with empty file path", "FenceWindow");
                    return;
                }

                logger.Info($"Removing '{Path.GetFileName(filePath)}' from fence '{fenceInfo.Name}' via context menu", "FenceWindow");
                RemoveItem(filePath, confirm: false);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Failed to remove item from fence via context menu", true);
            }
        }

        private void FenceWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            if (scrollHeight < 1)
                return;

            scrollOffset -= Math.Sign(e.Delta) * 10;
            if (scrollOffset < 0)
                scrollOffset = 0;
            if (scrollOffset > scrollHeight)
                scrollOffset = scrollHeight;

            Invalidate();
        }

        private void ThumbnailProvider_IconThumbnailLoaded(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void RemoveSelectedItems()
        {
            if (selectedItems.Count == 0) return;
            try
            {
                var count = selectedItems.Count;
                foreach (var item in selectedItems.ToList())
                    fenceInfo.Files.Remove(item);
                if (selectedItems.Contains(hoveringItem)) hoveringItem = null;
                if (selectedItems.Contains(selectedItem)) selectedItem = null;
                selectedItems.Clear();
                iconCache.ClearCache();
                Save();
                Invalidate();
                logger.Info($"Removed {count} items from fence '{fenceInfo.Name}'", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to remove selected items from fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        private void ToggleSearch()
        {
            if (isSearchActive)
            {
                CloseSearch();
            }
            else
            {
                OpenSearch();
            }
        }

        private void OpenSearch()
        {
            if (searchBox == null)
            {
                searchBox = new TextBox
                {
                    Width = 150,
                    Height = 22,
                    Font = new Font("Segoe UI", 9),
                    BorderStyle = BorderStyle.FixedSingle
                };
                searchBox.TextChanged += SearchBox_TextChanged;
                searchBox.KeyDown += SearchBox_KeyDown;
                this.Controls.Add(searchBox);
            }

            isSearchActive = true;
            searchBox.Text = searchQuery;
            searchBox.Location = new Point(Width - searchBox.Width - 8, (titleHeight - searchBox.Height) / 2);
            searchBox.Visible = true;
            searchBox.BringToFront();
            searchBox.Focus();
            Invalidate();
        }

        private void CloseSearch()
        {
            isSearchActive = false;
            searchQuery = "";
            searchMatchCount = 0;
            fenceInfo.SearchFilter = "";
            if (searchBox != null)
            {
                searchBox.Visible = false;
            }
            Save();
            Invalidate();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            searchQuery = searchBox.Text;
            fenceInfo.SearchFilter = searchQuery;
            searchMatchCount = string.IsNullOrEmpty(searchQuery)
                ? fenceInfo.Files.Count
                : fenceInfo.Files.Count(f => Path.GetFileName(f).IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            Invalidate();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseSearch();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                CloseSearch();
                e.Handled = true;
            }
        }

        private List<string> GetFilteredFiles()
        {
            if (string.IsNullOrEmpty(searchQuery))
                return fenceInfo.Files;

            return fenceInfo.Files
                .Where(f => Path.GetFileName(f).IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void SortFiles(string column, bool ascending)
        {
            fenceInfo.SortColumn = column;
            fenceInfo.SortAscending = ascending;

            try
            {
                switch (column.ToLowerInvariant())
                {
                    case "name":
                        fenceInfo.Files.Sort((a, b) =>
                        {
                            var cmp = string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
                            return ascending ? cmp : -cmp;
                        });
                        break;
                    case "type":
                        fenceInfo.Files.Sort((a, b) =>
                        {
                            var extA = Path.GetExtension(a).ToLowerInvariant();
                            var extB = Path.GetExtension(b).ToLowerInvariant();
                            var cmp = string.Compare(extA, extB, StringComparison.OrdinalIgnoreCase);
                            if (cmp == 0)
                                cmp = string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
                            return ascending ? cmp : -cmp;
                        });
                        break;
                    case "date":
                        fenceInfo.Files.Sort((a, b) =>
                        {
                            DateTime dateA, dateB;
                            try { dateA = File.GetLastWriteTime(a); }
                            catch { dateA = DateTime.MinValue; }
                            try { dateB = File.GetLastWriteTime(b); }
                            catch { dateB = DateTime.MinValue; }
                            var cmp = DateTime.Compare(dateA, dateB);
                            return ascending ? cmp : -cmp;
                        });
                        break;
                }

                Save();
                Invalidate();
                logger.Info($"Sorted fence '{fenceInfo.Name}' by {column} {(ascending ? "ascending" : "descending")}", "FenceWindow");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to sort fence '{fenceInfo.Name}'", "FenceWindow", ex);
            }
        }

        #region Internal Drag and Drop

        private struct FenceGridLayout
        {
            public int ItemSpacing;
            public int ActualItemWidth;
            public int ActualItemHeight;
            public int ItemsPerRow;

            public static FenceGridLayout Calculate(int clientWidth, int titleHeightVal, int itemSpacing, int iconSize, int baseItemWidth, int baseTextHeight)
            {
                var actualItemWidth = Math.Max(iconSize + 10, baseItemWidth);
                var actualItemHeight = iconSize + baseTextHeight + 10;
                var itemsPerRow = Math.Max(1, (clientWidth - itemSpacing) / (actualItemWidth + itemSpacing));

                return new FenceGridLayout
                {
                    ItemSpacing = itemSpacing,
                    ActualItemWidth = actualItemWidth,
                    ActualItemHeight = actualItemHeight,
                    ItemsPerRow = itemsPerRow
                };
            }

            public Point GetItemPosition(int index, int titleHeightVal, int scrollOffsetVal)
            {
                var row = index / ItemsPerRow;
                var col = index % ItemsPerRow;
                var x = ItemSpacing + col * (ActualItemWidth + ItemSpacing);
                var y = ItemSpacing + row * (ActualItemHeight + ItemSpacing) + titleHeightVal - scrollOffsetVal;
                return new Point(x, y);
            }

            public int GetGridIndex(Point position, int titleHeightVal, int scrollOffsetVal, int maxItems)
            {
                var contentY = position.Y - titleHeightVal + scrollOffsetVal;
                var row = Math.Max(0, (contentY - ItemSpacing) / (ActualItemHeight + ItemSpacing));
                var col = Math.Max(0, (position.X - ItemSpacing) / (ActualItemWidth + ItemSpacing));
                col = Math.Min(col, ItemsPerRow - 1);
                var index = (int)(row * ItemsPerRow + col);
                return Math.Min(index, maxItems);
            }
        }

        private string GetItemAtPosition(Point position)
        {
            var layout = FenceGridLayout.Calculate(Width, titleHeight, fenceInfo.ItemSpacing, fenceInfo.IconSize, itemWidth, textHeight);
            var x = layout.ItemSpacing;
            var y = layout.ItemSpacing;
            
            foreach (var file in fenceInfo.Files)
            {
                var itemRect = new Rectangle(x, y + titleHeight - scrollOffset, layout.ActualItemWidth, layout.ActualItemHeight);
                
                if (itemRect.Contains(position))
                {
                    return file;
                }

                x += layout.ActualItemWidth + layout.ItemSpacing;
                if (x + layout.ActualItemWidth > Width)
                {
                    x = layout.ItemSpacing;
                    y += layout.ActualItemHeight + layout.ItemSpacing;
                }
            }
            
            return null;
        }

        private int GetGridPositionIndex(Point position)
        {
            var layout = FenceGridLayout.Calculate(Width, titleHeight, fenceInfo.ItemSpacing, fenceInfo.IconSize, itemWidth, textHeight);
            return layout.GetGridIndex(position, titleHeight, scrollOffset, fenceInfo.Files.Count);
        }

        private void StartItemDrag(string itemPath, Point startLocation)
        {
            if (!ItemExists(itemPath))
            {
                logger.Warning($"Cannot drag item that no longer exists: {itemPath}", "FenceWindow");
                fenceInfo.Files.Remove(itemPath);
                selectedItem = null;
                Save();
                Refresh();
                return;
            }
            
            isDraggingItem = true;
            draggingItem = itemPath;
            dragCurrentPoint = startLocation;
            this.Cursor = Cursors.Hand;
            this.Text = $"{fenceInfo.Name} - Dragging {Path.GetFileName(itemPath)}";
            
            try
            {
                var paths = selectedItems.Count > 1
                    ? string.Join("\n", selectedItems)
                    : itemPath;
                this.DoDragDrop(new DataObject("FencelessItemPaths", paths), DragDropEffects.Move);
            }
            catch { }
            
            logger.Debug($"Started dragging item '{Path.GetFileName(itemPath)}' in fence '{fenceInfo.Name}'", "FenceWindow");
        }

        private void UpdateDragTarget(Point currentLocation)
        {
            if (!isDraggingItem) return;
            
            dragTargetIndex = GetGridPositionIndex(currentLocation);
        }

        #endregion
    }
}
