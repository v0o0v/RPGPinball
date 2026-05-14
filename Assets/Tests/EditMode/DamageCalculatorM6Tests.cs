using NUnit.Framework;
using RPGPinball.Combat;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 6 DamageCalculator [5][6][7] 활성화 검증.
    /// </summary>
    public class DamageCalculatorM6Tests
    {
        // ── [5] 플리퍼 파생형 ─────────────────────────────────

        [Test]
        public void Basic_FlipperVariant_DealsExpectedBaseDamage()
        {
            var ctx = DamageContext.Default(1, DamageType.Physical);
            ctx.CritChance = 0f;
            ctx.FlipperVariantId = FlipperVariantId.Basic;
            var result = DamageCalculator.Calculate(ctx);
            Assert.IsTrue(result.FinalDamage > 0f);
        }

        [Test]
        public void Spike_FlipperDebuff_ReducesEffectiveDef()
        {
            // 충분히 높은 base damage 가 되도록 PlayerLevel 100 + AdditivePercent 사용
            var ctxNoDebuff = DamageContext.Default(100, DamageType.Physical);
            ctxNoDebuff.AdditivePercents = new[] { 5f }; // 데미지 큰 폭으로 키움
            ctxNoDebuff.TargetDefense = 5;
            ctxNoDebuff.IsSpikeDebuffActive = false;
            // 크리티컬 변동 제거: CritChance=0
            ctxNoDebuff.CritChance = 0f;
            var r1 = DamageCalculator.Calculate(ctxNoDebuff);

            var ctxDebuff = DamageContext.Default(100, DamageType.Physical);
            ctxDebuff.AdditivePercents = new[] { 5f };
            ctxDebuff.TargetDefense = 5;
            ctxDebuff.IsSpikeDebuffActive = true;
            ctxDebuff.CritChance = 0f;
            var r2 = DamageCalculator.Calculate(ctxDebuff);

            Assert.IsTrue(r2.FinalDamage > r1.FinalDamage, "Spike 디버프 시 데미지가 더 커야 함");
        }

        [Test]
        public void Shockwave_SecondaryHit_DoesNotCriticallyHit()
        {
            // CritChance 1.0 으로 강제해도 충격파 2차는 IsShockwaveSecondaryHit=true 라 미적용
            var ctx = DamageContext.Default(1, DamageType.Physical);
            ctx.CritChance = 1f;
            ctx.IsShockwaveSecondaryHit = true;
            var result = DamageCalculator.Calculate(ctx);
            Assert.IsFalse(result.IsCritical);
        }

        // ── [6] 룬 효과 ────────────────────────────────────────

        private DamageContext MakeCtx(int level = 100, DamageType type = DamageType.Physical)
        {
            var c = DamageContext.Default(level, type);
            c.AdditivePercents = new[] { 5f };
            c.CritChance = 0f;
            return c;
        }

        [Test]
        public void ExecutionerRune_DoublesDamage_WhenHpBelow30Percent()
        {
            var ctx = MakeCtx();
            ctx.TargetCurrentHpRatio = 0.25f;
            ctx.EquippedRuneIds = new[] { RuneId.Executioner };
            ctx.EquippedRuneGrades = new[] { RuneGrade.Normal };

            var ctxNoRune = MakeCtx();
            ctxNoRune.TargetCurrentHpRatio = 0.25f;

            var withRune = DamageCalculator.Calculate(ctx).FinalDamage;
            var noRune = DamageCalculator.Calculate(ctxNoRune).FinalDamage;

            Assert.That(withRune, Is.GreaterThan(noRune * 1.5f));
        }

        [Test]
        public void ExecutionerRune_LegendaryGrade_AmplifiesByGradeMultiplier()
        {
            var ctxNormal = MakeCtx();
            ctxNormal.TargetCurrentHpRatio = 0.25f;
            ctxNormal.EquippedRuneIds = new[] { RuneId.Executioner };
            ctxNormal.EquippedRuneGrades = new[] { RuneGrade.Normal };

            var ctxLegend = MakeCtx();
            ctxLegend.TargetCurrentHpRatio = 0.25f;
            ctxLegend.EquippedRuneIds = new[] { RuneId.Executioner };
            ctxLegend.EquippedRuneGrades = new[] { RuneGrade.Legendary };

            var normal = DamageCalculator.Calculate(ctxNormal).FinalDamage;
            var legend = DamageCalculator.Calculate(ctxLegend).FinalDamage;

            Assert.That(legend, Is.GreaterThan(normal * 1.5f), "전설 등급(×2.25)이 일반(×1.0)보다 1.5배 이상 강해야 함");
        }

        [Test]
        public void AdversityRune_Boosts_WhenTimeBelow60Seconds()
        {
            var ctxLow = MakeCtx();
            ctxLow.RemainingTimeSeconds = 30f;
            ctxLow.EquippedRuneIds = new[] { RuneId.Adversity };
            ctxLow.EquippedRuneGrades = new[] { RuneGrade.Normal };

            var ctxHigh = MakeCtx();
            ctxHigh.RemainingTimeSeconds = 120f;
            ctxHigh.EquippedRuneIds = new[] { RuneId.Adversity };
            ctxHigh.EquippedRuneGrades = new[] { RuneGrade.Normal };

            Assert.That(DamageCalculator.Calculate(ctxLow).FinalDamage,
                Is.GreaterThan(DamageCalculator.Calculate(ctxHigh).FinalDamage * 1.2f));
        }

        // ── [7] 타로카드 ──────────────────────────────────────

        [Test]
        public void TarotR05_ElementalAmplifier_BoostsMagicDamage()
        {
            // 기본 마법 데미지가 충분히 크도록 PlayerLevel 100 + 추가 합연산
            var ctxBase = DamageContext.Default(100, DamageType.Magic);
            ctxBase.AdditivePercents = new[] { 5f };
            ctxBase.CritChance = 0f;
            var ctxAmp = DamageContext.Default(100, DamageType.Magic);
            ctxAmp.AdditivePercents = new[] { 5f };
            ctxAmp.CritChance = 0f;
            ctxAmp.EquippedTarotCardIds = new[] { TarotCardId.R05_ElementalAmplifier };

            var b = DamageCalculator.Calculate(ctxBase).FinalDamage;
            var a = DamageCalculator.Calculate(ctxAmp).FinalDamage;
            Assert.That(a, Is.GreaterThan(b * 1.1f));
        }

        [Test]
        public void TarotL03_DoomSeal_BoostsDamageOnLowHpTarget()
        {
            var ctxLow = MakeCtx();
            ctxLow.TargetCurrentHpRatio = 0.15f;
            ctxLow.EquippedTarotCardIds = new[] { TarotCardId.L03_DoomSeal };

            var ctxHigh = MakeCtx();
            ctxHigh.TargetCurrentHpRatio = 0.5f;
            ctxHigh.EquippedTarotCardIds = new[] { TarotCardId.L03_DoomSeal };

            Assert.That(DamageCalculator.Calculate(ctxLow).FinalDamage,
                Is.GreaterThan(DamageCalculator.Calculate(ctxHigh).FinalDamage * 1.3f));
        }

        // ── 글로벌 배율 (광란의 부적) ─────────────────────────

        [Test]
        public void GlobalDamageMultiplier_FrenzyCharm_DoublesDamage()
        {
            var ctxNormal = MakeCtx();
            var ctxFrenzy = MakeCtx();
            ctxFrenzy.GlobalDamageMultiplier = 2.0f;

            var n = DamageCalculator.Calculate(ctxNormal).FinalDamage;
            var f = DamageCalculator.Calculate(ctxFrenzy).FinalDamage;
            Assert.That(f, Is.GreaterThan(n * 1.8f));
        }
    }
}
