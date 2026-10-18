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
            "PartySize,3\nMinPartySize,1\nTotalDays,100\nDeathLimit,15\nMaxFatigue,200\nFatigueRecoveryPerDay,10\nFatigueBreakdown,100\nFatigueBattleEntry,3\nFatigueEquipment,1\nFatigueOnHit,2\nFatigueOnDog,10\nFatigueOnAllyDog,5\nFatigueOnAllyDeath,15\nFatigueOnKill,2\nVirtueChancePercent,25\nVirtueFatigue,40\nRestDays,1\n" +
            "DogGraceMs,3000\nDogGraceBreakHits,3\nDogDeathChancePercent,30\nBurnTickMs,1000\n" +
            "StormStartMs,45000\nStormTickMs,1000\nStormBaseDamage,0.55\nStormGrowth,0.55\n" +
            "BurnDamagePerStack,0.27\nHitMinDamage,0.5\nCritDamagePercent,150\nCritFatigueTaken,9\nCritFatigueTakenAllies,2\nCritFatigueDealt,3\nCritFatigueDealtAllies,1\n" +
            "PotionSlots,3\nPotionCooldownMs,1500\nRetreatChancePercent,60\nRetreatCooldownMs,5000\n" +
            "PostBattleHealPercent,10\nMinCooldownMs,200\nDropCount,2\nEliteDropCount,3\nInventoryWidth,10\nInventoryHeight,3\nEliteBagPercent,50\nCampHealPercent,30\nCampFatigueRelief,20\nTier1Percent,200\nTier2Percent,300\nTier3Percent,400\nMapBranchChancePercent,50\nFinalBossLevel,14\n" +
            "ShopSlots,4\nShopRefreshBase,3\nShopRefreshStep,2\nSellPercent,50\nCoinsPerGold,20\nShopPotionChancePercent,50\nCoinsPerEnemy,2\nCoinsPerFloor,1\nEliteCoinPercent,200\n" +
            "GoldPerFloor,2\nGoldClear,20\nHirePrice,30\nRecruitCount,2\nRosterLimit,8\nTreatmentPrice,20\nTreatmentRelief,40\nExpPerFloor,1\nExpClear,5\n" +
            "StatBaseline,10\nStatMin,1\nStatMax,30\nStatPowerPercentPerPoint,10\nHpPercentPerConPoint,5\n" +
            "HitBasePercent,80\nHitMinPercent,5\nHitMaxPercent,95\nHitPerStatPoint,1\nDodgePerDexPoint,0.5\nCritPerDexPoint,0.5\nDogDeathPercentPerConPoint,1\nVirtuePerSpiPoint,1\nFatiguePercentPerEndPoint,3\nRecruitStatSpread,2\n" +
            "RecruitTraitWeight0,30\nRecruitTraitWeight1,50\nRecruitTraitWeight2,20\n";

        public const string Jobs =
            "Id,Name.ko-KR,Name.en-US,MaxHp,WeaponItemId,RecommendedRow,PassiveTrigger,PassiveCondition,PassiveRows,PassiveEffect,PassiveTarget,PassiveMagnitude,PassiveText.ko-KR,PassiveText.en-US,Figure,Str,Dex,Con,Int,Spi,End,GrowthStr,GrowthDex,GrowthCon,GrowthInt,GrowthSpi,GrowthEnd,PassiveName.ko-KR,PassiveName.en-US\n" +
            "knight,기사,Knight,38,sword,1,BattleStart,InRows,front:1,Shield,Self,5.45,보호막 {0},Shield {0},unit/job/knight,12,9,12,6,9,11,110,50,90,20,50,50,선봉,Vanguard\n" +
            "bishop,주교,Bishop,24.5,staff,3,,,,,,,,,,7,8,9,11,14,10,20,50,50,90,110,50,,\n";

        public const string Items =
            "Id,Name.ko-KR,Name.en-US,Category,Width,Height,CooldownMs,Rows,Effect1Kind,Effect1Target,Effect1Reach,Effect1Value,Effect2Kind,Effect2Target,Effect2Reach,Effect2Value,ShopWeight,Icon,Price,Melee,Stars,StarDamage,CritChancePercent\n" +
            "sword,소드,Sword,Weapon,2,1,2500,front:2,Damage,EnemyFront,1,2-4,,,,,0,item/sword,0,true,,,5\n" +
            "staff,지팡이,Staff,Support,3,1,4000,back:2,Heal,AllyLowestHp,,2.73,,,,,0,,0,false,,,\n" +
            "mace,메이스,Mace,Weapon,3,2,3200,front:1,Damage,EnemyFront,2,1.23-3.68,Shield,Self,,0.98,10,item/mace,16,true,,,5\n" +
            "claw,발톱,Claw,Weapon,1,1,2000,all,Damage,EnemyBack,1,0.5-1.5,,,,,0,,0,false,,,\n";

        public const string Potions =
            "Id,Name.ko-KR,Name.en-US,Effect,Magnitude,ShopWeight,Icon,Price\n" +
            "tonic,강장제,Tonic,Heal,14,5,potion/tonic,8\n";

        public const string Enemies =
            "Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure,FigureScale,Phases\n" +
            "rat,쥐,Rat,2,11,claw:200,unit/enemy/rat,100,\n" +
            "ogre,오우거,Ogre,9,54.5,claw:300+mace:150,,150,50=mace:200|25=mace:300+claw:300\n";

        public const string EnemyGroups =
            "Id,DungeonId,MinFloor,MaxFloor,IsBoss,IsElite,Enemies\n" +
            "rats,mine,1,2,false,false,rat+rat\n" +
            "ogre_guard,mine,2,2,false,true,ogre+rat\n" +
            "ogre_lair,mine,0,0,true,false,ogre+rat+rat\n";

        public const string Affinities =
            "Id,Name.ko-KR,Name.en-US,EnemyCooldownPermille\n" +
            "swift,신속,Swift,-80\n";

        public const string Dungeons =
            "Id,Name.ko-KR,Name.en-US,AffinityId,Floors,MapMinWidth,MapMaxWidth,DurationDays,StartingPotions,Background," +
            "EliteMinFloor,EliteChancePercent,CampMinFloor,CampChancePercent,CampFloor,EnemyHpPerFloorPercent,EnemyPowerPerFloorPercent,Tier1Floor,Tier2Floor,Tier3Floor,ShopMinFloor,ShopChancePercent\n" +
            "mine,광산,Mine,swift,2,2,3,2,tonic,background/dungeon/mine,2,20,0,0,0,5,10,2,0,0,2,20\n";

        public const string Mercenaries =
            "Id,Name.ko-KR,Name.en-US,JobId,Starting,Traits\n" +
            "rowan,로언,Rowan,knight,true,keen+sturdy\n" +
            "ella,엘라,Ella,bishop,true,\n" +
            "teo,테오,Teo,,false,\n" +
            "bran,브란,Bran,,false,\n";

        /// <summary>Three traits (Slice D stage 37): keen against dull, sturdy with an empty effect cell or two.</summary>
        public const string Traits =
            "Id,Name.ko-KR,Name.en-US,Kind,Polarity,Weight,Excludes,HitPercent,DodgePercent,CritPercent,DogDeathPercent,VirtuePercent,FatiguePercent,MaxHpPercent\n" +
            "dull,둔함,Dull,Innate,Negative,10,,-5,,,,,,\n" +
            "keen,예리함,Keen,Innate,Positive,10,dull,5,2,1,,,,\n" +
            "sturdy,강골,Sturdy,Innate,Positive,5,,,,,-5,3,-10,8\n";

        public const string Bags =
            "Id,Name.ko-KR,Name.en-US,Width,Height,Start,Price,ShopWeight,LootWeight\n" +
            "pack,배낭,Pack,3,2,true,0,0,0\n" +
            "pouch,주머니,Pouch,3,1,false,6,5,1\n";

        public const string FatigueStates =
            "Id,Name.ko-KR,Name.en-US,Kind,CooldownPercent,HealTakenPercent,DeathChanceDelta,Description.ko-KR,Description.en-US\n" +
            "fearful,공포,Fearful,Affliction,25,0,0,느리다,Slower\n" +
            "focused,집중,Focused,Virtue,-20,0,0,빠르다,Faster\n";

        /// <summary>No recipe in the minimal set (stage 23): a test that needs one replaces this file.</summary>
        public const string Recipes = "Id,Result,BaseItem,BaseTier,Catalyst,CatalystTier\n";

        /// <summary>Three levels (stage 27): no Id and no Name, the level is the key.</summary>
        public const string Levels =
            "Level,ExpToNext\n" +
            "1,10\n" +
            "2,20\n" +
            "3,0\n";

        /// <summary>The town's buildings (stage 30): two levels each, as TestData.Buildings.</summary>
        public const string Buildings =
            "Id,Level,Price,Days,Bonus\n" +
            "barracks,1,0,0,0\nbarracks,2,40,3,2\n" +
            "inn,1,0,0,0\ninn,2,40,2,20\n" +
            "coach,1,0,0,0\ncoach,2,40,3,1\n";

        /// <summary>Source file name -> CSV text for every definition.</summary>
        public static Dictionary<string, string> ValidSources()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { StaticDataFiles.Balance.SourceFileName, Balance },
                { StaticDataFiles.Job.SourceFileName, Jobs },
                { StaticDataFiles.Item.SourceFileName, Items },
                { StaticDataFiles.Bag.SourceFileName, Bags },
                { StaticDataFiles.Potion.SourceFileName, Potions },
                { StaticDataFiles.Enemy.SourceFileName, Enemies },
                { StaticDataFiles.EnemyGroup.SourceFileName, EnemyGroups },
                { StaticDataFiles.Affinity.SourceFileName, Affinities },
                { StaticDataFiles.Dungeon.SourceFileName, Dungeons },
                { StaticDataFiles.Mercenary.SourceFileName, Mercenaries },
                { StaticDataFiles.FatigueState.SourceFileName, FatigueStates },
                { StaticDataFiles.Recipe.SourceFileName, Recipes },
                { StaticDataFiles.Level.SourceFileName, Levels },
                { StaticDataFiles.Building.SourceFileName, Buildings },
                { StaticDataFiles.Trait.SourceFileName, Traits },
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
