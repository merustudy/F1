using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Data;
using F1.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace F1.Editor.Setup
{
    /// <summary>
    /// Makes the Localization settings, locales and the UI String Tables equal to LocalePolicy and
    /// UI_StaticText.csv. Idempotent; nothing here is edited in the Inspector.
    /// </summary>
    public static class LocalizationSetup
    {
        public const string DoneToken = "F1_LOCALIZATION_SYNC_DONE";
        public const string Root = "Assets/@Localization";
        public const string SourcePath = Root + "/Source/UI_StaticText.csv";
        public const string SettingsPath = Root + "/Settings/LocalizationSettings.asset";
        public const string LocalesDirectory = Root + "/Locales";
        public const string TablesDirectory = Root + "/Tables";
        public const string MissingMessage = "[Missing:{key}]";

        const string ProjectLocaleProperty = "m_ProjectLocaleIdentifier.m_Code";
        const string PreloadBehaviorProperty = "m_PreloadBehavior";

        [MenuItem("F1/Localization/Sync UI Strings")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        public static UiStringSource ReadSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new DataException($"{SourcePath}: file is missing.");
            }

            return UiStringSource.Parse(File.ReadAllText(SourcePath), Path.GetFileName(SourcePath));
        }

        public static void Sync()
        {
            UiStringSource source = ReadSource();

            LocalizationSettings settings = EnsureSettings();
            List<Locale> locales = EnsureLocales();
            ConfigureSettings(settings, locales);
            StringTableCollection collection = EnsureCollection(locales);
            ApplySource(collection, source);
        }

        static LocalizationSettings EnsureSettings()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
            {
                return settings;
            }

            settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "LocalizationSettings";
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        static List<Locale> EnsureLocales()
        {
            var locales = new List<Locale>();
            foreach (string code in LocalePolicy.SupportedCodes)
            {
                Locale locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code));
                if (locale == null)
                {
                    Directory.CreateDirectory(LocalesDirectory);
                    locale = Locale.CreateLocale(new LocaleIdentifier(code));
                    AssetDatabase.CreateAsset(locale, $"{LocalesDirectory}/Locale_{code}.asset");
                    LocalizationEditorSettings.AddLocale(locale);
                }

                locales.Add(locale);
            }

            return locales;
        }

        static void ConfigureSettings(LocalizationSettings settings, List<Locale> locales)
        {
            Locale defaultLocale = locales.Single(l => l.Identifier.Code == LocalePolicy.DefaultCode);

            // These two have no instance API. Done first so the direct changes below are not overwritten.
            var serialized = new SerializedObject(settings);
            serialized.FindProperty(ProjectLocaleProperty).stringValue = LocalePolicy.DefaultCode;
            serialized.FindProperty(PreloadBehaviorProperty).intValue = (int)PreloadBehavior.PreloadSelectedLocaleAndFallbacks;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            bool changed = false;

            // The package only picks a fixed locale at startup. Boot applies the locale from settings.json.
            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            bool selectorsOk = selectors.Count == 1
                && selectors[0] is SpecificLocaleSelector specific
                && specific.LocaleId.Code == LocalePolicy.DefaultCode;
            if (!selectorsOk)
            {
                selectors.Clear();
                selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(LocalePolicy.DefaultCode) });
                changed = true;
            }

            LocalizedStringDatabase database = settings.GetStringDatabase();
            if (database.NoTranslationFoundMessage != MissingMessage)
            {
                database.NoTranslationFoundMessage = MissingMessage;
                changed = true;
            }

            if (!database.UseFallback)
            {
                database.UseFallback = true;
                changed = true;
            }

            if (database.DefaultTable.TableCollectionName != UiStrings.TableName)
            {
                database.DefaultTable = UiStrings.TableName;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(settings);
            }

            // Every other locale falls back to the default locale.
            foreach (Locale locale in locales)
            {
                FallbackLocale fallback = locale.Metadata.GetMetadata<FallbackLocale>();
                if (locale == defaultLocale)
                {
                    if (fallback != null)
                    {
                        locale.Metadata.RemoveMetadata(fallback);
                        EditorUtility.SetDirty(locale);
                    }
                }
                else if (fallback == null)
                {
                    locale.Metadata.AddMetadata(new FallbackLocale(defaultLocale));
                    EditorUtility.SetDirty(locale);
                }
                else if (fallback.Locale != defaultLocale)
                {
                    fallback.Locale = defaultLocale;
                    EditorUtility.SetDirty(locale);
                }
            }
        }

        static StringTableCollection EnsureCollection(List<Locale> locales)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UiStrings.TableName);
            if (collection == null)
            {
                Directory.CreateDirectory(TablesDirectory);
                collection = LocalizationEditorSettings.CreateStringTableCollection(UiStrings.TableName, TablesDirectory, locales);
            }

            foreach (Locale locale in locales)
            {
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    table = (StringTable)collection.AddNewTable(locale.Identifier);
                }

                // Preloaded tables are ready right after initialization, so UI text can be read synchronously.
                if (!LocalizationEditorSettings.GetPreloadTableFlag(table))
                {
                    LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                }
            }

            return collection;
        }

        static void ApplySource(StringTableCollection collection, UiStringSource source)
        {
            SharedTableData shared = collection.SharedData;
            bool sharedChanged = false;
            var changedTables = new HashSet<StringTable>();
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (UiStringRow row in source.Rows)
            {
                keys.Add(row.Key);
                SharedTableData.SharedTableEntry sharedEntry = shared.GetEntry(row.Key);
                if (sharedEntry == null)
                {
                    sharedEntry = shared.AddKey(row.Key);
                    sharedChanged = true;
                }

                sharedChanged |= SetComment(sharedEntry, row.Comment);

                foreach (StringTable table in collection.StringTables)
                {
                    string value = row.Values[table.LocaleIdentifier.Code];
                    bool smart = UiStringSource.IsSmart(value);
                    StringTableEntry entry = table.GetEntry(sharedEntry.Id);
                    if (entry == null || entry.Value != value)
                    {
                        entry = table.AddEntry(sharedEntry.Id, value);
                        changedTables.Add(table);
                    }

                    if (entry.IsSmart != smart)
                    {
                        entry.IsSmart = smart;
                        changedTables.Add(table);
                    }
                }
            }

            foreach (SharedTableData.SharedTableEntry stale in shared.Entries.Where(e => !keys.Contains(e.Key)).ToList())
            {
                collection.RemoveEntry(stale.Id);
                sharedChanged = true;
                foreach (StringTable table in collection.StringTables)
                {
                    changedTables.Add(table);
                }
            }

            if (sharedChanged)
            {
                EditorUtility.SetDirty(shared);
            }

            foreach (StringTable table in changedTables)
            {
                EditorUtility.SetDirty(table);
            }
        }

        static bool SetComment(SharedTableData.SharedTableEntry entry, string text)
        {
            Comment comment = entry.Metadata.GetMetadata<Comment>();
            if (string.IsNullOrEmpty(text))
            {
                if (comment == null)
                {
                    return false;
                }

                entry.Metadata.RemoveMetadata(comment);
                return true;
            }

            if (comment == null)
            {
                entry.Metadata.AddMetadata(new Comment { CommentText = text });
                return true;
            }

            if (comment.CommentText == text)
            {
                return false;
            }

            comment.CommentText = text;
            return true;
        }

        /// <summary>Every difference between the CSV, LocalePolicy and the localization assets.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();

            UiStringSource source;
            try
            {
                source = ReadSource();
            }
            catch (DataException exception)
            {
                problems.Add(exception.Message);
                return problems;
            }

            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                problems.Add("There is no active LocalizationSettings. Run F1/Localization/Sync UI Strings.");
                return problems;
            }

            List<string> localeCodes = LocalizationEditorSettings.GetLocales().Select(l => l.Identifier.Code).OrderBy(c => c, StringComparer.Ordinal).ToList();
            List<string> supported = LocalePolicy.SupportedCodes.OrderBy(c => c, StringComparer.Ordinal).ToList();
            if (!localeCodes.SequenceEqual(supported))
            {
                problems.Add($"Locale assets [{string.Join(", ", localeCodes)}] do not match LocalePolicy [{string.Join(", ", supported)}].");
            }

            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            if (selectors.Count != 1 || !(selectors[0] is SpecificLocaleSelector specific) || specific.LocaleId.Code != LocalePolicy.DefaultCode)
            {
                problems.Add("Startup locale selectors must be a single SpecificLocaleSelector for the default locale.");
            }

            if (settings.GetStringDatabase().NoTranslationFoundMessage != MissingMessage)
            {
                problems.Add($"NoTranslationFoundMessage must be '{MissingMessage}'.");
            }

            var serialized = new SerializedObject(settings);
            if (serialized.FindProperty(ProjectLocaleProperty).stringValue != LocalePolicy.DefaultCode)
            {
                problems.Add($"The project locale must be '{LocalePolicy.DefaultCode}'.");
            }

            if (serialized.FindProperty(PreloadBehaviorProperty).intValue != (int)PreloadBehavior.PreloadSelectedLocaleAndFallbacks)
            {
                problems.Add("Preload behavior must be PreloadSelectedLocaleAndFallbacks.");
            }

            Locale defaultLocale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(LocalePolicy.DefaultCode));
            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                Locale fallback = locale.Metadata.GetMetadata<FallbackLocale>()?.Locale;
                if (locale != defaultLocale && fallback != defaultLocale)
                {
                    problems.Add($"Locale {locale.Identifier.Code} must fall back to {LocalePolicy.DefaultCode}.");
                }
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UiStrings.TableName);
            if (collection == null)
            {
                problems.Add($"String Table Collection '{UiStrings.TableName}' does not exist.");
                return problems;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (UiStringRow row in source.Rows)
            {
                keys.Add(row.Key);
                SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(row.Key);
                if (sharedEntry == null)
                {
                    problems.Add($"{row.Key}: not in the String Table.");
                    continue;
                }

                foreach (string code in LocalePolicy.SupportedCodes)
                {
                    var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                    StringTableEntry entry = table?.GetEntry(sharedEntry.Id);
                    if (entry == null)
                    {
                        problems.Add($"{row.Key} [{code}]: missing in the String Table.");
                    }
                    else if (entry.Value != row.Values[code])
                    {
                        problems.Add($"{row.Key} [{code}]: table has '{entry.Value}', CSV has '{row.Values[code]}'.");
                    }
                    else if (entry.IsSmart != UiStringSource.IsSmart(row.Values[code]))
                    {
                        problems.Add($"{row.Key} [{code}]: Smart flag does not match the text.");
                    }

                    if (table != null && !LocalizationEditorSettings.GetPreloadTableFlag(table))
                    {
                        problems.Add($"Table {code} is not preloaded.");
                    }
                }
            }

            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
            {
                if (!keys.Contains(entry.Key))
                {
                    problems.Add($"{entry.Key}: in the String Table but not in the CSV.");
                }
            }

            return problems.Distinct().ToList();
        }
    }
}
