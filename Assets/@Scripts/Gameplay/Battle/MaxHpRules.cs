namespace F1.Gameplay
{
    /// <summary>
    /// The maximum HP items' arithmetic (Design/35 판정 6, Slice D stage 56 — Design/38 판정 1): when the maximum changes, the HP moves in the same
    /// proportion, rounded down. The battle raises it when it starts (<see cref="BattleEngine"/>), the expedition's screens show the same values before
    /// (<see cref="ExpeditionRules.ShownHp"/>) and the expedition takes them back after (<see cref="ExpeditionRules.HpAfterBattle"/>): one rule, so the
    /// screen and the battle never differ.
    /// </summary>
    public static class MaxHpRules
    {
        /// <summary>The HP when the maximum goes from <paramref name="fromMax"/> to <paramref name="toMax"/>: in the same proportion, rounded down.</summary>
        public static int Scale(int hp, int fromMax, int toMax)
        {
            return (int)((long)hp * toMax / fromMax);
        }
    }
}
