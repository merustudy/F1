namespace F1.Save
{
    /// <summary>Save DTO of settings.json. The owner is SettingManager.</summary>
    public sealed class SettingsData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;

        /// <summary>BCP-47 code. Never an index or an enum.</summary>
        public string LocaleCode;
    }
}
