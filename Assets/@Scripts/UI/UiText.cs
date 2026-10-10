using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Text that screens build from game data: names in the current locale and descriptions of
    /// items and potions. For display only.
    /// </summary>
    public static class UiText
    {
        /// <summary>A game data name in the current locale.</summary>
        public static string Name(LocalizedText text)
        {
            return text.Resolve(Managers.Setting.LocaleCode);
        }

        public static string Mercenary(string mercenaryId)
        {
            return Name(Managers.Data.Data.Mercenaries.Get(mercenaryId).Name);
        }

        public static string Job(string jobId)
        {
            return Name(Managers.Data.Data.Jobs.Get(jobId).Name);
        }

        /// <summary>What the job's passive does, or an empty string when it has none.</summary>
        public static string Passive(JobData job)
        {
            return job.Passive == null
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, Name(job.PassiveText), job.Passive.Magnitude);
        }

        /// <summary>Names of mercenaries separated by commas, or "None" for an empty list.</summary>
        public static string MercenaryList(IReadOnlyList<string> mercenaryIds)
        {
            if (mercenaryIds.Count == 0)
            {
                return UiStrings.Get(UiKeys.Common.None);
            }

            var names = new List<string>();
            foreach (string id in mercenaryIds)
            {
                names.Add(Mercenary(id));
            }

            return string.Join(", ", names);
        }

        /// <summary>The key of a row's name ("Row 1"). Prefab builders use it for fixed labels.</summary>
        public static string RowKey(int row)
        {
            switch (row)
            {
                case 1: return UiKeys.Common.Row1;
                case 2: return UiKeys.Common.Row2;
                case 3: return UiKeys.Common.Row3;
                case 4: return UiKeys.Common.Row4;
                default: throw new ArgumentOutOfRangeException(nameof(row), row, "No text for this row.");
            }
        }

        /// <summary>Milliseconds as seconds with one decimal, for example "2.9".</summary>
        public static string Seconds(int milliseconds)
        {
            int tenths = milliseconds / 100;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", tenths / 10, tenths % 10);
        }

        /// <summary>"Name · tier · grade", the tier in its colour (round 35); a Common item leaves the tier out: "Name · grade" (round 41).</summary>
        public static string ItemTitle(EquippedItem item)
        {
            return item.Tier == ItemTier.Common
                ? UiStrings.Get(UiKeys.Item.TitlePlain, Name(item.Item.Name), item.Grade)
                : UiStrings.Get(UiKeys.Item.Title, Name(item.Item.Name), TierWord(item.Tier), item.Grade);
        }

        /// <summary>
        /// The title in Diablo's tooltip (round 57, the user: "인벤 창에 등급 점수는 삭제"): the name and, above Common, the tier word — no
        /// grade. The inventory window's box and the merchant's tooltip show it; cards and other lines keep <see cref="ItemTitle"/>.
        /// </summary>
        public static string TooltipTitle(EquippedItem item)
        {
            return item.Tier == ItemTier.Common
                ? Name(item.Item.Name)
                : UiStrings.Get(UiKeys.Item.TooltipTitle, Name(item.Item.Name), TierWord(item.Tier));
        }

        /// <summary>The name of a tier (Docs/Design/02_Combat_System.md §4).</summary>
        public static string TierName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Common: return UiStrings.Get(UiKeys.Item.Common);
                case ItemTier.Bronze: return UiStrings.Get(UiKeys.Item.Bronze);
                case ItemTier.Silver: return UiStrings.Get(UiKeys.Item.Silver);
                case ItemTier.Gold: return UiStrings.Get(UiKeys.Item.Gold);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        /// <summary>The name of a tier in the tier's colour.</summary>
        public static string TierWord(ItemTier tier)
        {
            return Colored(TierName(tier), UiPalette.TierText(tier));
        }

        /// <summary>Under a chosen item's facts: what putting it on the same item at the same tier does (round 35). A Common item names no tier of its own.</summary>
        public static string MergeHint(EquippedItem item)
        {
            return item.Tier == ItemTier.Common
                ? UiStrings.Get(UiKeys.Item.MergeHintPlain, Name(item.Item.Name), TierWord(item.Tier + 1))
                : UiStrings.Get(UiKeys.Item.MergeHint, Name(item.Item.Name), TierWord(item.Tier), TierWord(item.Tier + 1));
        }

        /// <summary>The name of a state of the breakdown: an affliction or a virtue (Docs/Design/04_Lobby_100Day_Economy.md §3).</summary>
        public static string FatigueStateName(FatigueStateData state)
        {
            return Name(state.Name);
        }

        /// <summary>The name of a state in its colour: red for an affliction, gold for a virtue (round 36).</summary>
        public static string FatigueStateWord(FatigueStateData state)
        {
            return Colored(Name(state.Name), UiPalette.FatigueState(state.Kind));
        }

        /// <summary>"공포: 아이템이 25% 느리게 돈다": the state and what it does, for the party side's detail line.</summary>
        public static string FatigueStateDetail(FatigueStateData state)
        {
            return UiStrings.Get(UiKeys.Board.StateDetail, FatigueStateWord(state), Name(state.Description));
        }

        /// <summary>The job on the state line under a party member's feet, with the member's state after it while it is in one: "마검사 · 공포".</summary>
        public static string JobLine(string jobId, FatigueStateData state)
        {
            return state == null ? Job(jobId) : UiStrings.Get(UiKeys.Board.JobState, Job(jobId), FatigueStateWord(state));
        }

        /// <summary>A value before and after a change: "11 → 22".</summary>
        public static string Change(string before, string after)
        {
            return UiStrings.Get(UiKeys.Item.Change, before, after);
        }

        const string FactSeparator = " / ";

        /// <summary>
        /// An item's card (round 42): the facts on one line, the effects one per line, and the fatigue line, which is null when
        /// the item neither costs fatigue nor is a base weapon.
        /// </summary>
        /// <param name="starDamage">What the stars on the item's board add to a weapon's damage (stage 20): its damage line counts it.</param>
        public static void ItemCard(EquippedItem item, out string facts, out string effects, out string fatigue, int starDamage = 0)
        {
            facts = string.Join(FactSeparator, ItemFacts(item));
            effects = string.Join("\n", ItemEffects(item, starDamage));
            var parts = new List<string>();
            AddFatigue(parts, item);
            fatigue = parts.Count == 0 ? null : parts[0];
        }

        /// <summary>
        /// An item's lines in Diablo II's tooltip (round 49, the inventory popup's box): the title in its rarity colour, each fact on a
        /// line, the effects in the magic blue, the fatigue last in its own colour. Whoever shows them centres them.
        /// </summary>
        public static string TooltipLines(EquippedItem item)
        {
            var lines = new List<string> { Colored(TooltipTitle(item), UiPalette.Rarity(item.Tier)) };
            lines.AddRange(ItemFacts(item));
            foreach (string effect in ItemEffects(item))
            {
                lines.Add(Colored(effect, UiPalette.TooltipEffect));
            }

            AddFatigue(lines, item);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// The facts on a tile of the shop or the loot (round 46), under its name and its kind with its cells: the cooldown and where
        /// it works, dimmed, a line each; the effects, a line each; the fatigue last.
        /// </summary>
        public static string ItemTileFacts(EquippedItem item)
        {
            ItemData data = item.Item;
            var lines = new List<string> { Colored(CooldownFact(data), UiPalette.TextDim) };
            if (!data.Rows.IsEveryRow)
            {
                lines.Add(Colored(Rows(data.Rows), UiPalette.TextDim));
            }

            lines.AddRange(ItemEffects(item));
            AddFatigue(lines, item);
            return string.Join("\n", lines);
        }

        /// <summary>An item's category, size, cooldown, where it works and its effects on one line, the fatigue last.</summary>
        public static string ItemSummary(EquippedItem item)
        {
            List<string> parts = ItemFacts(item);
            parts.AddRange(ItemEffects(item));
            AddFatigue(parts, item);
            return string.Join(FactSeparator, parts);
        }

        /// <summary>
        /// What the item costs in fatigue when a battle starts (round 32): in the fatigue's violet for equipment that
        /// costs, "no fatigue" dimmed for a base weapon, nothing for the rest (Docs/Design/04_Lobby_100Day_Economy.md §3).
        /// </summary>
        static void AddFatigue(List<string> parts, EquippedItem item)
        {
            int cost = FatigueRules.ItemCost(Managers.Data.Data.Balance, item);
            if (cost > 0)
            {
                parts.Add(Colored(UiStrings.Get(UiKeys.Item.FatigueCost, cost), UiPalette.Fatigue));
            }
            else if (item.IsBase)
            {
                parts.Add(Colored(UiStrings.Get(UiKeys.Item.BaseWeapon), UiPalette.TextDim));
            }
        }

        public static string Colored(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        /// <summary>The words of an item's category (Docs/Design/02_Combat_System.md §4).</summary>
        public static string CategoryKey(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return UiKeys.Item.Weapon;
                case ItemCategory.Armor: return UiKeys.Item.Armor;
                case ItemCategory.Attack: return UiKeys.Item.Attack;
                case ItemCategory.Support: return UiKeys.Item.Support;
                case ItemCategory.Other: return UiKeys.Item.Other;
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        /// <summary>A bag's line (Slice B stage 19): its name, its squares, and what it adds.</summary>
        public static string BagDetail(BagData bag)
        {
            return Name(bag.Name) + " — " + UiStrings.Get(UiKeys.Map.ShopBagSub, bag.Width, bag.Height) + " / " + UiStrings.Get(UiKeys.Map.BagFacts, bag.Area);
        }

        static List<string> ItemFacts(EquippedItem item)
        {
            ItemData data = item.Item;
            var facts = new List<string>
            {
                UiStrings.Get(CategoryKey(data.Category)),
                UiStrings.Get(UiKeys.Item.Size, data.Width, data.Height),
            };

            // A melee weapon says so: the stars of a whetstone strengthen only those (stage 20).
            if (data.Melee)
            {
                facts.Add(UiStrings.Get(UiKeys.Item.Melee));
            }

            facts.Add(CooldownFact(data));

            // An item that works anywhere in the line needs no fact about where.
            if (!data.Rows.IsEveryRow)
            {
                facts.Add(Rows(data.Rows));
            }

            return facts;
        }

        /// <summary>The cooldown, or "never activates" for an item without effects (stage 20).</summary>
        static string CooldownFact(ItemData data)
        {
            return data.IsPassive ? UiStrings.Get(UiKeys.Item.NoActivation) : UiStrings.Get(UiKeys.Item.Cooldown, Seconds(data.CooldownMs));
        }

        /// <param name="starDamage">What stars add to the item's weapon damage on its board (stage 20, as the battle adds it), or 0.</param>
        static List<string> ItemEffects(EquippedItem item, int starDamage = 0)
        {
            var effects = new List<string>();
            BalanceData balance = Managers.Data.Data.Balance;
            foreach (ItemEffect effect in item.Item.Effects)
            {
                bool weaponDamage = effect.Kind == EffectKind.Damage && item.Item.Category == ItemCategory.Weapon;
                effects.Add(Effect(effect, item.Magnitude(balance, effect) + (weaponDamage ? starDamage : 0)));
            }

            // A star item's work (stage 20): what a melee weapon on one of its stars deals more, at its tier.
            if (item.Item.Stars.Count > 0)
            {
                effects.Add(UiStrings.Get(UiKeys.Item.StarDamage, item.StarDamage));
            }

            return effects;
        }

        /// <summary>
        /// What the stars on a board say of an item there (Slice B stage 20; Docs/Architecture/12_UI.md "격자 보드"), for its card: on a melee
        /// weapon, the star items that strengthen it and by how much in gold ("★ 숫돌 +1"); on a star item, the stars a melee weapon lies on
        /// and which ("걸린 ★ 1/2 — 롱소드"). Null for anything else, and for a weapon no star reaches.
        /// </summary>
        public static string StarLine(ItemBoard board, BoardItem placed)
        {
            if (board == null || placed == null)
            {
                return null;
            }

            if (placed.Item.Item.Stars.Count > 0)
            {
                List<BoardItem> lit = StarRules.Lit(board, placed);
                int stars = placed.Item.Item.Stars.Count;
                return lit.Count == 0
                    ? UiStrings.Get(UiKeys.Item.StarLitNone, stars)
                    : UiStrings.Get(UiKeys.Item.StarLit, lit.Count, stars, string.Join(", ", lit.ConvertAll(weapon => Name(weapon.Item.Item.Name))));
            }

            List<BoardItem> sources = StarRules.Sources(board, placed);
            if (sources.Count == 0)
            {
                return null;
            }

            var names = new List<string>();
            foreach (BoardItem source in sources)
            {
                string name = Name(source.Item.Item.Name);
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }

            return Colored(UiStrings.Get(UiKeys.Item.StarBonus, string.Join("·", names), StarRules.DamageOn(board, placed)), UiPalette.StarLit);
        }

        /// <summary>Where in its line the owner must stand: "only in the front row", "only within the rear 3 rows".</summary>
        public static string Rows(RowSpan rows)
        {
            if (rows.From == RowEnd.Front)
            {
                return rows.Reach == 1 ? UiStrings.Get(UiKeys.Item.RowsFrontOne) : UiStrings.Get(UiKeys.Item.RowsFront, rows.Reach);
            }

            return rows.Reach == 1 ? UiStrings.Get(UiKeys.Item.RowsBackOne) : UiStrings.Get(UiKeys.Item.RowsBack, rows.Reach);
        }

        /// <param name="magnitude">The effect's size on the item it is of (<see cref="EquippedItem.Magnitude"/>: its grade and tier).</param>
        public static string Effect(ItemEffect effect, int magnitude)
        {
            return UiStrings.Get(EffectKey(effect.Kind), Target(effect), magnitude);
        }

        public static string PotionDetails(PotionData potion)
        {
            switch (potion.Effect)
            {
                case PotionEffect.Heal: return UiStrings.Get(UiKeys.Potion.Heal, potion.Magnitude);
                case PotionEffect.Shield: return UiStrings.Get(UiKeys.Potion.Shield, potion.Magnitude);
                default: throw new ArgumentOutOfRangeException(nameof(potion), potion.Effect, "No text for this potion effect.");
            }
        }

        static string EffectKey(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Damage: return UiKeys.Effect.Damage;
                case EffectKind.Heal: return UiKeys.Effect.Heal;
                case EffectKind.Shield: return UiKeys.Effect.Shield;
                case EffectKind.Burn: return UiKeys.Effect.Burn;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "No text for this effect.");
            }
        }

        /// <summary>Who an effect hits: "the front enemy", "the rear 2 enemies", "self" and so on.</summary>
        static string Target(ItemEffect effect)
        {
            switch (effect.Target)
            {
                case TargetMode.EnemyFront:
                    return effect.Reach == 1 ? UiStrings.Get(UiKeys.Target.EnemyFront) : UiStrings.Get(UiKeys.Target.EnemyFrontMany, effect.Reach);
                case TargetMode.EnemyBack:
                    return effect.Reach == 1 ? UiStrings.Get(UiKeys.Target.EnemyBack) : UiStrings.Get(UiKeys.Target.EnemyBackMany, effect.Reach);
                case TargetMode.EnemyAll: return UiStrings.Get(UiKeys.Target.EnemyAll);
                case TargetMode.Self: return UiStrings.Get(UiKeys.Target.Self);
                case TargetMode.AllyLowestHp: return UiStrings.Get(UiKeys.Target.AllyLowestHp);
                case TargetMode.AllyAll: return UiStrings.Get(UiKeys.Target.AllyAll);
                default: throw new ArgumentOutOfRangeException(nameof(effect), effect.Target, "No text for this target.");
            }
        }
    }
}
