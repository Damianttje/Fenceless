using System.Drawing;
using System.Windows.Forms;
using Fenceless.Model;
using Fenceless.UI.Widgets;
using Fenceless.Util;
using Newtonsoft.Json.Linq;

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
    ("Live folder snapshots include files and directories", LiveFolderSnapshotsIncludeFilesAndDirectories),
    ("Fence info normalization repairs invalid loaded values", FenceInfoNormalizationRepairsInvalidLoadedValues),
    ("Fence export parser rejects invalid documents", FenceExportParserRejectsInvalidDocuments),
    ("Fence export parser accepts typed documents", FenceExportParserAcceptsTypedDocuments),
    ("Update release selection honors prerelease setting", UpdateReleaseSelectionHonorsPrereleaseSetting),
    ("Update asset matching requires strict package names", UpdateAssetMatchingRequiresStrictPackageNames),
    ("Update SHA256 validation accepts standard checksum files", UpdateSha256ValidationAcceptsStandardChecksumFiles),
    ("Update prompt is suppressed for skipped version", UpdatePromptIsSuppressedForSkippedVersion),
    ("Update staging validation requires root app files", UpdateStagingValidationRequiresRootAppFiles)
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

static void FenceInfoNormalizationRepairsInvalidLoadedValues()
{
    var info = new FenceInfo
    {
        Id = Guid.Empty,
        Name = "   ",
        Width = -1,
        Height = 99999,
        TitleHeight = 0,
        Transparency = 200,
        AutoHideDelay = 1,
        BorderWidth = -5,
        CornerRadius = 99,
        IconSize = 3,
        ItemSpacing = 100,
        BackgroundTransparency = -1,
        TitleBackgroundTransparency = 101,
        TextTransparency = 500,
        BorderTransparency = -30,
        FenceTypeValue = 999,
        Files = new List<string> { "", "C:\\Temp\\a.txt", "C:\\Temp\\a.txt" },
        SortColumn = null!,
        SearchFilter = null!,
        WatchPath = null!,
        FileFilter = null!,
        ProcessFilter = null!,
        WidgetDisplayMode = null!,
        UpdateInterval = -1,
        MaxItems = 0
    };

    FenceInfoValidator.Normalize(info);

    Assert.False(info.Id == Guid.Empty, "id should be generated");
    Assert.Equal("Untitled Fence", info.Name, "name");
    Assert.Equal(524, info.Width, "width");
    Assert.Equal(2000, info.Height, "height");
    Assert.Equal(25, info.TitleHeight, "title height");
    Assert.Equal(100, info.Transparency, "transparency");
    Assert.Equal(500, info.AutoHideDelay, "auto hide delay");
    Assert.Equal(0, info.BorderWidth, "border width");
    Assert.Equal(50, info.CornerRadius, "corner radius");
    Assert.Equal(16, info.IconSize, "icon size");
    Assert.Equal(50, info.ItemSpacing, "item spacing");
    Assert.Equal(0, info.BackgroundTransparency, "background opacity");
    Assert.Equal(100, info.TitleBackgroundTransparency, "title opacity");
    Assert.Equal(100, info.TextTransparency, "text opacity");
    Assert.Equal(0, info.BorderTransparency, "border opacity");
    Assert.Equal(FenceType.Standard, info.FenceType, "fence type");
    Assert.Equal(1, info.Files.Count, "file count");
    Assert.Equal("", info.SortColumn, "sort column");
    Assert.Equal("", info.SearchFilter, "search filter");
    Assert.Equal("", info.WatchPath, "watch path");
    Assert.Equal("", info.FileFilter, "file filter");
    Assert.Equal("", info.ProcessFilter, "process filter");
    Assert.Equal("Auto", info.WidgetDisplayMode, "display mode");
    Assert.Equal(3000, info.UpdateInterval, "update interval");
    Assert.Equal(50, info.MaxItems, "max items");
}

static void FenceExportParserRejectsInvalidDocuments()
{
    Assert.Throws<InvalidDataException>(() => FenceExportDocument.Parse(""));
    Assert.Throws<InvalidDataException>(() => FenceExportDocument.Parse("{\"Version\":1}"));
}

