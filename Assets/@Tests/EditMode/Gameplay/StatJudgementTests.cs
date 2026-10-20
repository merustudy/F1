using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The stats' rolls (Slice D stage 33; 2026-10-20 스탯 개정: Docs/Design/02_Combat_System.md §4·§5·§6, 04_Lobby_100Day_Economy.md §3): the hit (dexterity;
    /// a magic blow never misses) and the dodge, the miss that deals and triggers nothing, the critical chance with dexterity or wisdom by the weapon's school,
    /// the death chance and the virtue with endurance, and the fatigue a mercenary gains with endurance. Chances are in ten-thousandths. One rule per test.
    /// </summary>
    public sealed class StatJudgementTests
    {
        /// <summary>The shipped bounds: 80% against a target at the baseline, within 5..95%.</summary>
        static BalanceData Shipped(params (string Key, int Value)[] more)
        {
            var overrides = new List<(string, int)> { ("HitBasePercent", 80), ("HitMaxPercent", 95) };
            overrides.AddRange(more);
            return TestData.Balance(overrides.ToArray());
        }

        static StatBlock With(Stat stat, int value)
        {
            return StatBlock.Uniform(10).With(stat, value);
        }

        static BattleUnitSetup Merc(string id, int row, int hp, StatBlock stats, params EquippedItem[] items)
        {
            BattleUnitSetup unit = TestData.Mercenary(id, row, hp, items);
            unit.Stats = stats;
            return unit;
        }

        static EquippedItem Blade(int cooldownMs = 1000, TargetMode target = TargetMode.EnemyFront, int value = 100)
        {
            return new EquippedItem(TestData.Item("blade", cooldownMs, EffectKind.Damage, target, value));
        }

        static BattleEngine Battle(BalanceData balance, BattleUnitSetup[] party, BattleUnitSetup[] enemies, int untilMs)
        {
            var battle = new BattleEngine(TestData.Setup(balance, party, enemies));
            battle.AdvanceTo(untilMs);
            return battle;
        }

        [Test]
        public void HitChance_IsTheBase_WithTheAttackersDexterity_LessTheTargetsDodge_WithinTheBounds()
        {
            BalanceData balance = Shipped();
            Assert.AreEqual(200, StatRules.Accuracy(balance, With(Stat.Dex, 12), 0), "Dexterity 12: 2 points (2026-10-20: whatever the weapon's axis).");
            Assert.AreEqual(0, StatRules.Accuracy(balance, With(Stat.Str, 14), 0), "Strength moves no hit.");
            Assert.AreEqual(-300, StatRules.Accuracy(balance, With(Stat.Dex, 7), 0));
            Assert.AreEqual(1000, StatRules.Accuracy(balance, null, 10), "An enemy's Accuracy 10.");
            Assert.AreEqual(100, StatRules.Dodge(balance, With(Stat.Dex, 12), 0), "Dexterity 12: half a point each.");
            Assert.AreEqual(-150, StatRules.Dodge(balance, With(Stat.Dex, 7), 0));
            Assert.AreEqual(500, StatRules.Dodge(balance, null, 5), "An enemy's Dodge 5.");
            Assert.AreEqual(8100, StatRules.HitChance(balance, 200, 100));
            Assert.AreEqual(9500, StatRules.HitChance(balance, 5000, 0), "Never over HitMaxPercent.");
            Assert.AreEqual(500, StatRules.HitChance(balance, 0, 9000), "Never under HitMinPercent.");
        }

        [Test]
        public void CanMiss_IsAPhysicalWeaponsOrAttackItemsDamageOrBurn_Only_AndMagicAlwaysHits()
        {
            ItemData weapon = TestData.Item("w", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            ItemData flask = TestData.Item("f", 1000, EffectKind.Burn, TargetMode.EnemyFront, 2, ItemCategory.Attack);
            ItemData wand = TestData.Item("m", 1000, EffectKind.Damage, TargetMode.EnemyFront, school: ItemSchool.Magic);
            ItemData fire = TestData.Item("fire", 1000, EffectKind.Burn, TargetMode.EnemyFront, 2, ItemCategory.Attack, school: ItemSchool.Magic);
            ItemData staff = TestData.Item("s", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Weapon, school: ItemSchool.Magic);
            ItemData charm = TestData.Item("c", 1000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Support);
            ItemData shield = TestData.Item("b", 1000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Armor);
            Assert.IsTrue(StatRules.CanMiss(weapon, weapon.Effects[0]));
            Assert.IsTrue(StatRules.CanMiss(flask, flask.Effects[0]), "A physical attack item can miss.");
            Assert.IsFalse(StatRules.CanMiss(wand, wand.Effects[0]), "A magic blow never misses (2026-10-20 스탯 개정).");
            Assert.IsFalse(StatRules.CanMiss(fire, fire.Effects[0]));
            Assert.IsFalse(StatRules.CanMiss(staff, staff.Effects[0]), "A healing staff's heal never misses.");
            Assert.IsFalse(StatRules.CanMiss(charm, charm.Effects[0]));
            Assert.IsFalse(StatRules.CanMiss(shield, shield.Effects[0]));

            // In battle: a magic weapon lands every blow where a physical one with the same owner misses them all.
            BalanceData balance = TestData.Balance(("HitBasePercent", 0), ("HitMinPercent", 0), ("HitMaxPercent", 0));
            BattleUnitSetup physical = Merc("p", 1, 100000, StatBlock.Uniform(10), Blade());
            BattleUnitSetup magic = Merc("m", 2, 100000, StatBlock.Uniform(10), new EquippedItem(wand));
            BattleEngine battle = Battle(balance, new[] { physical, magic }, new[] { TestData.Enemy("e", 1, 1000000) }, 1000);
            Assert.AreEqual(1, battle.Events.Count(e => e.Kind == BattleEventKind.Missed), "The blade missed.");
            Assert.AreEqual(1, battle.Events.Count(e => e.Kind == BattleEventKind.Damaged), "The wand hit.");
        }

        [Test]
        public void AMiss_DealsNothing_TiresNobody_AndTriggersNoWeaponHitPassive_WhileHealsStillLand()
        {
            // Every hit misses (a chance of 0): the mercenary's blade with a burn-on-hit passive, the enemy's claw on a mercenary that tires when hit.
            BalanceData balance = TestData.Balance(("HitBasePercent", 0), ("HitMinPercent", 0), ("HitMaxPercent", 0), ("FatigueOnHit", 5));
            BattleUnitSetup merc = Merc("m", 1, 1000, StatBlock.Uniform(10), Blade(), new EquippedItem(TestData.Item("herbs", 1000, EffectKind.Heal, TargetMode.Self, 50, ItemCategory.Support)));
            merc.Hp = 500;
            merc.Passive = new PassiveSpec(PassiveTrigger.WeaponHit, PassiveCondition.None, null, PassiveEffect.Burn, PassiveTarget.EventTarget, 3);
            var claw = new EquippedItem(TestData.Item("claw", 1000, EffectKind.Damage, TargetMode.EnemyFront));
            BattleEngine battle = Battle(balance, new[] { merc }, new[] { TestData.Enemy("e", 1, 100000, claw) }, 1000);

            List<BattleEvent> misses = battle.Events.Where(e => e.Kind == BattleEventKind.Missed).ToList();
            Assert.AreEqual(2, misses.Count, "The blade and the claw both missed.");
            Assert.IsTrue(misses.All(e => e.B == 0));
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.Damaged), "Nothing was dealt.");
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.BurnApplied), "No weapon-hit passive.");
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.FatigueChanged), "A miss is no hit: no fatigue.");
            Assert.AreEqual(1, battle.Events.Count(e => e.Kind == BattleEventKind.Healed), "A heal never misses.");
        }

        [Test]
        public void TheHit_IsRolledForEachUnitAWeaponHits()
        {
            BalanceData balance = Shipped(("HitBasePercent", 50), ("HitMaxPercent", 50));
            BattleUnitSetup merc = Merc("m", 1, 100000, StatBlock.Uniform(10), Blade(1000, TargetMode.EnemyAll, 1));
            BattleEngine battle = Battle(balance, new[] { merc }, new[] { TestData.Enemy("a", 1, 1000000), TestData.Enemy("b", 2, 1000000), TestData.Enemy("c", 3, 1000000) }, 30000);
            bool mixed = battle.Events
                .Where(e => e.Kind == BattleEventKind.Missed || e.Kind == BattleEventKind.Damaged)
                .GroupBy(e => e.TimeMs)
                .Any(at => at.Any(e => e.Kind == BattleEventKind.Missed) && at.Any(e => e.Kind == BattleEventKind.Damaged));
            Assert.IsTrue(mixed, "In one attack some units were missed and others hit.");
        }

        [Test]
        public void TheHitChance_CountsDexterity_AndTheEnemysDodge_InTheLog()
        {
            // Dexterity 14 (+4) against an enemy with Dodge 10 (-10): 74%.
            BalanceData balance = Shipped();
            BattleUnitSetup enemy = TestData.Enemy("e", 1, 1000000);
            enemy.Dodge = 10;
            BattleEngine battle = Battle(balance, new[] { Merc("m", 1, 100000, With(Stat.Dex, 14), Blade()) }, new[] { enemy }, 60000);
            Assert.IsTrue(battle.Events.Any(e => e.Kind == BattleEventKind.Missed), "Some of sixty attacks missed.");
            Assert.IsTrue(battle.Events.Where(e => e.Kind == BattleEventKind.Missed).All(e => e.B == 7400 && e.A >= 7400));
        }

        [Test]
        public void TheCriticalChance_AddsDexterityToAPhysicalWeapon_AndWisdomToAMagicOne_InHalfPoints()
        {
            BalanceData balance = Shipped();
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            ItemData five = new ItemData("five", TestData.Text("five"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 5, school: ItemSchool.Physical);
            Assert.AreEqual(600, StatRules.CritChance(balance, With(Stat.Dex, 12), five));
            Assert.AreEqual(450, StatRules.CritChance(balance, With(Stat.Dex, 9), five), "4.5%.");
            Assert.AreEqual(500, StatRules.CritChance(balance, With(Stat.Wis, 12), five), "Wisdom moves no physical weapon.");
            Assert.AreEqual(500, StatRules.CritChance(balance, null, five), "An enemy: the item's.");
            Assert.AreEqual(0, StatRules.CritChance(balance, With(Stat.Dex, 30), Blade().Item), "A weapon without a critical chance gains none.");
            ItemData wand = new ItemData("wand", TestData.Text("wand"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 5, school: ItemSchool.Magic);
            Assert.AreEqual(600, StatRules.CritChance(balance, With(Stat.Wis, 12), wand), "A magic weapon: wisdom (2026-10-20 스탯 개정).");
            Assert.AreEqual(500, StatRules.CritChance(balance, With(Stat.Dex, 12), wand), "Dexterity moves no magic weapon.");

            // 99% and dexterity 12 (+1%): certain, and the log says the chance.
            ItemData sure = new ItemData("sure", TestData.Text("sure"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 99, school: ItemSchool.Physical);
            BattleEngine battle = Battle(TestData.Balance(), new[] { Merc("m", 1, 100000, With(Stat.Dex, 12), new EquippedItem(sure)) }, new[] { TestData.Enemy("e", 1, 100000) }, 1000);
            Assert.AreEqual(10000, battle.Events.Single(e => e.Kind == BattleEventKind.CriticalHit).C);
        }

        [Test]
        public void TheRangersPassive_AddsToTheCriticalChance_OfItsWeaponsThatHaveOne()
        {
            // Slice D stage 36 (Docs/Design/02_Combat_System.md §7): "무기의 치명타 확률 +10%" — a weapon item with a critical chance, nothing else.
            BalanceData balance = Shipped();
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            ItemData five = new ItemData("five", TestData.Text("five"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 5, school: ItemSchool.Physical);
            var passive = new PassiveSpec(PassiveTrigger.Always, PassiveCondition.None, null, PassiveEffect.CritChancePercent, PassiveTarget.Self, 10);

            Assert.AreEqual(10, StatRules.CritBonusPercent(passive, five));
            Assert.AreEqual(1700, StatRules.CritChance(balance, With(Stat.Dex, 14), five, StatRules.CritBonusPercent(passive, five)), "5 + 10, and dexterity 14's 2.");
            Assert.AreEqual(0, StatRules.CritBonusPercent(passive, Blade().Item), "A weapon without a critical chance gains none.");
            // Only a weapon that deals damage can have a critical chance at all (ItemData refuses it elsewhere), so the passive reaches weapons only.
            Assert.AreEqual(0, StatRules.CritBonusPercent(null, five));
            Assert.Throws<DataException>(
                () => new PassiveSpec(PassiveTrigger.Always, PassiveCondition.SelfInDog, null, PassiveEffect.CritChancePercent, PassiveTarget.Self, 10),
                "It holds everywhere: no condition, so the card can count it without the row.");

            // In battle: 90% and the passive's 10 make it certain, and the log says so.
            ItemData ninety = new ItemData("ninety", TestData.Text("ninety"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 90, school: ItemSchool.Physical);
            BattleUnitSetup ranger = Merc("r", 1, 100000, With(Stat.Dex, 10), new EquippedItem(ninety));
            ranger.Passive = passive;
            BattleEngine battle = Battle(TestData.Balance(), new[] { ranger }, new[] { TestData.Enemy("e", 1, 100000) }, 1000);
            Assert.AreEqual(10000, battle.Events.Single(e => e.Kind == BattleEventKind.CriticalHit).C);
        }

        [Test]
        public void TheDeathChance_FallsWithEndurance()
        {
            // 2026-10-20 스탯 개정: endurance, before constitution (which gives the HP now).
            BalanceData balance = TestData.Balance(("DogDeathChancePercent", 30), ("DogGraceMs", 0));
            Assert.AreEqual(28, FatigueRules.DogDeathChancePercent(balance, null, With(Stat.End, 12)));
            Assert.AreEqual(33, FatigueRules.DogDeathChancePercent(balance, null, With(Stat.End, 7)));
            Assert.AreEqual(30, FatigueRules.DogDeathChancePercent(balance, null, With(Stat.Con, 12)), "Constitution moves it no more.");
            Assert.AreEqual(30, FatigueRules.DogDeathChancePercent(balance, null, null));
            FatigueStateData reckless = TestData.FatigueStates().Single(s => s.Id == "reckless");
            Assert.AreEqual(53, FatigueRules.DogDeathChancePercent(balance, reckless, With(Stat.End, 12)), "With the state's +25.");

            // At death's door from the start, without grace: the first hit rolls the death with endurance 12's chance.
            BattleUnitSetup merc = Merc("m", 1, 1000, With(Stat.End, 12));
            merc.Hp = 0;
            var claw = new EquippedItem(TestData.Item("claw", 1000, EffectKind.Damage, TargetMode.EnemyFront));
            BattleEngine battle = Battle(balance, new[] { merc }, new[] { TestData.Enemy("e", 1, 100000, claw) }, 1000);
            Assert.AreEqual(28, battle.Events.First(e => e.Kind == BattleEventKind.DeathRolled).A);
        }

        [Test]
        public void TheVirtueChance_GrowsWithEndurance()
        {
            // 2026-10-20 스탯 개정: endurance, before spirit (wisdom now, which moves the magic).
            BalanceData balance = TestData.Balance();
            Assert.AreEqual(29, StatRules.VirtueChancePercent(balance, With(Stat.End, 14)));
            Assert.AreEqual(25, StatRules.VirtueChancePercent(balance, With(Stat.Wis, 14)), "Wisdom moves it no more.");
            Assert.AreEqual(25, StatRules.VirtueChancePercent(balance, null));
            Assert.AreEqual(100, StatRules.VirtueChancePercent(TestData.Balance(("VirtueChancePercent", 95)), With(Stat.End, 20)), "Within 0..100.");

            // A mercenary that starts at the threshold breaks down at once, rolling endurance 14's chance.
            BattleUnitSetup merc = Merc("m", 1, 1000, With(Stat.End, 14)).WithFatigue(100);
            BattleEngine battle = Battle(balance, new[] { merc }, new[] { TestData.Enemy("e", 1, 100000) }, 100);
            Assert.AreEqual(29, battle.Events.Single(e => e.Kind == BattleEventKind.BrokeDown).B);
        }

        [Test]
        public void Endurance_SoftensTheFatigueGained_AndLeavesWhatTakesItOff()
        {
            BalanceData balance = TestData.Balance();
            Assert.AreEqual(9, StatRules.FatigueGain(balance, With(Stat.End, 13), 10), "10 x 91% = 9.1.");
            Assert.AreEqual(11, StatRules.FatigueGain(balance, With(Stat.End, 7), 10), "10 x 109% = 10.9.");
            Assert.AreEqual(1, StatRules.FatigueGain(balance, With(Stat.End, 20), 1), "0.7 rounds up.");
            Assert.AreEqual(0, StatRules.FatigueGain(balance, With(Stat.End, 30), 1), "0.4 rounds to nothing.");
            Assert.AreEqual(-3, StatRules.FatigueGain(balance, With(Stat.End, 20), -3), "Relief stays as it is.");
            Assert.AreEqual(10, StatRules.FatigueGain(balance, null, 10));

            // A critical hit taken tires an enduring mercenary less; one dealt relieves it as much as anyone.
            BalanceData crits = TestData.Balance(("CritFatigueTaken", 10), ("CritFatigueDealt", 3));
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            var sure = new EquippedItem(new ItemData("sure", TestData.Text("sure"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 100));
            BattleEngine taken = Battle(crits, new[] { Merc("m", 1, 100000, With(Stat.End, 13)).WithFatigue(50) }, new[] { TestData.Enemy("e", 1, 100000, sure) }, 1000);
            Assert.AreEqual(9, taken.Events.Single(e => e.Kind == BattleEventKind.FatigueChanged && e.Id == BattleEvent.FatigueCritTaken).A);
            BattleEngine dealt = Battle(crits, new[] { Merc("m", 1, 100000, With(Stat.End, 13), sure).WithFatigue(50) }, new[] { TestData.Enemy("e", 1, 100000) }, 1000);
            Assert.AreEqual(-3, dealt.Events.Single(e => e.Kind == BattleEventKind.FatigueChanged && e.Id == BattleEvent.FatigueCritDealt).A);
        }

        [Test]
        public void TheBattleEntryCost_IsOneEvent_AfterEndurance()
        {
            // Entry 5, a base weapon (no equipment cost): endurance 15 pays 5 x 85% = 4.25 -> 4.
            StaticData data = TestData.Data(("FatigueBattleEntry", 5));
            ExpeditionState expedition = ExpeditionRules.Create(data, "cave", 3, new[] { new PartyMember("anna", "tank", 1, 10, stats: With(Stat.End, 15)) });
            ExpeditionRules.BeginBattle(data, expedition, ExpeditionRules.AvailableNodes(expedition).First(n => n.IsFought).Id);
            Assert.AreEqual(14, expedition.Members.Single().Fatigue);
        }

        [Test]
        public void AnEnemysAccuracyAndDodge_ComeFromItsData()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Enemies = parts.Enemies.Select(e => e.Id == "grunt" ? new EnemyData(e.Id, e.Name, e.Level, e.MaxHp, e.Items, e.Figure, e.FigureScale, e.Phases, accuracy: 10, dodge: 5) : e).ToList();
            var data = new StaticData(parts);
            ExpeditionState expedition = ExpeditionRules.Create(data, "cave", 3, new[] { new PartyMember("anna", "tank", 1) });
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1);
            Assert.IsTrue(setup.Enemies.All(e => e.Accuracy == 10 && e.Dodge == 5));
            Assert.AreEqual(0, setup.Party.Single().Accuracy);
            Assert.Throws<DataException>(() => new EnemyData("x", TestData.Text("x"), 1, 10, new List<ItemGrant>(), accuracy: 101));
            Assert.Throws<DataException>(() => TestData.Balance(("HitBasePercent", 100), ("HitMaxPercent", 95)), "The base lies within the bounds.");
        }

        [Test]
        public void SameSetupWithMisses_GivesTheSameLog_HoweverItIsStepped()
        {
            BattleSetup Setup()
            {
                var claw = new EquippedItem(TestData.Item("claw", 900, EffectKind.Damage, TargetMode.EnemyFront, 100));
                BattleUnitSetup enemy = TestData.Enemy("e", 1, 3000, claw);
                enemy.Accuracy = 5;
                return TestData.Setup(Shipped(), new[] { Merc("m", 1, 3000, new StatBlock(13, 12, 12, 6, 9, 11), Blade(1300)) }, new[] { enemy });
            }

            var whole = new BattleEngine(Setup());
            var stepped = new BattleEngine(Setup());
            whole.AdvanceTo(30000);
            for (int t = 200; t <= 30000; t += 200)
            {
                stepped.AdvanceTo(t);
            }

            Assert.IsTrue(whole.Events.Any(e => e.Kind == BattleEventKind.Missed));
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(stepped.Events));
        }
    }
}
