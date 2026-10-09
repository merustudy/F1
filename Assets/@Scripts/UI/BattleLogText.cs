using F1.Core;
using F1.Data;
using F1.Gameplay;

namespace F1.UI
{
    /// <summary>Turns battle events into lines of text. It reads the log; it decides nothing.</summary>
    public static class BattleLogText
    {
        /// <summary>The line for an event, or null for events that are not shown (item activations, storm damage per unit).</summary>
        public static string Line(BattleEvent e, BattleEngine engine)
        {
            string body = Body(e, engine);
            return body == null ? null : UiStrings.Get(UiKeys.Log.Line, UiText.Seconds(e.TimeMs), body);
        }

        /// <summary>The event as one short sentence without its time, for the panel's captions; null for events that are not shown.</summary>
        public static string Caption(BattleEvent e, BattleEngine engine)
        {
            return Body(e, engine);
        }

        public static string UnitName(BattleEngine engine, UnitRef unit)
        {
            return UiText.Name(engine.Unit(unit).Setup.Name);
        }

        /// <summary>What caused a damage, heal, shield or burn event: "unit's item", a passive, burn, storm or a potion.</summary>
        public static string Source(BattleEngine engine, UnitRef source, string cause)
        {
            StaticData data = Managers.Data.Data;
            if (source.IsNone)
            {
                switch (cause)
                {
                    case BattleEvent.CauseBurn: return UiStrings.Get(UiKeys.Log.SourceBurn);
                    case BattleEvent.CauseStorm: return UiStrings.Get(UiKeys.Log.SourceStorm);
                    default: return UiText.Name(data.Potions.Get(cause).Name);
                }
            }

            string unit = UnitName(engine, source);
            return cause == BattleEvent.CausePassive
                ? UiStrings.Get(UiKeys.Log.SourcePassive, unit)
                : UiStrings.Get(UiKeys.Log.SourceItem, unit, UiText.Name(data.Items.Get(cause).Name));
        }

        /// <summary>Which sentence explains a death: the collapse's (it names no hit and no roll), or the one for how the grace ended.</summary>
        public static string DeathKey(DeathCause death)
        {
            if (death.Collapsed)
            {
                return UiKeys.Death.Collapsed;
            }

            return death.GraceWasBroken ? UiKeys.Death.GraceBroken : UiKeys.Death.AfterGrace;
        }

        /// <summary>
        /// One sentence that explains a death: when the mercenary fell and, unless the fatigue collapse killed it, what broke the grace
        /// and the final roll. The collapse's sentence names no hit: a mercenary who entered at 0 HP may never have been hit.
        /// </summary>
        public static string Death(DeathCause death, BattleEngine engine)
        {
            string key = DeathKey(death);
            string name = UnitName(engine, death.Unit);
            string fell = UiText.Seconds(death.DogEnteredMs);
            string died = UiText.Seconds(death.DiedMs);
            if (death.Collapsed)
            {
                return UiStrings.Get(key, name, fell, died);
            }

            return UiStrings.Get(
                key,
                name,
                fell,
                died,
                Source(engine, death.LastHitSource, death.LastHitCause),
                death.DeathChancePercent,
                death.DeathRoll,
                death.GraceHits);
        }

