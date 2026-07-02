using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class LiveFolderFence : IFenceProvider, IWidgetDataProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly FileSystemWatcher watcher;
        private readonly Logger logger;
        private readonly object snapshotLock = new object();
        private Timer debounceTimer;
        private FenceWidgetSnapshot snapshot;
        private bool disposed = false;

        public event Action? ItemsChanged;

        public LiveFolderFence(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            logger = Logger.Instance;
            snapshot = FenceWidgetSnapshot.Empty(FenceType.LiveFolder, fenceInfo.Name, "No folder configured");

            if (string.IsNullOrEmpty(fenceInfo.WatchPath) || !Directory.Exists(fenceInfo.WatchPath))
            {
                logger.Warning($"Invalid watch path for LiveFolder fence '{fenceInfo.Name}': '{fenceInfo.WatchPath}'", "LiveFolderFence");
                snapshot = FenceWidgetSnapshot.Empty(FenceType.LiveFolder, fenceInfo.Name, "Folder unavailable");
                return;
            }

            watcher = new FileSystemWatcher(fenceInfo.WatchPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                IncludeSubdirectories = fenceInfo.WatchRecursive,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            PopulateFiles();
            logger.Info($"LiveFolder fence '{fenceInfo.Name}' watching: {fenceInfo.WatchPath}", "LiveFolderFence");
        }

        private void PopulateFiles()
        {
            try
            {
                var files = GetFilteredItems();
                fenceInfo.Files.Clear();
                fenceInfo.Files.AddRange(files.Select(item => item.FullName).Take(fenceInfo.MaxItems > 0 ? fenceInfo.MaxItems : int.MaxValue));
                UpdateSnapshot(files.Take(fenceInfo.MaxItems > 0 ? fenceInfo.MaxItems : int.MaxValue).ToList());
                ItemsChanged?.Invoke();
                logger.Debug($"Populated {fenceInfo.Files.Count} files for LiveFolder '{fenceInfo.Name}'", "LiveFolderFence");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to populate files for LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }

        private List<FileSystemInfo> GetFilteredItems()
        {
            try
            {
                var searchOption = fenceInfo.WatchRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var root = new DirectoryInfo(fenceInfo.WatchPath);
                var files = root.Exists
                    ? root.EnumerateFileSystemInfos("*", searchOption).ToList()
                    : new List<FileSystemInfo>();

                if (!string.IsNullOrEmpty(fenceInfo.FileFilter))
                {
                    var filters = fenceInfo.FileFilter.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var extensions = filters.Where(f => f.StartsWith("*") || f.StartsWith(".")).ToArray();
                    if (extensions.Length > 0)
                    {
                        files = files.Where(f =>
                        {
                            if (f is DirectoryInfo)
                                return true;

                            var ext = Path.GetExtension(f.FullName);
                            return extensions.Any(e => e.TrimStart('*').Equals(ext, StringComparison.OrdinalIgnoreCase));
                        }).ToList();
                    }
                }

                return files
                    .OrderByDescending(f => SafeLastWriteTime(f))
                    .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.Error($"Error filtering files in LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
                return new List<FileSystemInfo>();
            }
        }

        private static DateTime SafeLastWriteTime(FileSystemInfo info)
        {
            try { return info.LastWriteTime; }
            catch { return DateTime.MinValue; }
        }

        private void UpdateSnapshot(IReadOnlyList<FileSystemInfo> items)
        {
            var widgetItems = new List<FenceWidgetItem>(items.Count);
            foreach (var item in items)
            {
                try
                {
                    var isDirectory = item is DirectoryInfo;
                    var subtitle = isDirectory ? "Folder" : Path.GetExtension(item.Name).TrimStart('.').ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(subtitle))
                        subtitle = isDirectory ? "Folder" : "File";

                    var detail = item.LastWriteTime.ToString("g");
                    if (item is FileInfo fileInfo)
                    {
                        detail = $"{FormatFileSize(fileInfo.Length)}  |  {detail}";
                    }

                    widgetItems.Add(new FenceWidgetItem(
                        item.FullName,
                        isDirectory ? FenceEntryKind.Folder : FenceEntryKind.File,
                        item.Name,
                        subtitle,
                        detail,
                        item.FullName,
                        item.FullName,
                        timestamp: item.LastWriteTime,
                        iconPath: item.FullName));
                }
                catch
                {
                }
            }

            var subtitleText = string.IsNullOrEmpty(fenceInfo.WatchPath)
                ? "No folder selected"
                : fenceInfo.WatchPath;
            var status = widgetItems.Count == 0 ? "No matching items" : $"{widgetItems.Count} item{(widgetItems.Count == 1 ? "" : "s")}";

            lock (snapshotLock)
            {
                snapshot = new FenceWidgetSnapshot(FenceType.LiveFolder, widgetItems, fenceInfo.Name, subtitleText, status, DateTime.Now);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        private void SchedulePopulate()
        {
            if (disposed) return;

            if (debounceTimer == null)
            {
                debounceTimer = new Timer(_ => PopulateFiles(), null, Timeout.Infinite, Timeout.Infinite);
            }

            debounceTimer.Change(250, Timeout.Infinite);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                SchedulePopulate();
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling file system change for '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                SchedulePopulate();
            }
            catch (Exception ex)
            {
                logger.Error($"Error handling rename for '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            logger.Error($"FileSystemWatcher error for '{fenceInfo.Name}': {e.GetException()?.Message}", "LiveFolderFence", e.GetException());
        }

        public void Refresh()
        {
            PopulateFiles();
        }

        public FenceWidgetSnapshot GetSnapshot()
        {
            lock (snapshotLock)
            {
                return snapshot;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            try
            {
                if (watcher != null)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                debounceTimer?.Dispose();
                logger.Debug($"Disposed LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence");
            }
            catch (Exception ex)
            {
                logger.Error($"Error disposing LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }
    }
}
