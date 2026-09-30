using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class CsvTableTests
    {
        const string Source = "Test.csv";

        [Test]
        public void Parse_ReadsHeadersAndRowsWithLineNumbers()
        {
            CsvTable table = CsvTable.Parse("Id,Value\na,1\nb,2\n", Source);

            CollectionAssert.AreEqual(new[] { "Id", "Value" }, table.Headers);
            Assert.AreEqual(2, table.Rows.Count);
            Assert.AreEqual("a", table.Rows[0].Text("Id"));
            Assert.AreEqual(2, table.Rows[0].Line);
            Assert.AreEqual("2", table.Rows[1].Text("Value"));
            Assert.AreEqual(3, table.Rows[1].Line);
        }

        [Test]
        public void Parse_WhenFieldIsQuoted_KeepsCommaQuoteAndLineBreak()
        {
            CsvTable table = CsvTable.Parse("Id,Text\na,\"one, \"\"two\"\"\r\nthree\"\nb,plain\n", Source);

            Assert.AreEqual("one, \"two\"\nthree", table.Rows[0].Text("Text"));
            Assert.AreEqual("plain", table.Rows[1].Text("Text"));
            Assert.AreEqual(4, table.Rows[1].Line, "A line break inside a quoted field still advances the line number.");
        }

        [Test]
        public void Parse_AcceptsBomCrLfBlankLinesAndMissingFinalNewline()
        {
            CsvTable table = CsvTable.Parse("﻿Id,Value\r\n\r\na,1\r\n\r\nb,2", Source);

            Assert.AreEqual("Id", table.Headers[0]);
            Assert.AreEqual(2, table.Rows.Count);
            Assert.AreEqual("2", table.Rows[1].Text("Value"));
        }

        [Test]
        public void Parse_KeepsTrailingEmptyField()
        {
            CsvTable table = CsvTable.Parse("Id,Value\na,", Source);

            Assert.IsTrue(table.Rows[0].IsEmpty("Value"));
        }

        [Test]
        public void Parse_WhenTextIsNull_ReportsMissingFile()
        {
            var exception = Assert.Throws<DataException>(() => CsvTable.Parse(null, Source));

            StringAssert.Contains(Source, exception.Message);
            StringAssert.Contains("missing", exception.Message);
        }

        [Test]
        public void Parse_WhenEmpty_ReportsMissingHeaderRow()
        {
            Assert.Throws<DataException>(() => CsvTable.Parse("", Source));
        }

        [Test]
        public void Parse_WhenRowHasWrongFieldCount_ReportsFileAndLine()
        {
            var exception = Assert.Throws<DataException>(() => CsvTable.Parse("Id,Value\na,1\nb\n", Source));

            StringAssert.Contains("Test.csv(3)", exception.Message);
        }

        [TestCase("Id,Id\na,b\n")]
        [TestCase("Id,\na,b\n")]
        [TestCase("Id, Value\na,b\n")]
        public void Parse_WhenHeaderIsDuplicateEmptyOrPadded_Throws(string text)
        {
            Assert.Throws<DataException>(() => CsvTable.Parse(text, Source));
        }

        [TestCase("Id,Text\na,\"open\n")]
        [TestCase("Id,Text\na,\"closed\"tail\n")]
        [TestCase("Id,Text\na,mid\"quote\n")]
        public void Parse_WhenQuotingIsBroken_Throws(string text)
        {
            Assert.Throws<DataException>(() => CsvTable.Parse(text, Source));
        }

        [Test]
        public void RequireHeaders_WhenRequiredHeaderMissing_Throws()
        {
            CsvTable table = CsvTable.Parse("Id\na\n", Source);

            var exception = Assert.Throws<DataException>(() => table.RequireHeaders(new[] { "Id", "Value" }));
            StringAssert.Contains("'Value'", exception.Message);
        }

        [Test]
        public void RequireHeaders_WhenUnknownHeaderPresent_Throws()
        {
            CsvTable table = CsvTable.Parse("Id,Valeu\na,1\n", Source);

            var exception = Assert.Throws<DataException>(() => table.RequireHeaders(new[] { "Id" }, new[] { "Value" }));
            StringAssert.Contains("'Valeu'", exception.Message);
        }

        [Test]
        public void RequireHeaders_AcceptsOptionalHeaders()
        {
            CsvTable table = CsvTable.Parse("Value,Id\n1,a\n", Source);

            Assert.DoesNotThrow(() => table.RequireHeaders(new[] { "Id" }, new[] { "Value", "Other" }));
        }
    }
}
