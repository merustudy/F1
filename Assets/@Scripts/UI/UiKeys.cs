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
            public const string Front = "Common.Front";
            public const string Rear = "Common.Rear";
        }

        public static class Title
        {
            public const string GameName = "Title.GameName";
            public const string NewRun = "Title.NewRun";
            public const string Continue = "Title.Continue";
            public const string Language = "Title.Language";
            public const string LanguageName = "Title.LanguageName";
        }
    }
}
