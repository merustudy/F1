using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// Where the units of one side stand (Docs/Design/02_Combat_System.md §2). A side is a line:
    /// one unit per row, from row 1 back, with no empty row between them. When a unit leaves, the
    /// ones behind it advance. The lobby party, the expedition party and the battle all follow
    /// this, so the rule is written here once.
    ///
    /// Every function takes the rows of the standing units only (the living, the chosen).
    /// </summary>
    public static class Formation
    {
        /// <summary>Why these rows are not a legal line, or null when they are.</summary>
        public static string Problem(IReadOnlyList<int> rows)
        {
            var taken = new bool[BattleRows.Count + 1];
            foreach (int row in rows)
            {
                if (!BattleRows.IsValid(row))
                {
                    return $"Row {row} is outside {BattleRows.Front}..{BattleRows.Count}.";
                }

                if (taken[row])
                {
                    return $"Row {row} holds more than one.";
                }

                taken[row] = true;
            }

            for (int row = BattleRows.Front + 1; row <= BattleRows.Count; row++)
            {
                if (taken[row] && !taken[row - 1])
                {
                    return $"Row {row} is occupied but row {row - 1} in front of it is empty.";
                }
            }

            return null;
        }

        /// <summary>
        /// The advance rule: every unit moves forward over the empty rows in front of it. Units keep
        /// their order. Returns true when someone moved.
        /// </summary>
        public static bool CloseGaps(int[] rows)
        {
            var taken = new bool[BattleRows.Count + 1];
            foreach (int row in rows)
            {
                taken[row] = true;
            }

            var newRow = new int[BattleRows.Count + 1];
            int next = BattleRows.Front;
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                if (taken[row])
                {
                    newRow[row] = next++;
                }
            }

            bool moved = false;
            for (int i = 0; i < rows.Length; i++)
            {
                moved |= newRow[rows[i]] != rows[i];
                rows[i] = newRow[rows[i]];
            }

            return moved;
        }

        /// <summary>A unit from outside can only take the first empty row: the one right behind the line.</summary>
        public static bool CanJoin(IReadOnlyList<int> rows, int row)
        {
            return BattleRows.IsValid(row) && row == rows.Count + 1;
        }

        /// <summary>A unit of the line can move only to a row where someone else stands.</summary>
        public static bool CanMove(IReadOnlyList<int> rows, int index, int row)
        {
            return rows[index] != row && IndexOf(rows, row) >= 0;
        }

        /// <summary>Moves a unit to a row: it trades places with the unit standing there.</summary>
        public static void Move(int[] rows, int index, int row)
        {
            int other = IndexOf(rows, row);
            rows[other] = rows[index];
            rows[index] = row;
        }

        static int IndexOf(IReadOnlyList<int> rows, int row)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == row)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
