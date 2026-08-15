namespace SynergyStrapper.Models.SettingTasks
{
    public class FontModPresetTask : StringBaseTask
    {
        public string? GetFileHash()
        {
            if (!File.Exists(Paths.CustomFont))
                return null;

            using var fileStream = File.OpenRead(Paths.CustomFont);
            return MD5Hash.Stringify(App.MD5Provider.ComputeHash(fileStream));
        }

        public FontModPresetTask() : base("ModPreset", "TextFont")
        {
            if (File.Exists(Paths.CustomFont))
                OriginalState = Paths.CustomFont;
        }

        public override void Execute()
        {
            if (!String.IsNullOrEmpty(NewState))
            {
                if (String.Equals(NewState, Paths.CustomFont, StringComparison.InvariantCultureIgnoreCase))
                {
                    OriginalState = File.Exists(Paths.CustomFont) ? NewState : OriginalState;
                    return;
                }

                if (!File.Exists(NewState))
                {
                    App.Logger.WriteLine("FontModPresetTask::Execute", $"Font file does not exist: {NewState}");
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Paths.CustomFont)!);
                string temporaryPath = Paths.CustomFont + ".tmp";

                try
                {
                    Filesystem.AssertReadOnly(temporaryPath);
                    File.Copy(NewState, temporaryPath, true);
                    Filesystem.AssertReadOnly(Paths.CustomFont);
                    File.Move(temporaryPath, Paths.CustomFont, true);
                }
                finally
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }

                OriginalState = File.Exists(Paths.CustomFont) ? NewState : OriginalState;
            }
            else if (File.Exists(Paths.CustomFont))
            {
                Filesystem.AssertReadOnly(Paths.CustomFont);
                File.Delete(Paths.CustomFont);
                OriginalState = "";
            }
            else
            {
                OriginalState = "";
            }
        }
    }
}
