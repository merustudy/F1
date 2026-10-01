using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Where units may stand (Docs/Design/02_Combat_System.md §2): a line of one unit per row from
    /// row 1 back, with no empty row between them.
    /// </summary>
    public sealed class FormationTests
    {
        [Test]
        public void Problem_IsNull_ForALineFilledFromTheFront()
        {
            Assert.IsNull(Formation.Problem(new int[0]));
            Assert.IsNull(Formation.Problem(new[] { 1 }));
            Assert.IsNull(Formation.Problem(new[] { 2, 1 }), "The order of the units does not matter.");
            Assert.IsNull(Formation.Problem(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Problem_NamesARowOutOfRange_ARowTakenTwice_AndAGap()
        {
            StringAssert.Contains("Row 0", Formation.Problem(new[] { 0 }));
            StringAssert.Contains($"Row {BattleRows.Count + 1}", Formation.Problem(new[] { 1, BattleRows.Count + 1 }));
            StringAssert.Contains("Row 1 holds more than one", Formation.Problem(new[] { 1, 1 }));
            StringAssert.Contains("row 1 in front of it is empty", Formation.Problem(new[] { 2 }));
            StringAssert.Contains("row 2 in front of it is empty", Formation.Problem(new[] { 1, 3 }));
        }

        [Test]
        public void CloseGaps_MovesEveryoneForwardOverEmptyRows_KeepingTheirOrder()
        {
            int[] rows = { 3, 2 };
            Assert.IsTrue(Formation.CloseGaps(rows));
            CollectionAssert.AreEqual(new[] { 2, 1 }, rows);

            rows = new[] { 4 };
            Assert.IsTrue(Formation.CloseGaps(rows));
            CollectionAssert.AreEqual(new[] { 1 }, rows);

            rows = new[] { 1, 4, 3 };
            Assert.IsTrue(Formation.CloseGaps(rows));
            CollectionAssert.AreEqual(new[] { 1, 3, 2 }, rows);
        }

        [Test]
        public void CloseGaps_WhenThereIsNoGap_ChangesNothing()
        {
            int[] rows = { 2, 1, 3 };

            Assert.IsFalse(Formation.CloseGaps(rows));

            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, rows);
            Assert.IsFalse(Formation.CloseGaps(new int[0]));
        }

        [Test]
        public void CanJoin_OnlyTheFirstEmptyRow()
        {
            Assert.IsTrue(Formation.CanJoin(new int[0], 1));
            Assert.IsFalse(Formation.CanJoin(new int[0], 2), "Row 1 would stay empty in front.");

            int[] two = { 2, 1 };
            Assert.IsFalse(Formation.CanJoin(two, 1), "Taken.");
            Assert.IsFalse(Formation.CanJoin(two, 2), "Taken.");
            Assert.IsTrue(Formation.CanJoin(two, 3));
            Assert.IsFalse(Formation.CanJoin(two, 4), "Row 3 would stay empty in front.");
            Assert.IsFalse(Formation.CanJoin(two, 0));
        }

        [Test]
        public void CanJoin_AFullLine_TakesNobody()
        {
            int[] full = { 1, 2, 3, 4 };

            for (int row = 0; row <= BattleRows.Count + 1; row++)
            {
                Assert.IsFalse(Formation.CanJoin(full, row), $"Row {row}");
            }
        }

        [Test]
        public void CanMove_OnlyToAnotherRowWhereSomeoneElseStands()
        {
            int[] rows = { 1, 2 };

            Assert.IsTrue(Formation.CanMove(rows, 0, 2));
            Assert.IsTrue(Formation.CanMove(rows, 1, 1));
            Assert.IsFalse(Formation.CanMove(rows, 0, 1), "Already there.");
            Assert.IsFalse(Formation.CanMove(rows, 0, 3), "Nobody stands in row 3.");
            Assert.IsFalse(Formation.CanMove(rows, 1, 0));
            Assert.IsFalse(Formation.CanMove(new[] { 1 }, 0, 2), "Alone.");
        }

        [Test]
        public void Move_TradesPlacesWithWhoStandsThere()
        {
            int[] rows = { 1, 2, 3 };

            Formation.Move(rows, 0, 3);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, rows);

            Formation.Move(rows, 1, 1);
            CollectionAssert.AreEqual(new[] { 3, 1, 2 }, rows);

            Assert.IsNull(Formation.Problem(rows), "Trading places never leaves a gap.");
        }
    }
}
