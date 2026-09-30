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
            Assert.AreEqual(BattleRow.Front, knight.RecommendedRow);
            Assert.AreEqual(PassiveTrigger.BattleStart, knight.Passive.Trigger);
            Assert.AreEqual(PassiveCondition.Front, knight.Passive.Condition);
            Assert.AreEqual(PassiveEffect.Shield, knight.Passive.Effect);
            Assert.AreEqual(PassiveTarget.Self, knight.Passive.Target);
            Assert.AreEqual(20, knight.Passive.Magnitude);
            Assert.AreEqual("보호막 {0}", knight.PassiveText.Resolve("ko-KR"));
            Assert.AreEqual("Shield {0}", knight.PassiveText.Resolve("en-US"));
            Assert.IsNull(data.Jobs.Get("bishop").Passive, "Empty Passive* cells mean no passive.");
            Assert.IsNull(data.Jobs.Get("bishop").PassiveText);

            ItemData mace = data.Items.Get("mace");
            Assert.AreEqual(ItemCategory.Weapon, mace.Category);
            Assert.AreEqual(3200, mace.CooldownMs);
            Assert.AreEqual(RowRequirement.Front, mace.Row);
            Assert.AreEqual(2, mace.Effects.Count);
            Assert.AreEqual(EffectKind.Shield, mace.Effects[1].Kind);
            Assert.AreEqual(TargetMode.Self, mace.Effects[1].Target);
            Assert.AreEqual(40, mace.Effects[1].PowerPercent);
            Assert.AreEqual(10, mace.RewardWeight);
            Assert.AreEqual(1, data.Items.Get("sword").Effects.Count);

            PotionData tonic = data.Potions.Get("tonic");
            Assert.AreEqual(PotionEffect.Heal, tonic.Effect);
            Assert.AreEqual(50, tonic.Magnitude);

            EnemyData ogre = data.Enemies.Get("ogre");
            Assert.AreEqual(9, ogre.Level);
            Assert.AreEqual(200, ogre.MaxHp);
            Assert.AreEqual("mace", ogre.Items[1].ItemId);
            Assert.AreEqual(8, ogre.Items[1].Grade);

            EnemyGroupData lair = data.EnemyGroups.Get("ogre_lair");
            Assert.IsTrue(lair.IsBoss);
            CollectionAssert.AreEqual(new[] { "ogre" }, lair.Front);
            CollectionAssert.AreEqual(new[] { "rat" }, lair.Rear);
            CollectionAssert.IsEmpty(data.EnemyGroups.Get("rats").Rear);

            Assert.AreEqual(-80, data.Affinities.Get("swift").EnemyCooldownPermille);

            DungeonData mine = data.Dungeons.Get("mine");
            Assert.AreEqual("swift", mine.AffinityId);
            Assert.AreEqual(2, mine.Floors);
            Assert.AreEqual(30, mine.FatigueCost);
            Assert.AreEqual(10, mine.RewardGradeAt(2));
            CollectionAssert.AreEqual(new[] { "tonic" }, mine.StartingPotions);

            Assert.AreEqual("knight", data.Mercenaries.Get("rowan").JobId);
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
            StringAssert.Contains("\"RecommendedRow\": \"Front\"", json, "Enums are written by name.");
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
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, TestCsv.Potions + "tonic,강장제 둘,Tonic Two,Heal,10,1\n");

            StringAssert.Contains("duplicate id 'tonic'", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenRowsInvalid_ReportsEveryRowWithFileAndLine()
        {
            DataTransformException exception = TransformFails(
                StaticDataFiles.Potion,
                "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,RewardWeight\n" +
                "Tonic,강장제,Tonic,Heal,50,5\n" +
                "salve,,Salve,Heal,50,5\n" +
                "brew,양조주,TODO,Heal,50,5\n" +
                "tonic,강장제,Tonic,Explode,50,5\n" +
                "draught,물약,Draught,Heal,0,5\n");

            Assert.AreEqual(5, exception.Errors.Count);
            for (int i = 0; i < 5; i++)
            {
                StringAssert.StartsWith($"PotionData.csv({i + 2})", exception.Errors[i]);
            }
        }

        [Test]
        public void Transform_WhenCsvMalformed_ReportsLine()
        {
            DataTransformException exception = TransformFails(StaticDataFiles.Potion, "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,RewardWeight\ntonic,강장제\n");

            StringAssert.Contains("PotionData.csv(2)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenGrantListMalformed_ReportsCell()
        {
            foreach (string items in new[] { "claw", "claw:", "claw:0", "claw:x", "Claw:4", "claw:4+" })
            {
                DataTransformException exception = TransformFails(
                    StaticDataFiles.Enemy,
                    "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items\nrat,쥐,Rat,2,30," + items + "\nogre,오우거,Ogre,9,200,claw:12\n");

                StringAssert.Contains("EnemyData.csv(2) [Items]", exception.Errors[0], items);
            }
        }

        [Test]
        public void Transform_WhenPassiveIsOnlyPartlyFilled_Reports()
        {
            string jobs = TestCsv.Jobs.Replace("bishop,주교,Bishop,90,3,staff,10,Rear,,,,,", "bishop,주교,Bishop,90,3,staff,10,Rear,Heal,,,,");

            DataTransformException exception = TransformFails(StaticDataFiles.Job, jobs);

            StringAssert.StartsWith("JobData.csv(3)", exception.Errors[0]);
        }

        [Test]
        public void Transform_WhenPassiveAndItsTextDoNotComeTogether_Reports()
        {
            string textOnly = TestCsv.Jobs.Replace("Rear,,,,,,,", "Rear,,,,,,보호막,Shield");
            string passiveOnly = TestCsv.Jobs.Replace("Self,20,보호막 {0},Shield {0}", "Self,20,,");
            string oneLocale = TestCsv.Jobs.Replace("Self,20,보호막 {0},Shield {0}", "Self,20,보호막 {0},");

            StringAssert.StartsWith("JobData.csv(3)", TransformFails(StaticDataFiles.Job, textOnly).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, passiveOnly).Errors[0]);
            StringAssert.StartsWith("JobData.csv(2)", TransformFails(StaticDataFiles.Job, oneLocale).Errors[0]);
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
