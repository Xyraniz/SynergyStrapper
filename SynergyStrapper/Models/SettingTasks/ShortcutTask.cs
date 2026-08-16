namespace SynergyStrapper.Models.SettingTasks
{
    public class ShortcutTask : BoolBaseTask
    {
        private string _shortcutPath;

        private string _exeFlags;
        private string? _iconPath;

        public ShortcutTask(string name, string lnkFolder, string lnkName, string exeFlags = "", string? iconPath = null) : base("Shortcut", name)
        {
            _shortcutPath = Path.Combine(lnkFolder, lnkName);
            _exeFlags = exeFlags;
            _iconPath = iconPath;

            OriginalState = File.Exists(_shortcutPath);
        }

        public override void Execute()
        {
            if (NewState)
                Shortcut.Create(Paths.Application, _exeFlags, _shortcutPath, _iconPath);
            else if (File.Exists(_shortcutPath))
                File.Delete(_shortcutPath);

            OriginalState = NewState;
        }
    }
}