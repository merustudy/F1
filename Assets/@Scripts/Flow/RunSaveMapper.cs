using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Data;
using F1.Gameplay;
using F1.Save;

namespace F1.Flow
{
    /// <summary>A save file that cannot be turned back into a valid state.</summary>
    public sealed class RunSaveException : Exception
    {
        public RunSaveException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Converts between the run save DTO and the domain state. Reading validates everything against
    /// the static data: a file that does not describe a legal state is rejected as a whole.
    /// </summary>
    public static class RunSaveMapper
    {
        // ---- State -> DTO --------------------------------------------------------------------

        public static RunSaveData ToSave(RunState run, ExpeditionRecord expedition)
        {
            return new RunSaveData
            {
                SchemaVersion = RunSaveData.CurrentSchemaVersion,
                Run = ToRecord(run),
                Expedition = expedition,
            };
        }

        public static RunRecord ToRecord(RunState run)
        {
            var record = new RunRecord
            {
                Seed = Text(run.Seed),
                Day = run.Day,
                Roster = new List<MercenaryRecord>(),
                Fallen = new List<string>(run.Fallen),
                Party = new List<PartySlotRecord>(),
                ExpeditionCount = run.ExpeditionCount,
                ClearedDungeons = new List<ClearRecord>(),
                IsOver = run.IsOver,
            };

            foreach (MercenaryState mercenary in run.Roster)
            {
                record.Roster.Add(new MercenaryRecord { Id = mercenary.Id, JobId = mercenary.JobId, Fatigue = mercenary.Fatigue, Affliction = mercenary.AfflictionId });
            }

            foreach (PartySlot slot in run.Party)
            {
                record.Party.Add(new PartySlotRecord { MercenaryId = slot.MercenaryId, Row = slot.Row });
            }

            foreach (KeyValuePair<string, int> cleared in run.ClearedDungeons)
            {
                record.ClearedDungeons.Add(new ClearRecord { DungeonId = cleared.Key, Count = cleared.Value });
            }

            return record;
        }

        /// <param name="battle">The battle being fought, or null when the expedition is between battles.</param>
        public static ExpeditionRecord ToRecord(ExpeditionState expedition, BattleEngine battle)
        {
            var record = new ExpeditionRecord
            {
                DungeonId = expedition.DungeonId,
                Seed = Text(expedition.Seed),
                Members = new List<MemberRecord>(),
                Inventory = ToRecords(expedition.Inventory),
                Potions = new List<string>(expedition.Potions),
                Phase = expedition.Phase.ToString(),
                CurrentNodeId = expedition.CurrentNodeId,
                BattlesWon = expedition.BattlesWon,
                Loot = new List<OfferRecord>(),
                Coins = expedition.Coins,
                Shop = ToRecord(expedition.Shop),
            };

            foreach (ExpeditionMember member in expedition.Members)
            {
                record.Members.Add(new MemberRecord
                {
                    MercenaryId = member.MercenaryId,
                    JobId = member.JobId,
                    Row = member.Row,
                    MaxHp = member.MaxHp,
                    Hp = member.Hp,
                    Alive = member.Alive,
                    Items = ToRecords(member.Items),
                    Fatigue = member.Fatigue,
                    State = member.StateId,
                });
            }

            foreach (ItemOffer drop in expedition.Loot)
            {
                record.Loot.Add(drop == null ? null : new OfferRecord { Kind = drop.Kind.ToString(), Id = drop.Id, Grade = drop.Grade, Tier = drop.Tier.ToString() });
            }

            if (battle != null)
            {
                var inputs = new List<BattleInputRecord>();
                foreach (BattleInput input in battle.Inputs)
                {
                    inputs.Add(new BattleInputRecord
                    {
                        TimeMs = input.TimeMs,
                        Kind = input.Kind.ToString(),
                        PotionSlot = input.PotionSlot,
                        PartyIndex = input.PartyIndex,
                    });
                }

                record.Battle = new BattleRecord
                {
                    ConfirmedTimeMs = battle.TimeMs,
                    Inputs = inputs,
                    LogHash = Text(BattleLog.Hash(battle.Events)),
                };
            }

            return record;
        }

