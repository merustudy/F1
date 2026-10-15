using F1.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The mark of an item's tier on a piece (round 41; round 69 pips): no mark for Common, one more pip for each tier up.</summary>
    public sealed class TierStyleTests
    {
        [Test]
        public void Common_HasNoPips()
        {
            Assert.AreEqual(0, TierStyle.Pips(ItemTier.Common));
        }

        [Test]
        public void EachTierAboveCommon_DrawsOneMorePip()
        {
            Assert.AreEqual(1, TierStyle.Pips(ItemTier.Tier1));
            Assert.AreEqual(2, TierStyle.Pips(ItemTier.Tier2));
            Assert.AreEqual(3, TierStyle.Pips(ItemTier.Tier3));
            Assert.AreEqual(TierStyle.MostPips, TierStyle.Pips(ItemTier.Tier3), "The tag has room for the last tier's pips.");
        }

        [Test]
        public void TheTierTag_WidensByOnePitchPerPip()
        {
            Assert.AreEqual(TierStyle.PipPitch, TierStyle.TagWidth(2) - TierStyle.TagWidth(1), 0.001f);
            Assert.AreEqual(TierStyle.PipPitch, TierStyle.TagWidth(3) - TierStyle.TagWidth(2), 0.001f);
            Assert.Greater(TierStyle.TagWidth(1), TierStyle.PipSize + TierStyle.TagPad, "A pip sits inside the pad.");
        }
    }
}
