using System;
using System.IO;
using F1.Core;
using F1.Save;
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

        [TestCase("Resource")]
        [TestCase("Save")]
        [TestCase("Setting")]
        [TestCase("Scene")]
        public void Configure_WhenManagerIsMissing_ThrowsAndStaysUnconfigured(string missing)
        {
            ManagerSet set = CreateFullSet();
            typeof(ManagerSet).GetField(missing).SetValue(set, null);

            Assert.Throws<ArgumentException>(() => Managers.Configure(set));
            Assert.IsFalse(Managers.IsConfigured);
        }

        [Test]
        public void Getter_WhenConfigured_ReturnsRegisteredInstance()
        {
            ManagerSet set = CreateFullSet();

            Managers.Configure(set);

            Assert.IsTrue(Managers.IsConfigured);
            Assert.AreSame(set.Resource, Managers.Resource);
            Assert.AreSame(set.Save, Managers.Save);
            Assert.AreSame(set.Setting, Managers.Setting);
            Assert.AreSame(set.Scene, Managers.Scene);
        }

        static ManagerSet CreateFullSet()
        {
            // Constructors do not touch the disk or Addressables.
            var save = new SaveManager(Path.Combine(Path.GetTempPath(), "F1Tests", "unused"));
            return new ManagerSet
            {
                Resource = new ResourceManager(),
                Save = save,
                Setting = new SettingManager(save),
                Scene = new SceneManagerEx(),
            };
        }
    }
}
