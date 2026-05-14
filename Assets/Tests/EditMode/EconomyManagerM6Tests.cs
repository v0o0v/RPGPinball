using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Tests.EditMode
{
    public class EconomyManagerM6Tests
    {
        private GameObject host;
        private EconomyManager econ;
        private PlayerData pd;

        [SetUp]
        public void SetUp()
        {
            TestSingletonReset.ClearAllManagers();
            host = new GameObject("TestEconomy");
            econ = host.AddComponent<EconomyManager>();
            pd = ScriptableObject.CreateInstance<PlayerData>();
            pd.gold = 1000;
            pd.manaCrystal = 50;
            pd.bossSoul = 5;
            econ.Initialize(pd, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (pd != null) Object.DestroyImmediate(pd);
        }

        [Test]
        public void Add_IncreasesBalance()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            econ.Add(CurrencyId.Gold, 500, "test");
            Assert.AreEqual(before + 500, econ.GetBalance(CurrencyId.Gold));
        }

        [Test]
        public void Spend_DecreasesBalance_WhenSufficient()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            Assert.IsTrue(econ.Spend(CurrencyId.Gold, 300, "test"));
            Assert.AreEqual(before - 300, econ.GetBalance(CurrencyId.Gold));
        }

        [Test]
        public void Spend_FailsIfInsufficient()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            Assert.IsFalse(econ.Spend(CurrencyId.Gold, before + 1, "test"));
            Assert.AreEqual(before, econ.GetBalance(CurrencyId.Gold));
        }

        [Test]
        public void Has_ReturnsCorrectAnswer()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            Assert.IsTrue(econ.Has(CurrencyId.Gold, before));
            Assert.IsFalse(econ.Has(CurrencyId.Gold, before + 1));
        }

        [Test]
        public void Suppress_BlocksAutoBossReward()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            econ.Suppress(true);
            EventBus.Publish(new OnBossDefeated
            {
                BossId = BossId.Act1_FleshPlant,
                GoldReward = 500,
                ManaCrystal = 0,
                BossSoul = 0,
                SpReward = 0,
                XpReward = 0
            });
            Assert.AreEqual(before, econ.GetBalance(CurrencyId.Gold));
            econ.Suppress(false);
        }

        [Test]
        public void BossDefeated_AwardsGold_WhenNotSuppressed()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            econ.Suppress(false);
            EventBus.Publish(new OnBossDefeated
            {
                BossId = BossId.Act1_FleshPlant,
                GoldReward = 300,
                ManaCrystal = 5,
                BossSoul = 2,
                SpReward = 0,
                XpReward = 0
            });
            Assert.AreEqual(before + 300, econ.GetBalance(CurrencyId.Gold));
        }

        [Test]
        public void AddNegativeOrZero_Ignored()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            econ.Add(CurrencyId.Gold, 0, "test");
            econ.Add(CurrencyId.Gold, -5, "test");
            Assert.AreEqual(before, econ.GetBalance(CurrencyId.Gold));
        }
    }
}
