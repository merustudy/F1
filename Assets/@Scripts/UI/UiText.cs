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

        public static string Row(BattleRow row)
        {
            return UiStrings.Get(row == BattleRow.Front ? UiKeys.Common.Front : UiKeys.Common.Rear);
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

        /// <summary>Category, cooldown, row requirement and each effect, one per line.</summary>
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

            if (data.Row == RowRequirement.Front)
            {
                lines.Add(UiStrings.Get(UiKeys.Item.RowFront));
            }
            else if (data.Row == RowRequirement.Rear)
            {
                lines.Add(UiStrings.Get(UiKeys.Item.RowRear));
            }

            foreach (ItemEffect effect in data.Effects)
            {
                lines.Add(Effect(effect, item.Grade));
            }

            return lines;
        }

        public static string Effect(ItemEffect effect, int grade)
        {
            return UiStrings.Get(EffectKey(effect.Kind), UiStrings.Get(TargetKey(effect.Target)), effect.MagnitudeAt(grade));
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

        static string TargetKey(TargetMode mode)
        {
            switch (mode)
            {
                case TargetMode.EnemyFront: return UiKeys.Target.EnemyFront;
                case TargetMode.EnemyRear: return UiKeys.Target.EnemyRear;
                case TargetMode.EnemyAll: return UiKeys.Target.EnemyAll;
                case TargetMode.Self: return UiKeys.Target.Self;
                case TargetMode.AllyLowestHp: return UiKeys.Target.AllyLowestHp;
                case TargetMode.AllyAll: return UiKeys.Target.AllyAll;
                default: throw new ArgumentOutOfRangeException(nameof(mode), mode, "No text for this target.");
            }
        }
    }
}
