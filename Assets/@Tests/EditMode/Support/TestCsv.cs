using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Tests
{
    /// <summary>A minimal, valid set of CSV sources for transform tests. Tests replace one file to break one rule.</summary>
    internal static class TestCsv
    {
        public const string Balance =
            "Key,Value\n" +
            "PartySize,3\nMinPartySize,1\nTotalDays,100\nMaxFatigue,100\nFatigueRecoveryPerDay,10\nRestDays,1\n" +
            "DogGraceMs,3000\nDogGraceBreakHits,3\nDogDeathChancePercent,30\nBurnTickMs,1000\n" +
            "StormStartMs,45000\nStormTickMs,1000\nStormBaseDamage,2\nStormGrowth,2\n" +
            "PotionSlots,3\nPotionCooldownMs,1500\nRetreatChancePercent,60\nRetreatCooldownMs,5000\n" +
            "PostBattleHealPercent,10\nMinCooldownMs,200\nRewardChoices,3\nInventoryCells,10\nMapBranchChancePercent,50\nFinalBossLevel,14\n";

        public const string Jobs =
            "Id,Name.ko-KR,Name.en-US,MaxHp,ItemSlots,WeaponItemId,WeaponGrade,RecommendedRow,PassiveTrigger,PassiveCondition,PassiveRows,PassiveEffect,PassiveTarget,PassiveMagnitude,PassiveText.ko-KR,PassiveText.en-US,Figure\n" +
            "knight,기사,Knight,140,3,sword,10,1,BattleStart,InRows,front:1,Shield,Self,20,보호막 {0},Shield {0},unit/job/knight\n" +
            "bishop,주교,Bishop,90,3,staff,10,3,,,,,,,,,\n";

        public const string Items =
            "Id,Name.ko-KR,Name.en-US,Category,Size,CooldownMs,Rows,Effect1Kind,Effect1Target,Effect1Reach,Effect1Power,Effect2Kind,Effect2Target,Effect2Reach,Effect2Power,RewardWeight,Icon\n" +
            "sword,소드,Sword,Weapon,1,2500,front:2,Damage,EnemyFront,1,100,,,,,0,item/sword\n" +
            "staff,지팡이,Staff,Support,1,4000,back:2,Heal,AllyLowestHp,,100,,,,,0,\n" +
            "mace,메이스,Mace,Weapon,2,3200,front:1,Damage,EnemyFront,2,100,Shield,Self,,40,10,item/mace\n" +
            "claw,발톱,Claw,Weapon,1,2000,all,Damage,EnemyBack,1,100,,,,,0,\n";

        public const string Potions =
            "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,RewardWeight\n" +
            "tonic,강장제,Tonic,Heal,50,5\n";

        public const string Enemies =
            "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure\n" +
            "rat,쥐,Rat,2,30,claw:4,unit/enemy/rat\n" +
            "ogre,오우거,Ogre,9,200,claw:12+mace:8,\n";

        public const string EnemyGroups =
            "Id,DungeonId,MinFloor,MaxFloor,IsBoss,Enemies\n" +
            "rats,mine,1,2,false,rat+rat\n" +
            "ogre_lair,mine,0,0,true,ogre+rat+rat\n";

        public const string Affinities =
            "Id,Name.ko-KR,Name.en-US,EnemyCooldownPermille\n" +
            "swift,신속,Swift,-80\n";

        public const string Dungeons =
            "Id,Name.ko-KR,Name.en-US,AffinityId,Floors,MapMinWidth,MapMaxWidth,FatigueCost,DurationDays,ItemGradeBase,ItemGradePerFloor,StartingPotions,Background\n" +
            "mine,광산,Mine,swift,2,2,3,30,2,8,2,tonic,background/dungeon/mine\n";

        public const string Mercenaries =
            "Id,Name.ko-KR,Name.en-US,JobId\n" +
            "rowan,로언,Rowan,knight\n" +
            "ella,엘라,Ella,bishop\n";

        /// <summary>Source file name -> CSV text for every definition.</summary>
        public static Dictionary<string, string> ValidSources()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { StaticDataFiles.Balance.SourceFileName, Balance },
                { StaticDataFiles.Job.SourceFileName, Jobs },
                { StaticDataFiles.Item.SourceFileName, Items },
                { StaticDataFiles.Potion.SourceFileName, Potions },
                { StaticDataFiles.Enemy.SourceFileName, Enemies },
                { StaticDataFiles.EnemyGroup.SourceFileName, EnemyGroups },
                { StaticDataFiles.Affinity.SourceFileName, Affinities },
                { StaticDataFiles.Dungeon.SourceFileName, Dungeons },
                { StaticDataFiles.Mercenary.SourceFileName, Mercenaries },
            };
        }

        /// <summary>The valid sources with one file replaced.</summary>
        public static Func<string, string> With(StaticDataFiles.Entry file, string csv)
        {
            Dictionary<string, string> sources = ValidSources();
            sources[file.SourceFileName] = csv;
            return Reader(sources);
        }

        public static Func<string, string> Reader(Dictionary<string, string> sources)
        {
            return name => sources.TryGetValue(name, out string text) ? text : null;
        }
    }
}
