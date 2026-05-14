using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 도감 저항력 전이 검증. CollectionManager 내부 임계치 1/3/7/15/30.
    /// </summary>
    public class GimmickResistLevelTests
    {
        private GameObject host;
        private CollectionManager cm;

        [SetUp]
        public void SetUp()
        {
            TestSingletonReset.ClearAllManagers();
            host = new GameObject("TestCollection");
            cm = host.AddComponent<CollectionManager>();
            cm.InitializeForTest();
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        private void SimulateDeaths(GimmickId id, int n)
        {
            for (int i = 0; i < n; i++) cm.RegisterDeath(id);
        }

        [Test]
        public void NoDeath_ResistLevel_IsZero()
        {
            Assert.AreEqual(0, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void OneDeath_ResistLevel_IsOne()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 1);
            Assert.AreEqual(1, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void ThreeDeaths_ResistLevel_IsTwo()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 3);
            Assert.AreEqual(2, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void SevenDeaths_ResistLevel_IsThree()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 7);
            Assert.AreEqual(3, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void FifteenDeaths_ResistLevel_IsFour()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 15);
            Assert.AreEqual(4, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void ThirtyDeaths_ResistLevel_IsFive()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 30);
            Assert.AreEqual(5, cm.GetResistLevel(GimmickId.HiddenBumper));
        }

        [Test]
        public void ResistReduction_Lv3_Is30Percent()
        {
            SimulateDeaths(GimmickId.HiddenBumper, 7);
            Assert.That(cm.GetResistReduction(GimmickId.HiddenBumper),
                Is.EqualTo(0.3f).Within(0.001f));
        }
    }
}
