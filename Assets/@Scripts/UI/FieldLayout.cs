namespace F1.UI
{
    /// <summary>
    /// Where the columns of a field stand. A field holds one column per row of each side: the
    /// party's rows on the left with row 1 next to the middle, the enemy's rows on the right, every
    /// column the same width. The battle screen lays its field out with this when it opens, and the
    /// party side of the node map and the reward screen puts its columns in the same places, so the
    /// party keeps the battle's shape on every screen whatever the party size is. Rows are given as
    /// indices (0 is row 1) and every x is measured from the field's left edge.
    /// </summary>
    public static class FieldLayout
    {
        /// <summary>Space between two columns of one side.</summary>
        public const float ColumnGap = 10f;

        /// <summary>Space between the two sides: between their row 1 columns.</summary>
        public const float SideGap = 40f;

        /// <summary>The width of one column of a field that holds this many columns on the two sides together.</summary>
        public static float ColumnWidth(float fieldWidth, int partyColumns, int enemyColumns)
        {
            int columns = partyColumns + enemyColumns;
            return (fieldWidth - SideGap - ColumnGap * (columns - 2)) / columns;
        }

        /// <summary>How wide a side of this many columns is, the gaps between them included.</summary>
        public static float SideWidth(float columnWidth, int columns)
        {
            return columnWidth * columns + ColumnGap * (columns - 1);
        }

        /// <summary>The left edge of a party column. Row 1 stands next to the middle, the rows behind it to its left.</summary>
        public static float PartyColumnX(float columnWidth, int partyColumns, int rowIndex)
        {
            return (columnWidth + ColumnGap) * (partyColumns - 1 - rowIndex);
        }

        /// <summary>The left edge of an enemy column. Row 1 stands next to the middle, the rows behind it to its right.</summary>
        public static float EnemyColumnX(float columnWidth, int partyColumns, int rowIndex)
        {
            return SideWidth(columnWidth, partyColumns) + SideGap + (columnWidth + ColumnGap) * rowIndex;
        }
    }
}
