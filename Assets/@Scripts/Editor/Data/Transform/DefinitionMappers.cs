using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;

namespace F1.Editor.Data
{
    /// <summary>Shared row loop: builds one definition per row and records errors with file and row.</summary>
    internal static class RowMapping
    {
        public const string Id = "Id";
        public const string Name = "Name";

        public static List<T> MapRows<T>(CsvTable table, List<string> errors, Func<CsvRow, T> map)
        {
            var items = new List<T>();
            foreach (CsvRow row in table.Rows)
            {
                try
                {
                    items.Add(map(row));
                }
                catch (DataException exception)
                {
                    errors.Add(row.Contextualize(exception.Message));
                }
            }

            return items;
        }

        /// <summary>"Id", the localized "Name.*" headers, then the definition's own headers.</summary>
        public static IEnumerable<string> Headers(params string[] own)
        {
            return new[] { Id }.Concat(CsvRow.LocalizedHeaders(Name)).Concat(own);
        }
    }

    internal static class BalanceMapper
    {
        public static List<BalanceEntry> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { "Key", "Value" });
            return RowMapping.MapRows(table, errors, row => new BalanceEntry(row.Key("Key"), row.Int("Value")));
        }
    }

    internal static class JobMapper
    {
        public static List<JobData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(
                "MaxHp", "ItemSlots", "WeaponItemId", "WeaponGrade", "RecommendedRow",
                "PassiveTrigger", "PassiveCondition", "PassiveEffect", "PassiveTarget", "PassiveMagnitude"));

            return RowMapping.MapRows(table, errors, row => new JobData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("MaxHp"),
                row.Int("ItemSlots"),
                row.Id("WeaponItemId"),
                row.Int("WeaponGrade"),
                row.Enum<BattleRow>("RecommendedRow"),
                ReadPassive(row)));
        }

        /// <summary>A job without a passive leaves every Passive* cell empty.</summary>
        static PassiveSpec ReadPassive(CsvRow row)
        {
            string[] headers = { "PassiveTrigger", "PassiveCondition", "PassiveEffect", "PassiveTarget", "PassiveMagnitude" };
            if (headers.All(row.IsEmpty))
            {
                return null;
            }

            return new PassiveSpec(
                row.Enum<PassiveTrigger>("PassiveTrigger"),
                row.Enum<PassiveCondition>("PassiveCondition"),
                row.Enum<PassiveEffect>("PassiveEffect"),
                row.Enum<PassiveTarget>("PassiveTarget"),
                row.Int("PassiveMagnitude"));
        }
    }

    internal static class ItemMapper
    {
        public static List<ItemData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(
                "Category", "CooldownMs", "Row",
                "Effect1Kind", "Effect1Target", "Effect1Power",
                "Effect2Kind", "Effect2Target", "Effect2Power",
                "RewardWeight"));

            return RowMapping.MapRows(table, errors, row =>
            {
                var effects = new List<ItemEffect> { ReadEffect(row, "Effect1") };
                string[] second = { "Effect2Kind", "Effect2Target", "Effect2Power" };
                if (!second.All(row.IsEmpty))
                {
                    effects.Add(ReadEffect(row, "Effect2"));
                }

                return new ItemData(
                    row.Id(RowMapping.Id),
                    row.Localized(RowMapping.Name),
                    row.Enum<ItemCategory>("Category"),
                    row.Int("CooldownMs"),
                    row.Enum<RowRequirement>("Row"),
                    effects,
                    row.Int("RewardWeight"));
            });
        }

        static ItemEffect ReadEffect(CsvRow row, string prefix)
        {
            return new ItemEffect(
                row.Enum<EffectKind>(prefix + "Kind"),
                row.Enum<TargetMode>(prefix + "Target"),
                row.Int(prefix + "Power"));
        }
    }

    internal static class PotionMapper
    {
        public static List<PotionData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Effect", "Magnitude", "RewardWeight"));
            return RowMapping.MapRows(table, errors, row => new PotionData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Enum<PotionEffect>("Effect"),
                row.Int("Magnitude"),
                row.Int("RewardWeight")));
        }
    }

    internal static class EnemyMapper
    {
        public static List<EnemyData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Level", "MaxHp", "Items"));
            return RowMapping.MapRows(table, errors, row => new EnemyData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("Level"),
                row.Int("MaxHp"),
                row.GrantList("Items")));
        }
    }

    internal static class EnemyGroupMapper
    {
        public static List<EnemyGroupData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { RowMapping.Id, "DungeonId", "MinFloor", "MaxFloor", "IsBoss", "Front", "Rear" });
            return RowMapping.MapRows(table, errors, row => new EnemyGroupData(
                row.Id(RowMapping.Id),
                row.Id("DungeonId"),
                row.Int("MinFloor"),
                row.Int("MaxFloor"),
                row.Bool("IsBoss"),
                row.IdList("Front"),
                row.IdList("Rear")));
        }
    }

    internal static class AffinityMapper
    {
        public static List<AffinityData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("EnemyCooldownPermille"));
            return RowMapping.MapRows(table, errors, row => new AffinityData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("EnemyCooldownPermille")));
        }
    }

    internal static class DungeonMapper
    {
        public static List<DungeonData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(
                "AffinityId", "Floors", "MapMinWidth", "MapMaxWidth", "FatigueCost", "DurationDays",
                "ItemGradeBase", "ItemGradePerFloor", "StartingPotions"));

            return RowMapping.MapRows(table, errors, row => new DungeonData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Id("AffinityId"),
                row.Int("Floors"),
                row.Int("MapMinWidth"),
                row.Int("MapMaxWidth"),
                row.Int("FatigueCost"),
                row.Int("DurationDays"),
                row.Int("ItemGradeBase"),
                row.Int("ItemGradePerFloor"),
                row.IdList("StartingPotions")));
        }
    }

    internal static class MercenaryMapper
    {
        public static List<MercenaryData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("JobId"));
            return RowMapping.MapRows(table, errors, row => new MercenaryData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Id("JobId")));
        }
    }
}
