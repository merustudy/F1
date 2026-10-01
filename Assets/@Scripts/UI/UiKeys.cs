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
        }

        public static class Lobby
        {
            public const string Title = "Lobby.Title";
            public const string Day = "Lobby.Day";
            public const string Roster = "Lobby.Roster";
            public const string Fatigue = "Lobby.Fatigue";
            public const string Remove = "Lobby.Remove";
            public const string Expedition = "Lobby.Expedition";
            public const string Affinity = "Lobby.Affinity";
            public const string Cost = "Lobby.Cost";
            public const string Cleared = "Lobby.Cleared";
            public const string Party = "Lobby.Party";
            public const string EmptyRow = "Lobby.EmptyRow";
            public const string Rest = "Lobby.Rest";
            public const string Depart = "Lobby.Depart";
            public const string DepartOk = "Lobby.DepartOk";
            public const string PartyTooSmall = "Lobby.PartyTooSmall";
            public const string NotEnoughFatigue = "Lobby.NotEnoughFatigue";
            public const string Fallen = "Lobby.Fallen";
            public const string RunOver = "Lobby.RunOver";
            public const string RunOverDay = "Lobby.RunOverDay";
            public const string ToTitle = "Lobby.ToTitle";
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
        }

        public static class Board
        {
            public const string Hp = "Board.Hp";
            public const string EmptySlot = "Board.EmptySlot";
            public const string Grade = "Board.Grade";
            public const string Forward = "Board.Forward";
            public const string Back = "Board.Back";
            public const string Hint = "Board.Hint";
            public const string InventoryShow = "Board.InventoryShow";
            public const string InventoryHide = "Board.InventoryHide";
            public const string InventoryTitle = "Board.InventoryTitle";
            public const string InventoryHint = "Board.InventoryHint";
            public const string InventoryEmpty = "Board.InventoryEmpty";
            public const string ToInventory = "Board.ToInventory";
        }

        public static class Item
        {
            public const string Title = "Item.Title";
            public const string Weapon = "Item.Weapon";
            public const string Support = "Item.Support";
            public const string Size = "Item.Size";
            public const string Cooldown = "Item.Cooldown";
            public const string RowsFront = "Item.RowsFront";
            public const string RowsFrontOne = "Item.RowsFrontOne";
            public const string RowsBack = "Item.RowsBack";
            public const string RowsBackOne = "Item.RowsBackOne";
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
            public const string Pause = "Battle.Pause";
            public const string Speed = "Battle.Speed";
            public const string Hp = "Battle.Hp";
            public const string Shield = "Battle.Shield";
            public const string Burn = "Battle.Burn";
            public const string DogGrace = "Battle.DogGrace";
            public const string DogRolling = "Battle.DogRolling";
            public const string PartyFallen = "Battle.PartyFallen";
            public const string EnemyFallen = "Battle.EnemyFallen";
            public const string PotionHint = "Battle.PotionHint";
            public const string PotionArmed = "Battle.PotionArmed";
            public const string PotionWait = "Battle.PotionWait";
            public const string Retreat = "Battle.Retreat";
            public const string RetreatWait = "Battle.RetreatWait";
            public const string Victory = "Battle.Victory";
            public const string Defeat = "Battle.Defeat";
            public const string Retreated = "Battle.Retreated";
            public const string NoDeaths = "Battle.NoDeaths";
            public const string ShowLog = "Battle.ShowLog";
            public const string LogTitle = "Battle.LogTitle";
            public const string FigurePlaceholder = "Battle.FigurePlaceholder";
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
        }

        public static class Death
        {
            public const string AfterGrace = "Death.AfterGrace";
            public const string GraceBroken = "Death.GraceBroken";
        }

        public static class Reward
        {
            public const string Title = "Reward.Title";
            public const string Hint = "Reward.Hint";
            public const string Item = "Reward.Item";
            public const string Potion = "Reward.Potion";
            public const string Select = "Reward.Select";
            public const string Selected = "Reward.Selected";
            public const string TakePotion = "Reward.TakePotion";
            public const string PotionFull = "Reward.PotionFull";
            public const string Skip = "Reward.Skip";
            public const string ToInventory = "Reward.ToInventory";
        }

        public static class Settle
        {
            public const string Cleared = "Settle.Cleared";
            public const string Wiped = "Settle.Wiped";
            public const string Retreated = "Settle.Retreated";
            public const string Survivors = "Settle.Survivors";
            public const string Fallen = "Settle.Fallen";
            public const string Fatigue = "Settle.Fatigue";
            public const string Days = "Settle.Days";
            public const string ItemsLost = "Settle.ItemsLost";
            public const string RunOver = "Settle.RunOver";
        }

        public static class Save
        {
            public const string Failed = "Save.Failed";
            public const string Retry = "Save.Retry";
        }
    }
}
