using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace F1.Data
{
    /// <summary>Envelope of every generated data file.</summary>
    public sealed class DataFileJson<T>
    {
        [JsonProperty(Order = 1, Required = Required.Always)]
        public int SchemaVersion;

        [JsonProperty(Order = 2, Required = Required.Always)]
        public List<T> Items;
    }

    /// <summary>
    /// Reads and writes generated data files. Definitions are serialized directly: property order is
    /// fixed by attributes and loading goes through each definition's validating constructor.
    /// Output is byte-stable (two-space indent, "\n", no timestamp).
    /// </summary>
    public static class StaticDataJson
    {
        sealed class LocalizedTextConverter : JsonConverter<LocalizedText>
        {
            public override void WriteJson(JsonWriter writer, LocalizedText value, JsonSerializer serializer)
            {
                // Locale codes are written in ordinal order so output does not depend on insertion order.
                var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> pair in value.Values)
                {
                    sorted.Add(pair.Key, pair.Value);
                }

                writer.WriteStartObject();
                foreach (KeyValuePair<string, string> pair in sorted)
                {
                    writer.WritePropertyName(pair.Key);
                    writer.WriteValue(pair.Value);
                }

                writer.WriteEndObject();
            }

            public override LocalizedText ReadJson(
                JsonReader reader,
                Type objectType,
                LocalizedText existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                var values = serializer.Deserialize<Dictionary<string, string>>(reader);
                return new LocalizedText(values);
            }
        }

        static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            Culture = CultureInfo.InvariantCulture,
            Converters = { new StringEnumConverter(), new LocalizedTextConverter() },
        });

        /// <summary>Items must already be in their final order (by id).</summary>
        public static string Serialize<T>(List<T> items)
        {
            var file = new DataFileJson<T>
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

        public static List<T> Deserialize<T>(string json, string fileName)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new DataException($"{fileName}: file is empty.");
            }

            DataFileJson<T> file;
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    file = Serializer.Deserialize<DataFileJson<T>>(reader);
                }
            }
            catch (Exception exception) when (!(exception is OutOfMemoryException))
            {
                // A rule broken inside a definition constructor may arrive wrapped by the serializer.
                throw new DataException($"{fileName}: {Innermost(exception).Message}");
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

            foreach (T item in file.Items)
            {
                if (item == null)
                {
                    throw new DataException($"{fileName}: an item is null.");
                }
            }

            return file.Items;
        }

        static Exception Innermost(Exception exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is DataException)
                {
                    return current;
                }
            }

            return exception;
        }
    }
}
