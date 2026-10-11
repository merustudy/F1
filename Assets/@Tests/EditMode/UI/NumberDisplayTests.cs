using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Slice B stage 22 (Docs/Architecture/12_UI.md "수의 표시"): numbers in hundredths are shown whole.</summary>
    public sealed class NumberDisplayTests
    {
        [Test]
        public void AnAmount_IsRoundedHalfUp()
        {
            Assert.AreEqual(2, UiText.Amount(249));
            Assert.AreEqual(3, UiText.Amount(250));
            Assert.AreEqual(0, UiText.Amount(49), "Under 0.50 shows 0 (and a number that rounds to 0 does not rise).");
        }

        [Test]
        public void Hp_IsRoundedHalfUp_ButAtLeastOneWhileThereIsAny()
        {
            Assert.AreEqual(38, UiText.Hp(3818));
            Assert.AreEqual(1, UiText.Hp(30), "0.30 HP is not death's door: it shows 1.");
            Assert.AreEqual(0, UiText.Hp(0), "0 is death's door or death.");
        }

        [Test]
        public void ARange_ShowsBothEndsRounded_OrOneNumberWhenTheyMeet()
        {
            Assert.AreEqual("2-4", UiText.Range(200, 400));
            Assert.AreEqual("3-9", UiText.Range(311, 933));
            Assert.AreEqual("1-2", UiText.Range(100, 200), "The dagger.");
            Assert.AreEqual("1", UiText.Range(67, 133), "Both ends round to 1.");
        }
    }
}
