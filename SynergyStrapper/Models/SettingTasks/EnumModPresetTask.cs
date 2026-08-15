using SynergyStrapper.Models.SettingTasks.Base;

namespace SynergyStrapper.Models.SettingTasks
{
    public class EnumModPresetTask<T> : EnumBaseTask<T> where T : struct, Enum
    {
        private readonly Dictionary<T, Dictionary<string, ModPresetFileData>> _fileDataMap = new();

        public EnumModPresetTask(string name, Dictionary<T, Dictionary<string, string>> map) : base("ModPreset", name)
        {
            foreach (var enumPair in map)
            {
                var dataMap = new Dictionary<string, ModPresetFileData>();

                foreach (var resourcePair in enumPair.Value)
                    dataMap[resourcePair.Key] = new ModPresetFileData(resourcePair.Key, resourcePair.Value);

                _fileDataMap[enumPair.Key] = dataMap;
            }

            // A preset is active only when every file belonging to it matches. A
            // partially applied preset must be treated as the default state so it
            // can be repaired safely instead of being reported as fully enabled.
            foreach (var enumPair in _fileDataMap)
            {
                if (enumPair.Value.Count > 0 && enumPair.Value.Values.All(x => x.HashMatches()))
                {
                    OriginalState = enumPair.Key;
                    break;
                }
            }
        }

        public override void Execute()
        {
            bool hasKnownPresetFiles = _fileDataMap.Values
                .SelectMany(x => x.Values)
                .Any(x => File.Exists(x.FullFilePath));

            if (NewState.Equals(OriginalState)
                && (!NewState.Equals(default(T)) || !hasKnownPresetFiles))
            {
                return;
            }

            if (!NewState.Equals(default(T)) && !_fileDataMap.ContainsKey(NewState))
                throw new InvalidOperationException($"Unknown preset value '{NewState}'.");

            // Remove only files that are known to belong to another preset. This
            // preserves user-created files when their content does not match one
            // of the embedded resources.
            foreach (var enumPair in _fileDataMap)
            {
                if (enumPair.Key.Equals(NewState))
                    continue;

                foreach (ModPresetFileData data in enumPair.Value.Values)
                {
                    if (!data.HashMatches())
                        continue;

                    Filesystem.AssertReadOnly(data.FullFilePath);
                    File.Delete(data.FullFilePath);
                }
            }

            if (!NewState.Equals(default(T)))
            {
                foreach (ModPresetFileData data in _fileDataMap[NewState].Values)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(data.FullFilePath)!);

                    using Stream resourceStream = data.ResourceStream;
                    using var memoryStream = new MemoryStream();
                    resourceStream.CopyTo(memoryStream);

                    Filesystem.AssertReadOnly(data.FullFilePath);
                    File.WriteAllBytes(data.FullFilePath, memoryStream.ToArray());
                }
            }

            OriginalState = NewState;
        }
    }
}
