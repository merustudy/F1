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

            if (save.SchemaVersion == 14)
            {
                From14To15(save);
            }

            if (save.SchemaVersion == 15)
            {
                From15To16(save);
            }

            if (save.SchemaVersion == 16)
            {
                From16To17(save);
            }
        }

        /// <summary>
        /// Version 17 (2026-10-18, the user: the gate has no upgrade) lists no gate among the buildings: a version 16 gate's level and its
        /// construction are dropped (the gold paid for a construction going on is not given back; it was already spent).
        /// </summary>
        static void From16To17(RunSaveData save)
        {
            if (save.Run != null)
            {
                save.Run.Buildings?.RemoveAll(building => building != null && building.BuildingId == "gate");
                save.Run.Constructions?.RemoveAll(construction => construction != null && construction.BuildingId == "gate");
            }

            save.SchemaVersion = 17;
        }

        /// <summary>Version 16 (Slice C stage 30) adds the town's building levels and constructions. A version 15 town is all at level 1 with nothing being built.</summary>
        static void From15To16(RunSaveData save)
        {
            if (save.Run != null)
            {
                save.Run.Buildings = new List<BuildingLevelRecord>();
                save.Run.Constructions = new List<ConstructionRecord>();
            }

            save.SchemaVersion = 16;
        }

        /// <summary>Version 15 (Slice C stage 27) adds a mercenary's level and experience. A version 14 mercenary is level 1 with none.</summary>
        static void From14To15(RunSaveData save)
        {
            if (save.Run?.Roster != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster)
                {
                    if (mercenary != null)
                    {
                        mercenary.Level = 1;
                        mercenary.Exp = 0;
                    }
                }
            }

            save.SchemaVersion = 15;
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
