using System;
using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>
    /// A game data name or description in every supported locale. It knows nothing about Unity
    /// Localization; the presentation layer passes the current locale code to <see cref="Resolve"/>.
    /// </summary>
    public sealed class LocalizedText
    {
        public const string MissingMarker = "[Missing]";

        static readonly string[] PlaceholderValues = { "TODO", "TBD", "[MISSING]" };

        readonly Dictionary<string, string> _values;

        public LocalizedText(IReadOnlyDictionary<string, string> values)
        {
            if (values == null)
            {
                throw new DataException("Localized text has no values.");
            }

            _values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string code in LocalePolicy.SupportedCodes)
            {
                if (!values.TryGetValue(code, out string text))
                {
                    throw new DataException($"Localized text is missing locale '{code}'.");
                }

                if (string.IsNullOrWhiteSpace(text) || Array.IndexOf(PlaceholderValues, text.Trim().ToUpperInvariant()) >= 0)
                {
                    throw new DataException($"Localized text for locale '{code}' is empty or a placeholder.");
                }

                _values.Add(code, text);
            }

            if (values.Count != _values.Count)
            {
                foreach (string code in values.Keys)
                {
                    if (!_values.ContainsKey(code))
                    {
                        throw new DataException($"Localized text has unsupported locale '{code}'.");
                    }
                }
            }
        }

        public IReadOnlyDictionary<string, string> Values => _values;

        public bool Has(string localeCode)
        {
            return localeCode != null && _values.ContainsKey(localeCode);
        }

        /// <summary>Current locale, then the default locale, then an explicit missing marker.</summary>
        public string Resolve(string localeCode)
        {
            if (localeCode != null && _values.TryGetValue(localeCode, out string text))
            {
                return text;
            }

            return _values.TryGetValue(LocalePolicy.DefaultCode, out text) ? text : MissingMarker;
        }
    }
}
