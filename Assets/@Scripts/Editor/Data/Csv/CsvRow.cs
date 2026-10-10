using System;
using System.Collections.Generic;
using System.Globalization;
using F1.Data;

namespace F1.Editor.Data
{
    /// <summary>
    /// One data row with strict typed accessors. Nothing is converted silently: a missing value,
    /// surrounding whitespace, an undefined enum name or an out-of-range number is an error that
    /// names the file, the row and the header.
    /// </summary>
    public sealed class CsvRow
    {
        public const char ListSeparator = '+';

        readonly CsvTable _table;
        readonly List<string> _fields;

        internal CsvRow(CsvTable table, int line, List<string> fields)
        {
            _table = table;
            Line = line;
            _fields = fields;
        }

        /// <summary>1-based line number of the row in the source file.</summary>
        public int Line { get; }

        public string Where(string header)
        {
            return $"{_table.Source}({Line}) [{header}]";
        }

        /// <summary>
        /// Adds the file and row to an error raised while building a definition from this row.
        /// Messages from the accessors of this class already carry them.
        /// </summary>
        public string Contextualize(string message)
        {
            string prefix = $"{_table.Source}({Line})";
            return message.StartsWith(prefix, StringComparison.Ordinal) ? message : $"{prefix}: {message}";
        }

        public bool IsEmpty(string header)
        {
            return Raw(header).Length == 0;
        }

        /// <summary>Required text. Empty values and surrounding whitespace are errors.</summary>
        public string Text(string header)
        {
            string value = Raw(header);
            if (value.Length == 0)
            {
                throw new DataException($"{Where(header)}: value is required.");
            }

            if (value != value.Trim())
            {
                throw new DataException($"{Where(header)}: value has surrounding whitespace.");
            }

            return value;
        }

        public string Id(string header)
        {
            string value = Text(header);
            if (!DataId.IsValid(value))
            {
                throw new DataException($"{Where(header)}: '{value}' is not a valid id (English snake_case).");
            }

            return value;
        }

        /// <summary>A list of ids joined with '+'. An empty cell is an empty list.</summary>
        public List<string> IdList(string header)
        {
            var ids = new List<string>();
            if (IsEmpty(header))
            {
                return ids;
            }

            foreach (string part in Text(header).Split(ListSeparator))
            {
                if (!DataId.IsValid(part))
                {
                    throw new DataException($"{Where(header)}: '{part}' is not a valid id (English snake_case).");
                }

                ids.Add(part);
            }

            return ids;
        }

        /// <summary>A span of a line: "front:N", "back:N" or "all".</summary>
        public RowSpan Rows(string header)
        {
            string value = Text(header);
            if (!RowSpan.TryParse(value, out RowSpan span))
            {
                throw new DataException(
                    $"{Where(header)}: '{value}' is not 'front:N', 'back:N' or '{RowSpan.Everywhere}' (N is 1..{BattleRows.Count}).");
            }

            return span;
        }

        /// <summary>A span of a line, or null for an empty cell.</summary>
        public RowSpan OptionalRows(string header)
        {
            return IsEmpty(header) ? null : Rows(header);
        }

        /// <summary>A list of "id:grade" pairs joined with '+', for example "claws:5+bite:4".</summary>
        public List<ItemGrant> GrantList(string header)
        {
            var grants = new List<ItemGrant>();
            foreach (string part in Text(header).Split(ListSeparator))
            {
                string[] pair = part.Split(':');
                if (pair.Length != 2 || !DataId.IsValid(pair[0]) || !TryParseInt(pair[1], out int grade) || grade < 1)
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'item_id:grade'.");
                }

                grants.Add(new ItemGrant(pair[0], grade));
            }

            return grants;
        }

        /// <summary>An item's star squares (stage 20): "x:y" pairs joined with '+', for example "0:-1+0:1"; none for an empty cell.</summary>
        public List<StarSquare> StarSquares(string header)
        {
            var squares = new List<StarSquare>();
            if (IsEmpty(header))
            {
                return squares;
            }

            foreach (string part in Text(header).Split(ListSeparator))
            {
                string[] pair = part.Split(':');
                if (pair.Length != 2 || !TryParseInt(pair[0], out int x) || !TryParseInt(pair[1], out int y))
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'x:y'.");
                }

                squares.Add(new StarSquare(x, y));
            }

            return squares;
        }

        /// <summary>A PascalCase constant name, for example "DogGraceMs".</summary>
        public string Key(string header)
        {
            string value = Text(header);
            bool valid = char.IsUpper(value[0]);
            foreach (char c in value)
            {
                valid &= (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
            }

            if (!valid)
            {
                throw new DataException($"{Where(header)}: '{value}' is not a PascalCase key.");
            }

            return value;
        }

        public int Int(string header, int min = int.MinValue, int max = int.MaxValue)
        {
            string value = Text(header);
            if (!TryParseInt(value, out int number))
            {
                throw new DataException($"{Where(header)}: '{value}' is not an integer.");
            }

            if (number < min || number > max)
            {
                throw new DataException($"{Where(header)}: {number} is outside {min}..{max}.");
            }

            return number;
        }

        public int OptionalInt(string header, int defaultValue, int min = int.MinValue, int max = int.MaxValue)
        {
            return IsEmpty(header) ? defaultValue : Int(header, min, max);
        }

        public bool Bool(string header)
        {
            string value = Text(header);
            switch (value)
            {
                case "true": return true;
                case "false": return false;
                default: throw new DataException($"{Where(header)}: '{value}' is not 'true' or 'false'.");
            }
        }

        /// <summary>Only names defined by the enum are accepted; numbers are not.</summary>
        public TEnum Enum<TEnum>(string header)
            where TEnum : struct
        {
            string value = Text(header);
            // Enum.TryParse also accepts numbers and comma-separated lists; only a single defined name is allowed here.
            if (System.Enum.IsDefined(typeof(TEnum), value) && System.Enum.TryParse(value, false, out TEnum parsed))
            {
                return parsed;
            }

            throw new DataException($"{Where(header)}: '{value}' is not a {typeof(TEnum).Name}.");
        }

        /// <summary>Reads "&lt;field&gt;.&lt;locale&gt;" for every supported locale.</summary>
        public LocalizedText Localized(string field)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string code in LocalePolicy.SupportedCodes)
            {
                values.Add(code, Text(LocalizedHeader(field, code)));
            }

            try
            {
                return new LocalizedText(values);
            }
            catch (DataException exception)
            {
                throw new DataException($"{Where(field)}: {exception.Message}");
            }
        }

        public static string LocalizedHeader(string field, string localeCode)
        {
            return field + "." + localeCode;
        }

        /// <summary>The headers a localized field occupies, one per supported locale.</summary>
        public static IEnumerable<string> LocalizedHeaders(string field)
        {
            foreach (string code in LocalePolicy.SupportedCodes)
            {
                yield return LocalizedHeader(field, code);
            }
        }

        /// <summary>Invariant culture, the whole string, no whitespace, no '+' sign, no separators.</summary>
        static bool TryParseInt(string value, out int number)
        {
            number = 0;
            return value.Length > 0
                && value[0] != '+'
                && int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
        }

        string Raw(string header)
        {
            if (!_table.TryGetColumn(header, out int index))
            {
                throw new DataException($"{_table.Source}: header '{header}' is missing.");
            }

            return _fields[index];
        }
    }
}
