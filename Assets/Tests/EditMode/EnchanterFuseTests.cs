using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    public class EnchanterFuseTests
    {
        private GameObject econHost;
        private GameObject enchHost;
        private EconomyManager econ;
        private EnchanterManager ench;
        private PlayerData pd;

        [SetUp]
        public void SetUp()
        {
            TestSingletonReset.ClearAllManagers();
            econHost = new GameObject("Econ");
            econ = econHost.AddComponent<EconomyManager>();
            pd = ScriptableObject.CreateInstance<PlayerData>();
            pd.gold = 10000;
            econ.Initialize(pd, null);

            enchHost = new GameObject("Ench");
            ench = enchHost.AddComponent<EnchanterManager>();
            ench.InitializeForTest();
        }

        [TearDown]
        public void TearDown()
        {
            if (econHost != null) Object.DestroyImmediate(econHost);
            if (enchHost != null) Object.DestroyImmediate(enchHost);
            if (pd != null) Object.DestroyImmediate(pd);
        }

        [Test]
        public void Fuse_ThreeNormal_ProducesOneRare_AndCostsGold()
        {
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            long goldBefore = econ.GetBalance(CurrencyId.Gold);

            Assert.IsTrue(ench.FuseRune(RuneId.Executioner, RuneGrade.Normal));
            Assert.AreEqual(goldBefore - Constants.RuneFuseGoldNormalToRare, econ.GetBalance(CurrencyId.Gold));

            int normalCount = 0, rareCount = 0;
            foreach (var r in ench.Inventory)
            {
                if (r.id == RuneId.Executioner && r.grade == RuneGrade.Normal) normalCount++;
                if (r.id == RuneId.Executioner && r.grade == RuneGrade.Rare) rareCount++;
            }
            Assert.AreEqual(0, normalCount);
            Assert.AreEqual(1, rareCount);
        }

        [Test]
        public void Fuse_TwoNormal_Fails()
        {
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            Assert.IsFalse(ench.FuseRune(RuneId.Executioner, RuneGrade.Normal));
        }

        [Test]
        public void Fuse_LegendaryToHigher_Fails()
        {
            ench.AddRune(RuneId.Executioner, RuneGrade.Legendary);
            ench.AddRune(RuneId.Executioner, RuneGrade.Legendary);
            ench.AddRune(RuneId.Executioner, RuneGrade.Legendary);
            Assert.IsFalse(ench.FuseRune(RuneId.Executioner, RuneGrade.Legendary));
        }

        [Test]
        public void Equip_FillsSocket_Once()
        {
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            Assert.IsTrue(ench.EquipRune(RuneId.Executioner, RuneGrade.Normal, "skill1", socketCount: 1));
            // 두 번째 시도는 보유 부족
            Assert.IsFalse(ench.EquipRune(RuneId.Executioner, RuneGrade.Normal, "skill1", socketCount: 1));
        }

        [Test]
        public void Equip_SocketFull_Fails()
        {
            ench.AddRune(RuneId.Executioner, RuneGrade.Normal);
            ench.AddRune(RuneId.Pierce, RuneGrade.Normal);
            Assert.IsTrue(ench.EquipRune(RuneId.Executioner, RuneGrade.Normal, "skill1", socketCount: 1));
            Assert.IsFalse(ench.EquipRune(RuneId.Pierce, RuneGrade.Normal, "skill1", socketCount: 1));
        }
    }
}
