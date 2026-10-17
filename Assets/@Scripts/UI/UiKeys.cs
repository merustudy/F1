namespace F1.UI
{
    /// <summary>
    /// Keys of UI_StaticText.csv. Code never writes a key as a string literal elsewhere; a test checks
    /// that this list and the CSV hold exactly the same keys.
    /// </summary>
    public static class UiKeys
    {
        public static class Common
        {
            public const string Confirm = "Common.Confirm";
            public const string Cancel = "Common.Cancel";
            public const string Continue = "Common.Continue";
            public const string Row1 = "Common.Row1";
            public const string Row2 = "Common.Row2";
            public const string Row3 = "Common.Row3";
            public const string Row4 = "Common.Row4";
            public const string None = "Common.None";
            public const string Close = "Common.Close";
        }

        public static class Title
        {
            public const string GameName = "Title.GameName";
            public const string NewRun = "Title.NewRun";
            public const string Continue = "Title.Continue";
            public const string Language = "Title.Language";
            public const string LanguageName = "Title.LanguageName";
            public const string Quit = "Title.Quit";
            public const string OverwriteWarning = "Title.OverwriteWarning";
            public const string SaveUnreadable = "Title.SaveUnreadable";
            public const string SaveRestored = "Title.SaveRestored";
            public const string SettingsNotSaved = "Title.SettingsNotSaved";
            public const string Music = "Title.Music";
            public const string Effects = "Title.Effects";
            public const string VolumeOn = "Title.VolumeOn";
            public const string VolumeLow = "Title.VolumeLow";
            public const string VolumeOff = "Title.VolumeOff";
            public const string VolumeNotSaved = "Title.VolumeNotSaved";
        }

        public static class Lobby
        {
            public const string Title = "Lobby.Title";
            public const string Day = "Lobby.Day";
            public const string DayFinalStand = "Lobby.DayFinalStand";
            public const string Gold = "Lobby.Gold";
            public const string Fatigue = "Lobby.Fatigue";
            public const string FatigueState = "Lobby.FatigueState";
            public const string Remove = "Lobby.Remove";
            public const string Dismiss = "Lobby.Dismiss";
            public const string Expedition = "Lobby.Expedition";
            public const string EmptyRow = "Lobby.EmptyRow";
            public const string Rest = "Lobby.Rest";
            public const string Depart = "Lobby.Depart";
            public const string ToFinalStand = "Lobby.ToFinalStand";
            public const string DepartOk = "Lobby.DepartOk";
            public const string PartyTooSmall = "Lobby.PartyTooSmall";
            public const string RunOver = "Lobby.RunOver";
            public const string FinalStandDue = "Lobby.FinalStandDue";
            public const string Victory = "Lobby.Victory";
            public const string DefeatDeathLimit = "Lobby.DefeatDeathLimit";
            public const string DefeatFinalStand = "Lobby.DefeatFinalStand";
            public const string ToTitle = "Lobby.ToTitle";

            // The town (Round 71): the buildings' plates and bubbles.
            public const string Barracks = "Lobby.Barracks";
            public const string Inn = "Lobby.Inn";
            public const string Coach = "Lobby.Coach";
            public const string Gate = "Lobby.Gate";
            public const string FinalStand = "Lobby.FinalStand";
            public const string FinalStandSub = "Lobby.FinalStandSub";
            public const string InnClosed = "Lobby.InnClosed";
            public const string BarracksHint = "Lobby.BarracksHint";
            public const string InnHint = "Lobby.InnHint";
            public const string CoachHint = "Lobby.CoachHint";
            public const string DungeonLocked = "Lobby.DungeonLocked";

            // The quarters' windows (stage 26).
            public const string BarracksTitle = "Lobby.BarracksTitle";
            public const string InnTitle = "Lobby.InnTitle";
            public const string CoachTitle = "Lobby.CoachTitle";
            public const string RosterCount = "Lobby.RosterCount";
            public const string Deaths = "Lobby.Deaths";
            public const string ClearedOf = "Lobby.ClearedOf";
            public const string LockedOf = "Lobby.LockedOf";
            public const string Hire = "Lobby.Hire";
            public const string NoCandidates = "Lobby.NoCandidates";
            public const string CoachNote = "Lobby.CoachNote";
            public const string Treat = "Lobby.Treat";
            public const string InnNote = "Lobby.InnNote";

            // Levels (stage 27).
            public const string Level = "Lobby.Level";
            public const string Exp = "Lobby.Exp";
            public const string ExpMax = "Lobby.ExpMax";

            // The result window (Round 72).
            public const string EndedOn = "Lobby.EndedOn";
            public const string DayValue = "Lobby.DayValue";
            public const string Expeditions = "Lobby.Expeditions";
            public const string Times = "Lobby.Times";
            public const string ExpeditionsValue = "Lobby.ExpeditionsValue";
            public const string ExpeditionsOf = "Lobby.ExpeditionsOf";
            public const string FallenLabel = "Lobby.FallenLabel";
            public const string FallenValue = "Lobby.FallenValue";
            public const string Survivors = "Lobby.Survivors";

            // The town's buildings, their levels and their construction (stage 30, Round 78).
            public const string BuildingPlate = "Lobby.BuildingPlate";
            public const string Construction = "Lobby.Construction";
            public const string UpgradeHint = "Lobby.UpgradeHint";
            public const string UpgradeLine = "Lobby.UpgradeLine";
            public const string UpgradeButton = "Lobby.UpgradeButton";
            public const string MaxLevelLine = "Lobby.MaxLevelLine";
            public const string ConstructionLine = "Lobby.ConstructionLine";
            public const string LockedNote = "Lobby.LockedNote";
            public const string EffectBarracks = "Lobby.EffectBarracks";
            public const string EffectInn = "Lobby.EffectInn";
            public const string EffectCoach = "Lobby.EffectCoach";
            public const string EffectGate = "Lobby.EffectGate";
            public const string GateTitle = "Lobby.GateTitle";
            public const string GateNote = "Lobby.GateNote";
            public const string GateLocked = "Lobby.GateLocked";
        }

        /// <summary>The expedition screen (Round 71 E2): the map, the chosen dungeon and the departure.</summary>
        public static class Expedition
        {
            public const string Title = "Expedition.Title";
            public const string ToLobby = "Expedition.ToLobby";
            public const string Home = "Expedition.Home";
            public const string MarkerOpen = "Expedition.MarkerOpen";
            public const string MarkerLocked = "Expedition.MarkerLocked";
            public const string Floors = "Expedition.Floors";
            public const string Days = "Expedition.Days";
            public const string Potions = "Expedition.Potions";
            public const string UnlockHint = "Expedition.UnlockHint";
            public const string FinalHint = "Expedition.FinalHint";
            public const string Depart = "Expedition.Depart";
            public const string Locked = "Expedition.Locked";
            public const string FinalName = "Expedition.FinalName";
            public const string AffinityLabel = "Expedition.AffinityLabel";
            public const string FloorsLabel = "Expedition.FloorsLabel";
            public const string DaysLabel = "Expedition.DaysLabel";
            public const string PotionsLabel = "Expedition.PotionsLabel";
            public const string ClearedLabel = "Expedition.ClearedLabel";
            public const string ClearedCount = "Expedition.ClearedCount";
            public const string FinalStandHint = "Expedition.FinalStandHint";
            public const string RewardLabel = "Expedition.RewardLabel";
            public const string Reward = "Expedition.Reward";

            // The party popup (Round 75 B, 2026-10-17; the stage and the list, Round 76 D).
            public const string PartyTitle = "Expedition.PartyTitle";
            public const string PartyDungeon = "Expedition.PartyDungeon";
            public const string RosterHeader = "Expedition.RosterHeader";
            public const string EmptyRowHint = "Expedition.EmptyRowHint";
            public const string Forward = "Expedition.Forward";
            public const string Back = "Expedition.Back";
            public const string Join = "Expedition.Join";
            public const string Leave = "Expedition.Leave";
            public const string EmptySlot = "Expedition.EmptySlot";

            // Under the details, in the strip's stead (Round 77, 2026-10-17).
            public const string Roster = "Expedition.Roster";
            public const string PartyCount = "Expedition.PartyCount";
        }

        public static class Map
        {
            public const string Progress = "Map.Progress";
            public const string Battle = "Map.Battle";
            public const string Boss = "Map.Boss";
            public const string SelectNode = "Map.SelectNode";
            public const string NodeTitle = "Map.NodeTitle";
            public const string Unknown = "Map.Unknown";
            public const string Enter = "Map.Enter";
            public const string Elite = "Map.Elite";
            public const string Camp = "Map.Camp";
            public const string EliteHint = "Map.EliteHint";
            public const string CampHint = "Map.CampHint";
            public const string EnterCamp = "Map.EnterCamp";
            public const string AtCamp = "Map.AtCamp";
            public const string CampChoose = "Map.CampChoose";
            public const string Rest = "Map.Rest";
            public const string RestHeal = "Map.RestHeal";
            public const string RestFatigue = "Map.RestFatigue";
            public const string Mend = "Map.Mend";
            public const string MendBody = "Map.MendBody";
            public const string MendTitle = "Map.MendTitle";
            public const string MendHint = "Map.MendHint";
            public const string MendBack = "Map.MendBack";
            public const string MendConfirm = "Map.MendConfirm";
            public const string Forge = "Map.Forge";
            public const string ForgeBody = "Map.ForgeBody";
            public const string ForgeTitle = "Map.ForgeTitle";
            public const string ForgeHint = "Map.ForgeHint";
            public const string ForgeChoose = "Map.ForgeChoose";
            public const string ForgeConfirm = "Map.ForgeConfirm";
            public const string Shop = "Map.Shop";
            public const string ShopHint = "Map.ShopHint";
            public const string EnterShop = "Map.EnterShop";
            public const string AtShop = "Map.AtShop";
            public const string ShopChoose = "Map.ShopChoose";
            public const string ShopBuyHint = "Map.ShopBuyHint";
            public const string ShopCoinsLabel = "Map.ShopCoinsLabel";
            public const string ShopRefresh = "Map.ShopRefresh";
            public const string ShopLeave = "Map.ShopLeave";
            public const string ShopBought = "Map.ShopBought";
            public const string Coins = "Map.Coins";
            public const string ShopPotion = "Map.ShopPotion";

            /// <summary>Under the name of a bag on offer or of a bag dropped (Slice B stage 19). {0} x {1} = its squares, {2} = how many.</summary>
            public const string ShopBagSub = "Map.ShopBagSub";
            public const string Sell = "Map.Sell";
            public const string CannotSell = "Map.CannotSell";
            public const string ShopSold = "Map.ShopSold";
            public const string BagFacts = "Map.BagFacts";
            public const string OfferPrice = "Map.OfferPrice";
        }

        public static class Board
        {
            public const string Hp = "Board.Hp";
            public const string Forward = "Board.Forward";
            public const string Back = "Board.Back";
            public const string Hint = "Board.Hint";
            public const string InventoryShow = "Board.InventoryShow";
            public const string InventoryHide = "Board.InventoryHide";
            public const string InventoryHeading = "Board.InventoryHeading";
            public const string InventoryTitle = "Board.InventoryTitle";
            public const string InventoryHint = "Board.InventoryHint";
            public const string TurnHint = "Board.TurnHint";
            public const string ToInventory = "Board.ToInventory";
            public const string FatigueTag = "Board.FatigueTag";
            public const string MergeInto = "Board.MergeInto";
            public const string FatigueTotal = "Board.FatigueTotal";
            public const string JobState = "Board.JobState";
            public const string StateDetail = "Board.StateDetail";

            /// <summary>The words over a held thing's ghost (Slice B stage 19): the item it lies over goes to the inventory; a bag is laid.</summary>
            public const string GhostToInventory = "Board.GhostToInventory";
            public const string GhostBag = "Board.GhostBag";
        }

        public static class Item
        {
            public const string TooltipTitle = "Item.TooltipTitle";
            public const string Weapon = "Item.Weapon";
            public const string Armor = "Item.Armor";
            public const string Attack = "Item.Attack";
            public const string Support = "Item.Support";
            public const string Other = "Item.Other";
            public const string Size = "Item.Size";
            public const string Cooldown = "Item.Cooldown";
            public const string RowsFront = "Item.RowsFront";
            public const string RowsFrontOne = "Item.RowsFrontOne";
            public const string RowsBack = "Item.RowsBack";
            public const string RowsBackOne = "Item.RowsBackOne";
            public const string FatigueCost = "Item.FatigueCost";
            public const string BaseWeapon = "Item.BaseWeapon";
            public const string Common = "Item.Common";
            public const string Tier1 = "Item.Tier1";
            public const string Tier2 = "Item.Tier2";
            public const string Tier3 = "Item.Tier3";
            public const string MergeHint = "Item.MergeHint";
            public const string MergeHintPlain = "Item.MergeHintPlain";
            public const string Change = "Item.Change";
            public const string Melee = "Item.Melee";
            public const string NoActivation = "Item.NoActivation";
            public const string StarDamage = "Item.StarDamage";
            public const string Crit = "Item.Crit";
            public const string StarBonus = "Item.StarBonus";
            public const string StarLit = "Item.StarLit";
            public const string StarLitNone = "Item.StarLitNone";
            public const string Unique = "Item.Unique";
            public const string UniqueLine = "Item.UniqueLine";
            public const string UniqueTitle = "Item.UniqueTitle";
            public const string Recipe = "Item.Recipe";
        }

        public static class Effect
        {
            public const string Damage = "Effect.Damage";
            public const string Heal = "Effect.Heal";
            public const string Shield = "Effect.Shield";
            public const string Burn = "Effect.Burn";
        }

        public static class Target
        {
            public const string EnemyFront = "Target.EnemyFront";
            public const string EnemyFrontMany = "Target.EnemyFrontMany";
            public const string EnemyBack = "Target.EnemyBack";
            public const string EnemyBackMany = "Target.EnemyBackMany";
            public const string EnemyAll = "Target.EnemyAll";
            public const string Self = "Target.Self";
            public const string AllyLowestHp = "Target.AllyLowestHp";
            public const string AllyAll = "Target.AllyAll";
        }

        public static class Potion
        {
            public const string Heal = "Potion.Heal";
            public const string Shield = "Potion.Shield";
        }

        public static class Battle
        {
            public const string Time = "Battle.Time";
            public const string StormIn = "Battle.StormIn";
            public const string StormActive = "Battle.StormActive";
            public const string Title = "Battle.Title";
            public const string Pause = "Battle.Pause";
            public const string Speed = "Battle.Speed";
            public const string Hp = "Battle.Hp";
            public const string Shield = "Battle.Shield";
            public const string Burn = "Battle.Burn";
            public const string DogGrace = "Battle.DogGrace";
            public const string DogRolling = "Battle.DogRolling";
            public const string PartyFallen = "Battle.PartyFallen";
            public const string EnemyFallen = "Battle.EnemyFallen";
            public const string PotionArmed = "Battle.PotionArmed";
            public const string Retreat = "Battle.Retreat";
            public const string RetreatWait = "Battle.RetreatWait";
            public const string Victory = "Battle.Victory";
            public const string Defeat = "Battle.Defeat";
            public const string Retreated = "Battle.Retreated";
            public const string NoDeaths = "Battle.NoDeaths";
            public const string Coins = "Battle.Coins";
            public const string LootCount = "Battle.LootCount";
            public const string LootHint = "Battle.LootHint";
            public const string LootPickedHint = "Battle.LootPickedHint";
            public const string BagTitle = "Battle.BagTitle";
            public const string ShowLog = "Battle.ShowLog";
            public const string LogTitle = "Battle.LogTitle";
            public const string FigurePlaceholder = "Battle.FigurePlaceholder";
        }

        /// <summary>The texts that rise from a unit or the clock when something happens in battle.</summary>
        public static class Fx
        {
            public const string Damage = "Fx.Damage";
            public const string Absorbed = "Fx.Absorbed";
            public const string Heal = "Fx.Heal";
            public const string Shield = "Fx.Shield";
            public const string Burn = "Fx.Burn";
            public const string DogEntered = "Fx.DogEntered";
            public const string Survived = "Fx.Survived";
            public const string Died = "Fx.Died";
            public const string RetreatSucceeded = "Fx.RetreatSucceeded";
            public const string RetreatFailed = "Fx.RetreatFailed";
            public const string Storm = "Fx.Storm";
            public const string Crit = "Fx.Crit";
            public const string Phase = "Fx.Phase";
            public const string CritDamage = "Fx.CritDamage";
            public const string Collapsed = "Fx.Collapsed";
        }

        public static class Log
        {
            public const string Line = "Log.Line";
            public const string SourceItem = "Log.SourceItem";
            public const string SourcePassive = "Log.SourcePassive";
            public const string SourceBurn = "Log.SourceBurn";
            public const string SourceStorm = "Log.SourceStorm";
            public const string Damage = "Log.Damage";
            public const string DamageAbsorbed = "Log.DamageAbsorbed";
            public const string CriticalHit = "Log.CriticalHit";
            public const string Heal = "Log.Heal";
            public const string Shield = "Log.Shield";
            public const string Burn = "Log.Burn";
            public const string DogEntered = "Log.DogEntered";
            public const string DogExited = "Log.DogExited";
            public const string GraceBroken = "Log.GraceBroken";
            public const string DeathSurvived = "Log.DeathSurvived";
            public const string DeathFailed = "Log.DeathFailed";
            public const string Died = "Log.Died";
            public const string PartyAdvanced = "Log.PartyAdvanced";
            public const string EnemyAdvanced = "Log.EnemyAdvanced";
            public const string PotionUsed = "Log.PotionUsed";
            public const string RetreatFailed = "Log.RetreatFailed";
            public const string RetreatSucceeded = "Log.RetreatSucceeded";
            public const string Storm = "Log.Storm";
            public const string BrokeDown = "Log.BrokeDown";
            public const string Virtue = "Log.Virtue";
            public const string Collapsed = "Log.Collapsed";
            public const string FatigueStateEnded = "Log.FatigueStateEnded";
            public const string PhaseChanged = "Log.PhaseChanged";
            public const string FatigueDog = "Log.FatigueDog";
            public const string FatigueCritTaken = "Log.FatigueCritTaken";
            public const string FatigueAllyDog = "Log.FatigueAllyDog";
            public const string FatigueAllyDeath = "Log.FatigueAllyDeath";
            public const string FatigueVirtue = "Log.FatigueVirtue";
        }

        public static class Death
        {
            public const string AfterGrace = "Death.AfterGrace";
            public const string GraceBroken = "Death.GraceBroken";
            public const string Collapsed = "Death.Collapsed";
        }

        /// <summary>The loot of a won battle (Slice B stage 18), picked on the battle screen (round 47).</summary>
        public static class Loot
        {
            public const string Title = "Loot.Title";
            public const string Selected = "Loot.Selected";
            public const string ToInventory = "Loot.ToInventory";
        }

        public static class Settle
        {
            public const string Cleared = "Settle.Cleared";
            public const string Wiped = "Settle.Wiped";
            public const string Retreated = "Settle.Retreated";
            public const string Survivors = "Settle.Survivors";
            public const string Fallen = "Settle.Fallen";
            public const string Fatigue = "Settle.Fatigue";
            public const string SurvivorState = "Settle.SurvivorState";
            public const string Days = "Settle.Days";
            public const string ItemsLost = "Settle.ItemsLost";
            public const string RunOver = "Settle.RunOver";
            public const string Victory = "Settle.Victory";
            public const string DeathLimit = "Settle.DeathLimit";
            public const string FinalStandLost = "Settle.FinalStandLost";
            public const string Gold = "Settle.Gold";
            public const string GoldWithCoins = "Settle.GoldWithCoins";
            public const string Exp = "Settle.Exp";
            public const string LevelUps = "Settle.LevelUps";
            public const string LevelUp = "Settle.LevelUp";
        }

        public static class Save
        {
            public const string Failed = "Save.Failed";
            public const string Retry = "Save.Retry";
        }
    }
}
