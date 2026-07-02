using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using Fenceless.Win32;
using Fenceless.Util;

namespace Fenceless.Model
{
    public class FenceEntry
    {
        public string Path { get; }

        public EntryType Type { get; }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(this.Path);

        private FenceEntry(string path, EntryType type)
        {
            Path = path;
            Type = type;
        }

        public static FenceEntry FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
                
            // Validate and sanitize the path
            try
            {
                var fullPath = System.IO.Path.GetFullPath(path);
                
                // Additional security checks
                if (fullPath.Contains("..") || fullPath.Contains("~"))
                {
                    Logger.Instance?.Warning($"Potentially unsafe path blocked: {fullPath}", "FenceEntry");
                    return null;
                }
                
                // Check if path exists
                if (File.Exists(fullPath))
                    return new FenceEntry(fullPath, EntryType.File);
                else if (Directory.Exists(fullPath))
                    return new FenceEntry(fullPath, EntryType.Folder);
                else 
                    return null;
            }
            catch (Exception ex)
            {
                Logger.Instance?.Warning($"Invalid path provided: {path} - {ex.Message}", "FenceEntry");
                return null;
            }
        }

        public Icon ExtractIcon(ThumbnailProvider thumbnailProvider)
        {
            if (Type == EntryType.File)
            {
                if (thumbnailProvider.IsSupported(Path))
                    return thumbnailProvider.GenerateThumbnail(Path);
                else
                    return Icon.ExtractAssociatedIcon(Path);
            }
            else
            {
                return IconUtil.FolderLarge;
            }
        }

        public void Open()
        {
            Task.Run(() =>
            {
                var logger = Logger.Instance;
                try
                {
                    // Validate the path before using it
                    if (string.IsNullOrWhiteSpace(Path))
                    {
                        logger?.Warning("Cannot open item with null or empty path", "FenceEntry");
                        return;
                    }
                    
                    // Sanitize path and perform security checks
                    var fullPath = System.IO.Path.GetFullPath(this.Path);
                    if (fullPath.Contains("..") || fullPath.Contains("~"))
                    {
                        logger?.Warning($"Potentially unsafe path blocked: {fullPath}", "FenceEntry");
                        return;
                    }
                    
                    // Verify the path still exists before trying to open
                    if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                    {
                        logger?.Warning($"Cannot open item that no longer exists: {fullPath}", "FenceEntry");
                        return;
                    }
                    
                    if (Type == EntryType.File)
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = true,
                            ErrorDialog = false // We'll handle errors ourselves
                        };
                        Process.Start(startInfo);
                        logger?.Debug($"Opened file: {fullPath}", "FenceEntry");
                    }
                    else if (Type == EntryType.Folder)
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{fullPath}\"",
                            UseShellExecute = true,
                            ErrorDialog = false // We'll handle errors ourselves
                        };
                        Process.Start(startInfo);
                        logger?.Debug($"Opened folder: {fullPath}", "FenceEntry");
                    }
                }
                catch (Exception e)
                {
                    logger?.Error($"Failed to open item '{Path}': {e.Message}", "FenceEntry", e);
                    // Show a user-friendly error message
                    System.Windows.Forms.MessageBox.Show(
                        $"Failed to open '{System.IO.Path.GetFileName(Path)}':\n\n{e.Message}",
                        "Error Opening Item",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error
                    );
                }
            });
        }
    }
}
