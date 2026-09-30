using System;
using System.Collections.Generic;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class StaticDataLoaderTests
    {
        const string ValidJobs =
            "Id,Name.ko-KR,Name.en-US\n" +
            "knight,기사,Knight\n" +
            "bishop,주교,Bishop\n";

        static IReadOnlyDictionary<string, string> Generate()
        {
            return StaticDataTransformer.Transform(name => name == StaticDataFiles.Job.SourceFileName ? ValidJobs : null).GeneratedFiles;
        }

        static StaticData Load(IReadOnlyDictionary<string, string> generated, string jobJsonOverride = null)
        {
            return StaticDataLoader.Load(file =>
                jobJsonOverride != null && file == StaticDataFiles.Job ? jobJsonOverride : generated[file.GeneratedFileName]);
        }

        [Test]
        public void Load_FromGeneratedJson_ReturnsSameDataAsTransform()
        {
            StaticData data = Load(Generate());

            Assert.AreEqual(2, data.Jobs.Count);
            Assert.AreEqual("Knight", data.Job("knight").Name.Resolve("en-US"));
            Assert.AreEqual("주교", data.Job("bishop").Name.Resolve("ko-KR"));
        }

        [Test]
        public void Job_WhenIdUnknown_ThrowsInsteadOfReturningDefault()
        {
            StaticData data = Load(Generate());

            var exception = Assert.Throws<KeyNotFoundException>(() => data.Job("samurai"));
            StringAssert.Contains("samurai", exception.Message);
            Assert.Throws<KeyNotFoundException>(() => data.Job(null));
        }

        [Test]
        public void Load_WhenJsonHasUnknownProperty_Throws()
        {
            IReadOnlyDictionary<string, string> generated = Generate();
            string edited = generated[StaticDataFiles.Job.GeneratedFileName].Replace("\"Id\": \"knight\",", "\"Id\": \"knight\",\n      \"Power\": 3,");

            var exception = Assert.Throws<DataException>(() => Load(generated, edited));
            StringAssert.Contains(StaticDataFiles.Job.GeneratedFileName, exception.Message);
        }

        [Test]
        public void Load_WhenJsonIsMissingAProperty_Throws()
        {
            const string json = "{\n  \"SchemaVersion\": 1,\n  \"Items\": [\n    { \"Id\": \"knight\" }\n  ]\n}\n";

            Assert.Throws<DataException>(() => Load(Generate(), json));
        }

        [Test]
        public void Load_WhenSchemaVersionDiffers_Throws()
        {
            IReadOnlyDictionary<string, string> generated = Generate();
            string edited = generated[StaticDataFiles.Job.GeneratedFileName].Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 2");

            var exception = Assert.Throws<DataException>(() => Load(generated, edited));
            StringAssert.Contains("schema version", exception.Message);
        }

        [Test]
        public void Load_WhenJsonHasDuplicateIds_Throws()
        {
            IReadOnlyDictionary<string, string> generated = Generate();
            string edited = generated[StaticDataFiles.Job.GeneratedFileName].Replace("\"bishop\"", "\"knight\"");

            Assert.Throws<DataValidationException>(() => Load(generated, edited));
        }

        [Test]
        public void Load_WhenJsonRecordBreaksDefinitionRule_Throws()
        {
            IReadOnlyDictionary<string, string> generated = Generate();
            string edited = generated[StaticDataFiles.Job.GeneratedFileName].Replace("\"Id\": \"knight\"", "\"Id\": \"Knight\"");

            Assert.Throws<DataException>(() => Load(generated, edited));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{ \"SchemaVersion\": 1 }")]
        public void Load_WhenJsonIsUnreadable_Throws(string json)
        {
            Assert.Throws<DataException>(() => Load(Generate(), json));
        }

        [Test]
        public void Load_WhenReaderIsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StaticDataLoader.Load(null));
        }
    }
}
