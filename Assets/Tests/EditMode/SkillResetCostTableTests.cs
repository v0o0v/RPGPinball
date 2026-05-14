using NUnit.Framework;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    public class SkillResetCostTableTests
    {
        private SkillResetCostTable MakeTable()
        {
            var so = ScriptableObject.CreateInstance<SkillResetCostTable>();
            so.costs = new int[] { 0, 1000, 3000, 5000 };
            return so;
        }

        [Test]
        public void FirstReset_IsFree()
        {
            Assert.AreEqual(0, MakeTable().GetCost(0));
        }

        [Test]
        public void SecondReset_IsThousand()
        {
            Assert.AreEqual(1000, MakeTable().GetCost(1));
        }

        [Test]
        public void ThirdReset_IsThreeThousand()
        {
            Assert.AreEqual(3000, MakeTable().GetCost(2));
        }

        [Test]
        public void FourthAndBeyond_IsFiveThousandConstant()
        {
            var table = MakeTable();
            Assert.AreEqual(5000, table.GetCost(3));
            Assert.AreEqual(5000, table.GetCost(4));
            Assert.AreEqual(5000, table.GetCost(10));
            Assert.AreEqual(5000, table.GetCost(99));
        }
    }
}
