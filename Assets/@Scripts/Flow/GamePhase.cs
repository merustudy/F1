namespace F1.Flow
{
    /// <summary>
    /// Where the game is once a run exists. Computed from the state; never stored.
    /// Rules: Docs/Architecture/11_APPLICATION_FLOW.md.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>No expedition, no battle and no report to confirm.</summary>
        Lobby,
        /// <summary>On an expedition, choosing the next node.</summary>
        NodeMap,
        /// <summary>A battle session exists. It stays here after the battle ended, until the screen closes it.</summary>
        Battle,
        /// <summary>On an expedition, choosing a reward.</summary>
        Reward,
        /// <summary>On an expedition, at a camp node: choosing what to do there.</summary>
        Camp,
        /// <summary>On an expedition, at a shop node: buying for the region coins, or leaving (Slice B stage 17).</summary>
        Shop,
        /// <summary>The expedition ended and its settlement report has not been confirmed.</summary>
        Settlement,
    }
}
