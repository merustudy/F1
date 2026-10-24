using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Data;
using F1.Gameplay;
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

            if (save.SchemaVersion == 22)
            {
                From22To23(save);
            }

            if (save.SchemaVersion == 23)
            {
                From23To24(save, data);
            }

            if (save.SchemaVersion == 24)
            {
                From24To25(save, data);
            }

            if (save.SchemaVersion == 25)
            {
                From25To26(save);
            }

            if (save.SchemaVersion == 26)
            {
                From26To27(save, data);
            }

            if (save.SchemaVersion == 27)
            {
                From27To28(save, data);
            }

            if (save.SchemaVersion == 28)
            {
                // Version 29 (stage 57): a mercenary's and a candidate's Figure; a version 28 file has none (the job's own figure). The shape is otherwise the same.
                save.SchemaVersion = 29;
            }
        }

        /// <summary>
        /// Version 28 (Slice D stage 55, the story): the run's SeenDialogues. A run of version 27 began before the story came into the game, so every
        /// dialogue of the data counts as heard: none breaks into it (Round 101 판정 13).
        /// </summary>
        static void From27To28(RunSaveData save, StaticData data)
        {
            if (save.Run != null)
            {
                save.Run.SeenDialogues = new List<string>();
                foreach (DialogueData dialogue in data.DialoguesInOrder)
                {
                    save.Run.SeenDialogues.Add(dialogue.Id);
                }
            }

            save.SchemaVersion = 28;
        }

        /// <summary>
        /// Version 27 (Slice D stage 49, 원정의 밀도 — Docs/Design/03_Dungeon_Structure.md §1, Design/26 판정 15): the mine became 19 floors and a boss, so the map
        /// an expedition in it rebuilds from its seed is another map. The DTO's shape is the same. An expedition going on in the mine (not a final stand) ends
        /// as a retreat where it stood and is settled as any retreat is (RunRules.Settle): the floor it had reached on the old map of 30 floors, found from
        /// the seed by the old widths (2..4 a floor, the generator's first draws), is moved to the new map at the same share of the way (1 + (n − 1) × 18/29,
        /// rounded), and the party stands on that floor's first node. A battle that was going on is left unfought: the members are as it began. Anything
        /// else in the file is as it was.
        /// </summary>
        static void From26To27(RunSaveData save, StaticData data)
        {
            const string Mine = "abandoned_mine";
            ExpeditionRecord record = save.Expedition;
            if (record != null && save.Run != null && !record.FinalStand && record.DungeonId == Mine
                && ulong.TryParse(record.Seed, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed))
            {
                DungeonData mine = data.Dungeons.Get(Mine);
                int floor = NewMineFloor(OldMineFloor(seed, record.CurrentNodeId), mine.Floors);
                NodeMap map = MapGenerator.Generate(data, mine, seed);
                record.CurrentNodeId = floor == 0 ? -1 : map.OnFloor(floor)[0].Id;
                record.BattlesWon = floor == 0 ? 0 : record.BattlesWon;
                record.Phase = nameof(ExpeditionPhase.ChoosingNode);
                record.Battle = null;
                record.Shop = null;
                record.Loot = new List<OfferRecord>();

                RunState run = RunSaveMapper.ToRunState(save.Run, data);
                ExpeditionState expedition = RunSaveMapper.ToExpeditionState(record, data, run);
                expedition.Result = ExpeditionResult.Retreated;
                expedition.Phase = ExpeditionPhase.Finished;
                RunRules.Settle(data, run, expedition);
                save.Run = RunSaveMapper.ToRecord(run);
                save.Expedition = null;
            }

            save.SchemaVersion = 27;
        }

        /// <summary>The floor of a node on the mine's map of version 26 (30 floors of 2..4 nodes; the boss counts as the last floor); 0 before the first.</summary>
        internal static int OldMineFloor(ulong seed, int nodeId)
        {
            const int OldFloors = 30, OldMinWidth = 2, OldMaxWidth = 4;
            if (nodeId < 0)
            {
                return 0;
            }

            var rng = new Pcg32(SeedDeriver.Derive(seed, "map", 0), RngStream.Map);
            int first = 0;
            for (int floor = 1; floor <= OldFloors; floor++)
            {
                first += rng.NextInt(OldMinWidth, OldMaxWidth);
                if (nodeId < first)
                {
                    return floor;
                }
            }

            return OldFloors;
        }

        /// <summary>An old floor of 30 at the same share of the way on a map of <paramref name="floors"/>, rounded half up; 0 stays 0.</summary>
        internal static int NewMineFloor(int oldFloor, int floors)
        {
            const int OldFloors = 30;
            return oldFloor <= 0 ? 0 : 1 + (2 * (oldFloor - 1) * (floors - 1) + (OldFloors - 1)) / (2 * (OldFloors - 1));
        }

        /// <summary>
        /// Version 26 (Slice D stage 47, 직업군 확대 — Docs/Design/05 §1, Design/20): the ranger became the archer (the same job under its new id; the rogue,
        /// the occultist and the minstrel are new and appear in no older file). Every roster mercenary's, candidate's and member's job id "ranger" becomes
        /// "archer". The DTO's shape is the same; nothing else changes.
        /// </summary>
        static void From25To26(RunSaveData save)
        {
            const string Ranger = "ranger", Archer = "archer";
            if (save.Run != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster ?? new List<MercenaryRecord>())
                {
                    if (mercenary != null && mercenary.JobId == Ranger)
                    {
                        mercenary.JobId = Archer;
                    }
                }

                foreach (RecruitRecord recruit in save.Run.Recruits ?? new List<RecruitRecord>())
                {
                    if (recruit != null && recruit.JobId == Ranger)
                    {
                        recruit.JobId = Archer;
                    }
                }
            }

            foreach (MemberRecord member in save.Expedition?.Members ?? new List<MemberRecord>())
            {
                if (member != null && member.JobId == Ranger)
                {
                    member.JobId = Archer;
                }
            }

            save.SchemaVersion = 26;
        }

        /// <summary>
        /// Version 24 (2026-10-20 스탯 개정, Docs/Design/05 §2): spirit became wisdom and the stats moved to the new scale — a job's base values 1..8 around the
        /// power baseline 5 instead of 10. Every mercenary's and candidate's stat is halved, rounded half up ((old + 1) / 2, the old 10 is the new 5), within
        /// StatMin..StatMax, and its Spi becomes its Wis. A member of an expedition going on gets the maximum HP of its new constitution (a job has no HP
        /// of its own any more) and keeps its HP in proportion, at least 1 while it had any.
        /// </summary>
        static void From23To24(RunSaveData save, StaticData data)
        {
            var statsOf = new Dictionary<string, StatsRecord>(StringComparer.Ordinal);
            var traitsOf = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (save.Run != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster ?? new List<MercenaryRecord>())
                {
                    if (mercenary != null && mercenary.Stats != null)
                    {
                        Rescale(mercenary.Stats, data.Balance);
                        statsOf[mercenary.Id ?? ""] = mercenary.Stats;
                        traitsOf[mercenary.Id ?? ""] = mercenary.Traits ?? new List<string>();
                    }
                }

                foreach (RecruitRecord recruit in save.Run.Recruits ?? new List<RecruitRecord>())
                {
                    if (recruit != null && recruit.Stats != null)
                    {
                        Rescale(recruit.Stats, data.Balance);
                    }
                }
            }

            foreach (MemberRecord member in save.Expedition?.Members ?? new List<MemberRecord>())
            {
                if (member == null || member.MaxHp < 1 || !statsOf.TryGetValue(member.MercenaryId ?? "", out StatsRecord record))
                {
                    continue;
                }

                var stats = new StatBlock(record.Str, record.Dex, record.Con, record.Int, record.Wis, record.End);
                int maxHp = LevelRules.MaxHpOf(data, member.JobId, stats, TraitRules.BonusOf(data, traitsOf[member.MercenaryId ?? ""]));
                int hp = (int)((long)member.Hp * maxHp / member.MaxHp);
                member.Hp = member.Hp > 0 && hp < 1 ? 1 : Math.Min(hp, maxHp);
                member.MaxHp = maxHp;
            }

            save.SchemaVersion = 24;
        }

        /// <summary>
        /// Version 25 (Slice D stage 42, 장비 요구 스탯 — Docs/Design/02 §4, 05 §2): equipment asks its owner for a stat, and every mercenary of a job wields its
        /// base weapon. A roster mercenary's or a candidate's stat its job's base weapon asks for is raised to what it asks when it fell short (a candidate of
        /// version 24 could roll under it; the stagecoach keeps them above it since). The DTO's shape is the same; nothing else changes. A member of an
        /// expedition going on takes the roster's stats, so it is lifted with its mercenary.
        /// </summary>
        static void From24To25(RunSaveData save, StaticData data)
        {
            if (save.Run != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster ?? new List<MercenaryRecord>())
                {
                    if (mercenary != null)
                    {
                        LiftToBaseWeapon(data, mercenary.JobId, mercenary.Stats);
                    }
                }

                foreach (RecruitRecord recruit in save.Run.Recruits ?? new List<RecruitRecord>())
                {
                    if (recruit != null)
                    {
                        LiftToBaseWeapon(data, recruit.JobId, recruit.Stats);
                    }
                }
            }

            save.SchemaVersion = 25;
        }

        /// <summary>Raises the one stat a job's base weapon asks for to what it asks (<see cref="StatRules.MeetingBaseWeapon"/>). A record the mapper will refuse (no stats, an unknown job) is left to it.</summary>
        static void LiftToBaseWeapon(StaticData data, string jobId, StatsRecord record)
        {
            if (record == null || jobId == null || !data.Jobs.Contains(jobId))
            {
                return;
            }

            StatBlock lifted = StatRules.MeetingBaseWeapon(data, jobId, new StatBlock(record.Str, record.Dex, record.Con, record.Int, record.Wis, record.End));
            record.Str = lifted.Str;
            record.Dex = lifted.Dex;
            record.Con = lifted.Con;
            record.Int = lifted.Int;
            record.Wis = lifted.Wis;
            record.End = lifted.End;
        }

        /// <summary>Half, rounded half up, within the balance's range; Spi read into Wis and cleared.</summary>
        static void Rescale(StatsRecord stats, BalanceData balance)
        {
            int Half(int old) => Math.Max(balance.StatMin, Math.Min(balance.StatMax, (old + 1) / 2));
            stats.Str = Half(stats.Str);
            stats.Dex = Half(stats.Dex);
            stats.Con = Half(stats.Con);
            stats.Int = Half(stats.Int);
            stats.Wis = Half(stats.Spi ?? stats.Wis);
            stats.Spi = null;
            stats.End = Half(stats.End);
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

        /// <summary>
        /// Version 23 (Slice D stage 38) added whether a member was at death's door on the expedition: a version 22 expedition going on counts as
        /// none (the field reads false), so its survivors roll no death's-door trait for the battles before.
        /// </summary>
        static void From22To23(RunSaveData save)
        {
            save.SchemaVersion = 23;
        }
    }
}
