using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
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

            if (save.SchemaVersion == 6)
            {
                From6To7(save);
            }

            if (save.SchemaVersion == 7)
            {
                From7To8(save);
            }

            if (save.SchemaVersion == 8)
            {
                From8To9(save);
            }

            if (save.SchemaVersion == 9)
            {
                From9To10(save, data);
            }
        }

        /// <summary>
        /// Up to version 9 a board was an ordered list of items on the job's cells, an item taking its size in cells; from version 10 it
        /// is a grid of squares with bags (Slice B stage 19). Every member gets the start bag at the top-left, and its items lie unturned at
        /// the left edge one row after another in their old order, so the reading order is the old activation order (an item of the old
        /// size N is N rows tall now). The rows below the start bag that are needed get a bag of one row across the frame each. The
        /// inventory, a list counted by area, becomes a grid (round 49): its items in their old order, each at its first room. Unknown
        /// items are left for the validation; a file whose board needs a row bag the data does not have, or whose inventory does not fit
        /// the grid, cannot be read.
        /// </summary>
        static void From9To10(RunSaveData save, StaticData data)
        {
            if (save.Expedition?.Members != null)
            {
                BagData start = data.StartBag;
                BagData row = null;
                foreach (BagData bag in data.Bags.Ordered)
                {
                    if (!bag.Start && bag.Width == BoardFrame.Width && bag.Height == 1)
                    {
                        row = bag;
                        break;
                    }
                }

                foreach (MemberRecord member in save.Expedition.Members)
                {
                    if (member == null)
                    {
                        continue;
                    }

                    member.Bags = new List<BagRecord> { new BagRecord { BagId = start.Id, X = 0, Y = 0, Turns = 0 } };
                    int y = 0;
                    foreach (ItemRecord item in member.Items ?? new List<ItemRecord>())
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        item.X = 0;
                        item.Y = y;
                        item.Turns = 0;
                        y += data.Items.Contains(item.ItemId) ? data.Items.Get(item.ItemId).Height : 1;
                    }

                    for (int bagRow = start.Height; bagRow < y; bagRow++)
                    {
                        if (row == null || bagRow >= BoardFrame.Height)
                        {
                            throw new RunSaveException($"The board of '{member.MercenaryId}' cannot be laid out as a grid.");
                        }

                        member.Bags.Add(new BagRecord { BagId = row.Id, X = 0, Y = bagRow, Turns = 0 });
                    }
                }
            }

            if (save.Expedition?.Inventory != null)
            {
                var grid = new InventoryGrid(data.Balance.InventoryWidth, data.Balance.InventoryHeight);
                foreach (ItemRecord item in save.Expedition.Inventory)
                {
                    if (item == null || !data.Items.Contains(item.ItemId))
                    {
                        continue;
                    }

                    ItemData shape = data.Items.Get(item.ItemId);
                    if (!grid.FindRoom(shape.Width, shape.Height, null, out Placement at))
                    {
                        throw new RunSaveException("The inventory cannot be laid out as a grid.");
                    }

                    item.X = at.X;
                    item.Y = at.Y;
                    item.Turns = at.Turns;
                    grid.Items.Add(new BoardItem(new EquippedItem(shape, 1), at));
                }
            }

            save.SchemaVersion = 10;
        }

        /// <summary>
        /// Up to version 8 a won battle offered a reward to choose (PendingRewards); from version 9 it drops loot to pick up (Slice B
        /// stage 18). The old choices are not read any more: a file in the middle of one goes on to choosing the next node with no
        /// loot, so that one reward is left behind.
        /// </summary>
        static void From8To9(RunSaveData save)
        {
            if (save.Expedition != null)
            {
                if (save.Expedition.Phase == "ChoosingReward")
                {
                    save.Expedition.Phase = "ChoosingNode";
                }

                save.Expedition.Loot = new List<OfferRecord>();
            }

            save.SchemaVersion = 9;
        }

        /// <summary>
        /// Version 7 had no region coins and no shop (Slice B stage 17): an expedition of that version has no coins (the DTO's 0)
        /// and is at no shop (null), and no phase of it is the shop's.
        /// </summary>
        static void From7To8(RunSaveData save)
        {
            save.SchemaVersion = 8;
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

        /// <summary>The name version 5 gave the tier every item had before tiers (From6To7 renames it to Common).</summary>
        const string Version5Bronze = "Bronze";

        /// <summary>Version 4 had no item tiers (Slice B stage 14): every item was what version 5 called Bronze, now Common. (Its reward choices are left behind by From8To9.)</summary>
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
            }

            save.SchemaVersion = 5;
        }

        /// <summary>Version 5 had no fatigue states (Slice B stage 15): nobody was afflicted or virtuous, so the new fields stay null.</summary>
        static void From5To6(RunSaveData save)
        {
            save.SchemaVersion = 6;
        }

        /// <summary>
        /// Version 6 named the tiers Bronze, Silver, Gold and Diamond (The Bazaar's). Version 7 (2026-10-07 round 41) calls the same four
        /// Common, Bronze, Silver and Gold: the base tier is unmarked and the three above it read as copper, silver and gold. Only the
        /// names change; a name that is not one of the four is left for the validation to refuse.
        /// </summary>
        static void From6To7(RunSaveData save)
        {
            if (save.Expedition != null)
            {
                if (save.Expedition.Members != null)
                {
                    foreach (MemberRecord member in save.Expedition.Members)
                    {
                        RenameTiers(member?.Items);
                    }
                }

                RenameTiers(save.Expedition.Inventory);
            }

            save.SchemaVersion = 7;
        }

        static void RenameTiers(List<ItemRecord> items)
        {
            if (items == null)
            {
                return;
            }

            foreach (ItemRecord item in items)
            {
                if (item != null)
                {
                    item.Tier = Version7Name(item.Tier);
                }
            }
        }

        static string Version7Name(string version6Name)
        {
            switch (version6Name)
            {
                case "Bronze": return nameof(ItemTier.Common);
                case "Silver": return nameof(ItemTier.Bronze);
                case "Gold": return nameof(ItemTier.Silver);
                case "Diamond": return nameof(ItemTier.Gold);
                default: return version6Name;
            }
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
                    item.Tier = Version5Bronze;
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