        static string Body(BattleEvent e, BattleEngine engine)
        {
            switch (e.Kind)
            {
                case BattleEventKind.Damaged:
                    if (e.Id == BattleEvent.CauseStorm)
                    {
                        // The storm line already says how much everyone takes.
                        return null;
                    }

                    return e.B > 0
                        ? UiStrings.Get(UiKeys.Log.DamageAbsorbed, Source(engine, e.Source, e.Id), UnitName(engine, e.Target), e.A, e.B)
                        : UiStrings.Get(UiKeys.Log.Damage, Source(engine, e.Source, e.Id), UnitName(engine, e.Target), e.A);
                case BattleEventKind.Healed:
                    return UiStrings.Get(UiKeys.Log.Heal, Source(engine, e.Source, e.Id), UnitName(engine, e.Target), e.B);
                case BattleEventKind.ShieldGained:
                    return UiStrings.Get(UiKeys.Log.Shield, Source(engine, e.Source, e.Id), UnitName(engine, e.Target), e.A);
                case BattleEventKind.BurnApplied:
                    return UiStrings.Get(UiKeys.Log.Burn, Source(engine, e.Source, e.Id), UnitName(engine, e.Target), e.A);
                case BattleEventKind.DogEntered:
                    return UiStrings.Get(UiKeys.Log.DogEntered, UnitName(engine, e.Target), UiText.Seconds(e.A - e.TimeMs));
                case BattleEventKind.DogExited:
                    return UiStrings.Get(UiKeys.Log.DogExited, UnitName(engine, e.Target));
                case BattleEventKind.GraceBroken:
                    return UiStrings.Get(UiKeys.Log.GraceBroken, UnitName(engine, e.Target), e.A);
                case BattleEventKind.DeathRolled:
                    return UiStrings.Get(e.C == 1 ? UiKeys.Log.DeathFailed : UiKeys.Log.DeathSurvived, UnitName(engine, e.Target), e.A, e.B);
                case BattleEventKind.Died:
                    return UiStrings.Get(UiKeys.Log.Died, UnitName(engine, e.Target));
                case BattleEventKind.RowsAdvanced:
                    return UiStrings.Get(e.A == (int)BattleSide.Party ? UiKeys.Log.PartyAdvanced : UiKeys.Log.EnemyAdvanced, e.B);
                case BattleEventKind.PotionUsed:
                    return UiStrings.Get(UiKeys.Log.PotionUsed, UiText.Name(Managers.Data.Data.Potions.Get(e.Id).Name), UnitName(engine, e.Target));
                case BattleEventKind.RetreatAttempted:
                    return UiStrings.Get(e.C == 1 ? UiKeys.Log.RetreatSucceeded : UiKeys.Log.RetreatFailed, e.A, e.B);
                case BattleEventKind.StormTicked:
                    return UiStrings.Get(UiKeys.Log.Storm, e.A);
                case BattleEventKind.FatigueChanged:
                    return FatigueLine(e, engine);
                case BattleEventKind.BrokeDown:
                {
                    FatigueStateData state = Managers.Data.Data.FatigueStates.Get(e.Id);
                    return UiStrings.Get(
                        state.Kind == FatigueStateKind.Virtue ? UiKeys.Log.Virtue : UiKeys.Log.BrokeDown,
                        UnitName(engine, e.Target),
                        UiText.FatigueStateName(state),
                        e.A,
                        e.B);
                }

                case BattleEventKind.Collapsed:
                    return UiStrings.Get(UiKeys.Log.Collapsed, UnitName(engine, e.Target));
                case BattleEventKind.FatigueStateEnded:
                    return UiStrings.Get(UiKeys.Log.FatigueStateEnded, UnitName(engine, e.Target), UiText.FatigueStateName(Managers.Data.Data.FatigueStates.Get(e.Id)));
                default:
                    return null;
            }
        }

        /// <summary>
        /// A fatigue change worth a line: death's door (the unit's own, or an ally's), an ally's death, a virtue. A hit's and a
        /// kill's are too many to read; the pips under the unit's feet show them.
        /// </summary>
        static string FatigueLine(BattleEvent e, BattleEngine engine)
        {
            string key;
            switch (e.Id)
            {
                case BattleEvent.FatigueDog: key = UiKeys.Log.FatigueDog; break;
                case BattleEvent.FatigueAllyDog: key = UiKeys.Log.FatigueAllyDog; break;
                case BattleEvent.FatigueAllyDeath: key = UiKeys.Log.FatigueAllyDeath; break;
                case BattleEvent.FatigueVirtue: key = UiKeys.Log.FatigueVirtue; break;
                default: return null;
            }

            return UiStrings.Get(key, UnitName(engine, e.Target), e.A, e.C);
        }
    }
}
