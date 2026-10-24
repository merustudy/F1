using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class StaticDataTransformerTests
    {
        static TransformResult Transform()
        {
            return StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources()));
        }

        static DataTransformException TransformFails(StaticDataFiles.Entry file, string csv)
        {
            return Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(TestCsv.With(file, csv)));
        }

        [Test]
        public void Transform_RefusesPhases_ThatAreMalformed_OutOfRange_OrNotFalling()
        {
            string head = "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure,FigureScale,Phases\n";
            StringAssert.Contains("is not 'hp percent=item_id:power percent+...'", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,mace:200\n").Errors[0]);
            StringAssert.Contains("HpPercent must be 1..99", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,100=mace:200\n").Errors[0]);
            StringAssert.Contains("falling HP percents", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,50=mace:200|50=claw:200\n").Errors[0]);
            StringAssert.Contains("is not 'item_id:power percent'", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,50=mace\n").Errors[0]);
            DataTransformException unknown = TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,50=axe:200\n");
            StringAssert.Contains("Phases: 'axe' does not exist", unknown.Errors[0]);
        }

        [Test]
        public void Transform_RefusesDeepItems_ThatAreMalformed_BelowTheSecondFloor_NotRising_OrUnknown()
        {
            // 2026-10-20 (Docs/Design/03_Dungeon_Structure.md §4): "floor=item:power+item:power|floor=...", floors from 2 and rising.
            string head = "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure,FigureScale,DeepItems\n";
            StringAssert.Contains("is not 'floor=item_id:power percent+...'", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,mace:100\n").Errors[0]);
            StringAssert.Contains("Floor must be at least 2", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,1=mace:100\n").Errors[0]);
            StringAssert.Contains("rising floors", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,12=mace:100|9=claw:100\n").Errors[0]);
            StringAssert.Contains("is not 'item_id:power percent'", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,9=mace\n").Errors[0]);
            StringAssert.Contains("DeepItems: 'axe' does not exist", TransformFails(StaticDataFiles.Enemy, head + "rat,쥐,Rat,2,11,claw:200,,100,9=axe:100\n").Errors[0]);
        }

        [Test]
        public void Transform_RefusesLevels_ThatDoNotRunFromOne_OrWhoseLastTakesExperience_AndTheOldBonusColumn()
        {
            StringAssert.Contains("run from 1 without a gap", TransformFails(StaticDataFiles.Level, "Level,ExpToNext\n1,10\n3,0\n").Errors[0]);
            StringAssert.Contains("ExpToNext must be 0 at the last level", TransformFails(StaticDataFiles.Level, "Level,ExpToNext\n1,10\n2,20\n").Errors[0]);
            StringAssert.Contains("ExpToNext must be 0 at the last level", TransformFails(StaticDataFiles.Level, "Level,ExpToNext\n1,0\n2,0\n").Errors[0]);
            StringAssert.Contains("at least one level", TransformFails(StaticDataFiles.Level, "Level,ExpToNext\n").Errors[0]);
            StringAssert.Contains("Level 0 must be at least 1", TransformFails(StaticDataFiles.Level, "Level,ExpToNext\n0,10\n").Errors[0]);
            // Slice D stage 34 took the HP bonus out: its column is an unknown header now.
            StringAssert.Contains("MaxHpBonusPercent", TransformFails(StaticDataFiles.Level, "Level,ExpToNext,MaxHpBonusPercent\n1,10,0\n2,0,10\n").Errors[0]);
        }

        [Test]
        public void Transform_RefusesBuildings_ThatAreUnknown_SkipALevel_StartWithAPrice_OrWhoseBonusFalls()
        {
            // Stage 30: BuildingData.csv (Id, Level, Price, Days, Bonus) and StaticData's ladder per building.
            const string head = "Id,Level,Price,Days,Bonus\n";
            const string rest = "inn,1,0,0,0\ncoach,1,0,0,0\n";
            StringAssert.Contains("is not a building of the town", TransformFails(StaticDataFiles.Building, head + "tower,1,0,0,0\n" + rest).Errors[0]);
            StringAssert.Contains("'barracks' has no level", TransformFails(StaticDataFiles.Building, head + rest).Errors[0]);
            StringAssert.Contains("run from 1 without a gap", TransformFails(StaticDataFiles.Building, head + "barracks,1,0,0,0\nbarracks,3,40,3,2\n" + rest).Errors[0]);
            StringAssert.Contains("the first level is the start", TransformFails(StaticDataFiles.Building, head + "barracks,1,10,0,0\n" + rest).Errors[0]);
            StringAssert.Contains("takes a Price and Days above 0", TransformFails(StaticDataFiles.Building, head + "barracks,1,0,0,0\nbarracks,2,40,0,2\n" + rest).Errors[0]);
            StringAssert.Contains("Bonus cannot be under the level below it", TransformFails(StaticDataFiles.Building, head + "barracks,1,0,0,0\nbarracks,2,40,3,2\nbarracks,3,80,5,1\n" + rest).Errors[0]);
        }

        [Test]
        public void Transform_RefusesARecipe_WhoseResultIsNotUnique_OrWhoseCatalystHasNoStars()
        {
            // Stage 23: RecipeData.csv maps (Id, Result, BaseItem, BaseTier, Catalyst, CatalystTier; no Name) and StaticData checks it.
            DataTransformException exception = TransformFails(StaticDataFiles.Recipe, "Id,Result,BaseItem,BaseTier,Catalyst,CatalystTier\nr,sword,mace,Tier3,claw,Tier3\n");
            string errors = string.Join("\n", exception.Errors);
            StringAssert.Contains("not a unique item", errors);
            StringAssert.Contains("has no stars", errors);
        }

        [Test]
        public void Transform_WhenSourcesValid_BuildsDataAndOneFilePerDefinition()
        {
            TransformResult result = Transform();

            Assert.AreEqual(2, result.Data.Jobs.Count);
            Assert.AreEqual("기사", result.Data.Jobs.Get("knight").Name.Resolve("ko-KR"));
            Assert.AreEqual(3000, result.Data.Balance.DogGraceMs);
            Assert.AreEqual(StaticDataFiles.All.Count, result.GeneratedFiles.Count);
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(result.GeneratedFiles.ContainsKey(file.GeneratedFileName), file.GeneratedFileName);
            }
        }

        [Test]
        public void Transform_MapsEveryColumnOfEveryDefinition()
        {
            StaticData data = Transform().Data;

            JobData knight = data.Jobs.Get("knight");
            // 2026-10-20 스탯 개정: no MaxHp column (constitution gives the HP); Wis for Spi.
            Assert.AreEqual(new StatBlock(12, 9, 12, 6, 9, 11), knight.Stats);
            Assert.AreEqual(110, knight.Growth.Str);
            Assert.AreEqual(14, data.Jobs.Get("bishop").Stats.Wis);
            Assert.AreEqual("sword", knight.WeaponItemId);
            Assert.AreEqual(1, knight.RecommendedRow);
            Assert.AreEqual(PassiveTrigger.BattleStart, knight.Passive.Trigger);
            Assert.AreEqual(PassiveCondition.InRows, knight.Passive.Condition);
            Assert.AreEqual(RowEnd.Front, knight.Passive.Rows.From);
            Assert.AreEqual(1, knight.Passive.Rows.Reach);
            Assert.AreEqual(PassiveEffect.Shield, knight.Passive.Effect);
            Assert.AreEqual(PassiveTarget.Self, knight.Passive.Target);
            Assert.AreEqual(545, knight.Passive.Magnitude, "A shield is in hundredths: 5.45.");
            Assert.AreEqual("보호막 {0}", knight.PassiveText.Resolve("ko-KR"));
            Assert.AreEqual("Shield {0}", knight.PassiveText.Resolve("en-US"));
            Assert.IsNull(data.Jobs.Get("bishop").Passive, "Empty Passive* cells mean no passive.");
            Assert.IsNull(data.Jobs.Get("bishop").PassiveText);
            Assert.AreEqual(("선봉", "Vanguard"), (knight.PassiveName.Resolve("ko-KR"), knight.PassiveName.Resolve("en-US")), "The passive's short name as the job's trait (stage 37).");
            Assert.IsNull(data.Jobs.Get("bishop").PassiveName);
            // The traits (Slice D stage 37): every column, an empty effect cell is 0, an empty Excludes none; the starting roster's in the data's order.
            TraitData keen = data.Traits.Get("keen");
            Assert.AreEqual((TraitKind.Innate, TraitPolarity.Positive, (TraitSource?)null, 10, "dull"), (keen.Kind, keen.Polarity, keen.Source, keen.Weight, keen.Excludes));
            // An acquired trait names its source (stage 38).
            Assert.AreEqual((TraitKind.Acquired, (TraitSource?)TraitSource.DeathsDoor, -5), (data.Traits.Get("scar").Kind, data.Traits.Get("scar").Source, data.Traits.Get("scar").MaxHpPercent));
            Assert.AreEqual((5, 2, 1, 0, 0, 0, 0), (keen.HitPercent, keen.DodgePercent, keen.CritPercent, keen.DogDeathPercent, keen.VirtuePercent, keen.FatiguePercent, keen.MaxHpPercent));
            TraitData sturdy = data.Traits.Get("sturdy");
            Assert.AreEqual((5, (string)null, -5, 3, -10, 8), (sturdy.Weight, sturdy.Excludes, sturdy.DogDeathPercent, sturdy.VirtuePercent, sturdy.FatiguePercent, sturdy.MaxHpPercent));
            Assert.AreEqual("둔함", data.Traits.Get("dull").Name.Resolve("ko-KR"));
            // Stage 57: a build drawn in its figure that shifts stats, a virtue's mark with the new effects; empty cells are false, none, 100 and 0.
            TraitData huge = data.Traits.Get("huge");
            Assert.AreEqual((true, true, "huge", 130, 2, -1, 0), (huge.Build, huge.Rare, huge.Figure, huge.FigureScale, huge.Str, huge.Dex, huge.Con));
            TraitData calm = data.Traits.Get("calm");
            Assert.AreEqual(((TraitSource?)TraitSource.Virtue, "focused", 10, 5, -10, -1), (calm.Source, calm.SourceState, calm.GrowthPercent, calm.DamagePercent, calm.HealTakenPercent, calm.AllyFatigueAtStart));
            Assert.AreEqual((false, false, (string)null, 100, (string)null), (keen.Build, keen.Rare, keen.Figure, keen.FigureScale, keen.SourceState));
            CollectionAssert.AreEqual(new[] { "huge" }, data.BuildTraits.Select(t => t.Id).ToList());
            // Stage 57: a job's figures by gender ("+"-joined addresses, empty for none), a name's gender and a starting mercenary's figure.
            CollectionAssert.AreEqual(new[] { "unit/job/knight", "unit/job/blade" }, data.Jobs.Get("knight").FiguresMale);
            CollectionAssert.AreEqual(new[] { "unit/job/dame" }, data.Jobs.Get("knight").FiguresFemale);
            Assert.IsEmpty(data.Jobs.Get("bishop").FiguresMale);
            Assert.AreEqual((Gender.Male, "unit/job/blade"), (data.Mercenaries.Get("rowan").Gender, data.Mercenaries.Get("rowan").Figure));
            Assert.AreEqual((Gender.Female, (string)null), (data.Mercenaries.Get("ella").Gender, data.Mercenaries.Get("ella").Figure));
            CollectionAssert.AreEqual(new[] { "keen", "sturdy" }, data.Mercenaries.Get("rowan").Traits);
            Assert.AreEqual(0, data.Mercenaries.Get("teo").Traits.Count);
            Assert.AreEqual("unit/job/knight", knight.Figure);
            Assert.IsNull(data.Jobs.Get("bishop").Figure, "An empty Figure cell means the job has no art.");
            Assert.AreEqual("unit/enemy/rat", data.Enemies.Get("rat").Figure);
            Assert.IsNull(data.Enemies.Get("ogre").Figure);

            ItemData mace = data.Items.Get("mace");
            Assert.AreEqual(ItemCategory.Weapon, mace.Category);
            Assert.AreEqual(3, mace.Width, "Slice B stage 19: an item is a shape of squares.");
            Assert.AreEqual(2, mace.Height);
            Assert.AreEqual(2, data.Items.Get("sword").Width);
            Assert.AreEqual(1, data.Items.Get("sword").Height);
            Assert.AreEqual("pack", data.StartBag.Id, "The bags come from their own table.");
            Assert.AreEqual(3, data.Bags.Get("pouch").Width);
            Assert.AreEqual("item/sword", data.Items.Get("sword").Icon);
            Assert.IsNull(data.Items.Get("staff").Icon, "An empty Icon cell means the item has no art.");
            Assert.AreEqual(3200, mace.CooldownMs);
            Assert.AreEqual("front:1", mace.Rows.ToString());
            Assert.AreEqual("front:2", data.Items.Get("sword").Rows.ToString());
            Assert.AreEqual("back:2", data.Items.Get("staff").Rows.ToString());
            Assert.IsTrue(data.Items.Get("claw").Rows.IsEveryRow);
            Assert.AreEqual(2, mace.Effects.Count);
            Assert.AreEqual(TargetMode.EnemyFront, mace.Effects[0].Target);
            Assert.AreEqual(2, mace.Effects[0].Reach);
            Assert.AreEqual(EffectKind.Shield, mace.Effects[1].Kind);
            Assert.AreEqual(123, mace.Effects[0].Min, "Damage \"1.23-3.68\" in hundredths (stage 22).");
            Assert.AreEqual(368, mace.Effects[0].Max);
            Assert.AreEqual(98, mace.Effects[1].Min, "A shield has one value.");
            Assert.AreEqual(98, mace.Effects[1].Max);
            Assert.AreEqual(200, data.Items.Get("sword").Effects[0].Min, "\"2-4\".");
            Assert.AreEqual(400, data.Items.Get("sword").Effects[0].Max);
            Assert.AreEqual(5, data.Items.Get("sword").CritChancePercent);
            Assert.AreEqual(0, data.Items.Get("claw").CritChancePercent, "An empty cell is no crit chance.");
            Assert.AreEqual(55, data.Balance.StormBaseDamage, "A balance value in hundredths is written with decimals.");
            Assert.AreEqual(50, data.Balance.HitMinDamage);
            Assert.AreEqual(27, data.Balance.BurnDamagePerStack);
            Assert.AreEqual(150, data.Balance.PhysicalCritDamagePercent);
            Assert.AreEqual(150, data.Balance.MagicCritDamagePercent);
            Assert.AreEqual(5, data.Balance.PowerBaseline, "2026-10-20 스탯 개정: the power baseline, the fatigue baseline, the cooldown per point, the HP bands.");
            Assert.AreEqual(5, data.Balance.FatigueBaseline);
            Assert.AreEqual(1, data.Balance.CooldownPercentPerPoint);
            Assert.AreEqual((500, 1000, 1500, 10, 20), (data.Balance.HpPerConPointLow, data.Balance.HpPerConPointMid, data.Balance.HpPerConPointHigh, data.Balance.HpBandLow, data.Balance.HpBandMid));
            Assert.AreEqual(50, data.Balance.CritPerWisPoint, "0.5 in hundredths.");
            Assert.AreEqual((ItemSchool?)ItemSchool.Physical, data.Items.Get("sword").School);
            Assert.AreEqual((ItemSchool?)ItemSchool.Magic, data.Items.Get("claw").School);
            Assert.IsNull(data.Items.Get("staff").School, "A support item has no school.");
            Assert.AreEqual(10, data.Traits.Get("keen").CritDamagePercent);
            Assert.AreEqual(0, data.Traits.Get("dull").CritDamagePercent, "An empty cell.");
            Assert.AreEqual(1400, data.Potions.Get("tonic").Magnitude);
            Assert.AreEqual(TargetMode.Self, mace.Effects[1].Target);
            Assert.AreEqual(0, mace.Effects[1].Reach, "An empty Reach cell: the target takes none.");
            Assert.AreEqual(TargetMode.EnemyBack, data.Items.Get("claw").Effects[0].Target);
            Assert.AreEqual(1, data.Items.Get("claw").Effects[0].Reach);
            Assert.AreEqual(10, mace.ShopWeight);
            Assert.AreEqual(1, data.Items.Get("sword").Effects.Count);

            PotionData tonic = data.Potions.Get("tonic");
            Assert.AreEqual(PotionEffect.Heal, tonic.Effect);
            Assert.AreEqual(1400, tonic.Magnitude, "14 in hundredths (stage 22).");
            Assert.AreEqual("potion/tonic", tonic.Icon);

            EnemyData ogre = data.Enemies.Get("ogre");
            // A boss's phases (stage 28): "50=mace:200|25=mace:300+claw:300".
            Assert.AreEqual(2, ogre.Phases.Count);
            Assert.AreEqual(50, ogre.Phases[0].HpPercent);
            Assert.AreEqual("mace", ogre.Phases[0].Items[0].ItemId);
            Assert.AreEqual(200, ogre.Phases[0].Items[0].PowerPercent);
            Assert.AreEqual(25, ogre.Phases[1].HpPercent);
            Assert.AreEqual(2, ogre.Phases[1].Items.Count);
            Assert.IsEmpty(data.Enemies.Get("rat").Phases, "An empty cell: no phase.");
            // Deep items (2026-10-20): "9=claw:100|12=mace:100+claw:100" on the rat, an empty cell on the ogre.
            EnemyData rat = data.Enemies.Get("rat");
            CollectionAssert.AreEqual(new[] { 9, 12 }, rat.DeepItems.Select(bundle => bundle.Floor));
            Assert.AreEqual("claw", rat.DeepItems[0].Items[0].ItemId);
            Assert.AreEqual(100, rat.DeepItems[0].Items[0].PowerPercent);
            Assert.AreEqual(2, rat.DeepItems[1].Items.Count);
            Assert.IsEmpty(ogre.DeepItems, "An empty cell: no bundle.");
            Assert.AreEqual(150, ogre.FigureScale, "a boss is drawn larger than the common figure place");
            Assert.AreEqual(EnemyData.DefaultFigureScale, data.Enemies.Get("rat").FigureScale);
            Assert.AreEqual(9, ogre.Level);
            Assert.AreEqual(5450, ogre.MaxHp);
            Assert.AreEqual("mace", ogre.Items[1].ItemId);
            Assert.AreEqual(150, ogre.Items[1].PowerPercent, "An enemy carries its items at its own power (stage 22).");

            EnemyGroupData lair = data.EnemyGroups.Get("ogre_lair");
            Assert.IsTrue(lair.IsBoss);
            CollectionAssert.AreEqual(new[] { "ogre", "rat", "rat" }, lair.Enemies, "From the front: row 1 first.");
            CollectionAssert.AreEqual(new[] { "rat", "rat" }, data.EnemyGroups.Get("rats").Enemies);
            Assert.IsFalse(lair.IsElite);
            Assert.IsTrue(data.EnemyGroups.Get("ogre_guard").IsElite);

            Assert.AreEqual(-80, data.Affinities.Get("swift").EnemyCooldownPermille);

            DungeonData mine = data.Dungeons.Get("mine");
            Assert.AreEqual("swift", mine.AffinityId);
            Assert.AreEqual(2, mine.Floors);
            Assert.AreEqual(2, mine.DurationDays);
            CollectionAssert.AreEqual(new[] { "tonic" }, mine.StartingPotions);
            Assert.AreEqual("background/dungeon/mine", mine.Background);
            Assert.AreEqual(2, mine.EliteMinFloor);
            Assert.AreEqual(20, mine.EliteChancePercent);
            Assert.AreEqual(0, mine.CampMinFloor);
            Assert.AreEqual(0, mine.CampChancePercent);
            Assert.AreEqual(0, mine.CampFloor);
            Assert.AreEqual(5, mine.EnemyHpPerFloorPercent);
            Assert.AreEqual(10, mine.EnemyPowerPerFloorPercent);
            Assert.AreEqual(2, mine.Tier1Floor);
            Assert.AreEqual(0, mine.Tier2Floor);
            Assert.AreEqual(0, mine.Tier3Floor);
            Assert.AreEqual(ItemTier.Common, mine.ItemTierAt(1, elite: false));
            Assert.AreEqual(ItemTier.Tier1, mine.ItemTierAt(2, elite: false));
            Assert.AreEqual(ItemTier.Tier2, mine.ItemTierAt(2, elite: true), "An elite's loot is a tier up.");
            Assert.AreEqual(300, data.Balance.TierPercent(ItemTier.Tier2));

            Assert.AreEqual("knight", data.Mercenaries.Get("rowan").JobId);
            Assert.IsTrue(data.Mercenaries.Get("rowan").Starting);
            Assert.IsFalse(data.Mercenaries.Get("teo").Starting, "A name of the recruit pool (stage 26).");
            Assert.IsNull(data.Mercenaries.Get("teo").JobId, "Its job is rolled when it is drawn.");
            CollectionAssert.AreEquivalent(new[] { "rowan", "ella" }, data.StartingMercenaries.Select(m => m.Id));
            CollectionAssert.AreEquivalent(new[] { "teo", "bran" }, data.RecruitPool.Select(m => m.Id));

            // LevelData.csv (stage 27): Level, ExpToNext; no Id and no Name (the HP bonus left in Slice D stage 34).
            Assert.AreEqual(3, data.MaxLevel);
            Assert.AreEqual(10, data.LevelAt(1).ExpToNext);
            // JobData.csv's growth grades (Slice D stage 34).
            Assert.AreEqual(new StatBlock(110, 50, 90, 20, 50, 50), data.Jobs.Get("knight").Growth);
            Assert.IsTrue(data.LevelAt(3).IsLast);
            // BuildingData.csv (stage 30): two levels per building; the second of the barracks costs 40 gold and 3 days for +2.
            Assert.AreEqual(2, data.MaxBuildingLevel("barracks"));
            Assert.AreEqual(40, data.BuildingAt("barracks", 2).Price);
            Assert.AreEqual(3, data.BuildingAt("barracks", 2).Days);
            Assert.AreEqual(2, data.BuildingAt("barracks", 2).Bonus);
            Assert.AreEqual(0, data.BuildingAt("coach", 1).Bonus);
            Assert.Throws<ArgumentOutOfRangeException>(() => data.BuildingAt("gate", 1), "The gate is no building (2026-10-18).");

            FatigueStateData fearful = data.FatigueStates.Get("fearful");
            Assert.AreEqual(FatigueStateKind.Affliction, fearful.Kind);
            Assert.AreEqual(25, fearful.CooldownPercent);
            Assert.AreEqual("느리다", fearful.Description.Resolve("ko-KR"));
            Assert.AreEqual(FatigueStateKind.Virtue, data.FatigueStates.Get("focused").Kind);
            Assert.AreEqual(-20, data.FatigueStates.Get("focused").CooldownPercent);
        }

        [Test]
        public void Transform_WritesItemsSortedByIdWithStableFormatting()
        {
            string json = Transform().GeneratedFiles[StaticDataFiles.Job.GeneratedFileName];

            Assert.Less(json.IndexOf("\"bishop\"", StringComparison.Ordinal), json.IndexOf("\"knight\"", StringComparison.Ordinal));
            StringAssert.StartsWith("{\n  \"SchemaVersion\": 1,\n  \"Items\": [\n", json);
            StringAssert.EndsWith("}\n", json);
            Assert.IsFalse(json.Contains("\r"), "Generated JSON uses \\n only.");
            StringAssert.Contains("기사", json, "Non-ASCII text is written as is, not escaped.");
            StringAssert.Contains("\"Trigger\": \"BattleStart\"", json, "Enums are written by name.");
            StringAssert.Contains("\"RecommendedRow\": 1", json, "Rows are written as numbers.");
            StringAssert.Contains("\"Rows\": {\n          \"From\": \"Front\",\n          \"Reach\": 1\n        }", json, "A span is written as its end and reach.");
        }

        [Test]
        public void Transform_WhenRunTwice_ProducesIdenticalText()
        {
            IReadOnlyDictionary<string, string> first = Transform().GeneratedFiles;
            IReadOnlyDictionary<string, string> second = Transform().GeneratedFiles;

            foreach (KeyValuePair<string, string> file in first)
            {
                Assert.AreEqual(file.Value, second[file.Key], file.Key);
            }
        }

        [Test]
        public void Transform_WhenColumnAndRowOrderChange_ProducesIdenticalText()
        {
            const string reordered =
                "Name.en-US,EnemyCooldownPermille,Id,Name.ko-KR\r\n" +
                "Swift,-80,swift,신속\r\n";

            string expected = Transform().GeneratedFiles[StaticDataFiles.Affinity.GeneratedFileName];
            string actual = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Affinity, reordered))
                .GeneratedFiles[StaticDataFiles.Affinity.GeneratedFileName];

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Transform_WhenSourceFileMissing_ReportsEveryMissingFile()
        {
            var exception = Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(_ => null));

            Assert.AreEqual(StaticDataFiles.All.Count, exception.Errors.Count);
            StringAssert.Contains(StaticDataFiles.Job.SourceFileName, string.Join("\n", exception.Errors));
        }

        [Test]
        public void Transform_WhenRequiredHeaderMissing_ReportsHeader()
        {
            DataTransformException exception = TransformFails(StaticDataFiles.Affinity, "Id,Name.ko-KR,EnemyCooldownPermille\nswift,신속,-80\n");

            StringAssert.Contains("Name.en-US", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenHeaderUnknown_ReportsHeader()
        {
            DataTransformException exception = TransformFails(
                StaticDataFiles.Affinity,
                "Id,Name.ko-KR,Name.en-US,Name.ja-JP,EnemyCooldownPermille\nswift,신속,Swift,迅速,-80\n");

            StringAssert.Contains("Name.ja-JP", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenIdDuplicated_ReportsId()
        {
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, TestCsv.Potions + "tonic,강장제 둘,Tonic Two,Heal,10,1,,8\n");

            StringAssert.Contains("duplicate id 'tonic'", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenRowsInvalid_ReportsEveryRowWithFileAndLine()
        {
            DataTransformException exception = TransformFails(
                StaticDataFiles.Potion,
                "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,ShopWeight,Icon,Price\n" +
                "Tonic,강장제,Tonic,Heal,50,5,,8\n" +
                "salve,,Salve,Heal,50,5,,8\n" +
                "brew,양조주,TODO,Heal,50,5,,8\n" +
                "tonic,강장제,Tonic,Explode,50,5,,8\n" +
                "draught,물약,Draught,Heal,0,5,,8\n");

            Assert.AreEqual(5, exception.Errors.Count);
            for (int i = 0; i < 5; i++)
            {
                StringAssert.StartsWith($"PotionData.csv({i + 2})", exception.Errors[i]);
            }
        }

        [Test]
        public void Transform_WhenTheBackgroundCellIsEmpty_TheDungeonHasNoArt()
        {
            string dungeons = TestCsv.Dungeons.Replace(",tonic,background/dungeon/mine,", ",tonic,,");
            StringAssert.Contains(",tonic,,", dungeons);

            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Dungeon, dungeons)).Data;

            Assert.IsNull(data.Dungeons.Get("mine").Background);
        }

        [Test]
        public void Transform_WhenCsvMalformed_ReportsLine()
        {
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,ShopWeight,Icon,Price\ntonic,강장제\n");

            StringAssert.Contains("PotionData.csv(2)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenGrantListMalformed_ReportsCell()
        {
            foreach (string items in new[] { "claw", "claw:", "claw:0", "claw:x", "Claw:4", "claw:4+" })
            {
                DataTransformException exception = TransformFails(
                    StaticDataFiles.Enemy,
                    "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure,FigureScale\nrat,쥐,Rat,2,30," + items + ",,100\nogre,오우거,Ogre,9,200,claw:12,,100\n");

                StringAssert.Contains("EnemyData.csv(2) [Items]", exception.Errors[0], items);
            }
        }

        [Test]
        public void Transform_WhenPassiveIsOnlyPartlyFilled_Reports()
        {
            string jobs = TestCsv.Jobs.Replace("staff,3,,", "staff,3,Heal,");
            StringAssert.Contains("staff,3,Heal,", jobs);

            DataTransformException exception = TransformFails(StaticDataFiles.Job, jobs);

            StringAssert.StartsWith("JobData.csv(3)", exception.Errors[0]);
        }

        [Test]
        public void Transform_ReadsTheJobsStats_AndTheItemsSchool_AndRefusesWhatIsNotOne()
        {
            // Slice D stage 32: the six stat columns are required. 2026-10-20 스탯 개정: School (Physical or Magic) is an optional column; a weapon or an attack
            // item left empty is physical, and the school and the category say which stat grows the item (no ScalesWith column any more).
            StaticData data = StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources())).Data;
            Assert.IsTrue(data.Jobs.Get("knight").Stats.Equals(new StatBlock(12, 9, 12, 6, 9, 11)));
            Assert.AreEqual((Stat?)Stat.Str, data.Items.Get("sword").ScalesWith, "A physical weapon: strength.");
            Assert.AreEqual((Stat?)Stat.Int, data.Items.Get("claw").ScalesWith, "A magic weapon: intellect.");
            Assert.AreEqual((Stat?)Stat.Wis, data.Items.Get("staff").ScalesWith, "A support item: wisdom.");

            // Stage 34: the six growth columns follow the stats; dropping End keeps them.
            string withoutEnd = TestCsv.Jobs.Replace(",Wis,End,GrowthStr", ",Wis,GrowthStr").Replace(",9,11,110,", ",9,110,").Replace(",14,10,20,", ",14,20,");
            StringAssert.Contains("End", TransformFails(StaticDataFiles.Job, withoutEnd).Errors[0]);
            string notANumber = TestCsv.Jobs.Replace("unit/job/knight,12,", "unit/job/knight,strong,");
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, notANumber).Errors[0]);
            string oldSpirit = TestCsv.Jobs.Replace(",Wis,End,GrowthStr", ",Spi,End,GrowthStr");
            StringAssert.Contains("Wis", TransformFails(StaticDataFiles.Job, oldSpirit).Errors[0], "Spirit's column is wisdom's now.");
            string withHp = TestCsv.Jobs.Replace("Name.en-US,WeaponItemId", "Name.en-US,MaxHp,WeaponItemId").Replace("Knight,sword", "Knight,38,sword").Replace("Bishop,staff", "Bishop,24.5,staff");
            StringAssert.Contains("MaxHp", TransformFails(StaticDataFiles.Job, withHp).Errors[0], "A job has no HP column any more.");

            // Without the School column every weapon is physical; a school that is not one, or a school on a support item, is refused.
            string[] lines = TestCsv.Items.Split('\n');
            string noSchool = string.Join("\n", lines.Select((line, i) => i == 0 ? line.Replace(",School", "") : line.Length == 0 ? line : line.Substring(0, line.LastIndexOf(','))));
            StaticData physical = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, noSchool)).Data;
            Assert.IsTrue(physical.Items.Ordered.Where(i => i.Category == ItemCategory.Weapon).All(i => i.School == ItemSchool.Physical));
            Assert.IsNull(physical.Items.Get("staff").School);
            StringAssert.StartsWith("ItemData.csv(2)", TransformFails(StaticDataFiles.Item, TestCsv.Items.Replace(",5,Physical\nstaff", ",5,Luck\nstaff")).Errors[0]);
            StringAssert.Contains("only a weapon, an attack item or defensive gear has a School", TransformFails(StaticDataFiles.Item, TestCsv.Items.Replace("false,,,,\nmace", "false,,,,Magic\nmace")).Errors[0]);
            string oldAxis = TestCsv.Items.Replace("CritChancePercent,School", "CritChancePercent,ScalesWith");
            StringAssert.Contains("ScalesWith", TransformFails(StaticDataFiles.Item, oldAxis).Errors[0], "The axis column is gone.");
        }

        [Test]
        public void Transform_ReadsWhenAnItemActivates_AndTheCritItTakesOff()
        {
            // Slice D stage 39: Trigger (empty = Cooldown, BattleStart) and CritTakenPercent (empty = 0) are optional columns; ItemData checks them.
            StaticData data = StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources())).Data;
            Assert.IsTrue(data.Items.Ordered.All(i => i.Trigger == ItemTrigger.Cooldown && i.CritTakenPercent == 0), "Without the columns: every item cycles and takes nothing off.");

            // The two columns after School (2026-10-20): every row of the test items gets two empty cells, the helm fills them.
            string[] rows = TestCsv.Items.Split('\n');
            string items = string.Join("\n", rows.Select((line, i) => i == 0 ? line + ",Trigger,CritTakenPercent" : line.Length == 0 ? line : line + ",,"))
                + "helm,투구,Helm,Armor,2,1,0,all,Shield,Self,,2.18,,,,,10,,12,false,,,,,BattleStart,3\n";
            StaticData withArmour = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, items)).Data;
            ItemData helm = withArmour.Items.Get("helm");
            Assert.AreEqual(ItemTrigger.BattleStart, helm.Trigger);
            Assert.AreEqual(3, helm.CritTakenPercent);
            Assert.AreEqual(218, helm.Effects[0].Min);
            Assert.AreEqual(ItemTrigger.Cooldown, withArmour.Items.Get("sword").Trigger, "An empty Trigger is the cooldown.");
            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace("Armor,2,1,0,all", "Armor,2,1,5000,all")).Errors[0], "A cooldown with BattleStart.");
            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace("helm,투구,Helm,Armor", "helm,투구,Helm,Support")).Errors[0], "CritTakenPercent off defensive gear.");
            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace(",BattleStart,3", ",Sometimes,3")).Errors[0]);
        }

        [Test]
        public void Transform_ReadsWhatAnItemAsksOfItsOwner_AStatAndAValueTogether()
        {
            // Slice D stage 42 (Docs/Design/02 §4 "장비 요구 스탯"): ReqStat (a Stat name) and ReqValue are optional columns, both or neither, on a weapon or
            // defensive gear. Without the columns no item asks anything.
            StaticData data = StaticDataTransformer.Transform(TestCsv.Reader(TestCsv.ValidSources())).Data;
            Assert.IsTrue(data.Items.Ordered.All(i => i.ReqStat == null && i.ReqValue == 0), "Without the columns: anyone wields anything.");

            string[] rows = TestCsv.Items.Split('\n');
            string items = string.Join("\n", rows.Select((line, i) => i == 0 ? line + ",ReqStat,ReqValue" : line.Length == 0 ? line : line + ",,"))
                + "axe,도끼,Axe,Weapon,2,1,3000,front:1,Damage,EnemyFront,1,2-4,,,,,10,,12,true,,,5,Physical,Str,7\n";
            StaticData asking = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, items)).Data;
            Assert.AreEqual((Stat?)Stat.Str, asking.Items.Get("axe").ReqStat);
            Assert.AreEqual(7, asking.Items.Get("axe").ReqValue);
            Assert.IsNull(asking.Items.Get("sword").ReqStat, "Empty cells: nothing asked.");
            Assert.AreEqual(0, asking.Items.Get("sword").ReqValue);

            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace(",Physical,Str,7", ",Physical,Str,")).Errors[0], "A stat without its value.");
            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace(",Physical,Str,7", ",Physical,,7")).Errors[0], "A value without its stat.");
            StringAssert.StartsWith("ItemData.csv(6)", TransformFails(StaticDataFiles.Item, items.Replace(",Physical,Str,7", ",Physical,Luck,7")).Errors[0], "Not a stat.");
            StringAssert.Contains("only equipment (a weapon or defensive gear) has a ReqStat", TransformFails(StaticDataFiles.Item, items.Replace("0,false,,,,,,\nmace", "0,false,,,,,Str,5\nmace")).Errors[0],
                "A support item (the staff) asks nothing.");
        }

        [Test]
        public void Transform_WhenPassiveAndItsTextDoNotComeTogether_Reports()
        {
            string textOnly = TestCsv.Jobs.Replace("staff,3,,,,,,,,", "staff,3,,,,,,,보호막,Shield");
            StringAssert.Contains("보호막,Shield", textOnly);
            string passiveOnly = TestCsv.Jobs.Replace("Self,5.45,보호막 {0},Shield {0}", "Self,5.45,,");
            string oneLocale = TestCsv.Jobs.Replace("Self,5.45,보호막 {0},Shield {0}", "Self,5.45,보호막 {0},");

            StringAssert.StartsWith("JobData.csv(3)", TransformFails(StaticDataFiles.Job, textOnly).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, passiveOnly).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, oneLocale).Errors[0]);
        }

        [TestCase("")]
        [TestCase("1")]
        [TestCase("1+2")]
        [TestCase("front:0")]
        [TestCase("back:5")]
        [TestCase("middle:2")]
        [TestCase("1,2")]
        public void Transform_WhenItemRowsAreNotASpan_Reports(string rows)
        {
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,2,1,2500,front:2,", "sword,소드,Sword,Weapon,2,1,2500," + (rows.Contains(",") ? "\"" + rows + "\"" : rows) + ",");
            StringAssert.DoesNotContain("2500,front:2,", items);

            DataTransformException exception = TransformFails(StaticDataFiles.Item, items);

            StringAssert.StartsWith("ItemData.csv(2)", exception.Errors[0]);
            StringAssert.Contains("[Rows]", exception.Errors[0]);
        }

        /// <summary>A whetstone-like row (Slice B stage 20) after the test items: no effects, no cooldown, its stars as given.</summary>
        static string WithHone(string stars)
        {
            return TestCsv.Items + "hone,숫돌,Hone,Other,1,1,0,all,,,,,,,,,8,,8,false," + stars + ",1,,\n";
        }

        [Test]
        public void Transform_ReadsMeleeStarsAndAnItemWithoutEffects()
        {
            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, WithHone("0:-1+0:1"))).Data;

            ItemData hone = data.Items.Get("hone");
            Assert.IsTrue(hone.IsPassive);
            Assert.AreEqual(0, hone.CooldownMs);
            CollectionAssert.AreEqual(new[] { (0, -1), (0, 1) }, hone.Stars.Select(s => (s.X, s.Y)));
            Assert.AreEqual(100, hone.StarDamage, "+1 in hundredths (stage 22).");
            Assert.IsTrue(data.Items.Get("sword").Melee);
            Assert.IsFalse(data.Items.Get("staff").Melee);
            Assert.IsEmpty(data.Items.Get("sword").Stars, "An empty cell: no stars.");
            Assert.AreEqual(0, data.Items.Get("sword").StarDamage);
        }

        [TestCase("0:0")]
        [TestCase("0:-2")]
        [TestCase("0:-1+0:-1")]
        [TestCase("0-1")]
        public void Transform_WhenStarsAreNotSquaresRoundTheItem_Reports(string stars)
        {
            DataTransformException exception = TransformFails(StaticDataFiles.Item, WithHone(stars));

            StringAssert.StartsWith("ItemData.csv(6)", exception.Errors[0]);
        }

        [TestCase("0")]
        [TestCase("4")]
        [TestCase("x")]
        public void Transform_WhenItemWidthIsNotOneToThree_Reports(string size)
        {
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,2,1,2500,", "sword,소드,Sword,Weapon," + size + ",1,2500,");
            StringAssert.DoesNotContain("Weapon,2,1,2500,front:2", items);

            DataTransformException exception = TransformFails(StaticDataFiles.Item, items);

            StringAssert.StartsWith("ItemData.csv(2)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenPassiveRowsDoNotMatchTheCondition_Reports()
        {
            // InRows needs a span; every other condition takes none.
            string withoutRows = TestCsv.Jobs.Replace("BattleStart,InRows,front:1,Shield", "BattleStart,InRows,,Shield");
            string rowsNotAsked = TestCsv.Jobs.Replace("BattleStart,InRows,front:1,Shield", "BattleStart,None,front:1,Shield");
            string notASpan = TestCsv.Jobs.Replace("BattleStart,InRows,front:1,Shield", "BattleStart,InRows,1,Shield");
            StringAssert.Contains("InRows,,Shield", withoutRows);
            StringAssert.Contains("None,front:1,Shield", rowsNotAsked);
            StringAssert.Contains("InRows,1,Shield", notASpan);

            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, withoutRows).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, rowsNotAsked).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, notASpan).Errors[0]);
        }

        [TestCase("EnemyFront,,2-4", Description = "A target counted from an end needs a reach.")]
        [TestCase("EnemyFront,0,2-4")]
        [TestCase("EnemyBack,5,2-4", Description = "Deeper than a side has rows.")]
        [TestCase("EnemyAll,2,2-4", Description = "All enemies: no reach to give.")]
        public void Transform_WhenTheReachDoesNotFitTheTarget_Reports(string targetReachPower)
        {
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,2,1,2500,front:2,Damage,EnemyFront,1,2-4,", "sword,소드,Sword,Weapon,2,1,2500,front:2,Damage," + targetReachPower + ",");
            StringAssert.Contains("Damage," + targetReachPower + ",", items);

            DataTransformException exception = TransformFails(StaticDataFiles.Item, items);

            StringAssert.StartsWith("ItemData.csv(2)", exception.Errors[0]);
            StringAssert.Contains("Reach", exception.Errors[0]);
        }

        [TestCase("", Description = "Nobody.")]
        [TestCase("rat+rat+rat+rat+rat", Description = "More than one per row can hold.")]
        public void Transform_WhenAnEnemyGroupDoesNotFitOnePerRow_Reports(string enemies)
        {
            string groups = TestCsv.EnemyGroups.Replace("rats,mine,1,2,false,false,rat+rat", "rats,mine,1,2,false,false," + enemies);
            StringAssert.Contains("rats,mine,1,2,false,false," + enemies + "\n", groups);

            DataTransformException exception = TransformFails(StaticDataFiles.EnemyGroup, groups);

            StringAssert.StartsWith("EnemyGroupData.csv(2)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenBalanceKeyMissingUnknownOrOutOfRange_Reports()
        {
            StringAssert.Contains("'RestDays' is missing", TransformFails(StaticDataFiles.Balance, TestCsv.Balance.Replace("RestDays,1\n", "")).Errors[0]);
            StringAssert.Contains("unknown key 'Bonus'", TransformFails(StaticDataFiles.Balance, TestCsv.Balance + "Bonus,1\n").Errors[0]);
            StringAssert.Contains("'DogDeathChancePercent'", TransformFails(StaticDataFiles.Balance, TestCsv.Balance.Replace("DogDeathChancePercent,30", "DogDeathChancePercent,130")).Errors[0]);
            StringAssert.Contains("duplicate key", TransformFails(StaticDataFiles.Balance, TestCsv.Balance + "RestDays,2\n").Errors[0]);
            StringAssert.Contains("PascalCase", TransformFails(StaticDataFiles.Balance, TestCsv.Balance + "rest_days,2\n").Errors[0]);
        }
    }
}
