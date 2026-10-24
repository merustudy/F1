using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The trait expansion of stage 57 (Docs/Design/39_Trait_Expansion_Proposal.md, Docs/Design/05_Mercenary_Growth.md §6): the build slot a candidate
    /// draws before its other innate traits, with what the build shifts in its stats; the growth a trait adds; the death of an ally as a source; the
    /// mark a breakdown or a virtue leaves from that state; the damage, the healing and the allies' fatigue a trait moves in battle; and the figure a
    /// build is drawn in. One rule per test.
    /// </summary>
    public sealed class TraitExpansionTests
    {
        static readonly LocalizedText T = TestData.Text("t");

        static TraitData Build(string id, int weight, int str = 0, int dex = 0, int con = 0, string figure = null, int figureScale = 100)
        {
            return new TraitData(id, T, TraitKind.Innate, TraitPolarity.Positive, null, weight, null, 0, 0, 0, 0, 0, 0, 0,
                build: true, figure: figure, str: str, dex: dex, con: con, figureScale: figureScale);
        }

        static TraitData Mark(string id, TraitSource source, string state, int weight)
        {
            return new TraitData(id, T, TraitKind.Acquired, TraitPolarity.Negative, source, weight, null, -1, 0, 0, 0, 0, 0, 0, sourceState: state);
        }

        static StaticData DataWith(IEnumerable<TraitData> traits, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Traits = TestData.Traits().Concat(traits).ToList();
            return new StaticData(parts);
        }

        static BattleUnitSetup WithTraits(BattleUnitSetup unit, TraitBonus traits)
        {
            unit.Traits = traits;
            return unit;
        }

        static TraitBonus Bonus(int damage = 0, int healTaken = 0, int allyFatigue = 0)
        {
            return new TraitBonus(0, 0, 0, 0, 0, 0, 0, 0, 0, damage, healTaken, allyFatigue);
        }

        // ---- The build slot ---------------------------------------------------------------------

        [Test]
        public void ACandidate_DrawsItsBuildAfterItsStats_WithTheBuildsShiftInItsStats_ThenItsOtherInnateTraits()
        {
            StaticData data = DataWith(new[] { Build("huge", 1, str: 2, dex: -2) },
                ("BuildWeightNone", 0), ("RecruitTraitWeight0", 0), ("RecruitTraitWeight1", 1), ("RecruitCount", 3), ("RosterLimit", 8));

            RunState run = RunRules.NewRun(data, 11);

            Assert.AreEqual(3, run.Recruits.Count);
            foreach (RecruitState recruit in run.Recruits)
            {
                Assert.AreEqual("huge", recruit.Traits[0], "The build comes first.");
                Assert.AreEqual(2, recruit.Traits.Count, "Then one innate trait, never a second build.");
                StatBlock shifted = TraitRules.Shifted(data, StatRules.BaseOf(data, recruit.JobId), new[] { "huge" });
                Assert.AreEqual(StatRules.MeetingBaseWeapon(data, recruit.JobId, shifted), recruit.Stats, recruit.Id);
            }
        }

        [Test]
        public void DataWithoutBuilds_DrawsNoBuild_SoTheCandidatesComeAsBefore()
        {
            StaticData none = DataWith(new TraitData[0], ("BuildWeightNone", 0), ("RecruitTraitWeight0", 0), ("RecruitTraitWeight2", 1));
            StaticData some = DataWith(new TraitData[0], ("BuildWeightNone", 50), ("RecruitTraitWeight0", 0), ("RecruitTraitWeight2", 1));

            CollectionAssert.AreEqual(RunRules.NewRun(none, 5).Recruits.Select(r => r.Id + r.Stats + string.Join("+", r.Traits)),
                RunRules.NewRun(some, 5).Recruits.Select(r => r.Id + r.Stats + string.Join("+", r.Traits)));
            var rng = new Pcg32(9, RngStream.Recruit);
            Assert.IsNull(TraitRules.DrawBuild(some, rng));
            Assert.AreEqual(new Pcg32(9, RngStream.Recruit).NextInt(1000), rng.NextInt(1000), "Nothing was drawn.");
        }

        [Test]
        public void TheBuildDraw_WeighsNoneAgainstEachBuild_InOneDraw()
        {
            StaticData data = DataWith(new[] { Build("huge", 25, str: 1), Build("tiny", 25, con: -1) }, ("BuildWeightNone", 50));
            var drawn = new Dictionary<string, int> { { "none", 0 }, { "huge", 0 }, { "tiny", 0 } };
            var rng = new Pcg32(3, RngStream.Recruit);
            for (int i = 0; i < 4000; i++)
            {
                drawn[TraitRules.DrawBuild(data, rng) ?? "none"]++;
            }

            Assert.AreEqual(2000, drawn["none"], 150);
            Assert.AreEqual(1000, drawn["huge"], 120);
            Assert.AreEqual(1000, drawn["tiny"], 120);
        }

        [Test]
        public void Shifted_AddsWhatTheTraitsShift_WithinStatMinAndStatMax_AndBuildOfFindsTheBuild()
        {
            StaticData data = DataWith(new[] { Build("huge", 1, str: 5, dex: -5, con: 1) }, ("StatMin", 1), ("StatMax", 12));
            StatBlock shifted = TraitRules.Shifted(data, new StatBlock(10, 3, 4, 5, 6, 7), new[] { "keen", "huge" });

            Assert.AreEqual(new StatBlock(12, 1, 5, 5, 6, 7), shifted, "10 + 5 stops at 12, 3 - 5 at 1; keen shifts no stat.");
            Assert.AreEqual("huge", TraitRules.BuildOf(data, new[] { "keen", "huge" }).Id);
            Assert.IsNull(TraitRules.BuildOf(data, new[] { "keen" }));
        }

        // ---- Growth ----------------------------------------------------------------------------

        [Test]
        public void ATraitsGrowth_AddsToEveryGrade_AndAGradeNeverGoesUnderZero()
        {
            StaticDataParts parts = TestData.Parts(("StatMax", 99));
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank"
                    ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, StatBlock.Uniform(10), new StatBlock(200, 100, 0, 0, 0, 0))
                    : job)
                .ToList();
            parts.Traits = new List<TraitData>
            {
                new TraitData("keen_mind", T, TraitKind.Innate, TraitPolarity.Positive, null, 10, null, 0, 0, 0, 0, 0, 0, 0, growthPercent: 100),
                new TraitData("dull_mind", T, TraitKind.Innate, TraitPolarity.Negative, null, 10, null, 0, 0, 0, 0, 0, 0, 0, growthPercent: -100),
            };
            var data = new StaticData(parts);

            var keen = new MercenaryState { Id = "anna", JobId = "tank", Level = 2, Stats = StatBlock.Uniform(10), Traits = new List<string> { "keen_mind" } };
            Assert.AreEqual(new StatBlock(3, 2, 1, 1, 1, 1), LevelRules.Grow(data, keen, 1, new Pcg32(1, RngStream.Growth)), "Every grade a hundred more: no chance left.");
            var dull = new MercenaryState { Id = "ben", JobId = "tank", Level = 2, Stats = StatBlock.Uniform(10), Traits = new List<string> { "dull_mind" } };
            Assert.AreEqual(new StatBlock(1, 0, 0, 0, 0, 0), LevelRules.Grow(data, dull, 1, new Pcg32(1, RngStream.Growth)), "200 - 100 = 100, 100 - 100 = 0, and 0 - 100 stops at 0: no grade under it.");
        }

        // ---- What an expedition leaves ---------------------------------------------------------

        [Test]
        public void TheDeathOfAnAlly_IsASourceAfterTheOthers_ForTheSurvivorsOnly()
        {
            StaticData data = TestData.DataWithTraits();
            var member = new ExpeditionMember { MercenaryId = "anna", Alive = true, StateId = "fearful" };
            CollectionAssert.AreEqual(new[] { TraitSource.Clear, TraitSource.Affliction, TraitSource.AllyDied }, TraitRules.SourcesOf(data, ExpeditionResult.Cleared, member, allyDied: true));
            CollectionAssert.AreEqual(new[] { TraitSource.Clear, TraitSource.Affliction }, TraitRules.SourcesOf(data, ExpeditionResult.Cleared, member));
            member.Alive = false;
            Assert.IsEmpty(TraitRules.SourcesOf(data, ExpeditionResult.Cleared, member, allyDied: true));
        }

        [Test]
        public void Settle_LeavesAnAllysDeathsTrait_OnTheSurvivors_AtItsChance()
        {
            var loss = new TraitData("loss_t", T, TraitKind.Acquired, TraitPolarity.Negative, TraitSource.AllyDied, 10, null, 0, 0, 0, 0, -5, 0, 0);
            StaticData data = DataWith(new[] { loss }, ("TraitChanceAllyDiedPercent", 100));
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == 3).Id;
            ExpeditionMember cora = expedition.Members.Single(m => m.MercenaryId == "cora");
            cora.Alive = false;
            cora.Hp = 0;
            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = ExpeditionResult.Cleared;

            RunRules.Settle(data, run, expedition);

            CollectionAssert.AreEqual(new[] { "loss_t" }, RunRules.FindMercenary(run, "anna").Traits);
            CollectionAssert.AreEqual(new[] { "loss_t" }, RunRules.FindMercenary(run, "ben").Traits);
        }

        [Test]
        public void ABreakdownsMark_ComesFromThatStateOnly_BesideTheCommonOnes()
        {
            StaticData data = DataWith(new[] { Mark("fear_mark", TraitSource.Affliction, "fearful", 20), Mark("gloom_mark", TraitSource.Affliction, "hopeless", 1000) },
                ("TraitChanceAfflictionPercent", 100));
            var drawn = new HashSet<string>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                drawn.UnionWith(TraitRules.DrawAcquired(data, new string[0], new[] { TraitSource.Affliction }, new Pcg32(seed, RngStream.Trait), "fearful"));
            }

            CollectionAssert.AreEquivalent(new[] { "fear_mark", "dread_t" }, drawn, "Fear's own mark and the common one; never hopelessness's.");
        }

        // ---- In battle -------------------------------------------------------------------------

        [Test]
        public void ATraitsDamagePercent_CountsWithAStates_OnWhatItsItemsDealTheOtherSide()
        {
            BalanceData balance = TestData.Balance();
            var strong = new FatigueStateData("strong", T, FatigueStateKind.Virtue, 0, 0, 0, T, 0, 0, 0, 25, 0, 0);
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    WithTraits(TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 100)), Bonus(damage: -20)),
                    WithTraits(TestData.Mercenary("b", 2, 1000, TestData.Attack(1000, 100)).WithFatigue(40, strong), Bonus(damage: 25)),
                    TestData.Mercenary("c", 3, 1000, TestData.Attack(1000, 100))),
                TestData.Units(TestData.Enemy("e", 1, 100000))));

            battle.AdvanceTo(1000);

            List<BattleEvent> hits = battle.Events.Where(e => e.Kind == BattleEventKind.Damaged && e.Source.Side == BattleSide.Party).ToList();
            int plain = hits.Single(e => e.Source.Index == 2).A;
            Assert.AreEqual(plain * 80 / 100, hits.Single(e => e.Source.Index == 0).A);
            Assert.AreEqual(plain * 150 / 100, hits.Single(e => e.Source.Index == 1).A, "The state's 25 and the trait's 25.");
        }

        [Test]
        public void ATraitsHealTakenPercent_ChangesTheHealingTheUnitTakes()
        {
            BalanceData balance = TestData.Balance();
            EquippedItem herb = new EquippedItem(TestData.Item("herb", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Support), powerPercent: 10);
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    WithTraits(TestData.Mercenary("a", 1, 500, herb).WithMaxHp(1000), Bonus(healTaken: 50)),
                    TestData.Mercenary("b", 2, 500, herb).WithMaxHp(1000)),
                TestData.Units(TestData.Enemy("e", 1, 100000))));

            battle.AdvanceTo(1000);

            List<BattleEvent> heals = battle.Events.Where(e => e.Kind == BattleEventKind.Healed).ToList();
            int plain = heals.First(e => e.Target.Index == 1).A;
            Assert.Greater(plain, 0);
            Assert.AreEqual(plain * 150 / 100, heals.First(e => e.Target.Index == 0).A);
        }

        [Test]
        public void ATraitsAllyFatigue_MovesEveryAllyAsTheBattleStarts_NotTheUnitItself()
        {
            BalanceData balance = TestData.Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    WithTraits(TestData.Mercenary("a", 1, 1000).WithFatigue(10), Bonus(allyFatigue: 2)),
                    TestData.Mercenary("b", 2, 1000).WithFatigue(10),
                    WithTraits(TestData.Mercenary("c", 3, 1000).WithFatigue(10), Bonus(allyFatigue: -1))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));

            Assert.AreEqual(10 - 1, battle.Party[0].Fatigue, "Only c's -1.");
            Assert.AreEqual(10 + 2 - 1, battle.Party[1].Fatigue);
            Assert.AreEqual(10 + 2, battle.Party[2].Fatigue, "a's +2; c's own -1 is not for c.");
            Assert.IsTrue(battle.Events.Any(e => e.Kind == BattleEventKind.FatigueChanged && e.Id == BattleEvent.FatigueTrait && e.Source.Index == 0 && e.Target.Index == 1 && e.A == 2));
        }

        // ---- The figure ------------------------------------------------------------------------

        [Test]
        public void AMercenarysFigure_IsItsJobsInItsBuild_WhereThatIsDrawn_AtTheBuildsScale()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, "unit/job/tank", job.Stats, job.Growth) : job)
                .ToList();
            parts.Traits = new List<TraitData> { Build("huge", 1, str: 1, figure: "huge", figureScale: 130), Build("lean", 1, dex: 1) };
            var data = new StaticData(parts);
            var drawn = new HashSet<string> { "unit/job/tank", "unit/job/tank-huge" };

            Assert.AreEqual("unit/job/tank-huge", ExpeditionArt.FigureAddressOf(data, "tank", null, new[] { "huge" }, drawn.Contains));
            Assert.AreEqual(1.3f, ExpeditionArt.ScaleOf(data, "tank", null, new[] { "huge" }, drawn.Contains), 1e-6f);
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", null, new[] { "lean" }, drawn.Contains), "A build without a variant: the job's own.");
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", null, new string[0], drawn.Contains));
            drawn.Remove("unit/job/tank-huge");
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", null, new[] { "huge" }, drawn.Contains), "Not drawn for the job: its own.");
            Assert.AreEqual(1f, ExpeditionArt.ScaleOf(data, "tank", null, new[] { "huge" }, drawn.Contains), "And its own size.");
            Assert.AreEqual("pose/job/tank-huge-attack", ArtAddress.PoseOf(ArtAddress.BuildFigure("unit/job/tank", "huge"), ArtAddress.Attack));
        }

        // ---- The figure by the name's gender (the user's "이름의 성별로") ------------------------------

        static StaticData DataWithFigures(params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank"
                    ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, "unit/job/tank", job.Stats, job.Growth, job.PassiveName,
                        new[] { "unit/job/tank", "unit/job/blade" }, new[] { "unit/job/dame" })
                    : job)
                .ToList();
            parts.Mercenaries = parts.Mercenaries
                .Select((m, i) => m.Starting ? m : new MercenaryData(m.Id, m.Name, null, false, null, i % 2 == 0 ? Gender.Female : Gender.Male))
                .ToList();
            parts.Traits = new List<TraitData> { Build("huge", 1, str: 1, figure: "huge", figureScale: 130) };
            return new StaticData(parts);
        }

        [Test]
        public void ACandidatesFigure_IsOneOfItsJobsFiguresOfItsNamesGender_AndNoneForAJobWithout_AndAHireKeepsIt()
        {
            StaticData data = DataWithFigures(("RecruitCount", 3), ("RosterLimit", 8));
            var seen = new HashSet<string>();
            for (ulong seed = 1; seed <= 60; seed++)
            {
                foreach (RecruitState recruit in RunRules.NewRun(data, seed).Recruits)
                {
                    Gender gender = data.Mercenaries.Get(recruit.Id).Gender;
                    if (recruit.JobId != "tank")
                    {
                        Assert.IsNull(recruit.Figure, "A job without figures by gender: its own.");
                        continue;
                    }

                    if (gender == Gender.Female)
                    {
                        Assert.AreEqual("unit/job/dame", recruit.Figure);
                    }
                    else
                    {
                        CollectionAssert.Contains(new[] { "unit/job/tank", "unit/job/blade" }, recruit.Figure);
                    }

                    seen.Add(recruit.Figure);
                }
            }

            CollectionAssert.IsSubsetOf(new[] { "unit/job/tank", "unit/job/blade", "unit/job/dame" }, seen.ToList(), "Every figure comes now and then.");
            var rng = new Pcg32(4, RngStream.Recruit);
            Assert.AreEqual("unit/job/dame", RunRules.DrawFigure(data.Jobs.Get("tank"), Gender.Female, rng));
            Assert.AreEqual(new Pcg32(4, RngStream.Recruit).NextInt(1000), rng.NextInt(1000), "One figure draws nothing.");

            RunState run = RunRules.NewRun(data, 3);
            run.Gold = 1000;
            run.Recruits[0].Figure = "unit/job/blade";
            Assert.AreEqual("unit/job/blade", RunRules.Hire(data, run, 0).Figure);
        }

        [Test]
        public void AMercenarysFigure_StandsInItsBuild_WhereDrawnForThatFigure_AndAFigureNotOfItsJobIsTheJobsOwn()
        {
            StaticData data = DataWithFigures();
            var drawn = new HashSet<string> { "unit/job/tank", "unit/job/blade", "unit/job/blade-huge" };
            Assert.AreEqual("unit/job/blade-huge", ExpeditionArt.FigureAddressOf(data, "tank", "unit/job/blade", new[] { "huge" }, drawn.Contains));
            Assert.AreEqual(1.3f, ExpeditionArt.ScaleOf(data, "tank", "unit/job/blade", new[] { "huge" }, drawn.Contains), 1e-6f);
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", null, new[] { "huge" }, drawn.Contains), "The huge tank is not drawn.");
            Assert.AreEqual(1f, ExpeditionArt.ScaleOf(data, "tank", null, new[] { "huge" }, drawn.Contains));
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", "unit/job/ghost", new string[0], drawn.Contains), "Not one of the job's.");
            Assert.AreEqual("unit/job/tank", ExpeditionArt.FigureAddressOf(data, "tank", "unit/job/dame", new string[0], drawn.Contains), "One of the job's but not drawn.");
            CollectionAssert.AreEqual(new[] { "unit/job/tank", "unit/job/blade", "unit/job/dame" }, ExpeditionArt.FiguresOf(data.Jobs.Get("tank")).ToList());
        }
    }
}
