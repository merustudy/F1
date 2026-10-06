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
        public void EveryShippedDungeon_ProducesAMapAndBattleSetupsForEveryFoughtNode()
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
                        if (!node.IsFought)
                        {
                            Assert.IsNull(node.EnemyGroupId, $"{dungeon.Id}: nobody waits at camp {node.Id}");
                            continue;
                        }

                        BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, state, node.EnemyGroupId, seed, node.Floor);
                        Assert.DoesNotThrow(() => new BattleEngine(setup), $"{dungeon.Id} {node.EnemyGroupId} on floor {node.Floor}");
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
        public void EveryShippedJob_CanUseItsWeaponInItsRecommendedRow_OfAFullParty()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.IsTrue(
                    data.Items.Get(job.WeaponItemId).UsableIn(job.RecommendedRow, data.Balance.PartySize),
                    $"{job.Id}: {job.WeaponItemId} in row {job.RecommendedRow} of {data.Balance.PartySize}");
            }
        }

        [Test]
        public void EveryShippedEnemyGroup_ThreatensFromTheStart()
        {
            // Where an item works is counted on the living line, so nobody stays dead weight for good: the
            // line only shortens. What matters is that the group acts before anyone has died.
            StaticData data = LoadShipped();

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                bool someoneActsAtTheStart = false;
                for (int i = 0; i < group.Enemies.Count; i++)
                {
                    int row = BattleRows.Front + i;
                    EnemyData enemy = data.Enemies.Get(group.Enemies[i]);
                    someoneActsAtTheStart |= enemy.Items.Any(grant => data.Items.Get(grant.ItemId).UsableIn(row, group.Enemies.Count));
                }

                Assert.IsTrue(someoneActsAtTheStart, $"{group.Id}: nobody can use an item where they stand at the start");
            }
        }

        /// <summary>
        /// The items are classed as the design says (Docs/Design/02_Combat_System.md §4, 2026-10-04): every job's own weapon is a
        /// weapon (the bishop's healing staff too), the buckler is defensive gear, the flask and the spit are attacks that are not
        /// weapons, and the charm, the pouch and the chant are support items.
        /// </summary>
        [Test]
        public void ShippedItems_AreClassedAsDecided()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.AreEqual(ItemCategory.Weapon, data.Items.Get(job.WeaponItemId).Category, job.Id);
            }

            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("healing_staff").Category);
            Assert.AreEqual(ItemCategory.Armor, data.Items.Get("buckler").Category);
            Assert.AreEqual(ItemCategory.Attack, data.Items.Get("ember_flask").Category);
            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("lantern_staff").Category);
            foreach (string support in new[] { "ward_charm", "herb_pouch", "mending_chant" })
            {
                Assert.AreEqual(ItemCategory.Support, data.Items.Get(support).Category, support);
            }
        }

        static StaticData LoadShipped()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            return StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
        }
    }
}
