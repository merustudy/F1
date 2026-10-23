using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The story's dialogues in the lobby (Slice D stage 55, Docs/Design/33 §12): the run's start brings its dialogue first, a dungeon's unlocking
    /// brings its own once the dungeon is open, each once a run, in the dialogues' order; the last heard is the one repeated; the weeks left count down.
    /// </summary>
    public sealed class StoryRulesTests
    {
        static StaticData Data()
        {
            return new StaticData(TestData.WithStory(TestData.Parts()));
        }

        [Test]
        public void DueDialogue_IsTheRunStartFirst_ThenTheUnlockOnceThePitIsOpen_EachOnce()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);

            Assert.AreEqual("hello", StoryRules.DueDialogue(data, run).Id);
            StoryRules.MarkSeen(data, run, "hello");
            Assert.IsNull(StoryRules.DueDialogue(data, run), "The pit is still locked.");

            run.ClearedDungeons["cave"] = 1;
            Assert.AreEqual("pit_open", StoryRules.DueDialogue(data, run).Id);
            StoryRules.MarkSeen(data, run, "pit_open");
            StoryRules.MarkSeen(data, run, "pit_open");
            Assert.IsNull(StoryRules.DueDialogue(data, run));
            CollectionAssert.AreEqual(new[] { "hello", "pit_open" }, run.SeenDialogues, "Heard in order, each once.");
        }

        [Test]
        public void DueDialogue_ComesInTheDataOrder_AndAnEndedRunHasNone()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);
            run.ClearedDungeons["cave"] = 1;

            Assert.AreEqual("hello", StoryRules.DueDialogue(data, run).Id, "The dialogue of order 10 comes before the one of order 20.");
            run.Outcome = RunOutcome.Victory;
            Assert.IsNull(StoryRules.DueDialogue(data, run));
        }

        [Test]
        public void LastHeard_IsTheHeardOneLastInOrder()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);
            Assert.IsNull(StoryRules.LastHeard(data, run));

            StoryRules.MarkSeen(data, run, "pit_open");
            StoryRules.MarkSeen(data, run, "hello");

            Assert.AreEqual("pit_open", StoryRules.LastHeard(data, run).Id, "By the dialogues' order, not by when it was heard.");
        }

        [Test]
        public void MarkSeen_RefusesADialogueThatDoesNotExist()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);

            Assert.Throws<System.ArgumentException>(() => StoryRules.MarkSeen(data, run, "nobody"));
        }

        [Test]
        public void WeeksLeft_CountsTheLastWeek_AndNeverGoesUnderZero()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);
            int total = data.Balance.TotalDays;

            Assert.AreEqual(total, StoryRules.WeeksLeft(data, run));
            run.Day = total;
            Assert.AreEqual(1, StoryRules.WeeksLeft(data, run));
            run.Day = total + 3;
            Assert.AreEqual(0, StoryRules.WeeksLeft(data, run));
        }

        [Test]
        public void WeeksLeftOn_TellsAnyWeek_TheDayOfTheEclipseIsZero()
        {
            StaticData data = Data();
            int total = data.Balance.TotalDays;

            Assert.AreEqual(total - 1, StoryRules.WeeksLeftOn(data, 2), "An expedition from the first week is back with one week fewer.");
            Assert.AreEqual(1, StoryRules.WeeksLeftOn(data, total));
            Assert.AreEqual(0, StoryRules.WeeksLeftOn(data, total + 1));
        }
    }
}
