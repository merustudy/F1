using System.Collections.Generic;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class CsvRowTests
    {
        enum Sample
        {
            Alpha,
            Beta,
        }

        static CsvRow Row(string header, string value)
        {
            string quoted = "\"" + value.Replace("\"", "\"\"") + "\"";
            return CsvTable.Parse($"{header}\n{quoted}\n", "Test.csv").Rows[0];
        }

        [TestCase("0", 0)]
        [TestCase("42", 42)]
        [TestCase("-7", -7)]
        public void Int_WhenPlainInteger_Parses(string text, int expected)
        {
            Assert.AreEqual(expected, Row("N", text).Int("N"));
        }

        [TestCase(" 5")]
        [TestCase("5 ")]
        [TestCase("+5")]
        [TestCase("1,000")]
        [TestCase("5.0")]
        [TestCase("5e2")]
        [TestCase("0x10")]
        [TestCase("five")]
        [TestCase("99999999999")]
        [TestCase("")]
        public void Int_WhenNotAPlainInteger_Throws(string text)
        {
            var exception = Assert.Throws<DataException>(() => Row("N", text).Int("N"));

            StringAssert.Contains("Test.csv(2) [N]", exception.Message);
        }

        [Test]
        public void Int_WhenOutsideRange_Throws()
        {
            Assert.Throws<DataException>(() => Row("N", "0").Int("N", min: 1));
            Assert.Throws<DataException>(() => Row("N", "11").Int("N", min: 1, max: 10));
            Assert.AreEqual(10, Row("N", "10").Int("N", min: 1, max: 10));
        }

        [Test]
        public void OptionalInt_WhenEmpty_ReturnsDefault()
        {
            Assert.AreEqual(3, Row("N", "").OptionalInt("N", 3));
            Assert.AreEqual(8, Row("N", "8").OptionalInt("N", 3));
        }

        [Test]
        public void Bool_AcceptsOnlyLowercaseTrueAndFalse()
        {
            Assert.IsTrue(Row("B", "true").Bool("B"));
            Assert.IsFalse(Row("B", "false").Bool("B"));
            foreach (string text in new[] { "True", "1", "yes", "" })
            {
                Assert.Throws<DataException>(() => Row("B", text).Bool("B"), text);
            }
        }

        [Test]
        public void Enum_AcceptsOnlyDefinedNames()
        {
            Assert.AreEqual(Sample.Beta, Row("E", "Beta").Enum<Sample>("E"));
            foreach (string text in new[] { "beta", "1", "Gamma", "Alpha,Beta", "" })
            {
                Assert.Throws<DataException>(() => Row("E", text).Enum<Sample>("E"), text);
            }
        }

        [TestCase("knight", true)]
        [TestCase("abyss_lord", true)]
        [TestCase("tier2_sword", true)]
        [TestCase("Knight", false)]
        [TestCase("abyss lord", false)]
        [TestCase("abyss-lord", false)]
        [TestCase("_knight", false)]
        [TestCase("knight_", false)]
        [TestCase("2sword", false)]
        [TestCase("기사", false)]
        public void Id_AcceptsOnlyEnglishSnakeCase(string text, bool valid)
        {
            if (valid)
            {
                Assert.AreEqual(text, Row("Id", text).Id("Id"));
            }
            else
            {
                Assert.Throws<DataException>(() => Row("Id", text).Id("Id"));
            }
        }

        [Test]
        public void Rows_ReadsASpanOfTheLine_AndOptionalRowsTakesAnEmptyCell()
        {
            Assert.AreEqual("front:2", Row("Rows", "front:2").Rows("Rows").ToString());
            Assert.AreEqual("back:3", Row("Rows", "back:3").Rows("Rows").ToString());
            Assert.IsTrue(Row("Rows", "all").Rows("Rows").IsEveryRow);
            Assert.IsNull(Row("Rows", "").OptionalRows("Rows"));
            Assert.AreEqual("back:1", Row("Rows", "back:1").OptionalRows("Rows").ToString());

            foreach (string text in new[] { "", "1", "1+2", "front", "front:0", "back:5", "middle:2", "front: 2" })
            {
                var exception = Assert.Throws<DataException>(() => Row("Rows", text).Rows("Rows"), text);
                StringAssert.Contains("Test.csv(2) [Rows]", exception.Message);
            }
        }

        [Test]
        public void IdList_SplitsOnPlus_AndEmptyCellIsEmptyList()
        {
            CollectionAssert.AreEqual(new List<string> { "sword", "shield" }, Row("Items", "sword+shield").IdList("Items"));
            CollectionAssert.IsEmpty(Row("Items", "").IdList("Items"));
            Assert.Throws<DataException>(() => Row("Items", "sword+").IdList("Items"));
            Assert.Throws<DataException>(() => Row("Items", "sword+Shield").IdList("Items"));
        }

        [Test]
        public void Text_WhenEmptyOrPadded_Throws()
        {
            Assert.Throws<DataException>(() => Row("T", "").Text("T"));
            Assert.Throws<DataException>(() => Row("T", " padded").Text("T"));
            Assert.AreEqual("two words", Row("T", "two words").Text("T"));
        }

        [Test]
        public void Localized_ReadsEverySupportedLocale()
        {
            CsvRow row = CsvTable.Parse("Name.ko-KR,Name.en-US\n기사,Knight\n", "Test.csv").Rows[0];

            LocalizedText text = row.Localized("Name");

            Assert.AreEqual("기사", text.Resolve("ko-KR"));
            Assert.AreEqual("Knight", text.Resolve("en-US"));
        }

        [Test]
        public void Localized_WhenLocaleHeaderMissing_Throws()
        {
            CsvRow row = CsvTable.Parse("Name.ko-KR\n기사\n", "Test.csv").Rows[0];

            var exception = Assert.Throws<DataException>(() => row.Localized("Name"));
            StringAssert.Contains("Name.en-US", exception.Message);
        }

        [Test]
        public void Contextualize_AddsFileAndRowOnlyWhenMissing()
        {
            CsvRow row = Row("Id", "knight");

            Assert.AreEqual("Test.csv(2): broken", row.Contextualize("broken"));
            Assert.AreEqual("Test.csv(2) [Id]: broken", row.Contextualize("Test.csv(2) [Id]: broken"));
        }
    }
}
