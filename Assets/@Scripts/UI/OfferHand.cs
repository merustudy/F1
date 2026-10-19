using System;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;

namespace F1.UI
{
    /// <summary>
    /// An offer in the hand: a drop of a won battle's loot on the battle screen, or a good of the shop on the node map (Slice B stage 19;
    /// one class for both since 2026-10-20, R17). It goes where the manager says the drop can be taken or the good bought, merges as that
    /// would, and is taken or bought where it is put. The screen says how a placing command of the manager is run (the node map notes the
    /// purchase and spends its pick; the battle screen drops its pick).
    /// </summary>
    public sealed class OfferHand : IOutsideHand
    {
        public enum Source
        {
            /// <summary>A drop of the loot (<see cref="ExpeditionManager.BattleLoot"/>), taken where it is put.</summary>
            Loot,

            /// <summary>A good of the shop's stock (<see cref="ExpeditionManager.ShopStock"/>), bought where it is put.</summary>
            Shop,
        }

        readonly Source _source;
        readonly Action<Action> _run;

        /// <param name="run">Runs the placing command of the manager it is given, with whatever the screen does around it.</param>
        public OfferHand(Source source, int slot, Action<Action> run)
        {
            _source = source;
            Slot = slot;
            _run = run ?? throw new ArgumentNullException(nameof(run));
            ItemOffer offer = source == Source.Loot ? Managers.Expedition.BattleLoot[slot] : Managers.Expedition.ShopStock[slot];
            StaticData data = Managers.Data.Data;
            Item = offer.Kind == OfferKind.Item ? new EquippedItem(data.Items.Get(offer.Id), tier: offer.Tier) : null;
            Bag = offer.Kind == OfferKind.Bag ? data.Bags.Get(offer.Id) : null;
        }

        /// <summary>The slot of the loot or of the stock the offer lies in.</summary>
        public int Slot { get; }

        public EquippedItem Item { get; }
        public BagData Bag { get; }

        public bool CanPlace(int member, Placement at)
        {
            ExpeditionManager manager = Managers.Expedition;
            return _source == Source.Loot ? manager.CanTakeLoot(Slot, member, at) : manager.CanBuyToBoard(Slot, member, at);
        }

        public bool MergesAt(int member, int x, int y)
        {
            ExpeditionManager manager = Managers.Expedition;
            return _source == Source.Loot ? manager.LootMergesAt(Slot, member, x, y) : manager.ShopMergesAt(Slot, member, x, y);
        }

        public void Place(int member, Placement at)
        {
            ExpeditionManager manager = Managers.Expedition;
            int slot = Slot;
            if (_source == Source.Loot)
            {
                _run(() => manager.TakeLoot(slot, member, at));
            }
            else
            {
                _run(() => manager.BuyToBoard(slot, member, at));
            }
        }

        public bool CanPlaceInInventory(Placement at)
        {
            ExpeditionManager manager = Managers.Expedition;
            return _source == Source.Loot ? manager.CanTakeLootToInventoryAt(Slot, at) : manager.CanBuyToInventoryAt(Slot, at);
        }

        public bool MergesInInventoryAt(int x, int y)
        {
            ExpeditionManager manager = Managers.Expedition;
            return _source == Source.Loot ? manager.LootMergesInInventoryAt(Slot, x, y) : manager.ShopMergesInInventoryAt(Slot, x, y);
        }

        public void PlaceInInventory(Placement at)
        {
            ExpeditionManager manager = Managers.Expedition;
            int slot = Slot;
            if (_source == Source.Loot)
            {
                _run(() => manager.TakeLootToInventoryAt(slot, at));
            }
            else
            {
                _run(() => manager.BuyToInventoryAt(slot, at));
            }
        }
    }
}
