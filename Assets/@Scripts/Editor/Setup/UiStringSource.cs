using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using F1.Data;
using F1.Editor.Data;

namespace F1.Editor.Setup
{
    /// <summary>One row of UI_StaticText.csv.</summary>
    public sealed class UiStringRow
    {
        public UiStringRow(string key, string comment, IReadOnlyDictionary<string, string> values)
        {
            Key = key;
            Comment = comment;
            Values = values;
        }

        public string Key { get; }
        public string Comment { get; }

        /// <summary>Locale code -> text, for every supported locale.</summary>
        public IReadOnlyDictionary<string, string> Values { get; }
    }

    /// <summary>
    /// UI_StaticText.csv, parsed and validated. The CSV is the only source of UI text; the String
    /// Tables are made equal to it.
    /// </summary>
    public sealed class UiStringSource
    {
        public const string KeyHeader = "Key";
        public const string CommentHeader = "Shared Comments";

        static readonly Regex KeyPattern = new Regex("^[A-Z][A-Za-z0-9]*(\\.[A-Z][A-Za-z0-9]*)+$", RegexOptions.CultureInvariant);
        static readonly string[] PlaceholderValues = { "TODO", "TBD", "[MISSING]" };

        UiStringSource(List<UiStringRow> rows)
        {
            Rows = rows;
        }

        public IReadOnlyList<UiStringRow> Rows { get; }

        /// <summary>Throws a DataException that lists every problem found.</summary>
        public static UiStringSource Parse(string csv, string sourceName)
        {
            CsvTable table = CsvTable.Parse(csv, sourceName);
            var headers = new List<string> { KeyHeader, CommentHeader };
            headers.AddRange(LocalePolicy.SupportedCodes);
            table.RequireHeaders(headers);

            var problems = new List<string>();
            var rows = new List<UiStringRow>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (CsvRow row in table.Rows)
            {
                try
                {
                    string key = row.Text(KeyHeader);
                    if (!KeyPattern.IsMatch(key))
                    {
                        throw new DataException($"{row.Where(KeyHeader)}: '{key}' is not '<Area>.<Element>' in PascalCase.");
                    }

                    if (!seen.Add(key))
                    {
                        throw new DataException($"{row.Where(KeyHeader)}: duplicate key '{key}'.");
                    }

                    var values = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (string code in LocalePolicy.SupportedCodes)
                    {
                        string value = row.Text(code);
                        if (Array.IndexOf(PlaceholderValues, value.ToUpperInvariant()) >= 0)
                        {
                            throw new DataException($"{row.Where(code)}: '{value}' is a placeholder.");
                        }

                        values.Add(code, value);
                    }

                    string comment = row.IsEmpty(CommentHeader) ? string.Empty : row.Text(CommentHeader);
                    rows.Add(new UiStringRow(key, comment, values));
                }
                catch (DataException exception)
                {
                    problems.Add(exception.Message);
                }
            }

            if (problems.Count > 0)
            {
                throw new DataException(string.Join("\n", problems));
            }

            return new UiStringSource(rows);
        }

        /// <summary>Text with "{" is formatted with arguments at runtime.</summary>
        public static bool IsSmart(string value)
        {
            return value.IndexOf('{') >= 0;
        }
    }
}
