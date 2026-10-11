using System.Globalization;

namespace F1.Data
{
    /// <summary>
    /// Hundredths (Slice B stage 22, Docs/Design/02_Combat_System.md §13): HP, damage, heal, shield and the storm are counted in 0.01s
    /// as integers, so 2.37 is 237. Float is never used for them: the game and the sim must agree to the last digit on every machine.
    /// The screen shows whole numbers (<see cref="Round"/>).
    /// </summary>
    public static class Centi
    {
        /// <summary>1.00.</summary>
        public const int One = 100;

        /// <summary>"2", "2.5", "2.37" become 200, 250, 237. At most two decimals, no sign, no exponent.</summary>
        public static bool TryParse(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int dot = text.IndexOf('.');
            string whole = dot < 0 ? text : text.Substring(0, dot);
            string fraction = dot < 0 ? "" : text.Substring(dot + 1);
            if (whole.Length == 0 || fraction.Length > 2 || (dot >= 0 && fraction.Length == 0))
            {
                return false;
            }

            foreach (char c in whole + fraction)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            if (!int.TryParse(whole, NumberStyles.None, CultureInfo.InvariantCulture, out int units) || units > int.MaxValue / One - 1)
            {
                return false;
            }

            int hundredths = fraction.Length == 0 ? 0 : int.Parse(fraction.PadRight(2, '0'), CultureInfo.InvariantCulture);
            value = units * One + hundredths;
            return true;
        }

        /// <summary>A whole number for the screen: rounded half up (2.49 is 2, 2.50 is 3). Negative values round the same way from zero.</summary>
        public static int Round(int hundredths)
        {
            return hundredths >= 0 ? (hundredths + One / 2) / One : -((-hundredths + One / 2) / One);
        }

        /// <summary>The value as data writes it: "2", "2.5", "2.37".</summary>
        public static string ToText(int hundredths)
        {
            int units = hundredths / One;
            int rest = hundredths % One;
            if (rest == 0)
            {
                return units.ToString(CultureInfo.InvariantCulture);
            }

            return rest % 10 == 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}.{1}", units, rest / 10)
                : string.Format(CultureInfo.InvariantCulture, "{0}.{1:00}", units, rest);
        }
    }
}
