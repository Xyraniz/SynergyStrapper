using SynergyStrapper.Enums.FlagPresets;

namespace SynergyStrapper
{
    public class FastFlagManager : JsonManager<Dictionary<string, object>>
    {
        private Dictionary<string, object> OriginalProp = new();

        public override string ClassName => nameof(FastFlagManager);

        public override string LOG_IDENT_CLASS => ClassName;

        public override string FileName => "ClientAppSettings.json";

        public override string FileLocation => Path.Combine(Paths.Modifications, "ClientSettings", FileName);

        public bool Changed => !DictionariesEqual(OriginalProp, Prop);

        public static IReadOnlyDictionary<string, string> PresetFlags = new Dictionary<string, string>
        {
            { "Rendering.ManualFullscreen", "FFlagHandleAltEnterFullscreenManually" },
            { "Rendering.DisableScaling", "DFFlagDisableDPIScale" },
            { "Rendering.MSAA", "FIntDebugForceMSAASamples" },

            { "Rendering.TextureQuality.OverrideEnabled", "DFFlagTextureQualityOverrideEnabled" },
            { "Rendering.TextureQuality.Level", "DFIntTextureQualityOverride" },
        };

        // Conservative, reversible flags intended to reduce GPU/VRAM load.
        // Roblox may change or remove these flags; users can disable the profile at any time.
        public static IReadOnlyDictionary<string, string> PerformancePresetFlags => new Dictionary<string, string>
        {
            { "FIntDebugForceMSAASamples", "1" },
            { "DFFlagTextureQualityOverrideEnabled", "True" },
            { "DFIntTextureQualityOverride", "1" },
        };

        public static IReadOnlyDictionary<MSAAMode, string?> MSAAModes => new Dictionary<MSAAMode, string?>
        {
            { MSAAMode.Default, null },
            { MSAAMode.x1, "1" },
            { MSAAMode.x2, "2" },
            { MSAAMode.x4, "4" }
        };

        public static IReadOnlyDictionary<TextureQuality, string?> TextureQualityLevels => new Dictionary<TextureQuality, string?>
        {
            { TextureQuality.Default, null },
            { TextureQuality.Level0, "0" },
            { TextureQuality.Level1, "1" },
            { TextureQuality.Level2, "2" },
            { TextureQuality.Level3, "3" },
        };

        // all fflags are stored as strings
        // to delete a flag, set the value as null
        public void SetValue(string key, object? value)
        {
            const string LOG_IDENT = "FastFlagManager::SetValue";

            if (value is null)
            {
                if (Prop.ContainsKey(key))
                    App.Logger.WriteLine(LOG_IDENT, $"Deletion of '{key}' is pending");

                Prop.Remove(key);
            }
            else
            {
                if (Prop.TryGetValue(key, out object? currentValue))
                {
                    if (String.Equals(currentValue?.ToString(), value.ToString(), StringComparison.Ordinal))
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"Changing of '{key}' from '{currentValue}' to '{value}' is pending");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Setting of '{key}' to '{value}' is pending");
                }

                Prop[key] = value.ToString()!;
            }
        }

        // this returns null if the fflag doesn't exist
        public string? GetValue(string key)
        {
            // check if we have an updated change for it pushed first
            if (Prop.TryGetValue(key, out object? value) && value is not null)
                return value.ToString();

            return null;
        }

        public IReadOnlyList<string> GetBackupNames()
        {
            if (!Directory.Exists(Paths.SavedBackups))
                return Array.Empty<string>();

            return Directory.GetFiles(Paths.SavedBackups, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !String.IsNullOrEmpty(x))
                .Select(x => x!)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool SaveBackup(string name)
        {
            string? safeName = SanitizeBackupName(name);
            if (safeName is null)
                return false;

            try
            {
                Directory.CreateDirectory(Paths.SavedBackups);
                string path = Path.Combine(Paths.SavedBackups, $"{safeName}.json");
                string contents = JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, contents, Encoding.UTF8);
                App.Logger.WriteLine("FastFlagManager::SaveBackup", $"Saved backup '{safeName}'.");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("FastFlagManager::SaveBackup", ex);
                return false;
            }
        }

