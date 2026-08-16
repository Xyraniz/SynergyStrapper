namespace SynergyStrapper.Integrations;

public sealed record CleanerResult(int ScannedFiles, int DeletedFiles, int FailedFiles, int SkippedFiles)
{
    public int CandidateFiles { get; init; }
}

public static class Cleaner
{
    private const string LogIdent = "Cleaner::CleanOldFiles";
    private const int DefaultMaxAgeDays = 30;
    private const int DefaultMaxFilesPerDirectory = 200;

    public static CleanerResult Preview() => CleanOldFiles(true);

    public static CleanerResult CleanOldFiles() => CleanOldFiles(false);

    public static CleanerResult CleanOldFiles(bool previewOnly)
    {
        var policy = App.Settings.Prop.Features.Cleaner;
        int maxAgeDays = Math.Clamp(policy.MaxAgeDays, 1, 3650);
        int maxFilesPerDirectory = Math.Clamp(policy.MaxFilesPerDirectory, 1, 5000);
        DateTime threshold = DateTime.UtcNow.AddDays(-maxAgeDays);
        int scannedFiles = 0;
        int deletedFiles = 0;
        int failedFiles = 0;
        int skippedFiles = 0;
        int candidates = 0;

        App.Logger.WriteLine(LogIdent, $"Starting {(previewOnly ? "preview" : "maintenance cleanup")} for files older than {maxAgeDays} days");

        var directories = new List<(string Name, string Path)>();
        if (policy.SynergyLogs) directories.Add(("SynergyStrapper logs", Paths.Logs));
        if (policy.SynergyDownloads) directories.Add(("SynergyStrapper downloads", Paths.Downloads));
        if (policy.RobloxLogs) directories.Add(("Roblox logs", Paths.RobloxLogs));
        if (policy.RobloxCache) directories.Add(("Roblox cache", Paths.RobloxCache));

        foreach (var directory in directories.Where(x => !string.IsNullOrWhiteSpace(x.Path)).GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase).Select(x => x.First()))
        {
            if (!Directory.Exists(directory.Path))
                continue;

            string rootPath;
            try
            {
                rootPath = Path.GetFullPath(directory.Path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                skippedFiles++;
                App.Logger.WriteException(LogIdent, ex);
                continue;
            }

            int deletedInDirectory = 0;
            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint
                };

                foreach (string filePath in Directory.EnumerateFiles(rootPath, "*", options))
                {
                    scannedFiles++;
                    if (deletedInDirectory >= maxFilesPerDirectory || !IsSafeFile(filePath, rootPath))
                    {
                        skippedFiles++;
                        continue;
                    }

                    try
                    {
                        string? activeLog = App.Logger.FileLocation;
                        if (File.GetLastWriteTimeUtc(filePath) >= threshold ||
                            (!string.IsNullOrWhiteSpace(activeLog) && string.Equals(Path.GetFullPath(filePath), Path.GetFullPath(activeLog), StringComparison.OrdinalIgnoreCase)))
                        {
                            skippedFiles++;
                            continue;
                        }

                        candidates++;
                        if (previewOnly)
                            continue;

                        File.Delete(filePath);
                        deletedFiles++;
                        deletedInDirectory++;
                    }
                    catch (Exception ex)
                    {
                        failedFiles++;
                        App.Logger.WriteException(LogIdent, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LogIdent, ex);
                failedFiles++;
            }
        }

        App.Logger.WriteLine(LogIdent, $"Cleanup finished: scanned={scannedFiles}, candidates={candidates}, deleted={deletedFiles}, failed={failedFiles}, skipped={skippedFiles}");
        return new CleanerResult(scannedFiles, deletedFiles, failedFiles, skippedFiles) { CandidateFiles = candidates };
    }

    private static bool IsSafeFile(string filePath, string rootPath)
    {
        if (!File.Exists(filePath))
            return false;

        try
        {
            FileAttributes attributes = File.GetAttributes(filePath);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
                return false;

            string fullPath = Path.GetFullPath(filePath);
            string rootWithSeparator = rootPath + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return false;

            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrWhiteSpace(windowsDirectory))
            {
                string windowsRoot = Path.GetFullPath(windowsDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (fullPath.StartsWith(windowsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException(LogIdent, ex);
            return false;
        }
    }
}
