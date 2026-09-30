using F1.Gameplay;

namespace F1.Flow
{
    /// <summary>
    /// One battle in progress or just ended. Screens read the engine (units, cooldowns, event log);
    /// only ExpeditionManager advances it and feeds it inputs.
    /// </summary>
    public sealed class BattleSession
    {
        internal BattleSession(BattleEngine engine, MapNode node)
        {
            Engine = engine;
            Node = node;
        }

        public BattleEngine Engine { get; }

        /// <summary>The map node this battle is fought at.</summary>
        public MapNode Node { get; }

        public bool IsFinished => Engine.Result != BattleResult.Ongoing;
    }
}
