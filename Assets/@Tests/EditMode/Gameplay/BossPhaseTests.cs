using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// A boss's phases (Slice C stage 28, Docs/Design/02_Combat_System.md §9): when its HP falls to a phase's percent its items are replaced by
    /// that phase's, judged where it stands, with every cooldown starting over from that moment; several phases passed at once are entered one
    /// after the other; a party unit never has phases, and the data refuses phases that do not fall.
    /// </summary>
    public sealed class BossPhaseTests
    {
        /// <summary>A boss of 1000 HP that carries one weak attack, then stronger ones at 50% and 25%; the party hits it for a fixed amount every 4 s.</summary>
        static BattleEngine Battle(int partyDamage, out BattleUnitSetup boss)
        {
            BalanceData balance = TestData.Balance();
            boss = TestData.Enemy("boss", 1, 1000, TestData.Attack(5000, 10, "weak"));
            boss.Phases = new List<BattlePhaseSetup>
            {
                new BattlePhaseSetup { HpPercent = 50, Items = new[] { TestData.Attack(3000, 20, "strong") } },
                new BattlePhaseSetup { HpPercent = 25, Items = new[] { TestData.Attack(2000, 30, "furious"), TestData.Attack(4000, 5, "spite") } },
            };
            BattleUnitSetup hero = TestData.Mercenary("hero", 1, 100000, TestData.Attack(4000, partyDamage, "blade"));
            return new BattleEngine(TestData.Setup(balance, TestData.Units(hero), TestData.Units(boss)));
        }

        [Test]
        public void ReachingAPhasesPercent_ReplacesTheItems_AndTheirCooldownsStartOver()
        {
            // 300 every 4 s: 1000 - 300 - 300 = 400 at 8 s (40%): under 50%, the first phase. The second comes at 12 s (100, 10%).
            BattleEngine battle = Battle(300, out BattleUnitSetup boss);
            BattleUnit unit = battle.Enemies[0];
            Assert.AreEqual(0, unit.Phase);
            Assert.AreEqual("weak", unit.Items[0].Equipped.Item.Id);

            battle.AdvanceTo(4000);
            Assert.AreEqual(700, unit.Hp);
            Assert.AreEqual(0, unit.Phase, "70%: no phase yet.");

            battle.AdvanceTo(8000);
            Assert.AreEqual(400, unit.Hp);
            Assert.AreEqual(1, unit.Phase);
            Assert.AreEqual(1, unit.Items.Count);
            Assert.AreEqual("strong", unit.Items[0].Equipped.Item.Id);
            Assert.AreEqual(11000, unit.Items[0].NextFireMs, "A 3 s cooldown from the moment of the phase (8 s).");
            Assert.IsTrue(unit.Items[0].Active);
            BattleEvent changed = battle.Events.Single(e => e.Kind == BattleEventKind.PhaseChanged);
            Assert.AreEqual(8000, changed.TimeMs);
            Assert.AreEqual(unit.Ref, changed.Target);
            Assert.AreEqual(1, changed.A);
            Assert.AreEqual(50, changed.B);
            Assert.AreEqual(400, changed.C);
            Assert.AreSame(boss.Phases[0].Items, unit.EquippedNow);

            // The strong attack fires at 11 s, not at 3 s as a fresh item would have, and before the second phase at 12 s.
            battle.AdvanceTo(10999);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.ItemActivated && e.Id == "strong"));
            battle.AdvanceTo(11000);
            Assert.IsTrue(battle.Events.Any(e => e.Kind == BattleEventKind.ItemActivated && e.Id == "strong" && e.TimeMs == 11000));
            Assert.AreEqual(1, unit.Phase, "Still the first phase.");
        }

        [Test]
        public void TwoPhasesPassedByOneBlow_AreEnteredInOrder_AndTheLastOneCarries()
        {
            // 800 at 4 s: 1000 - 800 = 200 (20%): both the 50% and the 25% phases at once.
            BattleEngine battle = Battle(800, out BattleUnitSetup _);
            BattleUnit unit = battle.Enemies[0];

            battle.AdvanceTo(4000);

            Assert.AreEqual(2, unit.Phase);
            CollectionAssert.AreEqual(new[] { "furious", "spite" }, unit.Items.Select(i => i.Equipped.Item.Id));
            CollectionAssert.AreEqual(new[] { 1, 2 }, battle.Events.Where(e => e.Kind == BattleEventKind.PhaseChanged).Select(e => e.A), "One event per phase, in order.");
            Assert.AreEqual(0, unit.Items[0].SlotIndex);
            Assert.AreEqual(1, unit.Items[1].SlotIndex);
        }

        [Test]
        public void APhase_IsEnteredOnce_AndAReplayGivesTheSameBattle()
        {
            // Phases at 8 s and 12 s, dead at 16 s: 20 s sees it all.
            BattleEngine live = Battle(300, out BattleUnitSetup _);
            live.AdvanceTo(20000);
            Assert.AreEqual(2, live.Events.Count(e => e.Kind == BattleEventKind.PhaseChanged), "50% and 25%, each once.");
            Assert.IsFalse(live.Enemies[0].Alive);

            BattleEngine replayed = BattleEngine.Replay(Battle(300, out BattleUnitSetup _).Setup, new List<BattleInput>(), 20000);
            Assert.AreEqual(BattleLog.Hash(live.Events), BattleLog.Hash(replayed.Events));
        }

        [Test]
        public void TheData_RefusesPhasesThatDoNotFall_OrOutOfRange_OrWithoutItems()
        {
            var items = new List<ItemGrant> { new ItemGrant("claw", 100) };
            var name = TestData.Text("x");
            Assert.DoesNotThrow(() => new EnemyData("x", name, 1, 10, items, phases: new[] { new EnemyPhase(75, items), new EnemyPhase(25, items) }));
            Assert.Throws<DataException>(() => new EnemyData("x", name, 1, 10, items, phases: new[] { new EnemyPhase(25, items), new EnemyPhase(75, items) }));
            Assert.Throws<DataException>(() => new EnemyData("x", name, 1, 10, items, phases: new[] { new EnemyPhase(50, items), new EnemyPhase(50, items) }));
            Assert.Throws<DataException>(() => new EnemyPhase(0, items));
            Assert.Throws<DataException>(() => new EnemyPhase(100, items));
            Assert.Throws<DataException>(() => new EnemyPhase(50, new List<ItemGrant>()));
            Assert.IsEmpty(new EnemyData("x", name, 1, 10, items).Phases);
        }

        [Test]
        public void AnEnemyWithoutPhases_CarriesAnEmptyList_IntoTheBattleSetup_AndAMercenaryNone()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1)));
            run.ClearedDungeons["cave"] = 1;
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "pit");
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "pit_lord", 1);
            Assert.IsNotNull(setup.Enemies[0].Phases, "An enemy without phases carries an empty list, not null.");
            Assert.IsEmpty(setup.Enemies[0].Phases, "The test data's pit lord has none.");
            Assert.IsNull(setup.Party[0].Phases, "A party unit never has phases.");
        }
    }
}
