using System;
using System.Collections.Generic;
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
            Assert.AreEqual(140, knight.MaxHp);
            Assert.AreEqual(3, knight.ItemSlots);
            Assert.AreEqual("sword", knight.WeaponItemId);
            Assert.AreEqual(10, knight.WeaponGrade);
            Assert.AreEqual(1, knight.RecommendedRow);
            Assert.AreEqual(PassiveTrigger.BattleStart, knight.Passive.Trigger);
            Assert.AreEqual(PassiveCondition.InRows, knight.Passive.Condition);
            Assert.AreEqual(RowEnd.Front, knight.Passive.Rows.From);
            Assert.AreEqual(1, knight.Passive.Rows.Reach);
            Assert.AreEqual(PassiveEffect.Shield, knight.Passive.Effect);
            Assert.AreEqual(PassiveTarget.Self, knight.Passive.Target);
            Assert.AreEqual(20, knight.Passive.Magnitude);
            Assert.AreEqual("보호막 {0}", knight.PassiveText.Resolve("ko-KR"));
            Assert.AreEqual("Shield {0}", knight.PassiveText.Resolve("en-US"));
            Assert.IsNull(data.Jobs.Get("bishop").Passive, "Empty Passive* cells mean no passive.");
            Assert.IsNull(data.Jobs.Get("bishop").PassiveText);
            Assert.AreEqual("unit/job/knight", knight.Figure);
            Assert.IsNull(data.Jobs.Get("bishop").Figure, "An empty Figure cell means the job has no art.");
            Assert.AreEqual("unit/enemy/rat", data.Enemies.Get("rat").Figure);
            Assert.IsNull(data.Enemies.Get("ogre").Figure);

            ItemData mace = data.Items.Get("mace");
            Assert.AreEqual(ItemCategory.Weapon, mace.Category);
            Assert.AreEqual(2, mace.Size);
            Assert.AreEqual(1, data.Items.Get("sword").Size);
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
            Assert.AreEqual(TargetMode.Self, mace.Effects[1].Target);
            Assert.AreEqual(0, mace.Effects[1].Reach, "An empty Reach cell: the target takes none.");
            Assert.AreEqual(40, mace.Effects[1].PowerPercent);
            Assert.AreEqual(TargetMode.EnemyBack, data.Items.Get("claw").Effects[0].Target);
            Assert.AreEqual(1, data.Items.Get("claw").Effects[0].Reach);
            Assert.AreEqual(10, mace.RewardWeight);
            Assert.AreEqual(1, data.Items.Get("sword").Effects.Count);

            PotionData tonic = data.Potions.Get("tonic");
            Assert.AreEqual(PotionEffect.Heal, tonic.Effect);
            Assert.AreEqual(50, tonic.Magnitude);
            Assert.AreEqual("potion/tonic", tonic.Icon);

            EnemyData ogre = data.Enemies.Get("ogre");
            Assert.AreEqual(150, ogre.FigureScale, "a boss is drawn larger than the common figure place");
            Assert.AreEqual(EnemyData.DefaultFigureScale, data.Enemies.Get("rat").FigureScale);
            Assert.AreEqual(9, ogre.Level);
            Assert.AreEqual(200, ogre.MaxHp);
            Assert.AreEqual("mace", ogre.Items[1].ItemId);
            Assert.AreEqual(8, ogre.Items[1].Grade);

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
            Assert.AreEqual(10, mine.RewardGradeAt(2));
            CollectionAssert.AreEqual(new[] { "tonic" }, mine.StartingPotions);
            Assert.AreEqual("background/dungeon/mine", mine.Background);
            Assert.AreEqual(2, mine.EliteMinFloor);
            Assert.AreEqual(20, mine.EliteChancePercent);
            Assert.AreEqual(0, mine.CampMinFloor);
            Assert.AreEqual(0, mine.CampChancePercent);
            Assert.AreEqual(0, mine.CampFloor);
            Assert.AreEqual(5, mine.EnemyHpPerFloorPercent);
            Assert.AreEqual(1, mine.EnemyGradePerFloor);
            Assert.AreEqual(2, mine.SilverFloor);
            Assert.AreEqual(0, mine.GoldFloor);
            Assert.AreEqual(0, mine.DiamondFloor);
            Assert.AreEqual(ItemTier.Bronze, mine.RewardTierAt(1, elite: false));
            Assert.AreEqual(ItemTier.Silver, mine.RewardTierAt(2, elite: false));
            Assert.AreEqual(ItemTier.Gold, mine.RewardTierAt(2, elite: true), "An elite rewards a tier up.");
            Assert.AreEqual(300, data.Balance.TierPercent(ItemTier.Gold));

            Assert.AreEqual("knight", data.Mercenaries.Get("rowan").JobId);

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
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, TestCsv.Potions + "tonic,강장제 둘,Tonic Two,Heal,10,1,\n");

            StringAssert.Contains("duplicate id 'tonic'", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenRowsInvalid_ReportsEveryRowWithFileAndLine()
        {
            DataTransformException exception = TransformFails(
                StaticDataFiles.Potion,
                "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,RewardWeight,Icon\n" +
                "Tonic,강장제,Tonic,Heal,50,5,\n" +
                "salve,,Salve,Heal,50,5,\n" +
                "brew,양조주,TODO,Heal,50,5,\n" +
                "tonic,강장제,Tonic,Explode,50,5,\n" +
                "draught,물약,Draught,Heal,0,5,\n");

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
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,RewardWeight,Icon\ntonic,강장제\n");

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
            string jobs = TestCsv.Jobs.Replace("staff,10,3,,", "staff,10,3,Heal,");
            StringAssert.Contains("staff,10,3,Heal,", jobs);

            DataTransformException exception = TransformFails(StaticDataFiles.Job, jobs);

            StringAssert.StartsWith("JobData.csv(3)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenPassiveAndItsTextDoNotComeTogether_Reports()
        {
            string textOnly = TestCsv.Jobs.Replace("staff,10,3,,,,,,,,", "staff,10,3,,,,,,,보호막,Shield");
            StringAssert.Contains("보호막,Shield", textOnly);
            string passiveOnly = TestCsv.Jobs.Replace("Self,20,보호막 {0},Shield {0}", "Self,20,,");
            string oneLocale = TestCsv.Jobs.Replace("Self,20,보호막 {0},Shield {0}", "Self,20,보호막 {0},");

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
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,1,2500,front:2,", "sword,소드,Sword,Weapon,1,2500," + (rows.Contains(",") ? "\"" + rows + "\"" : rows) + ",");
            StringAssert.DoesNotContain("2500,front:2,", items);

            DataTransformException exception = TransformFails(StaticDataFiles.Item, items);

            StringAssert.StartsWith("ItemData.csv(2)", exception.Errors[0]);
            StringAssert.Contains("[Rows]", exception.Errors[0]);
        }

        [TestCase("0")]
        [TestCase("4")]
        [TestCase("x")]
        public void Transform_WhenItemSizeIsNotOneToThree_Reports(string size)
        {
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,1,2500,", "sword,소드,Sword,Weapon," + size + ",2500,");
            StringAssert.DoesNotContain("Weapon,1,2500,front:2", items);

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

        [TestCase("EnemyFront,,100", Description = "A target counted from an end needs a reach.")]
        [TestCase("EnemyFront,0,100")]
        [TestCase("EnemyBack,5,100", Description = "Deeper than a side has rows.")]
        [TestCase("EnemyAll,2,100", Description = "All enemies: no reach to give.")]
        public void Transform_WhenTheReachDoesNotFitTheTarget_Reports(string targetReachPower)
        {
            string items = TestCsv.Items.Replace("sword,소드,Sword,Weapon,1,2500,front:2,Damage,EnemyFront,1,100,", "sword,소드,Sword,Weapon,1,2500,front:2,Damage," + targetReachPower + ",");
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
