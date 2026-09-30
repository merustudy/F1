using System;
using System.Text;
using Newtonsoft.Json;

namespace F1.Save
{
    public enum SaveLoadStatus
    {
        /// <summary>Neither the file nor its backup exists.</summary>
        Missing,
        Loaded,
        /// <summary>The file was unreadable; the backup was used and the file was repaired from it.</summary>
        RestoredFromBackup,
        /// <summary>Both the file and its backup are unreadable.</summary>
        Corrupt,
    }

    public readonly struct SaveLoadResult<T>
        where T : class
    {
        public SaveLoadResult(SaveLoadStatus status, T value)
        {
            Status = status;
            Value = value;
        }

        public SaveLoadStatus Status { get; }
        public T Value { get; }
        public bool HasValue => Status == SaveLoadStatus.Loaded || Status == SaveLoadStatus.RestoredFromBackup;
    }

    /// <summary>
    /// Format and IO of save files only. Applying loaded state is the job of the state's owner.
    /// </summary>
    public sealed class SaveManager
    {
        static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
        };

        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        readonly SaveStorage _storage;

        public SaveManager(string rootPath)
        {
            _storage = new SaveStorage(rootPath);
        }

        public string RootPath => _storage.RootPath;

        public void Initialize()
        {
            _storage.Initialize();
        }

        public void Save<T>(string fileName, T dto)
            where T : class
        {
            _storage.Write(fileName, Serialize(dto));
        }

        /// <summary>Writes bytes produced by <see cref="Serialize{T}"/>. Used to retry the exact snapshot that failed.</summary>
        public void SaveBytes(string fileName, byte[] bytes)
        {
            _storage.Write(fileName, bytes);
        }

        /// <summary>
        /// Loads the file; falls back to the backup when the file cannot be parsed or fails
        /// <paramref name="isValid"/>. A successful fallback repairs the file without touching the backup.
        /// </summary>
        public SaveLoadResult<T> Load<T>(string fileName, Func<T, bool> isValid = null)
            where T : class
        {
            byte[] primary = _storage.ReadPrimary(fileName);
            if (primary != null && TryDeserialize(primary, isValid, out T value))
            {
                return new SaveLoadResult<T>(SaveLoadStatus.Loaded, value);
            }

            byte[] backup = _storage.ReadBackup(fileName);
            if (backup != null && TryDeserialize(backup, isValid, out value))
            {
                _storage.Write(fileName, backup, rotateBackup: false);
                return new SaveLoadResult<T>(SaveLoadStatus.RestoredFromBackup, value);
            }

            SaveLoadStatus status = primary == null && backup == null ? SaveLoadStatus.Missing : SaveLoadStatus.Corrupt;
            return new SaveLoadResult<T>(status, null);
        }

        public void Delete(string fileName)
        {
            _storage.Delete(fileName);
        }

        public static byte[] Serialize<T>(T dto)
            where T : class
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            return Utf8.GetBytes(JsonConvert.SerializeObject(dto, JsonSettings));
        }

        static bool TryDeserialize<T>(byte[] bytes, Func<T, bool> isValid, out T value)
            where T : class
        {
            try
            {
                value = JsonConvert.DeserializeObject<T>(Utf8.GetString(bytes), JsonSettings);
            }
            catch (Exception exception) when (exception is JsonException || exception is ArgumentException)
            {
                value = null;
                return false;
            }

            if (value == null || (isValid != null && !isValid(value)))
            {
                value = null;
                return false;
            }

            return true;
        }
    }
}
