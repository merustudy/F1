using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using F1.Save;

namespace F1.Flow
{
    /// <summary>How the battle in progress came back from the save file.</summary>
    public enum BattleResume
    {
        /// <summary>No battle was in progress.</summary>
        None,
        /// <summary>The replay matched the saved digest of the log.</summary>
        Exact,
        /// <summary>The replay worked but differs from what was saved: rules or data changed since.</summary>
        Diverged,
        /// <summary>The recorded inputs are not valid under the current rules; the battle starts over.</summary>
        Restarted,
    }

    /// <summary>
    /// Owns the expedition in progress, its battle session and the settlement report.
    /// Rules are computed by ExpeditionRules and BattleEngine; this class checks the phase, calls
    /// the rule and confirms the result through RunManager, which writes the save file. A finished
    /// battle and a finished expedition are applied at once, never when a screen or an animation
    /// is done.
    /// </summary>
    public sealed class ExpeditionManager
    {
        readonly DataManager _data;
        readonly RunManager _run;

        /// <summary>Events of the current battle that have already been checked for losses to confirm.</summary>
        int _checkedEvents;

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

        /// <summary>How the last <see cref="Restore"/> rebuilt the battle in progress.</summary>
        public BattleResume LastResume { get; private set; }

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

                switch (Expedition.Phase)
                {
                    case ExpeditionPhase.ChoosingReward: return GamePhase.Reward;
                    case ExpeditionPhase.AtCamp: return GamePhase.Camp;
                    case ExpeditionPhase.AtShop: return GamePhase.Shop;
                    default: return GamePhase.NodeMap;
                }
            }
        }

        // ---- Continue ------------------------------------------------------------------------

        /// <summary>
        /// Takes over the expedition RunManager loaded from the save file. Called once at boot, after
        /// RunManager.Load. A battle in progress is rebuilt from its setup and recorded inputs up to
        /// the confirmed time, so everything confirmed before the app closed happens again.
        /// </summary>
        public void Restore()
        {
            ExpeditionState expedition = _run.TakeLoadedExpedition(out BattleRecord record);
            LastResume = BattleResume.None;
            if (expedition == null)
            {
                return;
            }

            Expedition = expedition;
            if (expedition.Phase != ExpeditionPhase.InBattle)
            {
                return;
            }

            BattleSetup setup = ExpeditionRules.BuildBattleSetup(_data.Data, expedition);
            BattleEngine engine;
            try
            {
                engine = BattleEngine.Replay(setup, RunSaveMapper.ToInputs(record), record.ConfirmedTimeMs);
                LastResume = RunSaveMapper.LogHash(record) == BattleLog.Hash(engine.Events) ? BattleResume.Exact : BattleResume.Diverged;
            }
            catch (InvalidOperationException)
            {
                // A recorded input is not possible under the current rules. Nothing of this battle was
                // applied to the expedition yet, so it can be fought again from the start.
                engine = new BattleEngine(setup);
                LastResume = BattleResume.Restarted;
            }

            Battle = new BattleSession(engine, expedition.Map.Get(expedition.CurrentNodeId));
            _checkedEvents = engine.Events.Count;

            // Only a replay under changed rules can come back already ended or at another state.
            // What it shows now is what counts, so it is confirmed at once.
            if (LastResume != BattleResume.Exact || Battle.IsFinished)
            {
                ConfirmBattleIfEnded();
                Commit();
            }
        }

        // ---- Lobby -> expedition -------------------------------------------------------------

        public void Depart(string dungeonId)
        {
            _run.RequireWritable();
            Require(GamePhase.Lobby);
            Expedition = _run.BeginExpedition(dungeonId);
            Commit();
        }

        // ---- Node map ------------------------------------------------------------------------

        /// <summary>Nodes that can be entered now. Empty outside the node map.</summary>
        public IReadOnlyList<MapNode> AvailableNodes()
        {
            return Phase == GamePhase.NodeMap ? ExpeditionRules.AvailableNodes(Expedition) : new List<MapNode>();
        }

        /// <summary>Enters a node: a battle starts there, or the party makes camp, or it goes into the shop.</summary>
        public void EnterNode(int nodeId)
        {
            _run.RequireWritable();
            Require(GamePhase.NodeMap);
            MapNodeKind kind = Expedition.Map.Get(nodeId).Kind;
            if (kind == MapNodeKind.Camp)
            {
                ExpeditionRules.EnterCamp(Expedition, nodeId);
                Commit();
                return;
            }

            if (kind == MapNodeKind.Shop)
            {
                ExpeditionRules.EnterShop(_data.Data, Expedition, nodeId);
                Commit();
                return;
            }

            BattleSetup setup = ExpeditionRules.BeginBattle(_data.Data, Expedition, nodeId);
            Battle = new BattleSession(new BattleEngine(setup), Expedition.Map.Get(nodeId));
            _checkedEvents = Battle.Engine.Events.Count;
            Commit();
        }

        // ---- Camp ----------------------------------------------------------------------------

        /// <summary>Rests at the camp: HP and fatigue come back, and the party goes on to the next floor.</summary>
        public void RestAtCamp()
        {
            _run.RequireWritable();
            Require(GamePhase.Camp);
            ExpeditionRules.RestAtCamp(_data.Data, Expedition);
            Commit();
        }

        // ---- The shop (Slice B stage 17) -----------------------------------------------------

        /// <summary>The shop's offers by slot while the party is at one (a sold slot is null); empty otherwise.</summary>
        public IReadOnlyList<RewardOption> ShopStock => Phase == GamePhase.Shop ? Expedition.Shop.Stock : new List<RewardOption>();

        /// <summary>What an offer costs in region coins.</summary>
        public int PriceOf(RewardOption offer)
        {
            return ExpeditionRules.PriceOf(_data.Data, offer);
        }

        /// <summary>What the next refresh costs at the shop the party is at; 0 elsewhere.</summary>
        public int RefreshCost => Phase == GamePhase.Shop ? ExpeditionRules.RefreshCost(_data.Data, Expedition) : 0;

        /// <summary>True when the coins cover the offer in a slot.</summary>
        public bool CanAfford(int slot)
        {
            return Phase == GamePhase.Shop && ExpeditionRules.CanAfford(_data.Data, Expedition, slot);
        }

        /// <summary>True when the item in a slot could be bought onto that cell now (as a reward would go there).</summary>
        public bool CanBuyToBoard(int slot, int memberIndex, int cell)
        {
            return Phase == GamePhase.Shop && ExpeditionRules.CanBuyToBoard(_data.Data, Expedition, slot, memberIndex, cell);
        }

        public void BuyToBoard(int slot, int memberIndex, int cell)
        {
            _run.RequireWritable();
            Require(GamePhase.Shop);
            ExpeditionRules.BuyToBoard(_data.Data, Expedition, slot, memberIndex, cell);
            Commit();
        }

        /// <summary>True when the item in a slot could be bought straight into the inventory now.</summary>
        public bool CanBuyToInventory(int slot)
        {
            return Phase == GamePhase.Shop && ExpeditionRules.CanBuyToInventory(_data.Data, Expedition, slot);
        }

        public void BuyToInventory(int slot)
        {
            _run.RequireWritable();
            Require(GamePhase.Shop);
            ExpeditionRules.BuyToInventory(_data.Data, Expedition, slot);
            Commit();
        }

        /// <summary>True when the potion in a slot could be bought now: the coins cover it and a potion slot is empty.</summary>
        public bool CanBuyPotion(int slot)
        {
            return Phase == GamePhase.Shop && ExpeditionRules.CanBuyPotion(_data.Data, Expedition, slot);
        }

        public void BuyPotion(int slot)
        {
            _run.RequireWritable();
            Require(GamePhase.Shop);
            ExpeditionRules.BuyPotion(_data.Data, Expedition, slot);
            Commit();
        }

        /// <summary>Whether buying the item in a slot onto a cell would merge it into the item there.</summary>
        public bool ShopMergesAt(int slot, int memberIndex, int cell)
        {
            return Phase == GamePhase.Shop && ExpeditionRules.ShopMergesAt(Expedition, slot, memberIndex, cell);
        }

        /// <summary>True when the stock can be refreshed now: at a shop, with the coins for it.</summary>
        public bool CanRefreshShop => Phase == GamePhase.Shop && ExpeditionRules.CanRefreshShop(_data.Data, Expedition);

        public void RefreshShop()
        {
            _run.RequireWritable();
            Require(GamePhase.Shop);
            ExpeditionRules.RefreshShop(_data.Data, Expedition);
            Commit();
        }

        /// <summary>Leaves the shop: the party goes on to the next floor.</summary>
        public void LeaveShop()
        {
            _run.RequireWritable();
            Require(GamePhase.Shop);
            ExpeditionRules.LeaveShop(Expedition);
            Commit();
        }

        /// <summary>The region coins the battle on show brought: those of its node when it was won, 0 otherwise (the boss brings none).</summary>
        public int BattleCoins => Battle != null && Battle.IsFinished && Battle.Engine.Result == BattleResult.Victory ? ExpeditionRules.CoinsFor(_data.Data, Battle.Node) : 0;

        // ---- Tiers ---------------------------------------------------------------------------

        /// <summary>Whether putting an item of a board or the inventory on a cell would merge it into the item there (a tier up).</summary>
        public bool MergesAt(EquippedItem item, int memberIndex, int cell)
        {
            return IsBetweenBattles && ExpeditionRules.MergesAt(Expedition, item, memberIndex, cell);
        }

        /// <summary>Whether taking an item reward onto a cell would merge it into the item there.</summary>
        public bool RewardMergesAt(int optionIndex, int memberIndex, int cell)
        {
            return Phase == GamePhase.Reward && ExpeditionRules.RewardMergesAt(Expedition, optionIndex, memberIndex, cell);
        }

        /// <summary>Whether some board holds what the item would merge into.</summary>
        public bool HasMergeTarget(EquippedItem item)
        {
            return IsBetweenBattles && ExpeditionRules.HasMergeTarget(Expedition, item);
        }

        /// <summary>Whether the item at a cell of a member's board can go a tier up at the camp (its upkeep).</summary>
        public bool CanUpgradeAtCamp(int memberIndex, int cell)
        {
            return Phase == GamePhase.Camp && ExpeditionRules.CanUpgradeAtCamp(Expedition, memberIndex, cell);
        }

        /// <summary>The camp's upkeep: the item at a cell goes a tier up, and the party goes on.</summary>
        public void UpgradeAtCamp(int memberIndex, int cell)
        {
            _run.RequireWritable();
            Require(GamePhase.Camp);
            ExpeditionRules.UpgradeAtCamp(Expedition, memberIndex, cell);
            Commit();
        }

        // ---- Battle --------------------------------------------------------------------------

        /// <summary>
        /// Moves battle time forward. Does nothing once the battle has ended, and nothing while the
        /// last change is still unsaved: time must not run past a state that is not confirmed.
        /// </summary>
        public void AdvanceBattle(int deltaMs)
        {
            Require(GamePhase.Battle);
            if (deltaMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaMs), deltaMs, "Battle time cannot go backwards.");
            }

            if (Battle.IsFinished || deltaMs == 0 || _run.IsSaveBlocked)
            {
                return;
            }

            Battle.Engine.AdvanceTo(Battle.Engine.TimeMs + deltaMs);

            // Time passing alone is not saved. A mercenary falling or dying is: once it is on disk,
            // continuing replays at least up to that moment and the loss happens again.
            bool lossHappened = PartyLossSinceLastCheck();
            if (ConfirmBattleIfEnded() || lossHappened)
            {
                Commit();
            }
        }

        /// <summary>Uses a potion at the current battle time. False when it cannot be used now.</summary>
        public bool TryUsePotion(int potionSlot, int partyIndex)
        {
            _run.RequireWritable();
            Require(GamePhase.Battle);
            if (!Battle.Engine.TryUsePotion(potionSlot, partyIndex))
            {
                return false;
            }

            Commit();
            return true;
        }

        /// <summary>Attempts to retreat at the current battle time. False when no attempt can be made now.</summary>
        public bool TryRetreat()
        {
            _run.RequireWritable();
            Require(GamePhase.Battle);
            if (!Battle.Engine.TryRetreat())
            {
                return false;
            }

            // A failed attempt is saved too, so closing the app cannot buy another roll.
            ConfirmBattleIfEnded();
            Commit();
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

        // ---- Rewards, the item boards and the inventory --------------------------------------

        /// <summary>True when the item reward could be put at that cell now: into free cells, or in place of the item there.</summary>
        public bool CanPlaceReward(int optionIndex, int memberIndex, int cell)
        {
            return Phase == GamePhase.Reward && ExpeditionRules.CanPlaceReward(_data.Data, Expedition, optionIndex, memberIndex, cell);
        }

        public void TakeItemReward(int optionIndex, int memberIndex, int cell)
        {
            _run.RequireWritable();
            Require(GamePhase.Reward);
            ExpeditionRules.TakeItemReward(_data.Data, Expedition, optionIndex, memberIndex, cell);
            Commit();
        }

        /// <summary>True when the item reward fits the inventory's free cells now.</summary>
        public bool CanTakeRewardToInventory(int optionIndex)
        {
            return Phase == GamePhase.Reward && ExpeditionRules.CanTakeRewardToInventory(_data.Data, Expedition, optionIndex);
        }

        public void TakeItemRewardToInventory(int optionIndex)
        {
            _run.RequireWritable();
            Require(GamePhase.Reward);
            ExpeditionRules.TakeItemRewardToInventory(_data.Data, Expedition, optionIndex);
            Commit();
        }

        /// <summary>False when every potion slot is full, so a potion reward cannot be taken.</summary>
        public bool CanTakePotionReward => Phase == GamePhase.Reward && ExpeditionRules.FreePotionSlot(Expedition) >= 0;

        public void TakePotionReward(int optionIndex)
        {
            _run.RequireWritable();
            Require(GamePhase.Reward);
            ExpeditionRules.TakePotionReward(Expedition, optionIndex);
            Commit();
        }

        public void SkipReward()
        {
            _run.RequireWritable();
            Require(GamePhase.Reward);
            ExpeditionRules.SkipReward(Expedition);
            Commit();
        }

        /// <summary>True when the item at a cell can go to a cell of a board now (free cells, or trading places with the item there).</summary>
        public bool CanMoveItem(int fromMember, int fromCell, int toMember, int toCell)
        {
            return IsBetweenBattles && ExpeditionRules.CanMoveItem(Expedition, fromMember, fromCell, toMember, toCell);
        }

        public void MoveItem(int fromMember, int fromCell, int toMember, int toCell)
        {
            _run.RequireWritable();
            RequireBetweenBattles();
            ExpeditionRules.MoveItem(Expedition, fromMember, fromCell, toMember, toCell);
            Commit();
        }

        /// <summary>True when a cell holds an item that can be picked up now; where it may go is asked with the other Can... queries.</summary>
        public bool CanPickItem(int memberIndex, int cell)
        {
            return IsBetweenBattles && ExpeditionRules.CanPickItem(Expedition, memberIndex, cell);
        }

        /// <summary>True when the item at a cell can go to the inventory now: it must fit the inventory's free cells.</summary>
        public bool CanMoveToInventory(int memberIndex, int cell)
        {
            return IsBetweenBattles && ExpeditionRules.CanMoveToInventory(_data.Data, Expedition, memberIndex, cell);
        }

        public void MoveToInventory(int memberIndex, int cell)
        {
            _run.RequireWritable();
            RequireBetweenBattles();
            ExpeditionRules.MoveToInventory(_data.Data, Expedition, memberIndex, cell);
            Commit();
        }

        public bool CanPlaceFromInventory(int inventoryIndex, int memberIndex, int cell)
        {
            return IsBetweenBattles && ExpeditionRules.CanPlaceFromInventory(_data.Data, Expedition, inventoryIndex, memberIndex, cell);
        }

        public void PlaceFromInventory(int inventoryIndex, int memberIndex, int cell)
        {
            _run.RequireWritable();
            RequireBetweenBattles();
            ExpeditionRules.PlaceFromInventory(_data.Data, Expedition, inventoryIndex, memberIndex, cell);
            Commit();
        }

        /// <summary>True when the member can move to that row now: between battles, to a row where another living member stands.</summary>
        public bool CanMoveToRow(int memberIndex, int row)
        {
            return IsBetweenBattles && ExpeditionRules.CanMoveToRow(Expedition, memberIndex, row);
        }

        /// <summary>Moves a member to a row. The member standing there takes the mover's old row.</summary>
        public void MoveToRow(int memberIndex, int row)
        {
            _run.RequireWritable();
            RequireBetweenBattles();
            ExpeditionRules.MoveToRow(Expedition, memberIndex, row);
            Commit();
        }

        // ---- Settlement ----------------------------------------------------------------------

        /// <summary>The screen is done showing the settlement report.</summary>
        public void AcknowledgeReport()
        {
            Require(GamePhase.Settlement);
            Report = null;
        }

        bool IsBetweenBattles => Phase == GamePhase.NodeMap || Phase == GamePhase.Reward || Phase == GamePhase.Camp || Phase == GamePhase.Shop;

        /// <summary>
        /// Applies an ended battle to the expedition, and an ended expedition to the run, in the
        /// same call that ended it. Returns true when the battle had ended.
        /// </summary>
        bool ConfirmBattleIfEnded()
        {
            if (!Battle.IsFinished)
            {
                return false;
            }

            ExpeditionRules.CompleteBattle(_data.Data, Expedition, Battle.Engine);
            if (Expedition.Phase == ExpeditionPhase.Finished)
            {
                Report = _run.Settle(Expedition);
                Expedition = null;
            }

            return true;
        }

        /// <summary>True when a party member fell to 0 HP or died in the events not looked at yet.</summary>
        bool PartyLossSinceLastCheck()
        {
            IReadOnlyList<BattleEvent> events = Battle.Engine.Events;
            bool loss = false;
            for (; _checkedEvents < events.Count; _checkedEvents++)
            {
                BattleEvent e = events[_checkedEvents];
                bool isLoss = e.Kind == BattleEventKind.Died || e.Kind == BattleEventKind.DogEntered;
                loss |= isLoss && !e.Target.IsNone && e.Target.Side == BattleSide.Party;
            }

            return loss;
        }

        /// <summary>
        /// Confirms the current state: the run with the expedition, and the battle record while a
        /// battle is being fought. Every command that changed something ends with this.
        /// </summary>
        void Commit()
        {
            if (Expedition == null)
            {
                _run.Save(null);
                return;
            }

            BattleEngine fighting = Expedition.Phase == ExpeditionPhase.InBattle ? Battle.Engine : null;
            _run.Save(RunSaveMapper.ToRecord(Expedition, fighting));
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
                throw new InvalidOperationException($"Not allowed in phase {Phase}; needs the node map, the reward choice, a camp or a shop.");
            }
        }
    }
}
