using F1.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The mark of an item's tier on a piece (round 41): no mark for Common, one more star for each tier up.</summary>
    public sealed class TierStyleTests
    {
        [Test]
        public void Common_HasNoStars()
        {
            Assert.AreEqual(0, TierStyle.Stars(ItemTier.Common));
        }

        [Test]
        public void EachTierAboveCommon_DrawsOneMoreStar()
        {
            Assert.AreEqual(1, TierStyle.Stars(ItemTier.Bronze));
            Assert.AreEqual(2, TierStyle.Stars(ItemTier.Silver));
            Assert.AreEqual(3, TierStyle.Stars(ItemTier.Gold));
            Assert.AreEqual(TierStyle.MostStars, TierStyle.Stars(ItemTier.Gold), "The tag has room for the last tier's stars.");
        }

        [Test]
        public void TheTierTag_WidensByOnePitchPerStar()
        {
            Assert.AreEqual(TierStyle.StarPitch, TierStyle.TagWidth(2) - TierStyle.TagWidth(1), 0.001f);
            Assert.AreEqual(TierStyle.StarPitch, TierStyle.TagWidth(3) - TierStyle.TagWidth(2), 0.001f);
            Assert.Greater(TierStyle.TagWidth(1), TierStyle.StarSize + TierStyle.TagPad, "A star sits inside the pad.");
        }
    }
}
