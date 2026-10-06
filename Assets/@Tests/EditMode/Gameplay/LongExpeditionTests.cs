using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The long expedition of Docs/Design/03_Dungeon_Structure.md §1 (Slice B): elite and camp nodes on the map, resting at a
    /// camp, and the enemies of the deeper floors.
    /// </summary>
    public sealed class LongExpeditionTests
    {
        static readonly PartyMember[] Party =
        {
            new PartyMember("anna", "tank", 1, 50),
            new PartyMember("ben", "healer", 2, 10),
            new PartyMember("cora", "striker", 3, 0),
        };

        /// <summary>
        /// The test data with an eight-floor cave: elites and camps by chance from floor 3, every node of floor 8 a camp, the
        /// boss on floor 9. Its groups: "pair" on floors 1..8, an elite "guard" on floors 3..8, the boss "lair".
        /// </summary>
        static StaticData DeepCave(int elitePercent = 30, int campPercent = 30, int hpPerFloor = 0, int gradePerFloor = 0, bool eliteGroups = true)
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 8, 2, 4, 3, 8, 1, new List<string> { "tonic" }, null,
                    eliteMinFloor: 3, eliteChancePercent: elitePercent, campMinFloor: 3, campChancePercent: campPercent, campFloor: 8,
                    enemyHpPerFloorPercent: hpPerFloor, enemyGradePerFloor: gradePerFloor),
            };
            var groups = new List<EnemyGroupData>
            {
                new EnemyGroupData("pair", "cave", 1, 8, false, new[] { "grunt", "grunt" }),
                new EnemyGroupData("lair", "cave", 0, 0, true, new[] { "chief", "grunt" }),
            };
            if (eliteGroups)
            {
                groups.Add(new EnemyGroupData("guard", "cave", 3, 8, false, new[] { "chief", "chief" }, isElite: true));
            }

            parts.EnemyGroups = groups;
            return new StaticData(parts);
        }

        // ---- The map -------------------------------------------------------------------------

        [Test]
        public void Map_FirstFloorIsBattles_TheCampFloorIsCamps_ElitesAndCampsElsewhereByChance()
        {
            StaticData data = DeepCave();
            DungeonData dungeon = data.Dungeons.Get("cave");
            int elites = 0, camps = 0;

            for (ulong seed = 1; seed <= 300; seed++)
            {
                NodeMap map = MapGenerator.Generate(data, dungeon, seed);
                Assert.AreEqual(9, map.FloorCount);
                Assert.IsTrue(map.OnFloor(1).All(n => n.Kind == MapNodeKind.Battle), "The first floor is battles only.");
                Assert.IsTrue(map.OnFloor(8).All(n => n.Kind == MapNodeKind.Camp && n.EnemyGroupId == null), "The camp floor is all camps.");
                Assert.IsTrue(map.OnFloor(7).All(n => n.Kind != MapNodeKind.Camp), "No camp just before the camp floor.");
                Assert.IsTrue(map.OnFloor(2).All(n => n.Kind == MapNodeKind.Battle), "Nothing special before floor 3.");
                foreach (MapNode node in map.Nodes.Where(n => n.Floor <= 8))
                {
                    switch (node.Kind)
                    {
                        case MapNodeKind.Elite:
                            elites++;
                            Assert.AreEqual("guard", node.EnemyGroupId, "An elite node holds an elite group.");
                            break;
                        case MapNodeKind.Battle:
                            Assert.AreEqual("pair", node.EnemyGroupId, "A battle node never holds an elite group.");
                            break;
                        case MapNodeKind.Camp:
                            camps += node.Floor < 8 ? 1 : 0;
                            Assert.IsNull(node.EnemyGroupId);
                            break;
                        default:
                            Assert.Fail($"{node.Kind} on floor {node.Floor}.");
                            break;
                    }
                }

                Assert.AreEqual(MapNodeKind.Boss, map.OnFloor(9).Single().Kind);
            }

            Assert.Greater(elites, 0, "Elites appear by chance.");
            Assert.Greater(camps, 0, "Camps appear by chance before the camp floor.");
        }

        [Test]
        public void Data_RefusesAnEliteFloorWithoutAnEliteGroup_AndAWrongCampFloor_AndABossThatIsAnElite()
        {
            Assert.Throws<DataValidationException>(() => DeepCave(eliteGroups: false));
            Assert.DoesNotThrow(() => DeepCave(elitePercent: 0, eliteGroups: false), "No elite can stand anywhere: no elite group is needed.");

            List<string> potions = new List<string>();
            Assert.Throws<DataException>(() => new DungeonData("d", TestData.Text("d"), "swift", 5, 2, 3, 2, 8, 2, potions, null, campFloor: 1));
            Assert.Throws<DataException>(() => new DungeonData("d", TestData.Text("d"), "swift", 5, 2, 3, 2, 8, 2, potions, null, campFloor: 6));
            Assert.Throws<DataException>(() => new DungeonData("d", TestData.Text("d"), "swift", 5, 2, 3, 2, 8, 2, potions, null, eliteChancePercent: 101));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 0, 0, true, new[] { "grunt" }, isElite: true));
        }

        // ---- The deeper floors ---------------------------------------------------------------

        [Test]
        public void TheEnemiesOfADeeperFloor_HaveMoreHpAndBetterItems_ButTheBossIsAsItsDataSays()
        {
            StaticData data = DeepCave(hpPerFloor: 10, gradePerFloor: 2);
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);
            EnemyData grunt = data.Enemies.Get("grunt");

            BattleSetup first = ExpeditionRules.BuildBattleSetup(data, state, "pair", 1, floor: 1);
            BattleSetup fifth = ExpeditionRules.BuildBattleSetup(data, state, "pair", 1, floor: 5);
            BattleSetup boss = ExpeditionRules.BuildBattleSetup(data, state, "lair", 1, floor: 9);

            Assert.AreEqual(grunt.MaxHp, first.Enemies[0].MaxHp);
            Assert.AreEqual(grunt.MaxHp * 140 / 100, fifth.Enemies[0].MaxHp, "Four floors deeper: +40%.");
            Assert.AreEqual(fifth.Enemies[0].MaxHp, fifth.Enemies[0].Hp);
            Assert.AreEqual(grunt.Items[0].Grade + 8, fifth.Enemies[0].Items[0].Grade, "Four floors deeper: +8 grades.");
            Assert.AreEqual(data.Enemies.Get("chief").MaxHp, boss.Enemies[0].MaxHp, "The boss is not scaled.");
            Assert.AreEqual(grunt.MaxHp, boss.Enemies[1].MaxHp, "Nor are its guards.");
        }

        // ---- Camps ---------------------------------------------------------------------------

        /// <summary>An expedition of the deep cave whose next choice is a camp node: the first floor's node won, then floors walked until a camp is offered.</summary>
        static ExpeditionState AtACampNode(StaticData data, out MapNode camp)
        {
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 3, Party);
            for (int guard = 0; guard < 20; guard++)
            {
                List<MapNode> next = ExpeditionRules.AvailableNodes(state);
                camp = next.FirstOrDefault(n => n.Kind == MapNodeKind.Camp);
                if (camp != null)
                {
                    return state;
                }

                // Walk on without fighting: a test moves the party along the map directly.
                MapNode step = next[0];
                state.CurrentNodeId = step.Id;
            }

            throw new InvalidOperationException("No camp was reached.");
        }

        [Test]
        public void ACamp_IsEntered_NotFought_AndTheBoardsAndRowsCanBeRearrangedThere()
        {
            StaticData data = DeepCave();
            ExpeditionState state = AtACampNode(data, out MapNode camp);

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BeginBattle(data, state, camp.Id), "Nobody is fought at a camp.");
            ExpeditionRules.EnterCamp(state, camp.Id);

            Assert.AreEqual(ExpeditionPhase.AtCamp, state.Phase);
            Assert.AreEqual(camp.Id, state.CurrentNodeId);
            Assert.IsEmpty(ExpeditionRules.AvailableNodes(state), "The party stays until it chooses what to do.");
            Assert.IsTrue(ExpeditionRules.CanMoveToRow(state, 0, 2), "Rows can be changed at a camp.");
            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 0), "So can the boards.");
        }

        [Test]
        public void RestingAtACamp_GivesTheLivingHpAndTakesFatigueOff_ThenThePartyGoesOn()
        {
            StaticData data = DeepCave();
            ExpeditionState state = AtACampNode(data, out MapNode camp);
            state.Members[0].Hp = 10;           // max 100: +30
            state.Members[1].Hp = 55;           // max 60: back to 60
            state.Members[2].Alive = false;
            state.Members[2].Hp = 0;
            ExpeditionRules.EnterCamp(state, camp.Id);

            ExpeditionRules.RestAtCamp(data, state);

            Assert.AreEqual(40, state.Members[0].Hp, "30% of 100.");
            Assert.AreEqual(60, state.Members[1].Hp, "Not above the maximum.");
            Assert.AreEqual(0, state.Members[2].Hp, "The dead do not rest.");
            Assert.AreEqual(30, state.Members[0].Fatigue, "50 - 20.");
            Assert.AreEqual(0, state.Members[1].Fatigue, "10 - 20, not below 0.");
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            CollectionAssert.AreEqual(camp.NextNodeIds, ExpeditionRules.AvailableNodes(state).Select(n => n.Id), "On from the camp.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RestAtCamp(data, state), "Once per camp.");
        }

        [Test]
        public void OnlyACampNode_CanBeCampedAt()
        {
            StaticData data = DeepCave();
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 3, Party);
            MapNode battle = ExpeditionRules.AvailableNodes(state)[0];

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.EnterCamp(state, battle.Id));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RestAtCamp(data, state), "Not at a camp.");
        }
    }
}
