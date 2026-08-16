using System.Security.Cryptography;
using System.Text.Json;

namespace SynergyStrapper.Integrations;

public enum FastFlagAvailability
{
    Available,
    Unavailable,
    Unknown,
    Unverified
}

public sealed record FastFlagAvailabilityEntry(string Name, FastFlagAvailability Status, string SourceRevision, DateTime CheckedAtUtc);

public sealed class FastFlagAllowlistDocument
{
    public string Revision { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public DateTime RetrievedAtUtc { get; set; }
    public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class FastFlagAvailabilityService
{
    private const string DefaultAllowlistUrl = "https://raw.githubusercontent.com/MaximumADHD/Roblox-Fast-Flags/main/flags.json";
    private static FastFlagAllowlistDocument? _document;

    public static FastFlagAllowlistDocument? Document => _document;

    public static IReadOnlyList<FastFlagAvailabilityEntry> CheckCurrentFlags()
    {
        var flags = App.FastFlags.Prop.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        if (!App.Settings.Prop.Features.EnableFastFlagAvailabilityCheck)
            return flags.Select(x => new FastFlagAvailabilityEntry(x, FastFlagAvailability.Unverified, "disabled", DateTime.UtcNow)).ToList();

        if (_document is null)
            LoadCached();

        if (_document is null || _document.Flags.Count == 0)
            return flags.Select(x => new FastFlagAvailabilityEntry(x, FastFlagAvailability.Unknown, "none", DateTime.UtcNow)).ToList();

        return flags.Select(x => new FastFlagAvailabilityEntry(
            x,
            _document.Flags.Contains(x) ? FastFlagAvailability.Available : FastFlagAvailability.Unavailable,
            _document.Revision,
            DateTime.UtcNow)).ToList();
    }

    public static async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        string url = string.IsNullOrWhiteSpace(App.Settings.Prop.Features.FastFlagAllowlistUrl)
            ? DefaultAllowlistUrl
            : App.Settings.Prop.Features.FastFlagAllowlistUrl.Trim();
        try
        {
            using HttpResponseMessage response = await App.HttpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            HashSet<string> flags = ParseAllowlist(body);
            if (flags.Count == 0)
                throw new InvalidDataException("The allowlist did not contain any valid flag names.");

            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
            _document = new FastFlagAllowlistDocument
            {
                Revision = App.Settings.Prop.Features.FastFlagAllowlistRevision,
                Sha256 = hash,
                RetrievedAtUtc = DateTime.UtcNow,
                Flags = flags
            };
            if (string.IsNullOrWhiteSpace(_document.Revision))
                _document.Revision = _document.RetrievedAtUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            Directory.CreateDirectory(Paths.SavedBackups);
            File.WriteAllText(Path.Combine(Paths.SavedBackups, "fastflag-allowlist.json"), JsonSerializer.Serialize(_document, new JsonSerializerOptions { WriteIndented = true }));
            App.Settings.Prop.Features.FastFlagAllowlistRevision = _document.Revision;
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FastFlagAvailabilityService::RefreshAsync", ex);
            LoadCached();
            return false;
        }
    }

    public static bool LoadCached()
    {
        try
        {
            string path = Path.Combine(Paths.SavedBackups, "fastflag-allowlist.json");
            if (!File.Exists(path))
                return false;
            _document = JsonSerializer.Deserialize<FastFlagAllowlistDocument>(File.ReadAllText(path));
            return _document?.Flags.Count > 0;
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("FastFlagAvailabilityService::LoadCached", ex);
            _document = null;
            return false;
        }
    }

    private static HashSet<string> ParseAllowlist(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ReadFlags(document.RootElement, result);
        return result;
    }

    private static void ReadFlags(JsonElement element, HashSet<string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (JsonElement child in element.EnumerateArray())
                    ReadFlags(child, result);
                break;
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (FastFlagHealthCheck.IsValidName(property.Name))
                        result.Add(property.Name);
                    ReadFlags(property.Value, result);
                }
                break;
            case JsonValueKind.String:
                string? value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value) && FastFlagHealthCheck.IsValidName(value))
                    result.Add(value);
                break;
        }
    }
}
