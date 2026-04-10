using Fenceless.Model;
using Fenceless.UI;
using Fenceless.Util;
using Fenceless.Win32;
using Peter;
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
        private bool shouldUpdateSelection;
        private bool shouldRunDoubleClick;
        private bool hasSelectionUpdated;
        private bool hasHoverUpdated;
        private bool isMinified;
        private int prevHeight;

        private int scrollHeight;
        private int scrollOffset;

        // New fields for transparency and autohide
        private bool isAutoHidden = false;
    private FormsTimer autoHideTimer;
        private double normalOpacity = 1.0;
        private bool isMouseInside = false;
        
        // Visibility monitor to prevent Show Desktop from hiding the window
    private System.Threading.Timer visibilityMonitor;

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
            
            logger.Info($"Fence window '{fenceInfo.Name}' created successfully at ({fenceInfo.PosX}, {fenceInfo.PosY})", "FenceWindow");
        }

        private void SetupEventHandlers()
        {
            removeItemToolStripMenuItem.Click += (sender, e) =>
            {
                if (hoveringItem != null)
                {
                    try
                    {
                        var result = MessageBox.Show(this, 
                            $"Remove '{Path.GetFileName(hoveringItem)}' from this fence?\n\nThis will not delete the file, only remove it from the fence.",
                            "Remove Item", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            fenceInfo.Files.Remove(hoveringItem);
                            hoveringItem = null;
                            selectedItem = null;
                            Save();
                            Refresh();
                            logger.Info($"Removed item from fence '{fenceInfo.Name}'", "FenceWindow");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to remove item from fence '{fenceInfo.Name}'", "FenceWindow", ex);
                    }
                }
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
                            // Swap with previous item
                            fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex - 1];
                            fenceInfo.Files[currentIndex - 1] = hoveringItem;
                            
                            Save();
                            Refresh();
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
                            // Swap with next item
                            fenceInfo.Files[currentIndex] = fenceInfo.Files[currentIndex + 1];
                            fenceInfo.Files[currentIndex + 1] = hoveringItem;
                            
                            Save();
                            Refresh();
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

        private void RenderEntry(Graphics g, FenceEntry entry, int x, int y, int itemWidth, int itemHeight, int iconSize, Color textColor)
        {
            try
            {
                var icon = entry.ExtractIcon(thumbnailProvider);
                var name = entry.Name;

                // Get or create cached scaled bitmap
                var cacheKey = $"{entry.Path}_{iconSize}";
                var iconBitmap = iconCache.GetIcon(entry.Path, iconSize);
                
                if (iconBitmap == null) return; // Safety check

                var textPosition = new PointF(x, y + iconBitmap.Height + 5);
                var textMaxSize = new SizeF(itemWidth, textHeight);

                var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

                var textSize = g.MeasureString(name, iconFont, textMaxSize, stringFormat);
                var outlineRect = new Rectangle(x - 2, y - 2, itemWidth + 2, iconBitmap.Height + (int)textSize.Height + 5 + 2);
                var outlineRectInner = outlineRect.Shrink(1);

                var mousePos = PointToClient(MousePosition);
                var mouseOver = !isDraggingItem && mousePos.X >= x && mousePos.Y >= y && mousePos.X < x + outlineRect.Width && mousePos.Y < y + outlineRect.Height;

                // Check if this item is being dragged
                var isBeingDragged = isDraggingItem && draggingItem == entry.Path;

                if (mouseOver && !isBeingDragged)
                {
                    hoveringItem = entry.Path;
                    hasHoverUpdated = true;
                }

                if (mouseOver && shouldUpdateSelection && !isBeingDragged)
                {
                    selectedItem = entry.Path;
                    shouldUpdateSelection = false;
                    hasSelectionUpdated = true;
                }

                if (mouseOver && shouldRunDoubleClick && !isDraggingItem)
                {
                    shouldRunDoubleClick = false;
                    entry.Open();
                }

                // Apply transparency and visual effects for dragged items
                float opacity = isBeingDragged ? 0.3f : 1.0f;
                
                // Selection and hover highlighting
                if (selectedItem == entry.Path && !isBeingDragged)
                {
                    if (mouseOver)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(180, SystemColors.ActiveBorder), 2), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(120, SystemColors.GradientActiveCaption)), outlineRect);
                    }
                    else
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(150, SystemColors.ActiveBorder), 2), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(100, SystemColors.GradientInactiveCaption)), outlineRect);
                    }
                }
                else if (!isBeingDragged)
                {
                    if (mouseOver)
                    {
                        g.DrawRectangle(new Pen(Color.FromArgb(120, SystemColors.ActiveBorder)), outlineRectInner);
                        g.FillRectangle(new SolidBrush(Color.FromArgb(80, SystemColors.ActiveCaption)), outlineRect);
                    }
                }

                // Draw icon centered with optional transparency
                var iconRect = new Rectangle(x + itemWidth / 2 - iconBitmap.Width / 2, y, iconBitmap.Width, iconBitmap.Height);
                
                if (isBeingDragged)
                {
                    // Use simple alpha blending for dragged items
                    using (var imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                        colorMatrix.Matrix33 = opacity; // Alpha channel
                        imageAttributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(iconBitmap, iconRect, 0, 0, iconBitmap.Width, iconBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                    }
                }
                else
                {
                    g.DrawImage(iconBitmap, iconRect);
                }
                
                // Draw text with shadow if enabled
                var textColorWithOpacity = isBeingDragged ? 
                    Color.FromArgb((int)(textColor.A * opacity), textColor.R, textColor.G, textColor.B) : textColor;
                    
                if (fenceInfo.ShowShadow && !isBeingDragged) // Skip shadow for dragged items to improve performance
                {
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(180, 15, 15, 15)))
                    {
                        g.DrawString(name, iconFont, shadowBrush, 
                            new RectangleF(textPosition.Move(shadowDist, shadowDist), textMaxSize), stringFormat);
                    }
                }
                
                // Draw main text
                using (var textBrush = new SolidBrush(textColorWithOpacity))
                {
                    g.DrawString(name, iconFont, textBrush, new RectangleF(textPosition, textMaxSize), stringFormat);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering entry '{entry?.Path}': {ex.Message}", "FenceWindow", ex);
                
                // Draw error placeholder
                using (var errorBrush = new SolidBrush(Color.Red))
                {
                    g.FillRectangle(errorBrush, x, y, itemWidth, itemHeight);
                }
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
                this.Opacity = 0.1; // Nearly invisible but still responsive to mouse
            }
        }

        private void ShowFence()
        {
            if (isAutoHidden)
            {
                isAutoHidden = false;
                this.Opacity = normalOpacity;
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            
            // Additional protection: Hide from Alt+Tab after handle is created
            HideFromAltTab(Handle);
            
            // Prevent minimize to survive Show Desktop
            DesktopUtil.PreventMinimize(Handle);
            
            // Start visibility monitor to keep window visible
            InitializeVisibilityMonitor();
            
            logger?.Debug($"Fence window '{fenceInfo?.Name ?? "Unknown"}' configured to prevent minimize", "FenceWindow");
        }

        private void InitializeVisibilityMonitor()
        {
            visibilityMonitor = new System.Threading.Timer(_ => EnsureFenceVisible(true), null,
                TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
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
            
            // Handle keyboard shortcuts
            if (keyData == (Keys.Control | Keys.Alt | Keys.T))
            {
                ToggleTransparency();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.Alt | Keys.S))
            {
                ShowAllFences();
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

        private void RemoveSelectedItem()
        {
            if (selectedItem != null)
            {
                try
                {
                    fenceInfo.Files.Remove(selectedItem);
                    selectedItem = null;
                    hoveringItem = null;
                    Save();
                    Refresh();
                    logger.Info($"Removed selected item from fence '{fenceInfo.Name}' via keyboard", "FenceWindow");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to remove selected item from fence '{fenceInfo.Name}'", "FenceWindow", ex);
                }
            }
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
            fenceInfo.Files.Remove(hoveringItem);
            hoveringItem = null;
            Save();
            Refresh();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var hasHoveringItem = hoveringItem != null;
            var itemIndex = hasHoveringItem ? fenceInfo.Files.IndexOf(hoveringItem) : -1;
            
            // Item-specific actions
            deleteItemToolStripMenuItem.Visible = hasHoveringItem;
            removeItemToolStripMenuItem.Visible = hasHoveringItem;
            moveItemUpToolStripMenuItem.Visible = hasHoveringItem && itemIndex > 0;
            moveItemDownToolStripMenuItem.Visible = hasHoveringItem && itemIndex < fenceInfo.Files.Count - 1;
            toolStripSeparator3.Visible = hasHoveringItem;
        }

        private void FenceWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // This handles the KeyDown event for the form
            // ProcessCmdKey already handles our shortcuts, but this can be used for other keys
        }

        private void FenceWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !lockedToolStripMenuItem.Checked)
                e.Effect = DragDropEffects.Move;
        }

        private void FenceWindow_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var dropped = (string[])e.Data.GetData(DataFormats.FileDrop);
                var addedFiles = 0;
                
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
                Refresh();
            }
        }

        private void FenceWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !lockedToolStripMenuItem.Checked)
            {
                dragStartPoint = e.Location;
                
                // Find item under cursor
                var itemPath = GetItemAtPosition(e.Location);
                if (itemPath != null && ItemExists(itemPath))
                {
                    selectedItem = itemPath;
                    Refresh();
                }
                else if (itemPath != null)
                {
                    // Item no longer exists, remove it from the fence
                    logger.Warning($"Item no longer exists, removing from fence: {itemPath}", "FenceWindow");
                    fenceInfo.Files.Remove(itemPath);
                    selectedItem = null;
                    Save();
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
                visibilityMonitor?.Dispose();
                
                // Dispose fonts
                titleFont?.Dispose();
                iconFont?.Dispose();
                
                // Dispose other resources
                thumbnailProvider?.Dispose();
                throttledMove?.Dispose();
                throttledResize?.Dispose();
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
            // Only handle double-click if we're not dragging and not in a drag gesture
            if (!isDraggingItem && selectedItem != null)
            {
                // Get the current mouse position and check if it's over an item
                var mousePos = PointToClient(MousePosition);
                var itemPath = GetItemAtPosition(mousePos);
                
                // Verify the double-clicked item still exists and matches the selected item
                if (itemPath != null && itemPath == selectedItem && ItemExists(itemPath))
                {
                    // Open the item directly
                    var entry = FenceEntry.FromPath(itemPath);
                    if (entry != null)
                    {
                        logger.Info($"Double-clicked item '{System.IO.Path.GetFileName(itemPath)}' in fence '{fenceInfo.Name}'", "FenceWindow");
                        entry.Open();
                    }
                    else
                    {
                        logger.Warning($"Could not create entry for item: {itemPath}", "FenceWindow");
                    }
                }
                else if (itemPath != null && !ItemExists(itemPath))
                {
                    // Item no longer exists, remove it
                    logger.Warning($"Double-clicked item no longer exists, removing: {itemPath}", "FenceWindow");
                    fenceInfo.Files.Remove(itemPath);
                    selectedItem = null;
                    Save();
                    Refresh();
                }
            }
        }

        private void FenceWindow_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.Clip = new Region(ClientRectangle);
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Use customizable colors with transparency
                var backgroundColor = ApplyTransparency(Color.FromArgb(fenceInfo.BackgroundColor), fenceInfo.BackgroundTransparency);
                var titleBackgroundColor = ApplyTransparency(Color.FromArgb(fenceInfo.TitleBackgroundColor), fenceInfo.TitleBackgroundTransparency);
                var textColor = ApplyTransparency(Color.FromArgb(fenceInfo.TextColor), fenceInfo.TextTransparency);
                var borderColor = ApplyTransparency(Color.FromArgb(fenceInfo.BorderColor), fenceInfo.BorderTransparency);

                // Background with customizable color and transparency
                using (var backgroundBrush = new SolidBrush(backgroundColor))
                {
                    if (fenceInfo.CornerRadius > 0)
                    {
                        // Draw rounded rectangle if corner radius is set
                        using (var path = CreateRoundedRectanglePath(ClientRectangle, fenceInfo.CornerRadius))
                        {
                            e.Graphics.FillPath(backgroundBrush, path);
                        }
                    }
                    else
                    {
                        e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
                    }
                }

                // Title background with customizable color and transparency
                using (var titleBrush = new SolidBrush(titleBackgroundColor))
                {
                    var titleRect = new RectangleF(0, 0, Width, titleHeight);
                    if (fenceInfo.CornerRadius > 0)
                    {
                        // Only round the top corners for title
                        using (var titlePath = CreateRoundedRectanglePath(titleRect, fenceInfo.CornerRadius, true))
                        {
                            e.Graphics.FillPath(titleBrush, titlePath);
                        }
                    }
                    else
                    {
                        e.Graphics.FillRectangle(titleBrush, titleRect);
                    }
                }

                // Title text with customizable color and transparency
                using (var textBrush = new SolidBrush(textColor))
                {
                    e.Graphics.DrawString(Text, titleFont, textBrush, new PointF(Width / 2, titleOffset), 
                        new StringFormat { Alignment = StringAlignment.Center });
                }

                // Border if enabled
                if (fenceInfo.BorderWidth > 0)
                {
                    using (var borderPen = new Pen(borderColor, fenceInfo.BorderWidth))
                    {
                        if (fenceInfo.CornerRadius > 0)
                        {
                            var borderRect = new Rectangle(fenceInfo.BorderWidth / 2, fenceInfo.BorderWidth / 2, 
                                Width - fenceInfo.BorderWidth, Height - fenceInfo.BorderWidth);
                            using (var borderPath = CreateRoundedRectanglePath(borderRect, fenceInfo.CornerRadius))
                            {
                                e.Graphics.DrawPath(borderPen, borderPath);
                            }
                        }
                        else
                        {
                            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                        }
                    }
                }

                // Items
                var itemSpacing = fenceInfo.ItemSpacing;
                var iconSize = fenceInfo.IconSize;
                var actualItemWidth = Math.Max(iconSize + 10, itemWidth);
                var actualItemHeight = iconSize + textHeight + 10;
                
                var x = itemSpacing;
                var y = itemSpacing;
                scrollHeight = 0;
                e.Graphics.Clip = new Region(new Rectangle(0, titleHeight, Width, Height - titleHeight));
                
                foreach (var file in fenceInfo.Files)
                {
                    try
                    {
                        var entry = FenceEntry.FromPath(file);
                        if (entry == null)
                            continue;

                        RenderEntry(e.Graphics, entry, x, y + titleHeight - scrollOffset, actualItemWidth, actualItemHeight, iconSize, textColor);

                        var itemBottom = y + actualItemHeight;
                        if (itemBottom > scrollHeight)
                            scrollHeight = itemBottom;

                        x += actualItemWidth + itemSpacing;
                        if (x + actualItemWidth > Width)
                        {
                            x = itemSpacing;
                            y += actualItemHeight + itemSpacing;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Error rendering file '{file}': {ex.Message}", "FenceWindow", ex);
                        // Continue with next item instead of crashing
                    }
                }

                scrollHeight -= (ClientRectangle.Height - titleHeight);

                // Scroll bars
                if (scrollHeight > 0)
                {
                    var contentHeight = Height - titleHeight;
                    var scrollbarHeight = contentHeight - scrollHeight;
                    using (var scrollBrush = new SolidBrush(Color.FromArgb(150, borderColor)))
                    {
                        e.Graphics.FillRectangle(scrollBrush, new Rectangle(Width - 5, titleHeight + scrollOffset, 5, scrollbarHeight));
                    }
                    scrollOffset = Math.Min(scrollOffset, scrollHeight);
                }

                // Click handlers
                if (shouldUpdateSelection && !hasSelectionUpdated)
                    selectedItem = null;

                if (!hasHoverUpdated)
                    hoveringItem = null;

                // Render drag target indicator
                if (isDraggingItem && dragTargetIndex >= 0)
                {
                    RenderDragTargetIndicator(e.Graphics, dragTargetIndex);
                }

                // Render dragged item at cursor position
                if (isDraggingItem && draggingItem != null)
                {
                    RenderDraggedItem(e.Graphics, draggingItem, dragCurrentPoint);
                }

                shouldRunDoubleClick = false;
                shouldUpdateSelection = false;
                hasSelectionUpdated = false;
                hasHoverUpdated = false;
            }
            catch (OutOfMemoryException ex)
            {
                logger.Critical("Out of memory in paint method, clearing caches", "FenceWindow", ex);
                
                // Emergency cleanup
                ClearIconCache();
                
                // Draw simple error state
                try
                {
                    using (var errorBrush = new SolidBrush(Color.DarkRed))
                    {
                        e.Graphics.FillRectangle(errorBrush, ClientRectangle);
                    }
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        e.Graphics.DrawString("Memory Error - Cache Cleared", SystemFonts.DefaultFont, textBrush, 10, 10);
                    }
                }
                catch
                {
                    // If even error drawing fails, just continue
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error in paint method: {ex.Message}", "FenceWindow", ex);
                
                // Draw simple error state
                try
                {
                    using (var errorBrush = new SolidBrush(Color.Red))
                    {
                        e.Graphics.FillRectangle(errorBrush, ClientRectangle);
                    }
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        e.Graphics.DrawString($"Render Error: {ex.Message}", SystemFonts.DefaultFont, textBrush, 10, 10);
                    }
                }
                catch
                {
                    // If even error drawing fails, just continue
                }
            }
        }

        private Color ApplyTransparency(Color baseColor, int transparencyPercent)
        {
            // Convert transparency percentage to alpha value (0-255)
            int alpha = (int)Math.Round(transparencyPercent * 255.0 / 100.0);
            alpha = Math.Max(0, Math.Min(255, alpha)); // Clamp to valid range
            
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(RectangleF rect, int radius, bool topOnly = false)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2;
            var arc = new RectangleF(0, 0, diameter, diameter);

            // Top left corner
            arc.Location = new PointF(rect.Left, rect.Top);
            path.AddArc(arc, 180, 90);

            // Top right corner
            arc.Location = new PointF(rect.Right - diameter, rect.Top);
            path.AddArc(arc, 270, 90);

            if (topOnly)
            {
                // Straight lines for bottom
                path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
                path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
                path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
            }
            else
            {
                // Bottom right corner
                arc.Location = new PointF(rect.Right - diameter, rect.Bottom - diameter);
                path.AddArc(arc, 0, 90);

                // Bottom left corner
                arc.Location = new PointF(rect.Left, rect.Bottom - diameter);
                path.AddArc(arc, 90, 90);
            }

            path.CloseFigure();
            return path;
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
            FenceManager.Instance.CreateFence("New fence");
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
                shellContextMenu.CustomMenuItemSelected += OnRemoveFromFence;
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

                var fileName = System.IO.Path.GetFileName(filePath);
                logger.Info($"Removing '{fileName}' from fence '{fenceInfo.Name}' via context menu", "FenceWindow");

                // Only remove from the fence list, don't delete the actual file
                fenceInfo.Files.Remove(filePath);
                hoveringItem = null;
                
                // Clear icon cache for the removed item to free memory
                iconCache.ClearCache();
                
                Save();
                Refresh();
                
                logger.Info($"Successfully removed '{fileName}' from fence via context menu", "FenceWindow");
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

        #region Internal Drag and Drop

        private string GetItemAtPosition(Point position)
        {
            var itemSpacing = fenceInfo.ItemSpacing;
            var iconSize = fenceInfo.IconSize;
            var actualItemWidth = Math.Max(iconSize + 10, itemWidth);
            var actualItemHeight = iconSize + textHeight + 10;
            
            var x = itemSpacing;
            var y = itemSpacing;
            
            foreach (var file in fenceInfo.Files)
            {
                var entry = FenceEntry.FromPath(file);
                if (entry == null)
                    continue;

                var itemRect = new Rectangle(x, y + titleHeight - scrollOffset, actualItemWidth, actualItemHeight);
                
                if (itemRect.Contains(position))
                {
                    return file;
                }

                x += actualItemWidth + itemSpacing;
                if (x + actualItemWidth > Width)
                {
                    x = itemSpacing;
                    y += actualItemHeight + itemSpacing;
                }
            }
            
            return null;
        }

        private int GetGridPositionIndex(Point position)
        {
            var itemSpacing = fenceInfo.ItemSpacing;
            var iconSize = fenceInfo.IconSize;
            var actualItemWidth = Math.Max(iconSize + 10, itemWidth);
            var actualItemHeight = iconSize + textHeight + 10;
            
            var contentY = position.Y - titleHeight + scrollOffset;
            var itemsPerRow = Math.Max(1, (Width - itemSpacing) / (actualItemWidth + itemSpacing));
            
            var row = Math.Max(0, (contentY - itemSpacing) / (actualItemHeight + itemSpacing));
            var col = Math.Max(0, (position.X - itemSpacing) / (actualItemWidth + itemSpacing));
            
            col = Math.Min(col, itemsPerRow - 1);
            
            var index = (int)(row * itemsPerRow + col);
            return Math.Min(index, fenceInfo.Files.Count);
        }

        private void StartItemDrag(string itemPath, Point startLocation)
        {
            // Verify item still exists before starting drag
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
            
            // Set cursor to indicate dragging
            this.Cursor = Cursors.Hand;
            
            // Update window title to show drag status
            this.Text = $"{fenceInfo.Name} - Dragging {Path.GetFileName(itemPath)}";
            
            logger.Debug($"Started dragging item '{Path.GetFileName(itemPath)}' in fence '{fenceInfo.Name}'", "FenceWindow");
        }

        private void UpdateDragTarget(Point currentLocation)
        {
            if (!isDraggingItem) return;
            
            dragTargetIndex = GetGridPositionIndex(currentLocation);
        }

        #endregion
        
        #region Drag Feedback Rendering

        private void RenderDragTargetIndicator(Graphics g, int targetIndex)
        {
            try
            {
                var itemSpacing = fenceInfo.ItemSpacing;
                var iconSize = fenceInfo.IconSize;
                var actualItemWidth = Math.Max(iconSize + 10, itemWidth);
                var actualItemHeight = iconSize + textHeight + 10;
                var itemsPerRow = Math.Max(1, (Width - itemSpacing) / (actualItemWidth + itemSpacing));
                
                var row = targetIndex / itemsPerRow;
                var col = targetIndex % itemsPerRow;
                
                var x = itemSpacing + col * (actualItemWidth + itemSpacing);
                var y = itemSpacing + row * (actualItemHeight + itemSpacing) + titleHeight - scrollOffset;
                
                // Simple pulsing effect without complex math
                var pulsePhase = (Environment.TickCount / 300) % 4;
                var alpha = pulsePhase < 2 ? 120 : 80;
                
                using (var pen = new Pen(Color.FromArgb(alpha, SystemColors.Highlight), 2))
                using (var brush = new SolidBrush(Color.FromArgb(alpha / 8, SystemColors.Highlight)))
                {
                    var indicatorRect = new Rectangle(x - 1, y - 1, actualItemWidth + 2, actualItemHeight + 2);
                    
                    // Fill with subtle background
                    g.FillRectangle(brush, indicatorRect);
                    
                    // Draw simple border
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(pen, indicatorRect);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering drag target indicator: {ex.Message}", "FenceWindow", ex);
            }
        }

        private void RenderDraggedItem(Graphics g, string itemPath, Point cursorPosition)
        {
            try
            {
                var entry = FenceEntry.FromPath(itemPath);
                if (entry == null) return;
                
                var iconSize = fenceInfo.IconSize;
                var cacheKey = $"{entry.Path}_{iconSize}";
                
                // Use cached icon if available
                var iconBitmap = iconCache.GetIcon(entry.Path, iconSize);
                
                // Fallback to creating temporary bitmap if cache failed (shouldn't happen often)
                if (iconBitmap == null)
                {
                    var icon = entry.ExtractIcon(thumbnailProvider);
                    if (icon.Width != iconSize || icon.Height != iconSize)
                    {
                        iconBitmap = new Bitmap(iconSize, iconSize);
                        using (var graphics = Graphics.FromImage(iconBitmap))
                        {
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.DrawIcon(icon, new Rectangle(0, 0, iconSize, iconSize));
                        }
                        // Don't cache here to avoid issues during drag
                    }
                    else
                    {
                        iconBitmap = icon.ToBitmap();
                    }
                }
                
                if (iconBitmap == null) return;
                
                // Position the dragged item slightly offset from cursor
                var drawX = cursorPosition.X - iconSize / 2;
                var drawY = cursorPosition.Y - iconSize / 2;
                
                // Simple shadow without complex effects
                using (var shadowBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                {
                    g.FillEllipse(shadowBrush, drawX + 2, drawY + 2, iconSize, iconSize);
                }
                
                // Draw the dragged icon with transparency
                using (var imageAttributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    var colorMatrix = new System.Drawing.Imaging.ColorMatrix();
                    colorMatrix.Matrix33 = 0.8f; // Slightly transparent
                    imageAttributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(iconBitmap, new Rectangle(drawX, drawY, iconSize, iconSize), 
                        0, 0, iconBitmap.Width, iconBitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                }
                
                // Draw simplified item name
                var textColor = ApplyTransparency(Color.FromArgb(fenceInfo.TextColor), fenceInfo.TextTransparency);
                using (var textBrush = new SolidBrush(Color.FromArgb(180, textColor.R, textColor.G, textColor.B)))
                {
                    var textRect = new RectangleF(drawX - 20, drawY + iconSize + 2, iconSize + 40, 20);
                    var stringFormat = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                    g.DrawString(entry.Name, iconFont, textBrush, textRect, stringFormat);
                }
                
                // The icon cache manages disposal, so no need to dispose here
                if (iconBitmap == null)
                {
                    logger.Warning($"Failed to get icon for dragged item '{itemPath}'", "FenceWindow");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error rendering dragged item '{itemPath}': {ex.Message}", "FenceWindow", ex);
            }
        }

        #endregion
    }
}

