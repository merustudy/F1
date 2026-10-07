using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Fatigue in battle (Docs/Design/04_Lobby_100Day_Economy.md §3, Slice B stage 15): what tires a mercenary and what
    /// relieves it, the breakdown at the threshold into an affliction or a virtue, what the states do, and the collapse at the
    /// maximum. The states of <see cref="TestData.FatigueStates"/>: fearful (+25% cooldown), hopeless (-50% healing taken),
    /// reckless (+25 death chance); focused (-20% cooldown), stalwart (+50% healing, -15 death chance).
    /// </summary>
    public sealed class FatigueBreakdownTests
    {
        static List<BattleEvent> Of(BattleEngine battle, BattleEventKind kind)
        {
            return battle.Events.Where(e => e.Kind == kind).ToList();
        }

        /// <summary>The test balance with the battle's fatigue events turned on (TestData leaves them at 0): hit 2, death's door 10, an ally's 5, an ally's death 15, a kill -2.</summary>
        static BalanceData Balance(params (string Key, int Value)[] overrides)
        {
            var values = new List<(string, int)> { ("FatigueOnHit", 2), ("FatigueOnDog", 10), ("FatigueOnAllyDog", 5), ("FatigueOnAllyDeath", 15), ("FatigueOnKill", 2) };
            values.AddRange(overrides);
            return TestData.Balance(values.ToArray());
        }

        static FatigueStateData State(string id)
        {
            return TestData.FatigueStates().Single(s => s.Id == id);
        }

        /// <summary>A battle whose only blow is the enemy's attack of this damage every second from 1000 ms; the party has no items unless given.</summary>
        static BattleEngine HitEverySecond(BalanceData balance, int damage, params BattleUnitSetup[] party)
        {
            return new BattleEngine(TestData.Setup(balance, party, TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, damage)))));
        }

        // ---- What tires and what relieves ----------------------------------------------------

        [Test]
        public void AHitThatGetsThrough_Tires_AShieldedOneDoesNot()
        {
            BalanceData balance = Balance();
            BattleEngine battle = HitEverySecond(balance, 8,
                TestData.Mercenary("a", 1, 100).WithPassive(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self, 20));

            battle.AdvanceTo(2000);
            Assert.AreEqual(0, battle.Party[0].Fatigue, "The shield took the first two blows whole.");
            Assert.IsEmpty(Of(battle, BattleEventKind.FatigueChanged));

            battle.AdvanceTo(4000);
            Assert.AreEqual(2 * balance.FatigueOnHit, battle.Party[0].Fatigue, "The third blow got through by 4 and the fourth whole.");
            BattleEvent first = Of(battle, BattleEventKind.FatigueChanged)[0];
            Assert.AreEqual("hit", first.Id);
            Assert.AreEqual(balance.FatigueOnHit, first.A);
            Assert.AreEqual(balance.FatigueOnHit, first.C);
        }

        [Test]
        public void ABurnTick_IsAHitThatTiresNobody_AStormTickTires()
        {
            // The enemy's hex burns 3 at 2500 ms (and again at 5000); the ticks at 3000, 4000, 5000 deal 3, 2, 1.
            // The storm starts at 6000 and deals 1 a tick. 2026-10-07 decision: a burn tick raises no fatigue, a storm tick does.
            BalanceData balance = Balance(("StormStartMs", 6000), ("StormTickMs", 1000), ("StormBaseDamage", 1), ("StormGrowth", 0));
            var hex = new EquippedItem(TestData.Item("hex", 2500, EffectKind.Burn, TargetMode.EnemyFront, 100, ItemCategory.Support), 3);
            var battle = new BattleEngine(TestData.Setup(balance, TestData.Units(TestData.Mercenary("a", 1, 100)), TestData.Units(TestData.Enemy("e", 1, 1000, hex))));

            battle.AdvanceTo(4500);
            Assert.AreEqual(95, battle.Party[0].Hp, "Two burn ticks got through.");
            Assert.AreEqual(2, Of(battle, BattleEventKind.Damaged).Count(e => e.Id == BattleEvent.CauseBurn));
            Assert.AreEqual(0, battle.Party[0].Fatigue, "A burn tick tires nobody.");
            Assert.IsEmpty(Of(battle, BattleEventKind.FatigueChanged));

            battle.AdvanceTo(6000);
            Assert.AreEqual(90, battle.Party[0].Hp, "The ticks at 5000 (1) and 6000 (3: the hex burned again at 5000), then the first storm tick (1).");
            Assert.AreEqual(BattleEvent.CauseStorm, Of(battle, BattleEventKind.Damaged).Last().Id);
            Assert.AreEqual(balance.FatigueOnHit, battle.Party[0].Fatigue, "The storm tick is a hit that tires.");
            CollectionAssert.AreEqual(new[] { "hit" }, Of(battle, BattleEventKind.FatigueChanged).Select(e => e.Id));
        }

        [Test]
        public void DeathsDoor_TiresTheOneWhoReachesIt_AndItsAllies_AndADeathTiresTheAlliesMore()
        {
            BalanceData balance = Balance(("DogDeathChancePercent", 100), ("DogGraceBreakHits", 1));
            BattleEngine battle = HitEverySecond(balance, 50, TestData.Mercenary("a", 1, 50), TestData.Mercenary("b", 2, 100));

            // 1000 ms: a falls to 0 and is at death's door.
            battle.AdvanceTo(1000);
            Assert.IsTrue(battle.Party[0].InDog);
            Assert.AreEqual(balance.FatigueOnHit + balance.FatigueOnDog, battle.Party[0].Fatigue);
            Assert.AreEqual(balance.FatigueOnAllyDog, battle.Party[1].Fatigue);

            // 2000 ms: the next hit kills a (the grace breaks on the first hit, the roll is certain).
            battle.AdvanceTo(2000);
            Assert.IsFalse(battle.Party[0].Alive);
            Assert.AreEqual(balance.FatigueOnAllyDog + balance.FatigueOnAllyDeath, battle.Party[1].Fatigue);
            CollectionAssert.AreEqual(new[] { "hit", "dog", "ally_dog", "ally_death" }, Of(battle, BattleEventKind.FatigueChanged).Select(e => e.Id));
        }

        [Test]
        public void KillingAnEnemy_RelievesTheKiller_ButNotBelowZero()
        {
            BalanceData balance = Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 50)).WithFatigue(3)),
                TestData.Units(TestData.Enemy("e", 1, 50), TestData.Enemy("f", 2, 1000))));

            battle.AdvanceTo(1000);
            Assert.IsFalse(battle.Enemies[0].Alive);
            Assert.AreEqual(3 - balance.FatigueOnKill, battle.Party[0].Fatigue);
            Assert.AreEqual("kill", Of(battle, BattleEventKind.FatigueChanged).Single().Id);

            // At 0 already: the relief has nothing to take and logs nothing.
            BattleEngine fresh = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 50))),
                TestData.Units(TestData.Enemy("e", 1, 50), TestData.Enemy("f", 2, 1000))));
            fresh.AdvanceTo(1000);
            Assert.AreEqual(0, fresh.Party[0].Fatigue);
            Assert.IsEmpty(Of(fresh, BattleEventKind.FatigueChanged));
        }

        [Test]
        public void Enemies_NeverTire()
        {
            BalanceData balance = Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 10))),
                TestData.Units(TestData.Enemy("e", 1, 100))));

            battle.AdvanceTo(3000);

            Assert.AreEqual(0, battle.Enemies[0].Fatigue);
            Assert.IsNull(battle.Enemies[0].State);
            Assert.IsTrue(Of(battle, BattleEventKind.FatigueChanged).All(e => e.Target.Side == BattleSide.Party));
        }

        // ---- The breakdown -------------------------------------------------------------------

        [Test]
        public void ReachingTheThreshold_BreaksDown_IntoAnAffliction_OnceOnly()
        {
            BalanceData balance = Balance(("VirtueChancePercent", 0), ("FatigueOnHit", 10));
            BattleEngine battle = HitEverySecond(balance, 1, TestData.Mercenary("a", 1, 1000).WithFatigue(95));

            battle.AdvanceTo(1000);

            BattleUnit a = battle.Party[0];
            Assert.AreEqual(105, a.Fatigue);
            Assert.IsNotNull(a.State);
            Assert.AreEqual(FatigueStateKind.Affliction, a.State.Kind);
            BattleEvent broke = Of(battle, BattleEventKind.BrokeDown).Single();
            Assert.AreEqual(a.State.Id, broke.Id);
            Assert.AreEqual(0, broke.B, "The virtue chance.");
            Assert.AreEqual(0, broke.C, "An affliction.");
            Assert.That(broke.A, Is.InRange(0, 99));

            // More fatigue over the threshold rolls no second time.
            battle.AdvanceTo(3000);
            Assert.AreEqual(125, a.Fatigue);
            Assert.AreEqual(1, Of(battle, BattleEventKind.BrokeDown).Count);
        }

        [Test]
        public void ReachingTheThreshold_CanBreakDownIntoAVirtue_WhichBringsTheFatigueDown()
        {
            BalanceData balance = Balance(("VirtueChancePercent", 100), ("FatigueOnHit", 10));
            BattleEngine battle = HitEverySecond(balance, 1, TestData.Mercenary("a", 1, 1000).WithFatigue(95));

            battle.AdvanceTo(1000);

            BattleUnit a = battle.Party[0];
            Assert.AreEqual(FatigueStateKind.Virtue, a.State.Kind);
            Assert.AreEqual(balance.VirtueFatigue, a.Fatigue);
            BattleEvent broke = Of(battle, BattleEventKind.BrokeDown).Single();
            Assert.AreEqual(1, broke.C);
            BattleEvent down = Of(battle, BattleEventKind.FatigueChanged).Last();
            Assert.AreEqual("virtue", down.Id);
            Assert.AreEqual(balance.VirtueFatigue - 105, down.A);
            Assert.AreEqual(balance.VirtueFatigue, down.C);

            // Virtuous, the unit climbs past the threshold again without another breakdown.
            battle.AdvanceTo(9000);
            Assert.Greater(a.Fatigue, balance.FatigueBreakdown);
            Assert.AreEqual(1, Of(battle, BattleEventKind.BrokeDown).Count);
            Assert.AreEqual(FatigueStateKind.Virtue, a.State.Kind, "A virtue lasts.");
        }

        [Test]
        public void TheBreakdown_IsDeterministic_AndDrawsFromItsOwnStream_SoDeathRollsStayTheSame()
        {
            BalanceData balance = Balance(("FatigueOnHit", 10));
            BattleEngine first = HitEverySecond(balance, 1, TestData.Mercenary("a", 1, 1000).WithFatigue(95));
            BattleEngine second = HitEverySecond(balance, 1, TestData.Mercenary("a", 1, 1000).WithFatigue(95));
            first.RunToEnd();
            second.RunToEnd();

            Assert.AreEqual(BattleLog.Hash(first.Events), BattleLog.Hash(second.Events));
            Assert.AreEqual(first.Party[0].State.Id, second.Party[0].State.Id);

            // A battle that breaks down and one that does not, same seed: the same death roll.
            BalanceData deadly = Balance(("DogDeathChancePercent", 50), ("DogGraceBreakHits", 1));
            BattleEngine withBreakdown = HitEverySecond(deadly, 50, TestData.Mercenary("a", 1, 50).WithFatigue(95), TestData.Mercenary("b", 2, 1000));
            BattleEngine without = HitEverySecond(deadly, 50, TestData.Mercenary("a", 1, 50), TestData.Mercenary("b", 2, 1000));
            withBreakdown.AdvanceTo(2000);
            without.AdvanceTo(2000);
            Assert.IsNotEmpty(Of(withBreakdown, BattleEventKind.BrokeDown));
            Assert.AreEqual(Of(without, BattleEventKind.DeathRolled).First().B, Of(withBreakdown, BattleEventKind.DeathRolled).First().B);
        }

        [Test]
        public void FatigueBroughtIn_IsJudgedAtTheStart_AndAStateBroughtInSetsTheFirstCooldown()
        {
            BalanceData balance = Balance(("VirtueChancePercent", 0));
            BattleEngine atThreshold = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100).WithFatigue(100)),
                TestData.Units(TestData.Enemy("e", 1, 100))));
            Assert.IsNotNull(atThreshold.Party[0].State, "Breaks down as the battle starts.");
            Assert.AreEqual(BattleEventKind.BattleStarted, atThreshold.Events[0].Kind);
            Assert.AreEqual(BattleEventKind.BrokeDown, atThreshold.Events[1].Kind);

            BattleEngine afflicted = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100).WithFatigue(120, State("fearful"))),
                TestData.Units(TestData.Enemy("e", 1, 100))));
            Assert.IsEmpty(Of(afflicted, BattleEventKind.BrokeDown), "Already in a state: no new breakdown.");

            BattleEngine fearful = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 10)).WithFatigue(120, State("fearful"))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));
            Assert.AreEqual(1250, fearful.Party[0].Items[0].NextFireMs, "A quarter slower from the first swing.");
        }

        // ---- What the states do ----------------------------------------------------------------

        [Test]
        public void AnAffliction_SlowsTheItems_OrHalvesTheHealing_OrMakesDeathsDoorDeadlier()
        {
            BalanceData balance = Balance(("DogDeathChancePercent", 30), ("DogGraceBreakHits", 1));

            BattleEngine fearful = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 1)).WithFatigue(120, State("fearful"))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));
            fearful.AdvanceTo(2500);
            Assert.AreEqual(2, Of(fearful, BattleEventKind.ItemActivated).Count, "At 1250 and 2500, not 1000, 2000.");

            ItemData staff = TestData.Item("staff", 1000, EffectKind.Heal, TargetMode.AllyLowestHp, 100, ItemCategory.Support);
            BattleEngine hopeless = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 50, new EquippedItem(staff, 20)).WithMaxHp(100).WithFatigue(120, State("hopeless"))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));
            hopeless.AdvanceTo(1000);
            BattleEvent healed = Of(hopeless, BattleEventKind.Healed).Single();
            Assert.AreEqual(10, healed.A, "Half of 20.");
            Assert.AreEqual(60, hopeless.Party[0].Hp);

            BattleEngine reckless = HitEverySecond(balance, 50, TestData.Mercenary("a", 1, 50).WithFatigue(120, State("reckless")), TestData.Mercenary("b", 2, 1000));
            reckless.AdvanceTo(2000);
            Assert.AreEqual(55, Of(reckless, BattleEventKind.DeathRolled).First().A, "30 + 25.");
        }

        [Test]
        public void AVirtue_QuickensTheItems_OrHelpsTheHealing_AndDeathsDoor_WithinBounds()
        {
            BalanceData balance = Balance(("DogDeathChancePercent", 10), ("DogGraceBreakHits", 1));

            BattleEngine focused = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 1)).WithFatigue(40, State("focused"))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));
            focused.AdvanceTo(1600);
            Assert.AreEqual(2, Of(focused, BattleEventKind.ItemActivated).Count, "At 800 and 1600.");

            ItemData staff = TestData.Item("staff", 1000, EffectKind.Heal, TargetMode.AllyLowestHp, 100, ItemCategory.Support);
            BattleEngine stalwart = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 50, new EquippedItem(staff, 20)).WithMaxHp(100).WithFatigue(40, State("stalwart"))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));
            stalwart.AdvanceTo(1000);
            Assert.AreEqual(30, Of(stalwart, BattleEventKind.Healed).Single().A, "20 and half again.");

            BattleEngine door = HitEverySecond(balance, 50, TestData.Mercenary("a", 1, 50).WithFatigue(40, State("stalwart")), TestData.Mercenary("b", 2, 1000));
            door.AdvanceTo(2000);
            Assert.AreEqual(0, Of(door, BattleEventKind.DeathRolled).First().A, "10 - 15, not below 0.");
        }

        [Test]
        public void AnAffliction_EndsWhenTheFatigueComesBackUnderTheThreshold()
        {
            BalanceData balance = Balance(("FatigueOnKill", 5));
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 50)).WithFatigue(102, State("fearful"))),
                TestData.Units(TestData.Enemy("e", 1, 10), TestData.Enemy("f", 2, 1000))));

            battle.AdvanceTo(1250);

            Assert.AreEqual(97, battle.Party[0].Fatigue);
            Assert.IsNull(battle.Party[0].State);
            Assert.AreEqual("fearful", Of(battle, BattleEventKind.FatigueStateEnded).Single().Id);
        }

        // ---- The collapse ----------------------------------------------------------------------

        [Test]
        public void ReachingTheMaximum_SendsTheUnitToDeathsDoor_AndKillsItIfItIsThereAlready()
        {
            BalanceData balance = Balance(("FatigueOnHit", 10), ("DogGraceBreakHits", 5), ("DogDeathChancePercent", 0));
            BattleEngine battle = HitEverySecond(balance, 1, TestData.Mercenary("a", 1, 1000).WithFatigue(195, State("fearful")), TestData.Mercenary("b", 2, 1000));

            battle.AdvanceTo(1000);

            BattleUnit a = battle.Party[0];
            Assert.AreEqual(balance.MaxFatigue, a.Fatigue);
            Assert.IsTrue(a.InDog);
            Assert.AreEqual(0, a.Hp);
            Assert.IsTrue(a.Alive);
            BattleEvent collapsed = Of(battle, BattleEventKind.Collapsed).Single();
            Assert.AreEqual(0, collapsed.C, "To death's door.");
            Assert.AreEqual(balance.FatigueOnAllyDog, battle.Party[1].Fatigue, "The collapse tires the ally like any death's door.");

            // At death's door, the next hit is counted (grace) but tires the unit, which stands at the maximum: it collapses again and dies.
            battle.AdvanceTo(2000);
            Assert.IsFalse(a.Alive);
            Assert.AreEqual(2, Of(battle, BattleEventKind.Collapsed).Count);
            Assert.AreEqual(1, Of(battle, BattleEventKind.Collapsed)[1].C);
        }

        [Test]
        public void StartingAtTheMaximum_CollapsesAsTheBattleStarts()
        {
            BalanceData balance = Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100).WithFatigue(200), TestData.Mercenary("b", 2, 100).WithFatigue(50)),
                TestData.Units(TestData.Enemy("e", 1, 100))));

            Assert.IsTrue(battle.Party[0].InDog);
            Assert.AreEqual(0, battle.Party[0].Hp);
            Assert.AreEqual(50 + balance.FatigueOnAllyDog, battle.Party[1].Fatigue);
            Assert.AreEqual(BattleResult.Ongoing, battle.Result, "At death's door, not dead.");
        }

        // ---- Between battles -------------------------------------------------------------------

        [Test]
        public void TheBattle_WritesFatigueAndStateBackToTheMember_AndTheNextSetupCarriesThem()
        {
            StaticData data = TestData.Data(("VirtueChancePercent", 0), ("FatigueBattleEntry", 0));
            var party = new[] { new PartyMember("anna", "tank", 1, 100, "fearful"), new PartyMember("ben", "healer", 2, 0), new PartyMember("cora", "striker", 3, 0) };
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, party);
            Assert.AreEqual("fearful", state.Members[0].StateId, "The affliction leaves with her.");

            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, state, "pair", 7);
            Assert.AreEqual(100, setup.Party[0].Fatigue);
            Assert.AreEqual("fearful", setup.Party[0].FatigueState.Id);
            Assert.IsNull(setup.Party[1].FatigueState);
            Assert.AreEqual(data.FatigueStates.Count, setup.FatigueStates.Count);

            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            battle.RunToEnd();
            ExpeditionRules.CompleteBattle(data, state, battle);
            for (int i = 0; i < state.Members.Count; i++)
            {
                Assert.AreEqual(battle.Party[i].Fatigue, state.Members[i].Fatigue);
                Assert.AreEqual(battle.Party[i].State?.Id, state.Members[i].StateId);
            }
        }

        [Test]
        public void TheCampsRest_EndsAnAffliction_OnceTheFatigueIsUnderTheThreshold()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData> { new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }, campFloor: 2) };
            var data = new StaticData(parts);
            var party = new[] { new PartyMember("anna", "tank", 1, 110, "fearful"), new PartyMember("ben", "healer", 2, 150, "hopeless") };
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, party);
            state.CurrentNodeId = state.Map.OnFloor(1)[0].Id;
            ExpeditionRules.EnterCamp(state, ExpeditionRules.AvailableNodes(state)[0].Id);

            ExpeditionRules.RestAtCamp(data, state);

            Assert.AreEqual(90, state.Members[0].Fatigue);
            Assert.IsNull(state.Members[0].StateId, "Under the threshold: the affliction ended.");
            Assert.AreEqual(130, state.Members[1].Fatigue);
            Assert.AreEqual("hopeless", state.Members[1].StateId, "Still over it.");
        }

        [Test]
        public void TheSettlement_KeepsAnAfflictionAtHome_DropsAVirtue_AndDaysOffEndTheAffliction()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);
            MercenaryState anna = run.Roster.First(m => m.Id == "anna");
            MercenaryState ben = run.Roster.First(m => m.Id == "ben");
            anna.Fatigue = 120;
            anna.AfflictionId = "fearful";
            RunRules.PlaceInParty(data, run, "anna", 1);
            RunRules.PlaceInParty(data, run, "ben", 2);
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            Assert.AreEqual("fearful", expedition.Members[0].StateId);

            expedition.Members[0].Fatigue = 130;
            expedition.Members[1].Fatigue = 40;
            expedition.Members[1].StateId = "focused";
            expedition.Result = ExpeditionResult.Retreated;
            expedition.Phase = ExpeditionPhase.Finished;
            SettlementReport report = RunRules.Settle(data, run, expedition);

            Assert.AreEqual("fearful", anna.AfflictionId, "An affliction comes home.");
            Assert.AreEqual(130, anna.Fatigue);
            Assert.IsNull(ben.AfflictionId, "A virtue ends with the expedition.");
            CollectionAssert.AreEqual(new[] { "fearful", null }, report.SurvivorStates, "The report says who came home afflicted, in the survivors' order.");

            // Days off bring the fatigue under the threshold: the affliction ends.
            while (anna.Fatigue >= data.Balance.FatigueBreakdown)
            {
                RunRules.Rest(data, run);
            }

            Assert.IsNull(anna.AfflictionId);
        }

        // ---- Data ------------------------------------------------------------------------------

        [Test]
        public void FatigueStateData_RefusesEmptyOrOutOfRangeChanges_AndTheSetNeedsBothKinds()
        {
            LocalizedText t = TestData.Text("x");
            Assert.Throws<DataException>(() => new FatigueStateData("none", t, FatigueStateKind.Affliction, 0, 0, 0, t), "Changes nothing.");
            Assert.Throws<DataException>(() => new FatigueStateData("slow", t, FatigueStateKind.Affliction, 101, 0, 0, t));
            Assert.Throws<DataException>(() => new FatigueStateData("fast", t, FatigueStateKind.Virtue, -51, 0, 0, t));
            Assert.Throws<DataException>(() => new FatigueStateData("heal", t, FatigueStateKind.Virtue, 0, 201, 0, t));
            Assert.Throws<DataException>(() => new FatigueStateData("door", t, FatigueStateKind.Affliction, 0, 0, 101, t));
            Assert.DoesNotThrow(() => new FatigueStateData("ok", t, FatigueStateKind.Affliction, 0, -100, 100, t));

            StaticDataParts parts = TestData.Parts();
            parts.FatigueStates = TestData.FatigueStates().Where(s => s.Kind == FatigueStateKind.Affliction).ToList();
            Assert.Throws<DataValidationException>(() => new StaticData(parts), "No virtue to break down into.");
            Assert.Throws<DataException>(() => Balance(("VirtueFatigue", 100)), "A virtue at the threshold would break down again at once.");
        }
    }
}
