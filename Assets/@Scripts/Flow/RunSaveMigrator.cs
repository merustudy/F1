using System.Collections.Generic;
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

            if (save.SchemaVersion == 21)
            {
                From21To22(save);
            }
        }

        /// <summary>
        /// Version 22 (Slice D stage 37) added the traits: a version 21 run had none, so every mercenary and candidate gets an empty list (the starting
        /// roster's traits of the data are not given to a run already going: Docs/Design/16 §5-7).
        /// </summary>
        static void From21To22(RunSaveData save)
        {
            if (save.Run != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster ?? new List<MercenaryRecord>())
                {
                    if (mercenary != null)
                    {
                        mercenary.Traits = mercenary.Traits ?? new List<string>();
                    }
                }

                foreach (RecruitRecord recruit in save.Run.Recruits ?? new List<RecruitRecord>())
                {
                    if (recruit != null)
                    {
                        recruit.Traits = recruit.Traits ?? new List<string>();
                    }
                }
            }

            save.SchemaVersion = 22;
        }
    }
}
