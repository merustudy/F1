using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>Runtime state of one item slot during a battle.</summary>
    public sealed class BattleItemState
    {
        internal BattleItemState(int slotIndex, EquippedItem equipped, bool active, int cooldownMs, int starDamage)
        {
            SlotIndex = slotIndex;
            Equipped = equipped;
            Active = active;
            CooldownMs = cooldownMs;
            NextFireMs = cooldownMs;
            StarDamage = starDamage;
        }

        /// <summary>Position on the owner's board: the activation order.</summary>
        public int SlotIndex { get; }
        public EquippedItem Equipped { get; }

        /// <summary>
        /// False while the owner stands outside the span of the line the item works in, and always for an item that never activates
        /// (<see cref="ItemData.IsPassive"/>, Slice B stage 20). An inactive
        /// item does not fill its cooldown and never fires. It is judged again whenever someone on
        /// the owner's side dies, because the span is counted on the living line.
        /// </summary>
        public bool Active { get; internal set; }

        /// <summary>Effective cooldown after modifiers.</summary>
        public int CooldownMs { get; }

        public int NextFireMs { get; internal set; }

        /// <summary>What the item deals more by the stars on it (Slice B stage 20): added to a weapon's damage before the weapon-power passive.</summary>
        public int StarDamage { get; }
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
            Row = setup.Row;
            Hp = setup.Hp;
            Alive = true;
            Fatigue = setup.Fatigue;
            State = setup.FatigueState;
        }

        public BattleSide Side { get; }

        /// <summary>Position in the unit order of its side: row 1 first. It never changes, even when the unit advances.</summary>
        public int Index { get; }

        public BattleUnitSetup Setup { get; }
        public IReadOnlyList<BattleItemState> Items { get; }

        public UnitRef Ref => new UnitRef(Side, Index);
        public int MaxHp => Setup.MaxHp;

        /// <summary>
        /// The row the unit stands in now. It starts at Setup.Row and goes down when the rows in
        /// front empty. A dead unit keeps the row it died in.
        /// </summary>
        public int Row { get; internal set; }

        public int Hp { get; internal set; }
        public int Shield { get; internal set; }
        public int Burn { get; internal set; }
        public bool Alive { get; internal set; }

        /// <summary>At 0 HP but not dead. Hits may now trigger death rolls.</summary>
        public bool InDog { get; internal set; }

        /// <summary>Battle time until which hits are only counted, not rolled.</summary>
        public int GraceEndMs { get; internal set; }

        public int GraceHits { get; internal set; }

        /// <summary>The unit's fatigue now (a party unit's; an enemy's stays 0), and the affliction or virtue it is in, or null (Docs/Design/04_Lobby_100Day_Economy.md §3).</summary>
        public int Fatigue { get; internal set; }
        public FatigueStateData State { get; internal set; }
    }
}
