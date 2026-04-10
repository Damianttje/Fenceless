using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fenceless.Util
{
    public class ThumbnailProvider : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        // Supported .NET images as per https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromfile
        private static readonly string[] SupportedExtensions =
        {
            ".bmp",
            ".gif",
            ".jpg",
            ".jpeg",
            ".png",
            ".tiff",
            ".tif"
        };

        private class ThumbnailState
        {
            public Icon icon;
        }

        // Only allow 4 concurrent images to be decoded to try and prevent OOM errors
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(4);
        private readonly ConcurrentDictionary<string, ThumbnailState> iconCache = new ConcurrentDictionary<string, ThumbnailState>();
        public event EventHandler IconThumbnailLoaded;
        private bool disposed = false;
        private readonly Logger logger = Logger.Instance;

        public bool IsSupported(string path)
        {
            return SupportedExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        public Icon GenerateThumbnail(string path)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ThumbnailProvider));

            if (!iconCache.TryGetValue(path, out var state))
            {
                return SubmitGeneratorTask(path).icon;
            }
            else
            {
                return state.icon;
            }
        }

        private ThumbnailState SubmitGeneratorTask(string path)
        {
            var state = new ThumbnailState() { icon = Icon.ExtractAssociatedIcon(path) };
            iconCache[path] = state;

            Task.Run(() =>
            {
                if (disposed) return;
                
                try
                {
                    semaphore.Wait();
                    if (disposed) return;

                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096))
                    {
                        using (var ms = new MemoryStream())
                        {
                            fileStream.CopyTo(ms, 8192);
                            ms.Position = 0;
                            using (var img = Image.FromStream(ms))
                            {
                                var thumb = (Bitmap)img.GetThumbnailImage(32, 32, () => false, IntPtr.Zero);
                                var hIcon = thumb.GetHicon();
                                var icon = Icon.FromHandle(hIcon);
                                state.icon = icon;
                                DestroyIcon(hIcon);
                                IconThumbnailLoaded?.Invoke(this, new EventArgs());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error($"Failed to generate thumbnail for {path}", "ThumbnailProvider", ex);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            return state;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                semaphore?.Dispose();
                
                foreach (var cache in iconCache.Values)
                {
                    cache.icon?.Dispose();
                }
                iconCache.Clear();
            }
        }
    }
}
