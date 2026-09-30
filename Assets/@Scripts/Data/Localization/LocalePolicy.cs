using System;
using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>
    /// The single source of supported locales (BCP-47 codes). Settings validation, the language UI,
    /// the data transformer and localization checks all read this.
    /// </summary>
    public static class LocalePolicy
    {
        public const string DefaultCode = "ko-KR";

        public static readonly IReadOnlyList<string> SupportedCodes = new[] { "ko-KR", "en-US" };

        public static bool IsSupported(string code)
        {
            for (int i = 0; i < SupportedCodes.Count; i++)
            {
                if (string.Equals(SupportedCodes[i], code, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Maps a code that differs only by letter case (for example "KO-kr") to its canonical form.</summary>
        public static bool TryNormalize(string code, out string canonical)
        {
            if (!string.IsNullOrEmpty(code))
            {
                for (int i = 0; i < SupportedCodes.Count; i++)
                {
                    if (string.Equals(SupportedCodes[i], code, StringComparison.OrdinalIgnoreCase))
                    {
                        canonical = SupportedCodes[i];
                        return true;
                    }
                }
            }

            canonical = null;
            return false;
        }
    }
}
