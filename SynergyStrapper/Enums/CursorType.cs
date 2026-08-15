namespace SynergyStrapper.Enums
{
    public enum CursorType
    {
        [EnumSort(Order = 1)]
        [EnumName(FromTranslation = "Common.Default")]
        Default,

        [EnumSort(Order = 2)]
        From2006,

        [EnumSort(Order = 3)]
        From2013,

        [EnumSort(Order = 4)]
        [EnumName(FromTranslation = "Menu.Mods.Cursor.CompetitiveCircle")]
        CompetitiveCircle,

        [EnumSort(Order = 5)]
        [EnumName(FromTranslation = "Menu.Mods.Cursor.CompetitiveCrosshair")]
        CompetitiveCrosshair,

        [EnumSort(Order = 6)]
        [EnumName(FromTranslation = "Menu.Mods.Cursor.Custom")]
        Custom
    }
}
