using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Which sentence explains a death: the part of <see cref="BattleLogText"/> that needs no string table. The fatigue collapse's
    /// sentence names no hit and no roll, so a mercenary nobody hit (entered at 0 HP) is explained without a source.
    /// </summary>
    public sealed class BattleLogTextTests
    {
        [Test]
        public void DeathKey_WhenTheFatigueCollapseKilled_IsTheCollapseSentence_EvenWithNoHitOnRecord()
        {
            var death = new DeathCause { Unit = new UnitRef(BattleSide.Party, 1), Collapsed = true, LastHitSource = UnitRef.None, LastHitCause = null };

            Assert.AreEqual(UiKeys.Death.Collapsed, BattleLogText.DeathKey(death));
        }

        [Test]
        public void DeathKey_WhenAHitKilled_SaysHowTheGraceEnded()
        {
            var afterGrace = new DeathCause { Unit = new UnitRef(BattleSide.Party, 0), LastHitCause = "claw", GraceWasBroken = false };
            var graceBroken = new DeathCause { Unit = new UnitRef(BattleSide.Party, 0), LastHitCause = "claw", GraceWasBroken = true, GraceHits = 3 };

            Assert.AreEqual(UiKeys.Death.AfterGrace, BattleLogText.DeathKey(afterGrace));
            Assert.AreEqual(UiKeys.Death.GraceBroken, BattleLogText.DeathKey(graceBroken));
        }
    }
}