static void FenceExportParserAcceptsTypedDocuments()
{
    var json = """
    {
      "Version": 1,
      "ExportDate": "2026-07-02T00:00:00Z",
      "Fences": [
        {
          "Id": "00000000-0000-0000-0000-000000000000",
          "Name": "Imported",
          "Width": 100,
          "Height": 9999,
          "FenceTypeValue": 2,
          "MaxItems": 0
        }
      ]
    }
    """;

    var doc = FenceExportDocument.Parse(json);

    Assert.Equal(1, doc.Fences.Count, "fence count");
    Assert.Equal("Imported", doc.Fences[0].Name, "name");
    Assert.False(doc.Fences[0].Id == Guid.Empty, "id should be generated");
    Assert.Equal(200, doc.Fences[0].Width, "width");
    Assert.Equal(2000, doc.Fences[0].Height, "height");
    Assert.Equal(FenceType.RunningTasks, doc.Fences[0].FenceType, "type");
    Assert.Equal(20, doc.Fences[0].MaxItems, "running tasks max items");
}

static void UpdateReleaseSelectionHonorsPrereleaseSetting()
{
    var releases = JArray.Parse("""
    [
      { "tag_name": "v1.1.0.0-beta.1", "prerelease": true, "draft": false },
      { "tag_name": "v1.0.1.6", "prerelease": false, "draft": false },
      { "tag_name": "v9.0.0.0", "prerelease": false, "draft": true }
    ]
    """);

    var stable = UpdateService.SelectLatestRelease(releases, includePrereleases: false);
    Assert.Equal(new Version(1, 0, 1, 6), stable.Version, "stable version");

    var withPrerelease = UpdateService.SelectLatestRelease(releases, includePrereleases: true);
    Assert.Equal(new Version(1, 1, 0, 0), withPrerelease.Version, "prerelease version");
}

static void UpdateAssetMatchingRequiresStrictPackageNames()
{
    var release = JObject.Parse("""
    {
      "assets": [
        { "name": "Fenceless-v1.1.0.0-win-x64.zip", "browser_download_url": "https://example.test/app.zip" },
        { "name": "Fenceless-v1.1.0.0-win-x64.zip.sha256", "browser_download_url": "https://example.test/app.zip.sha256" }
      ]
    }
    """);

    Assert.True(UpdateService.TryFindStrictAssets(release, new Version(1, 1, 0, 0), out var zip, out var sha, out var error), error);
    Assert.Equal("Fenceless-v1.1.0.0-win-x64.zip", zip.Name, "zip asset");
    Assert.Equal("Fenceless-v1.1.0.0-win-x64.zip.sha256", sha.Name, "sha asset");

    var looseRelease = JObject.Parse("""
    {
      "assets": [
        { "name": "Fenceless.zip", "browser_download_url": "https://example.test/app.zip" }
      ]
    }
    """);

    Assert.False(UpdateService.TryFindStrictAssets(looseRelease, new Version(1, 1, 0, 0), out _, out _, out _),
        "loose asset names should not match");
}

static void UpdateSha256ValidationAcceptsStandardChecksumFiles()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "FencelessHash_" + Guid.NewGuid().ToString("N") + ".txt");
    File.WriteAllText(tempPath, "hello");

    try
    {
        var expected = UpdateService.ComputeSha256(tempPath);
        var parsed = UpdateService.ExtractSha256($"{expected}  Fenceless-v1.1.0.0-win-x64.zip");
        Assert.Equal(expected, parsed, "parsed hash");
        Assert.True(UpdateService.VerifySha256(tempPath, expected), "hash should verify");
    }
    finally
    {
        File.Delete(tempPath);
    }
}

static void UpdatePromptIsSuppressedForSkippedVersion()
{
    Assert.False(UpdateService.ShouldPromptForVersion(new Version(1, 1, 0, 0), "1.1.0.0"), "skipped version should suppress");
    Assert.True(UpdateService.ShouldPromptForVersion(new Version(1, 1, 0, 1), "1.1.0.0"), "different version should prompt");
}

static void UpdateStagingValidationRequiresRootAppFiles()
{
    var root = Path.Combine(Path.GetTempPath(), "FencelessStage_" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);

    try
    {
        File.WriteAllText(Path.Combine(root, "Fenceless.exe"), "");
        File.WriteAllText(Path.Combine(root, "Fenceless.dll"), "");
        File.WriteAllText(Path.Combine(root, "Fenceless.runtimeconfig.json"), "");
        UpdateService.ValidateStagedUpdate(root);

        File.Delete(Path.Combine(root, "Fenceless.dll"));
        Assert.Throws<InvalidDataException>(() => UpdateService.ValidateStagedUpdate(root));
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

    public static void Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"expected {typeof(TException).Name}, got {ex.GetType().Name}");
        }

        throw new InvalidOperationException($"expected {typeof(TException).Name}");
    }
}
