using System;
using System.Collections.Generic;
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
