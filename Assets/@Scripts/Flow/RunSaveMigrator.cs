using F1.Save;

namespace F1.Flow
{
    /// <summary>
    /// Brings an older run save up to the current schema, one version at a time. A newer file than
    /// this build knows is refused.
    /// </summary>
    public static class RunSaveMigrator
    {
        /// <summary>
        /// The oldest schema this build can still read. Version 1 had two rows (front, rear); a run
        /// saved under those rules is not converted to numbered rows.
        /// </summary>
        public const int OldestReadableVersion = 2;

        public static void Migrate(RunSaveData save)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }

            // When the schema changes and older files should still load, add one step per version here:
            //     if (save.SchemaVersion == 2) { From2To3(save); }
            // Each step edits the DTO in place and sets SchemaVersion to the next number.
        }
    }
}
