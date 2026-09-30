using System.Linq;
using F1.Data;
using F1.Editor.Setup;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class UiStringSourceTests
    {
        const string Header = "Key,Shared Comments,ko-KR,en-US\n";

        static UiStringSource Parse(string rows)
        {
            return UiStringSource.Parse(Header + rows, "UI_StaticText.csv");
        }

        static string ErrorOf(string csv)
        {
            return Assert.Throws<DataException>(() => UiStringSource.Parse(csv, "UI_StaticText.csv")).Message;
        }

        [Test]
        public void Parse_WhenValid_ReadsKeyCommentAndEveryLocale()
        {
            UiStringSource source = Parse("Title.NewRun,Starts a fresh run.,새 런,New Run\nCommon.Confirm,,확인,OK\n");

            Assert.AreEqual(2, source.Rows.Count);
            UiStringRow first = source.Rows[0];
            Assert.AreEqual("Title.NewRun", first.Key);
            Assert.AreEqual("Starts a fresh run.", first.Comment);
            Assert.AreEqual("새 런", first.Values["ko-KR"]);
            Assert.AreEqual("New Run", first.Values["en-US"]);
            Assert.AreEqual(string.Empty, source.Rows[1].Comment, "A comment is optional.");
        }

        [Test]
        public void Parse_WhenLocaleColumnMissing_Throws()
        {
            StringAssert.Contains("en-US", ErrorOf("Key,Shared Comments,ko-KR\nTitle.NewRun,,새 런\n"));
        }

        [Test]
        public void Parse_WhenUnsupportedLocaleColumn_Throws()
        {
            StringAssert.Contains("ja-JP", ErrorOf("Key,Shared Comments,ko-KR,en-US,ja-JP\nTitle.NewRun,,새 런,New Run,新規\n"));
        }

        [TestCase("title.NewRun")]
        [TestCase("Title.newRun")]
        [TestCase("NewRun")]
        [TestCase("Title.New_Run")]
        [TestCase("Title..NewRun")]
        [TestCase("Title.NewRun.")]
        public void Parse_WhenKeyIsNotPascalCaseSegments_Throws(string key)
        {
            StringAssert.Contains(key, ErrorOf(Header + key + ",,새 런,New Run\n"));
        }

        [Test]
        public void Parse_WhenKeyHasThreeSegments_IsValid()
        {
            Assert.AreEqual("Battle.Result.Victory", Parse("Battle.Result.Victory,,승리,Victory\n").Rows[0].Key);
        }

        [Test]
        public void Parse_WhenKeyIsDuplicated_Throws()
        {
            StringAssert.Contains("duplicate", ErrorOf(Header + "Title.NewRun,,새 런,New Run\nTitle.NewRun,,새 런,New Run\n"));
        }

        [Test]
        public void Parse_WhenTranslationIsEmpty_Throws()
        {
            StringAssert.Contains("en-US", ErrorOf(Header + "Title.NewRun,,새 런,\n"));
        }

        [TestCase("TODO")]
        [TestCase("tbd")]
        [TestCase("[Missing]")]
        public void Parse_WhenTranslationIsPlaceholder_Throws(string placeholder)
        {
            StringAssert.Contains("placeholder", ErrorOf(Header + "Title.NewRun,,새 런," + placeholder + "\n"));
        }

        [Test]
        public void Parse_WhenSeveralRowsAreWrong_ReportsAllOfThem()
        {
            string message = ErrorOf(Header + "bad,,새 런,New Run\nTitle.NewRun,,,New Run\n");

            Assert.AreEqual(2, message.Split('\n').Length, message);
        }

        [TestCase("Language: {0}", true)]
        [TestCase("New Run", false)]
        public void IsSmart_IsTrueOnlyForTextWithPlaceholders(string value, bool expected)
        {
            Assert.AreEqual(expected, UiStringSource.IsSmart(value));
        }

        [Test]
        public void Parse_WhenQuotedValueContainsComma_KeepsIt()
        {
            UiStringSource source = Parse("Battle.Log,,\"{0}, {1} 피해\",\"{0}, {1} damage\"\n");

            Assert.AreEqual("{0}, {1} 피해", source.Rows.Single().Values["ko-KR"]);
        }
    }
}
