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
        /// The oldest schema this build can still read. The conversions of versions 2..11 were deleted on
        /// 2026-10-16 (Slice C stage 25, the user's decision): no shipped build wrote them, only development
        /// saves. Such a file is refused like version 1 was; the title offers a new run.
        /// </summary>
        public const int OldestReadableVersion = 12;

        /// <param name="data">The static data, for what an old file did not store.</param>
        public static void Migrate(RunSaveData save, StaticData data)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }

            // Each step edits the DTO in place and sets SchemaVersion to the next number.
            if (save.SchemaVersion == 12)
            {
                From12To13(save);
            }

            if (save.SchemaVersion == 13)
            {
                From13To14(save);
            }
        }

        /// <summary>
        /// Version 14 (Slice C stage 26) adds the lobby's gold, the stagecoach's candidates, the dismissed mercenaries and the expeditions per
        /// dungeon. A version 13 run had none of them: no gold, no candidates (the next settlement draws some), nobody dismissed, and its
        /// expeditions by dungeon are unknown (the total, ExpeditionCount, stays).
        /// </summary>
        static void From13To14(RunSaveData save)
        {
            if (save.Run != null)
            {
                save.Run.Gold = 0;
                save.Run.Recruits = new List<RecruitRecord>();
                save.Run.Dismissed = new List<string>();
                save.Run.Expeditions = new List<ExpeditionCountRecord>();
            }

            save.SchemaVersion = 14;
        }

        /// <summary>
        /// Version 12 said only whether the run was over (IsOver: nobody left alive). Version 13 says how the run stands (Outcome)
        /// and why a lost one was lost (DefeatCause), and whether the expedition is the final stand. A version 12 run was over only
        /// by a wipeout, so its outcome follows from its roster; none of its expeditions was a final stand. IsOver is not read.
        /// </summary>
        static void From12To13(RunSaveData save)
        {
            if (save.Run != null)
            {
                bool nobodyLeft = save.Run.Roster == null || save.Run.Roster.Count == 0;
                save.Run.Outcome = nobodyLeft ? "Defeat" : "Ongoing";
                save.Run.DefeatCause = nobodyLeft ? "Wipeout" : "None";
            }

            if (save.Expedition != null)
            {
                save.Expedition.FinalStand = false;
            }

            save.SchemaVersion = 13;
        }
    }
}
