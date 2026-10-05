using System;
using System.Threading.Tasks;
using F1.Data;
using F1.Save;
using UnityEngine;

namespace F1.Core
{
    /// <summary>Owns setting values: the locale and the music and effect volumes. settings.json is the only settings store.</summary>
    public sealed class SettingManager
    {
        public const string FileName = "settings.json";

        readonly SaveManager _save;
        readonly Func<string, Task> _applyRuntimeLocale;
        SettingsData _data;

        /// <param name="applyRuntimeLocale">
        /// Makes the running app use a locale code (UnityLocaleAdapter.ApplyAsync). Null when there is
        /// no runtime to follow, as in tests.
        /// </param>
        public SettingManager(SaveManager save, Func<string, Task> applyRuntimeLocale = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _applyRuntimeLocale = applyRuntimeLocale;
        }

        /// <summary>Raised after a locale change is saved. The argument is the new locale code.</summary>
        public event Action<string> LocaleChanged;

        /// <summary>Raised after a volume change is saved.</summary>
        public event Action VolumeChanged;

        public bool IsLoaded => _data != null;

        public string LocaleCode => Require().LocaleCode;

        /// <summary>The music's volume, 0..100.</summary>
        public int MusicVolume => Require().MusicVolume;

        /// <summary>The effects' volume, 0..100.</summary>
        public int EffectVolume => Require().EffectVolume;

        /// <summary>
        /// Loads settings. On first run (or when the file is unreadable) the system locale is used if it is
        /// supported, otherwise the default, and both volumes are full. A stored locale that is no longer valid
        /// is normalized, a volume out of 0..100 is brought to the nearer end, and an older file is brought to the
        /// current version (version 1 had no volumes: they read as full); any of these is saved.
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

                int music = VolumeLevels.Clamp(data.MusicVolume);
                int effects = VolumeLevels.Clamp(data.EffectVolume);
                mustSave |= music != data.MusicVolume || effects != data.EffectVolume || data.SchemaVersion != SettingsData.CurrentSchemaVersion;
                data.MusicVolume = music;
                data.EffectVolume = effects;
                data.SchemaVersion = SettingsData.CurrentSchemaVersion;
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

        /// <summary>
        /// Changes the locale: the runtime locale is applied, then the setting is saved, then
        /// LocaleChanged is raised. If applying or saving fails the runtime locale is put back and
        /// nothing changes.
        /// </summary>
        public async Task ChangeLocaleAsync(string localeCode)
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

            SettingsData next = Copy(current);
            next.LocaleCode = canonical;
            try
            {
                await ApplyRuntimeLocale(canonical);
                _save.Save(FileName, next);
            }
            catch
            {
                // The saved locale is still the old one; the running app must agree with it.
                await ApplyRuntimeLocale(current.LocaleCode);
                throw;
            }

            _data = next;
            LocaleChanged?.Invoke(canonical);
        }

        /// <summary>Changes the music's volume: it is saved, then VolumeChanged is raised. If saving fails nothing changes.</summary>
        public void ChangeMusicVolume(int volume)
        {
            SettingsData next = Copy(Require());
            next.MusicVolume = CheckVolume(volume);
            Commit(next);
        }

        /// <summary>Changes the effects' volume: it is saved, then VolumeChanged is raised. If saving fails nothing changes.</summary>
        public void ChangeEffectVolume(int volume)
        {
            SettingsData next = Copy(Require());
            next.EffectVolume = CheckVolume(volume);
            Commit(next);
        }

        void Commit(SettingsData next)
        {
            SettingsData current = Require();
            if (next.MusicVolume == current.MusicVolume && next.EffectVolume == current.EffectVolume)
            {
                return;
            }

            _save.Save(FileName, next);
            _data = next;
            VolumeChanged?.Invoke();
        }

        static int CheckVolume(int volume)
        {
            if (volume < VolumeLevels.Off || volume > VolumeLevels.Full)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), volume, $"A volume is {VolumeLevels.Off}..{VolumeLevels.Full}.");
            }

            return volume;
        }

        static SettingsData Copy(SettingsData data)
        {
            return new SettingsData
            {
                SchemaVersion = data.SchemaVersion,
                LocaleCode = data.LocaleCode,
                MusicVolume = data.MusicVolume,
                EffectVolume = data.EffectVolume,
            };
        }

        Task ApplyRuntimeLocale(string localeCode)
        {
            return _applyRuntimeLocale != null ? _applyRuntimeLocale(localeCode) : Task.CompletedTask;
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
