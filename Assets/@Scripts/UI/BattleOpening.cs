using System;
using System.Collections.Generic;
using F1.Gameplay;

namespace F1.UI
{
    /// <summary>
    /// Round 104 (2026-10-24, the user's "원정 전투 시작시 초기 1~2초 동안 장착 중인 아이템에 의한 버프/디버프/방어력효과 적용", shown only — 판정 2):
    /// the battle's opening as the screen plays it. The engine fired every item that activates when a battle starts at 0 ms (Docs/Design/02 §3);
    /// the screen shows those activations one by one while its clock stands at 0.0 s, then lets the battle run. A step is one activation and its
    /// effects, in the engine's order (the party first, unit and slot order). An activation whose effects are all maximum HP is no step: that HP is
    /// there from the first frame (the user's "최대 체력은 미리 적용"), and only its caption is told at once. Nothing here changes the battle.
    /// </summary>
    public sealed class BattleOpening
    {
        /// <summary>When the first step plays, the time between two steps, and how long the last one stands before the battle runs (seconds at x1).</summary>
        public const float First = 0.3f;
        public const float Gap = 0.35f;
        public const float Tail = 0.5f;

        /// <summary>The opening is never longer than this: with many steps the gap shrinks.</summary>
        public const float Longest = 2f;

        public sealed class Step
        {
            /// <summary>The index in the log of the activation, then of each of its effects.</summary>
            public readonly List<int> Events = new List<int>();

            /// <summary>When it plays, in seconds of the opening at x1.</summary>
            public float At;
        }

        readonly List<Step> _steps = new List<Step>();
        readonly List<int> _atOnce = new List<int>();

        BattleOpening()
        {
        }

        public IReadOnlyList<Step> Steps => _steps;

        /// <summary>The events told as the screen opens: the captions of the activations that only raise the maximum HP.</summary>
        public IReadOnlyList<int> AtOnce => _atOnce;

        /// <summary>How long the clock stands at 0.0 s, in seconds at x1. 0 when nothing activated: the battle runs at once.</summary>
        public float Length => _steps.Count == 0 ? 0f : _steps[_steps.Count - 1].At + Tail;

        /// <param name="events">The battle's log as the engine left it at 0 ms.</param>
        /// <param name="startsTheBattle">Whether an item (by id) is one that activates when a battle starts.</param>
        public static BattleOpening Of(IReadOnlyList<BattleEvent> events, Func<string, bool> startsTheBattle)
        {
            var opening = new BattleOpening();
            Step current = null;
            bool onlyMaxHp = false;
            for (int i = 0; i < events.Count; i++)
            {
                BattleEvent e = events[i];
                if (e.TimeMs != 0)
                {
                    break;
                }

                if (e.Kind == BattleEventKind.ItemActivated)
                {
                    opening.Close(current, onlyMaxHp);
                    current = e.Id != null && startsTheBattle(e.Id) ? new Step() : null;
                    current?.Events.Add(i);
                    onlyMaxHp = true;
                }
                else if (current != null && e.Id == events[current.Events[0]].Id)
                {
                    current.Events.Add(i);
                    onlyMaxHp &= e.Kind == BattleEventKind.MaxHpRaised;
                }
            }

            opening.Close(current, onlyMaxHp);
            float[] times = Times(opening._steps.Count);
            for (int i = 0; i < times.Length; i++)
            {
                opening._steps[i].At = times[i];
            }

            return opening;
        }

        /// <summary>When each of <paramref name="count"/> steps plays: from <see cref="First"/>, <see cref="Gap"/> apart, all within <see cref="Longest"/>.</summary>
        public static float[] Times(int count)
        {
            var times = new float[count];
            float gap = count > 1 ? Math.Min(Gap, (Longest - First - Tail) / (count - 1)) : 0f;
            for (int i = 0; i < count; i++)
            {
                times[i] = First + i * gap;
            }

            return times;
        }

        void Close(Step step, bool onlyMaxHp)
        {
            if (step == null)
            {
                return;
            }

            // An activation with effects that only raise the maximum HP is told at once; one without effects (it did nothing there) still plays.
            if (onlyMaxHp && step.Events.Count > 1)
            {
                _atOnce.AddRange(step.Events.GetRange(1, step.Events.Count - 1));
                return;
            }

            _steps.Add(step);
        }
    }
}
