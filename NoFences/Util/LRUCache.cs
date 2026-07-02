using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Fenceless.Win32;

namespace Fenceless.Util
{
    /// <summary>
    /// Thread-safe Least Recently Used (LRU) cache for Bitmap objects with automatic disposal
    /// </summary>
    public class LRUCache<TKey, TValue> : IDisposable where TValue : IDisposable
    {
        private readonly int _maxSize;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public LRUCache(int maxSize = 50)
        {
            _maxSize = Math.Max(1, maxSize);
            _cacheMap = new Dictionary<TKey, LinkedListNode<CacheItem>>();
            _lruList = new LinkedList<CacheItem>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    value = default(TValue);
                    return false;
                }

                if (_cacheMap.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (value == null) return;

            lock (_lock)
            {
                if (_disposed)
                {
                    value?.Dispose();
                    return;
                }

                if (_cacheMap.TryGetValue(key, out var existingNode))
                {
                    // Update existing item
                    _lruList.Remove(existingNode);
                    existingNode.Value.Value?.Dispose();
                    existingNode.Value.Value = value;
                    _lruList.AddFirst(existingNode);
                }
                else
                {
                    // Add new item
                    var newItem = new CacheItem { Key = key, Value = value };
                    var newNode = _lruList.AddFirst(newItem);
                    _cacheMap[key] = newNode;

                    // Remove oldest items if over capacity
                    while (_lruList.Count > _maxSize)
                    {
                        var oldestItem = _lruList.Last;
                        if (oldestItem != null)
                        {
                            _cacheMap.Remove(oldestItem.Value.Key);
                            oldestItem.Value.Value?.Dispose();
                            _lruList.RemoveLast();
                        }
                    }
                }
            }
        }

        public void Remove(TKey key)
        {
            lock (_lock)
            {
                if (_disposed) return;

                if (_cacheMap.TryGetValue(key, out var node))
                {
                    _cacheMap.Remove(key);
                    _lruList.Remove(node);
                    node.Value.Value?.Dispose();
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                if (_disposed) return;

                foreach (var item in _lruList)
                {
                    item.Value?.Dispose();
                }

                _cacheMap.Clear();
                _lruList.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _disposed ? 0 : _cacheMap.Count;
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;

                foreach (var item in _lruList)
                {
                    item.Value?.Dispose();
                }

                _cacheMap.Clear();
                _lruList.Clear();
                _disposed = true;
            }
        }

        private class CacheItem
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
        }
    }

    /// <summary>
    /// Specialized LRU cache for icons with built-in loading and error handling
    /// </summary>
    public class IconCache : IDisposable
    {
        private readonly LRUCache<string, Bitmap> _cache;
        private readonly Logger _logger;
        private readonly object _loadLock = new object();
        private bool _disposed = false;

        public IconCache(int maxSize = 50)
        {
            _cache = new LRUCache<string, Bitmap>(maxSize);
            _logger = Logger.Instance;
        }

        public Bitmap GetIcon(string filePath, int size)
        {
            if (string.IsNullOrEmpty(filePath) || _disposed)
                return null;

            var cacheKey = $"{filePath}|{size}";

            // Try to get from cache first
            if (_cache.TryGet(cacheKey, out var cachedIcon))
            {
                return cachedIcon;
            }

            // Load icon if not in cache
            lock (_loadLock)
            {
                // Double-check after acquiring lock
                if (_cache.TryGet(cacheKey, out cachedIcon))
                {
                    return cachedIcon;
                }

                try
                {
                    var icon = LoadIconFromFile(filePath, size);
                    if (icon != null)
                    {
                        _cache.Add(cacheKey, icon);
                        return icon;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Failed to load icon for {filePath}", "IconCache", ex);
                }
            }

            return null;
        }

        private Bitmap LoadIconFromFile(string filePath, int size)
        {
            try
            {
                if (System.IO.Directory.Exists(filePath))
                {
                    using (var folderBitmap = IconUtil.FolderLarge.ToBitmap())
                    {
                        return new Bitmap(folderBitmap, new Size(size, size));
                    }
                }

                if (System.IO.File.Exists(filePath))
                {
                    using (var originalIcon = Icon.ExtractAssociatedIcon(filePath))
                    {
                        if (originalIcon != null)
                        {
                            using (var iconBitmap = originalIcon.ToBitmap())
                            {
                                return new Bitmap(iconBitmap, new Size(size, size));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Warning($"Could not extract icon from {filePath}: {ex.Message}", "IconCache");
            }

            // Return default icon if extraction fails
            try
            {
                using (var defaultBitmap = SystemIcons.Application.ToBitmap())
                {
                    return new Bitmap(defaultBitmap, new Size(size, size));
                }
            }
            catch
            {
                // Ultimate fallback
                return new Bitmap(size, size);
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            _logger?.Debug("Icon cache cleared", "IconCache");
        }

        public int CacheCount => _cache.Count;

        public void Dispose()
        {
            if (_disposed) return;
            
            _cache?.Dispose();
            _disposed = true;
        }
    }
}
