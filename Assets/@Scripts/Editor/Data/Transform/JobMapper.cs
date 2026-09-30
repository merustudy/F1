using System.Collections.Generic;
using System.Linq;
using F1.Data;

namespace F1.Editor.Data
{
    /// <summary>Explicit CSV-to-definition mapping for JobData.csv.</summary>
    internal static class JobMapper
    {
        const string Id = "Id";
        const string Name = "Name";

        public static List<JobData> Map(CsvTable table, List<string> errors)
        {
            table.RequireHeaders(new[] { Id }.Concat(CsvRow.LocalizedHeaders(Name)));

            var jobs = new List<JobData>();
            foreach (CsvRow row in table.Rows)
            {
                try
                {
                    jobs.Add(new JobData(row.Id(Id), row.Localized(Name)));
                }
                catch (DataException exception)
                {
                    errors.Add(row.Contextualize(exception.Message));
                }
            }

            return jobs;
        }
    }
}
