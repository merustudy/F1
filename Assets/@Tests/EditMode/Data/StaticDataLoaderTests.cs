using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class StaticDataLoaderTests
    {
        static IReadOnlyDictionary<string, string> Generate()
        {
            return StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources())).GeneratedFiles;
        }

        /// <summary>Loads the generated files, optionally replacing the JSON of one definition.</summary>
        static StaticData Load(IReadOnlyDictionary<string, string> generated, StaticDataFiles.Entry replaced = null, string json = null)
        {
            return StaticDataLoader.Load(file => file == replaced ? json : generated[file.GeneratedFileName]);
        }

        static string Json(StaticDataFiles.Entry file)
        {
            return Generate()[file.GeneratedFileName];
        }

        [Test]
        public void Load_FromGeneratedJson_ReturnsTheSameDataAsTheTransform()
        {
            StaticData data = Load(Generate());

            Assert.AreEqual(2, data.Jobs.Count);
            Assert.AreEqual("Knight", data.Jobs.Get("knight").Name.Resolve("en-US"));
            Assert.AreEqual(PassiveEffect.Shield, data.Jobs.Get("knight").Passive.Effect);
            Assert.IsNull(data.Jobs.Get("bishop").Passive);
            Assert.AreEqual(2, data.Items.Get("mace").Effects.Count);
            Assert.AreEqual(8, data.Enemies.Get("ogre").Items[1].Grade);
            Assert.AreEqual(30, data.Balance.DogDeathChancePercent);
            CollectionAssert.AreEqual(new[] { "tonic" }, data.Dungeons.Get("mine").StartingPotions);
        }

        [Test]
        public void SerializeThenLoad_IsByteStable()
        {
            IReadOnlyDictionary<string, string> generated = Generate();

            Dictionary<string, string> again = StaticDataLoader.Serialize(Load(generated));

            foreach (KeyValuePair<string, string> file in generated)
            {
                Assert.AreEqual(file.Value, again[file.Key], file.Key);
            }
        }

        [Test]
        public void Tables_IterateInIdOrder_AndThrowForUnknownIds()
        {
            StaticData data = Load(Generate());

            CollectionAssert.AreEqual(new[] { "claw", "mace", "staff", "sword" }, data.Items.Ordered.Select(item => item.Id));
            Assert.IsTrue(data.Items.Contains("mace"));
            Assert.IsFalse(data.Items.Contains("katana"));
            Assert.IsFalse(data.Items.Contains(null));
            var exception = Assert.Throws<KeyNotFoundException>(() => data.Items.Get("katana"));
            StringAssert.Contains("katana", exception.Message);
            Assert.Throws<KeyNotFoundException>(() => data.Jobs.Get(null));
        }

        [Test]
        public void Load_WhenJsonHasUnknownProperty_Throws()
        {
            string edited = Json(StaticDataFiles.Potion).Replace("\"Id\": \"tonic\",", "\"Id\": \"tonic\",\n      \"Power\": 3,");

            var exception = Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, edited));
            StringAssert.Contains(StaticDataFiles.Potion.GeneratedFileName, exception.Message);
        }

        [Test]
        public void Load_WhenJsonIsMissingAProperty_Throws()
        {
            const string json = "{\n  \"SchemaVersion\": 1,\n  \"Items\": [\n    { \"Id\": \"tonic\" }\n  ]\n}\n";

            Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, json));
        }

        [Test]
        public void Load_WhenSchemaVersionDiffers_Throws()
        {
            string edited = Json(StaticDataFiles.Potion).Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 2");

            var exception = Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, edited));
            StringAssert.Contains("schema version", exception.Message);
        }

        [Test]
        public void Load_WhenJsonRecordBreaksADefinitionRule_ThrowsWithTheFileName()
        {
            string edited = Json(StaticDataFiles.Potion).Replace("\"Magnitude\": 50", "\"Magnitude\": 0");

            var exception = Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, edited));
            StringAssert.Contains(StaticDataFiles.Potion.GeneratedFileName, exception.Message);
            StringAssert.Contains("Magnitude", exception.Message);
        }

        [Test]
        public void Load_WhenEnumNameIsUnknown_Throws()
        {
            string edited = Json(StaticDataFiles.Potion).Replace("\"Effect\": \"Heal\"", "\"Effect\": \"Explode\"");

            Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, edited));
        }

        [Test]
        public void Load_WhenLocalizedTextIsIncomplete_Throws()
        {
            string edited = Json(StaticDataFiles.Potion).Replace("\"en-US\": \"Tonic\",", "");

            Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, edited));
        }

        [Test]
        public void Load_WhenAReferenceIsBroken_ThrowsValidationException()
        {
            string edited = Json(StaticDataFiles.Mercenary).Replace("\"JobId\": \"knight\"", "\"JobId\": \"samurai\"");

            var exception = Assert.Throws<DataValidationException>(() => Load(Generate(), StaticDataFiles.Mercenary, edited));
            StringAssert.Contains("samurai", exception.Problems[0]);
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{ \"SchemaVersion\": 1 }")]
        [TestCase("{ \"SchemaVersion\": 1, \"Items\": [ null ] }")]
        public void Load_WhenJsonIsUnreadable_Throws(string json)
        {
            Assert.Throws<DataException>(() => Load(Generate(), StaticDataFiles.Potion, json));
        }

        [Test]
        public void Load_WhenReaderIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StaticDataLoader.Load(null));
        }
    }
}
