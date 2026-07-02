using System.Drawing;
using System.Windows.Forms;
using Fenceless.Model;
using Fenceless.UI.Widgets;
using Fenceless.Util;

var tests = new (string Name, Action Body)[]
{
    ("Shortcut parser accepts configured modifier shortcuts", ShortcutParserAcceptsConfiguredModifierShortcuts),
    ("Shortcut parser rejects unsafe or unsupported shortcuts", ShortcutParserRejectsUnsafeOrUnsupportedShortcuts),
    ("Legacy virtual entries parse into typed models", LegacyVirtualEntriesParseIntoTypedModels),
    ("Grid layout clamps to at least one column", GridLayoutClampsToAtLeastOneColumn),
    ("Grid layout maps points to bounded item indexes", GridLayoutMapsPointsToBoundedItemIndexes),
    ("Layout snapshot hit tests filtered items", LayoutSnapshotHitTestsFilteredItems),
    ("Icon cache returns folder bitmaps for directories", IconCacheReturnsFolderBitmapsForDirectories),
    ("Clipboard image legacy entries parse", ClipboardImageLegacyEntriesParse),
    ("Widget renderer hit tests rows", WidgetRendererHitTestsRows),
    ("Live folder snapshots include files and directories", LiveFolderSnapshotsIncludeFilesAndDirectories)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

return failures == 0 ? 0 : 1;

static void ShortcutParserAcceptsConfiguredModifierShortcuts()
{
    Assert.True(ShortcutParser.TryParse("Ctrl+Alt+T", out var parsed), "Ctrl+Alt+T should parse");
    Assert.Equal(Keys.T, parsed.Key, "key");
    Assert.True(parsed.Ctrl, "ctrl");
    Assert.True(parsed.Alt, "alt");
    Assert.False(parsed.Shift, "shift");

    Assert.True(ShortcutParser.TryParse("F5", out var refresh), "F5 should parse without modifiers");
    Assert.Equal(Keys.F5, refresh.Key, "refresh key");
}

static void ShortcutParserRejectsUnsafeOrUnsupportedShortcuts()
{
    Assert.False(ShortcutParser.TryParse("Ctrl", out _), "modifier-only shortcut should fail");
    Assert.False(ShortcutParser.TryParse("Windows+F", out _), "Windows modifier is not supported by current hotkey manager");
    Assert.False(ShortcutParser.TryParse("A", out _), "letter shortcut without modifiers should fail");
    Assert.False(ShortcutParser.TryParse("Ctrl+Alt+T+H", out _), "multiple non-modifier keys should fail");
}

static void GridLayoutClampsToAtLeastOneColumn()
{
    var layout = FenceGridLayout.Calculate(20, 15, 64, 75, 35);
    Assert.Equal(1, layout.ItemsPerRow, "items per row");
    Assert.Equal(109, layout.ActualItemHeight, "item height");
}

static void LegacyVirtualEntriesParseIntoTypedModels()
{
    var task = FenceEntryModel.FromLegacyValue("task:12345:Explorer");
    Assert.Equal(FenceEntryKind.Task, task.Kind, "task kind");
    Assert.Equal("Explorer", task.DisplayName, "task display name");
    Assert.Equal(new IntPtr(12345), task.TaskHandle, "task handle");

    var clip = FenceEntryModel.FromLegacyValue("clip:7:copied text");
    Assert.Equal(FenceEntryKind.ClipboardText, clip.Kind, "clip kind");
    Assert.Equal(7, clip.ClipboardIndex, "clip index");
    Assert.Equal("copied text", clip.DisplayName, "clip display name");

    var clipImage = FenceEntryModel.FromLegacyValue("clipimg:3:Image");
    Assert.Equal(FenceEntryKind.ClipboardImage, clipImage.Kind, "clip image kind");
    Assert.Equal(3, clipImage.ClipboardIndex, "clip image index");
}

static void GridLayoutMapsPointsToBoundedItemIndexes()
{
    var layout = FenceGridLayout.Calculate(300, 15, 32, 75, 35);
    Assert.Equal(3, layout.ItemsPerRow, "items per row");

    var secondRowPoint = layout.GetItemPosition(4, titleHeight: 25, scrollOffset: 0);
    var index = layout.GetGridIndex(new Point(secondRowPoint.X + 1, secondRowPoint.Y + 1), 25, 0, maxItems: 10);
    Assert.Equal(4, index, "index");

    var bounded = layout.GetGridIndex(new Point(5000, 5000), 25, 0, maxItems: 5);
    Assert.Equal(5, bounded, "bounded index");
}

static void LayoutSnapshotHitTestsFilteredItems()
{
    var entries = new[] { "first.txt", "second.txt" };
    var snapshot = FenceItemLayoutSnapshot.Create(
        entries,
        clientWidth: 300,
        titleHeight: 25,
        scrollOffset: 0,
        itemSpacing: 15,
        iconSize: 32,
        baseItemWidth: 75,
        baseTextHeight: 35);

    var secondBounds = snapshot.Items[1].Bounds;
    var hit = snapshot.HitTest(new Point(secondBounds.X + 2, secondBounds.Y + 2));
    Assert.Equal("second.txt", hit, "hit test");
}

static void IconCacheReturnsFolderBitmapsForDirectories()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "FencelessTests_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempPath);

    try
    {
        using var cache = new IconCache(4);
        var bitmap = cache.GetIcon(tempPath, 32);
        if (bitmap == null)
            throw new InvalidOperationException("folder bitmap should exist");

        Assert.Equal(32, bitmap.Width, "folder bitmap width");
        Assert.Equal(32, bitmap.Height, "folder bitmap height");
    }
    finally
    {
        Directory.Delete(tempPath, recursive: true);
    }
}

