using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;

namespace F1.Tests
{
    /// <summary>
    /// The application layer over the small test data, without Unity scenes or Addressables.
    /// It saves into a temporary directory of its own; fixtures call <see cref="DeleteSaveRoots"/> on tear down.
    /// </summary>
    internal sealed class FlowTestKit
    {
        public const ulong Seed = 7;

        static readonly List<string> SaveRoots = new List<string>();

        readonly ulong _seed;

        public FlowTestKit(StaticData data = null, ulong seed = Seed, string saveRoot = null)
        {
            Data = data ?? StrongParty();
            _seed = seed;
            if (saveRoot == null)
            {
                saveRoot = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
                SaveRoots.Add(saveRoot);
            }

            SaveRoot = saveRoot;
            Save = new SaveManager(saveRoot);
            Save.Initialize();

            var dataManager = new DataManager(Data);
            Run = new RunManager(dataManager, Save, () => seed);
            Expedition = new ExpeditionManager(dataManager, Run);
        }

        public StaticData Data { get; }
        public string SaveRoot { get; }
        public SaveManager Save { get; }
        public RunManager Run { get; }
        public ExpeditionManager Expedition { get; }

        public string RunFilePath => Path.Combine(SaveRoot, RunManager.FileName);

        /// <summary>
        /// Closes the app and starts it again: new managers over the same save directory, then the
        /// boot steps that read the run.
        /// </summary>
        public FlowTestKit Restart(StaticData data = null)
        {
            var next = new FlowTestKit(data ?? Data, _seed, SaveRoot);
            next.Run.Load();
            next.Expedition.Restore();
            return next;
        }

        /// <summary>What run.json holds now, as text.</summary>
        public string SavedText()
        {
            return File.ReadAllText(RunFilePath);
        }

        /// <summary>Removes the temporary save directories made by kits of the finished test.</summary>
        public static void DeleteSaveRoots()
        {
            foreach (string root in SaveRoots)
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
                else if (File.Exists(root))
                {
                    File.Delete(root);
                }
            }

            SaveRoots.Clear();
        }

        /// <summary>The test data with weapons strong enough that the default party always clears the dungeon.</summary>
        public static StaticData StrongParty(params (string Key, int Value)[] balanceOverrides)
        {
            StaticDataParts parts = TestData.Parts(balanceOverrides);
            parts.Jobs = new List<JobData>
            {
                new JobData("tank", TestData.Text("tank"), 100, "blade", 200, 1, null),
                new JobData("healer", TestData.Text("healer"), 60, "staff", 10, 2, null),
                new JobData("striker", TestData.Text("striker"), 80, "blade", 200, 3, null),
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

        public static List<PartySlot> Party(params (string Id, int Row)[] slots)
        {
            return slots.Select(s => new PartySlot { MercenaryId = s.Id, Row = s.Row }).ToList();
        }

        public static List<PartySlot> DefaultParty()
        {
            return Party(("anna", 1), ("ben", 2), ("cora", 3));
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
                    throw new InvalidOperationException("The battle did not end.");
                }
            }
        }

        /// <summary>
        /// Plays with no input until the expedition is over: first available node, fight, close,
        /// leave the loot, rest at a camp, leave a shop. Stops at the settlement report.
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
                    case GamePhase.Loot:
                        Expedition.LeaveLoot();
                        break;
                    case GamePhase.Camp:
                        Expedition.RestAtCamp();
                        break;
                    case GamePhase.Shop:
                        Expedition.LeaveShop();
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected phase {Expedition.Phase}.");
                }
            }
        }
    }
}
