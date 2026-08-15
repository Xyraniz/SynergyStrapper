using System.IO;

namespace SynergyStrapper.Integrations
{
    public sealed record CleanerResult(int ScannedFiles, int DeletedFiles, int FailedFiles, int SkippedFiles);

    public static class Cleaner
    {
        private const string LogIdent = "Cleaner::CleanOldFiles";
        private const int MaxFileAgeDays = 30;
        private const int MaxFilesPerDirectory = 200;

        public static CleanerResult CleanOldFiles()
        {
            App.Logger.WriteLine(LogIdent, $"Starting maintenance cleanup for files older than {MaxFileAgeDays} days");

            DateTime threshold = DateTime.UtcNow.AddDays(-MaxFileAgeDays);
            int scannedFiles = 0;
            int deletedFiles = 0;
            int failedFiles = 0;
            int skippedFiles = 0;

            var directories = new[]
            {
                (Name: "SynergyStrapper logs", Path: Paths.Logs),
                (Name: "SynergyStrapper downloads", Path: Paths.Downloads),
                (Name: "Roblox logs", Path: Paths.RobloxLogs),
                (Name: "Roblox cache", Path: Paths.RobloxCache)
            }
            .Where(x => !String.IsNullOrWhiteSpace(x.Path))
            .GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First());

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory.Path))
                {
                    App.Logger.WriteLine(LogIdent, $"Skipping missing directory: {directory.Name}");
                    continue;
                }

                string rootPath;
                try
                {
                    rootPath = Path.GetFullPath(directory.Path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogIdent, $"Skipping invalid directory: {directory.Name}");
                    App.Logger.WriteException(LogIdent, ex);
                    continue;
                }

                int deletedInDirectory = 0;
                App.Logger.WriteLine(LogIdent, $"Scanning {directory.Name}: {rootPath}");

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

                        if (deletedInDirectory >= MaxFilesPerDirectory)
                        {
                            skippedFiles++;
                            continue;
                        }

                        if (!IsSafeFile(filePath, rootPath))
                        {
                            skippedFiles++;
                            continue;
                        }

                        try
                        {
                            if (File.GetLastWriteTimeUtc(filePath) >= threshold)
                            {
                                skippedFiles++;
                                continue;
                            }

                            File.Delete(filePath);
                            deletedFiles++;
                            deletedInDirectory++;
                        }
                        catch (Exception ex)
                        {
                            failedFiles++;
                            App.Logger.WriteLine(LogIdent, $"Unable to delete '{filePath}'");
                            App.Logger.WriteException(LogIdent, ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(LogIdent, $"Failed to scan {directory.Name}");
                    App.Logger.WriteException(LogIdent, ex);
                }
            }

            App.Logger.WriteLine(
                LogIdent,
                $"Cleanup finished: scanned={scannedFiles}, deleted={deletedFiles}, failed={failedFiles}, skipped={skippedFiles}");

            return new CleanerResult(scannedFiles, deletedFiles, failedFiles, skippedFiles);
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

                if (fullPath.Equals(App.Logger.FileLocation, StringComparison.OrdinalIgnoreCase))
                    return false;

                string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (!String.IsNullOrWhiteSpace(windowsDirectory))
                {
                    string windowsRoot = Path.GetFullPath(windowsDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (fullPath.StartsWith(windowsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogIdent, $"Unable to validate '{filePath}'");
                App.Logger.WriteException(LogIdent, ex);
                return false;
            }
        }
    }
}
