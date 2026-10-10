using F1.UI;
using NUnit.Framework;
using UnityEngine;

namespace F1.Tests
{
    /// <summary>
    /// Where an item's card goes (round 42): above the board panel over the cell's column on the party side, beside the board
    /// in battle, and under the shop's window over the offer's tile (round 44); the merchant's tooltip over the good (round 57). Top-left
    /// pixel space of the screen.
    /// </summary>
    public sealed class TooltipPlacementTests
    {
        static readonly Rect Screen = new Rect(0f, 0f, 1920f, 1080f);
        static readonly Rect Panel = new Rect(0f, 620f, 1920f, 460f);
        static readonly Vector2 Size = new Vector2(400f, 220f);

        [Test]
        public void Above_CentresTheCardOnTheCell_JustAboveThePanel_AndPointsTheNotchAtTheCell()
        {
            var cell = new Rect(690f, 708f, 180f, 60f);

            Rect card = TooltipPlacement.Above(cell, Size, Panel.yMin, Screen, out float notchX);

            Assert.AreEqual(cell.center.x, card.center.x, 0.001f);
            Assert.AreEqual(Panel.yMin - TooltipPlacement.Gap, card.yMax, 0.001f, "Its bottom edge is just above the panel.");
            Assert.AreEqual(Size, card.size);
            Assert.AreEqual(cell.center.x - card.x, notchX, 0.001f, "The notch is under the cell's middle.");
        }

        [Test]
        public void Above_StaysInsideTheScreen_AndTheNotchStillPointsAtTheCell()
        {
            var rearmost = new Rect(120f, 646f, 180f, 60f);
            Rect card = TooltipPlacement.Above(rearmost, Size, Panel.yMin, Screen, out float notchX);
            Assert.AreEqual(TooltipPlacement.ScreenMargin, card.x, 0.001f, "Pushed in from the left edge.");
            Assert.AreEqual(rearmost.center.x - card.x, notchX, 0.001f);

            var farRight = new Rect(1800f, 646f, 180f, 60f);
            card = TooltipPlacement.Above(farRight, Size, Panel.yMin, Screen, out notchX);
            Assert.AreEqual(Screen.xMax - TooltipPlacement.ScreenMargin, card.xMax, 0.001f, "Pushed in from the right edge.");
            Assert.AreEqual(farRight.center.x - card.x, notchX, 0.001f, "The notch still points at the cell.");

            var offScreen = new Rect(1900f, 646f, 180f, 60f);
            TooltipPlacement.Above(offScreen, Size, Panel.yMin, Screen, out notchX);
            Assert.AreEqual(Size.x - TooltipPlacement.NotchInset, notchX, 0.001f, "The notch keeps off the card's corner.");
        }

        [Test]
        public void Beside_StandsTheCardNextToTheBoard_OnTheSideAsked_AtTheCellsHeight_TheNotchAtTheCellsMiddle()
        {
            var partyCell = new Rect(500f, 646f, 180f, 60f);
            Rect left = TooltipPlacement.Beside(partyCell, Size, true, Panel, Screen, out float notchY, out bool cardIsLeft);
            Assert.AreEqual(partyCell.xMin - TooltipPlacement.BesideGap, left.xMax, 0.001f, "To the left of the party's board.");
            Assert.AreEqual(partyCell.yMin, left.y, 0.001f);
            Assert.IsTrue(cardIsLeft);
            Assert.AreEqual(30f, notchY, 0.001f, "The notch at the cell's middle height, down from the card's top.");

            var enemyCell = new Rect(1052f, 708f, 180f, 60f);
            Rect right = TooltipPlacement.Beside(enemyCell, Size, false, Panel, Screen, out notchY, out cardIsLeft);
            Assert.AreEqual(enemyCell.xMax + TooltipPlacement.BesideGap, right.x, 0.001f, "To the right of the enemy's board.");
            Assert.AreEqual(enemyCell.yMin, right.y, 0.001f);
            Assert.IsFalse(cardIsLeft);
            Assert.AreEqual(30f, notchY, 0.001f);
        }

