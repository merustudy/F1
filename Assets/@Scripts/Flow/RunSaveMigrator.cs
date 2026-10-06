using System;
using System.Collections.Generic;
using F1.Data;
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

        /// <summary>
        /// The fatigue a version 3 file counted down from (BalanceData.MaxFatigue of those builds). It describes the
        /// old files, not the rules: the current maximum is data.
        /// </summary>
        const int Version3MaxFatigue = 100;

        /// <param name="data">The static data, for what an old file did not store (which items are base weapons).</param>
        public static void Migrate(RunSaveData save, StaticData data)
        {
            if (save.SchemaVersion < OldestReadableVersion || save.SchemaVersion > RunSaveData.CurrentSchemaVersion)
            {
                throw new RunSaveException($"Schema version {save.SchemaVersion} cannot be read.");
            }

            // Each step edits the DTO in place and sets SchemaVersion to the next number.
            if (save.SchemaVersion == 2)
            {
                From2To3(save);
            }

            if (save.SchemaVersion == 3)
            {
                From3To4(save, data);
            }

            if (save.SchemaVersion == 4)
            {
                From4To5(save);
            }

            if (save.SchemaVersion == 5)
            {
                From5To6(save);
            }
        }

        /// <summary>
        /// Version 2 stored a board as one entry per slot, null for an empty one, and had no inventory.
        /// The entries that hold an item become the board in order; the inventory starts empty.
        /// Whether the board still fits its cells, now that items have sizes, is for the validation.
        /// </summary>
        static void From2To3(RunSaveData save)
        {
            if (save.Expedition != null)
            {
                if (save.Expedition.Members != null)
                {
                    foreach (MemberRecord member in save.Expedition.Members)
                    {
                        member?.Items?.RemoveAll(item => item == null);
                    }
                }

                save.Expedition.Inventory = new List<ItemRecord>();
            }

            save.SchemaVersion = 3;
        }

        /// <summary>
        /// Version 3 kept what was left of a mercenary's fatigue, counted down from its maximum, and paid an expedition's
        /// cost at the settlement; version 4 counts fatigue up from 0 and carries it on the expedition (Slice B). What was
        /// left becomes what was spent, and a member on an expedition carries what its mercenary left with. Version 3 did
        /// not mark base weapons: every job's weapon is one (none of them was ever a reward). Out-of-range numbers are left
        /// for the validation.
        /// </summary>
        static void From3To4(RunSaveData save, StaticData data)
        {
            var spent = new Dictionary<string, int>(StringComparer.Ordinal);
            if (save.Run?.Roster != null)
            {
                foreach (MercenaryRecord mercenary in save.Run.Roster)
                {
                    if (mercenary == null)
                    {
                        continue;
                    }

                    mercenary.Fatigue = Version3MaxFatigue - mercenary.Fatigue;
                    if (mercenary.Id != null)
                    {
                        spent[mercenary.Id] = mercenary.Fatigue;
                    }
                }
            }

            if (save.Expedition != null)
            {
                if (save.Expedition.Members != null)
                {
                    foreach (MemberRecord member in save.Expedition.Members)
                    {
                        if (member == null)
                        {
                            continue;
                        }

                        member.Fatigue = member.MercenaryId != null && spent.TryGetValue(member.MercenaryId, out int fatigue) ? fatigue : 0;
                        MarkBaseWeapons(member.Items, data);
                    }
                }

                MarkBaseWeapons(save.Expedition.Inventory, data);
            }

            save.SchemaVersion = 4;
        }

        /// <summary>Version 4 had no item tiers (Slice B stage 14): every item and every reward was what is now Bronze.</summary>
        static void From4To5(RunSaveData save)
        {
            if (save.Expedition != null)
            {
                if (save.Expedition.Members != null)
                {
                    foreach (MemberRecord member in save.Expedition.Members)
                    {
                        MarkBronze(member?.Items);
                    }
                }

                MarkBronze(save.Expedition.Inventory);
                if (save.Expedition.PendingRewards != null)
                {
                    foreach (RewardRecord reward in save.Expedition.PendingRewards)
                    {
                        if (reward != null)
                        {
                            reward.Tier = nameof(ItemTier.Bronze);
                        }
                    }
                }
            }

            save.SchemaVersion = 5;
        }

        /// <summary>Version 5 had no fatigue states (Slice B stage 15): nobody was afflicted or virtuous, so the new fields stay null.</summary>
        static void From5To6(RunSaveData save)
        {
            save.SchemaVersion = 6;
        }

        static void MarkBronze(List<ItemRecord> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (ItemRecord item in items)
            {
                if (item != null)
                {
                    item.Tier = nameof(ItemTier.Bronze);
                }
            }
        }

        static void MarkBaseWeapons(List<ItemRecord> items, StaticData data)
        {
            if (items == null)
            {
                return;
            }

            foreach (ItemRecord item in items)
            {
                if (item == null)
                {
                    continue;
                }

                foreach (JobData job in data.Jobs.Ordered)
                {
                    item.Base |= job.WeaponItemId == item.ItemId;
                }
            }
        }
    }
}
