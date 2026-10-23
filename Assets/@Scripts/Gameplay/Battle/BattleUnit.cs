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
        /// the owner's side dies, because the span is counted on the living line. An item that activates when a battle starts (Slice D stage 39)
        /// is active where it works, but it fired once at 0 ms and never charges (<see cref="Cycles"/>).
        /// </summary>
        public bool Active { get; internal set; }

        /// <summary>Whether the item charges and fires on its cooldown (<see cref="ItemData.Cycles"/>): not one without effects, nor one that fires when a battle starts.</summary>
        public bool Cycles => Equipped.Item.Cycles;

        /// <summary>Effective cooldown after modifiers.</summary>
        public int CooldownMs { get; }

        public int NextFireMs { get; internal set; }

        /// <summary>What the item deals more by the stars on it (Slice B stage 20): added to a weapon's damage before the weapon-power passive.</summary>
        public int StarDamage { get; }
    }

    /// <summary>Runtime state of one unit during a battle. Only the engine changes it.</summary>
    public sealed class BattleUnit
    {
        List<BattleItemState> _items;

        internal BattleUnit(BattleSide side, int index, BattleUnitSetup setup, List<BattleItemState> items)
        {
            Side = side;
            Index = index;
            Setup = setup;
            _items = items;
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

        /// <summary>The items the unit carries now: the setup's, or the current phase's for a boss that has entered one (stage 28).</summary>
        public IReadOnlyList<BattleItemState> Items => _items;

        /// <summary>The phase a boss is in: 0 at the start, then 1.. as its HP falls through the phases' percents (stage 28).</summary>
        public int Phase { get; internal set; }

        /// <summary>Where the items lie, for the screen: the setup's layout, or the current phase's.</summary>
        public BoardLayout Layout => Phase == 0 || Setup.Phases == null ? Setup.Layout : Setup.Phases[Phase - 1].Layout;

        /// <summary>The equipped items of the unit now, as the setup lists them (for a screen that lays out a board without a layout).</summary>
        public IReadOnlyList<EquippedItem> EquippedNow => Phase == 0 || Setup.Phases == null ? Setup.Items : Setup.Phases[Phase - 1].Items;

        internal void EnterPhase(int phase, List<BattleItemState> items)
        {
            Phase = phase;
            _items = items;
        }

        public UnitRef Ref => new UnitRef(Side, Index);
        public int MaxHp => Setup.MaxHp + MaxHpBonus;

        /// <summary>What the maximum HP items raised it by for this battle (Design/35 판정 6). The expedition's own maximum is <see cref="BattleUnitSetup.MaxHp"/>.</summary>
        public int MaxHpBonus { get; internal set; }

        /// <summary>
        /// The row the unit stands in now. It starts at Setup.Row and goes down when the rows in
        /// front empty. A dead unit keeps the row it died in.
        /// </summary>
        public int Row { get; internal set; }

        public int Hp { get; internal set; }
        public int Shield { get; internal set; }
        public int Burn { get; internal set; }

        /// <summary>The effect vocabulary's states (Slice D stage 44, Docs/Design/02_Combat_System.md §5): bleed and regen stacks, and thorns in hundredths.</summary>
        public int Bleed { get; internal set; }
        public int Regen { get; internal set; }
        public int Thorns { get; internal set; }

        /// <summary>The seven states of stage 45 (Docs/Design/02_Combat_System.md §5), in stacks that lose one at every tick.</summary>
        public int Freeze { get; internal set; }
        public int Blind { get; internal set; }
        public int Aim { get; internal set; }
        public int Keen { get; internal set; }
        public int Fury { get; internal set; }
        public int Haste { get; internal set; }
        public int Vulnerable { get; internal set; }
        public bool Alive { get; internal set; }

        /// <summary>The unit's stacks of one of the seven states.</summary>
        public int StacksOf(EffectKind state)
        {
            switch (state)
            {
                case EffectKind.Freeze: return Freeze;
                case EffectKind.Blind: return Blind;
                case EffectKind.Aim: return Aim;
                case EffectKind.Keen: return Keen;
                case EffectKind.Fury: return Fury;
                case EffectKind.Haste: return Haste;
                case EffectKind.Vulnerable: return Vulnerable;
                default: throw new System.ArgumentOutOfRangeException(nameof(state), state, "Not a state.");
            }
        }

        internal void SetStacks(EffectKind state, int stacks)
        {
            switch (state)
            {
                case EffectKind.Freeze: Freeze = stacks; break;
                case EffectKind.Blind: Blind = stacks; break;
                case EffectKind.Aim: Aim = stacks; break;
                case EffectKind.Keen: Keen = stacks; break;
                case EffectKind.Fury: Fury = stacks; break;
                case EffectKind.Haste: Haste = stacks; break;
                case EffectKind.Vulnerable: Vulnerable = stacks; break;
                default: throw new System.ArgumentOutOfRangeException(nameof(state), state, "Not a state.");
            }
        }

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
