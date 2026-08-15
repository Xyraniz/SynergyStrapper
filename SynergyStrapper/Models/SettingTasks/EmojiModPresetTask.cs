using System.Windows;

namespace SynergyStrapper.Models.SettingTasks
{
    public class EmojiModPresetTask : EnumBaseTask<EmojiType>
    {
        private string _filePath => Path.Combine(Paths.Modifications, @"content\fonts\TwemojiMozilla.ttf");

        private IEnumerable<KeyValuePair<EmojiType, string>>? QueryCurrentValue()
        {
            if (!File.Exists(_filePath))
                return null;

            using var fileStream = File.OpenRead(_filePath);
            string hash = MD5Hash.Stringify(App.MD5Provider.ComputeHash(fileStream));

            return EmojiTypeEx.Hashes.Where(x => x.Value == hash);
        }

        public EmojiModPresetTask() : base("ModPreset", "EmojiFont")
        {
            var query = QueryCurrentValue();

            if (query is not null)
                OriginalState = query.FirstOrDefault().Key;
        }

        public override void Execute()
        {
            const string LOG_IDENT = "EmojiModPresetTask::Execute";
            var query = QueryCurrentValue();

            if (NewState != EmojiType.Default && (query is null || query.FirstOrDefault().Key != NewState))
            {
                string temporaryPath = _filePath + ".tmp";

                try
                {
                    using HttpResponseMessage response = App.HttpClient.GetAsync(NewState.GetUrl()).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    byte[] contents = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                    Filesystem.AssertReadOnly(temporaryPath);
                    File.WriteAllBytes(temporaryPath, contents);
                    Filesystem.AssertReadOnly(_filePath);
                    File.Move(temporaryPath, _filePath, true);
                    OriginalState = NewState;
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException(LOG_IDENT, ex);

                    Frontend.ShowConnectivityDialog(
                        String.Format(Strings.Dialog_Connectivity_UnableToConnect, "GitHub"),
                        $"{Strings.Menu_Mods_Presets_EmojiType_Error}\n\n{Strings.Dialog_Connectivity_TryAgainLater}",
                        MessageBoxImage.Warning,
                        ex
                    );
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
            }
            else if (query is not null && query.Any())
            {
                Filesystem.AssertReadOnly(_filePath);
                File.Delete(_filePath);
                OriginalState = EmojiType.Default;
            }
        }
    }
}
