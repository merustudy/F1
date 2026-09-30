using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;

namespace F1.Tests
{
    /// <summary>The application layer over the small test data, without Unity scenes or Addressables.</summary>
    internal sealed class FlowTestKit
    {
        public const ulong Seed = 7;

        public FlowTestKit(StaticData data = null, ulong seed = Seed)
        {
            Data = data ?? StrongParty();
            var dataManager = new DataManager(Data);
            Run = new RunManager(dataManager, () => seed);
            Expedition = new ExpeditionManager(dataManager, Run);
        }

        public StaticData Data { get; }
        public RunManager Run { get; }
        public ExpeditionManager Expedition { get; }

        /// <summary>The test data with weapons strong enough that the default party always clears the dungeon.</summary>
        public static StaticData StrongParty(params (string Key, int Value)[] balanceOverrides)
        {
            StaticDataParts parts = TestData.Parts(balanceOverrides);
            parts.Jobs = new List<JobData>
            {
                new JobData("tank", TestData.Text("tank"), 100, 3, "blade", 200, BattleRow.Front, null),
                new JobData("healer", TestData.Text("healer"), 60, 3, "staff", 10, BattleRow.Rear, null),
                new JobData("striker", TestData.Text("striker"), 80, 2, "blade", 200, BattleRow.Rear, null),
            };
            return new StaticData(parts);
        }

        /// <summary>The test data with enemies that kill the party: every battle is lost.</summary>
        public static StaticData DeadlyEnemies(params (string Key, int Value)[] balanceOverrides)
        {
            StaticDataParts parts = TestData.Parts(balanceOverrides);
            parts.Enemies = new List<EnemyData>
            {
                new EnemyData("grunt", TestData.Text("grunt"), 2, 5000, new List<ItemGrant> { new ItemGrant("claw", 500) }),
                new EnemyData("chief", TestData.Text("chief"), 9, 5000, new List<ItemGrant> { new ItemGrant("claw", 500) }),
            };
            return new StaticData(parts);
        }

        public static List<PartySlot> Party(params (string Id, BattleRow Row)[] slots)
        {
            return slots.Select(s => new PartySlot { MercenaryId = s.Id, Row = s.Row }).ToList();
        }

        public static List<PartySlot> DefaultParty()
        {
            return Party(("anna", BattleRow.Front), ("ben", BattleRow.Rear), ("cora", BattleRow.Rear));
        }

        /// <summary>A new run with the default party, standing in the lobby.</summary>
        public FlowTestKit InLobby()
        {
            Run.StartNewRun();
            Run.SetParty(DefaultParty());
            return this;
        }

        /// <summary>Departed to the test dungeon; the node map is showing.</summary>
        public FlowTestKit OnNodeMap()
        {
            InLobby();
            Expedition.Depart("cave");
            return this;
        }

        /// <summary>Entered the first available node; the battle has not advanced yet.</summary>
        public FlowTestKit InBattle()
        {
            OnNodeMap();
            Expedition.EnterNode(Expedition.AvailableNodes()[0].Id);
            return this;
        }

        /// <summary>Advances the current battle in steps until it ends. The session stays open.</summary>
        public void FightToTheEnd(int stepMs = 100)
        {
            int guard = 0;
            while (!Expedition.Battle.IsFinished)
            {
                Expedition.AdvanceBattle(stepMs);
                if (++guard > 1000000)
                {
                    throw new System.InvalidOperationException("The battle did not end.");
                }
            }
        }

        /// <summary>
        /// Plays with no input until the expedition is over: first available node, fight, close,
        /// skip rewards. Stops at the settlement report.
        /// </summary>
        public void PlayExpeditionToTheEnd()
        {
            while (Expedition.Phase != GamePhase.Settlement)
            {
                switch (Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        Expedition.EnterNode(Expedition.AvailableNodes()[0].Id);
                        break;
                    case GamePhase.Battle:
                        FightToTheEnd();
                        Expedition.CloseBattle();
                        break;
                    case GamePhase.Reward:
                        Expedition.SkipReward();
                        break;
                    default:
                        throw new System.InvalidOperationException($"Unexpected phase {Expedition.Phase}.");
                }
            }
        }
    }
}
