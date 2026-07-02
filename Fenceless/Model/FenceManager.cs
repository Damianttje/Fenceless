using System;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using Fenceless.Util;
using Fenceless.UI;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Fenceless.Model
{
    public class FenceManager
    {
        public static FenceManager Instance { get; } = new FenceManager();

        private const string MetaFileName = "__fence_metadata.xml";
        private readonly string basePath;
        private readonly List<FenceWindow> activeFences = new List<FenceWindow>();
        private GlobalHotkeyManager? hotkeyManager;
        private int toggleAutoHideHotkeyId = -1;
        private int showAllFencesHotkeyId = -1;
        private int toggleTransparencyHotkeyId = -1;
        private int createNewFenceHotkeyId = -1;
        private int openSettingsHotkeyId = -1;
        private int toggleLockHotkeyId = -1;
        private int minimizeAllFencesHotkeyId = -1;
        private int refreshFencesHotkeyId = -1;
        private readonly Logger logger;
        private static readonly object _saveLock = new object();
        private static readonly XmlSerializer FenceInfoSerializer = new XmlSerializer(typeof(FenceInfo));
        private System.Threading.Timer? _visibilityMonitor;
        private EventHandler? _displaySettingsChangedHandler;
        private int visibilityMonitorErrorCount = 0;

        public string LastHotkeyRegistrationStatus { get; private set; } = "";

        public FenceManager()
        {
            logger = Logger.Instance;
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless");
            EnsureDirectoryExists(basePath);
            logger.Info($"FenceManager initialized with base path: {basePath}", "FenceManager");
            InitializeGlobalHotkeys();
            InitializeVisibilityMonitor();
            InitializeMonitorDetection();
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
                    catch (Exception ex)
                    {
                        visibilityMonitorErrorCount++;
                        if (visibilityMonitorErrorCount == 1 || visibilityMonitorErrorCount % 20 == 0)
                        {
                            logger.Warning($"Visibility monitor failed {visibilityMonitorErrorCount} time(s): {ex.Message}", "FenceManager");
                        }
                    }
                }
            }, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250));
        }

        private void InitializeMonitorDetection()
        {
            _displaySettingsChangedHandler = (s, e) =>
            {
                try
                {
                    logger.Info("Display settings changed, checking fence positions", "FenceManager");
                    foreach (var fence in activeFences.ToArray())
                    {
                        try
                        {
                            fence.ClampToScreen();
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Failed to clamp fence to screen", "FenceManager", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Error handling display settings change", "FenceManager", ex);
                }
            };
            SystemEvents.DisplaySettingsChanged += _displaySettingsChangedHandler;
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

        private int[] _allHotkeyIds => new[]
        {
            toggleAutoHideHotkeyId, showAllFencesHotkeyId, toggleTransparencyHotkeyId,
            createNewFenceHotkeyId, openSettingsHotkeyId, toggleLockHotkeyId,
            minimizeAllFencesHotkeyId, refreshFencesHotkeyId
        };

        private void UnregisterAllHotkeys()
        {
            if (hotkeyManager == null)
                return;

            foreach (var id in _allHotkeyIds)
            {
                if (id != -1)
                {
                    hotkeyManager.UnregisterHotkey(id);
                }
            }
            toggleAutoHideHotkeyId = -1;
            showAllFencesHotkeyId = -1;
            toggleTransparencyHotkeyId = -1;
            createNewFenceHotkeyId = -1;
            openSettingsHotkeyId = -1;
            toggleLockHotkeyId = -1;
            minimizeAllFencesHotkeyId = -1;
            refreshFencesHotkeyId = -1;
        }

        public void RegisterGlobalHotkeys()
        {
            try
            {
                hotkeyManager ??= new GlobalHotkeyManager();
                UnregisterAllHotkeys();

                var settings = AppSettings.Instance;

                var failures = new List<string>();

                toggleAutoHideHotkeyId = RegisterConfiguredHotkey("Toggle Auto-Hide", settings.ToggleAutoHideShortcut, ToggleAllFencesAutoHide, failures);
                showAllFencesHotkeyId = RegisterConfiguredHotkey("Show All Fences", settings.ShowAllFencesShortcut, ShowAllFences, failures);
                toggleTransparencyHotkeyId = RegisterConfiguredHotkey("Toggle Transparency", settings.ToggleTransparencyShortcut, ToggleTransparency, failures);
                createNewFenceHotkeyId = RegisterConfiguredHotkey("Create New Fence", settings.CreateNewFenceShortcut, CreateNewFence, failures);
                openSettingsHotkeyId = RegisterConfiguredHotkey("Open Settings", settings.OpenSettingsShortcut, ShowGlobalSettings, failures);
                toggleLockHotkeyId = RegisterConfiguredHotkey("Toggle Lock", settings.ToggleLockShortcut, ToggleAllFencesLock, failures);
                minimizeAllFencesHotkeyId = RegisterConfiguredHotkey("Minimize All Fences", settings.MinimizeAllFencesShortcut, MinimizeAllFences, failures);
                refreshFencesHotkeyId = RegisterConfiguredHotkey("Refresh Fences", settings.RefreshFencesShortcut, RefreshAllFences, failures);

                LastHotkeyRegistrationStatus = failures.Count == 0
                    ? ""
                    : string.Join(Environment.NewLine, failures);

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

            if (!ShortcutParser.TryParse(shortcut, out var parsed))
                return false;

            key = parsed.Key;
            ctrl = parsed.Ctrl;
            alt = parsed.Alt;
            shift = parsed.Shift;
            return true;
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
                            FenceInfoValidator.Normalize(fenceInfo, AppSettings.Instance);
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

        private FenceInfo? LoadFenceInfo(string metaFile)
        {
            try
            {
                var fenceInfo = DeserializeFenceInfo(metaFile);
                logger.Debug($"Deserialized fence info from {metaFile}", "FenceManager");
                return fenceInfo;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to deserialize fence metadata from {metaFile}", "FenceManager", ex);

                var backupPath = metaFile + ".bak";
                if (!File.Exists(backupPath))
                    return null;

                try
                {
                    var backupInfo = DeserializeFenceInfo(backupPath);
                    File.Copy(backupPath, metaFile, true);
                    logger.Warning($"Recovered fence metadata from backup: {backupPath}", "FenceManager");
                    return backupInfo;
                }
                catch (Exception backupEx)
                {
                    logger.Error($"Failed to recover fence metadata from backup: {backupPath}", "FenceManager", backupEx);
                    return null;
                }
            }
        }

        private int RegisterConfiguredHotkey(string label, string shortcut, Action action, List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
                return -1;

            if (!TryParseShortcut(shortcut, out var key, out var ctrl, out var alt, out var shift))
            {
                failures.Add($"{label}: invalid shortcut '{shortcut}'.");
                return -1;
            }

            if (hotkeyManager == null)
            {
                failures.Add($"{label}: hotkey manager is unavailable.");
                return -1;
            }

            var result = hotkeyManager.RegisterHotkeyDetailed(key, ctrl: ctrl, alt: alt, shift: shift, action: action);
            if (!result.Registered)
            {
                failures.Add($"{label}: '{shortcut}' could not be registered. {result.Message}");
            }

            return result.Id;
        }

        private FenceInfo DeserializeFenceInfo(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                var info = FenceInfoSerializer.Deserialize(reader) as FenceInfo;
                if (info == null)
                    throw new InvalidDataException($"Fence metadata file did not contain a valid fence: {filePath}");

                return info;
            }
        }

        public void CreateFence(string name, FenceType fenceType = FenceType.Standard)
        {
            try
            {
                logger.Info($"Creating new fence: '{name}' (type: {fenceType})", "FenceManager");
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
                    ItemSpacing = settings.DefaultItemSpacing,
                    FenceType = fenceType
                };

                switch (fenceType)
                {
                    case FenceType.LiveFolder:
                        fenceInfo.MaxItems = 50;
                        fenceInfo.UpdateInterval = 3000;
                        fenceInfo.WatchRecursive = false;
                        break;
                    case FenceType.RunningTasks:
                        fenceInfo.MaxItems = 20;
                        fenceInfo.UpdateInterval = 3000;
                        fenceInfo.ShowMinimizedWindows = true;
                        break;
                    case FenceType.ClipboardHistory:
                        fenceInfo.MaxItems = 50;
                        fenceInfo.CaptureImages = true;
                        break;
                }

                FenceInfoValidator.Normalize(fenceInfo, settings);
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
                
                FenceInfoValidator.Normalize(fenceInfo, AppSettings.Instance);
                AtomicFenceInfoWrite(metaFile, fenceInfo);
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

        private void AtomicFenceInfoWrite(string metaFile, FenceInfo fenceInfo)
        {
            var tempPath = metaFile + ".tmp";
            var backupPath = metaFile + ".bak";

            try
            {
                using (var writer = new StreamWriter(tempPath))
                {
                    FenceInfoSerializer.Serialize(writer, fenceInfo);
                }

                if (File.Exists(metaFile))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Replace(tempPath, metaFile, backupPath, true);
                }
                else
                {
                    File.Move(tempPath, metaFile);
                }
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch (Exception ex) { logger.Warning($"Failed to delete temp metadata file: {ex.Message}", "FenceManager"); }
                }

                if (File.Exists(backupPath))
                {
                    try { File.Copy(backupPath, metaFile, true); } catch (Exception ex) { logger.Warning($"Failed to restore metadata backup: {ex.Message}", "FenceManager"); }
                }

                throw;
            }
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

        private void ToggleTransparency()
        {
            try
            {
                logger.Info("Toggling transparency for all fences", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    fence.CycleTransparency();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to toggle transparency", "FenceManager", ex);
            }
        }

        private void CreateNewFence()
        {
            try
            {
                logger.Info("Creating new fence from hotkey", "FenceManager");
                CreateFence("New Fence", FenceType.Standard);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to create new fence from hotkey", "FenceManager", ex);
            }
        }

        private void ToggleAllFencesLock()
        {
            try
            {
                logger.Info("Toggling lock for all fences", "FenceManager");
                bool newState = false;
                foreach (var fence in activeFences.ToList())
                {
                    var info = fence.GetFenceInfo();
                    info.Locked = !info.Locked;
                    newState = info.Locked;
                    fence.ApplySettings();
                    UpdateFence(info);
                }
                logger.Info($"Lock toggled to {newState} for all fences", "FenceManager");
            }
            catch (Exception ex)
            {
                logger.Error("Failed to toggle lock for all fences", "FenceManager", ex);
            }
        }

        private void MinimizeAllFences()
        {
            try
            {
                logger.Info("Minimizing all fences", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    fence.ForceHide();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to minimize all fences", "FenceManager", ex);
            }
        }

        private void RefreshAllFences()
        {
            try
            {
                logger.Info("Refreshing all fences", "FenceManager");
                foreach (var fence in activeFences.ToList())
                {
                    fence.RefreshFence();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to refresh all fences", "FenceManager", ex);
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

        public string ExportAllFences(bool relativePaths)
        {
            try
            {
                var exportData = new FenceExportDocument
                {
                    Version = 1,
                    ExportDate = DateTime.UtcNow.ToString("o"),
                    Settings = AppSettings.Instance,
                    Fences = activeFences.Select(f => f.GetFenceInfo()).Select(fi =>
                    {
                        if (relativePaths)
                        {
                            var exported = JsonConvert.DeserializeObject<FenceInfo>(JsonConvert.SerializeObject(fi));
                            if (exported == null)
                                return fi;

                            exported.Files = (fi.Files ?? new List<string>()).Select(p =>
                            {
                                try { return Path.GetRelativePath(basePath, p); }
                                catch { return p; }
                            }).ToList();
                            return exported;
                        }
                        return fi;
                    }).ToList()
                };

                return JsonConvert.SerializeObject(exportData, Formatting.Indented);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to export fences", "FenceManager", ex);
                throw;
            }
        }

        public int ImportFences(string json, bool overwriteExisting)
        {
            try
            {
                var importData = FenceExportDocument.Parse(json);
                var fences = importData.Fences;
                int imported = 0;

                foreach (var fenceData in fences)
                {
                    var fenceInfo = FenceInfoValidator.Normalize(fenceData, AppSettings.Instance);

                    if (!overwriteExisting)
                    {
                        if (activeFences.Any(f => f.GetFenceInfo().Id == fenceInfo.Id))
                        {
                            fenceInfo.Id = Guid.NewGuid();
                        }

                        fenceInfo.Name = GetUniqueFenceName(fenceInfo.Name);
                    }
                    else
                    {
                        var existing = activeFences.FirstOrDefault(f => f.GetFenceInfo().Name == fenceInfo.Name);
                        if (existing != null)
                        {
                            RemoveFence(existing.GetFenceInfo());
                            existing.Close();
                        }
                    }

                    UpdateFence(fenceInfo);
                    var fenceWindow = new FenceWindow(fenceInfo);
                    activeFences.Add(fenceWindow);
                    fenceWindow.FormClosed += (s, e) => activeFences.Remove(fenceWindow);
                    fenceWindow.Show();
                    imported++;
                }

                logger.Info($"Imported {imported} fence(s)", "FenceManager");
                return imported;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to import fences", "FenceManager", ex);
                throw;
            }
        }

        private string GetUniqueFenceName(string baseName)
        {
            var existingNames = new HashSet<string>(
                activeFences.Select(f => f.GetFenceInfo().Name),
                StringComparer.OrdinalIgnoreCase);

            if (!existingNames.Contains(baseName))
                return baseName;

            var index = 2;
            string candidate;
            do
            {
                candidate = $"{baseName} ({index})";
                index++;
            }
            while (existingNames.Contains(candidate));

            return candidate;
        }

        public void Dispose()
        {
            try
            {
                logger.Info("Disposing FenceManager", "FenceManager");
                _visibilityMonitor?.Dispose();
                if (_displaySettingsChangedHandler != null)
                {
                    SystemEvents.DisplaySettingsChanged -= _displaySettingsChangedHandler;
                    _displaySettingsChangedHandler = null;
                }
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
