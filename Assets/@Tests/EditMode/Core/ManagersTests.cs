using System;
using F1.Core;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class ManagersTests
    {
        [SetUp]
        public void SetUp()
        {
            Managers.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Managers.Reset();
        }

        [Test]
        public void Getter_WhenNotConfigured_Throws()
        {
            Assert.IsFalse(Managers.IsConfigured);
            Assert.Throws<InvalidOperationException>(() => { SceneManagerEx unused = Managers.Scene; });
        }

        [Test]
        public void Configure_WhenCalledTwice_Throws()
        {
            Managers.Configure(CreateFullSet());

            Assert.Throws<InvalidOperationException>(() => Managers.Configure(CreateFullSet()));
        }

        [Test]
        public void Configure_WhenManagerIsMissing_ThrowsAndStaysUnconfigured()
        {
            var set = CreateFullSet();
            set.Scene = null;

            Assert.Throws<ArgumentException>(() => Managers.Configure(set));
            Assert.IsFalse(Managers.IsConfigured);
        }

        [Test]
        public void Getter_WhenConfigured_ReturnsRegisteredInstance()
        {
            ManagerSet set = CreateFullSet();

            Managers.Configure(set);

            Assert.IsTrue(Managers.IsConfigured);
            Assert.AreSame(set.Scene, Managers.Scene);
        }

        static ManagerSet CreateFullSet()
        {
            return new ManagerSet
            {
                Scene = new SceneManagerEx(),
            };
        }
    }
}
