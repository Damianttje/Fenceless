using System;
using System.IO;

namespace Fenceless.Model
{
    public enum FenceEntryKind
    {
        File,
        Folder,
        Task,
        ClipboardText,
        ClipboardImage,
        Missing
    }

    public sealed class FenceEntryModel
    {
        public FenceEntryModel(string id, FenceEntryKind kind, string displayName, string path = "", IntPtr taskHandle = default, int? clipboardIndex = null)
        {
            Id = id ?? string.Empty;
            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            Path = path ?? string.Empty;
            TaskHandle = taskHandle;
            ClipboardIndex = clipboardIndex;
        }

        public string Id { get; }
        public FenceEntryKind Kind { get; }
        public string DisplayName { get; }
        public string Path { get; }
        public IntPtr TaskHandle { get; }
        public int? ClipboardIndex { get; }

        public static FenceEntryModel FromLegacyValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new FenceEntryModel(string.Empty, FenceEntryKind.Missing, string.Empty);

            if (value.StartsWith("task:", StringComparison.OrdinalIgnoreCase))
            {
                var colonIndex = value.IndexOf(':', 5);
                var displayName = colonIndex >= 0 && colonIndex + 1 < value.Length
                    ? value.Substring(colonIndex + 1)
                    : "Running task";

                var handle = IntPtr.Zero;
                if (colonIndex > 5 && long.TryParse(value.Substring(5, colonIndex - 5), out var hwnd))
                    handle = new IntPtr(hwnd);

                return new FenceEntryModel(value, FenceEntryKind.Task, displayName, taskHandle: handle);
            }

            if (value.StartsWith("clip:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(new[] { ':' }, 3);
                var index = parts.Length >= 2 && int.TryParse(parts[1], out var parsedIndex)
                    ? parsedIndex
                    : (int?)null;
                var displayName = parts.Length >= 3 ? parts[2] : "Clipboard item";

                return new FenceEntryModel(value, FenceEntryKind.ClipboardText, displayName, clipboardIndex: index);
            }

            if (value.StartsWith("clipimg:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(new[] { ':' }, 3);
                var index = parts.Length >= 2 && int.TryParse(parts[1], out var parsedIndex)
                    ? parsedIndex
                    : (int?)null;
                var displayName = parts.Length >= 3 ? parts[2] : "Clipboard image";

                return new FenceEntryModel(value, FenceEntryKind.ClipboardImage, displayName, clipboardIndex: index);
            }

            if (File.Exists(value))
                return new FenceEntryModel(value, FenceEntryKind.File, System.IO.Path.GetFileNameWithoutExtension(value), path: value);

            if (Directory.Exists(value))
                return new FenceEntryModel(value, FenceEntryKind.Folder, System.IO.Path.GetFileName(value), path: value);

            return new FenceEntryModel(value, FenceEntryKind.Missing, System.IO.Path.GetFileName(value), path: value);
        }
    }
}
