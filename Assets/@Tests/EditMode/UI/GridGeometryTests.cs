using F1.Gameplay;
using F1.UI;
using NUnit.Framework;
using UnityEngine;

namespace F1.Tests
{
    /// <summary>
    /// Where a held thing lands with the pointer at a point of a grid (round 54, Diablo II: its centre on the pointer, on the nearest
    /// squares, pulled in to fit), and the point of a grid under a screen position (Docs/Architecture/12_UI.md "격자 보드").
    /// </summary>
    public sealed class GridGeometryTests
    {
        [TestCase(1.5f, 0.5f, 3, 1, 0, 0, 0, TestName = "Three across, pointer on the middle square: its first square left of it")]
        [TestCase(2.5f, 1.5f, 3, 1, 0, 0, 1, TestName = "Three across near the frame's right: pulled in")]
        [TestCase(1.5f, 0.5f, 2, 1, 0, 1, 0, TestName = "Two across, pointer on a square's centre: a tie goes right")]
        [TestCase(1.2f, 0.5f, 2, 1, 0, 0, 0, TestName = "Two across, pointer left of the line: the squares either side of the line nearest")]
        [TestCase(1.8f, 0.5f, 2, 1, 0, 1, 0, TestName = "Two across, pointer right of the middle: the next squares")]
        [TestCase(0.5f, 3.5f, 3, 1, 1, 0, 2, TestName = "Turned a quarter, three down: centred on the pointer's row")]
        [TestCase(1.0f, 1.0f, 2, 2, 0, 0, 0, TestName = "Two by two, pointer on the crossing of the lines")]
        public void TheCentre_IsOnThePointer_OnTheNearestSquares_PulledIntoTheFrame(float px, float py, int width, int height, int turns, int x, int y)
        {
            Assert.AreEqual(new Placement(x, y, turns), GridGeometry.Anchor(new Vector2(px, py), width, height, turns, 3, 8));
        }

        [Test]
        public void TheInventory_PullsIn_ByItsOwnSize()
        {
            Assert.AreEqual(new Placement(7, 2), GridGeometry.Anchor(new Vector2(9.9f, 2.9f), 3, 1, 0, 10, 3));
            Assert.AreEqual(new Placement(0, 0), GridGeometry.Anchor(new Vector2(-0.4f, -0.4f), 2, 1, 0, 10, 3), "A near miss above and left is pulled in.");
        }

        [Test]
        public void ASquaresCentre_IsItsIndexAndAHalf_AndTheLineBetweenTwo_IsWhole()
        {
            var go = new GameObject("Squares", typeof(RectTransform));
            try
            {
                var area = (RectTransform)go.transform;
                area.pivot = new Vector2(0f, 1f);
                area.sizeDelta = new Vector2(GridGeometry.Width, GridGeometry.Height);
                area.position = Vector3.zero;

                Vector2 firstCentre = new Vector2(GridGeometry.Square / 2f, -GridGeometry.Square / 2f);
                Assert.IsTrue(GridGeometry.PointIn(area, firstCentre, null, 0f, out Vector2 point));
                Assert.AreEqual(0.5f, point.x, 1e-4f);
                Assert.AreEqual(0.5f, point.y, 1e-4f);

                Vector2 lineAfterFirst = new Vector2(GridGeometry.Square + GridGeometry.Gap / 2f, -GridGeometry.Square / 2f);
                Assert.IsTrue(GridGeometry.PointIn(area, lineAfterFirst, null, 0f, out point));
                Assert.AreEqual(1f, point.x, 1e-4f);

                Vector2 leftOutside = new Vector2(-20f, -10f);
                Assert.IsFalse(GridGeometry.PointIn(area, leftOutside, null, 16f, out _), "Farther out than the margin: not over the grid.");
                Assert.IsTrue(GridGeometry.PointIn(area, leftOutside, null, 25f, out point), "Within the margin: over it, pulled in later.");
                Assert.Less(point.x, 0f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
