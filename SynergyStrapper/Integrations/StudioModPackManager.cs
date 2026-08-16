using System.Text.Json;

namespace SynergyStrapper.Integrations;

public sealed class StudioModPackManifest
{
    public string Name { get; set; } = string.Empty;
    public string TargetProduct { get; set; } = "Studio";
    public string TargetVersionGuid { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public List<string> Files { get; set; } = new();
}

public sealed record StudioModPackResult(int AppliedFiles, int DeletedFiles, int SkippedFiles, string BackupPath);

public static class StudioModPackManager
{
    public static string RootDirectory => Path.Combine(Paths.Modifications, "StudioModPacks");

    public static IReadOnlyList<string> GetPackNames()
    {
        if (!Directory.Exists(RootDirectory))
            return Array.Empty<string>();
        return Directory.GetDirectories(RootDirectory)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string GetPackDirectory(string name)
    {
        string safe = SanitizeName(name);
        return Path.Combine(RootDirectory, safe);
    }

    public static void EnsurePack(string name)
    {
        string directory = GetPackDirectory(name);
        Directory.CreateDirectory(Path.Combine(directory, "ModFiles"));
        string manifestPath = Path.Combine(directory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new StudioModPackManifest
            {
                Name = SanitizeName(name),
                UpdatedAtUtc = DateTime.UtcNow
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public static bool ContainsDeleteEntries(string name)
    {
        EnsurePack(name);
        string modsDirectory = Path.Combine(GetPackDirectory(name), "ModFiles");
        return Directory.EnumerateFiles(modsDirectory, "*", SearchOption.AllDirectories)
            .Any(x => Path.GetFileName(x).StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase));
    }

    public static StudioModPackResult Apply(string name)
    {
        EnsurePack(name);
        string packDirectory = GetPackDirectory(name);
        string modsDirectory = Path.Combine(packDirectory, "ModFiles");
        string targetDirectory = ResolveStudioDirectory();
        string backupDirectory = Path.Combine(Paths.SavedBackups, "StudioModPacks", $"{SanitizeName(name)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}");
        int applied = 0;
        int deleted = 0;
        int skipped = 0;

        Directory.CreateDirectory(backupDirectory);
        var manifest = new StudioModPackManifest
        {
            Name = SanitizeName(name),
            TargetVersionGuid = App.StudioState.Prop.VersionGuid,
            UpdatedAtUtc = DateTime.UtcNow
        };

        foreach (string file in Directory.EnumerateFiles(modsDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(modsDirectory, file);
            if (!IsSafeRelative(relative))
            {
                skipped++;
                continue;
            }

            bool delete = Path.GetFileName(relative).StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase);
            string targetRelative = delete ? Path.GetFileName(relative)[7..] : relative;
            if (!IsSafeRelative(targetRelative))
            {
                skipped++;
                continue;
            }

            string target = Path.Combine(targetDirectory, targetRelative);
            string backup = Path.Combine(backupDirectory, targetRelative);
            if (File.Exists(target))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(target, backup, true);
            }

            if (delete)
            {
                if (File.Exists(target))
                {
                    File.Delete(target);
                    deleted++;
                }
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, true);
                applied++;
            }

            manifest.Files.Add(targetRelative);
        }

        File.WriteAllText(Path.Combine(backupDirectory, "backup-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(packDirectory, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        App.Settings.Prop.Features.ActiveStudioModPack = SanitizeName(name);
        App.Settings.Save();
        return new StudioModPackResult(applied, deleted, skipped, backupDirectory);
    }

    public static bool Restore(string backupDirectory)
    {
        string manifestPath = Path.Combine(backupDirectory, "backup-manifest.json");
        if (!File.Exists(manifestPath))
            return false;
        try
        {
            StudioModPackManifest? manifest = JsonSerializer.Deserialize<StudioModPackManifest>(File.ReadAllText(manifestPath));
            if (manifest is null)
                return false;
            string targetDirectory = ResolveStudioDirectory();
            foreach (string relative in manifest.Files)
            {
                if (!IsSafeRelative(relative))
                    continue;
                string backup = Path.Combine(backupDirectory, relative);
                string target = Path.Combine(targetDirectory, relative);
                if (File.Exists(backup))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(backup, target, true);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("StudioModPackManager::Restore", ex);
            return false;
        }
    }

    private static string ResolveStudioDirectory()
    {
        string guid = App.StudioState.Prop.VersionGuid;
        if (string.IsNullOrWhiteSpace(guid))
            throw new InvalidOperationException("Roblox Studio is not installed.");
        return App.Settings.Prop.Features.UseStaticDirectory
            ? FeatureManager.ResolveDeploymentDirectory(guid)
            : Path.Combine(Paths.Versions, guid);
    }

    private static string SanitizeName(string name)
    {
        string trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed is "." or ".." || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Invalid mod pack name.", nameof(name));
        return trimmed[..Math.Min(trimmed.Length, 64)];
    }

    private static bool IsSafeRelative(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
            return false;
        string normalized = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        return normalized.Split(Path.DirectorySeparatorChar).All(x => x is not ".." and not ".");
    }
}
