using NUnit.Framework;
using RPGPinball.Combat;
using RPGPinball.Core;

namespace RPGPinball.Tests.EditMode
{
    public class ComboSystemTests
    {
        [Test]
        public void ManaMultiplier_Under10_IsOne()
        {
            Assert.AreEqual(1f, ComboSystem.GetManaMultiplier(0));
            Assert.AreEqual(1f, ComboSystem.GetManaMultiplier(9));
        }

        [Test]
        public void ManaMultiplier_Tier1_Is1_5()
        {
            Assert.AreEqual(Constants.ComboMultTier1, ComboSystem.GetManaMultiplier(Constants.ComboTier1));
            Assert.AreEqual(Constants.ComboMultTier1, ComboSystem.GetManaMultiplier(Constants.ComboTier2 - 1));
        }

        [Test]
        public void ManaMultiplier_Tier2_Is2_0()
        {
            Assert.AreEqual(Constants.ComboMultTier2, ComboSystem.GetManaMultiplier(Constants.ComboTier2));
            Assert.AreEqual(Constants.ComboMultTier2, ComboSystem.GetManaMultiplier(100));
        }
    }
}