        /// <summary>The shop's stock by slot, a sold slot as null; null when the party is at no shop.</summary>
        static ShopRecord ToRecord(ShopState shop)
        {
            if (shop == null)
            {
                return null;
            }

            var record = new ShopRecord { Stock = new List<OfferRecord>(), Refreshes = shop.Refreshes };
            foreach (ItemOffer offer in shop.Stock)
            {
                record.Stock.Add(offer == null ? null : new OfferRecord { Kind = offer.Kind.ToString(), Id = offer.Id, Grade = offer.Grade, Tier = offer.Tier.ToString() });
            }

            return record;
        }

        static List<ItemRecord> ToRecords(IReadOnlyList<EquippedItem> items)
        {
            var records = new List<ItemRecord>();
            foreach (EquippedItem item in items)
            {
                records.Add(new ItemRecord { ItemId = item.Item.Id, Grade = item.Grade, Base = item.IsBase, Tier = item.Tier.ToString() });
            }

            return records;
        }

        // ---- DTO -> state --------------------------------------------------------------------

        /// <summary>
        /// Turns a loaded file into state. <paramref name="expedition"/> is null when the party is at
        /// home. Throws <see cref="RunSaveException"/> when the file does not describe a legal state.
        /// </summary>
        public static void Read(RunSaveData save, StaticData data, out RunState run, out ExpeditionState expedition)
        {
            Require(save != null, "The file is empty.");
            run = ToRunState(save.Run, data);
            expedition = save.Expedition == null ? null : ToExpeditionState(save.Expedition, data, run);
        }

        public static RunState ToRunState(RunRecord record, StaticData data)
        {
            Require(record != null, "Run is missing.");
            Require(record.Day >= 1, "Day must be at least 1.");
            Require(record.ExpeditionCount >= 0, "ExpeditionCount is negative.");
            Require(record.Roster != null && record.Fallen != null && record.Party != null && record.ClearedDungeons != null, "A list of the run is missing.");

            var run = new RunState
            {
                Seed = Seed(record.Seed),
                Day = record.Day,
                ExpeditionCount = record.ExpeditionCount,
            };

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (MercenaryRecord mercenary in record.Roster)
            {
                Require(mercenary != null && data.Mercenaries.Contains(mercenary.Id), $"Unknown mercenary '{mercenary?.Id}'.");
                Require(data.Jobs.Contains(mercenary.JobId), $"Unknown job '{mercenary.JobId}'.");
                Require(known.Add(mercenary.Id), $"Mercenary '{mercenary.Id}' is listed twice.");
                Require(mercenary.Fatigue >= 0 && mercenary.Fatigue <= data.Balance.MaxFatigue, $"Fatigue of '{mercenary.Id}' is out of range.");
                RequireState(data, mercenary.Affliction, mercenary.Fatigue, $"'{mercenary.Id}'", atHome: true);
                run.Roster.Add(new MercenaryState { Id = mercenary.Id, JobId = mercenary.JobId, Fatigue = mercenary.Fatigue, AfflictionId = mercenary.Affliction });
            }

            foreach (string fallen in record.Fallen)
            {
                Require(data.Mercenaries.Contains(fallen), $"Unknown mercenary '{fallen}'.");
                Require(known.Add(fallen), $"Mercenary '{fallen}' is listed twice.");
                run.Fallen.Add(fallen);
            }

            var party = new List<PartySlot>();
            foreach (PartySlotRecord slot in record.Party)
            {
                Require(slot != null, "A party slot is missing.");
                party.Add(new PartySlot { MercenaryId = slot.MercenaryId, Row = slot.Row });
            }

            string problem = RunRules.PartyProblem(data, run, party);
            Require(problem == null, problem);
            run.Party = party;

            foreach (ClearRecord cleared in record.ClearedDungeons)
            {
                Require(cleared != null && data.Dungeons.Contains(cleared.DungeonId), $"Unknown dungeon '{cleared?.DungeonId}'.");
                Require(cleared.Count >= 1, $"Clear count of '{cleared.DungeonId}' must be at least 1.");
                Require(!run.ClearedDungeons.ContainsKey(cleared.DungeonId), $"Dungeon '{cleared.DungeonId}' is listed twice.");
                run.ClearedDungeons.Add(cleared.DungeonId, cleared.Count);
            }

            Require(record.IsOver == (run.Roster.Count == 0), "IsOver does not match the roster.");
            run.IsOver = record.IsOver;
            return run;
        }

