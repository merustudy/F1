using System.Collections.Generic;
using System.IO;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the static data that ships: sources convert, generated files are current and load.</summary>
    public sealed class ShippedDataTests
    {
        [Test]
        public void GeneratedJson_IsUpToDateWithSources()
        {
            List<string> stale = DataTransformMenu.FindStaleFiles();

            Assert.IsEmpty(stale, "Stale generated files. Run F1/Data/Transform Static Data: " + string.Join(", ", stale));
        }

        [Test]
        public void GeneratedJson_LoadsThroughTheRuntimeLoader()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();

            StaticData data = StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));

            Assert.IsNotEmpty(data.Jobs);
        }

        [Test]
        public void EveryDefinition_HasSourceAndGeneratedFile()
        {
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.SourceDirectory, file.SourceFileName)), file.SourceFileName);
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.GeneratedDirectory, file.GeneratedFileName)), file.GeneratedFileName);
            }
        }

        [Test]
        public void EveryShippedText_HasEverySupportedLocale()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            StaticData data = StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));

            foreach (JobData job in data.Jobs.Values)
            {
                foreach (string code in LocalePolicy.SupportedCodes)
                {
                    Assert.IsTrue(job.Name.Has(code), $"{job.Id} {code}");
                }
            }
        }
    }
}
