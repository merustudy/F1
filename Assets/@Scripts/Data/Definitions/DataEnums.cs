namespace F1.Data
{
    public enum BattleRow
    {
        Front,
        Rear,
    }

    public enum ItemCategory
    {
        /// <summary>"Weapon hit" and "weapon power" passives apply to these.</summary>
        Weapon,
        Support,
    }

    /// <summary>The row the owner must actually stand in for the item to work.</summary>
    public enum RowRequirement
    {
        Any,
        Front,
        Rear,
    }

    public enum EffectKind
    {
        Damage,
        Heal,
        Shield,
        Burn,
    }

    /// <summary>"Enemy" and "Ally" are relative to the owner of the item.</summary>
    public enum TargetMode
    {
        EnemyFront,
        EnemyRear,
        EnemyAll,
        Self,
        AllyLowestHp,
        AllyAll,
    }

    public enum PassiveTrigger
    {
        BattleStart,
        /// <summary>A damage effect of one of the unit's weapon items was applied to a target.</summary>
        WeaponHit,
        /// <summary>A heal effect of one of the unit's items was applied to a target.</summary>
        Heal,
        /// <summary>A standing modifier, checked whenever it is relevant.</summary>
        Always,
    }

    public enum PassiveCondition
    {
        None,
        Front,
        Rear,
        SelfInDog,
    }

    public enum PassiveEffect
    {
        Shield,
        Burn,
        WeaponPowerPercent,
    }

    public enum PassiveTarget
    {
        Self,
        AllyAll,
        /// <summary>The unit the triggering effect was applied to.</summary>
        EventTarget,
    }

    public enum PotionEffect
    {
        Heal,
        Shield,
    }
}
