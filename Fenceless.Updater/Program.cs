using System.Diagnostics;

var options = HelperOptions.Parse(args);
if (!options.IsValid)
{
    Console.Error.WriteLine("Missing required updater arguments.");
    return 2;
}

try
{
    WaitForFencelessToExit(options.ProcessId);

    if (!CanWriteToDirectory(options.SourceDirectory))
    {
        if (!options.Elevated)
        {
            RelaunchElevated(options);
            return 0;
        }

        Console.Error.WriteLine("Updater does not have permission to write to the application directory.");
        return 5;
    }

    Directory.CreateDirectory(options.BackupRoot);
    var backupDirectory = Path.Combine(options.BackupRoot, "app_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

    BackupDirectory(options.SourceDirectory, backupDirectory);

    try
    {
        CopyDirectory(options.StagedDirectory, options.SourceDirectory, overwrite: true);
    }
    catch
    {
        RestoreBackup(backupDirectory, options.SourceDirectory);
        throw;
    }

    if (File.Exists(options.RestartExecutable))
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = options.RestartExecutable,
            WorkingDirectory = Path.GetDirectoryName(options.RestartExecutable) ?? options.SourceDirectory,
            UseShellExecute = true
        });
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static void WaitForFencelessToExit(int processId)
{
    if (processId <= 0)
        return;

    try
    {
        using var process = Process.GetProcessById(processId);
        process.WaitForExit(30000);
    }
    catch
    {
        // If the process is already gone or inaccessible, continue with the update.
    }
}

static bool CanWriteToDirectory(string directory)
{
    try
    {
        Directory.CreateDirectory(directory);
        var probePath = Path.Combine(directory, ".fenceless_update_probe");
        File.WriteAllText(probePath, DateTime.UtcNow.ToString("O"));
        File.Delete(probePath);
        return true;
    }
    catch
    {
        return false;
    }
}

static void RelaunchElevated(HelperOptions options)
{
    var exePath = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Fenceless.Updater.exe");
    Process.Start(new ProcessStartInfo
    {
        FileName = exePath,
        Arguments = options.ToArguments(elevated: true),
        Verb = "runas",
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Hidden
    });
}

static void BackupDirectory(string sourceDirectory, string backupDirectory)
{
    if (!Directory.Exists(sourceDirectory))
        throw new DirectoryNotFoundException(sourceDirectory);

    Directory.CreateDirectory(backupDirectory);
    CopyDirectory(sourceDirectory, backupDirectory, overwrite: false);
}

static void RestoreBackup(string backupDirectory, string sourceDirectory)
{
    if (!Directory.Exists(backupDirectory))
        return;

    CopyDirectory(backupDirectory, sourceDirectory, overwrite: true);
}

static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite)
{
    Directory.CreateDirectory(destinationDirectory);

    foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(sourceDirectory, directory);
        Directory.CreateDirectory(Path.Combine(destinationDirectory, relative));
    }

    foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(sourceDirectory, file);
        var destination = Path.Combine(destinationDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(file, destination, overwrite);
    }
}

sealed class HelperOptions
{
    public string SourceDirectory { get; private set; } = "";
    public string StagedDirectory { get; private set; } = "";
    public string BackupRoot { get; private set; } = "";
    public string RestartExecutable { get; private set; } = "";
    public int ProcessId { get; private set; }
    public bool Elevated { get; private set; }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(SourceDirectory) &&
        !string.IsNullOrWhiteSpace(StagedDirectory) &&
        !string.IsNullOrWhiteSpace(BackupRoot) &&
        !string.IsNullOrWhiteSpace(RestartExecutable) &&
        Directory.Exists(StagedDirectory);

    public static HelperOptions Parse(string[] args)
    {
        var options = new HelperOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            string NextValue() => i + 1 < args.Length ? args[++i] : "";

            switch (key)
            {
                case "--source":
                    options.SourceDirectory = NextValue();
                    break;
                case "--staged":
                    options.StagedDirectory = NextValue();
                    break;
                case "--backup":
                    options.BackupRoot = NextValue();
                    break;
                case "--restart":
                    options.RestartExecutable = NextValue();
                    break;
                case "--pid":
                    int.TryParse(NextValue(), out var pid);
                    options.ProcessId = pid;
                    break;
                case "--elevated":
                    options.Elevated = true;
                    break;
            }
        }

        return options;
    }

    public string ToArguments(bool elevated)
    {
        var parts = new List<string>
        {
            "--source", Quote(SourceDirectory),
            "--staged", Quote(StagedDirectory),
            "--backup", Quote(BackupRoot),
            "--restart", Quote(RestartExecutable),
            "--pid", ProcessId.ToString()
        };

        if (elevated)
            parts.Add("--elevated");

        return string.Join(" ", parts);
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
