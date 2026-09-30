using System;
using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>
    /// All static data, validated as a whole. Both the game and the simulator build it from the same
    /// generated JSON through <see cref="StaticDataLoader"/>.
    /// </summary>
    public sealed class StaticData
    {
        public StaticData(IEnumerable<JobData> jobs)
        {
            var problems = new List<string>();
            Jobs = Index(jobs, job => job.Id, JobData.DefinitionName, problems);

            if (problems.Count > 0)
            {
                throw new DataValidationException(problems);
            }
        }

        public IReadOnlyDictionary<string, JobData> Jobs { get; }

        public JobData Job(string id)
        {
            return Find(Jobs, id, JobData.DefinitionName);
        }

        static T Find<T>(IReadOnlyDictionary<string, T> table, string id, string definition)
        {
            if (id != null && table.TryGetValue(id, out T value))
            {
                return value;
            }

            throw new KeyNotFoundException($"{definition} '{id}' does not exist.");
        }

        static IReadOnlyDictionary<string, T> Index<T>(
            IEnumerable<T> items,
            Func<T, string> idOf,
            string definition,
            List<string> problems)
        {
            var table = new Dictionary<string, T>(StringComparer.Ordinal);
            if (items == null)
            {
                problems.Add($"{definition}: no data.");
                return table;
            }

            foreach (T item in items)
            {
                string id = idOf(item);
                if (table.ContainsKey(id))
                {
                    problems.Add($"{definition}: duplicate id '{id}'.");
                    continue;
                }

                table.Add(id, item);
            }

            return table;
        }
    }
}
