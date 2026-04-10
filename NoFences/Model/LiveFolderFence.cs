using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class LiveFolderFence : IFenceProvider
    {
        private readonly FenceInfo fenceInfo;
        private readonly FileSystemWatcher watcher;
        private readonly Logger logger;
        private bool disposed = false;

        public event Action? ItemsChanged;

        public LiveFolderFence(FenceInfo fenceInfo)
        {
            this.fenceInfo = fenceInfo;
            logger = Logger.Instance;

            if (string.IsNullOrEmpty(fenceInfo.WatchPath) || !Directory.Exists(fenceInfo.WatchPath))
            {
                logger.Warning($"Invalid watch path for LiveFolder fence '{fenceInfo.Name}': '{fenceInfo.WatchPath}'", "LiveFolderFence");
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
                var files = GetFilteredFiles();
                fenceInfo.Files.Clear();
                fenceInfo.Files.AddRange(files.Take(fenceInfo.MaxItems > 0 ? fenceInfo.MaxItems : int.MaxValue));
                ItemsChanged?.Invoke();
                logger.Debug($"Populated {fenceInfo.Files.Count} files for LiveFolder '{fenceInfo.Name}'", "LiveFolderFence");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to populate files for LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }

        private List<string> GetFilteredFiles()
        {
            try
            {
                var searchOption = fenceInfo.WatchRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(fenceInfo.WatchPath, "*.*", searchOption);

                if (!string.IsNullOrEmpty(fenceInfo.FileFilter))
                {
                    var filters = fenceInfo.FileFilter.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var extensions = filters.Where(f => f.StartsWith("*") || f.StartsWith(".")).ToArray();
                    if (extensions.Length > 0)
                    {
                        files = files.Where(f =>
                        {
                            var ext = Path.GetExtension(f);
                            return extensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                        }).ToArray();
                    }
                }

                return files.OrderBy(f => f).ToList();
            }
            catch (Exception ex)
            {
                logger.Error($"Error filtering files in LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
                return new List<string>();
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                PopulateFiles();
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
                PopulateFiles();
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
                logger.Debug($"Disposed LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence");
            }
            catch (Exception ex)
            {
                logger.Error($"Error disposing LiveFolder fence '{fenceInfo.Name}'", "LiveFolderFence", ex);
            }
        }
    }
}
