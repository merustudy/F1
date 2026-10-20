using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The mercenaries' traits (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): what they add up to, the innate ones a candidate draws
    /// (never twice, never two that clash, the same for the same seed, nothing rolled when nothing can come), the term each adds to its formula,
    /// and how they travel from the roster to the battle. One rule per test.
    /// </summary>
    public sealed class TraitRulesTests
    {
        /// <summary>The test data with TestData.Traits(): keen and dull clash, scar_t is acquired.</summary>
        static StaticData Data(params (string Key, int Value)[] balance)
        {
            return TestData.DataWithTraits(balance);
        }

        [Test]
        public void BonusOf_SumsTheTraits_AndNoneAddsNothing()
        {
            StaticData data = Data();
            TraitBonus bonus = TraitRules.BonusOf(data, new[] { "keen", "sturdy", "frail_t" });
            Assert.AreEqual((5, 0, 0, -5, 0, 0, -10), (bonus.HitPercent, bonus.DodgePercent, bonus.CritPercent, bonus.DogDeathPercent, bonus.VirtuePercent, bonus.FatiguePercent, bonus.MaxHpPercent));
            Assert.AreSame(TraitBonus.None, TraitRules.BonusOf(data, new string[0]));
            Assert.AreSame(TraitBonus.None, TraitRules.BonusOf(data, null));
        }

        [Test]
        public void CanTake_RefusesATraitHeld_AndOneThatClashesEitherWay()
        {
            StaticData data = Data();
            Assert.IsTrue(TraitRules.CanTake(data, new[] { "sturdy" }, data.Traits.Get("keen")));
            Assert.IsFalse(TraitRules.CanTake(data, new[] { "keen" }, data.Traits.Get("keen")), "Never twice.");
            Assert.IsFalse(TraitRules.CanTake(data, new[] { "keen" }, data.Traits.Get("dull")), "Keen names dull.");
            Assert.IsFalse(TraitRules.CanTake(data, new[] { "dull" }, data.Traits.Get("keen")), "The clash holds whichever names the other.");
        }

        [Test]
        public void DrawInnate_RollsNothing_WhenNoTraitCanCome_OrNoCountAboveNoneHasAWeight()
        {
            // Without traits, or with only "none" weighted, the stream is untouched: data without traits draws its candidates as before.
            var cases = new[]
            {
                new StaticData(TestData.Parts(("RecruitTraitWeight0", 0), ("RecruitTraitWeight1", 1))),
                Data(("RecruitTraitWeight0", 1)),
            };
            foreach (StaticData data in cases)
            {
                var rng = new Pcg32(7, RngStream.Recruit);
                var fresh = new Pcg32(7, RngStream.Recruit);
                Assert.AreEqual(0, TraitRules.DrawInnate(data, rng).Count);
                Assert.AreEqual(fresh.NextInt(1000), rng.NextInt(1000), "Nothing was rolled.");
            }
        }

        [Test]
        public void DrawInnate_CountsByTheWeights_NeverTwice_NeverTwoThatClash_NeverAnAcquiredOne()
        {
            StaticData two = Data(("RecruitTraitWeight0", 0), ("RecruitTraitWeight2", 1));
            StaticData one = Data(("RecruitTraitWeight0", 0), ("RecruitTraitWeight1", 1));
            for (ulong seed = 0; seed < 200; seed++)
            {
                List<string> drawn = TraitRules.DrawInnate(two, new Pcg32(seed, RngStream.Recruit));
                Assert.AreEqual(2, drawn.Count);
                Assert.AreEqual(2, drawn.Distinct().Count(), "Never twice.");
                Assert.IsFalse(drawn.Contains("keen") && drawn.Contains("dull"), "Never two that clash.");
                Assert.IsFalse(drawn.Contains("scar_t"), "An acquired trait is no candidate's.");
                Assert.AreEqual(1, TraitRules.DrawInnate(one, new Pcg32(seed, RngStream.Recruit)).Count);
            }
        }

        [Test]
        public void DrawInnate_IsTheSameForTheSameSeed_AndDrawsEveryInnateTraitSometimes()
        {
            StaticData data = Data(("RecruitTraitWeight0", 30), ("RecruitTraitWeight1", 50), ("RecruitTraitWeight2", 20));
            var seen = new HashSet<string>();
            for (ulong seed = 0; seed < 300; seed++)
            {
                List<string> first = TraitRules.DrawInnate(data, new Pcg32(seed, RngStream.Recruit));
                CollectionAssert.AreEqual(first, TraitRules.DrawInnate(data, new Pcg32(seed, RngStream.Recruit)));
                seen.UnionWith(first);
            }

            CollectionAssert.AreEquivalent(new[] { "keen", "dull", "sturdy", "frail_t" }, seen);
        }

        [Test]
        public void TheFormulas_AddTheTraitsTerms()
        {
            BalanceData balance = TestData.Balance(("HitBasePercent", 80), ("HitMaxPercent", 95), ("DogDeathChancePercent", 30));
            var traits = new TraitBonus(5, 3, 2, -5, 10, 10, -10);
            StatBlock baseline = StatBlock.Uniform(10);
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            var five = new ItemData("five", TestData.Text("five"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 5);

            Assert.AreEqual(500, StatRules.Accuracy(balance, baseline, 0, traits), "Hit +5 points, in ten-thousandths.");
            Assert.AreEqual(1000, StatRules.Accuracy(balance, null, 10, traits), "An enemy's Accuracy alone: it has no traits.");
            Assert.AreEqual(300, StatRules.Dodge(balance, baseline, 0, traits));
            Assert.AreEqual(700, StatRules.CritChance(balance, baseline, five, 0, traits), "5% + 2 points.");
            Assert.AreEqual(0, StatRules.CritChance(balance, baseline, blade, 0, traits), "An item without a critical chance has none.");
            Assert.AreEqual(35, StatRules.VirtueChancePercent(balance, baseline, traits));
            Assert.AreEqual(11, StatRules.FatigueGain(balance, baseline, 10, traits), "10 x 110%.");
            Assert.AreEqual(11, StatRules.FatigueGain(balance, null, 10, traits), "Without stats the traits still count.");
            Assert.AreEqual(-3, StatRules.FatigueGain(balance, baseline, -3, traits), "What takes fatigue off stays as it is.");
            Assert.AreEqual(25, FatigueRules.DogDeathChancePercent(balance, null, baseline, traits));
            StaticData data = TestData.Data();
            Assert.AreEqual(90, LevelRules.MaxHpOf(data, "tank", baseline, traits), "The tank's 100 less 10%.");
            Assert.AreEqual(1, LevelRules.MaxHpOf(data, "tank", baseline, new TraitBonus(0, 0, 0, 0, 0, 0, -100)), "Never under 1%.");
        }

        [Test]
        public void TheBattle_RollsTheHitAndTheDeath_WithTheTraits()
        {
            // A dull mercenary (-6 points) at the shipped 80%: every miss is logged at 74%. At death's door from the start, without grace, the first
            // hit rolls the death at 30% less a sturdy 5.
            BalanceData balance = TestData.Balance(("HitBasePercent", 80), ("HitMaxPercent", 95), ("DogDeathChancePercent", 30), ("DogGraceMs", 0));
            var blade = new EquippedItem(TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront));
            BattleUnitSetup dull = TestData.Mercenary("m", 1, 100000, blade);
            dull.Stats = StatBlock.Uniform(10);
            dull.Traits = new TraitBonus(-6, 0, 0, 0, 0, 0, 0);
            var battle = new BattleEngine(TestData.Setup(balance, new[] { dull }, new[] { TestData.Enemy("e", 1, 1000000) }));
            battle.AdvanceTo(60000);
            Assert.IsTrue(battle.Events.Any(e => e.Kind == BattleEventKind.Missed));
            Assert.IsTrue(battle.Events.Where(e => e.Kind == BattleEventKind.Missed).All(e => e.B == 7400));

            BattleUnitSetup sturdy = TestData.Mercenary("s", 1, 1000);
            sturdy.Stats = StatBlock.Uniform(10);
            sturdy.Traits = new TraitBonus(0, 0, 0, -5, 0, 0, 0);
            sturdy.Hp = 0;
            var claw = new EquippedItem(TestData.Item("claw", 1000, EffectKind.Damage, TargetMode.EnemyFront));
            var door = new BattleEngine(TestData.Setup(balance, new[] { sturdy }, new[] { TestData.Enemy("e", 1, 100000, claw) }));
            door.AdvanceTo(1000);
            Assert.AreEqual(25, door.Events.First(e => e.Kind == BattleEventKind.DeathRolled).A);
        }

        [Test]
        public void TheTraits_GoFromTheData_ToTheRoster_TheCandidates_TheHired_TheExpedition_AndTheBattle()
        {
            StaticDataParts parts = TestData.Parts(("RecruitTraitWeight0", 0), ("RecruitTraitWeight2", 1));
            parts.Traits = TestData.Traits();
            parts.Mercenaries = parts.Mercenaries
                .Select(m => m.Id == "anna" ? new MercenaryData("anna", m.Name, m.JobId, true, new[] { "frail_t", "keen" }) : m)
                .ToList();
            var data = new StaticData(parts);

            RunState run = RunRules.NewRun(data, 11);
            MercenaryState anna = RunRules.FindMercenary(run, "anna");
            CollectionAssert.AreEqual(new[] { "frail_t", "keen" }, anna.Traits, "The data's, in its order: nothing rolls them.");
            Assert.AreEqual(0, RunRules.FindMercenary(run, "ben").Traits.Count);
            Assert.IsTrue(run.Recruits.Count > 0 && run.Recruits.All(r => r.Traits.Count == 2), "Each candidate drew two.");

            run.Gold = 1000;
            List<string> born = run.Recruits[0].Traits;
            MercenaryState hired = RunRules.Hire(data, run, 0);
            CollectionAssert.AreEqual(born, hired.Traits, "A hire keeps what it was born with.");

            ExpeditionState expedition = ExpeditionRules.Create(data, "cave", 5, new[]
            {
                new PartyMember("anna", "tank", 1, traits: anna.Traits),
                new PartyMember("ben", "healer", 2),
            });
            ExpeditionMember member = expedition.Members[0];
            CollectionAssert.AreEqual(anna.Traits, member.Traits);
            Assert.AreEqual(90, member.MaxHp, "The tank's 100 less frail's 10%.");

            BattleSetup setup = ExpeditionRules.BeginBattle(data, expedition, ExpeditionRules.AvailableNodes(expedition)[0].Id);
            BattleUnitSetup unit = setup.Party.Single(u => u.SourceId == "anna");
            Assert.AreEqual((5, -10), (unit.Traits.HitPercent, unit.Traits.MaxHpPercent));
            Assert.AreEqual(0, setup.Party.Single(u => u.SourceId == "ben").Traits.HitPercent);
        }
    }
}