        [Test]
        public void Beside_WithNoRoomOnTheSideAsked_GoesToTheOtherSideOfTheBoard()
        {
            var rearmost = new Rect(120f, 646f, 180f, 60f);
            Rect card = TooltipPlacement.Beside(rearmost, Size, true, Panel, Screen, out _, out bool cardIsLeft);
            Assert.IsFalse(cardIsLeft, "Pushed in from the left edge it would cover its own board: it stands to the right instead.");
            Assert.AreEqual(rearmost.xMax + TooltipPlacement.BesideGap, card.x, 0.001f);

            var third = new Rect(310f, 646f, 180f, 60f);
            card = TooltipPlacement.Beside(third, Size, true, Panel, Screen, out _, out cardIsLeft);
            Assert.IsFalse(cardIsLeft);
            Assert.AreEqual(third.xMax + TooltipPlacement.BesideGap, card.x, 0.001f);

            var farRight = new Rect(1700f, 646f, 180f, 60f);
            card = TooltipPlacement.Beside(farRight, Size, false, Panel, Screen, out _, out cardIsLeft);
            Assert.IsTrue(cardIsLeft, "No room on the right: to the left instead.");
            Assert.AreEqual(farRight.xMin - TooltipPlacement.BesideGap, card.xMax, 0.001f);
        }

        [Test]
        public void Beside_StaysInsideThePanelAndTheScreen_AndTheNotchFollowsTheCell()
        {
            var low = new Rect(500f, 1000f, 180f, 60f);
            Rect card = TooltipPlacement.Beside(low, Size, true, Panel, Screen, out float notchY, out _);
            Assert.AreEqual(Panel.yMax - TooltipPlacement.ScreenMargin, card.yMax, 0.001f, "Lifted to stay in the panel.");
            Assert.AreEqual(low.center.y - card.y, notchY, 0.001f, "The notch still points at the cell's middle.");

            var tall = new Vector2(400f, 900f);
            card = TooltipPlacement.Beside(low, tall, true, Panel, Screen, out notchY, out _);
            Assert.AreEqual(Panel.yMin + TooltipPlacement.PanelInset, card.y, 0.001f, "A card taller than the room hangs from the panel's top.");
            Assert.AreEqual(low.center.y - card.y, notchY, 0.001f, "The notch still points at the cell's middle.");
        }

        [Test]
        public void Over_CentresTheMerchantsTooltipOnTheGood_JustAboveIt()
        {
            var spear = new Rect(1190f, 380f, 102f, 102f);

            Rect tip = TooltipPlacement.Over(spear, Size, Screen);

            Assert.AreEqual(spear.center.x, tip.center.x, 0.001f, "Centred on the good (round 57, Diablo II).");
            Assert.AreEqual(spear.yMin - TooltipPlacement.Gap, tip.yMax, 0.001f, "Its bottom edge just above the good.");
            Assert.AreEqual(Size, tip.size);
        }

        [Test]
        public void Over_GoesUnderTheGood_WhenThereIsNoRoomAbove_AndStaysInsideTheScreen()
        {
            var high = new Rect(1190f, 150f, 102f, 50f);
            Rect tip = TooltipPlacement.Over(high, Size, Screen);
            Assert.AreEqual(high.yMax + TooltipPlacement.Gap, tip.y, 0.001f, "Under the good.");

            var farRight = new Rect(1860f, 600f, 50f, 50f);
            tip = TooltipPlacement.Over(farRight, Size, Screen);
            Assert.AreEqual(Screen.xMax - TooltipPlacement.ScreenMargin, tip.xMax, 0.001f, "Pushed in from the right edge.");

            var tall = new Vector2(400f, 1000f);
            tip = TooltipPlacement.Over(high, tall, Screen);
            Assert.AreEqual(Screen.yMax - TooltipPlacement.ScreenMargin, tip.yMax, 0.001f, "Taller than the room under the good: lifted to stay on the screen.");
        }
    }
}
