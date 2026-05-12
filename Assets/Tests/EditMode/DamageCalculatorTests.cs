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

        // ── 마일스톤 3 신규 테스트 ──────────────────────────

        // 콤보 스트라이크: ComboCount × ComboStrikePerStack 합연산
        // base=10 (Lv.0), 콤보 20, +0.025/콤보 → 0.5 가산 → 10 × 1.5 = 15
        [Test]
        public void ComboStrike_AddsAdditivePerStack()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.ComboCount = 20;
            ctx.ComboStrikePerStack = 0.025f;
            ctx.ComboMaxStack = 100;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(15f, r.FinalDamage, 0.001f);
        }

        // 콤보 스트라이크: ComboMaxStack 초과 시 클램프
        // base=10, 콤보 100, +0.025/스택, MaxStack=10 → 0.25 가산 → 12.5
        [Test]
        public void ComboStrike_ClampedByMaxStack()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.ComboCount = 100;
            ctx.ComboStrikePerStack = 0.025f;
            ctx.ComboMaxStack = 10;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(12.5f, r.FinalDamage, 0.001f);
        }

        // 분노의 일격: HP 30% 이하 시 합연산 보너스
        [Test]
        public void FuryStrike_AppliedBelowHpThreshold()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.FuryStrikeBonus = 0.20f;
            ctx.TargetCurrentHpRatio = 0.25f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(12f, r.FinalDamage, 0.001f);
        }

        [Test]
        public void FuryStrike_NotAppliedAboveHpThreshold()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.FuryStrikeBonus = 0.20f;
            ctx.TargetCurrentHpRatio = 0.50f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(10f, r.FinalDamage, 0.001f);
        }

        // 플리퍼 스매시: IsAfterFlipperHit + FlipperSmashFactor 곱연산
        // base=10, 1.55 곱연산 → 15.5
        [Test]
        public void FlipperSmash_AppliedAsMultiplier()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.IsAfterFlipperHit = true;
            ctx.FlipperSmashFactor = 1.55f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(15.5f, r.FinalDamage, 0.001f);
        }

        // 관성 돌파 I: BallSpeed 비례 곱연산
        // 속도 40(최대), 인자 1.5 → 활성 배율 1.5 → base 10 × 1.5 = 15
        [Test]
        public void InertiaBreak_FullSpeedFullMultiplier()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.BallSpeed = Constants.BallMaxSpeed;
            ctx.InertiaBreakFactor = 1.5f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(15f, r.FinalDamage, 0.001f);
        }

        // 가속 코어: BallSpeed × AccelerationCoreCoeff
        // 속도 40, 계수 0.2 → +0.2 → base 10 × 1.2 = 12
        [Test]
        public void AccelerationCore_SpeedScaledMultiplier()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.BallSpeed = Constants.BallMaxSpeed;
            ctx.AccelerationCoreCoeff = 0.2f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(12f, r.FinalDamage, 0.001f);
        }

        // 아머 크래시: ArmorReductionPercent
        // base 10, DEF 5, 50% 감산 → 적용 DEF 2.5 → 10 - 2.5 = 7.5
        [Test]
        public void ArmorCrash_ReducesEffectiveDefense()
        {
            var ctx = DamageContext.Default(0, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.TargetDefense = 5;
            ctx.ArmorReductionPercent = 0.5f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(7.5f, r.FinalDamage, 0.001f);
        }
    }
}