        public static ExpeditionState ToExpeditionState(ExpeditionRecord record, StaticData data, RunState run)
        {
            Require(record != null, "Expedition is missing.");
            Require(data.Dungeons.Contains(record.DungeonId), $"Unknown dungeon '{record.DungeonId}'.");
            Require(record.Members != null && record.Inventory != null && record.Potions != null && record.Loot != null, "A list of the expedition is missing.");
            Require(record.BattlesWon >= 0, "BattlesWon is negative.");
            Require(record.Coins >= 0, "Coins are negative.");

            // The expedition seed is derived from the run, so the two must agree.
            ulong seed = Seed(record.Seed);
            Require(run.ExpeditionCount >= 1 && seed == SeedDeriver.Derive(run.Seed, "expedition", run.ExpeditionCount - 1), "The expedition seed does not belong to this run.");

            ExpeditionPhase phase = Parse<ExpeditionPhase>(record.Phase);
            Require(phase != ExpeditionPhase.Finished, "A finished expedition is never saved.");

            BalanceData balance = data.Balance;
            var state = new ExpeditionState
            {
                DungeonId = record.DungeonId,
                Seed = seed,
                Map = MapGenerator.Generate(data, data.Dungeons.Get(record.DungeonId), seed),
                Phase = phase,
                Result = ExpeditionResult.None,
                CurrentNodeId = record.CurrentNodeId,
                BattlesWon = record.BattlesWon,
                Coins = record.Coins,
            };

            Require(record.Members.Count >= balance.MinPartySize && record.Members.Count <= balance.PartySize, "The party size is out of range.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int alive = 0;
            foreach (MemberRecord member in record.Members)
            {
                Require(member != null, "A member is missing.");
                MercenaryState mercenary = RunRules.FindMercenary(run, member.MercenaryId);
                Require(mercenary != null, $"Member '{member.MercenaryId}' is not in the roster.");
                Require(mercenary.JobId == member.JobId, $"Member '{member.MercenaryId}' has a different job in the roster.");
                Require(seen.Add(member.MercenaryId), $"Member '{member.MercenaryId}' is listed twice.");
                Require(member.MaxHp >= 1 && member.Hp >= 0 && member.Hp <= member.MaxHp, $"HP of '{member.MercenaryId}' is out of range.");
                Require(member.Alive || member.Hp == 0, $"Dead member '{member.MercenaryId}' has HP.");
                Require(BattleRows.IsValid(member.Row), $"Row of '{member.MercenaryId}' is out of range.");
                Require(member.Fatigue >= 0 && member.Fatigue <= balance.MaxFatigue, $"Fatigue of member '{member.MercenaryId}' is out of range.");
                RequireState(data, member.State, member.Fatigue, $"member '{member.MercenaryId}'", atHome: false);

                // The board's cells come from the job; the items must still fit them.
                int cells = data.Jobs.Get(member.JobId).ItemSlots;
                List<EquippedItem> items = ToItems(member.Items, data, $"The board of '{member.MercenaryId}'");
                Require(ItemBoard.UsedCells(items) <= cells, $"The board of '{member.MercenaryId}' holds more than its {cells} cells.");

                if (member.Alive)
                {
                    alive++;
                }

                state.Members.Add(new ExpeditionMember
                {
                    MercenaryId = member.MercenaryId,
                    JobId = member.JobId,
                    Row = member.Row,
                    MaxHp = member.MaxHp,
                    Hp = member.Hp,
                    Alive = member.Alive,
                    Items = items,
                    ItemSlots = cells,
                    Fatigue = member.Fatigue,
                    StateId = member.State,
                });
            }

            Require(alive >= 1, "Nobody on the expedition is alive.");
            state.Inventory = ToItems(record.Inventory, data, "The inventory");
            Require(ItemBoard.UsedCells(state.Inventory) <= balance.InventoryCells, $"The inventory holds more than its {balance.InventoryCells} cells.");

            // The living stand one per row from the front, with no empty row between them.
            string formationProblem = Formation.Problem(ExpeditionRules.LivingRows(state, out _));
            Require(formationProblem == null, formationProblem);

            Require(record.Potions.Count == balance.PotionSlots, "The number of potion slots does not match.");
            state.Potions = new string[balance.PotionSlots];
            for (int i = 0; i < state.Potions.Length; i++)
            {
                string potionId = record.Potions[i];
                Require(potionId == null || data.Potions.Contains(potionId), $"Unknown potion '{potionId}'.");
                state.Potions[i] = potionId;
            }

            ReadPosition(record, state);
            ReadLoot(record, data, state);
            ReadShop(record, data, state);
            ValidateBattle(record, phase);
            return state;
        }

        /// <summary>
        /// A list of items (a board or the inventory): every entry present, known and graded. A base weapon
        /// must be the weapon of some job: nothing found on the way can become free of fatigue.
        /// </summary>
        static List<EquippedItem> ToItems(List<ItemRecord> records, StaticData data, string what)
        {
            Require(records != null, $"{what} is missing.");
            var items = new List<EquippedItem>();
            foreach (ItemRecord item in records)
            {
                Require(item != null, $"{what} has an empty entry.");
                Require(data.Items.Contains(item.ItemId), $"Unknown item '{item.ItemId}'.");
                Require(item.Grade >= 1, $"Grade of '{item.ItemId}' must be at least 1.");
                Require(!item.Base || IsJobWeapon(data, item.ItemId), $"'{item.ItemId}' is marked as a base weapon but no job carries it.");
                items.Add(new EquippedItem(data.Items.Get(item.ItemId), item.Grade, item.Base, Parse<ItemTier>(item.Tier)));
            }

            return items;
        }

        static bool IsJobWeapon(StaticData data, string itemId)
        {
            foreach (JobData job in data.Jobs.Ordered)
            {
                if (job.WeaponItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The recorded inputs of the battle in progress, in order.</summary>
        public static List<BattleInput> ToInputs(BattleRecord record)
        {
            var inputs = new List<BattleInput>();
            foreach (BattleInputRecord input in record.Inputs)
            {
                inputs.Add(new BattleInput(input.TimeMs, Parse<BattleInputKind>(input.Kind), input.PotionSlot, input.PartyIndex));
            }

            return inputs;
        }

        /// <summary>The saved digest of the event log, or null when it cannot be read.</summary>
        public static ulong? LogHash(BattleRecord record)
        {
            return ulong.TryParse(record.LogHash, NumberStyles.None, CultureInfo.InvariantCulture, out ulong hash) ? hash : (ulong?)null;
        }

        static void ReadPosition(ExpeditionRecord record, ExpeditionState state)
        {
            if (record.CurrentNodeId < 0)
            {
                Require(record.CurrentNodeId == -1 && state.Phase == ExpeditionPhase.ChoosingNode && record.BattlesWon == 0, "The expedition has no current node.");
                return;
            }

            Require(record.CurrentNodeId < state.Map.Nodes.Count, $"Node {record.CurrentNodeId} is not on the map.");
            MapNode node = state.Map.Get(record.CurrentNodeId);
            Require(state.Phase == ExpeditionPhase.InBattle || node.Kind != MapNodeKind.Boss, "The expedition cannot continue past the boss.");

            // A camp is camped at or left behind; a shop is shopped at or left behind; nothing else happens at either, and neither happens elsewhere.
            Require((state.Phase == ExpeditionPhase.AtCamp) == (node.Kind == MapNodeKind.Camp && state.Phase != ExpeditionPhase.ChoosingNode),
                "Only a camp node is camped at, and a camp node is either camped at or left behind.");
            Require((state.Phase == ExpeditionPhase.AtShop) == (node.Kind == MapNodeKind.Shop && state.Phase != ExpeditionPhase.ChoosingNode),
                "Only a shop node is shopped at, and a shop node is either shopped at or left behind.");
        }

        /// <summary>
        /// The loot (version 9): present exactly while the phase is picking it, no more drops than the screen has cards for, every drop a
        /// known item (a potion never drops), a taken slot null, and at least one drop still lying there (the last one taken ends the loot).
        /// </summary>
        static void ReadLoot(ExpeditionRecord record, StaticData data, ExpeditionState state)
        {
            Require((record.Loot.Count > 0) == (state.Phase == ExpeditionPhase.PickingLoot), "The loot does not match the phase.");
            Require(record.Loot.Count <= BalanceData.MaxLootCards, $"The loot holds more than {BalanceData.MaxLootCards} drops.");
            bool lying = false;
            foreach (OfferRecord drop in record.Loot)
            {
                ItemOffer offer = drop == null ? null : ReadOption(drop, data);
                Require(offer == null || offer.Kind == OfferKind.Item, "A potion never drops.");
                lying |= offer != null;
                state.Loot.Add(offer);
            }

            Require(record.Loot.Count == 0 || lying, "Every drop of the loot was taken, so the loot would be over.");
        }

        /// <summary>The shop the party is at (version 8): present exactly while the phase is the shop's, its stock within the slots, a sold slot null.</summary>
        static void ReadShop(ExpeditionRecord record, StaticData data, ExpeditionState state)
        {
            Require((record.Shop != null) == (state.Phase == ExpeditionPhase.AtShop), "The shop record does not match the phase.");
            if (record.Shop == null)
            {
                return;
            }

            Require(record.Shop.Stock != null && record.Shop.Refreshes >= 0, "The shop record is incomplete.");
            Require(record.Shop.Stock.Count <= data.Balance.ShopSlots, $"The shop offers more than its {data.Balance.ShopSlots} slots.");
            var shop = new ShopState { Refreshes = record.Shop.Refreshes };
            foreach (OfferRecord offer in record.Shop.Stock)
            {
                shop.Stock.Add(offer == null ? null : ReadOption(offer, data));
            }

            state.Shop = shop;
        }

        /// <summary>A drop of loot or an offer of a shop: a known item with a grade, or a known potion at Common with no grade.</summary>
        static ItemOffer ReadOption(OfferRecord record, StaticData data)
        {
            OfferKind kind = Parse<OfferKind>(record.Kind);
            ItemTier tier = Parse<ItemTier>(record.Tier);
            if (kind == OfferKind.Item)
            {
                Require(data.Items.Contains(record.Id) && record.Grade >= 1, $"Offer item '{record.Id}' is not valid.");
            }
            else
            {
                Require(data.Potions.Contains(record.Id) && record.Grade == 0 && tier == ItemTier.Common, $"Offer potion '{record.Id}' is not valid.");
            }

            return new ItemOffer(kind, record.Id, record.Grade, tier);
        }

        static void ValidateBattle(ExpeditionRecord record, ExpeditionPhase phase)
        {
            Require((record.Battle != null) == (phase == ExpeditionPhase.InBattle), "The battle record does not match the phase.");
            if (record.Battle == null)
            {
                return;
            }

            Require(record.Battle.ConfirmedTimeMs >= 0 && record.Battle.Inputs != null, "The battle record is incomplete.");
            int previous = 0;
            foreach (BattleInputRecord input in record.Battle.Inputs)
            {
                Require(input != null, "A battle input is missing.");
                Parse<BattleInputKind>(input.Kind);
                Require(input.TimeMs >= previous && input.TimeMs <= record.Battle.ConfirmedTimeMs, "Battle inputs are out of order.");
                Require(input.PotionSlot >= 0 && input.PartyIndex >= 0, "A battle input has a negative index.");
                previous = input.TimeMs;
            }
        }

        static string Text(ulong value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        static ulong Seed(string text)
        {
            Require(ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed), $"'{text}' is not a seed.");
            return seed;
        }

        /// <summary>Only names defined by the enum are accepted; numbers are not.</summary>
        /// <summary>
        /// A saved fatigue state must exist; an affliction must stand at or over the threshold (under it, it would have ended); a
        /// mercenary at home holds only an affliction (a virtue ends with the expedition).
        /// </summary>
        static void RequireState(StaticData data, string stateId, int fatigue, string who, bool atHome)
        {
            if (stateId == null)
            {
                return;
            }

            Require(data.FatigueStates.Contains(stateId), $"Unknown fatigue state '{stateId}' of {who}.");
            FatigueStateData state = data.FatigueStates.Get(stateId);
            Require(!atHome || state.Kind == FatigueStateKind.Affliction, $"{who} holds the virtue '{stateId}' at home.");
            Require(state.Kind != FatigueStateKind.Affliction || fatigue >= data.Balance.FatigueBreakdown, $"{who} holds the affliction '{stateId}' under the breakdown.");
        }

        static TEnum Parse<TEnum>(string name)
            where TEnum : struct
        {
            Require(name != null && Enum.IsDefined(typeof(TEnum), name), $"'{name}' is not a {typeof(TEnum).Name}.");
            return (TEnum)Enum.Parse(typeof(TEnum), name);
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new RunSaveException(message);
            }
        }
    }
}
