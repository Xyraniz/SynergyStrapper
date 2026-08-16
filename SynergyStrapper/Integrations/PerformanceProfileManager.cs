namespace SynergyStrapper.Integrations;

public static class PerformanceProfileManager
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> Profiles =
        new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Quality"] = new Dictionary<string, object?>
            {
                ["FIntDebugForceMSAASamples"] = 4,
                ["DFFlagTextureQualityOverrideEnabled"] = true,
                ["DFIntTextureQualityOverride"] = 3
            },
            ["Balanced"] = new Dictionary<string, object?>
            {
                ["FIntDebugForceMSAASamples"] = 2,
                ["DFFlagTextureQualityOverrideEnabled"] = true,
                ["DFIntTextureQualityOverride"] = 2
            },
            ["LowPower"] = new Dictionary<string, object?>
            {
                ["FIntDebugForceMSAASamples"] = 1,
                ["DFFlagTextureQualityOverrideEnabled"] = true,
                ["DFIntTextureQualityOverride"] = 0
            },
            ["LowLatency"] = new Dictionary<string, object?>
            {
                ["FIntDebugForceMSAASamples"] = 1,
                ["DFFlagTextureQualityOverrideEnabled"] = true,
                ["DFIntTextureQualityOverride"] = 1
            },
            ["Compatibility"] = new Dictionary<string, object?>
            {
                ["FIntDebugForceMSAASamples"] = null,
                ["DFFlagTextureQualityOverrideEnabled"] = null,
                ["DFIntTextureQualityOverride"] = null,
                ["DFIntTaskSchedulerTargetFps"] = null
            }
        };

    public static bool Apply(string profile, int fpsLimit)
    {
        if (!Profiles.TryGetValue(profile, out IReadOnlyDictionary<string, object?>? values))
            return false;

        string backupName = $"performance-before-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        App.FastFlags.SaveBackup(backupName);
        foreach (var pair in values)
            App.FastFlags.SetValue(pair.Key, pair.Value);
        SetFpsLimit(fpsLimit);
        App.Settings.Prop.Features.PerformanceProfile = profile;
        App.Settings.Prop.Features.FrameRateLimit = NormalizeFps(fpsLimit);
        App.FastFlags.Save();
        App.Settings.Save();
        return true;
    }

    public static bool RollbackLast()
    {
        string? backup = App.FastFlags.GetBackupNames()
            .Where(x => x.StartsWith("performance-before-", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (backup is null || !App.FastFlags.LoadBackup(backup, true))
            return false;
        App.FastFlags.Save();
        App.Settings.Prop.Features.PerformanceProfile = "Custom";
        App.Settings.Prop.Features.FrameRateLimit = 0;
        App.Settings.Save();
        return true;
    }

    public static void SetFpsLimit(int fpsLimit)
    {
        int normalized = NormalizeFps(fpsLimit);
        App.FastFlags.SetValue("DFIntTaskSchedulerTargetFps", normalized == 0 ? null : normalized);
    }

    private static int NormalizeFps(int fpsLimit)
    {
        if (fpsLimit <= 0)
            return 0;
        int[] supported = new[] { 30, 60, 120, 144, 240, 360 };
        return supported.OrderBy(x => Math.Abs(x - fpsLimit)).First();
    }
}
