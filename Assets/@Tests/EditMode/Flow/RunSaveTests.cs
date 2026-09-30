using System;
using System.IO;
using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Saving and continuing (Docs/Architecture/07_SAVE.md; Docs/Architecture/09_VERTICAL_SLICE.md, Acceptance 7).
    /// "Restart" closes the app and boots it again over the same save directory.
    /// </summary>
    public sealed class RunSaveTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        /// <summary>The state in memory, written the way it is saved. Two kits in the same state give the same text.</summary>
        static string Snapshot(FlowTestKit kit)
        {
            ExpeditionState expedition = kit.Expedition.Expedition;
            ExpeditionRecord record = expedition == null
                ? null
                : RunSaveMapper.ToRecord(expedition, expedition.Phase == ExpeditionPhase.InBattle ? kit.Expedition.Battle.Engine : null);
            return System.Text.Encoding.UTF8.GetString(SaveManager.Serialize(RunSaveMapper.ToSave(kit.Run.Run, record)));
        }

        static BattleEvent FirstPartyEvent(FlowTestKit kit, BattleEventKind kind)
        {
            return kit.Expedition.Battle.Engine.Events.FirstOrDefault(e => e.Kind == kind && e.Target.Side == BattleSide.Party && !e.Target.IsNone);
        }

        /// <summary>Makes every write fail: the save directory becomes a file.</summary>
        static void BreakSaving(FlowTestKit kit)
        {
            Directory.Delete(kit.SaveRoot, true);
            File.WriteAllText(kit.SaveRoot, "blocked");
        }

        static void RepairSaving(FlowTestKit kit)
        {
            File.Delete(kit.SaveRoot);
            Directory.CreateDirectory(kit.SaveRoot);
        }

        // ---- The run -------------------------------------------------------------------------

        [Test]
        public void Load_WhenNothingWasSaved_LeavesNoRun()
        {
            FlowTestKit kit = new FlowTestKit().Restart();

            Assert.AreEqual(SaveLoadStatus.Missing, kit.Run.LoadStatus);
            Assert.IsFalse(kit.Run.HasRun);
            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
        }

        [Test]
        public void ANewRun_IsSavedAtOnce_AndComesBackAfterARestart()
        {
            var kit = new FlowTestKit(seed: 4242);
            kit.Run.StartNewRun();

            Assert.IsTrue(File.Exists(kit.RunFilePath));
            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(SaveLoadStatus.Loaded, restarted.Run.LoadStatus);
            Assert.IsTrue(restarted.Run.HasRun);
            Assert.AreEqual(4242UL, restarted.Run.Run.Seed);
            Assert.AreEqual(Snapshot(kit), Snapshot(restarted));
        }

        [Test]
        public void LobbyCommands_AreSaved()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            kit.Run.Rest();
            RunRules.FindMercenary(kit.Run.Run, "anna").Fatigue = 55;
            kit.Run.SetParty(FlowTestKit.Party(("dan", BattleRow.Front), ("ben", BattleRow.Rear)));

            FlowTestKit restarted = kit.Restart();

            RunState run = restarted.Run.Run;
            Assert.AreEqual(2, run.Day);
            Assert.AreEqual(55, RunRules.FindMercenary(run, "anna").Fatigue);
            CollectionAssert.AreEqual(new[] { "dan", "ben" }, run.Party.Select(p => p.MercenaryId));
            Assert.AreEqual(BattleRow.Rear, run.Party[1].Row);
            Assert.IsFalse(restarted.Run.IsAway);
        }

        [Test]
        public void ANewRun_OverASavedExpedition_ReplacesIt()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();

            kit.Run.StartNewRun();
            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(GamePhase.Lobby, restarted.Expedition.Phase);
            Assert.IsNull(restarted.Expedition.Expedition);
            Assert.AreEqual(0, restarted.Run.Run.ExpeditionCount);
            Assert.IsFalse(restarted.Run.IsAway);
        }

        // ---- The expedition ------------------------------------------------------------------

        [Test]
        public void Restart_OnTheNodeMap_ComesBackToTheSameExpedition()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            kit.Expedition.SwapItems(0, 0, 0, 2);
            kit.Expedition.SetRow(1, BattleRow.Front);

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(GamePhase.NodeMap, restarted.Expedition.Phase);
            Assert.IsTrue(restarted.Run.IsAway);
            Assert.AreEqual(BattleResume.None, restarted.Expedition.LastResume);
            Assert.AreEqual(Snapshot(kit), Snapshot(restarted));
            CollectionAssert.AreEqual(
                kit.Expedition.AvailableNodes().Select(n => n.Id),
                restarted.Expedition.AvailableNodes().Select(n => n.Id),
                "The map is rebuilt from the expedition seed.");
        }

        [Test]
        public void Restart_AtTheRewardChoice_OffersTheSameRewards_AndTakingOneIsSaved()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.FightToTheEnd();
            kit.Expedition.CloseBattle();

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(GamePhase.Reward, restarted.Expedition.Phase);
            Assert.IsNull(restarted.Expedition.Battle, "An ended battle is not brought back.");
            Assert.AreEqual(Snapshot(kit), Snapshot(restarted));

            int item = restarted.Expedition.Expedition.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            string itemId = restarted.Expedition.Expedition.PendingRewards[item].Id;
            restarted.Expedition.TakeItemReward(item, 1, 2);
            FlowTestKit again = restarted.Restart();

            Assert.AreEqual(GamePhase.NodeMap, again.Expedition.Phase);
            Assert.AreEqual(itemId, again.Expedition.Expedition.Members[1].Items[2].Item.Id);
        }

        [Test]
        public void WhenTheExpeditionEnds_TheSettledRunIsSaved_AndNoExpeditionRemains()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            kit.PlayExpeditionToTheEnd();

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(GamePhase.Lobby, restarted.Expedition.Phase, "The report is not saved; the settlement already is.");
            Assert.IsNull(restarted.Expedition.Report);
            Assert.AreEqual(3, restarted.Run.Run.Day);
            Assert.AreEqual(70, RunRules.FindMercenary(restarted.Run.Run, "anna").Fatigue);
            Assert.AreEqual(1, restarted.Run.Run.ClearedDungeons["cave"]);
        }

        // ---- The battle ----------------------------------------------------------------------

        [Test]
        public void EnteringANode_SavesABattleWithNoInputs()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(GamePhase.Battle, restarted.Expedition.Phase);
            Assert.AreEqual(BattleResume.Exact, restarted.Expedition.LastResume);
            Assert.AreEqual(0, restarted.Expedition.Battle.Engine.TimeMs);
            Assert.AreEqual(kit.Expedition.Battle.Node.Id, restarted.Expedition.Battle.Node.Id);
        }

        [Test]
        public void TimePassingAlone_IsNotSaved()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            string before = kit.SavedText();

            kit.Expedition.AdvanceBattle(900);

            Assert.AreEqual(before, kit.SavedText());
        }

        [Test]
        public void Restart_InTheMiddleOfABattle_ReplaysItUpToTheLastInput()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(1000);
            Assert.IsTrue(kit.Expedition.TryUsePotion(0, 1));
            kit.Expedition.AdvanceBattle(500);

            FlowTestKit restarted = kit.Restart();
            BattleEngine engine = restarted.Expedition.Battle.Engine;

            Assert.AreEqual(BattleResume.Exact, restarted.Expedition.LastResume);
            Assert.AreEqual(1000, engine.TimeMs, "The half second after the potion was never confirmed; it is played again.");
            Assert.AreEqual(1, engine.Inputs.Count);
            Assert.IsNull(engine.Potions[0], "The potion stays used.");

            restarted.Expedition.AdvanceBattle(500);
            Assert.AreEqual(Snapshot(kit), Snapshot(restarted), "Played forward to the same time, the two battles are the same.");

            kit.FightToTheEnd();
            restarted.FightToTheEnd();
            Assert.AreEqual(BattleLog.Hash(kit.Expedition.Battle.Engine.Events), BattleLog.Hash(restarted.Expedition.Battle.Engine.Events));
        }

        [Test]
        public void AMercenaryFalling_IsSavedTheMomentItHappens()
        {
            FlowTestKit kit = new FlowTestKit(FlowTestKit.DeadlyEnemies()).InBattle();
            while (FirstPartyEvent(kit, BattleEventKind.DogEntered) == null)
            {
                kit.Expedition.AdvanceBattle(100);
            }

            int fellAtStep = kit.Expedition.Battle.Engine.TimeMs;
            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(fellAtStep, restarted.Expedition.Battle.Engine.TimeMs);
            Assert.IsTrue(restarted.Expedition.Battle.Engine.Party[0].InDog);
        }

        [Test]
        public void ADeath_OnceSaved_CannotBeUndoneByRestarting()
        {
            FlowTestKit kit = new FlowTestKit(FlowTestKit.DeadlyEnemies()).InBattle();
            while (FirstPartyEvent(kit, BattleEventKind.Died) == null)
            {
                kit.Expedition.AdvanceBattle(100);
            }

            BattleEvent death = FirstPartyEvent(kit, BattleEventKind.Died);
            FlowTestKit restarted = kit.Restart();
            BattleEngine engine = restarted.Expedition.Battle.Engine;

            Assert.GreaterOrEqual(engine.TimeMs, death.TimeMs, "The battle comes back at or after the death.");
            Assert.IsFalse(engine.Unit(death.Target).Alive, "The mercenary is dead again.");
            Assert.IsFalse(restarted.Expedition.TryUsePotion(0, death.Target.Index), "A potion cannot reach the dead.");

            string deadId = engine.Unit(death.Target).Setup.SourceId;
            restarted.PlayExpeditionToTheEnd();
            CollectionAssert.DoesNotContain(restarted.Run.Run.Roster.Select(m => m.Id), deadId);
            CollectionAssert.Contains(restarted.Run.Run.Fallen, deadId);
        }

        [Test]
        public void AFailedRetreat_IsSaved_AndARestartDoesNotGiveANewRoll()
        {
            // With a 1% chance the first attempt fails unless the roll is exactly 0.
            StaticData data = FlowTestKit.DeadlyEnemies(("RetreatChancePercent", 1), ("RetreatCooldownMs", 1000));
            FlowTestKit kit = new FlowTestKit(data).InBattle();
            kit.Expedition.AdvanceBattle(200);

            Assert.IsTrue(kit.Expedition.TryRetreat());
            Assert.IsFalse(kit.Expedition.Battle.IsFinished, "The first roll of this seed is not 0.");
            FlowTestKit restarted = kit.Restart();

            BattleEngine replayed = restarted.Expedition.Battle.Engine;
            Assert.AreEqual(1, replayed.Inputs.Count, "The failed attempt is part of the record.");
            Assert.AreEqual(kit.Expedition.Battle.Engine.RetreatReadyMs, replayed.RetreatReadyMs, "The cooldown is running again.");
            Assert.IsFalse(restarted.Expedition.TryRetreat(), "Restarting does not buy an attempt.");

            kit.Expedition.AdvanceBattle(1000);
            restarted.Expedition.AdvanceBattle(1000);
            Assert.IsTrue(kit.Expedition.TryRetreat());
            Assert.IsTrue(restarted.Expedition.TryRetreat());
            BattleEvent original = kit.Expedition.Battle.Engine.Events.Last(e => e.Kind == BattleEventKind.RetreatAttempted);
            BattleEvent again = replayed.Events.Last(e => e.Kind == BattleEventKind.RetreatAttempted);
            Assert.AreEqual(original.B, again.B, "The next attempt rolls the same number either way.");
        }

        [Test]
        public void RestartingAfterEveryStep_GivesTheSameExpeditionAsPlayingStraightThrough()
        {
            FlowTestKit straight = new FlowTestKit().OnNodeMap();
            straight.PlayExpeditionToTheEnd();

            FlowTestKit stepped = new FlowTestKit().OnNodeMap();
            int guard = 0;
            while (stepped.Expedition.Phase != GamePhase.Settlement)
            {
                Assert.Less(++guard, 200);
                stepped = stepped.Restart();
                switch (stepped.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        stepped.Expedition.EnterNode(stepped.Expedition.AvailableNodes()[0].Id);
                        break;
                    case GamePhase.Battle:
                        // Time that passes is not saved, so a battle is fought in one sitting here.
                        stepped.FightToTheEnd();
                        stepped.Expedition.CloseBattle();
                        break;
                    case GamePhase.Reward:
                        stepped.Expedition.SkipReward();
                        break;
                }
            }

            Assert.AreEqual(Snapshot(straight), Snapshot(stepped));
            Assert.AreEqual(straight.Expedition.Report.DayAfter, stepped.Expedition.Report.DayAfter);
        }

        // ---- Files that cannot be used -------------------------------------------------------

        [Test]
        public void Load_WhenTheFileIsGarbage_AndThereIsNoBackup_LeavesNoRun_ButANewRunStillWorks()
        {
            var kit = new FlowTestKit();
            File.WriteAllText(kit.RunFilePath, "{{{ not json");

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(SaveLoadStatus.Corrupt, restarted.Run.LoadStatus);
            Assert.IsFalse(restarted.Run.HasRun);

            restarted.Run.StartNewRun();
            Assert.AreEqual(SaveLoadStatus.Loaded, restarted.Restart().Run.LoadStatus);
        }

        [Test]
        public void Load_WhenTheFileIsGarbage_FallsBackToThePreviousSave()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            kit.Run.Rest();
            File.WriteAllText(kit.RunFilePath, "{{{ not json");

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, restarted.Run.LoadStatus);
            Assert.AreEqual(1, restarted.Run.Run.Day, "The backup is the save before the rest.");
            Assert.AreEqual(3, restarted.Run.Run.Party.Count);
        }

        [Test]
        public void Load_WhenTheFileDescribesAnIllegalState_TreatsItAsUnreadable()
        {
            var kit = new FlowTestKit();
            kit.Run.StartNewRun();
            File.WriteAllText(kit.RunFilePath, kit.SavedText().Replace("\"anna\"", "\"nobody\""));
            File.Delete(kit.RunFilePath + SaveStorage.BackupSuffix);

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(SaveLoadStatus.Corrupt, restarted.Run.LoadStatus);
            Assert.IsFalse(restarted.Run.HasRun);
        }

        [TestCase(0)]
        [TestCase(RunSaveData.CurrentSchemaVersion + 1)]
        public void Load_WhenTheSchemaVersionCannotBeRead_Refuses(int version)
        {
            var kit = new FlowTestKit();
            kit.Run.StartNewRun();
            string text = kit.SavedText();
            StringAssert.Contains("\"SchemaVersion\": 1", text);
            File.WriteAllText(kit.RunFilePath, text.Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": " + version));

            Assert.AreEqual(SaveLoadStatus.Corrupt, kit.Restart().Run.LoadStatus);
        }

        [Test]
        public void Restore_WhenARecordedInputNoLongerFits_FightsTheBattleAgainFromTheStart()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(1000);
            kit.Expedition.TryUsePotion(0, 1);
            string text = kit.SavedText();
            StringAssert.Contains("\"PotionSlot\": 0", text);
            File.WriteAllText(kit.RunFilePath, text.Replace("\"PotionSlot\": 0", "\"PotionSlot\": 2"));

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(BattleResume.Restarted, restarted.Expedition.LastResume);
            Assert.AreEqual(0, restarted.Expedition.Battle.Engine.TimeMs);
            Assert.IsEmpty(restarted.Expedition.Battle.Engine.Inputs);
            Assert.IsNotNull(restarted.Expedition.Battle.Engine.Potions[0], "Nothing of the old battle was applied, so the potion is back.");
            Assert.AreEqual(BattleResume.Exact, restarted.Restart().Expedition.LastResume, "The restarted battle was saved at once.");
        }

        [Test]
        public void Restore_WhenTheLogDigestDiffers_KeepsWhatTheReplayGives()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(1000);
            kit.Expedition.TryUsePotion(0, 1);
            var saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            saved.Expedition.Battle.LogHash = "1";
            kit.Save.Save(RunManager.FileName, saved);

            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(BattleResume.Diverged, restarted.Expedition.LastResume);
            Assert.AreEqual(1000, restarted.Expedition.Battle.Engine.TimeMs);
            Assert.AreEqual(1, restarted.Expedition.Battle.Engine.Inputs.Count);
        }

        // ---- When saving fails ---------------------------------------------------------------

        [Test]
        public void WhenSavingFails_TheChangeIsKeptInMemory_AndEverythingElseIsBlocked()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            bool? blocked = null;
            kit.Run.SaveBlockedChanged += value => blocked = value;
            BreakSaving(kit);

            Assert.DoesNotThrow(() => kit.Run.Rest());

            Assert.IsTrue(kit.Run.IsSaveBlocked);
            Assert.AreEqual(true, blocked);
            Assert.AreEqual(2, kit.Run.Run.Day, "The change happened; it is just not confirmed.");
            Assert.Throws<InvalidOperationException>(() => kit.Run.Rest());
            Assert.Throws<InvalidOperationException>(() => kit.Run.SetParty(FlowTestKit.DefaultParty()));
            Assert.Throws<InvalidOperationException>(() => kit.Run.StartNewRun());
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.Depart("cave"));
            Assert.AreEqual(2, kit.Run.Run.Day, "Nothing changed while blocked.");
        }

        [Test]
        public void RetrySave_WritesTheSnapshotThatFailed_AndUnblocks()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            BreakSaving(kit);
            kit.Run.Rest();
            string expected = Snapshot(kit);
            bool? blocked = null;
            kit.Run.SaveBlockedChanged += value => blocked = value;

            Assert.IsFalse(kit.Run.RetrySave(), "Still broken.");
            Assert.IsTrue(kit.Run.IsSaveBlocked);

            RepairSaving(kit);
            Assert.IsTrue(kit.Run.RetrySave());

            Assert.IsFalse(kit.Run.IsSaveBlocked);
            Assert.AreEqual(false, blocked);
            Assert.AreEqual(expected, kit.SavedText());
            Assert.AreEqual(2, kit.Restart().Run.Run.Day);
            Assert.DoesNotThrow(() => kit.Run.Rest(), "Commands work again.");
        }

        [Test]
        public void WhenSavingFailsAndTheAppCloses_TheLastSavedStateComesBack()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            string lastGood = kit.SavedText();
            BreakSaving(kit);
            kit.Run.Rest();

            // The disk is fine again the next time the app starts, but the rest was never confirmed.
            RepairSaving(kit);
            File.WriteAllText(kit.RunFilePath, lastGood);
            FlowTestKit restarted = kit.Restart();

            Assert.AreEqual(1, restarted.Run.Run.Day);
        }

        [Test]
        public void WhileBlocked_TheBattleDoesNotMove_AndTakesNoInput()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(500);
            BreakSaving(kit);

            Assert.IsTrue(kit.Expedition.TryUsePotion(0, 0), "The input was accepted; saving it failed.");
            Assert.IsTrue(kit.Run.IsSaveBlocked);

            kit.Expedition.AdvanceBattle(5000);
            Assert.AreEqual(500, kit.Expedition.Battle.Engine.TimeMs);
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TryRetreat());

            RepairSaving(kit);
            Assert.IsTrue(kit.Run.RetrySave());
            kit.Expedition.AdvanceBattle(100);
            Assert.AreEqual(600, kit.Expedition.Battle.Engine.TimeMs);
            Assert.AreEqual(1, kit.Restart().Expedition.Battle.Engine.Inputs.Count, "The retried save holds the potion input.");
        }
    }
}
