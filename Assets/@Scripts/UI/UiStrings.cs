using UnityEngine.Localization.Settings;

namespace F1.UI
{
    /// <summary>
    /// Text for labels built at runtime. Fixed labels in prefabs use LocalizeStringEvent instead.
    /// The result is for display only: never compare it, compute with it or save it.
    /// </summary>
    public static class UiStrings
    {
        /// <summary>Name of the single UI String Table Collection.</summary>
        public const string TableName = "F1_UI_Static";

        /// <summary>The text of a key in the current locale, with "{0}", "{1}"... replaced by the arguments.</summary>
        public static string Get(string key, params object[] arguments)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, arguments: arguments);
        }
    }
}
