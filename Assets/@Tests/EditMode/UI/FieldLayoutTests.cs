using F1.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Where the columns of a field stand: each side together, the two sides apart.</summary>
    public sealed class FieldLayoutTests
    {
        const float FieldWidth = 1680f;
        const int EnemyColumns = BattleRows.Count;

        [Test]
        public void Columns_FillTheField_FromItsLeftEdgeToItsRight([Range(1, BattleRows.Count)] int partyColumns)
        {
            float width = FieldLayout.ColumnWidth(FieldWidth, partyColumns, EnemyColumns);

            Assert.Greater(width, 0f);
            Assert.AreEqual(0f, FieldLayout.PartyColumnX(width, partyColumns, partyColumns - 1), 0.001f, "The party's rearmost row starts at the field's left edge.");
            Assert.AreEqual(FieldWidth, FieldLayout.EnemyColumnX(width, partyColumns, EnemyColumns - 1) + width, 0.001f, "The enemy's rearmost row ends at its right edge.");
        }

        [Test]
        public void Rows_StandOutwardsFromTheMiddle_Row1NextToIt([Range(1, BattleRows.Count)] int partyColumns)
        {
            float width = FieldLayout.ColumnWidth(FieldWidth, partyColumns, EnemyColumns);

            for (int row = 1; row < partyColumns; row++)
            {
                Assert.Less(FieldLayout.PartyColumnX(width, partyColumns, row), FieldLayout.PartyColumnX(width, partyColumns, row - 1), "A party row stands to the left of the row in front of it.");
            }

            for (int row = 1; row < EnemyColumns; row++)
            {
                Assert.Greater(FieldLayout.EnemyColumnX(width, partyColumns, row), FieldLayout.EnemyColumnX(width, partyColumns, row - 1), "An enemy row stands to the right of the row in front of it.");
            }

            Assert.Less(FieldLayout.PartyColumnX(width, partyColumns, 0), FieldLayout.EnemyColumnX(width, partyColumns, 0), "The party is on the left.");
        }

        [Test]
        public void ASide_StandsCloserTogether_ThanTheTwoSidesStandApart([Range(1, BattleRows.Count)] int partyColumns)
        {
            float width = FieldLayout.ColumnWidth(FieldWidth, partyColumns, EnemyColumns);

            float insideASide = FieldLayout.EnemyColumnX(width, partyColumns, 1) - (FieldLayout.EnemyColumnX(width, partyColumns, 0) + width);
            float betweenTheSides = FieldLayout.EnemyColumnX(width, partyColumns, 0) - (FieldLayout.PartyColumnX(width, partyColumns, 0) + width);

            Assert.AreEqual(FieldLayout.ColumnGap, insideASide, 0.001f);
            Assert.AreEqual(FieldLayout.SideGap, betweenTheSides, 0.001f);
            Assert.Greater(betweenTheSides, insideASide, "The wider gap between the sides is what tells them apart.");
            Assert.AreEqual(FieldLayout.SideWidth(width, partyColumns), FieldLayout.PartyColumnX(width, partyColumns, 0) + width, 0.001f, "The party's side is as wide as its columns and the gaps between them.");
        }
    }
}
