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

        /// <summary>"Name · tier · grade", the tier in its colour (round 35).</summary>
        public static string ItemTitle(EquippedItem item)
        {
            return UiStrings.Get(UiKeys.Item.Title, Name(item.Item.Name), TierWord(item.Tier), item.Grade);
        }

        /// <summary>The name of a tier (Docs/Design/02_Combat_System.md §4).</summary>
        public static string TierName(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return UiStrings.Get(UiKeys.Item.Bronze);
                case ItemTier.Silver: return UiStrings.Get(UiKeys.Item.Silver);
                case ItemTier.Gold: return UiStrings.Get(UiKeys.Item.Gold);
                case ItemTier.Diamond: return UiStrings.Get(UiKeys.Item.Diamond);
                default: throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        /// <summary>The name of a tier in the tier's colour.</summary>
        public static string TierWord(ItemTier tier)
        {
            return Colored(TierName(tier), UiPalette.TierText(tier));
        }

        /// <summary>Under a chosen item's facts: what putting it on the same item at the same tier does (round 35).</summary>
        public static string MergeHint(EquippedItem item)
        {
            return UiStrings.Get(UiKeys.Item.MergeHint, Name(item.Item.Name), TierWord(item.Tier), TierWord(item.Tier + 1));
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

        /// <summary>Category, size, cooldown, where it works and its fatigue on one line, then each effect on its own line.</summary>
        public static string ItemDetails(EquippedItem item)
        {
            List<string> facts = ItemFacts(item);
            AddFatigue(facts, item);
            var lines = new List<string> { string.Join(FactSeparator, facts) };
            lines.AddRange(ItemEffects(item));
            return string.Join("\n", lines);
        }

        /// <summary>The same facts and effects as <see cref="ItemDetails"/> on one line, the fatigue last.</summary>
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

        static string Colored(string text, Color color)
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

        static List<string> ItemFacts(EquippedItem item)
        {
            ItemData data = item.Item;
            var facts = new List<string>
            {
                UiStrings.Get(CategoryKey(data.Category)),
                UiStrings.Get(UiKeys.Item.Size, data.Size),
                UiStrings.Get(UiKeys.Item.Cooldown, Seconds(data.CooldownMs)),
            };

            // An item that works anywhere in the line needs no fact about where.
            if (!data.Rows.IsEveryRow)
            {
                facts.Add(Rows(data.Rows));
            }

            return facts;
        }

        static List<string> ItemEffects(EquippedItem item)
        {
            var effects = new List<string>();
            BalanceData balance = Managers.Data.Data.Balance;
            foreach (ItemEffect effect in item.Item.Effects)
            {
                effects.Add(Effect(effect, item.Magnitude(balance, effect)));
            }

            return effects;
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
