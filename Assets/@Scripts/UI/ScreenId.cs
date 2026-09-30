using System;
using System.Collections.Generic;
using F1.Core;
using F1.Flow;

namespace F1.UI
{
    public enum ScreenId
    {
        Title,
        Lobby,
        NodeMap,
        Battle,
        Reward,
        Settlement,
    }

    /// <summary>Where each screen's prefab is and when it is loaded. One screen is shown at a time.</summary>
    public static class ScreenCatalog
    {
        public static readonly IReadOnlyList<ScreenId> All = (ScreenId[])Enum.GetValues(typeof(ScreenId));

        public static string Address(ScreenId id)
        {
            switch (id)
            {
                case ScreenId.Title: return "ui/app/title-screen";
                case ScreenId.Lobby: return "ui/lobby/lobby-screen";
                case ScreenId.NodeMap: return "ui/expedition/node-map-screen";
                case ScreenId.Battle: return "ui/expedition/battle-screen";
                case ScreenId.Reward: return "ui/expedition/reward-screen";
                case ScreenId.Settlement: return "ui/expedition/settlement-screen";
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static ResourceScope Scope(ScreenId id)
        {
            switch (id)
            {
                case ScreenId.Title: return ResourceScope.App;
                case ScreenId.Lobby: return ResourceScope.Lobby;
                default: return ResourceScope.Expedition;
            }
        }

        /// <summary>The screen that shows a game phase.</summary>
        public static ScreenId ForPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Lobby: return ScreenId.Lobby;
                case GamePhase.NodeMap: return ScreenId.NodeMap;
                case GamePhase.Battle: return ScreenId.Battle;
                case GamePhase.Reward: return ScreenId.Reward;
                case GamePhase.Settlement: return ScreenId.Settlement;
                default: throw new ArgumentOutOfRangeException(nameof(phase), phase, null);
            }
        }
    }
}
