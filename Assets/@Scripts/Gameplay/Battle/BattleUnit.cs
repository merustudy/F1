using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>Runtime state of one item slot during a battle.</summary>
    public sealed class BattleItemState
    {
        internal BattleItemState(int slotIndex, EquippedItem equipped, bool active, int cooldownMs)
        {
            SlotIndex = slotIndex;
            Equipped = equipped;
            Active = active;
            CooldownMs = cooldownMs;
            NextFireMs = cooldownMs;
        }

        public int SlotIndex { get; }
        public EquippedItem Equipped { get; }

        /// <summary>False when the owner does not stand in the row the item requires. An inactive item never fires.</summary>
        public bool Active { get; }

        /// <summary>Effective cooldown after modifiers.</summary>
        public int CooldownMs { get; }

        public int NextFireMs { get; internal set; }
    }

    /// <summary>Runtime state of one unit during a battle. Only the engine changes it.</summary>
    public sealed class BattleUnit
    {
        internal BattleUnit(BattleSide side, int index, BattleUnitSetup setup, List<BattleItemState> items)
        {
            Side = side;
            Index = index;
            Setup = setup;
            Items = items;
            Hp = setup.Hp;
            Alive = true;
        }

        public BattleSide Side { get; }

        /// <summary>Position in the unit order of its side: front row first, then rear row.</summary>
        public int Index { get; }

        public BattleUnitSetup Setup { get; }
        public IReadOnlyList<BattleItemState> Items { get; }

        public UnitRef Ref => new UnitRef(Side, Index);
        public BattleRow Row => Setup.Row;
        public int MaxHp => Setup.MaxHp;

        public int Hp { get; internal set; }
        public int Shield { get; internal set; }
        public int Burn { get; internal set; }
        public bool Alive { get; internal set; }

        /// <summary>At 0 HP but not dead. Hits may now trigger death rolls.</summary>
        public bool InDog { get; internal set; }

        /// <summary>Battle time until which hits are only counted, not rolled.</summary>
        public int GraceEndMs { get; internal set; }

        public int GraceHits { get; internal set; }
    }
}
