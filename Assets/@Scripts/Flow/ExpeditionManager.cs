using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;

namespace F1.Flow
{
    /// <summary>
    /// Owns the expedition in progress, its battle session and the settlement report.
    /// Rules are computed by ExpeditionRules and BattleEngine; this class checks the phase, calls
    /// the rule and confirms the result. A finished battle and a finished expedition are applied
    /// at once, never when a screen or an animation is done.
    /// </summary>
    public sealed class ExpeditionManager
    {
        readonly DataManager _data;
        readonly RunManager _run;

        public ExpeditionManager(DataManager data, RunManager run)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _run.RunStarted += DropEverything;
        }

        /// <summary>The expedition in progress, or null. Read it; only rules change it.</summary>
        public ExpeditionState Expedition { get; private set; }

        /// <summary>The battle being fought or just ended, or null.</summary>
        public BattleSession Battle { get; private set; }

        /// <summary>The settlement of the expedition that just ended, until it is acknowledged; otherwise null.</summary>
        public SettlementReport Report { get; private set; }

        public GamePhase Phase
        {
            get
            {
                if (Battle != null)
                {
                    return GamePhase.Battle;
                }

                if (Report != null)
                {
                    return GamePhase.Settlement;
                }

                if (Expedition == null)
                {
                    return GamePhase.Lobby;
                }

                return Expedition.Phase == ExpeditionPhase.ChoosingReward ? GamePhase.Reward : GamePhase.NodeMap;
            }
        }

        // ---- Lobby -> expedition -------------------------------------------------------------

        public void Depart(string dungeonId)
        {
            Require(GamePhase.Lobby);
            Expedition = _run.BeginExpedition(dungeonId);
        }

        // ---- Node map ------------------------------------------------------------------------

        /// <summary>Nodes that can be entered now. Empty outside the node map.</summary>
        public IReadOnlyList<MapNode> AvailableNodes()
        {
            return Phase == GamePhase.NodeMap ? ExpeditionRules.AvailableNodes(Expedition) : new List<MapNode>();
        }

        public void EnterNode(int nodeId)
        {
            Require(GamePhase.NodeMap);
            BattleSetup setup = ExpeditionRules.BeginBattle(_data.Data, Expedition, nodeId);
            Battle = new BattleSession(new BattleEngine(setup), Expedition.Map.Get(nodeId));
        }

        // ---- Battle --------------------------------------------------------------------------

        /// <summary>Moves battle time forward. Does nothing once the battle has ended.</summary>
        public void AdvanceBattle(int deltaMs)
        {
            Require(GamePhase.Battle);
            if (deltaMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaMs), deltaMs, "Battle time cannot go backwards.");
            }

            if (Battle.IsFinished || deltaMs == 0)
            {
                return;
            }

            Battle.Engine.AdvanceTo(Battle.Engine.TimeMs + deltaMs);
            ConfirmBattleIfEnded();
        }

        /// <summary>Uses a potion at the current battle time. False when it cannot be used now.</summary>
        public bool TryUsePotion(int potionSlot, int partyIndex)
        {
            Require(GamePhase.Battle);
            return Battle.Engine.TryUsePotion(potionSlot, partyIndex);
        }

        /// <summary>Attempts to retreat at the current battle time. False when no attempt can be made now.</summary>
        public bool TryRetreat()
        {
            Require(GamePhase.Battle);
            if (!Battle.Engine.TryRetreat())
            {
                return false;
            }

            ConfirmBattleIfEnded();
            return true;
        }

        /// <summary>The screen is done showing the ended battle.</summary>
        public void CloseBattle()
        {
            Require(GamePhase.Battle);
            if (!Battle.IsFinished)
            {
                throw new InvalidOperationException("The battle has not ended.");
            }

            Battle = null;
        }

        // ---- Rewards and the item board ------------------------------------------------------

        public void TakeItemReward(int optionIndex, int memberIndex, int slotIndex)
        {
            Require(GamePhase.Reward);
            ExpeditionRules.TakeItemReward(_data.Data, Expedition, optionIndex, memberIndex, slotIndex);
        }

        /// <summary>False when every potion slot is full, so a potion reward cannot be taken.</summary>
        public bool CanTakePotionReward => Phase == GamePhase.Reward && ExpeditionRules.FreePotionSlot(Expedition) >= 0;

        public void TakePotionReward(int optionIndex)
        {
            Require(GamePhase.Reward);
            ExpeditionRules.TakePotionReward(Expedition, optionIndex);
        }

        public void SkipReward()
        {
            Require(GamePhase.Reward);
            ExpeditionRules.SkipReward(Expedition);
        }

        public void SwapItems(int memberA, int slotA, int memberB, int slotB)
        {
            RequireBetweenBattles();
            ExpeditionRules.SwapItems(Expedition, memberA, slotA, memberB, slotB);
        }

        public bool CanSetRow(int memberIndex, BattleRow row)
        {
            return IsBetweenBattles && ExpeditionRules.CanSetRow(_data.Data, Expedition, memberIndex, row);
        }

        public void SetRow(int memberIndex, BattleRow row)
        {
            RequireBetweenBattles();
            ExpeditionRules.SetRow(_data.Data, Expedition, memberIndex, row);
        }

        // ---- Settlement ----------------------------------------------------------------------

        /// <summary>The screen is done showing the settlement report.</summary>
        public void AcknowledgeReport()
        {
            Require(GamePhase.Settlement);
            Report = null;
        }

        bool IsBetweenBattles => Phase == GamePhase.NodeMap || Phase == GamePhase.Reward;

        /// <summary>
        /// Applies an ended battle to the expedition, and an ended expedition to the run, in the
        /// same call that ended it.
        /// </summary>
        void ConfirmBattleIfEnded()
        {
            if (!Battle.IsFinished)
            {
                return;
            }

            ExpeditionRules.CompleteBattle(_data.Data, Expedition, Battle.Engine);
            if (Expedition.Phase == ExpeditionPhase.Finished)
            {
                Report = _run.Settle(Expedition);
                Expedition = null;
            }
        }

        void DropEverything()
        {
            Expedition = null;
            Battle = null;
            Report = null;
        }

        void Require(GamePhase phase)
        {
            if (Phase != phase)
            {
                throw new InvalidOperationException($"Not allowed in phase {Phase}; needs {phase}.");
            }
        }

        void RequireBetweenBattles()
        {
            if (!IsBetweenBattles)
            {
                throw new InvalidOperationException($"Not allowed in phase {Phase}; needs the node map or the reward choice.");
            }
        }
    }
}
