namespace F1.Save
{
    /// <summary>Save DTO of settings.json. The owner is SettingManager.</summary>
    public sealed class SettingsData
    {
        /// <summary>2 (2026-10-05): the music and effect volumes. A version 1 file has neither and is read with both at full.</summary>
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion = CurrentSchemaVersion;

        /// <summary>BCP-47 code. Never an index or an enum.</summary>
        public string LocaleCode;

        /// <summary>0..100. A file without it (version 1) keeps this full value.</summary>
        public int MusicVolume = 100;

        /// <summary>0..100. A file without it (version 1) keeps this full value.</summary>
        public int EffectVolume = 100;
    }
}
