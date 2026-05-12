using NUnit.Framework;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    public class DamageCalculatorTests
    {
        // Damage_Formula.md §1[1]: PlayerBaseDamage=10. Lv.0 베이스 = 10. Lv.1 = 10.2 (10*(1+0.02))
        [Test]
        public void BaseDamage_Level0_Is10()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(10f, r.FinalDamage, 0.001f);
        }

        [Test]
        public void BaseDamage_Level1_Is10_2()
        {
            var ctx = DamageContext.Default(1, DamageType.Physical);
            ctx.CritChance = 0f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(10.2f, r.FinalDamage, 0.001f);
        }

        // Damage_Formula.md §1[1]: Lv.90 = 10 * (1 + 90*0.02) = 28
        [Test]
        public void BaseDamage_Level90_Is28()
        {
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(28f, r.FinalDamage, 0.001f);
        }

        // 합연산 +25% → 28 * 1.25 = 35
        [Test]
        public void AdditivePercents_Combine()
        {
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.AdditivePercents = new[] { 0.25f };
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(35f, r.FinalDamage, 0.001f);
        }

        // DEF 10 적: 28 - 10 = 18
        [Test]
        public void Defense_Subtracted()
        {
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.TargetDefense = 10;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(18f, r.FinalDamage, 0.001f);
        }

        // 미스릴 공 + 마법 데미지 → ×1.15
        [Test]
        public void MithrilMagic_AppliesMultiplier()
        {
            var ctx = DamageContext.Default(90, DamageType.Magic);
            ctx.CritChance = 0f;
            ctx.IsMithrilBall = true;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(28f * Constants.MithrilMagicMultiplier, r.FinalDamage, 0.001f);
        }

        // 미스릴 공 + 물리 데미지 → 배율 미적용
        [Test]
        public void MithrilPhysical_DoesNotApplyMultiplier()
        {
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.IsMithrilBall = true;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(28f, r.FinalDamage, 0.001f);
        }

        // 곱연산 4개 → 처음 2개만 곱, 나머지 2개는 합연산 전환
        // 베이스 10 (Lv.0) × 1.5 × 1.5 × (1 + 0.5 + 0.5) = 45
        [Test]
        public void MultiplierStackLimit_OverflowConvertsToAdditive()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.MultiplierFactors = new[] { 1.5f, 1.5f, 1.5f, 1.5f };
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(45f, r.FinalDamage, 0.001f);
        }

        // 최종 최소 데미지 1 보장
        [Test]
        public void FinalDamage_MinimumOne()
        {
            var ctx = DamageContext.Default(1, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.TargetDefense = 999;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(1f, r.FinalDamage, 0.001f);
        }
    }
}
