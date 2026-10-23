using System.Linq;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The story's data (Slice D stage 55, Architecture/05 "이야기"): the scenes, the speakers, the dialogues and their lines go through the same
    /// CSV → JSON way as any definition, and what the story may not do is refused there, before the game reads it.
    /// </summary>
    public sealed class StoryDataTests
    {
        const string SceneHeader = "Id,Sequence,Order,Image,DurationMs,ShortDurationMs,ShortLines,CameraFrom,CameraTo,Effect,Transition,Countdown,Line1.ko-KR,Line1.en-US,Line2.ko-KR,Line2.en-US,Line3.ko-KR,Line3.en-US,SoundNote\n";

        static string Errors(StaticDataFiles.Entry file, string csv)
        {
            DataTransformException exception = Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(TestCsv.With(file, csv)));
            return string.Join("\n", exception.Errors);
        }

        [Test]
        public void Transform_ReadsTheStory_InItsOrders()
        {
            StaticData data = StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources())).Data;

            var tale = data.ScenesOf("tale");
            CollectionAssert.AreEqual(new[] { "tale_one", "tale_end" }, tale.Select(s => s.Id));
            StorySceneData one = tale[0];
            Assert.AreEqual("story/tale/one", one.Image);
            Assert.AreEqual(new[] { "One", "Two" }, one.Lines.Select(l => l.Resolve("en-US")).ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, one.ShortLines);
            Assert.AreEqual(120, one.CameraTo.Zoom);
            Assert.AreEqual(StoryEffect.None, one.Effect);
            Assert.AreEqual("Two", one.LinesOf(shortCut: true).Single().Resolve("en-US"));
            Assert.IsTrue(tale[1].IsLobbyBanner);
            Assert.IsTrue(tale[1].Countdown);
            CollectionAssert.AreEqual(new[] { 1 }, tale[1].ShortLines, "'all' is every line.");

            Assert.AreEqual("portrait/npc/clerk-smile", data.Npcs.Get("clerk").PortraitOf("smile"));
            Assert.IsNull(data.Npcs.Get("clerk").PortraitOf("angry"));
            CollectionAssert.AreEqual(new[] { "hello_1", "hello_2" }, data.LinesOf("hello").Select(l => l.Id));
        }

        [Test]
        public void Scenes_RefuseAGapInTheLines_AShortCutWithoutLines_AndABadCamera()
        {
            StringAssert.Contains("without a gap",
                Errors(StaticDataFiles.StoryScene, SceneHeader + "a,tale,10,story/tale/a,1000,0,,500:500:100,500:500:100,,Cross,false,하나,One,,,셋,Three,\n"));
            StringAssert.Contains("short cut",
                Errors(StaticDataFiles.StoryScene, SceneHeader + "a,tale,10,story/tale/a,1000,500,,500:500:100,500:500:100,,Cross,false,하나,One,,,,,\n"));
            StringAssert.Contains("x:y:zoom",
                Errors(StaticDataFiles.StoryScene, SceneHeader + "a,tale,10,story/tale/a,1000,0,,500-500,500:500:100,,Cross,false,하나,One,,,,,\n"));
            StringAssert.Contains("zoom",
                Errors(StaticDataFiles.StoryScene, SceneHeader + "a,tale,10,story/tale/a,1000,0,,500:500:90,500:500:100,,Cross,false,하나,One,,,,,\n"));
            StringAssert.Contains("'<'",
                Errors(StaticDataFiles.StoryScene, SceneHeader + "a,tale,10,story/tale/a,1000,0,,500:500:100,500:500:100,,Cross,false,<b>하나</b>,One,,,,,\n"));
        }

        [Test]
        public void Scenes_AllowTheBannerOnlyLast_AndOrdersOnce()
        {
            StringAssert.Contains("only the last scene",
                Errors(StaticDataFiles.StoryScene, SceneHeader +
                    "a,tale,10,,1000,0,,,,,Cross,true,하나,One,,,,,\n" +
                    "b,tale,20,story/tale/b,1000,0,,500:500:100,500:500:100,,Cross,false,둘,Two,,,,,\n"));
            StringAssert.Contains("share an Order",
                Errors(StaticDataFiles.StoryScene, SceneHeader +
                    "a,tale,10,story/tale/a,1000,0,,500:500:100,500:500:100,,Cross,false,하나,One,,,,,\n" +
                    "b,tale,10,story/tale/b,1000,0,,500:500:100,500:500:100,,Cross,false,둘,Two,,,,,\n"));
        }

        [Test]
        public void Lines_NeedASpeakerAndOneOfItsFaces_AndADialogueNeedsALine()
        {
            const string header = "Id,Dialogue,Order,Speaker,Expression,Text.ko-KR,Text.en-US\n";
            StringAssert.Contains("has no expression 'angry'",
                Errors(StaticDataFiles.DialogueLine, header + "hello_1,hello,10,clerk,angry,안녕,Hello\n"));
            StringAssert.Contains("Speaker: 'nobody' does not exist",
                Errors(StaticDataFiles.DialogueLine, header + "hello_1,hello,10,nobody,calm,안녕,Hello\n"));
            StringAssert.Contains("it has no line",
                Errors(StaticDataFiles.DialogueLine, header));
        }

        [Test]
        public void Dialogues_UnlockOnlyWithADungeonThatUnlocks()
        {
            const string header = "Id,Order,Trigger,Dungeon\n";
            StringAssert.Contains("open from the start",
                Errors(StaticDataFiles.Dialogue, header + "hello,10,DungeonUnlocked,mine\n"));
            StringAssert.Contains("only a DungeonUnlocked dialogue names a Dungeon",
                Errors(StaticDataFiles.Dialogue, header + "hello,10,RunStart,mine\n"));
        }

        [Test]
        public void Speakers_NeedAFace_AndNoFaceTwice()
        {
            const string header = "Id,Name.ko-KR,Name.en-US,Figure,Portraits\n";
            StringAssert.Contains("listed twice",
                Errors(StaticDataFiles.Npc, header + "clerk,서기,Clerk,,calm:portrait/npc/a+calm:portrait/npc/b\n"));
            StringAssert.Contains("expression:address",
                Errors(StaticDataFiles.Npc, header + "clerk,서기,Clerk,,calm\n"));
        }
    }
}
