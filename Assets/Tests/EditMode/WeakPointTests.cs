using NUnit.Framework;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 약점 부위 메커니즘 검증. WeakPointHitbox는 OnCollisionEnter2D에서 발동되므로
    /// 본 테스트는 DamageContext.IsWeakPointHit + ignoresDefense 분기를 검증.
    /// </summary>
    public class WeakPointTests
    {
        [Test]
        public void WeakPoint_IgnoresDefense_AppliedAt0()
        {
            // 정상 케이스 — DEF 25, 28 베이스 → 28 - 25 = 3
            var ctxNormal = DamageContext.Default(90, DamageType.Physical);
            ctxNormal.CritChance = 0f;
            ctxNormal.TargetDefense = 25;
            var rNormal = DamageCalculator.Calculate(ctxNormal);
            Assert.AreEqual(3f, rNormal.FinalDamage, 0.001f);

            // 약점 타격 — DEF 무시 → 28 그대로
            var ctxWeak = DamageContext.Default(90, DamageType.Physical);
            ctxWeak.CritChance = 0f;
            ctxWeak.IsWeakPointHit = true;
            ctxWeak.TargetDefense = 0; // 약점 처리 시 WeakPointHitbox가 0으로 설정
            var rWeak = DamageCalculator.Calculate(ctxWeak);
            Assert.AreEqual(28f, rWeak.FinalDamage, 0.001f);
        }

        [Test]
        public void WeakPointSpec_Default_IsPhase1Active()
        {
            var spec = new WeakPointSpec
            {
                label = "테스트",
                localOffset = Vector2.zero,
                radius = 0.5f,
                ignoresDefense = false,
                activeFromPhase = BossPhase.P1,
                damageMultiplier = 1f
            };
            Assert.AreEqual(BossPhase.P1, spec.activeFromPhase);
        }

        [Test]
        public void FuryStrike_AppliedAtLowHp()
        {
            // HP 25% (≤30%) 시 FuryStrikeBonus 0.5 적용 → 28 × 1.5 = 42
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.TargetCurrentHpRatio = 0.25f;
            ctx.FuryStrikeBonus = 0.5f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(42f, r.FinalDamage, 0.001f);
        }

        [Test]
        public void FuryStrike_NotAppliedAboveThreshold()
        {
            var ctx = DamageContext.Default(90, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.TargetCurrentHpRatio = 0.5f; // 50% → 미발동
            ctx.FuryStrikeBonus = 0.5f;
            var r = DamageCalculator.Calculate(ctx);
            Assert.AreEqual(28f, r.FinalDamage, 0.001f);
        }
    }
}
