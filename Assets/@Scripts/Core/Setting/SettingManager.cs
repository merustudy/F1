using System;
using F1.Data;
using F1.Save;
using UnityEngine;

namespace F1.Core
{
    /// <summary>Owns setting values. settings.json is the only settings store.</summary>
    public sealed class SettingManager
    {
        public const string FileName = "settings.json";

        readonly SaveManager _save;
        SettingsData _data;

        public SettingManager(SaveManager save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        /// <summary>Raised after a locale change is saved. The argument is the new locale code.</summary>
        public event Action<string> LocaleChanged;

        public bool IsLoaded => _data != null;

        public string LocaleCode => Require().LocaleCode;

        /// <summary>
        /// Loads settings. On first run (or when the file is unreadable) the system locale is used if it is
        /// supported, otherwise the default. A stored locale that is no longer valid is normalized and saved.
        /// </summary>
        public void Load(string systemLocaleCode)
        {
            SaveLoadResult<SettingsData> loaded = _save.Load<SettingsData>(FileName, IsReadableVersion);

            SettingsData data;
            bool mustSave;
            if (loaded.HasValue)
            {
                data = loaded.Value;
                if (LocalePolicy.TryNormalize(data.LocaleCode, out string canonical))
                {
                    mustSave = canonical != data.LocaleCode;
                    data.LocaleCode = canonical;
                }
                else
                {
                    data.LocaleCode = LocalePolicy.DefaultCode;
                    mustSave = true;
                }
            }
            else
            {
                data = new SettingsData
                {
                    LocaleCode = LocalePolicy.TryNormalize(systemLocaleCode, out string systemCode)
                        ? systemCode
                        : LocalePolicy.DefaultCode,
                };
                mustSave = true;
            }

            if (mustSave)
            {
                _save.Save(FileName, data);
            }

            _data = data;
        }

        /// <summary>Changes the locale. The change is kept only if it was saved.</summary>
        public void SetLocale(string localeCode)
        {
            SettingsData current = Require();
            if (!LocalePolicy.TryNormalize(localeCode, out string canonical))
            {
                throw new ArgumentException($"Locale '{localeCode}' is not supported.", nameof(localeCode));
            }

            if (canonical == current.LocaleCode)
            {
                return;
            }

            var next = new SettingsData
            {
                SchemaVersion = current.SchemaVersion,
                LocaleCode = canonical,
            };
            _save.Save(FileName, next);

            _data = next;
            LocaleChanged?.Invoke(canonical);
        }

        /// <summary>Maps the OS language to a supported locale code, or null when it is not supported.</summary>
        public static string SystemLocaleCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Korean: return "ko-KR";
                case SystemLanguage.English: return "en-US";
                default: return null;
            }
        }

        static bool IsReadableVersion(SettingsData data)
        {
            return data.SchemaVersion >= 1 && data.SchemaVersion <= SettingsData.CurrentSchemaVersion;
        }

        SettingsData Require()
        {
            return _data ?? throw new InvalidOperationException("Settings are not loaded.");
        }
    }
}
