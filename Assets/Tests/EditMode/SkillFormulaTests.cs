using NUnit.Framework;
using RPGPinball.Combat;

namespace RPGPinball.Tests.EditMode
{
    public class SkillFormulaTests
    {
        [Test]
        public void Linear_Lv0_ReturnsBase()
        {
            // 10% + Lv*1% (탄성 강화)
            Assert.AreEqual(0.10f, SkillFormula.Linear(0.10f, 0.01f, 0), 0.0001f);
        }

        [Test]
        public void Linear_Lv5_Adds5xPerLevel()
        {
            // 10% + 5*1% = 15%
            Assert.AreEqual(0.15f, SkillFormula.Linear(0.10f, 0.01f, 5), 0.0001f);
        }

        [Test]
        public void Diminish_Lv0_Returns0()
        {
            Assert.AreEqual(0f, SkillFormula.Diminish(0.4f, 0.95f, 0), 0.0001f);
        }

        [Test]
        public void Diminish_Lv5_AtKnownValue()
        {
            // 0.4 × (1 - 0.95^5) = 0.4 × (1 - 0.7737...) = 0.4 × 0.2263 ≈ 0.0905
            float expected = 0.4f * (1f - UnityEngine.Mathf.Pow(0.95f, 5));
            Assert.AreEqual(expected, SkillFormula.Diminish(0.4f, 0.95f, 5), 0.0001f);
        }

        [Test]
        public void Diminish_LargeLv_ConvergesNearMax()
        {
            // 0.95^100 ≈ 0.006 → 0.4 × 0.994 ≈ 0.3976
            float result = SkillFormula.Diminish(0.4f, 0.95f, 100);
            Assert.Greater(result, 0.39f);
            Assert.LessOrEqual(result, 0.40f);
        }

        // 트리플 드로우: 기본 3, Lv 2당 +1
        [Test]
        public void StackLimit_TripleDraw_Lv0_Is3()
        {
            Assert.AreEqual(3, SkillFormula.StackLimit(3, 1, 0, 2));
        }

        [Test]
        public void StackLimit_TripleDraw_Lv1_Is3()
        {
            // Lv 1 / 2 = 0 steps → 3
            Assert.AreEqual(3, SkillFormula.StackLimit(3, 1, 1, 2));
        }

        [Test]
        public void StackLimit_TripleDraw_Lv2_Is4()
        {
            // Lv 2 / 2 = 1 step → 3 + 1 = 4
            Assert.AreEqual(4, SkillFormula.StackLimit(3, 1, 2, 2));
        }

        [Test]
        public void StackLimit_TripleDraw_Lv4_Is5()
        {
            // Lv 4 / 2 = 2 steps → 3 + 2 = 5
            Assert.AreEqual(5, SkillFormula.StackLimit(3, 1, 4, 2));
        }

        // 하이퍼 콤보: 기본 10, Lv당 +2
        [Test]
        public void StackLimit_HyperCombo_Lv4_Is18()
        {
            // 10 + 4*2 = 18
            Assert.AreEqual(18, SkillFormula.StackLimit(10, 2, 4, 1));
        }

        [Test]
        public void HardCapMin_BelowMin_ClampsToMin()
        {
            Assert.AreEqual(0.5f, SkillFormula.HardCapMin(0.3f, 0.5f), 0.0001f);
        }

        [Test]
        public void HardCapMin_AboveMin_PassesThrough()
        {
            Assert.AreEqual(0.8f, SkillFormula.HardCapMin(0.8f, 0.5f), 0.0001f);
        }

        // CooldownMultiplier: 두 패시브 곱연산
        // (1 - 0.4) × (1 - 0.3) = 0.6 × 0.7 = 0.42
        [Test]
        public void CooldownMultiplier_TwoReductions_CombinesMultiplicatively()
        {
            float result = SkillFormula.CooldownMultiplier(new[] { 0.4f, 0.3f });
            Assert.AreEqual(0.42f, result, 0.0001f);
        }
    }
}
