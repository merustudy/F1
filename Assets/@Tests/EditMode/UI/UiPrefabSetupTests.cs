using System;
using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Editor.Setup;
using F1.Flow;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the shipped screen prefabs against their builder, the font and the string table.</summary>
    public sealed class UiPrefabSetupTests
    {
        [Test]
        public void FindProblems_ForShippedPrefabs_IsEmpty()
        {
            List<string> problems = UiPrefabSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryScreen_HasAValidAddressInItsScope()
        {
            foreach (ScreenId id in ScreenCatalog.All)
            {
                string address = ScreenCatalog.Address(id);
                Assert.IsTrue(LogicalAddress.IsValid(address), address);
                StringAssert.StartsWith("ui/" + LogicalAddress.ScopeSegment(ScreenCatalog.Scope(id)) + "/", address);
            }

            Assert.AreEqual(ScreenCatalog.All.Count, ScreenCatalog.All.Select(ScreenCatalog.Address).Distinct().Count());
        }

        [Test]
        public void EveryGamePhase_HasAScreen()
        {
            foreach (GamePhase phase in (GamePhase[])Enum.GetValues(typeof(GamePhase)))
            {
                Assert.DoesNotThrow(() => ScreenCatalog.ForPhase(phase), phase.ToString());
            }

            Assert.AreEqual(ScreenId.Lobby, ScreenCatalog.ForPhase(GamePhase.Lobby));
            Assert.AreEqual(ScreenId.Battle, ScreenCatalog.ForPhase(GamePhase.Battle));
        }
    }
}
