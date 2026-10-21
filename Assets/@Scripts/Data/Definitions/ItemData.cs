using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// One effect of an item and its size at Common (Slice B stage 22, Docs/Design/02_Combat_System.md §4): damage from <see cref="Min"/> to
    /// <see cref="Max"/> in hundredths (<see cref="Centi"/>); a heal or a shield one value in hundredths; a burn its stacks. The item's tier
    /// multiplies it, and an enemy's power percent too (<c>EquippedItem</c>).
    /// </summary>
    public sealed class ItemEffect
    {
        /// <param name="reach">
        /// How many enemies are hit, counted from the front (EnemyFront) or from the back (EnemyBack).
        /// 0 for every other target.
        /// </param>
        /// <param name="min">The least damage, or the one value of any other effect.</param>
        /// <param name="max">The most damage; the same as <paramref name="min"/> for any other effect.</param>
        [JsonConstructor]
        public ItemEffect(EffectKind kind, TargetMode target, int reach, int min, int max)
        {
            if (min < 1)
            {
                throw new DataException($"Effect {kind} must be at least 1 (0.01 for damage, a heal, a shield or thorns; one stack for a burn, a bleed, a regen or a cleanse; one millisecond for a stagger or a charge).");
            }

            if (kind == EffectKind.Damage ? max < min : max != min)
            {
                throw new DataException(kind == EffectKind.Damage
                    ? "Damage needs its least no larger than its most."
                    : $"Effect {kind} has one value, not a range.");
            }

            // Any kind may go to either side (2026-10-21, the user: "모든 효과는 적/아군 서로 가리지 않고"): the data names the target. A blow on one's
            // own side never misses and never crits (Docs/Design/02_Combat_System.md §4).
            if (TakesReach(target))
            {
                if (reach < 1 || reach > BattleRows.Count)
                {
                    throw new DataException($"Effect target {target} needs a Reach of 1..{BattleRows.Count}.");
                }
            }
            else if (reach != 0)
            {
                throw new DataException($"Effect target {target} does not take a Reach.");
            }

            Kind = kind;
            Target = target;
            Reach = reach;
            Min = min;
            Max = max;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public EffectKind Kind { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public TargetMode Target { get; }

        /// <summary>How many enemies are hit from the end <see cref="Target"/> counts from. 0 when the target takes no reach.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Reach { get; }

        /// <summary>The least damage at Common in hundredths, or the one value of a heal or a shield (hundredths) or a burn (stacks).</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Min { get; }

        /// <summary>The most damage at Common in hundredths; <see cref="Min"/> for every other effect.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Max { get; }

        public static bool TargetsEnemy(TargetMode target)
        {
            return TakesReach(target) || target == TargetMode.EnemyAll;
        }

        /// <summary>Whether an effect's value counts stacks (a burn, a bleed, a regen, a cleanse, and the seven states of stage 45): a whole number, rounded half up by a stat.</summary>
        public static bool CountsStacks(EffectKind kind)
        {
            return kind == EffectKind.Burn || kind == EffectKind.Bleed || kind == EffectKind.Regen || kind == EffectKind.Cleanse || IsState(kind);
        }

        /// <summary>
        /// Whether the kind is one of the seven states of stage 45 (Docs/Design/02_Combat_System.md §5): stacks on a unit that change a number while they
        /// last and lose one at every tick.
        /// </summary>
        public static bool IsState(EffectKind kind)
        {
            return kind == EffectKind.Freeze || kind == EffectKind.Blind || kind == EffectKind.Aim || kind == EffectKind.Keen || kind == EffectKind.Fury
                || kind == EffectKind.Haste || kind == EffectKind.Vulnerable;
        }

        /// <summary>
        /// Whether the kind hurts the unit it lands on: damage, burn, bleed, stagger, freeze, blind, vulnerable. A physical one of these rolls its hit when it
        /// goes to the other side; a cleanse takes the stacked ones off (Docs/Design/02_Combat_System.md §5).
        /// </summary>
        public static bool Hurts(EffectKind kind)
        {
            return kind == EffectKind.Damage || kind == EffectKind.Burn || kind == EffectKind.Bleed || kind == EffectKind.Stagger
                || kind == EffectKind.Freeze || kind == EffectKind.Blind || kind == EffectKind.Vulnerable;
        }

        /// <summary>Whether an effect's value is a whole number in the data: stacks, or milliseconds (a stagger, a charge). The rest are hundredths.</summary>
        public static bool CountsWhole(EffectKind kind)
        {
            return CountsStacks(kind) || kind == EffectKind.Stagger || kind == EffectKind.Charge;
        }

        /// <summary>True for the targets that are counted from one end of the enemy line.</summary>
        public static bool TakesReach(TargetMode target)
        {
            return target == TargetMode.EnemyFront || target == TargetMode.EnemyBack;
        }

        /// <summary>
        /// A value of this effect at a tier's percent (<see cref="BalanceData.TierPercent"/>) and a power percent (100 for the party's items, an
        /// enemy's own for its items): value x tierPercent x powerPercent / 10000, rounded down, at least 1.
        /// </summary>
        public static int Scaled(int value, int tierPercent, int powerPercent)
        {
            long scaled = (long)value * tierPercent * powerPercent / 10000;
            return scaled < 1 ? 1 : (int)scaled;
        }
    }

    /// <summary>
    /// One star square of an item (Slice B stage 20, Docs/Design/02_Combat_System.md §4: Backpack Battles' stars): a square round the
    /// item, counted from the top-left square of its unturned shape. It turns with the item.
    /// </summary>
    public readonly struct StarSquare
    {
        [JsonConstructor]
        public StarSquare(int x, int y)
        {
            X = x;
            Y = y;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public int X { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Y { get; }
    }

    /// <summary>
    /// A battle card. Items exist only inside a dungeon and activate on their own cooldown (an item without effects never does: stage 20), or
    /// once when a battle starts (the armour, Slice D stage 39). Items are not relics: relics are a different definition, file and type.
    /// </summary>
    public sealed class ItemData
    {
        public const string DefinitionName = "Item";
        public const int MaxEffects = 2;
        public const int MinCooldownMs = 100;


        [JsonConstructor]
        public ItemData(
            string id,
            LocalizedText name,
            ItemCategory category,
            int width,
            int height,
            int cooldownMs,
            RowSpan rows,
            IReadOnlyList<ItemEffect> effects,
            int shopWeight,
            string icon = null,
            int price = 0,
            bool melee = false,
            IReadOnlyList<StarSquare> stars = null,
            int starDamage = 0,
            int critChancePercent = 0,
            bool unique = false,
            int dropWeight = 0,
            ItemSchool? school = null,
            ItemTrigger trigger = ItemTrigger.Cooldown,
            int critTakenPercent = 0,
            int critDamagePercent = 0,
            Stat? reqStat = null,
            int reqValue = 0)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            if (dropWeight < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': DropWeight cannot be negative.");
            }

            DropWeight = dropWeight;
            // What its owner needs to wield it (Slice D stage 42, Docs/Design/02_Combat_System.md §4 "장비 요구 스탯"): a stat and the least value of it, both
            // or neither, on equipment (a weapon, defensive gear) only. Whether the value lies within StatMin..StatMax is the dataset's check (StaticData).
            if ((reqStat == null) != (reqValue == 0))
            {
                throw new DataException($"{DefinitionName} '{id}': ReqStat and ReqValue come together (both, or neither).");
            }

            if (reqValue < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': ReqValue cannot be negative.");
            }

            if (reqStat != null && category != ItemCategory.Weapon && category != ItemCategory.Armor)
            {
                throw new DataException($"{DefinitionName} '{id}': only equipment (a weapon or defensive gear) has a ReqStat.");
            }

            ReqStat = reqStat;
            ReqValue = reqValue;
            // Physical or magic (2026-10-20 스탯 개정, Docs/Design/02_Combat_System.md §4·§5): a weapon and an attack item are physical unless the data says
            // Magic; defensive gear may say (its cooldown then grows with dexterity); a support item and anything else has none (a support item's power
            // grows with wisdom all the same).
            if ((category == ItemCategory.Support || category == ItemCategory.Other) && school != null)
            {
                throw new DataException($"{DefinitionName} '{id}': only a weapon, an attack item or defensive gear has a School.");
            }

            School = school ?? (category == ItemCategory.Weapon || category == ItemCategory.Attack ? ItemSchool.Physical : (ItemSchool?)null);
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (width < 1 || width > BoardFrame.MaxSide || height < 1 || height > BoardFrame.MaxSide)
            {
                throw new DataException($"{DefinitionName} '{id}': Width and Height must be 1..{BoardFrame.MaxSide}.");
            }

            stars = stars ?? new StarSquare[0];
            CheckStars(id, width, height, stars, starDamage);
            Rows = rows ?? throw new DataException($"{DefinitionName} '{id}': Rows is missing.");
            if (effects == null || effects.Count > MaxEffects)
            {
                throw new DataException($"{DefinitionName} '{id}': an item has 0..{MaxEffects} effects.");
            }

            // An item without effects never activates (stage 20): only a star item may be one, and it has no cooldown.
            // An item that activates when a battle starts (stage 39) has effects and no cooldown.
            if (trigger == ItemTrigger.BattleStart)
            {
                if (effects.Count == 0)
                {
                    throw new DataException($"{DefinitionName} '{id}': an item that activates when a battle starts needs an effect.");
                }

                if (cooldownMs != 0)
                {
                    throw new DataException($"{DefinitionName} '{id}': an item that activates when a battle starts has no cooldown, so its CooldownMs is 0.");
                }
            }
            else if (effects.Count == 0)
            {
                if (stars.Count == 0)
                {
                    throw new DataException($"{DefinitionName} '{id}': an item without effects must have stars.");
                }

                if (cooldownMs != 0)
                {
                    throw new DataException($"{DefinitionName} '{id}': an item without effects never activates, so its CooldownMs is 0.");
                }
            }
            else if (cooldownMs < MinCooldownMs)
            {
                throw new DataException($"{DefinitionName} '{id}': CooldownMs must be at least {MinCooldownMs}.");
            }

            if (melee && category != ItemCategory.Weapon)
            {
                throw new DataException($"{DefinitionName} '{id}': only a weapon can be a melee weapon.");
            }

            foreach (ItemEffect effect in effects)
            {
                if (effect == null)
                {
                    throw new DataException($"{DefinitionName} '{id}': an effect is missing.");
                }
            }

            if (shopWeight < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': ShopWeight cannot be negative.");
            }

            if (price < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': Price cannot be negative.");
            }

            // Only a weapon's damage can be a critical hit (stage 22).
            if (critChancePercent < 0 || critChancePercent > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': CritChancePercent must be 0..100.");
            }

            if (critChancePercent > 0 && (category != ItemCategory.Weapon || !HasDamage(effects)))
            {
                throw new DataException($"{DefinitionName} '{id}': only a weapon that deals damage can have a CritChancePercent.");
            }

            // What the weapon adds to the physical critical multiplier (2026-10-20 스탯 개정): a physical weapon with a critical chance, in percent points.
            if (critDamagePercent < 0 || critDamagePercent > MaxCritDamageBonus)
            {
                throw new DataException($"{DefinitionName} '{id}': CritDamagePercent must be 0..{MaxCritDamageBonus}.");
            }

            if (critDamagePercent > 0 && (critChancePercent == 0 || school != ItemSchool.Physical))
            {
                throw new DataException($"{DefinitionName} '{id}': only a physical weapon with a CritChancePercent can have a CritDamagePercent.");
            }

            // A unique item (stage 23) is forged at a camp (RecipeData), never bought or dropped.
            if (unique && (shopWeight != 0 || price != 0))
            {
                throw new DataException($"{DefinitionName} '{id}': a unique item has no ShopWeight and no Price.");
            }

            if (unique && dropWeight > 0)
            {
                throw new DataException($"{DefinitionName} '{id}': a unique item has no DropWeight.");
            }

            // What a helm takes off the critical chance of the weapons that hit its owner (stage 39): only defensive gear has it.
            if (critTakenPercent < 0 || critTakenPercent > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': CritTakenPercent must be 0..100.");
            }

            if (critTakenPercent > 0 && category != ItemCategory.Armor)
            {
                throw new DataException($"{DefinitionName} '{id}': only defensive gear (Armor) can have a CritTakenPercent.");
            }

            Category = category;
            Width = width;
            Height = height;
            CooldownMs = cooldownMs;
            Effects = effects;
            ShopWeight = shopWeight;
            Icon = ArtAddress.Optional(icon, $"{DefinitionName} '{id}'", nameof(Icon));
            Price = price;
            Melee = melee;
            Stars = stars;
            StarDamage = starDamage;
            CritChancePercent = critChancePercent;
            Unique = unique;
            Trigger = trigger;
            CritTakenPercent = critTakenPercent;
            CritDamagePercent = critDamagePercent;
        }

        /// <summary>The most a weapon or a trait adds to the physical critical multiplier, in percent points.</summary>
        public const int MaxCritDamageBonus = 500;

        static bool HasDamage(IReadOnlyList<ItemEffect> effects)
        {
            foreach (ItemEffect effect in effects)
            {
                if (effect.Kind == EffectKind.Damage)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Star squares lie round the shape (outside it, at most one square away, corners too) and once each; stars come with their damage.</summary>
        static void CheckStars(string id, int width, int height, IReadOnlyList<StarSquare> stars, int starDamage)
        {
            var seen = new HashSet<(int, int)>();
            foreach (StarSquare star in stars)
            {
                bool inside = star.X >= 0 && star.X < width && star.Y >= 0 && star.Y < height;
                bool near = star.X >= -1 && star.X <= width && star.Y >= -1 && star.Y <= height;
                if (inside || !near)
                {
                    throw new DataException($"{DefinitionName} '{id}': star square {star.X}:{star.Y} is not a square round its {width}x{height} shape.");
                }

                if (!seen.Add((star.X, star.Y)))
                {
                    throw new DataException($"{DefinitionName} '{id}': star square {star.X}:{star.Y} is listed twice.");
                }
            }

            if (stars.Count > 0 ? starDamage < 1 : starDamage != 0)
            {
                throw new DataException($"{DefinitionName} '{id}': StarDamage is at least 1 with stars and 0 without.");
            }
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public ItemCategory Category { get; }

        /// <summary>
        /// The squares of the item's shape across, unturned (Slice B stage 19, Docs/Design/02_Combat_System.md §4). An item lies on a
        /// board as a Width x Height rectangle, or Height x Width when turned a quarter.
        /// </summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Width { get; }

        /// <summary>The squares of the item's shape down, unturned.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Height { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int CooldownMs { get; }

        /// <summary>Where in its line the owner must stand for the item to work, counted from the front or the back.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public RowSpan Rows { get; }

        [JsonProperty(Order = 8, Required = Required.Always)]
        public IReadOnlyList<ItemEffect> Effects { get; }

        /// <summary>Relative chance to be stocked by a shop (Slice B stage 17). 0 means no shop stocks it. Battles drop what the enemies carried (stage 18), whatever this is.</summary>
        [JsonProperty(Order = 9, Required = Required.Always)]
        public int ShopWeight { get; }

        /// <summary>The logical address of the icon its cell shows. Null when it has no art yet: the cell shows the name.</summary>
        [JsonProperty(Order = 10, Required = Required.AllowNull)]
        public string Icon { get; }

        /// <summary>
        /// The address of the gold glow behind a unique item's icon (stage 23; <see cref="ArtAddress.GlowOf"/>), made from the icon by
        /// the art pipeline. Null for an item that is not unique or has no icon.
        /// </summary>
        [JsonIgnore]
        public string GlowIcon => Unique ? ArtAddress.GlowOf(Icon) : null;

        /// <summary>
        /// What a shop sells it for at Common, in region coins (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5); a tier
        /// multiplies it as it does the effects. 0 means the shop never stocks it.
        /// </summary>
        [JsonProperty(Order = 11, Required = Required.Always)]
        public int Price { get; }

        /// <summary>A melee weapon (stage 20): what a star of a whetstone strengthens. Only a weapon can be one.</summary>
        [JsonProperty(Order = 12, Required = Required.Always)]
        public bool Melee { get; }

        /// <summary>The item's star squares (stage 20), counted from the top-left square of its unturned shape; empty for an item without stars.</summary>
        [JsonProperty(Order = 13, Required = Required.Always)]
        public IReadOnlyList<StarSquare> Stars { get; }

        /// <summary>What a melee weapon on one of its stars deals more, at Common, in hundredths (a tier adds as much again per step); 0 without stars.</summary>
        [JsonProperty(Order = 14, Required = Required.Always)]
        public int StarDamage { get; }

        /// <summary>The chance in percent that the weapon's damage is a critical hit, rolled for each unit it hits (Slice B stage 22). 0 for any other item.</summary>
        [JsonProperty(Order = 15, Required = Required.Always)]
        public int CritChancePercent { get; }

        /// <summary>A unique item (Slice B stage 23): made at a camp's forge by a recipe, one per party, never in a shop or a drop.</summary>
        [JsonProperty(Order = 16, Required = Required.Always)]
        public bool Unique { get; }

        /// <summary>
        /// Relative chance to drop from a won battle as a general item, beside what the enemies carried (2026-10-17): 0 means it never
        /// drops that way (the whetstone and the base weapons come from shops; an enemy's item drops as the enemy's).
        /// </summary>
        [JsonProperty(Order = 17, Required = Required.Always)]
        public int DropWeight { get; }

        /// <summary>Physical or magic (2026-10-20 스탯 개정): a weapon's and an attack item's (physical unless the data says Magic), maybe defensive gear's; null for the rest.</summary>
        [JsonProperty(Order = 18, Required = Required.AllowNull)]
        public ItemSchool? School { get; }

        /// <summary>
        /// When the item activates (Slice D stage 39, Docs/Design/02_Combat_System.md §4 "방어구"): on its cooldown, or once when a battle starts
        /// (then it has no cooldown).
        /// </summary>
        [JsonProperty(Order = 19, Required = Required.Always)]
        public ItemTrigger Trigger { get; }

        /// <summary>
        /// Percent points taken off the critical chance of every weapon that hits the item's owner (Slice D stage 39, the iron helm), while the item
        /// works where its owner stands. The tier does not change it. 0 for an item without (only defensive gear has it).
        /// </summary>
        [JsonProperty(Order = 20, Required = Required.Always)]
        public int CritTakenPercent { get; }

        /// <summary>Percent points a physical weapon adds to the critical multiplier of its hits (2026-10-20 스탯 개정, Docs/Design/02 §4): the heavy weapons. 0 for the rest.</summary>
        [JsonProperty(Order = 21, Required = Required.Always)]
        public int CritDamagePercent { get; }

        /// <summary>
        /// The stat an owner needs to wield the item (Slice D stage 42, Docs/Design/02_Combat_System.md §4 "장비 요구 스탯"), with <see cref="ReqValue"/> the
        /// least of it; null for an item anyone wields. Asked wherever an item would go onto a mercenary's board; an enemy, which has no stats, is never asked.
        /// </summary>
        [JsonProperty(Order = 22, Required = Required.AllowNull)]
        public Stat? ReqStat { get; }

        /// <summary>The least the owner needs of <see cref="ReqStat"/>; 0 without one.</summary>
        [JsonProperty(Order = 23, Required = Required.Always)]
        public int ReqValue { get; }

        /// <summary>Whether the item asks its owner for a stat (stage 42).</summary>
        [JsonIgnore]
        public bool HasRequirement => ReqStat != null;

        /// <summary>
        /// The owner's stat that grows the item's power (2026-10-20 스탯 개정, Docs/Design/02_Combat_System.md §4; before, the data's ScalesWith column):
        /// strength for a physical weapon, intellect for a magic one (its damage and its heals alike), wisdom for an attack or a support item; null for
        /// defensive gear and the rest, which grow with none. An enemy has no stats: its items stay as they are.
        /// </summary>
        [JsonIgnore]
        public Stat? ScalesWith
        {
            get
            {
                if (Category == ItemCategory.Weapon)
                {
                    return School == ItemSchool.Magic ? Stat.Int : Stat.Str;
                }

                return Category == ItemCategory.Attack || Category == ItemCategory.Support ? Stat.Wis : (Stat?)null;
            }
        }

        /// <summary>
        /// The owner's stat that quickens the item (2026-10-20 스탯 개정): dexterity for physical equipment, wisdom for magic equipment (a weapon or
        /// defensive gear with a school); null for an item no stat quickens.
        /// </summary>
        [JsonIgnore]
        public Stat? QuickenedBy =>
            (Category == ItemCategory.Weapon || Category == ItemCategory.Armor) && School != null ? (School == ItemSchool.Magic ? Stat.Wis : Stat.Dex) : (Stat?)null;

        /// <summary>An item without effects (stage 20): it never activates and has no cooldown; it works by its stars.</summary>
        [JsonIgnore]
        public bool IsPassive => Effects.Count == 0;

        /// <summary>An item that charges and fires on its cooldown: one with effects that does not activate when a battle starts (stage 39).</summary>
        [JsonIgnore]
        public bool Cycles => Effects.Count > 0 && Trigger == ItemTrigger.Cooldown;

        /// <summary>The squares the item takes: on a board, and in the inventory, which counts squares (Slice B stage 19).</summary>
        [JsonIgnore]
        public int Area => Width * Height;

        /// <param name="row">The row the owner stands in.</param>
        /// <param name="lineLength">How many units of the owner's side are alive.</param>
        public bool UsableIn(int row, int lineLength)
        {
            return Rows.Contains(row, lineLength);
        }
    }
}
