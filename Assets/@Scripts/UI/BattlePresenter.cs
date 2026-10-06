using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Plays the battle's events as what the screen shows: a hit rises as a number and the one
    /// hit flashes and recoils, the one who struck lunges, the item that fired flashes in its
    /// cell, a fallen enemy fades and a fallen mercenary turns into a grave (the screen shows it and
    /// keeps the party's places meanwhile), an enemy felled by a unit's item is a kill moment (the
    /// screen slows, darkens and draws in on the two, then lets the enemy fade; Docs/Design/10 §5),
    /// a heavy blow shakes the stage, the storm's lightning flashes, a breakdown is a moment the screen plays
    /// (round 38, B: the slow, the dark, the zoom, the state pose, the burst and the word), and each event that reads as a sentence goes to the panel's captions. Each event asks for its sound
    /// (Docs/Architecture/14_SOUND.md "소리를 내는 자리"), so a skipped event makes none either. It reads the event
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
        readonly Action<UnitRef> _partyFell;
        readonly Func<bool> _killMoments;
        readonly Action<UnitRef, UnitRef> _killMoment;
        readonly Action<SoundEffect> _sound;
        readonly Action<UnitRef, FatigueStateData> _breakdown;
        int _played;

        /// <param name="unitView">The stage view of a unit, or null when it has none.</param>
        /// <param name="boardView">The panel line of a unit, or null when it has none.</param>
        /// <param name="clock">Where the storm's numbers rise from.</param>
        /// <param name="caption">Takes one line for the panel's captions.</param>
        /// <param name="partyFell">Turns a fallen mercenary into its grave: the screen's to do, as it keeps the party's places
        /// while the grave stands. Without it a fallen mercenary fades like an enemy.</param>
        /// <param name="killMoments">True while the screen plays kill moments (not at every speed).</param>
        /// <param name="killMoment">Plays an enemy's fall to a unit's item as a kill moment, the striker first: the screen's to
        /// do, as it slows the battle and holds the fallen until the moment is over, then lets it fade with its words.</param>
        /// <param name="sound">Plays a sound effect. Without it the battle is silent.</param>
        /// <param name="breakdown">Plays the moment of a mercenary's breakdown into a state (or, with a null state, its collapse at the most
        /// fatigue): the screen's to do, as it slows the battle and lights the unit as for a kill moment. Without it only the sound plays.</param>
        public BattlePresenter(
            BattleEngine engine,
            StaticData data,
            BattleFxLayer fx,
            Func<UnitRef, BattleUnitView> unitView,
            Func<UnitRef, BattleBoardView> boardView,
            RectTransform clock,
            Action<string> caption,
            Action<UnitRef> partyFell = null,
            Func<bool> killMoments = null,
            Action<UnitRef, UnitRef> killMoment = null,
            Action<SoundEffect> sound = null,
            Action<UnitRef, FatigueStateData> breakdown = null)
        {
            _engine = engine;
            _data = data;
            _fx = fx;
            _unitView = unitView;
            _boardView = boardView;
            _clock = clock;
            _caption = caption;
            _partyFell = partyFell;
            _killMoments = killMoments;
            _killMoment = killMoment;
            _sound = sound;
            _breakdown = breakdown;

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
                    PlayOne(e, _played);
                }
            }
        }

        /// <summary>
        /// The blow at this index of the log, when it killed an enemy: a unit's item that hurt it, its death logged right after
        /// (an enemy has no death's door, so its death follows the damage that killed it). Burn's, the storm's and a passive's
        /// damage are not blows. Null otherwise.
        /// </summary>
        BattleEvent KillingBlowAt(int index)
        {
            IReadOnlyList<BattleEvent> events = _engine.Events;
            if (index < 0 || index + 1 >= events.Count)
            {
                return null;
            }

            BattleEvent blow = events[index];
            BattleEvent death = events[index + 1];
            bool fromAnItem = !blow.Source.IsNone && blow.Id != BattleEvent.CauseBurn && blow.Id != BattleEvent.CauseStorm && blow.Id != BattleEvent.CausePassive;
            return blow.Kind == BattleEventKind.Damaged && fromAnItem && blow.Target.Side == BattleSide.Enemy
                && death.Kind == BattleEventKind.Died && death.Target.Equals(blow.Target) && death.TimeMs == blow.TimeMs
                ? blow
                : null;
        }

        bool KillMoments => _killMoment != null && _killMoments != null && _killMoments();

        void PlayOne(BattleEvent e, int index)
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
                    PlayDamage(e, KillMoments && KillingBlowAt(index) != null);
                    break;
                case BattleEventKind.Healed:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Heal, e.B), UiPalette.Good, false);
                    _unitView(e.Target)?.Flash(HealTint);
                    Sound(SoundEffect.Heal);
                    break;
                case BattleEventKind.ShieldGained:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Shield, e.A), UiPalette.Shield, false);
                    Sound(SoundEffect.Shield);
                    break;
                case BattleEventKind.BurnApplied:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Burn, e.A), UiPalette.Burn, false);
                    Sound(SoundEffect.Burn);
                    break;
                case BattleEventKind.DogEntered:
                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.DogEntered), UiPalette.Danger, true);
                    _fx.Shake(8f);
                    Sound(SoundEffect.DeathsDoor);
                    break;
                case BattleEventKind.DeathRolled:
                    if (e.C == 0)
                    {
                        FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Survived), UiPalette.TextDim, false);
                        Sound(SoundEffect.Survived);
                    }

                    break;
                case BattleEventKind.Died:
                    // A mercenary turns into a grave at once (2026-10-04, round 21). An enemy felled by a unit's item is a kill
                    // moment: the screen holds it and lets it fade, with its words, when the moment is over, and nothing shakes
                    // (2026-10-05, round 30). Any other enemy fades as before.
                    BattleEvent blow = KillingBlowAt(index - 1);
                    if (blow != null && KillMoments)
                    {
                        _killMoment(blow.Source, e.Target);
                        Sound(SoundEffect.KillMoment);
                        break;
                    }

                    if (e.Target.Side == BattleSide.Party && _partyFell != null)
                    {
                        _partyFell(e.Target);
                    }
                    else
                    {
                        _fx.Ghost(_unitView(e.Target)?.FigureArt);
                    }

                    // A mercenary's death is the game's heaviest sound (Docs/Design/12 §2).
                    Sound(e.Target.Side == BattleSide.Party ? SoundEffect.MercenaryDeath : SoundEffect.EnemyDown);

                    FloatOver(e.Target, UiStrings.Get(UiKeys.Fx.Died), UiPalette.Danger, true);
                    _fx.Shake(10f);
                    break;
                case BattleEventKind.PotionUsed:
                    Sound(SoundEffect.Potion);
                    break;
                case BattleEventKind.RetreatAttempted:
                    FloatOverParty(UiStrings.Get(e.C == 1 ? UiKeys.Fx.RetreatSucceeded : UiKeys.Fx.RetreatFailed), e.C == 1 ? UiPalette.Good : UiPalette.TextDim);
                    Sound(e.C == 1 ? SoundEffect.Retreat : SoundEffect.RetreatFailed);
                    break;
                case BattleEventKind.StormTicked:
                    _fx.Flash(0.22f);
                    _fx.Shake(4f);
                    _fx.Float(_clock, UiStrings.Get(UiKeys.Fx.Storm, e.A), UiPalette.Burn, true);
                    Sound(SoundEffect.Thunder);
                    break;
                case BattleEventKind.BrokeDown:
                {
                    // The breakdown (round 38, B): the screen plays its moment. An affliction sounds as death's door does, a virtue as a
                    // death roll survived: the nearest of the shipped sounds (Docs/Architecture/14_SOUND.md).
                    FatigueStateData state = _data.FatigueStates.Get(e.Id);
                    _breakdown?.Invoke(e.Target, state);
                    Sound(state.Kind == FatigueStateKind.Virtue ? SoundEffect.Survived : SoundEffect.DeathsDoor);
                    break;
                }

                case BattleEventKind.Collapsed:
                    // The collapse at the most fatigue: the breakdown's moment with its own words. What it brings (death's door, or the
                    // death) is logged right after and plays as its own event, with its words, shake and sound.
                    _breakdown?.Invoke(e.Target, null);
                    break;
            }
        }

        /// <summary>How an item's owner moves when the item fires, by what the item is (Docs/Design/10 §5).</summary>
        public enum Motion
        {
            /// <summary>A weapon: a lunge at the other side, with the attack pose.</summary>
            Strike,

            /// <summary>Defensive gear, an attack that is not a weapon, anything else: the lunge alone.</summary>
            Lunge,

            /// <summary>A support item: a swell with a warm light behind.</summary>
            Pulse,
        }

        public static Motion MotionOf(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return Motion.Strike;
                case ItemCategory.Support: return Motion.Pulse;
                case ItemCategory.Armor:
                case ItemCategory.Attack:
                case ItemCategory.Other:
                    return Motion.Lunge;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        /// <summary>
        /// The sound of an item that fires, by what the item is: one for each category the shipped items have
        /// (Docs/Design/12 §2). An item of the other category has none.
        /// </summary>
        public static SoundEffect? EffectOf(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return SoundEffect.ItemWeapon;
                case ItemCategory.Armor: return SoundEffect.ItemArmor;
                case ItemCategory.Attack: return SoundEffect.ItemAttack;
                case ItemCategory.Support: return SoundEffect.ItemSupport;
                case ItemCategory.Other: return null;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        /// <summary>The item's cell flashes; its owner moves as the item's category says, and the item sounds.</summary>
        void PlayActivation(BattleEvent e)
        {
            _boardView(e.Source)?.ItemOfSlot(e.A)?.Pulse();
            SoundEffect? effect = EffectOf(_data.Items.Get(e.Id).Category);
            if (effect.HasValue)
            {
                Sound(effect.Value);
            }

            BattleUnitView owner = _unitView(e.Source);
            if (owner == null)
            {
                return;
            }

            switch (MotionOf(_data.Items.Get(e.Id).Category))
            {
                case Motion.Strike:
                    owner.Lunge(Towards(e.Source.Side), withPose: true);
                    break;
                case Motion.Lunge:
                    owner.Lunge(Towards(e.Source.Side));
                    break;
                case Motion.Pulse:
                    owner.Pulse();
                    break;
            }
        }

        /// <summary>
        /// The HP lost rises as a number (orange for burn, red otherwise) and what the shield took
        /// as a second one. A blow from a unit makes the target flash and recoil; the storm's and
        /// burn's damage have no blow. A heavy blow shakes the stage, unless it is a kill moment's.
        /// </summary>
        void PlayDamage(BattleEvent e, bool killMoment)
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

            if (!storm && !burn && !killMoment)
            {
                // The kill moment's own sound carries its blow; the storm's and burn's damage are numbers only.
                Sound(lost <= 0 && e.B > 0 ? SoundEffect.Blocked : heavy ? SoundEffect.HitHeavy : SoundEffect.Hit);
            }

            if (!storm && !burn)
            {
                // A unit that shows its hit pose flashes at half strength: the pose already says it was hit.
                target.Flash(target.HasHitPose ? Color.Lerp(Color.white, HitTint, 0.5f) : HitTint);
                target.Recoil(-Towards(e.Target.Side));
                if (heavy && !killMoment)
                {
                    _fx.Shake(6f);
                }
            }
        }

        void Sound(SoundEffect effect)
        {
            _sound?.Invoke(effect);
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
