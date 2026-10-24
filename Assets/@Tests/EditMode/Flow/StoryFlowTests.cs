using System.Collections.Generic;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The story through the run manager and the save (Slice D stage 55, Architecture/07 "이야기의 표시"): a dialogue heard is saved with the run and
    /// comes back after a restart; a dialogue the data does not know is refused; a run of version 27 has heard every dialogue.
    /// </summary>
    public sealed class StoryFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        static StaticData Data()
        {
            return new StaticData(TestData.WithStory(TestData.Parts()));
        }

        [Test]
        public void MarkDialogueSeen_SavesIt_AndTheNextDueIsNone_AfterARestartToo()
        {
            var kit = new FlowTestKit(Data()).InLobby();
            Assert.AreEqual("hello", kit.Run.DueDialogue().Id);

            kit.Run.MarkDialogueSeen("hello");

            Assert.IsNull(kit.Run.DueDialogue());
            Assert.AreEqual("hello", kit.Run.LastHeardDialogue().Id);
            StringAssert.Contains("\"SeenDialogues\"", kit.SavedText());
            FlowTestKit again = kit.Restart();
            CollectionAssert.AreEqual(new[] { "hello" }, again.Run.Run.SeenDialogues);
            Assert.IsNull(again.Run.DueDialogue());
        }

        [Test]
        public void DueDialogue_IsNoneWhileThePartyIsAway()
        {
            var kit = new FlowTestKit(Data()).InLobby();
            kit.Run.MarkDialogueSeen("hello");
            kit.Run.Run.ClearedDungeons["cave"] = 1;
            Assert.AreEqual("pit_open", kit.Run.DueDialogue().Id);

            kit.Expedition.Depart("cave");

            Assert.IsNull(kit.Run.DueDialogue(), "A dialogue waits for the lobby.");
        }

        [Test]
        public void Read_RefusesAnUnknownDialogue_AndOneListedTwice()
        {
            StaticData data = Data();
            RunState run = RunRules.NewRun(data, 7);
            RunSaveData save = RunSaveMapper.ToSave(run, null);

            save.Run.SeenDialogues = new List<string> { "nobody" };
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _));
            save.Run.SeenDialogues = new List<string> { "hello", "hello" };
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _));
            save.Run.SeenDialogues = null;
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _));
        }

        [Test]
        public void Migrate_From27_HasHeardEveryDialogue()
        {
            StaticData data = Data();
            RunSaveData save = RunSaveMapper.ToSave(RunRules.NewRun(data, 7), null);
            save.SchemaVersion = 27;
            save.Run.SeenDialogues = null;

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion, "Brought on through 28 to the current version.");
            CollectionAssert.AreEqual(new[] { "hello", "pit_open" }, save.Run.SeenDialogues, "In the dialogues' order.");
            RunSaveMapper.Read(save, data, out RunState read, out ExpeditionState _);
            Assert.IsNull(StoryRules.DueDialogue(data, read), "No dialogue breaks into a run already going on.");
        }
    }
}
