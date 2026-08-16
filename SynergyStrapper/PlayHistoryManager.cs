namespace SynergyStrapper;

public sealed class PlayHistoryEntry
{
    public long PlaceId { get; set; }
    public long UniverseId { get; set; }
    public string GameName { get; set; } = "Unknown game";
    public string JobId { get; set; } = "";
    public DateTime LastPlayed { get; set; }
    public int PlayCount { get; set; } = 1;
    public string Region { get; set; } = "Unknown";
    public long TotalSeconds { get; set; }
    public long LastSessionSeconds { get; set; }

    [JsonIgnore]
    public string LastPlayedSummary
    {
        get
        {
            TimeSpan total = TimeSpan.FromSeconds(Math.Max(0, TotalSeconds));
            string totalText = total.TotalHours >= 1 ? $"{(int)total.TotalHours}h {total.Minutes}m" : $"{total.Minutes}m";
            return $"{LastPlayed:g} · {PlayCount} play{(PlayCount == 1 ? "" : "s")} · total {totalText}";
        }
    }

    [JsonIgnore]
    public string LaunchUrl => $"roblox://experiences/start?placeId={PlaceId}&gameInstanceId={JobId}";
}

public static class PlayHistoryManager
{
    private const int MaxEntries = 50;

    public static IReadOnlyList<PlayHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(Paths.History))
                return Array.Empty<PlayHistoryEntry>();

            return JsonSerializer.Deserialize<List<PlayHistoryEntry>>(File.ReadAllText(Paths.History)) ?? new List<PlayHistoryEntry>();
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("PlayHistoryManager::Load", ex);
            return Array.Empty<PlayHistoryEntry>();
        }
    }

    public static void Record(ActivityData activity)
    {
        if (activity.PlaceId <= 0)
            return;

        List<PlayHistoryEntry> entries = Load().ToList();
        long sessionSeconds = activity.TimeJoined == default
            ? 0
            : Math.Max(0, (long)((activity.TimeLeft ?? DateTime.Now) - activity.TimeJoined).TotalSeconds);
        string region = activity.MachineAddressValid && GlobalCache.ServerLocation.TryGetValue(activity.MachineAddress, out string? location)
            ? location ?? "Unknown"
            : "Unknown";

        PlayHistoryEntry? existing = entries.FirstOrDefault(x => x.PlaceId == activity.PlaceId && x.JobId == activity.JobId);
        if (existing is null)
        {
            entries.Insert(0, new PlayHistoryEntry
            {
                PlaceId = activity.PlaceId,
                UniverseId = activity.UniverseId,
                GameName = activity.UniverseDetails?.Data.Name ?? "Unknown game",
                JobId = activity.JobId,
                LastPlayed = activity.TimeJoined == default ? DateTime.Now : activity.TimeJoined,
                Region = region,
                LastSessionSeconds = sessionSeconds,
                TotalSeconds = sessionSeconds
            });
        }
        else
        {
            existing.GameName = activity.UniverseDetails?.Data.Name ?? existing.GameName;
            existing.UniverseId = activity.UniverseId;
            existing.LastPlayed = activity.TimeJoined == default ? DateTime.Now : activity.TimeJoined;
            existing.Region = region == "Unknown" ? existing.Region : region;
            existing.LastSessionSeconds = sessionSeconds;
            existing.TotalSeconds = Math.Max(0, existing.TotalSeconds + sessionSeconds);
            existing.PlayCount++;
            entries.Remove(existing);
            entries.Insert(0, existing);
        }

        Save(entries.Take(MaxEntries).ToList());
    }

    public static void Save(IReadOnlyList<PlayHistoryEntry> entries)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Paths.History)!);
            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Paths.History, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("PlayHistoryManager::Save", ex);
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(Paths.History))
                File.Delete(Paths.History);
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("PlayHistoryManager::Clear", ex);
        }
    }
}
