using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Editor.Setup;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the shipped Addressables configuration against the entry lists in code.</summary>
    public sealed class AddressablesSetupTests
    {
        [Test]
        public void FindProblems_ForShippedSettings_IsEmpty()
        {
            List<string> problems = AddressablesSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void AllEntries_HaveUniqueValidAddresses()
        {
            IReadOnlyList<AddressEntry> entries = AddressablesSetup.AllEntries();

            foreach (AddressEntry entry in entries)
            {
                Assert.IsTrue(LogicalAddress.IsValid(entry.Address), entry.Address);
            }

            Assert.AreEqual(entries.Count, entries.Select(e => e.Address).Distinct().Count());
        }

        [TestCase("data/app/job", true)]
        [TestCase("ui/lobby/lobby-screen", true)]
        [TestCase("slot/relic/fairy-dust", true)]
        [TestCase("data", false)]
        [TestCase("Data/App/Job", false)]
        [TestCase("data/app/job_data", false)]
        [TestCase("data//job", false)]
        [TestCase("/data/app", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void LogicalAddress_IsValid_AcceptsLowercaseKebabPaths(string address, bool expected)
        {
            Assert.AreEqual(expected, LogicalAddress.IsValid(address));
        }
    }
}
