using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace F1.Data
{
    public sealed class JobRecord
    {
        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id;

        [JsonProperty(Order = 2, Required = Required.Always)]
        public SortedDictionary<string, string> Name;
    }

    public static class JobJson
    {
        public static string Serialize(IEnumerable<JobData> jobs)
        {
            List<JobRecord> records = jobs
                .OrderBy(job => job.Id, StringComparer.Ordinal)
                .Select(job => new JobRecord
                {
                    Id = job.Id,
                    Name = StaticDataJson.ToRecord(job.Name),
                })
                .ToList();
            return StaticDataJson.Serialize(records);
        }

        public static List<JobData> Parse(string json)
        {
            string fileName = StaticDataFiles.Job.GeneratedFileName;
            var jobs = new List<JobData>();
            foreach (JobRecord record in StaticDataJson.Deserialize<JobRecord>(json, fileName))
            {
                try
                {
                    jobs.Add(new JobData(record.Id, StaticDataJson.ToLocalizedText(record.Name)));
                }
                catch (DataException exception)
                {
                    throw new DataException($"{fileName}: {exception.Message}");
                }
            }

            return jobs;
        }
    }
}
