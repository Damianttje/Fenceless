using Fenceless.Model;
using Fenceless.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fenceless.Util
{
    public sealed class UpdateService
    {
        public const string ReleasesApiUrl = "https://codeberg.org/api/v1/repos/Wavestorm/Fenceless/releases";
        public const string ReleasesPageUrl = "https://codeberg.org/Wavestorm/Fenceless/releases";
        public const string UserAgent = "Fenceless-UpdateChecker";

        private static readonly Lazy<UpdateService> _instance = new Lazy<UpdateService>(() => new UpdateService());
        private static readonly SemaphoreSlim _updateCheckLock = new SemaphoreSlim(1, 1);
        public static UpdateService Instance => _instance.Value;

        private readonly Logger? logger;

        public UpdateService()
        {
            try
            {
                logger = Logger.Instance;
            }
            catch
            {
                logger = null;
            }
        }

        public async Task CheckAndPromptAsync(bool force = false, CancellationToken cancellationToken = default)
        {
            if (!await _updateCheckLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                logger?.Info("Update check skipped because another check is already running", "UpdateService");
                return;
            }

            try
            {
                var settings = AppSettings.Instance;
                if (!force && !settings.EnableAutoUpdates)
                {
                    logger?.Debug("Automatic update checks are disabled", "UpdateService");
                    return;
                }

                if (!force && !IsCheckDue(settings.LastUpdateCheckUtc, settings.UpdateCheckIntervalMinutes, DateTime.UtcNow))
                {
                    logger?.Debug("Skipping update check because the interval has not elapsed", "UpdateService");
                    return;
                }

                var result = await CheckForUpdatesAsync(force, cancellationToken).ConfigureAwait(false);
                if (!result.Available)
                {
                    if (force)
                    {
                        ShowInfo(result.Message ?? "Fenceless is up to date.");
                    }
                    return;
                }

                if (!ShouldPromptForVersion(result.Version, settings.SkippedUpdateVersion))
                {
                    logger?.Info($"Update {result.Version} is skipped by user preference", "UpdateService");
                    return;
                }

                var prompt = CustomMessageBox.Show(
                    $"Fenceless {result.Version} is available.\n\nCurrent version: {GetCurrentVersion()}\nRelease: {result.ReleasePageUrl}\n\nChoose Yes to download and install, No to remind later, or Cancel to skip this version.",
                    "Update Available",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                if (prompt == DialogResult.Cancel)
                {
                    settings.SkippedUpdateVersion = result.Version.ToString();
                    settings.SaveSettings();
                    logger?.Info($"User skipped update {result.Version}", "UpdateService");
                    return;
                }

                if (prompt != DialogResult.Yes)
                    return;

                var prepared = await DownloadAndStageAsync(result, cancellationToken).ConfigureAwait(false);
                settings.PendingUpdateVersion = prepared.Version.ToString();
                settings.PendingUpdatePath = prepared.StagedDirectory;
                settings.SaveSettings();

                var install = CustomMessageBox.Show(
                    $"Fenceless {prepared.Version} is ready to install.\n\nThe app will close, install the update, and restart. If Program Files requires admin access, Windows will ask for permission.",
                    "Install Update",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);

                if (install == DialogResult.OK)
                {
                    LaunchUpdaterAndExit(prepared);
                }
            }
            catch (Exception ex)
            {
                logger?.Error("Update check failed", "UpdateService", ex);
                if (force)
                {
                    ShowError($"Update check failed: {ex.Message}");
                }
            }
            finally
            {
                _updateCheckLock.Release();
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(bool force, CancellationToken cancellationToken = default)
        {
            var settings = AppSettings.Instance;
            logger?.Info("Checking for updates...", "UpdateService");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                var response = await httpClient.GetStringAsync(ReleasesApiUrl, cancellationToken).ConfigureAwait(false);
                settings.LastUpdateCheckUtc = DateTime.UtcNow;
                settings.SaveSettings();

                if (string.IsNullOrWhiteSpace(response))
                    return UpdateCheckResult.NotAvailable("The release API returned an empty response.");

                var releases = JArray.Parse(response);
                var selected = SelectLatestRelease(releases, settings.IncludePrereleaseUpdates);
                if (selected.Release == null)
                    return UpdateCheckResult.NotAvailable("No suitable release was found.");

                var current = GetCurrentVersion();
                logger?.Info($"Current version: {current}, Latest version: {selected.Version}", "UpdateService");
                if (selected.Version <= current)
                    return UpdateCheckResult.NotAvailable("Fenceless is up to date.");

                if (!TryFindStrictAssets(selected.Release, selected.Version, out var zipAsset, out var shaAsset, out var assetError))
                    return UpdateCheckResult.NotAvailable(assetError);

                return UpdateCheckResult.AvailableUpdate(
                    selected.Version,
                    selected.Release["html_url"]?.ToString() ?? ReleasesPageUrl,
                    zipAsset.Name,
                    zipAsset.DownloadUrl,
                    shaAsset.Name,
                    shaAsset.DownloadUrl);
            }
        }

        public async Task<PreparedUpdate> DownloadAndStageAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
        {
            if (!update.Available)
                throw new InvalidOperationException("Cannot download an update that is not available.");

            var updatesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless", "updates");
            var versionRoot = Path.Combine(updatesRoot, update.Version.ToString());
            var downloadRoot = Path.Combine(versionRoot, "download");
            var stagedRoot = Path.Combine(versionRoot, "staged");

            Directory.CreateDirectory(downloadRoot);
            ResetDirectory(stagedRoot);

            var zipPath = Path.Combine(downloadRoot, update.ZipAssetName);
            var shaPath = Path.Combine(downloadRoot, update.Sha256AssetName);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                await DownloadFileAsync(httpClient, update.ZipDownloadUrl, zipPath, cancellationToken).ConfigureAwait(false);
                await DownloadFileAsync(httpClient, update.Sha256DownloadUrl, shaPath, cancellationToken).ConfigureAwait(false);
            }

            var expectedHash = ExtractSha256(File.ReadAllText(shaPath));
            if (!VerifySha256(zipPath, expectedHash))
                throw new InvalidDataException("Downloaded update failed SHA256 verification.");

            ZipFile.ExtractToDirectory(zipPath, stagedRoot, true);
            ValidateStagedUpdate(stagedRoot);

            logger?.Info($"Update {update.Version} staged at {stagedRoot}", "UpdateService");
            return new PreparedUpdate(update.Version, stagedRoot);
        }

        public void LaunchUpdaterAndExit(PreparedUpdate preparedUpdate)
        {
            var appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var updaterExe = PrepareHelperExecutable(appDir);

            var backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fenceless", "app-backups");
            var restartExe = Path.Combine(appDir, "Fenceless.exe");
            var args = BuildHelperArgs(appDir, preparedUpdate.StagedDirectory, backupRoot, restartExe, Environment.ProcessId, false);

            logger?.Info($"Launching updater helper for version {preparedUpdate.Version}", "UpdateService");
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterExe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Application.Exit();
        }

        public static bool IsCheckDue(DateTime lastCheckUtc, int intervalMinutes, DateTime nowUtc)
        {
            if (lastCheckUtc <= DateTime.MinValue.AddDays(1))
                return true;

            return nowUtc - lastCheckUtc >= TimeSpan.FromMinutes(Math.Max(15, intervalMinutes));
        }

        public static (JObject? Release, Version Version) SelectLatestRelease(JArray releases, bool includePrereleases)
        {
            (JObject Release, Version Version)? best = null;

            foreach (var releaseToken in releases.OfType<JObject>())
            {
                if (releaseToken["draft"]?.Value<bool>() ?? false)
                    continue;

                if (!includePrereleases && (releaseToken["prerelease"]?.Value<bool>() ?? false))
                    continue;

                var tag = releaseToken["tag_name"]?.ToString();
                if (!TryParseReleaseVersion(tag, out var parsedVersion) || parsedVersion == null)
                    continue;

                if (best == null || parsedVersion > best.Value.Version)
                    best = (releaseToken, parsedVersion);
            }

            if (best.HasValue)
                return best.Value;

            return (null, new Version(0, 0));
        }

        public static bool TryParseReleaseVersion(string? tag, out Version? version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            var normalized = tag.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);

            var prereleaseIndex = normalized.IndexOf('-');
            if (prereleaseIndex >= 0)
                normalized = normalized.Substring(0, prereleaseIndex);

            return Version.TryParse(normalized, out version);
        }

        public static bool TryFindStrictAssets(JObject release, Version version, out UpdateAsset zipAsset, out UpdateAsset shaAsset, out string error)
        {
            var expectedZip = $"Fenceless-v{version}-win-x64.zip";
            var expectedSha = expectedZip + ".sha256";
            var assets = release["assets"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>();
            var parsedAssets = assets
                .Select(asset => new UpdateAsset(asset["name"]?.ToString() ?? "", GetAssetDownloadUrl(asset)))
                .Where(asset => !string.IsNullOrWhiteSpace(asset.Name) && !string.IsNullOrWhiteSpace(asset.DownloadUrl))
                .ToList();

            var zipMatch = parsedAssets.FirstOrDefault(asset => string.Equals(asset.Name, expectedZip, StringComparison.OrdinalIgnoreCase));
            var shaMatch = parsedAssets.FirstOrDefault(asset => string.Equals(asset.Name, expectedSha, StringComparison.OrdinalIgnoreCase));

            if (zipMatch == null || shaMatch == null)
            {
                zipAsset = null!;
                shaAsset = null!;
                error = $"Release {version} is missing required assets: {expectedZip} and {expectedSha}.";
                return false;
            }

            zipAsset = zipMatch;
            shaAsset = shaMatch;
            error = "";
            return true;
        }

        public static string ExtractSha256(string content)
        {
            var match = Regex.Match(content ?? "", @"\b[a-fA-F0-9]{64}\b");
            if (!match.Success)
                throw new InvalidDataException("SHA256 file does not contain a valid hash.");

            return match.Value.ToLowerInvariant();
        }

        public static bool VerifySha256(string filePath, string expectedHash)
        {
            var actualHash = ComputeSha256(filePath);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        public static string ComputeSha256(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        public static void ValidateStagedUpdate(string stagedDirectory)
        {
            if (!Directory.Exists(stagedDirectory))
                throw new DirectoryNotFoundException($"Staged update directory does not exist: {stagedDirectory}");

            var requiredFiles = new[]
            {
                "Fenceless.exe",
                "Fenceless.dll",
                "Fenceless.runtimeconfig.json"
            };

            foreach (var required in requiredFiles)
            {
                var path = Path.Combine(stagedDirectory, required);
                if (!File.Exists(path))
                    throw new InvalidDataException($"Staged update is missing {required} at the archive root.");
            }
        }

        public static bool ShouldPromptForVersion(Version version, string? skippedVersion)
        {
            return !string.Equals(version.ToString(), skippedVersion?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static Version GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
        }

        private static async Task DownloadFileAsync(HttpClient httpClient, string url, string destinationPath, CancellationToken cancellationToken)
        {
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
                using (var destination = File.Create(destinationPath))
                {
                    await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static string GetAssetDownloadUrl(JObject asset)
        {
            return asset["browser_download_url"]?.ToString()
                ?? asset["download_url"]?.ToString()
                ?? "";
        }

        private static void ResetDirectory(string directory)
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);

            Directory.CreateDirectory(directory);
        }

        private static string BuildHelperArgs(string source, string staged, string backup, string restart, int pid, bool elevated)
        {
            var args = new List<string>
            {
                "--source", Quote(source),
                "--staged", Quote(staged),
                "--backup", Quote(backup),
                "--restart", Quote(restart),
                "--pid", pid.ToString()
            };

            if (elevated)
                args.Add("--elevated");

            return string.Join(" ", args);
        }

        private static string PrepareHelperExecutable(string appDirectory)
        {
            var sourceExe = Path.Combine(appDirectory, "Fenceless.Updater.exe");
            if (!File.Exists(sourceExe))
                throw new FileNotFoundException("Updater helper was not found in the application directory.", sourceExe);

            var helperRunDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Fenceless",
                "updates",
                "helper",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(helperRunDirectory);

            foreach (var file in Directory.EnumerateFiles(appDirectory, "Fenceless.Updater.*", SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, Path.Combine(helperRunDirectory, Path.GetFileName(file)), overwrite: true);
            }

            return Path.Combine(helperRunDirectory, "Fenceless.Updater.exe");
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static void ShowInfo(string message)
        {
            CustomMessageBox.Show(message, "Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ShowError(string message)
        {
            CustomMessageBox.Show(message, "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public sealed class UpdateCheckResult
    {
        private UpdateCheckResult()
        {
        }

        public bool Available { get; private set; }
        public Version Version { get; private set; } = new Version(0, 0);
        public string ReleasePageUrl { get; private set; } = UpdateService.ReleasesPageUrl;
        public string ZipAssetName { get; private set; } = "";
        public string ZipDownloadUrl { get; private set; } = "";
        public string Sha256AssetName { get; private set; } = "";
        public string Sha256DownloadUrl { get; private set; } = "";
        public string? Message { get; private set; }

        public static UpdateCheckResult NotAvailable(string message)
        {
            return new UpdateCheckResult { Message = message };
        }

        public static UpdateCheckResult AvailableUpdate(
            Version version,
            string releasePageUrl,
            string zipAssetName,
            string zipDownloadUrl,
            string sha256AssetName,
            string sha256DownloadUrl)
        {
            return new UpdateCheckResult
            {
                Available = true,
                Version = version,
                ReleasePageUrl = releasePageUrl,
                ZipAssetName = zipAssetName,
                ZipDownloadUrl = zipDownloadUrl,
                Sha256AssetName = sha256AssetName,
                Sha256DownloadUrl = sha256DownloadUrl
            };
        }
    }

    public sealed class UpdateAsset
    {
        public UpdateAsset(string name, string downloadUrl)
        {
            Name = name;
            DownloadUrl = downloadUrl;
        }

        public string Name { get; }
        public string DownloadUrl { get; }
    }

    public sealed class PreparedUpdate
    {
        public PreparedUpdate(Version version, string stagedDirectory)
        {
            Version = version;
            StagedDirectory = stagedDirectory;
        }

        public Version Version { get; }
        public string StagedDirectory { get; }
    }
}
