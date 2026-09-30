using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class LocalePolicyTests
    {
        [Test]
        public void DefaultCode_IsSupported()
        {
            Assert.IsTrue(LocalePolicy.IsSupported(LocalePolicy.DefaultCode));
        }

        [Test]
        public void SupportedCodes_AreKoreanAndEnglish()
        {
            CollectionAssert.AreEqual(new[] { "ko-KR", "en-US" }, LocalePolicy.SupportedCodes);
        }

        [TestCase("ko-KR", true)]
        [TestCase("en-US", true)]
        [TestCase("KO-kr", false)]
        [TestCase("ja-JP", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsSupported_MatchesCanonicalCodesOnly(string code, bool expected)
        {
            Assert.AreEqual(expected, LocalePolicy.IsSupported(code));
        }

        [TestCase("KO-kr", "ko-KR")]
        [TestCase("en-us", "en-US")]
        [TestCase("en-US", "en-US")]
        public void TryNormalize_WhenOnlyCaseDiffers_ReturnsCanonical(string code, string expected)
        {
            Assert.IsTrue(LocalePolicy.TryNormalize(code, out string canonical));
            Assert.AreEqual(expected, canonical);
        }

        [TestCase("ja-JP")]
        [TestCase("ko")]
        [TestCase("")]
        [TestCase(null)]
        public void TryNormalize_WhenUnsupported_ReturnsFalse(string code)
        {
            Assert.IsFalse(LocalePolicy.TryNormalize(code, out string canonical));
            Assert.IsNull(canonical);
        }
    }
}
