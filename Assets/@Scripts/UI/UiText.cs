using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Data;
using F1.Gameplay;

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

        public static string Row(int row)
        {
            return UiStrings.Get(RowKey(row));
        }

        /// <summary>Milliseconds as seconds with one decimal, for example "2.9".</summary>
        public static string Seconds(int milliseconds)
        {
            int tenths = milliseconds / 100;
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", tenths / 10, tenths % 10);
        }

        public static string ItemTitle(EquippedItem item)
        {
            return UiStrings.Get(UiKeys.Item.Title, Name(item.Item.Name), item.Grade);
        }

        /// <summary>Category, cooldown, the rows it works in and each effect, one per line.</summary>
        public static string ItemDetails(EquippedItem item)
        {
            return string.Join("\n", ItemFacts(item));
        }

        /// <summary>The same facts as <see cref="ItemDetails"/> on one line.</summary>
        public static string ItemSummary(EquippedItem item)
        {
            return string.Join(" / ", ItemFacts(item));
        }

        static List<string> ItemFacts(EquippedItem item)
        {
            ItemData data = item.Item;
            var lines = new List<string>
            {
                UiStrings.Get(data.Category == ItemCategory.Weapon ? UiKeys.Item.Weapon : UiKeys.Item.Support),
                UiStrings.Get(UiKeys.Item.Cooldown, Seconds(data.CooldownMs)),
            };

            // An item that works in every row needs no line about rows.
            if (data.Rows.Count < BattleRows.Count)
            {
                var rows = new List<string>();
                foreach (int row in data.Rows)
                {
                    rows.Add(Row(row));
                }

                lines.Add(UiStrings.Get(UiKeys.Item.Rows, string.Join(", ", rows)));
            }

            foreach (ItemEffect effect in data.Effects)
            {
                lines.Add(Effect(effect, item.Grade));
            }

            return lines;
        }

        public static string Effect(ItemEffect effect, int grade)
        {
            return UiStrings.Get(EffectKey(effect.Kind), Target(effect), effect.MagnitudeAt(grade));
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
