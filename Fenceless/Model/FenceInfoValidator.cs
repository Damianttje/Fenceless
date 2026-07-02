using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fenceless.Model
{
    public static class FenceInfoValidator
    {
        public const int MinFenceSize = 200;
        public const int MaxFenceSize = 2000;
        public const int MinTitleHeight = 15;
        public const int MaxTitleHeight = 100;
        public const int MinAutoHideDelay = 500;
        public const int MaxAutoHideDelay = 10000;
        public const int MinIconSize = 16;
        public const int MaxIconSize = 256;
        public const int MinItemSpacing = 5;
        public const int MaxItemSpacing = 50;
        public const int MinUpdateInterval = 500;
        public const int MaxUpdateInterval = 30000;
        public const int MinMaxItems = 1;
        public const int MaxWidgetItems = 500;

        public static FenceInfo Normalize(FenceInfo info, AppSettings? defaults = null)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (info.Id == Guid.Empty)
                info.Id = Guid.NewGuid();

            info.Name = NormalizeName(info.Name);
            info.Width = ClampOrDefault(info.Width, MinFenceSize, MaxFenceSize, defaults?.DefaultFenceWidth ?? 524);
            info.Height = ClampOrDefault(info.Height, MinFenceSize, MaxFenceSize, defaults?.DefaultFenceHeight ?? 517);
            info.TitleHeight = ClampOrDefault(info.TitleHeight, MinTitleHeight, MaxTitleHeight, defaults?.DefaultTitleHeight ?? 25);
            info.Transparency = Clamp(info.Transparency, 0, 100);
            info.AutoHideDelay = ClampOrDefault(info.AutoHideDelay, MinAutoHideDelay, MaxAutoHideDelay, defaults?.DefaultAutoHideDelay ?? 2000);
            info.BorderWidth = Clamp(info.BorderWidth, 0, 10);
            info.CornerRadius = Clamp(info.CornerRadius, 0, 50);
            info.IconSize = ClampOrDefault(info.IconSize, MinIconSize, MaxIconSize, defaults?.DefaultIconSize ?? 32);
            info.ItemSpacing = ClampOrDefault(info.ItemSpacing, MinItemSpacing, MaxItemSpacing, defaults?.DefaultItemSpacing ?? 15);
            info.BackgroundTransparency = Clamp(info.BackgroundTransparency, 0, 100);
            info.TitleBackgroundTransparency = Clamp(info.TitleBackgroundTransparency, 0, 100);
            info.TextTransparency = Clamp(info.TextTransparency, 0, 100);
            info.BorderTransparency = Clamp(info.BorderTransparency, 0, 100);
            info.FenceTypeValue = Enum.IsDefined(typeof(FenceType), info.FenceTypeValue)
                ? info.FenceTypeValue
                : (int)FenceType.Standard;

            info.Files = info.Files?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            info.SortColumn = info.SortColumn ?? string.Empty;
            info.SearchFilter = info.SearchFilter ?? string.Empty;
            info.WatchPath = info.WatchPath ?? string.Empty;
            info.FileFilter = info.FileFilter ?? string.Empty;
            info.ProcessFilter = info.ProcessFilter ?? string.Empty;
            info.WidgetDisplayMode = info.WidgetDisplayMode ?? "Auto";

            info.UpdateInterval = ClampOrDefault(info.UpdateInterval, MinUpdateInterval, MaxUpdateInterval, 3000);
            info.MaxItems = ClampOrDefault(info.MaxItems, MinMaxItems, MaxWidgetItems, DefaultMaxItems(info.FenceType));

            return info;
        }

        public static bool IsLiveFolderPathValid(FenceInfo info)
        {
            if (info == null || info.FenceType != FenceType.LiveFolder)
                return true;

            return !string.IsNullOrWhiteSpace(info.WatchPath) && Directory.Exists(info.WatchPath);
        }

        private static string NormalizeName(string name)
        {
            var trimmed = string.IsNullOrWhiteSpace(name) ? "Untitled Fence" : name.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed.Substring(0, 80);
        }

        private static int DefaultMaxItems(FenceType type)
        {
            switch (type)
            {
                case FenceType.RunningTasks:
                    return 20;
                case FenceType.LiveFolder:
                case FenceType.ClipboardHistory:
                    return 50;
                default:
                    return 50;
            }
        }

        private static int ClampOrDefault(int value, int min, int max, int fallback)
        {
            if (value <= 0)
                value = fallback;

            return Clamp(value, min, max);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
