using System;
using System.Collections.Generic;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class StaticDataTransformerTests
    {
        const string ValidJobs =
            "Id,Name.ko-KR,Name.en-US\n" +
            "knight,기사,Knight\n" +
            "bishop,주교,Bishop\n";

        static Func<string, string> Sources(string jobs)
        {
            var files = new Dictionary<string, string> { { StaticDataFiles.Job.SourceFileName, jobs } };
            return name => files.TryGetValue(name, out string text) ? text : null;
        }

        static DataTransformException TransformFails(string jobs)
        {
            return Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(Sources(jobs)));
        }

        [Test]
        public void Transform_WhenSourcesValid_BuildsDataAndOneFilePerDefinition()
        {
            TransformResult result = StaticDataTransformer.Transform(Sources(ValidJobs));

            Assert.AreEqual(2, result.Data.Jobs.Count);
            Assert.AreEqual("기사", result.Data.Job("knight").Name.Resolve("ko-KR"));
            Assert.AreEqual(StaticDataFiles.All.Count, result.GeneratedFiles.Count);
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(result.GeneratedFiles.ContainsKey(file.GeneratedFileName), file.GeneratedFileName);
            }
        }

        [Test]
        public void Transform_WritesItemsSortedByIdWithStableFormatting()
        {
            string json = StaticDataTransformer.Transform(Sources(ValidJobs)).GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];

            Assert.Less(json.IndexOf("\"bishop\"", StringComparison.Ordinal), json.IndexOf("\"knight\"", StringComparison.Ordinal));
            StringAssert.StartsWith("{\n  \"SchemaVersion\": 1,\n  \"Items\": [\n", json);
            StringAssert.EndsWith("}\n", json);
            Assert.IsFalse(json.Contains("\r"), "Generated JSON uses \\n only.");
            StringAssert.Contains("기사", json, "Non-ASCII text is written as is, not escaped.");
        }

        [Test]
        public void Transform_WhenRunTwice_ProducesIdenticalText()
        {
            string first = StaticDataTransformer.Transform(Sources(ValidJobs)).GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];
            string second = StaticDataTransformer.Transform(Sources(ValidJobs)).GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Transform_WhenColumnAndRowOrderChange_ProducesIdenticalText()
        {
            const string reordered =
                "Name.en-US,Id,Name.ko-KR\r\n" +
                "Bishop,bishop,주교\r\n" +
                "Knight,knight,기사\r\n";

            string expected = StaticDataTransformer.Transform(Sources(ValidJobs)).GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];
            string actual = StaticDataTransformer.Transform(Sources(reordered)).GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Transform_WhenSourceFileMissing_ReportsFileName()
        {
            var exception = Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(_ => null));

            StringAssert.Contains(StaticDataFiles.Job.SourceFileName, exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenRequiredHeaderMissing_ReportsHeader()
        {
            DataTransformException exception = TransformFails("Id,Name.ko-KR\nknight,기사\n");

            StringAssert.Contains("Name.en-US", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenHeaderUnknown_ReportsHeader()
        {
            DataTransformException exception = TransformFails("Id,Name.ko-KR,Name.en-US,Name.ja-JP\nknight,기사,Knight,騎士\n");

            StringAssert.Contains("Name.ja-JP", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenIdDuplicated_ReportsId()
        {
            DataTransformException exception = TransformFails(ValidJobs + "knight,기사 둘,Knight Two\n");

            StringAssert.Contains("duplicate id 'knight'", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenRowsInvalid_ReportsEveryRowWithFileAndLine()
        {
            DataTransformException exception = TransformFails(
                "Id,Name.ko-KR,Name.en-US\n" +
                "Knight,기사,Knight\n" +
                "bishop,,Bishop\n" +
                "paladin,성기사,TODO\n");

            Assert.AreEqual(3, exception.Errors.Count);
            StringAssert.StartsWith("JobData.csv(2)", exception.Errors[0]);
            StringAssert.StartsWith("JobData.csv(3)", exception.Errors[1]);
            StringAssert.StartsWith("JobData.csv(4)", exception.Errors[2]);
        }

        [Test]
        public void Transform_WhenCsvMalformed_ReportsLine()
        {
            DataTransformException exception = TransformFails("Id,Name.ko-KR,Name.en-US\nknight,기사\n");

            StringAssert.Contains("JobData.csv(2)", exception.Errors[0]);
        }
    }
}