static void ClipboardImageLegacyEntriesParse()
{
    var entry = FenceEntryModel.FromLegacyValue("clipimg:12:Screenshot");
    Assert.Equal(FenceEntryKind.ClipboardImage, entry.Kind, "kind");
    Assert.Equal(12, entry.ClipboardIndex, "index");
    Assert.Equal("Screenshot", entry.DisplayName, "display name");
}

static void WidgetRendererHitTestsRows()
{
    var item = new FenceWidgetItem(
        "item-1",
        FenceEntryKind.File,
        "Report.txt",
        "TXT",
        "1 KB",
        legacyValue: "C:\\Temp\\Report.txt",
        path: "C:\\Temp\\Report.txt");
    var snapshot = new FenceWidgetSnapshot(FenceType.LiveFolder, new[] { item }, "Live", "C:\\Temp", "1 item");
    using var font = new Font("Segoe UI", 9);
    using var iconCache = new IconCache(2);
    using var bitmap = new Bitmap(1, 1);
    using var graphics = Graphics.FromImage(bitmap);
    var context = new FenceWidgetRenderContext(
        graphics: graphics,
        bounds: new Rectangle(0, 0, 300, 220),
        fenceInfo: new FenceInfo(Guid.NewGuid()) { Name = "Live", FenceType = FenceType.LiveFolder },
        snapshot: snapshot,
        titleHeight: 25,
        scrollOffset: 0,
        selectedItem: "",
        hoveringItem: "",
        titleFont: font,
        bodyFont: font,
        iconCache: iconCache,
        textColor: Color.White,
        accentColor: Color.Green);

    var renderer = new LiveFolderWidgetRenderer();
    var hit = renderer.HitTest(context, new Point(20, 88));
    Assert.Equal("C:\\Temp\\Report.txt", hit, "hit");
}

static void LiveFolderSnapshotsIncludeFilesAndDirectories()
{
    var root = Path.Combine(Path.GetTempPath(), "FencelessLiveFolder_" + Guid.NewGuid().ToString("N"));
    var childDir = Path.Combine(root, "Child");
    var file = Path.Combine(root, "note.txt");
    Directory.CreateDirectory(childDir);
    File.WriteAllText(file, "hello");

    try
    {
        var info = new FenceInfo(Guid.NewGuid())
        {
            Name = "Live",
            FenceType = FenceType.LiveFolder,
            WatchPath = root,
            MaxItems = 10
        };
        using var provider = new LiveFolderFence(info);
        var snapshot = provider.GetSnapshot();
        Assert.True(snapshot.Items.Any(item => item.Path == file), "file should be present");
        Assert.True(snapshot.Items.Any(item => item.Path == childDir), "directory should be present");
    }
    finally
    {
        Directory.Delete(root, recursive: true);
    }
}

static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static void False(bool condition, string message)
    {
        if (condition) throw new InvalidOperationException(message);
    }

    public static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}
