using System.Collections;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using EffectChip = F1.UI.BattleUnitView.EffectChip;

namespace F1.Tests
{
    /// <summary>
    /// Round 104 (2026-10-24, the user's play feedback): the battle's opening holds the clock at 0.0 s while the items that activate when a battle starts
    /// play one by one, their shield held back from the badge until its step; a stagger turns stars over the head of the unit it pushed back for as
    /// long as it pushed, a second one adding on; and a right click on an effect chip or the badge opens that effect's card, which follows the
    /// numbers and closes at the next press or when the chip goes.
    /// </summary>
    public sealed class UiBattleFeedbackTests
    {
        string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = BootTestUtil.UseTemporarySaveRoot();
            LobbyScreen.StoryShown = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LobbyScreen.StoryShown = true;
            yield return BootTestUtil.ShutdownApp();
        }

        /// <summary>Departs with the <see cref="Front"/> member carrying these items under its base weapon (in the board's second row), and enters the first battle.</summary>
        static IEnumerator FirstBattleWith(params string[] itemIds)
        {
            yield return UiTestUtil.NewRunToLobby();
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();
            StaticData data = Managers.Data.Data;
            ExpeditionMember front = Front();
            int x = 0;
            foreach (string id in itemIds)
            {
                var item = new EquippedItem(data.Items.Get(id));
                UiTestUtil.Put(front, item, x, 1);
                x += item.Item.Width;
            }

            UiTestUtil.Screen<NodeMapScreen>().Refresh();
            yield return null;
            while (Managers.Expedition.Phase != GamePhase.Battle)
            {
                yield return UiTestUtil.GoIntoTheFirstNode();
            }
        }

        /// <summary>The front-most member with a melee base weapon one row high: a board with room under it, and the strength the armour asks.</summary>
        static ExpeditionMember Front()
        {
            int row = UiTestUtil.FlatRow(melee: true);
            return Managers.Expedition.Expedition.Members.Find(m => m.Alive && m.Row == row);
        }

        static BattleUnitView FrontView(BattleScreen battle)
        {
            string front = Front().MercenaryId;
            return UiTestUtil.Views<BattleUnitView>(battle).Single(v => v.Unit.Side == BattleSide.Party && v.Unit.Setup.SourceId == front);
        }

        static IEnumerator WaitForTheOpening(BattleScreen battle)
        {
            float started = Time.unscaledTime;
            while (battle.OpeningPlaying && Time.unscaledTime - started < 5f)
            {
                yield return null;
            }

            Assert.IsFalse(battle.OpeningPlaying, "The opening ends within two seconds at x1.");
        }

        [UnityTest]
        public IEnumerator Stagger_TurnsStarsOverTheHead_AsLongAsItPushed()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return FirstBattleWith("frost_flask");
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return WaitForTheOpening(battle);
            battle.Clock.Paused = true;
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            Assert.IsFalse(UiTestUtil.Views<BattleUnitView>(battle).Any(v => v.Stars.Shown), "No stars before a stagger.");

            // Step the battle as the screen would, a tenth of a second a frame, until the flask's stagger lands.
            BattleEvent stagger = null;
            for (int step = 0; step < 400 && stagger == null && !Managers.Expedition.Battle.IsFinished; step++)
            {
                Managers.Expedition.AdvanceBattle(100);
                yield return null;
                stagger = engine.Events.FirstOrDefault(e => e.Kind == BattleEventKind.Staggered && e.B > 0);
            }

            Assume.That(stagger, Is.Not.Null, "The flask staggered an enemy before the battle ended.");
            BattleUnitView target = UiTestUtil.Views<BattleUnitView>(battle).Single(v => v.Unit.Ref.Equals(stagger.Target));
            Assert.IsTrue(target.Stars.Shown, "Stars turn over the staggered unit.");
            Assert.AreEqual(5, target.Stars.Count);
            Assert.AreEqual(stagger.TimeMs + stagger.A, target.StunEndMs, "As long as the stagger pushed its items back (800 ms).");
            Assert.Greater(((RectTransform)target.Stars.transform).anchoredPosition.y, 0f, "Over the figure, not at its feet.");

            // Another stagger while they turn adds its time to their end, as the engine adds it to the items'.
            target.Stagger(engine.TimeMs, 500);
            Assert.AreEqual(stagger.TimeMs + stagger.A + 500, target.StunEndMs);

            while (engine.TimeMs < target.StunEndMs && !Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(100);
                yield return null;
            }

            yield return null;
            Assert.IsFalse(target.Stars.Shown, "They stop when the push is over.");
        }

        [UnityTest]
        public IEnumerator EffectChip_RightClickOpensItsCard_ThatFollowsTheNumbers_AndClosesAtTheNextPressOrWhenTheChipGoes()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return FirstBattleWith("iron_armor");
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return WaitForTheOpening(battle);
            battle.Clock.Paused = true;
            BalanceData balance = Managers.Data.Data.Balance;

            // The badge: the shield's card above it.
            BattleUnitView front = FrontView(battle);
            Assert.Greater(front.ShieldShown, 0);
            front.RightClickChip(EffectChip.Shield);
            yield return null;
            EffectTooltipView card = battle.EffectTooltip;
            Assert.IsTrue(card.IsShown);
            Assert.AreEqual(EffectChip.Shield, card.Chip);
            Assert.AreEqual("보호막 " + UiText.Amount(front.ShieldShown), card.Title);
            Assert.AreEqual("방어", card.Kind);
            Assert.AreEqual("피해를 HP보다 먼저 " + UiText.Amount(front.ShieldShown) + " 막는다", card.Effect);
            Assert.IsFalse(battle.Tooltip.IsShown, "One card at a time.");

            UiTestUtil.PressTheBackground();
            yield return null;
            yield return null;
            Assert.IsFalse(card.IsShown, "The next press closes it, as an item's card.");

            // A state chip: the card says what the stacks do now, follows them as they change, and closes when they are gone.
            BattleUnitView enemy = UiTestUtil.Views<BattleUnitView>(battle).First(v => v.Unit.Side == BattleSide.Enemy && v.Unit.Alive);
            enemy.Unit.SetStacks(EffectKind.Freeze, 3);
            yield return null;
            enemy.RightClickChip(EffectChip.Freeze);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreEqual("빙결 3", card.Title);
            Assert.AreEqual("해로운 상태", card.Kind);
            Assert.AreEqual("아이템의 쿨다운이 " + 3 * balance.FreezePercentPerStack + "% 느려진다", card.Effect);
            Assert.AreEqual("중첩 1에 " + balance.FreezePercentPerStack + "% · 다음 발동부터 · " + balance.BurnTickMs / 1000 + "초마다 1씩 준다", card.Rule);

            enemy.Unit.SetStacks(EffectKind.Freeze, 2);
            yield return null;
            yield return null;
            Assert.AreEqual("빙결 2", card.Title, "It follows the stacks.");

            enemy.Unit.SetStacks(EffectKind.Freeze, 0);
            yield return null;
            yield return null;
            Assert.IsFalse(card.IsShown, "The chip went, and the card with it.");

            // Not while a potion waits for its target.
            enemy.Unit.SetStacks(EffectKind.Freeze, 1);
            yield return null;
            PotionSlotView[] potions = UiTestUtil.Views<PotionSlotView>(battle);
            Assume.That(potions.Length, Is.GreaterThan(0), "The expedition starts with a potion.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            enemy.RightClickChip(EffectChip.Freeze);
            yield return null;
            Assert.IsFalse(card.IsShown, "Not while a potion is armed.");
        }
    }
}
