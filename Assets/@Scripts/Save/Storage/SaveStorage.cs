using System;
using System.IO;

namespace F1.Save
{
    public sealed class SaveWriteException : Exception
    {
        public SaveWriteException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// File-level IO for one save directory. Each file has a ".tmp" used only while writing and a
    /// one-generation ".bak" holding the previous content.
    /// </summary>
    public sealed class SaveStorage
    {
        public const string TempSuffix = ".tmp";
        public const string BackupSuffix = ".bak";

        public SaveStorage(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentException("Save root path is empty.", nameof(rootPath));
            }

            RootPath = rootPath;
        }

        public string RootPath { get; }

        /// <summary>Creates the directory and removes temp files left by an interrupted write.</summary>
        public void Initialize()
        {
            Directory.CreateDirectory(RootPath);
            foreach (string stale in Directory.GetFiles(RootPath, "*" + TempSuffix))
            {
                File.Delete(stale);
            }
        }

        public string PathOf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || fileName != Path.GetFileName(fileName))
            {
                throw new ArgumentException($"'{fileName}' is not a plain file name.", nameof(fileName));
            }

            return Path.Combine(RootPath, fileName);
        }

        public byte[] ReadPrimary(string fileName)
        {
            return ReadOrNull(PathOf(fileName));
        }

        public byte[] ReadBackup(string fileName)
        {
            return ReadOrNull(PathOf(fileName) + BackupSuffix);
        }

        /// <summary>
        /// Atomic write: temp file, flush, then replace. With <paramref name="rotateBackup"/> the previous
        /// content becomes the backup. A repair write passes false so a good backup is never overwritten.
        /// </summary>
        public void Write(string fileName, byte[] bytes, bool rotateBackup = true)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            string path = PathOf(fileName);
            string temp = path + TempSuffix;
            try
            {
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    File.Replace(temp, path, rotateBackup ? path + BackupSuffix : null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new SaveWriteException($"Could not write save file '{fileName}'.", exception);
            }
        }

        public void Delete(string fileName)
        {
            string path = PathOf(fileName);
            File.Delete(path);
            File.Delete(path + BackupSuffix);
            File.Delete(path + TempSuffix);
        }

        static byte[] ReadOrNull(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
