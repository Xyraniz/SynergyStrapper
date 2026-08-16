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

        public IReadOnlyList<FastFlagHealthIssue> GetHealthIssues()
            => FastFlagHealthCheck.Validate(Prop);

        public IReadOnlyList<FastFlagChange> GetPendingChanges()
        {
            var changes = new List<FastFlagChange>();
            var names = OriginalProp.Keys
                .Concat(Prop.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

            foreach (string name in names)
            {
                bool existedBefore = OriginalProp.TryGetValue(name, out object? beforeValue);
                bool existsNow = Prop.TryGetValue(name, out object? afterValue);
                string? before = existedBefore ? FormatValue(beforeValue) : null;
                string? after = existsNow ? FormatValue(afterValue) : null;

                if (!existedBefore && existsNow)
                    changes.Add(new FastFlagChange(name, FastFlagChangeKind.Added, null, after));
                else if (existedBefore && !existsNow)
                    changes.Add(new FastFlagChange(name, FastFlagChangeKind.Removed, before, null));
                else if (!String.Equals(before, after, StringComparison.Ordinal))
                    changes.Add(new FastFlagChange(name, FastFlagChangeKind.Changed, before, after));
            }

            return changes;
        }

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

        // Values retain their native JSON type whenever one is supplied. A string remains
        // a string for backwards compatibility; JsonElement values are converted to CLR
        // primitives so exported ClientAppSettings.json remains interoperable.
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
                    if (String.Equals(FormatValue(currentValue), FormatValue(value), StringComparison.Ordinal))
                        return;

                    App.Logger.WriteLine(LOG_IDENT, $"Changing of '{key}' from '{FormatValue(currentValue)}' to '{FormatValue(value)}' is pending");
                }
                else
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Setting of '{key}' to '{value}' is pending");
                }

                Prop[key] = NormalizeValue(value)!;
            }
        }

        // this returns null if the fflag doesn't exist
        public string? GetValue(string key)
        {
            // check if we have an updated change for it pushed first
            if (Prop.TryGetValue(key, out object? value) && value is not null)
                return FormatValue(value);

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
                            Prop[pair.Key] = NormalizeValue(pair.Value)!;

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

        public object? GetTypedValue(string key)
            => Prop.TryGetValue(key, out object? value) ? value : null;

        public override void Save()
        {
            base.Save();
            OriginalProp = Prop.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }

        private static string FormatValue(object? value)
        {
            if (value is JsonElement element)
                return element.GetRawText();
            return value switch
            {
                null => String.Empty,
                bool boolean => boolean ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? String.Empty,
                _ => value.ToString() ?? String.Empty
            };
        }

        private static object? NormalizeValue(object? value)
        {
            if (value is not JsonElement element)
                return value;

            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt64(out long integer) => integer,
                JsonValueKind.Number when element.TryGetDouble(out double number) => number,
                JsonValueKind.String => element.GetString() ?? String.Empty,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        private static bool DictionariesEqual(IReadOnlyDictionary<string, object> left, IReadOnlyDictionary<string, object> right)
        {
            if (left.Count != right.Count)
                return false;

            foreach (var pair in left)
            {
                if (!right.TryGetValue(pair.Key, out object? otherValue)
                    || !String.Equals(FormatValue(pair.Value), FormatValue(otherValue), StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Load(bool alertFailure = true)
        {
            bool result = base.Load(alertFailure);

            foreach (string key in Prop.Keys.ToList())
                Prop[key] = NormalizeValue(Prop[key])!;

            // clone the dictionary
            OriginalProp = Prop.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            if (GetPreset("Rendering.ManualFullscreen") != "False")
                SetPreset("Rendering.ManualFullscreen", "False");

            return result;
        }
    }
}
