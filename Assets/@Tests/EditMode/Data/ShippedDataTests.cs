using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the static data that ships: sources convert, generated files are current and load.</summary>
    public sealed class ShippedDataTests
    {
        [Test]
        public void GeneratedJson_IsUpToDateWithSources()
        {
            List<string> stale = DataTransformMenu.FindStaleFiles();

            Assert.IsEmpty(stale, "Stale generated files. Run F1/Data/Transform Static Data: " + string.Join(", ", stale));
        }

        [Test]
        public void GeneratedJson_LoadsThroughTheRuntimeLoader()
        {
            StaticData data = LoadShipped();

            Assert.IsNotEmpty(data.Jobs.Ordered);
            Assert.IsNotEmpty(data.Dungeons.Ordered);
        }

        [Test]
        public void EveryDefinition_HasSourceAndGeneratedFile()
        {
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.SourceDirectory, file.SourceFileName)), file.SourceFileName);
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.GeneratedDirectory, file.GeneratedFileName)), file.GeneratedFileName);
            }
        }

        /// <summary>The first mercenaries of the roster, lined up from row 1 back.</summary>
        static List<PartyMember> FirstParty(StaticData data)
        {
            return data.Mercenaries.Ordered
                .Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index))
                .ToList();
        }

        [Test]
        public void EveryShippedDungeon_ProducesAMapAndBattleSetupsForEveryNode()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    ExpeditionState state = ExpeditionRules.Create(data, dungeon.Id, seed, party);
                    foreach (MapNode node in state.Map.Nodes)
                    {
                        BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, state, node.EnemyGroupId, seed);
                        Assert.DoesNotThrow(() => new BattleEngine(setup), $"{dungeon.Id} {node.EnemyGroupId}");
                    }
                }
            }
        }

        [Test]
        public void ShippedBattles_AlwaysEnd()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                ExpeditionState state = ExpeditionRules.Create(data, group.DungeonId, 1, party);
                var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, group.Id, 1));

                Assert.DoesNotThrow(() => battle.RunToEnd(), group.Id);
                Assert.AreNotEqual(BattleResult.Ongoing, battle.Result);
            }
        }

        [Test]
        public void EveryShippedJob_CanUseItsWeaponInItsRecommendedRow()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.IsTrue(data.Items.Get(job.WeaponItemId).UsableIn(job.RecommendedRow), $"{job.Id}: {job.WeaponItemId} in row {job.RecommendedRow}");
            }
        }

        [Test]
        public void EveryShippedEnemyGroup_ThreatensFromTheStart_AndNobodyInItIsDeadWeight()
        {
            StaticData data = LoadShipped();

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                bool someoneActsAtTheStart = false;
                for (int i = 0; i < group.Enemies.Count; i++)
                {
                    int row = BattleRows.Front + i;
                    EnemyData enemy = data.Enemies.Get(group.Enemies[i]);
                    someoneActsAtTheStart |= enemy.Items.Any(grant => data.Items.Get(grant.ItemId).UsableIn(row));

                    // An enemy only ever moves forward: it must have an item for its row or for one in front of it.
                    Assert.IsTrue(
                        enemy.Items.Any(grant => data.Items.Get(grant.ItemId).Rows.Any(usable => usable <= row)),
                        $"{group.Id}: {enemy.Id} in row {row} can never use any of its items");
                }

                Assert.IsTrue(someoneActsAtTheStart, $"{group.Id}: nobody can use an item where they stand at the start");
            }
        }

        static StaticData LoadShipped()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            return StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
        }
    }
}
