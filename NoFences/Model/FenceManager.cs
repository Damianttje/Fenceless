using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using Fenceless.Util;
using Fenceless.UI;
using System.Windows.Forms;
using System.Linq;

namespace Fenceless.Model
{
    public class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private const string MetaFileName = "__fence_metadata.xml";
        private readonly string basePath;
        private readonly List<FenceWindow> activeFences = new List<FenceWindow>();
        private GlobalHotkeyManager hotkeyManager;
        private int toggleAutoHideHotkeyId = -1;
        private int showAllFencesHotkeyId = -1;
        private readonly Logger logger;
        private static readonly object _saveLock = new object();
        private static readonly XmlSerializer FenceInfoSerializer = new XmlSerializer(typeof(FenceInfo));
        private System.Threading.Timer _visibilityMonitor;

        public FenceManager()
        {
            logger = Logger.Instance;
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless");
            EnsureDirectoryExists(basePath);
            logger.Info($"FenceManager initialized with base path: {basePath}", "FenceManager");
            InitializeGlobalHotkeys();
            InitializeVisibilityMonitor();
        }

        private void InitializeVisibilityMonitor()
        {
            _visibilityMonitor = new System.Threading.Timer(_ =>
            {
                foreach (var fence in activeFences.ToArray())
                {
                    try
                    {
                        fence.CheckVisibility();
                    }
                    catch { }
                }
            }, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
        }

        private void InitializeGlobalHotkeys()
        {
            try
            {
                hotkeyManager = new GlobalHotkeyManager();
                RegisterGlobalHotkeys();
                logger.Info("Global hotkeys initialized successfully", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to initialize global hotkeys", "FenceManager", ex);
            }
        }

        private void RegisterGlobalHotkeys()
        {
            try
            {
                if (toggleAutoHideHotkeyId != -1)
                {
                    hotkeyManager.UnregisterHotkey(toggleAutoHideHotkeyId);
                    toggleAutoHideHotkeyId = -1;
                }
                if (showAllFencesHotkeyId != -1)
                {
                    hotkeyManager.UnregisterHotkey(showAllFencesHotkeyId);
                    showAllFencesHotkeyId = -1;
                }

                var settings = AppSettings.Instance;

                if (TryParseShortcut(settings.ToggleAutoHideShortcut, out var autoHideKey, out var autoHideCtrl, out var autoHideAlt, out var autoHideShift))
                {
                    toggleAutoHideHotkeyId = hotkeyManager.RegisterHotkey(
                        autoHideKey, ctrl: autoHideCtrl, alt: autoHideAlt, shift: autoHideShift, action: ToggleAllFencesAutoHide);
                }

                if (TryParseShortcut(settings.ShowAllFencesShortcut, out var showAllKey, out var showAllCtrl, out var showAllAlt, out var showAllShift))
                {
                    showAllFencesHotkeyId = hotkeyManager.RegisterHotkey(
                        showAllKey, ctrl: showAllCtrl, alt: showAllAlt, shift: showAllShift, action: ShowAllFences);
                }

                logger.Info("Global hotkeys registered from settings", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to register global hotkeys", "FenceManager", ex);
            }
        }

        private static bool TryParseShortcut(string shortcut, out Keys key, out bool ctrl, out bool alt, out bool shift)
        {
            key = Keys.None;
            ctrl = false;
            alt = false;
            shift = false;

            if (string.IsNullOrWhiteSpace(shortcut))
                return false;

            var parts = shortcut.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                    ctrl = true;
                else if (trimmed.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    alt = true;
                else if (trimmed.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    shift = true;
                else if (Enum.TryParse<Keys>(trimmed, true, out var parsedKey))
                    key = parsedKey;
            }

            return key != Keys.None;
        }

        public void LoadFences()
        {
            try
            {
                logger.Info("Loading fences from storage", "FenceManager");
                int loadedCount = 0;
                int errorCount = 0;
                
                foreach (var dir in Directory.EnumerateDirectories(basePath))
                {
                    var metaFile = Path.Combine(dir, MetaFileName);
                    if (!File.Exists(metaFile))
                        continue;

                    try
                    {
                        var fenceInfo = LoadFenceInfo(metaFile);
                        if (fenceInfo != null)
                        {
                            var fenceWindow = new FenceWindow(fenceInfo);
                            activeFences.Add(fenceWindow);
                            fenceWindow.FormClosed += (s, e) => activeFences.Remove(fenceWindow);
                            fenceWindow.Show();
                            loadedCount++;
                            logger.Debug($"Loaded fence '{fenceInfo.Name}' from {dir}", "FenceManager");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        // Log error but continue loading other fences
                        logger.Error($"Failed to load fence from {dir}", "FenceManager", ex);
                    }
                }

                if (loadedCount > 0)
                {
                    logger.Info($"Successfully loaded {loadedCount} fence(s). {errorCount} error(s) encountered.", "FenceManager");
                }
                else
                {
                    logger.Info("No fences found to load", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to load fences", "FenceManager", ex);
            }
        }

        private FenceInfo LoadFenceInfo(string metaFile)
        {
            try
            {
                using (var reader = new StreamReader(metaFile))
                {
                    var fenceInfo = FenceInfoSerializer.Deserialize(reader) as FenceInfo;
                    logger.Debug($"Deserialized fence info from {metaFile}", "FenceManager");
                    return fenceInfo;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to deserialize fence metadata from {metaFile}", "FenceManager", ex);
                return null;
            }
        }

        public void CreateFence(string name)
        {
            try
            {
                logger.Info($"Creating new fence: '{name}'", "FenceManager");
                var settings = AppSettings.Instance;
                
                int posX = 100 + (activeFences.Count * 30) % 400;
                int posY = 250 + (activeFences.Count * 30) % 300;
                
                var fenceInfo = new FenceInfo(Guid.NewGuid())
                {
                    Name = name,
                    PosX = posX,
                    PosY = posY,
                    Height = settings.DefaultFenceHeight,
                    Width = settings.DefaultFenceWidth,
                    TitleHeight = settings.DefaultTitleHeight,
                    Transparency = settings.DefaultTransparency,
                    AutoHide = settings.DefaultAutoHide,
                    AutoHideDelay = settings.DefaultAutoHideDelay,
                    BackgroundColor = settings.DefaultBackgroundColor,
                    TitleBackgroundColor = settings.DefaultTitleBackgroundColor,
                    TextColor = settings.DefaultTextColor,
                    BorderColor = settings.DefaultBorderColor,
                    BackgroundTransparency = settings.DefaultBackgroundTransparency,
                    TitleBackgroundTransparency = settings.DefaultTitleBackgroundTransparency,
                    TextTransparency = settings.DefaultTextTransparency,
                    BorderTransparency = settings.DefaultBorderTransparency,
                    BorderWidth = settings.DefaultBorderWidth,
                    CornerRadius = settings.DefaultCornerRadius,
                    ShowShadow = settings.DefaultShowShadow,
                    IconSize = settings.DefaultIconSize,
                    ItemSpacing = settings.DefaultItemSpacing
                };

                UpdateFence(fenceInfo);
                var fenceWindow = new FenceWindow(fenceInfo);
                activeFences.Add(fenceWindow);
                fenceWindow.FormClosed += (s, e) => activeFences.Remove(fenceWindow);
                fenceWindow.Show();
                
                logger.Info($"Fence '{name}' created successfully with ID {fenceInfo.Id}", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create fence '{name}'", "FenceManager", ex);
            }
        }

        public void RemoveFence(FenceInfo info)
        {
            try
            {
                logger.Info($"Removing fence '{info.Name}' (ID: {info.Id})", "FenceManager");
                var folderPath = GetFolderPath(info);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    logger.Debug($"Deleted fence directory: {folderPath}", "FenceManager");
                }
                logger.Info($"Fence '{info.Name}' removed successfully", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to remove fence '{info.Name}'", "FenceManager", ex);
            }
        }

        public void UpdateFence(FenceInfo fenceInfo)
        {
            try
            {
                var path = GetFolderPath(fenceInfo);
                EnsureDirectoryExists(path);

                var metaFile = Path.Combine(path, MetaFileName);
                
                using (var writer = new StreamWriter(metaFile))
                {
                    FenceInfoSerializer.Serialize(writer, fenceInfo);
                }
                logger.Debug($"Updated fence '{fenceInfo.Name}' metadata", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to update fence '{fenceInfo.Name}'", "FenceManager", ex);
            }
        }

        private void EnsureDirectoryExists(string dir)
        {
            try
            {
                var di = new DirectoryInfo(dir);
                if (!di.Exists)
                {
                    di.Create();
                    logger.Debug($"Created directory: {dir}", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to create directory {dir}", "FenceManager", ex);
            }
        }

        private string GetFolderPath(FenceInfo fenceInfo)
        {
            return Path.Combine(basePath, fenceInfo.Id.ToString());
        }

        public void SaveAllFences()
        {
            lock (_saveLock)
            {
                try
                {
                    logger.Info("Saving all fences", "FenceManager");
                    int savedCount = 0;
                    int errorCount = 0;
                    
                    // Create a snapshot to avoid modification during enumeration
                    var fencesSnapshot = activeFences.ToList();
                    
                    foreach (var fence in fencesSnapshot)
                    {
                        try
                        {
                            var fenceInfo = fence.GetFenceInfo();
                            UpdateFence(fenceInfo);
                            savedCount++;
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            logger.Error("Failed to save individual fence", "FenceManager", ex);
                        }
                    }
                    
                    logger.Info($"Saved {savedCount} fence(s) successfully. {errorCount} error(s) encountered.", "FenceManager");
                }
                catch (Exception ex)
                {
                    logger.Error("Failed to save all fences", "FenceManager", ex);
                }
            }
        }

        // Global hotkey actions
        private void ToggleAllFencesAutoHide()
        {
            try
            {
                logger.Info("Toggling auto-hide for all fences", "FenceManager");
                bool newAutoHideState = false;
                foreach (var fence in activeFences.ToList())
                {
                    var fenceInfo = fence.GetFenceInfo();
                    fenceInfo.AutoHide = !fenceInfo.AutoHide;
                    newAutoHideState = fenceInfo.AutoHide;
                    fence.UpdateAutoHideState();
                    UpdateFence(fenceInfo);
                }
                logger.Info($"Auto-hide toggled to {newAutoHideState} for all fences", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to toggle auto-hide for all fences", "FenceManager", ex);
            }
        }

        public void ShowAllFences()
        {
            try
            {
                logger.Info("Showing all fences", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    fence.ForceShow();
                }
                logger.Info($"Showed {activeFences.Count} fence(s)", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to show all fences", "FenceManager", ex);
            }
        }

        private void HideAllFences()
        {
            try
            {
                logger.Info("Hiding all fences", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    fence.ForceHide();
                }
                logger.Info($"Hidden {activeFences.Count} fence(s)", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to hide all fences", "FenceManager", ex);
            }
        }

        public void ApplySettingsToAllFences(int transparency, bool autoHide, int autoHideDelay)
        {
            try
            {
                logger.Info($"Applying settings to all fences - Transparency: {transparency}%, AutoHide: {autoHide}, Delay: {autoHideDelay}ms", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    var fenceInfo = fence.GetFenceInfo();
                    fenceInfo.Transparency = transparency;
                    fenceInfo.AutoHide = autoHide;
                    fenceInfo.AutoHideDelay = autoHideDelay;
                    
                    fence.ApplySettings();
                    UpdateFence(fenceInfo);
                }
                logger.Info($"Settings applied to {activeFences.Count} fence(s)", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to apply settings to all fences", "FenceManager", ex);
            }
        }

        public void ShowGlobalSettings()
        {
            try
            {
                logger.Debug("Opening global settings dialog", "FenceManager");
                var settingsForm = new SettingsForm();
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Refresh global hotkeys if they changed
                    RegisterGlobalHotkeys();
                    logger.Info("Global settings updated", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to show global settings", "FenceManager", ex);
            }
        }

        public void ShowFenceSettings(FenceInfo fenceInfo)
        {
            try
            {
                logger.Debug($"Opening settings for fence '{fenceInfo.Name}'", "FenceManager");
                var settingsForm = new SettingsForm();
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Find the fence window and refresh its settings
                    var fenceWindow = activeFences.FirstOrDefault(f => f.GetFenceInfo().Id == fenceInfo.Id);
                    fenceWindow?.ApplySettings();
                    logger.Info($"Settings updated for fence '{fenceInfo.Name}'", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to show fence settings for '{fenceInfo.Name}'", "FenceManager", ex);
            }
        }

        public void HighlightFence(Guid fenceId)
        {
            try
            {
                logger.Debug($"Highlighting fence with ID: {fenceId}", "FenceManager");
                var fenceWindow = activeFences.FirstOrDefault(f => f.GetFenceInfo().Id == fenceId);
                if (fenceWindow != null)
                {
                    fenceWindow.HighlightFence();
                    logger.Info($"Highlighted fence '{fenceWindow.GetFenceInfo().Name}'", "FenceManager");
                }
                else
                {
                    logger.Warning($"Fence with ID {fenceId} not found for highlighting", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to highlight fence with ID {fenceId}", "FenceManager", ex);
            }
        }

        public void ApplySettingsToFence(FenceInfo fenceInfo)
        {
            try
            {
                logger.Debug($"Applying settings to fence '{fenceInfo.Name}'", "FenceManager");
                var fenceWindow = activeFences.FirstOrDefault(f => f.GetFenceInfo().Id == fenceInfo.Id);
                if (fenceWindow != null)
                {
                    // Update the fence window's internal fence info reference
                    fenceWindow.UpdateFenceInfo(fenceInfo);
                    fenceWindow.ApplySettings();
                    logger.Info($"Applied settings to fence '{fenceInfo.Name}'", "FenceManager");
                }
                else
                {
                    logger.Warning($"Fence '{fenceInfo.Name}' not found for settings application", "FenceManager");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to apply settings to fence '{fenceInfo.Name}'", "FenceManager", ex);
            }
        }

        public List<FenceInfo> GetAllFenceInfos()
        {
            try
            {
                // Return current fence info from active fences
                var result = new List<FenceInfo>();
                foreach (var fence in activeFences.ToList())
                {
                    var fenceInfo = fence.GetFenceInfo();
                    result.Add(fenceInfo);
                }
                logger.Debug($"Retrieved {result.Count} fence infos", "FenceManager");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to get all fence infos", "FenceManager", ex);
                return new List<FenceInfo>();
            }
        }

        public int GetActiveFenceCount()
        {
            return activeFences.Count;
        }

        public void Dispose()
        {
            try
            {
                logger.Info("Disposing FenceManager", "FenceManager");
                _visibilityMonitor?.Dispose();
                hotkeyManager?.Dispose();
                logger.Debug("FenceManager disposed successfully", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Error disposing FenceManager", "FenceManager", ex);
            }
        }
    }
}
