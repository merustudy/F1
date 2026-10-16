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
                throw new DataException($"Effect {kind} must be at least 1 (0.01 for damage, a heal or a shield; one stack for a burn).");
            }

            if (kind == EffectKind.Damage ? max < min : max != min)
            {
                throw new DataException(kind == EffectKind.Damage
                    ? "Damage needs its least no larger than its most."
                    : $"Effect {kind} has one value, not a range.");
            }

            bool harmful = kind == EffectKind.Damage || kind == EffectKind.Burn;
            if (TargetsEnemy(target) != harmful)
            {
                throw new DataException($"Effect {kind} cannot target {target}.");
            }

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
    /// A battle card. Items exist only inside a dungeon and activate on their own cooldown (an item without effects never does: stage 20).
    /// Items are not relics: relics are a different definition, file and type.
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
            bool unique = false)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
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
            if (effects.Count == 0)
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

            // A unique item (stage 23) is forged at a camp (RecipeData), never bought or dropped.
            if (unique && (shopWeight != 0 || price != 0))
            {
                throw new DataException($"{DefinitionName} '{id}': a unique item has no ShopWeight and no Price.");
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
        }

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

        /// <summary>An item without effects (stage 20): it never activates and has no cooldown; it works by its stars.</summary>
        [JsonIgnore]
        public bool IsPassive => Effects.Count == 0;

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
