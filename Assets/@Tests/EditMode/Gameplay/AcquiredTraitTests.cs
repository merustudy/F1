using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The traits an expedition leaves on a survivor (Slice D stage 38, Docs/Design/05_Mercenary_Growth.md §6): the sources a survivor meets, the
    /// roll of each (a trait it can take, by the source's chance, never twice nor clashing, nothing rolled when nothing can come), the settlement
    /// that leaves them on its own stream, and the battle that marks who went to death's door. One rule per test.
    /// </summary>
    public sealed class AcquiredTraitTests
    {
        /// <summary>The test data with TestData.Traits(): one acquired trait of each source, two from death's door, grit_t clashing with dull.</summary>
        static StaticData Data(params (string Key, int Value)[] balance)
        {
            return TestData.DataWithTraits(balance);
        }

        /// <summary>An expedition to the cave that ended on its boss floor, with the members named dead.</summary>
        static ExpeditionState Ended(StaticData data, RunState run, ExpeditionResult result, params string[] dead)
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == 3).Id;
            foreach (ExpeditionMember member in expedition.Members.Where(m => dead.Contains(m.MercenaryId)))
            {
                member.Alive = false;
                member.Hp = 0;
            }

            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = result;
            return expedition;
        }

        [Test]
        public void SourcesOf_AreTheResult_DeathsDoor_AndTheStateComeHomeIn_InThatOrder_AndNoneForTheDead()
        {
            StaticData data = Data();
            var member = new ExpeditionMember { MercenaryId = "anna", Alive = true };
            CollectionAssert.AreEqual(new[] { TraitSource.Clear }, TraitRules.SourcesOf(data, ExpeditionResult.Cleared, member));

            member.DeathsDoor = true;
            member.StateId = "fearful";
            CollectionAssert.AreEqual(new[] { TraitSource.Retreat, TraitSource.DeathsDoor, TraitSource.Affliction }, TraitRules.SourcesOf(data, ExpeditionResult.Retreated, member));

            member.StateId = "focused";
            CollectionAssert.AreEqual(new[] { TraitSource.Clear, TraitSource.DeathsDoor, TraitSource.Virtue }, TraitRules.SourcesOf(data, ExpeditionResult.Cleared, member),
                "A virtue lasts to the end of the expedition.");

            member.Alive = false;
            Assert.AreEqual(0, TraitRules.SourcesOf(data, ExpeditionResult.Wiped, member).Count);
        }

        [Test]
        public void DrawAcquired_GivesOneOfEachSourcesTraits_ByItsChance_NeverOneHeld_NorOneThatClashes()
        {
            StaticData data = Data(("TraitChanceClearPercent", 100), ("TraitChanceRetreatPercent", 100), ("TraitChanceDeathsDoorPercent", 100));
            List<string> drawn = TraitRules.DrawAcquired(data, new string[0], new[] { TraitSource.Clear, TraitSource.DeathsDoor }, new Pcg32(3, RngStream.Trait));
            Assert.AreEqual("vet_t", drawn[0]);
            Assert.AreEqual(2, drawn.Count);
            CollectionAssert.Contains(new[] { "scar_t", "lucky_t" }, drawn[1]);

            CollectionAssert.AreEqual(new[] { "lucky_t" }, TraitRules.DrawAcquired(data, new[] { "vet_t", "scar_t" }, new[] { TraitSource.Clear, TraitSource.DeathsDoor },
                new Pcg32(3, RngStream.Trait)), "The clear's one is held; death's door leaves the one not held.");

            // grit_t, the retreat's one, clashes with dull: nothing can come, so nothing is rolled — and so for a chance of 0 (the affliction's here).
            foreach ((string[] held, TraitSource source) in new[] { (new[] { "dull" }, TraitSource.Retreat), (new string[0], TraitSource.Affliction) })
            {
                var rng = new Pcg32(5, RngStream.Trait);
                var fresh = new Pcg32(5, RngStream.Trait);
                Assert.AreEqual(0, TraitRules.DrawAcquired(data, held, new[] { source }, rng).Count, source.ToString());
                Assert.AreEqual(fresh.NextInt(1000), rng.NextInt(1000), "Nothing was rolled.");
            }
        }

        [Test]
        public void DrawAcquired_HitsAboutAsOftenAsTheChance_ChoosesByWeight_AndIsTheSameForTheSameSeed()
        {
            StaticData data = Data(("TraitChanceDeathsDoorPercent", 25));
            var counts = new Dictionary<string, int> { { "scar_t", 0 }, { "lucky_t", 0 } };
            int hits = 0;
            for (ulong seed = 0; seed < 2000; seed++)
            {
                List<string> drawn = TraitRules.DrawAcquired(data, new string[0], new[] { TraitSource.DeathsDoor }, new Pcg32(seed, RngStream.Trait));
                CollectionAssert.AreEqual(drawn, TraitRules.DrawAcquired(data, new string[0], new[] { TraitSource.DeathsDoor }, new Pcg32(seed, RngStream.Trait)));
                hits += drawn.Count;
                foreach (string id in drawn)
                {
                    counts[id]++;
                }
            }

            Assert.That(hits, Is.InRange(400, 600), "About 25% of 2000.");
            Assert.That(counts["scar_t"], Is.InRange(hits / 2 - 70, hits / 2 + 70), "Weights 10 and 10.");
        }

        [Test]
        public void Settle_LeavesTheTraitsOnTheSurvivors_OnTheirOwnStream_AndTellsTheReport()
        {
            StaticData data = Data(("TraitChanceClearPercent", 100), ("TraitChanceDeathsDoorPercent", 100));
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            ExpeditionState expedition = Ended(data, run, ExpeditionResult.Cleared, "cora");
            expedition.Members.Single(m => m.MercenaryId == "anna").DeathsDoor = true;
            expedition.Members.Single(m => m.MercenaryId == "cora").DeathsDoor = true;

            SettlementReport report = RunRules.Settle(data, run, expedition);

            // Anna's draw is her own stream's for this expedition: whoever settles first, and whatever the barracks does, it is the same.
            List<string> anna = TraitRules.DrawAcquired(data, new string[0], new[] { TraitSource.Clear, TraitSource.DeathsDoor },
                TraitRules.AcquiredStream(run.Seed, "anna", run.ExpeditionCount));
            Assert.AreEqual(2, anna.Count);
            CollectionAssert.AreEqual(anna, RunRules.FindMercenary(run, "anna").Traits);
            CollectionAssert.AreEqual(new[] { "vet_t" }, RunRules.FindMercenary(run, "ben").Traits);
            CollectionAssert.AreEqual(new[] { "anna", "ben" }, report.NewTraits.Select(n => n.MercenaryId).ToList(), "The dead roll nothing.");
            CollectionAssert.AreEqual(anna, report.NewTraits[0].TraitIds);
        }

        [Test]
        public void Settle_WithNoChance_LeavesNothing_AndARetreatLeavesItsOwn()
        {
            StaticData none = Data();
            RunState run = RunRules.NewRun(none, 7);
            RunRules.SetParty(none, run, FlowTestKit.Party(("anna", 1), ("ben", 2)));
            Assert.AreEqual(0, RunRules.Settle(none, run, Ended(none, run, ExpeditionResult.Cleared)).NewTraits.Count);
            Assert.IsTrue(run.Roster.All(m => m.Traits.Count == 0));

            StaticData data = Data(("TraitChanceRetreatPercent", 100));
            run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2)));
            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated));
            Assert.IsTrue(report.NewTraits.All(n => n.TraitIds.SequenceEqual(new[] { "grit_t" })));
            Assert.AreEqual(2, report.NewTraits.Count);
        }

        [Test]
        public void CompleteBattle_MarksWhoWentToDeathsDoor()
        {
            // Unarmed, the front member takes the hits to death's door (where it cannot die here); the retreat ends the battle with both alive.
            StaticData data = Data(("RetreatChancePercent", 100), ("DogDeathChancePercent", 0));
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 5, new[] { new PartyMember("anna", "tank", 1), new PartyMember("ben", "healer", 2) });
            foreach (ExpeditionMember member in state.Members)
            {
                member.Board.Items.Clear();
            }

            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            for (int t = 500; !battle.Events.Any(e => e.Kind == BattleEventKind.DogEntered) && t < 600000; t += 500)
            {
                battle.AdvanceTo(t);
            }

            Assert.IsTrue(battle.TryRetreat());
            ExpeditionRules.CompleteBattle(data, state, battle);

            Assert.AreEqual(ExpeditionResult.Retreated, state.Result);
            Assert.IsTrue(state.Members.All(m => m.Alive));
            Assert.AreEqual((true, false), (state.Members[0].DeathsDoor, state.Members[1].DeathsDoor));
        }
    }
}
