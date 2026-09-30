using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Data;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace F1.Core
{
    /// <summary>
    /// The only code that touches the Unity Localization API for locale selection. Not a manager:
    /// SettingManager owns the locale code and calls this to make the runtime follow it.
    /// </summary>
    public sealed class UnityLocaleAdapter
    {
        /// <summary>Initializes Unity Localization and checks that its locales are exactly the supported ones.</summary>
        public async Task InitializeAsync()
        {
            await WaitForInitializationAsync();

            var available = new List<string>();
            foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
            {
                available.Add(locale.Identifier.Code);
            }

            available.Sort(StringComparer.Ordinal);
            var supported = new List<string>(LocalePolicy.SupportedCodes);
            supported.Sort(StringComparer.Ordinal);
            if (string.Join(",", available) != string.Join(",", supported))
            {
                throw new InvalidOperationException(
                    $"Locale assets [{string.Join(", ", available)}] do not match LocalePolicy [{string.Join(", ", supported)}].");
            }
        }

        /// <summary>Selects the locale and waits until its preloaded tables are ready.</summary>
        public async Task ApplyAsync(string localeCode)
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
            {
                throw new InvalidOperationException($"Locale '{localeCode}' has no locale asset.");
            }

            if (LocalizationSettings.SelectedLocale != locale)
            {
                LocalizationSettings.SelectedLocale = locale;
            }

            await WaitForInitializationAsync();
        }

        public string CurrentCode => LocalizationSettings.SelectedLocale.Identifier.Code;

        static async Task WaitForInitializationAsync()
        {
            AsyncOperationHandle<LocalizationSettings> handle = LocalizationSettings.InitializationOperation;
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Unity Localization failed to initialize.", handle.OperationException);
            }
        }
    }
}
