using SynergyStrapper.Models.SettingTasks.Base;

namespace SynergyStrapper.Models.SettingTasks
{
    public class ModPresetTask : BoolBaseTask
    {
        private readonly Dictionary<string, ModPresetFileData> _fileDataMap = new();

        public ModPresetTask(string name, string path, string resource) : this(name, new() { { path, resource } }) { }

        public ModPresetTask(string name, Dictionary<string, string> pathMap) : base("ModPreset", name)
        {
            foreach (var pair in pathMap)
                _fileDataMap[pair.Key] = new ModPresetFileData(pair.Key, pair.Value);

            OriginalState = _fileDataMap.Count > 0 && _fileDataMap.Values.All(x => x.HashMatches());
        }

        public override void Execute()
        {
            bool allFilesMatch = _fileDataMap.Count > 0 && _fileDataMap.Values.All(x => x.HashMatches());
            bool anyFileMatches = _fileDataMap.Values.Any(x => x.HashMatches());

            // A partially applied preset must still be repaired when enabled,
            // and all known matching files must be removed when disabled.
            if ((NewState && allFilesMatch) || (!NewState && !anyFileMatches))
            {
                OriginalState = NewState;
                return;
            }

            foreach (ModPresetFileData data in _fileDataMap.Values)
            {
                bool hashMatches = data.HashMatches();

                if (NewState && !hashMatches)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(data.FullFilePath)!);

                    using Stream resourceStream = data.ResourceStream;
                    using var memoryStream = new MemoryStream();
                    resourceStream.CopyTo(memoryStream);

                    Filesystem.AssertReadOnly(data.FullFilePath);
                    File.WriteAllBytes(data.FullFilePath, memoryStream.ToArray());
                }
                else if (!NewState && hashMatches)
                {
                    Filesystem.AssertReadOnly(data.FullFilePath);
                    File.Delete(data.FullFilePath);
                }
            }

            OriginalState = NewState;
        }
    }
}
