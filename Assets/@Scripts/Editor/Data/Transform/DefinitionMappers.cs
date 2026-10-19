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
            // A value in hundredths is written with up to two decimals (stage 22): "0.55" is read as 55.
            return RowMapping.MapRows(table, errors, row =>
            {
                string key = row.Key("Key");
                return new BalanceEntry(key, BalanceData.IsCenti(key) ? row.Centi("Value") : row.Int("Value"));
            });
        }
    }

    internal static class JobMapper
    {
        public static List<JobData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(
                "MaxHp", "WeaponItemId", "RecommendedRow",
                "PassiveTrigger", "PassiveCondition", "PassiveRows", "PassiveEffect", "PassiveTarget", "PassiveMagnitude",
                RowMapping.Figure, "Str", "Dex", "Con", "Int", "Spi", "End",
                "GrowthStr", "GrowthDex", "GrowthCon", "GrowthInt", "GrowthSpi", "GrowthEnd")
                .Concat(CsvRow.LocalizedHeaders(PassiveText))
                .Concat(CsvRow.LocalizedHeaders(PassiveName)));

            return RowMapping.MapRows(table, errors, row => new JobData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Centi("MaxHp"),
                row.Id("WeaponItemId"),
                row.Int("RecommendedRow"),
                ReadPassive(row),
                ReadPassiveText(row),
                RowMapping.ReadArt(row, RowMapping.Figure),
                // The job's base stats (Slice D stage 32): whole numbers, every one required; StaticData checks the balance's range.
                new StatBlock(row.Int("Str"), row.Int("Dex"), row.Int("Con"), row.Int("Int"), row.Int("Spi"), row.Int("End")),
                // The growth grades (Slice D stage 34), every one required; JobData checks 0..200.
                new StatBlock(row.Int("GrowthStr"), row.Int("GrowthDex"), row.Int("GrowthCon"), row.Int("GrowthInt"), row.Int("GrowthSpi"), row.Int("GrowthEnd")),
                CsvRow.LocalizedHeaders(PassiveName).All(row.IsEmpty) ? null : row.Localized(PassiveName)));
        }

        const string PassiveText = "PassiveText";
        const string PassiveName = "PassiveName";

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

            // PassiveRows is filled only for the InRows condition; PassiveSpec checks that. A shield is in hundredths (stage 22).
            PassiveEffect effect = row.Enum<PassiveEffect>("PassiveEffect");
            return new PassiveSpec(
                row.Enum<PassiveTrigger>("PassiveTrigger"),
                row.Enum<PassiveCondition>("PassiveCondition"),
                row.OptionalRows("PassiveRows"),
                effect,
                row.Enum<PassiveTarget>("PassiveTarget"),
                effect == PassiveEffect.Shield ? row.Centi("PassiveMagnitude") : row.Int("PassiveMagnitude"));
        }
    }

    internal static class ItemMapper
    {
        public static List<ItemData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(
                "Category", "Width", "Height", "CooldownMs", "Rows",
                "Effect1Kind", "Effect1Target", "Effect1Reach", "Effect1Value",
                "Effect2Kind", "Effect2Target", "Effect2Reach", "Effect2Value",
                "ShopWeight", RowMapping.Icon, "Price", "Melee", "Stars", "StarDamage", "CritChancePercent"), new[] { "Unique", "DropWeight", "ScalesWith" });

            return RowMapping.MapRows(table, errors, row =>
            {
                // An item without effects (all of Effect1 empty) never activates: a star item (stage 20). ItemData checks the rest.
                var effects = new List<ItemEffect>();
                foreach (string prefix in new[] { "Effect1", "Effect2" })
                {
                    string[] columns = { prefix + "Kind", prefix + "Target", prefix + "Reach", prefix + "Value" };
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
                    row.IsEmpty("StarDamage") ? 0 : row.Centi("StarDamage"),
                    row.OptionalInt("CritChancePercent", 0),
                    row.OptionalBool("Unique", false),
                    row.OptionalInt("DropWeight", 0, 0),
                    // The stat that grows the item (Slice D stage 32): a Stat name, empty for none. ItemData checks it is one of the four.
                    row.OptionalEnum<Stat>("ScalesWith"));
            });
        }

        /// <summary>
        /// Reach is filled only for targets counted from an end of the enemy line; ItemEffect checks that. The value (stage 22): damage "least-most"
        /// in hundredths ("2-4", "1.5-2.5"), a heal or a shield one value in hundredths ("2.73"), a burn its stacks ("2").
        /// </summary>
        static ItemEffect ReadEffect(CsvRow row, string prefix)
        {
            EffectKind kind = row.Enum<EffectKind>(prefix + "Kind");
            int min;
            int max;
            if (kind == EffectKind.Burn)
            {
                min = max = row.Int(prefix + "Value");
            }
            else if (kind == EffectKind.Damage)
            {
                (min, max) = row.CentiRange(prefix + "Value");
            }
            else
            {
                min = max = row.Centi(prefix + "Value");
            }

            return new ItemEffect(kind, row.Enum<TargetMode>(prefix + "Target"), row.OptionalInt(prefix + "Reach", 0), min, max);
        }
    }

    /// <summary>RecipeData.csv (Slice B stage 23): the forge recipes. Tiers are ItemTier names (Common, Tier1, Tier2, Tier3).</summary>
    internal static class RecipeMapper
    {
        public static List<RecipeData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { RowMapping.Id, "Result", "BaseItem", "BaseTier", "Catalyst", "CatalystTier" });
            return RowMapping.MapRows(table, errors, row => new RecipeData(
                row.Id(RowMapping.Id),
                row.Id("Result"),
                row.Id("BaseItem"),
                row.Enum<ItemTier>("BaseTier"),
                row.Id("Catalyst"),
                row.Enum<ItemTier>("CatalystTier")));
        }
    }

    /// <summary>LevelData.csv (stage 27): Level and ExpToNext; no Id and no Name (the level is the key).</summary>
    internal static class LevelMapper
    {
        public static List<LevelData> Map(CsvTable table, List<string> errors)
        {
            // Slice D stage 34: no MaxHpBonusPercent any more (a level reaches the HP through the stats it grows).
            table.RequireHeaders(new[] { "Level", "ExpToNext" });
            return RowMapping.MapRows(table, errors, row => new LevelData(
                row.Int("Level"),
                row.Int("ExpToNext")));
        }
    }

    /// <summary>BuildingData.csv (Slice C stage 30): Id (a building of the town), Level, Price, Days, Bonus; no Name (the plates are UI text).</summary>
    internal static class BuildingMapper
    {
        public static List<BuildingData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { RowMapping.Id, "Level", "Price", "Days", "Bonus" });
            return RowMapping.MapRows(table, errors, row => new BuildingData(
                row.Id(RowMapping.Id),
                row.Int("Level"),
                row.Int("Price"),
                row.Int("Days"),
                row.Int("Bonus")));
        }
    }

    internal static class BagMapper
    {
        public static List<BagData> Map(CsvTable table, List<string> errors)
        {
            // Icon (round 79) is optional: a bag without one shows as the well where it is a thing.
            table.RequireHeaders(RowMapping.Headers("Width", "Height", "Start", "Price", "ShopWeight", "LootWeight"), new[] { RowMapping.Icon });
            return RowMapping.MapRows(table, errors, row => new BagData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("Width"),
                row.Int("Height"),
                row.Bool("Start"),
                row.Int("Price"),
                row.Int("ShopWeight"),
                row.Int("LootWeight"),
                System.Linq.Enumerable.Contains(table.Headers, RowMapping.Icon) ? RowMapping.ReadArt(row, RowMapping.Icon) : null));
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
                row.Centi("Magnitude"),
                row.Int("ShopWeight"),
                RowMapping.ReadArt(row, RowMapping.Icon),
                row.Int("Price")));
        }
    }

    internal static class EnemyMapper
    {
        public static List<EnemyData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers("Level", "MaxHp", "Items", RowMapping.Figure, "FigureScale"), new[] { "Phases", "Accuracy", "Dodge" });
            return RowMapping.MapRows(table, errors, row => new EnemyData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Int("Level"),
                row.Centi("MaxHp"),
                row.GrantList("Items"),
                RowMapping.ReadArt(row, RowMapping.Figure),
                row.Int("FigureScale"),
                row.PhaseList("Phases"),
                // Slice D stage 33: an enemy's hit and dodge in percent points (optional columns, 0 when empty); EnemyData checks the range.
                row.OptionalInt("Accuracy", 0),
                row.OptionalInt("Dodge", 0)));
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
                "StartingPotions", RowMapping.Background,
                "EliteMinFloor", "EliteChancePercent", "CampMinFloor", "CampChancePercent", "CampFloor",
                "EnemyHpPerFloorPercent", "EnemyPowerPerFloorPercent", "Tier1Floor", "Tier2Floor", "Tier3Floor", "ShopMinFloor", "ShopChancePercent"),
                new[] { "IsFinal", "UnlockDungeonId", "UnlockClears", "MapX", "MapY", "GeneralDropChancePercent", "ShopFloor" });

            return RowMapping.MapRows(table, errors, row => new DungeonData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Id("AffinityId"),
                row.Int("Floors"),
                row.Int("MapMinWidth"),
                row.Int("MapMaxWidth"),
                row.Int("DurationDays"),
                row.IdList("StartingPotions"),
                RowMapping.ReadArt(row, RowMapping.Background),
                row.Int("EliteMinFloor"),
                row.Int("EliteChancePercent"),
                row.Int("CampMinFloor"),
                row.Int("CampChancePercent"),
                row.Int("CampFloor"),
                row.Int("EnemyHpPerFloorPercent"),
                row.Int("EnemyPowerPerFloorPercent"),
                row.Int("Tier1Floor"),
                row.Int("Tier2Floor"),
                row.Int("Tier3Floor"),
                row.Int("ShopMinFloor"),
                row.Int("ShopChancePercent"),
                row.OptionalBool("IsFinal", false),
                row.OptionalId("UnlockDungeonId"),
                row.OptionalInt("UnlockClears", 0, 0),
                row.OptionalInt("MapX", 0, 0, 100),
                row.OptionalInt("MapY", 0, 0, 100),
                row.OptionalInt("GeneralDropChancePercent", 0, 0, 100),
                row.OptionalInt("ShopFloor", 0, 0)));
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
            table.RequireHeaders(RowMapping.Headers("JobId", "Starting", "Traits"));
            return RowMapping.MapRows(table, errors, row => new MercenaryData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.OptionalId("JobId"),
                row.Bool("Starting"),
                row.IdList("Traits")));
        }
    }

    /// <summary>TraitData.csv (Slice D stage 37): an empty effect cell is 0, an empty Excludes is none, an empty Source (stage 38) is an innate trait's.</summary>
    internal static class TraitMapper
    {
        static readonly string[] Effects = { "HitPercent", "DodgePercent", "CritPercent", "DogDeathPercent", "VirtuePercent", "FatiguePercent", "MaxHpPercent" };

        public static List<TraitData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(RowMapping.Headers(new[] { "Kind", "Polarity", "Source", "Weight", "Excludes" }.Concat(Effects).ToArray()));
            return RowMapping.MapRows(table, errors, row => new TraitData(
                row.Id(RowMapping.Id),
                row.Localized(RowMapping.Name),
                row.Enum<TraitKind>("Kind"),
                row.Enum<TraitPolarity>("Polarity"),
                row.OptionalEnum<TraitSource>("Source"),
                row.Int("Weight"),
                row.OptionalId("Excludes"),
                row.OptionalInt("HitPercent", 0),
                row.OptionalInt("DodgePercent", 0),
                row.OptionalInt("CritPercent", 0),
                row.OptionalInt("DogDeathPercent", 0),
                row.OptionalInt("VirtuePercent", 0),
                row.OptionalInt("FatiguePercent", 0),
                row.OptionalInt("MaxHpPercent", 0)));
        }
    }
}
