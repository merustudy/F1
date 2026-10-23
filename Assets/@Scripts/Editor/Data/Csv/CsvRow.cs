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

        /// <summary>An enemy's items: "id:power percent" pairs joined with '+', for example "rusty_blade:222+buckler:175" (stage 22: was id:grade).</summary>
        public List<ItemGrant> GrantList(string header)
        {
            return Grants(header, Text(header));
        }

        List<ItemGrant> Grants(string header, string text)
        {
            var grants = new List<ItemGrant>();
            foreach (string part in text.Split(ListSeparator))
            {
                string[] pair = part.Split(':');
                if (pair.Length != 2 || !DataId.IsValid(pair[0]) || !TryParseInt(pair[1], out int percent) || percent < 1)
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'item_id:power percent'.");
                }

                grants.Add(new ItemGrant(pair[0], percent));
            }

            return grants;
        }

        /// <summary>
        /// A boss's phases (Slice C stage 28): "75=item:100+item:100|50=item:120", each phase an HP percent and the items (as <see cref="GrantList"/>)
        /// the boss carries from there, separated by '|'. An empty cell is no phase.
        /// </summary>
        public List<EnemyPhase> PhaseList(string header)
        {
            var phases = new List<EnemyPhase>();
            if (!_table.TryGetColumn(header, out int _) || IsEmpty(header))
            {
                return phases;
            }

            foreach (string part in Text(header).Split('|'))
            {
                int equals = part.IndexOf('=');
                if (equals <= 0 || !TryParseInt(part.Substring(0, equals), out int percent))
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'hp percent=item_id:power percent+...'.");
                }

                phases.Add(new EnemyPhase(percent, Grants(header, part.Substring(equals + 1))));
            }

            return phases;
        }

        /// <summary>
        /// An enemy's deep items (2026-10-20, Docs/Design/03_Dungeon_Structure.md §4): "11=item:100|21=item:100+item:100", each bundle the first map
        /// floor it is carried from and the items (as <see cref="GrantList"/>), separated by '|'. An empty or missing cell is no bundle.
        /// </summary>
        public List<EnemyDepthItems> DepthList(string header)
        {
            var bundles = new List<EnemyDepthItems>();
            if (!_table.TryGetColumn(header, out int _) || IsEmpty(header))
            {
                return bundles;
            }

            foreach (string part in Text(header).Split('|'))
            {
                int equals = part.IndexOf('=');
                if (equals <= 0 || !TryParseInt(part.Substring(0, equals), out int floor))
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'floor=item_id:power percent+...'.");
                }

                bundles.Add(new EnemyDepthItems(floor, Grants(header, part.Substring(equals + 1))));
            }

            return bundles;
        }

        /// <summary>A value with up to two decimals, read in hundredths (Slice B stage 22, <see cref="global::F1.Data.Centi"/>): "38" is 3800, "5.45" is 545.</summary>
        public int Centi(string header)
        {
            string value = Text(header);
            if (!global::F1.Data.Centi.TryParse(value, out int hundredths))
            {
                throw new DataException($"{Where(header)}: '{value}' is not a number with at most two decimals.");
            }

            return hundredths;
        }

        /// <summary>A damage range "least-most" in hundredths, for example "2-4" or "1.5-2.5"; a single value is both ends.</summary>
        public (int Min, int Max) CentiRange(string header)
        {
            string value = Text(header);
            string[] ends = value.Split('-');
            if (ends.Length > 2 || !global::F1.Data.Centi.TryParse(ends[0], out int min) || !global::F1.Data.Centi.TryParse(ends[ends.Length - 1], out int max))
            {
                throw new DataException($"{Where(header)}: '{value}' is not 'least-most' with at most two decimals.");
            }

            return (min, max);
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

        /// <summary>A column a file may leave out: the default without the column or with an empty value.</summary>
        public int OptionalInt(string header, int defaultValue, int min = int.MinValue, int max = int.MaxValue)
        {
            return !_table.TryGetColumn(header, out int _) || IsEmpty(header) ? defaultValue : Int(header, min, max);
        }

        /// <summary>An id a file may leave out: null without the column or with an empty value.</summary>
        public string OptionalId(string header)
        {
            return !_table.TryGetColumn(header, out int _) || IsEmpty(header) ? null : Id(header);
        }

        /// <summary>A column a file may leave out (an optional header): the default without the column or with an empty value.</summary>
        public bool OptionalBool(string header, bool defaultValue)
        {
            return !_table.TryGetColumn(header, out int _) || IsEmpty(header) ? defaultValue : Bool(header);
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

        /// <summary>An enum a file may leave out (an optional header): null without the column or with an empty value.</summary>
        public TEnum? OptionalEnum<TEnum>(string header)
            where TEnum : struct
        {
            return !_table.TryGetColumn(header, out int _) || IsEmpty(header) ? (TEnum?)null : Enum<TEnum>(header);
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

        /// <summary>A localized field that may be left out (Slice D stage 55, a story scene's lines): null when every locale is empty; one empty locale beside a filled one is an error.</summary>
        public LocalizedText OptionalLocalized(string field)
        {
            foreach (string code in LocalePolicy.SupportedCodes)
            {
                if (!IsEmpty(LocalizedHeader(field, code)))
                {
                    return Localized(field);
                }
            }

            return null;
        }

        /// <summary>Text a file may leave empty: null for an empty value. Surrounding whitespace is still an error.</summary>
        public string OptionalText(string header)
        {
            return IsEmpty(header) ? null : Text(header);
        }

        /// <summary>A story camera (stage 55): "x:y:zoom" — the centre in thousandths of the picture, the zoom in percent ("500:500:100"). Null for an empty cell.</summary>
        public StoryCamera OptionalCamera(string header)
        {
            if (IsEmpty(header))
            {
                return null;
            }

            string value = Text(header);
            string[] parts = value.Split(':');
            if (parts.Length != 3 || !TryParseInt(parts[0], out int x) || !TryParseInt(parts[1], out int y) || !TryParseInt(parts[2], out int zoom))
            {
                throw new DataException($"{Where(header)}: '{value}' is not 'x:y:zoom' (thousandths, thousandths, percent).");
            }

            try
            {
                return new StoryCamera(x, y, zoom);
            }
            catch (DataException exception)
            {
                throw new DataException($"{Where(header)}: {exception.Message}");
            }
        }

        /// <summary>The lines a story scene keeps in the short cut (stage 55): "all" for every one of <paramref name="lineCount"/>, line numbers joined with '+' ("2", "1+2"), or empty for none.</summary>
        public List<int> LineNumbers(string header, int lineCount)
        {
            var numbers = new List<int>();
            if (IsEmpty(header))
            {
                return numbers;
            }

            string value = Text(header);
            if (value == "all")
            {
                for (int n = 1; n <= lineCount; n++)
                {
                    numbers.Add(n);
                }

                return numbers;
            }

            foreach (string part in value.Split(ListSeparator))
            {
                if (!TryParseInt(part, out int number))
                {
                    throw new DataException($"{Where(header)}: '{value}' is not 'all' or line numbers joined with '+'.");
                }

                numbers.Add(number);
            }

            return numbers;
        }

        /// <summary>A speaker's faces (stage 55): "expression:address" joined with '+' ("neutral:portrait/npc/linette-neutral+cold:…").</summary>
        public List<NpcPortrait> PortraitList(string header)
        {
            var portraits = new List<NpcPortrait>();
            foreach (string part in Text(header).Split(ListSeparator))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0 || colon == part.Length - 1)
                {
                    throw new DataException($"{Where(header)}: '{part}' is not 'expression:address'.");
                }

                try
                {
                    portraits.Add(new NpcPortrait(part.Substring(0, colon), part.Substring(colon + 1)));
                }
                catch (DataException exception)
                {
                    throw new DataException($"{Where(header)}: {exception.Message}");
                }
            }

            return portraits;
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
