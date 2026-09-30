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

        /// <summary>One sentence that explains a death: when the mercenary fell, what broke the grace and the final roll.</summary>
        public static string Death(DeathCause death, BattleEngine engine)
        {
            return UiStrings.Get(
                death.GraceWasBroken ? UiKeys.Death.GraceBroken : UiKeys.Death.AfterGrace,
                UnitName(engine, death.Unit),
                UiText.Seconds(death.DogEnteredMs),
                UiText.Seconds(death.DiedMs),
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
                case BattleEventKind.PotionUsed:
                    return UiStrings.Get(UiKeys.Log.PotionUsed, UiText.Name(Managers.Data.Data.Potions.Get(e.Id).Name), UnitName(engine, e.Target));
                case BattleEventKind.RetreatAttempted:
                    return UiStrings.Get(e.C == 1 ? UiKeys.Log.RetreatSucceeded : UiKeys.Log.RetreatFailed, e.A, e.B);
                case BattleEventKind.StormTicked:
                    return UiStrings.Get(UiKeys.Log.Storm, e.A);
                default:
                    return null;
            }
        }
    }
}
