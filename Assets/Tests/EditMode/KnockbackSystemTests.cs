using NUnit.Framework;
using RPGPinball.Combat;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    public class KnockbackSystemTests
    {
        [Test]
        public void None_Multiplier_IsOne()
        {
            Assert.AreEqual(1f, KnockbackSystem.GetMultiplier(KnockbackTier.None, false));
            Assert.AreEqual(1f, KnockbackSystem.GetMultiplier(KnockbackTier.None, true));
        }

        [Test]
        public void Resist_Multiplier_IsHalf()
        {
            Assert.AreEqual(0.5f, KnockbackSystem.GetMultiplier(KnockbackTier.Resist, false));
        }

        [Test]
        public void Immune_Blocks_NonUltimate()
        {
            Assert.AreEqual(0f, KnockbackSystem.GetMultiplier(KnockbackTier.Immune, false));
        }

        [Test]
        public void Immune_PenetratedBy_Ultimate()
        {
            Assert.AreEqual(1f, KnockbackSystem.GetMultiplier(KnockbackTier.Immune, true));
        }

        [Test]
        public void Absolute_Blocks_Even_Ultimate()
        {
            Assert.AreEqual(0f, KnockbackSystem.GetMultiplier(KnockbackTier.Absolute, false));
            Assert.AreEqual(0f, KnockbackSystem.GetMultiplier(KnockbackTier.Absolute, true));
        }
    }
}
