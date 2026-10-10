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
        public const string Figure = "Figure";
        public const string Background = "Background";
        public const string Icon = "Icon";

        /// <summary>The address of a piece of art (a unit's Figure, a dungeon's Background, an item's Icon). An empty cell means there is no art.</summary>
        public static string ReadArt(CsvRow row, string header)
        {
            return row.IsEmpty(header) ? null : row.Text(header);
        }

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
                "MaxHp", "WeaponItemId", "WeaponGrade", "RecommendedRow",
                "PassiveTrigger", "PassiveCondition", "PassiveRows", "PassiveEffect", "PassiveTarget", "PassiveMagnitude",
                RowMapping.Figure)
                .Concat(CsvRow.LocalizedHeaders(PassiveText)));

            return RowMapping.MapRows(table, errors, row => new JobData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("MaxHp"),
                row.Id("WeaponItemId"),
                row.Int("WeaponGrade"),
                row.Int("RecommendedRow"),
                ReadPassive(row),
                ReadPassiveText(row),
                RowMapping.ReadArt(row, RowMapping.Figure)));
        }

        const string PassiveText = "PassiveText";

        /// <summary>Empty in every locale when the job has no passive; otherwise required in every locale.</summary>
        static LocalizedText ReadPassiveText(CsvRow row)
        {
            return CsvRow.LocalizedHeaders(PassiveText).All(row.IsEmpty) ? null : row.Localized(PassiveText);
        }

        /// <summary>A job without a passive leaves every Passive* cell empty.</summary>
        static PassiveSpec ReadPassive(CsvRow row)
        {
            string[] headers = { "PassiveTrigger", "PassiveCondition", "PassiveRows", "PassiveEffect", "PassiveTarget", "PassiveMagnitude" };
            if (headers.All(row.IsEmpty))
            {
                return null;
            }

            // PassiveRows is filled only for the InRows condition; PassiveSpec checks that.
            return new PassiveSpec(
                row.Enum<PassiveTrigger>("PassiveTrigger"),
                row.Enum<PassiveCondition>("PassiveCondition"),
                row.OptionalRows("PassiveRows"),
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
                "Category", "Width", "Height", "CooldownMs", "Rows",
                "Effect1Kind", "Effect1Target", "Effect1Reach", "Effect1Power",
                "Effect2Kind", "Effect2Target", "Effect2Reach", "Effect2Power",
                "ShopWeight", RowMapping.Icon, "Price", "Melee", "Stars", "StarDamage"));

            return RowMapping.MapRows(table, errors, row =>
            {
                // An item without effects (all of Effect1 empty) never activates: a star item (stage 20). ItemData checks the rest.
                var effects = new List<ItemEffect>();
                foreach (string prefix in new[] { "Effect1", "Effect2" })
                {
                    string[] columns = { prefix + "Kind", prefix + "Target", prefix + "Reach", prefix + "Power" };
                    if (!columns.All(row.IsEmpty))
                    {
                        effects.Add(ReadEffect(row, prefix));
                    }
                }

                return new ItemData(
                    row.Id(RowMapping.Id),
                    row.Localized(RowMapping.Name),
                    row.Enum<ItemCategory>("Category"),
                    row.Int("Width"),
                    row.Int("Height"),
                    row.Int("CooldownMs"),
                    row.Rows("Rows"),
                    effects,
                    row.Int("ShopWeight"),
                    RowMapping.ReadArt(row, RowMapping.Icon),
                    row.Int("Price"),
                    row.Bool("Melee"),
                    row.StarSquares("Stars"),
                    row.OptionalInt("StarDamage", 0));
            });
        }

        /// <summary>Reach is filled only for targets counted from an end of the enemy line; ItemEffect checks that.</summary>
        static ItemEffect ReadEffect(CsvRow row, string prefix)
        {
            return new ItemEffect(
                row.Enum<EffectKind>(prefix + "Kind"),
                row.Enum<TargetMode>(prefix + "Target"),
                row.OptionalInt(prefix + "Reach", 0),
                row.Int(prefix + "Power"));
        }
    }

    internal static class BagMapper
    {
        public static List<BagData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Width", "Height", "Start", "Price", "ShopWeight", "LootWeight"));
            return RowMapping.MapRows(table, errors, row => new BagData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("Width"),
                row.Int("Height"),
                row.Bool("Start"),
                row.Int("Price"),
                row.Int("ShopWeight"),
                row.Int("LootWeight")));
        }
    }

    internal static class PotionMapper
    {
        public static List<PotionData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Effect", "Magnitude", "ShopWeight", RowMapping.Icon, "Price"));
            return RowMapping.MapRows(table, errors, row => new PotionData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Enum<PotionEffect>("Effect"),
                row.Int("Magnitude"),
                row.Int("ShopWeight"),
                RowMapping.ReadArt(row, RowMapping.Icon),
                row.Int("Price")));
        }
    }

    internal static class EnemyMapper
    {
        public static List<EnemyData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Level", "MaxHp", "Items", RowMapping.Figure, "FigureScale"));
            return RowMapping.MapRows(table, errors, row => new EnemyData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("Level"),
                row.Int("MaxHp"),
                row.GrantList("Items"),
                RowMapping.ReadArt(row, RowMapping.Figure),
                row.Int("FigureScale")));
        }
    }

    internal static class EnemyGroupMapper
    {
        public static List<EnemyGroupData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { RowMapping.Id, "DungeonId", "MinFloor", "MaxFloor", "IsBoss", "IsElite", "Enemies" });
            return RowMapping.MapRows(table, errors, row => new EnemyGroupData(
                row.Id(RowMapping.Id),
                row.Id("DungeonId"),
                row.Int("MinFloor"),
                row.Int("MaxFloor"),
                row.Bool("IsBoss"),
                row.IdList("Enemies"),
                row.Bool("IsElite")));
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
                "AffinityId", "Floors", "MapMinWidth", "MapMaxWidth", "DurationDays",
                "ItemGradeBase", "ItemGradePerFloor", "StartingPotions", RowMapping.Background,
                "EliteMinFloor", "EliteChancePercent", "CampMinFloor", "CampChancePercent", "CampFloor",
                "EnemyHpPerFloorPercent", "EnemyGradePerFloor", "BronzeFloor", "SilverFloor", "GoldFloor", "ShopMinFloor", "ShopChancePercent"));

            return RowMapping.MapRows(table, errors, row => new DungeonData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Id("AffinityId"),
                row.Int("Floors"),
                row.Int("MapMinWidth"),
                row.Int("MapMaxWidth"),
                row.Int("DurationDays"),
                row.Int("ItemGradeBase"),
                row.Int("ItemGradePerFloor"),
                row.IdList("StartingPotions"),
                RowMapping.ReadArt(row, RowMapping.Background),
                row.Int("EliteMinFloor"),
                row.Int("EliteChancePercent"),
                row.Int("CampMinFloor"),
                row.Int("CampChancePercent"),
                row.Int("CampFloor"),
                row.Int("EnemyHpPerFloorPercent"),
                row.Int("EnemyGradePerFloor"),
                row.Int("BronzeFloor"),
                row.Int("SilverFloor"),
                row.Int("GoldFloor"),
                row.Int("ShopMinFloor"),
                row.Int("ShopChancePercent")));
        }
    }

    internal static class FatigueStateMapper
    {
        public static List<FatigueStateData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Kind", "CooldownPercent", "HealTakenPercent", "DeathChanceDelta", "Description.ko-KR", "Description.en-US"));
            return RowMapping.MapRows(table, errors, row => new FatigueStateData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Enum<FatigueStateKind>("Kind"),
                row.Int("CooldownPercent"),
                row.Int("HealTakenPercent"),
                row.Int("DeathChanceDelta"),
                row.Localized("Description")));
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
