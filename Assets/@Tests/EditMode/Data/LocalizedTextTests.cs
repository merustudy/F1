using System.Collections.Generic;
using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class LocalizedTextTests
    {
        static Dictionary<string, string> Values(string korean, string english)
        {
            return new Dictionary<string, string> { { "ko-KR", korean }, { "en-US", english } };
        }

        [Test]
        public void Resolve_ReturnsTextOfRequestedLocale()
        {
            var text = new LocalizedText(Values("기사", "Knight"));

            Assert.AreEqual("기사", text.Resolve("ko-KR"));
            Assert.AreEqual("Knight", text.Resolve("en-US"));
            Assert.IsTrue(text.Has("en-US"));
        }

        [TestCase("ja-JP")]
        [TestCase("")]
        [TestCase(null)]
        public void Resolve_WhenLocaleUnknown_FallsBackToDefaultLocale(string localeCode)
        {
            var text = new LocalizedText(Values("기사", "Knight"));

            Assert.AreEqual(text.Resolve(LocalePolicy.DefaultCode), text.Resolve(localeCode));
            Assert.IsFalse(text.Has(localeCode));
        }

        [Test]
        public void Constructor_WhenLocaleMissing_Throws()
        {
            var values = new Dictionary<string, string> { { "ko-KR", "기사" } };

            var exception = Assert.Throws<DataException>(() => new LocalizedText(values));
            StringAssert.Contains("en-US", exception.Message);
        }

        [Test]
        public void Constructor_WhenUnsupportedLocalePresent_Throws()
        {
            Dictionary<string, string> values = Values("기사", "Knight");
            values.Add("ja-JP", "騎士");

            var exception = Assert.Throws<DataException>(() => new LocalizedText(values));
            StringAssert.Contains("ja-JP", exception.Message);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("TODO")]
        [TestCase("tbd")]
        [TestCase("[MISSING]")]
        public void Constructor_WhenValueIsEmptyOrPlaceholder_Throws(string value)
        {
            Assert.Throws<DataException>(() => new LocalizedText(Values("기사", value)));
        }

        [Test]
        public void Constructor_WhenValuesNull_Throws()
        {
            Assert.Throws<DataException>(() => new LocalizedText(null));
        }
    }
}
