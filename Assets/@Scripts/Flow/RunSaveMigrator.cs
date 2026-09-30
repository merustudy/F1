using F1.Save;

namespace F1.Flow
{
    /// <summary>
    /// Brings an older run save up to the current schema, one version at a time. A newer file than
    /// this build knows is refused.
    /// </summary>
    public static class RunSaveMigrator
    {
        /// <summary>The oldest schema this build can still read.</summary>
        public const int OldestReadableVersion = 1;

        public static void Migrate(RunSaveData save)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }

            // When the schema changes, add one step per version here:
            //     if (save.SchemaVersion == 1) { From1To2(save); }
            // Each step edits the DTO in place and sets SchemaVersion to the next number.
        }
    }
}
