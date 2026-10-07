using System;
using System.Globalization;
using System.IO;

namespace F1.Core
{
    /// <summary>
    /// The play log of the tuning stage: one line each time a screen comes up, so that a play session can be split into the
    /// time spent on each screen (Docs/Architecture/10_TESTING_VALIDATION.md "플레이 기록"). It is a diagnostic kept beside
    /// the saves, never in them: a line that cannot be written is dropped and the game goes on.
    /// </summary>
    public sealed class PlayLog
    {
        public const string FolderName = "Logs";

        readonly string _folder;
        readonly Func<DateTime> _now;

        /// <param name="folder">Where the files go. Made when the first line is written.</param>
        /// <param name="now">The clock; local time when omitted. Tests pass a fixed one.</param>
        public PlayLog(string folder, Func<DateTime> now = null)
        {
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
            _now = now ?? (() => DateTime.Now);
        }

        /// <summary>The file of this session, named after its first line. Null before one is written.</summary>
        public string FilePath { get; private set; }

        /// <summary>True after a line could not be written. Nothing more is tried.</summary>
        public bool Failed { get; private set; }

        /// <summary>play-&lt;start&gt;.log: one file per session of the app.</summary>
        public static string FileName(DateTime start)
        {
            return "play-" + start.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".log";
        }

        /// <summary>One line: the time to the millisecond, the screen and the state, separated by tabs.</summary>
        public static string FormatLine(DateTime at, string screen, string state)
        {
            return at.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\t" + screen + "\t" + state;
        }

        /// <summary>Appends a line. False when it could not be written; the log then stays quiet.</summary>
        public bool Append(string screen, string state)
        {
            if (Failed)
            {
                return false;
            }

            try
            {
                DateTime at = _now();
                if (FilePath == null)
                {
                    Directory.CreateDirectory(_folder);
                    FilePath = Path.Combine(_folder, FileName(at));
                }

                File.AppendAllText(FilePath, FormatLine(at, screen, state) + "\n");
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is NotSupportedException)
            {
                Failed = true;
                return false;
            }
        }
    }
}
