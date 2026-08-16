using System.IO.Compression;
using System.Text.Json;
using SynergyStrapper.Models.Persistable;

namespace SynergyStrapper.Integrations;

public static class FeatureManager
{
    public static readonly IReadOnlyDictionary<string, string> CursorSlotFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arrow"] = "ArrowCursor.png",
            ["ArrowFar"] = "ArrowFarCursor.png",
            ["IBeam"] = "IBeamCursor.png",
            ["Shiftlock"] = "MouseLockedCursor.png"
        };

    private static readonly byte[] PngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly string[] AllowedAudioExtensions = new[] { ".ogg", ".wav", ".mp3" };
    private const long MaxCursorBytes = 4 * 1024 * 1024;
    private const long MaxAudioBytes = 16 * 1024 * 1024;

    public static FeatureSettings Settings => App.Settings.Prop.Features;

    public static string CursorDirectory => Path.Combine(Paths.Modifications, "content", "textures");
    public static string ManagedDeathSoundPath => Path.Combine(Paths.Modifications, "content", "sounds", "oof.ogg");
    public static string ManagedPlayerIconPath => Path.Combine(Paths.Modifications, "Icons", "Player.ico");
    public static string ManagedStudioIconPath => Path.Combine(Paths.Modifications, "Icons", "Studio.ico");

    public static string GetCursorPath(string slot)
    {
        if (!CursorSlotFiles.TryGetValue(slot, out string? fileName))
            throw new ArgumentException($"Unknown cursor slot '{slot}'.", nameof(slot));

        return Path.Combine(CursorDirectory, fileName);
    }

    public static bool TrySetCursor(string slot, string sourcePath, out string error)
    {
        error = string.Empty;
        if (!CursorSlotFiles.ContainsKey(slot))
        {
            error = "Unknown cursor slot.";
            return false;
        }

        if (!ValidatePng(sourcePath, out error))
            return false;

        try
        {
            Directory.CreateDirectory(CursorDirectory);
            string destination = GetCursorPath(slot);
            BackupFile(destination);
            File.Copy(sourcePath, destination, true);
            Settings.CursorSlots[slot] = destination;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            App.Logger.WriteException("FeatureManager::TrySetCursor", ex);
            return false;
        }
    }

    public static bool RemoveCursor(string slot)
    {
        if (!CursorSlotFiles.ContainsKey(slot))
            return false;

        string path = GetCursorPath(slot);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            Settings.CursorSlots.Remove(slot);
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FeatureManager::RemoveCursor", ex);
            return false;
        }
    }

    public static bool TrySetDeathSound(string sourcePath, out string error)
    {
        error = string.Empty;
        try
        {
            FileInfo info = new(sourcePath);
            if (!info.Exists || info.Length == 0 || info.Length > MaxAudioBytes)
            {
                error = "The audio file must be non-empty and no larger than 16 MB.";
                return false;
            }

            if (!AllowedAudioExtensions.Contains(info.Extension, StringComparer.OrdinalIgnoreCase))
            {
                error = "Supported audio formats are OGG, WAV, and MP3.";
                return false;
            }

            string destination = ManagedDeathSoundPath;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            BackupFile(destination);
            File.Copy(sourcePath, destination, true);
            Settings.CustomDeathSoundPath = destination;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            App.Logger.WriteException("FeatureManager::TrySetDeathSound", ex);
            return false;
        }
    }

    public static bool RemoveDeathSound()
    {
        try
        {
            if (File.Exists(ManagedDeathSoundPath))
                File.Delete(ManagedDeathSoundPath);
            Settings.CustomDeathSoundPath = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FeatureManager::RemoveDeathSound", ex);
            return false;
        }
    }

    public static bool TryImportCursorSet(string archivePath, out string message)
    {
        message = string.Empty;
        string staging = Path.Combine(Paths.Temp, "CursorSet", Guid.NewGuid().ToString("N"));
        string backup = Path.Combine(Paths.Temp, "CursorSetBackup", Guid.NewGuid().ToString("N"));

        try
        {
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            var entries = archive.Entries.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
            if (entries.Count > CursorSlotFiles.Count + 1)
                throw new InvalidDataException("The cursor set contains too many files.");

            Directory.CreateDirectory(staging);
            foreach (ZipArchiveEntry entry in entries)
            {
                string fileName = Path.GetFileName(entry.Name);
                if (fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!CursorSlotFiles.Values.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException($"'{fileName}' is not an allowed cursor file.");

                string target = Path.Combine(staging, fileName);
                entry.ExtractToFile(target, true);
                if (!ValidatePng(target, out string pngError))
                    throw new InvalidDataException($"{fileName}: {pngError}");
            }

            Directory.CreateDirectory(backup);
            Directory.CreateDirectory(CursorDirectory);
            foreach (var pair in CursorSlotFiles)
            {
                string source = Path.Combine(staging, pair.Value);
                string destination = GetCursorPath(pair.Key);
                if (!File.Exists(source))
                    continue;
                if (File.Exists(destination))
                    File.Copy(destination, Path.Combine(backup, pair.Value), true);
                File.Copy(source, destination, true);
                Settings.CursorSlots[pair.Key] = destination;
            }

            message = "Cursor set imported successfully.";
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FeatureManager::TryImportCursorSet", ex);
            RestoreDirectory(backup, CursorDirectory);
            message = ex.Message;
            return false;
        }
        finally
        {
            TryDeleteDirectory(staging);
            TryDeleteDirectory(backup);
        }
    }

    public static bool TryExportCursorSet(string archivePath, out string message)
    {
        message = string.Empty;
        string tempArchive = archivePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivePath))!);
            if (File.Exists(tempArchive))
                File.Delete(tempArchive);

            using (ZipArchive archive = ZipFile.Open(tempArchive, ZipArchiveMode.Create))
            {
                foreach (var pair in CursorSlotFiles)
                {
                    string path = GetCursorPath(pair.Key);
                    if (File.Exists(path))
                        archive.CreateEntryFromFile(path, pair.Value, CompressionLevel.Optimal);
                }

                ZipArchiveEntry manifest = archive.CreateEntry("manifest.json");
                using StreamWriter writer = new(manifest.Open());
                writer.Write(JsonSerializer.Serialize(new
                {
                    format = 1,
                    name = "Synergy cursor set",
                    exportedAtUtc = DateTime.UtcNow,
                    files = CursorSlotFiles.Values
                }, new JsonSerializerOptions { WriteIndented = true }));
            }

            File.Move(tempArchive, archivePath, true);
            message = "Cursor set exported successfully.";
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FeatureManager::TryExportCursorSet", ex);
            try { if (File.Exists(tempArchive)) File.Delete(tempArchive); } catch { }
            message = ex.Message;
            return false;
        }
    }

    public static bool TrySetIcon(string product, string sourcePath, out string error)
    {
        error = string.Empty;
        if (!product.Equals("Player", StringComparison.OrdinalIgnoreCase) && !product.Equals("Studio", StringComparison.OrdinalIgnoreCase))
        {
            error = "Unknown product.";
            return false;
        }

        try
        {
            string extension = Path.GetExtension(sourcePath);
            if (!extension.Equals(".ico", StringComparison.OrdinalIgnoreCase) || new FileInfo(sourcePath).Length > MaxCursorBytes)
            {
                error = "Icons must be ICO files no larger than 4 MB.";
                return false;
            }

            string destination = product.Equals("Player", StringComparison.OrdinalIgnoreCase) ? ManagedPlayerIconPath : ManagedStudioIconPath;
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            BackupFile(destination);
            File.Copy(sourcePath, destination, true);
            if (product.Equals("Player", StringComparison.OrdinalIgnoreCase))
                Settings.PlayerIconPath = destination;
            else
                Settings.StudioIconPath = destination;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            App.Logger.WriteException("FeatureManager::TrySetIcon", ex);
            return false;
        }
    }

    public static bool TryApplyAppStorage(out string message)
    {
        message = string.Empty;
        string? appStorage = FindAppStoragePath();
        if (appStorage is null)
        {
            message = "Roblox appStorage.json was not found.";
            return false;
        }

        string backup = appStorage + ".synergy.bak";
        try
        {
            BackupFile(appStorage, backup);
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(appStorage));
            Dictionary<string, object?> values = document.RootElement.EnumerateObject()
                .ToDictionary(x => x.Name, x => (object?)x.Value.Clone(), StringComparer.OrdinalIgnoreCase);

            values["theme"] = Settings.AppStorage.DarkTheme ? "dark" : "light";
            values["minimizeToTray"] = Settings.AppStorage.MinimizeToTray;
            values["launchAtStartup"] = Settings.AppStorage.LaunchAtStartup;
            values["hideVersionDetails"] = Settings.AppStorage.HideVersionDetails;
            values["hideProductionDetails"] = Settings.AppStorage.HideProductionDetails;
            File.WriteAllText(appStorage, JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true }));
            message = "Roblox appStorage.json updated with a backup.";
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FeatureManager::TryApplyAppStorage", ex);
            try { if (File.Exists(backup)) File.Copy(backup, appStorage, true); } catch { }
            message = ex.Message;
            return false;
        }
    }

    public static string? FindAppStoragePath()
    {
        string[] candidates = new[]
        {
            Path.Combine(Paths.LocalAppData, "Roblox", "appStorage.json"),
            Path.Combine(Paths.LocalAppData, "Roblox", "ClientSettings", "appStorage.json")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public static string GetDeploymentDirectory(string versionGuid)
    {
        string configured = Settings.StaticDirectory;
        return Settings.UseStaticDirectory && IsSafeDirectory(configured)
            ? Path.GetFullPath(configured)
            : Path.Combine(Paths.Versions, versionGuid);
    }

    public static string ResolveDeploymentDirectory(string versionGuid)
    {
        string directory = GetDeploymentDirectory(versionGuid);
        Directory.CreateDirectory(directory);
        if (Settings.UseStaticDirectory && IsSafeDirectory(Settings.StaticDirectory))
            File.WriteAllText(Path.Combine(directory, ".synergy-static-manifest"), versionGuid);
        return directory;
    }

    public static bool IsSafeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        try
        {
            string full = Path.GetFullPath(path);
            string localAppData = Path.GetFullPath(Paths.LocalAppData);
            return full.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase) &&
                   !full.Equals(localAppData, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidatePng(string path, out string error)
    {
        error = string.Empty;
        try
        {
            FileInfo info = new(path);
            if (!info.Exists || info.Length < PngSignature.Length || info.Length > MaxCursorBytes)
            {
                error = "PNG files must be non-empty and no larger than 4 MB.";
                return false;
            }

            using FileStream stream = info.OpenRead();
            Span<byte> signature = stackalloc byte[PngSignature.Length];
            if (stream.Read(signature) != signature.Length || !signature.SequenceEqual(PngSignature))
            {
                error = "The selected file is not a valid PNG.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void BackupFile(string path, string? destination = null)
    {
        if (!File.Exists(path))
            return;
        destination ??= path + ".synergy.bak";
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(path, destination, true);
    }

    private static void RestoreDirectory(string backup, string destination)
    {
        if (!Directory.Exists(backup))
            return;
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.EnumerateFiles(backup))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }
}
