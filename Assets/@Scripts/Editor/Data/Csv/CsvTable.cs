using System;
using System.Collections.Generic;
using System.Text;
using F1.Data;

namespace F1.Editor.Data
{
    /// <summary>
    /// A parsed CSV file: a header row and data rows. Handles quoted fields, escaped quotes ("")
    /// and line breaks inside quoted fields. Fields are mapped by header name, never by position.
    /// </summary>
    public sealed class CsvTable
    {
        readonly Dictionary<string, int> _columns;

        CsvTable(string source, List<string> headers, List<CsvRow> rows, Dictionary<string, int> columns)
        {
            Source = source;
            Headers = headers;
            Rows = rows;
            _columns = columns;
        }

        /// <summary>File name used in error messages.</summary>
        public string Source { get; }
        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<CsvRow> Rows { get; }

        public bool HasHeader(string header)
        {
            return _columns.ContainsKey(header);
        }

        internal bool TryGetColumn(string header, out int index)
        {
            return _columns.TryGetValue(header, out index);
        }

        /// <summary>
        /// Every required header must exist and every header in the file must be known.
        /// An unknown header is an error so that a typo is never silently ignored.
        /// </summary>
        public void RequireHeaders(IEnumerable<string> required, IEnumerable<string> optional = null)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (string header in required)
            {
                known.Add(header);
                if (!_columns.ContainsKey(header))
                {
                    throw new DataException($"{Source}: required header '{header}' is missing.");
                }
            }

            if (optional != null)
            {
                foreach (string header in optional)
                {
                    known.Add(header);
                }
            }

            foreach (string header in Headers)
            {
                if (!known.Contains(header))
                {
                    throw new DataException($"{Source}: unknown header '{header}'.");
                }
            }
        }

        public static CsvTable Parse(string text, string source)
        {
            if (text == null)
            {
                throw new DataException($"{source}: file is missing.");
            }

            List<List<string>> records = ReadRecords(text, source, out List<int> lines);
            if (records.Count == 0)
            {
                throw new DataException($"{source}: header row is missing.");
            }

            List<string> headers = records[0];
            var columns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i];
                if (string.IsNullOrEmpty(header) || header != header.Trim())
                {
                    throw new DataException($"{source}: header {i + 1} is empty or has surrounding whitespace.");
                }

                if (columns.ContainsKey(header))
                {
                    throw new DataException($"{source}: duplicate header '{header}'.");
                }

                columns.Add(header, i);
            }

            var rows = new List<CsvRow>();
            var table = new CsvTable(source, headers, rows, columns);
            for (int i = 1; i < records.Count; i++)
            {
                if (records[i].Count != headers.Count)
                {
                    throw new DataException(
                        $"{source}({lines[i]}): row has {records[i].Count} fields but the header has {headers.Count}.");
                }

                rows.Add(new CsvRow(table, lines[i], records[i]));
            }

            return table;
        }

        /// <summary>Splits the text into records. Blank lines are skipped.</summary>
        static List<List<string>> ReadRecords(string text, string source, out List<int> lines)
        {
            var records = new List<List<string>>();
            lines = new List<int>();

            var field = new StringBuilder();
            var record = new List<string>();
            bool inQuotes = false;
            bool fieldWasQuoted = false;
            bool recordHasQuotedField = false;
            int line = 1;
            int recordLine = 1;
            int i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

            void EndField()
            {
                record.Add(field.ToString());
                field.Length = 0;
                recordHasQuotedField |= fieldWasQuoted;
                fieldWasQuoted = false;
            }

            void EndRecord(List<int> lineNumbers)
            {
                EndField();

                // A line with nothing on it is skipped. A single quoted empty field ("") is a real row.
                bool blank = record.Count == 1 && record[0].Length == 0 && !recordHasQuotedField;
                if (!blank)
                {
                    records.Add(record);
                    lineNumbers.Add(recordLine);
                }

                record = new List<string>();
                recordHasQuotedField = false;
            }

            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        // A line break inside a quoted field is always stored as "\n".
                    }
                    else
                    {
                        if (c == '\n')
                        {
                            line++;
                        }

                        field.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        if (field.Length > 0 || fieldWasQuoted)
                        {
                            throw new DataException($"{source}({line}): unexpected quote inside a field.");
                        }

                        inQuotes = true;
                        fieldWasQuoted = true;
                        break;
                    case ',':
                        EndField();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        EndRecord(lines);
                        line++;
                        recordLine = line;
                        break;
                    default:
                        if (fieldWasQuoted)
                        {
                            throw new DataException($"{source}({line}): text after a closing quote.");
                        }

                        field.Append(c);
                        break;
                }
            }

            if (inQuotes)
            {
                throw new DataException($"{source}({recordLine}): quoted field is not closed.");
            }

            if (field.Length > 0 || record.Count > 0 || fieldWasQuoted)
            {
                EndRecord(lines);
            }

            return records;
        }
    }
}
