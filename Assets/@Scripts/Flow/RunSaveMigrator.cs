using F1.Data;
using F1.Save;

namespace F1.Flow
{
    /// <summary>
    /// Brings an older run save up to the current schema, one version at a time. A newer file than
    /// this build knows is refused, and so is one older than <see cref="OldestReadableVersion"/>.
    /// </summary>
    public static class RunSaveMigrator
    {
        /// <summary>
        /// The oldest schema this build can still read. Version 21 (Slice D stage 36) changed the jobs (five, new ids: Docs/Design/05 §1),
        /// and by the user's decision (2026-10-18 Design/15 판정 17, "옛 저장은 삭제") the runs before it are not converted: every older
        /// file is refused like version 1 was, and the title offers a new run. The conversions of 12..20 went with it.
        /// </summary>
        public const int OldestReadableVersion = 21;

        /// <param name="data">The static data, for what an old file did not store (none since version 21).</param>
        public static void Migrate(RunSaveData save, StaticData data)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }
        }
    }
}
