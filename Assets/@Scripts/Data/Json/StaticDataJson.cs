using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>Envelope of every generated data file.</summary>
    public sealed class DataFileJson<TRecord>
    {
        [JsonProperty(Order = 1, Required = Required.Always)]
        public int SchemaVersion;

        [JsonProperty(Order = 2, Required = Required.Always)]
        public List<TRecord> Items;
    }

    /// <summary>
    /// Reads and writes generated data files. Output is byte-stable: the same definitions always
    /// produce the same bytes (fixed property order, two-space indent, "\n", no timestamp).
    /// </summary>
    public static class StaticDataJson
    {
        static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            Culture = CultureInfo.InvariantCulture,
        });

        public static string Serialize<TRecord>(List<TRecord> items)
        {
            var file = new DataFileJson<TRecord>
            {
                SchemaVersion = StaticDataFiles.SchemaVersion,
                Items = items,
            };

            var text = new StringWriter(CultureInfo.InvariantCulture) { NewLine = "\n" };
            using (var writer = new JsonTextWriter(text))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                Serializer.Serialize(writer, file);
            }

            return text.ToString() + "\n";
        }

        public static List<TRecord> Deserialize<TRecord>(string json, string fileName)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new DataException($"{fileName}: file is empty.");
            }

            DataFileJson<TRecord> file;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    file = Serializer.Deserialize<DataFileJson<TRecord>>(reader);
                }
            }
            catch (JsonException exception)
            {
                throw new DataException($"{fileName}: {exception.Message}");
            }

            if (file == null || file.Items == null)
            {
                throw new DataException($"{fileName}: file has no items.");
            }

            if (file.SchemaVersion != StaticDataFiles.SchemaVersion)
            {
                throw new DataException(
                    $"{fileName}: schema version {file.SchemaVersion} is not {StaticDataFiles.SchemaVersion}. Run the data transform.");
            }

            return file.Items;
        }

        /// <summary>Locale codes are written in ordinal order so output does not depend on insertion order.</summary>
        public static SortedDictionary<string, string> ToRecord(LocalizedText text)
        {
            var record = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in text.Values)
            {
                record.Add(pair.Key, pair.Value);
            }

            return record;
        }

        public static LocalizedText ToLocalizedText(SortedDictionary<string, string> record)
        {
            return new LocalizedText(record);
        }
    }
}
