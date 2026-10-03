using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Plays the battle's events as what the screen shows: a hit rises as a number and the one
    /// hit flashes and recoils, the one who struck lunges, the item that fired flashes in its
    /// cell, a fallen unit fades, a heavy blow shakes the stage, the storm's lightning flashes,
    /// and each event that reads as a sentence goes to the panel's captions. It reads the event
    /// log the engine has already settled and never changes anything in it. Only what just
    /// happened is played: when a long stretch of battle arrives at once (a continued battle,
    /// a test stepping ahead) the older events are skipped, so nothing replays the past.
    /// </summary>
    public sealed class BattlePresenter
    {
        /// <summary>An event older than this, against the engine's clock, is not played.</summary>
        public const int RecentMs = 300;

        /// <summary>A hit that takes at least this share of max HP is heavy: a large number and a shake.</summary>
        const float HeavyShare = 0.2f;

        static readonly Color HitTint = new Color(1f, 0.55f, 0.5f, 1f);
        static readonly Color HealTint = new Color(0.7f, 1f, 0.75f, 1f);

        readonly BattleEngine _engine;
        readonly StaticData _data;
        readonly BattleFxLayer _fx;
        readonly Func<UnitRef, BattleUnitView> _unitView;
        readonly Func<UnitRef, BattleBoardView> _boardView;
        readonly RectTransform _clock;
        readonly Action<string> _caption;
        int _played;

        /// <param name="unitView">The stage view of a unit, or null when it has none.</param>
        /// <param name="boardView">The panel line of a unit, or null when it has none.</param>
        /// <param name="clock">Where the storm's numbers rise from.</param>
        /// <param name="caption">Takes one line for the panel's captions.</param>
        public BattlePresenter(
            BattleEngine engine,
            StaticData data,
            BattleFxLayer fx,
            Func<UnitRef, BattleUnitView> unitView,
            Func<UnitRef, BattleBoardView> boardView,
            RectTransform clock,
            Action<string> caption)
        {
            _engine = engine;
            _data = data;
            _fx = fx;
            _unitView = unitView;
            _boardView = boardView;
            _clock = clock;
            _caption = caption;

            // What happened before the screen opened (a battle continued from a save) is not replayed.
            _played = engine.Events.Count;
        }

        /// <summary>How many events of the log have been passed, played or skipped.</summary>
        public int Played => _played;

        /// <summary>Plays the events logged since the last call, as far as they are recent.</summary>
        public void Play()
        {
            IReadOnlyList<BattleEvent> events = _engine.Events;
            for (; _played < events.Count; _played++)
            {
                BattleEvent e = events[_played];
                if (e.TimeMs >= _engine.TimeMs - RecentMs)
                {
                    PlayOne(e);
                }
            }
        }

        void PlayOne(BattleEvent e)
        {
            string caption = BattleLogText.Caption(e, _engine);
            if (caption != null)
            {
                _caption(caption);
            }

            switch (e.Kind)
            {
                case BattleEventKind.ItemActivated:
                    PlayActivation(e);
                    break;
                case BattleEventKind.Damaged:
                    PlayDamage(e);
                    break;
                case BattleEventKind.Healed:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Heal, e.B), UiPalette.Good, false);
                    _unitView(e.Target)?.Flash(HealTint);
                    break;
                case BattleEventKind.ShieldGained:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Shield, e.A), UiPalette.Shield, false);
                    break;
                case BattleEventKind.BurnApplied:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Burn, e.A), UiPalette.Burn, false);
                    break;
                case BattleEventKind.DogEntered:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.DogEntered), UiPalette.Danger, true);
                    _fx.Shake(8f);
                    break;
                case BattleEventKind.DeathRolled:
                    if (e.C == 0)
                    {
                        FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Survived), UiPalette.TextDim, false);
                    }

                    break;
                case BattleEventKind.Died:
                    _fx.Ghost(_unitView(e.Target)?.FigureArt);
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Died), UiPalette.Danger, true);
                    _fx.Shake(10f);
                    break;
                case BattleEventKind.RetreatAttempted:
                    FloatOverParty(UiStrings.Get(e.C == 1 ? UiKeys.Fx.RetreatSucceeded : UiKeys.Fx.RetreatFailed), e.C == 1 ? UiPalette.Good : UiPalette.TextDim);
                    break;
                case BattleEventKind.StormTicked:
                    _fx.Flash(0.22f);
                    _fx.Shake(4f);
                    _fx.Float(_clock, UiStrings.Get(UiKeys.Fx.Storm, e.A), UiPalette.Burn, true);
                    break;
            }
        }

        /// <summary>The item's cell flashes; a weapon's owner lunges at the other side.</summary>
        void PlayActivation(BattleEvent e)
        {
            _boardView(e.Source)?.ItemOfSlot(e.A)?.Pulse();
            if (_data.Items.Get(e.Id).Category == ItemCategory.Weapon)
            {
                _unitView(e.Source)?.Lunge(Towards(e.Source.Side));
            }
        }

        /// <summary>
        /// The HP lost rises as a number (orange for burn, red otherwise) and what the shield took
        /// as a second one. A blow from a unit makes the target flash and recoil; the storm's and
        /// burn's damage have no blow. A heavy blow shakes the stage.
        /// </summary>
        void PlayDamage(BattleEvent e)
        {
            BattleUnitView target = _unitView(e.Target);
            if (target == null)
            {
                return;
            }

            bool storm = e.Id == BattleEvent.CauseStorm;
            bool burn = e.Id == BattleEvent.CauseBurn;
            int lost = e.A - e.B;
            bool heavy = lost >= HeavyShare * target.Unit.MaxHp;
            if (lost > 0)
            {
                _fx.Float(target.FigureRect, UiStrings.Get(UiKeys.Fx.Damage, lost), burn ? UiPalette.Burn : UiPalette.Danger, heavy);
            }

            if (e.B > 0)
            {
                _fx.Float(target.FigureRect, UiStrings.Get(UiKeys.Fx.Absorbed, e.B), UiPalette.Shield, false);
            }

            if (!storm && !burn)
            {
                target.Flash(HitTint);
                target.Recoil(-Towards(e.Target.Side));
                if (heavy)
                {
                    _fx.Shake(6f);
                }
            }
        }

        void FloatOver(UnitRef unit, string text, Color color, bool big)
        {
            BattleUnitView view = _unitView(unit);
            if (view != null)
            {
                _fx.Float(view.FigureRect, text, color, big);
            }
        }

        /// <summary>Over the living party member nearest the enemy: where the party's own news rises.</summary>
        void FloatOverParty(string text, Color color)
        {
            BattleUnit front = null;
            foreach (BattleUnit unit in _engine.Party)
            {
                if (unit.Alive && (front == null || unit.Row < front.Row))
                {
                    front = unit;
                }
            }

            if (front != null)
            {
                FloatOver(front.Ref, text, color, true);
            }
        }

        /// <summary>The way a side faces: the party looks right, the enemy left.</summary>
        static float Towards(BattleSide side)
        {
            return side == BattleSide.Party ? 1f : -1f;
        }
    }
}
