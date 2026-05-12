using NUnit.Framework;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;

namespace RPGPinball.Tests.EditMode
{
    public class ManaSystemTests
    {
        private GameObject hostGO;
        private ManaSystem mana;

        [SetUp]
        public void SetUp()
        {
            hostGO = new GameObject("ManaSystem_Host");
            mana = hostGO.AddComponent<ManaSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (hostGO != null) Object.DestroyImmediate(hostGO);
        }

        [Test]
        public void Initial_ManaIsZero()
        {
            Assert.AreEqual(0, mana.Mana);
        }

        [Test]
        public void SetManaDirect_ClampsBelowZero()
        {
            mana.SetManaDirect(-50);
            Assert.AreEqual(0, mana.Mana);
        }

        [Test]
        public void SetManaDirect_ClampsAboveMax()
        {
            mana.SetManaDirect(999);
            Assert.AreEqual(Constants.ManaMax, mana.Mana);
        }

        [Test]
        public void TrySpend_SufficientMana_DeductsAndReturnsTrue()
        {
            mana.SetManaDirect(50);
            bool ok = mana.TrySpend(30);
            Assert.IsTrue(ok);
            Assert.AreEqual(20, mana.Mana);
        }

        [Test]
        public void TrySpend_InsufficientMana_ReturnsFalse()
        {
            mana.SetManaDirect(20);
            bool ok = mana.TrySpend(30);
            Assert.IsFalse(ok);
            Assert.AreEqual(20, mana.Mana);
        }

        [Test]
        public void Charge_RespectsEfficiency()
        {
            mana.SetManaDirect(0);
            mana.ChargeEfficiency = 2.0f;
            mana.Charge(Constants.ManaPerWall); // 3 × 2 = 6
            Assert.AreEqual(6, mana.Mana);
        }

        [Test]
        public void Charge_ClampedToMax()
        {
            mana.SetManaDirect(98);
            mana.ChargeEfficiency = 1.0f;
            mana.Charge(Constants.ManaPerMonster); // +8, but clamped to 100
            Assert.AreEqual(Constants.ManaMax, mana.Mana);
        }
    }
}