        public bool LoadBackup(string name, bool clearFlags)
        {
            string? safeName = SanitizeBackupName(name);
            if (safeName is null)
                return false;

            try
            {
                string path = Path.Combine(Paths.SavedBackups, $"{safeName}.json");
                if (!File.Exists(path))
                    return false;

                var loaded = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
                if (loaded is null)
                    return false;

                if (clearFlags)
                    Prop.Clear();

                foreach (var pair in loaded)
                {
                    if (pair.Value is not null)
                        Prop[pair.Key] = pair.Value.ToString()!;
                }

                App.Logger.WriteLine("FastFlagManager::LoadBackup", $"Loaded backup '{safeName}'.");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("FastFlagManager::LoadBackup", ex);
                return false;
            }
        }

        public bool DeleteBackup(string name)
        {
            string? safeName = SanitizeBackupName(name);
            if (safeName is null)
                return false;

            try
            {
                string path = Path.Combine(Paths.SavedBackups, $"{safeName}.json");
                if (!File.Exists(path))
                    return false;

                File.Delete(path);
                App.Logger.WriteLine("FastFlagManager::DeleteBackup", $"Deleted backup '{safeName}'.");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("FastFlagManager::DeleteBackup", ex);
                return false;
            }
        }

        private static string? SanitizeBackupName(string name)
        {
            string trimmed = name.Trim();
            if (String.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 64)
                return null;

            if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || trimmed is "." or "..")
                return null;

            return trimmed;
        }

        public void ApplyPerformancePreset(bool enabled)
        {
            foreach (var pair in PerformancePresetFlags)
                SetValue(pair.Key, enabled ? pair.Value : null);
        }

        public void SetPreset(string prefix, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
                SetValue(pair.Value, value);
        }

        public void SetPresetEnum(string prefix, string target, object? value)
        {
            foreach (var pair in PresetFlags.Where(x => x.Key.StartsWith(prefix)))
            {
                if (pair.Key.StartsWith($"{prefix}.{target}"))
                    SetValue(pair.Value, value);
                else
                    SetValue(pair.Value, null);
            }
        }

        public string? GetPreset(string name)
        {
            if (!PresetFlags.ContainsKey(name))
            {
                App.Logger.WriteLine("FastFlagManager::GetPreset", $"Could not find preset {name}");
                Debug.Assert(false, $"Could not find preset {name}");
                return null;
            }

            return GetValue(PresetFlags[name]);
        }

        public T GetPresetEnum<T>(IReadOnlyDictionary<T, string> mapping, string prefix, string value) where T : Enum
        {
            foreach (var pair in mapping)
            {
                if (pair.Value == "None")
                    continue;

                if (GetPreset($"{prefix}.{pair.Value}") == value)
                    return pair.Key;
            }

            return mapping.First().Key;
        }

        public override void Save()
        {
            // convert all flag values to strings before saving

            foreach (string key in Prop.Keys.ToList())
                Prop[key] = Prop[key]?.ToString() ?? String.Empty;

            base.Save();

            // clone the dictionary
            OriginalProp = new(Prop);
        }

        private static bool DictionariesEqual(IReadOnlyDictionary<string, object> left, IReadOnlyDictionary<string, object> right)
        {
            if (left.Count != right.Count)
                return false;

            foreach (var pair in left)
            {
                if (!right.TryGetValue(pair.Key, out object? otherValue)
                    || !String.Equals(pair.Value?.ToString(), otherValue?.ToString(), StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Load(bool alertFailure = true)
        {
            bool result = base.Load(alertFailure);

            // clone the dictionary
            OriginalProp = new(Prop);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");

            return result;
        }
    }
}
