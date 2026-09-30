using System;

namespace F1.Data
{
    /// <summary>
    /// Builds <see cref="StaticData"/> from generated JSON. The game (through DataManager) and the
    /// simulator (from files on disk) both go through this one function.
    /// </summary>
    public static class StaticDataLoader
    {
        /// <param name="readJson">Returns the generated JSON text of a definition.</param>
        public static StaticData Load(Func<StaticDataFiles.Entry, string> readJson)
        {
            if (readJson == null)
            {
                throw new ArgumentNullException(nameof(readJson));
            }

            return new StaticData(
                JobJson.Parse(readJson(StaticDataFiles.Job)));
        }
    }
}
