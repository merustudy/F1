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
                record.Roster.Add(new MercenaryRecord { Id = mercenary.Id, JobId = mercenary.JobId, Fatigue = mercenary.Fatigue });
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
                PendingRewards = new List<RewardRecord>(),
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
                });
            }

            foreach (RewardOption reward in expedition.PendingRewards)
            {
                record.PendingRewards.Add(new RewardRecord { Kind = reward.Kind.ToString(), Id = reward.Id, Grade = reward.Grade });
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

        static List<ItemRecord> ToRecords(IReadOnlyList<EquippedItem> items)
        {
            var records = new List<ItemRecord>();
            foreach (EquippedItem item in items)
            {
                records.Add(new ItemRecord { ItemId = item.Item.Id, Grade = item.Grade });
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
                run.Roster.Add(new MercenaryState { Id = mercenary.Id, JobId = mercenary.JobId, Fatigue = mercenary.Fatigue });
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
            Require(record.Members != null && record.Inventory != null && record.Potions != null && record.PendingRewards != null, "A list of the expedition is missing.");
            Require(record.BattlesWon >= 0, "BattlesWon is negative.");

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
            ReadRewards(record, data, state);
            ValidateBattle(record, phase);
            return state;
        }

        /// <summary>A list of items (a board or the inventory): every entry present, known and graded.</summary>
        static List<EquippedItem> ToItems(List<ItemRecord> records, StaticData data, string what)
        {
            Require(records != null, $"{what} is missing.");
            var items = new List<EquippedItem>();
            foreach (ItemRecord item in records)
            {
                Require(item != null, $"{what} has an empty entry.");
                Require(data.Items.Contains(item.ItemId), $"Unknown item '{item.ItemId}'.");
                Require(item.Grade >= 1, $"Grade of '{item.ItemId}' must be at least 1.");
                items.Add(new EquippedItem(data.Items.Get(item.ItemId), item.Grade));
            }

            return items;
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
        }

        static void ReadRewards(ExpeditionRecord record, StaticData data, ExpeditionState state)
        {
            Require((record.PendingRewards.Count > 0) == (state.Phase == ExpeditionPhase.ChoosingReward), "Pending rewards do not match the phase.");
            foreach (RewardRecord reward in record.PendingRewards)
            {
                Require(reward != null, "A reward is missing.");
                RewardKind kind = Parse<RewardKind>(reward.Kind);
                if (kind == RewardKind.Item)
                {
                    Require(data.Items.Contains(reward.Id) && reward.Grade >= 1, $"Reward item '{reward.Id}' is not valid.");
                }
                else
                {
                    Require(data.Potions.Contains(reward.Id) && reward.Grade == 0, $"Reward potion '{reward.Id}' is not valid.");
                }

                state.PendingRewards.Add(new RewardOption(kind, reward.Id, reward.Grade));
            }
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
