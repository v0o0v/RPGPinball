using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat
{
    /// <summary>
    /// Damage_Formula.md 10단계 데미지 파이프라인.
    /// 마일스톤 3에서 단계 [2]·[3] 본격 활성. [5][6][7]은 마일스톤 6 인계.
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Calculate(in DamageContext ctx)
        {
            // [1] 기본 물리 데미지
            float damage = Constants.PlayerBaseDamage * (1f + ctx.PlayerLevel * Constants.LevelDamageScale);

            // [2] 스킬 트리 패시브 — SkillTreeManager가 ctx 빌드 시 합/곱 슬롯에 주입
            // 합연산 (콤보 스트라이크·하이퍼 콤보·분노의 일격 등 컨텍스트 기반 보너스도 포함)
            float addPct = 0f;
            if (ctx.AdditivePercents != null)
            {
                foreach (var a in ctx.AdditivePercents) addPct += a;
            }
            // 컨텍스트 기반 합연산: 콤보 스트라이크
            if (ctx.ComboStrikePerStack > 0f && ctx.ComboCount > 0)
            {
                int effectiveStacks = Mathf.Min(ctx.ComboCount, ctx.ComboMaxStack > 0 ? ctx.ComboMaxStack : 10);
                addPct += ctx.ComboStrikePerStack * effectiveStacks;
            }
            // 분노의 일격 (HP 30% 이하)
            if (ctx.FuryStrikeBonus > 0f && ctx.TargetCurrentHpRatio > 0f && ctx.TargetCurrentHpRatio <= 0.3f)
            {
                addPct += ctx.FuryStrikeBonus;
            }
            damage *= 1f + addPct;

            // 곱연산 (관성 돌파 I 속도 비례, 플리퍼 스매시 등 컨텍스트 기반 곱도 포함)
            var mulList = BuildMultiplierList(ctx);
            float mulProduct = ApplyMultipliersWithStackLimit(mulList);
            damage *= mulProduct;

            // [3] 코어 효과
            damage *= ctx.CoreMultiplier;
            // 가속 코어: 속도 비례 추가 배율
            if (ctx.AccelerationCoreCoeff > 0f && ctx.BallSpeed > 0f)
            {
                float speedRatio = Mathf.Clamp01(ctx.BallSpeed / Constants.BallMaxSpeed);
                damage *= 1f + speedRatio * ctx.AccelerationCoreCoeff;
            }
            // 분열 코어: 분열 공 데미지 페널티 완화
            if (ctx.IsSplitBall)
            {
                float splitMul = 0.8f + ctx.SplitCoreRelief;
                damage *= Mathf.Clamp(splitMul, 0.8f, 1.0f);
            }

            // [4] 재질 보너스
            if (ctx.BallMaterial == BallMaterial.Mithril && ctx.DamageType == DamageType.Magic)
                damage *= Constants.MithrilMagicMultiplier;
            // 호환: IsMithrilBall 플래그도 지원
            else if (ctx.IsMithrilBall && ctx.DamageType == DamageType.Magic)
                damage *= Constants.MithrilMagicMultiplier;

            // [5] 플리퍼 파생형 — 마일스톤 6에서 채움
            damage *= ctx.FlipperDerivativeMultiplier;

            // [6] 룬 효과 — 마일스톤 6에서 채움
            damage *= ctx.RuneMultiplier;

            // [7] 타로카드 — 마일스톤 6에서 채움
            damage *= ctx.TarotMultiplier;

            // [8] 크리티컬 판정
            bool isCritical = false;
            float critChance = Mathf.Clamp01(ctx.CritChance + ctx.CritChanceBonus);
            // 약점 부위 타격 시 크리티컬 확률 추가
            if (ctx.IsWeakPointHit) critChance += ctx.WeakPointCritBonus;
            critChance = Mathf.Clamp01(critChance);

            if (critChance > 0f && Random.value < critChance)
            {
                damage *= ctx.CritMultiplier > 0f ? ctx.CritMultiplier : Constants.CritMultiplierDefault;
                isCritical = true;
            }

            // [9] 방어력 / 마법 저항력 감산 (아머 크래시 적용)
            int reduction = ctx.DamageType == DamageType.Magic ? ctx.TargetMagicResist : ctx.TargetDefense;
            float effectiveReduction = reduction * (1f - Mathf.Clamp01(ctx.ArmorReductionPercent));
            damage -= effectiveReduction;

            // [10] 최종 클램프
            if (damage < 1f) damage = 1f; // 최소 1 보장

            return new DamageResult
            {
                FinalDamage = damage,
                IsCritical = isCritical,
                DamageType = ctx.DamageType
            };
        }

        // 곱연산 배열 구축 (정적 입력 + 컨텍스트 기반 곱연산)
        private static List<float> BuildMultiplierList(in DamageContext ctx)
        {
            var result = new List<float>();
            if (ctx.MultiplierFactors != null)
            {
                foreach (var f in ctx.MultiplierFactors)
                {
                    if (f != 1f) result.Add(f);
                }
            }
            // 관성 돌파 I: BallSpeed 비례 (속도가 BallMinSpeed 초과 시 곱연산 활성)
            if (ctx.InertiaBreakFactor > 1f && ctx.BallSpeed > Constants.BallMinSpeed)
            {
                // 속도가 빠를수록 배율의 효력이 점진 증가
                float speedRatio = Mathf.Clamp01((ctx.BallSpeed - Constants.BallMinSpeed) / (Constants.BallMaxSpeed - Constants.BallMinSpeed));
                float activatedFactor = 1f + (ctx.InertiaBreakFactor - 1f) * speedRatio;
                if (activatedFactor != 1f) result.Add(activatedFactor);
            }
            // 플리퍼 스매시: 직전 0.5초 내 플리퍼 충돌 후 다음 1회 발동
            if (ctx.FlipperSmashFactor > 1f && ctx.IsAfterFlipperHit)
            {
                result.Add(ctx.FlipperSmashFactor);
            }
            return result;
        }

        // 동일 카테고리의 곱연산이 3개 이상이면 3번째부터 (factor-1)을 합산해 합연산으로 전환
        private static float ApplyMultipliersWithStackLimit(IReadOnlyList<float> factors)
        {
            if (factors == null || factors.Count == 0) return 1f;

            float product = 1f;
            float overflowAdd = 0f;
            int applied = 0;

            for (int i = 0; i < factors.Count; i++)
            {
                if (applied < Constants.MultiplierStackLimit)
                {
                    product *= factors[i];
                    applied++;
                }
                else
                {
                    // 3개째부터 합연산 전환: (배율 - 1)을 합산
                    overflowAdd += factors[i] - 1f;
                }
            }

            return product * (1f + overflowAdd);
        }
    }

    /// <summary>
    /// 데미지 계산 입력. 마일스톤 3에서 컨텍스트 기반 필드 7종 추가.
    /// </summary>
    public struct DamageContext
    {
        public int PlayerLevel;
        public DamageType DamageType;

        // 합연산 % 누계. 예: 0.25f = +25%
        public IReadOnlyList<float> AdditivePercents;
        // 곱연산 배율. 예: 1.3f = ×1.3
        public IReadOnlyList<float> MultiplierFactors;

        // ── 마일스톤 3 신규 필드 ────────────────────────────
        public BallMaterial BallMaterial;
        public float BallSpeed;             // 관성 돌파 I 계산용
        public int ComboCount;              // 콤보 스트라이크 계산용
        public bool IsAfterFlipperHit;      // 플리퍼 스매시 발동 조건
        public bool IsWeakPointHit;         // 약점 부위 타격 (마일스톤 4 본격)
        public float TargetCurrentHpRatio;  // 분노의 일격 (≤ 0.3)
        public int StackedFireBurns;        // 파이어볼 II 화상 중첩
        public bool IsSplitBall;            // 분열 공 여부 (분열 코어 계산)

        // 호환: 마일스톤 2 코드와의 호환을 위해 유지 (Wood/Mithril 매핑)
        public bool IsMithrilBall;

        // ── SkillTreeManager가 채우는 패시브 캐시 값 ────────
        public float ComboStrikePerStack;
        public int ComboMaxStack;
        public float FuryStrikeBonus;
        public float InertiaBreakFactor; // 1.0 + Lv 보너스 (전체 활성 배율)
        public float FlipperSmashFactor;
        public float CritChanceBonus;
        public float WeakPointCritBonus;
        public float ArmorReductionPercent;

        // ── 코어 ─────────────────────────────────────────────
        public float CoreMultiplier;            // 기본 1.0 (범용 슬롯)
        public float AccelerationCoreCoeff;     // 가속 코어 계수
        public float SplitCoreRelief;           // 분열 코어 페널티 완화

        // ── 마일스톤 6 슬롯 ─────────────────────────────────
        public float FlipperDerivativeMultiplier;
        public float RuneMultiplier;
        public float TarotMultiplier;

        // ── 크리티컬 ────────────────────────────────────────
        public float CritChance;
        public float CritMultiplier;

        // ── 적 ───────────────────────────────────────────────
        public int TargetDefense;
        public int TargetMagicResist;

        /// <summary>기본값 1.0 슬롯이 채워진 컨텍스트를 생성.</summary>
        public static DamageContext Default(int playerLevel, DamageType type)
        {
            return new DamageContext
            {
                PlayerLevel = playerLevel,
                DamageType = type,
                AdditivePercents = System.Array.Empty<float>(),
                MultiplierFactors = System.Array.Empty<float>(),
                BallMaterial = BallMaterial.Wood,
                BallSpeed = 0f,
                ComboCount = 0,
                IsAfterFlipperHit = false,
                IsWeakPointHit = false,
                TargetCurrentHpRatio = 1f,
                StackedFireBurns = 0,
                IsSplitBall = false,
                IsMithrilBall = false,
                ComboStrikePerStack = 0f,
                ComboMaxStack = 10,
                FuryStrikeBonus = 0f,
                InertiaBreakFactor = 1f,
                FlipperSmashFactor = 1f,
                CritChanceBonus = 0f,
                WeakPointCritBonus = 0f,
                ArmorReductionPercent = 0f,
                CoreMultiplier = 1f,
                AccelerationCoreCoeff = 0f,
                SplitCoreRelief = 0f,
                FlipperDerivativeMultiplier = 1f,
                RuneMultiplier = 1f,
                TarotMultiplier = 1f,
                CritChance = Constants.CritChanceDefault,
                CritMultiplier = Constants.CritMultiplierDefault,
                TargetDefense = 0,
                TargetMagicResist = 0
            };
        }
    }

    public struct DamageResult
    {
        public float FinalDamage;
        public bool IsCritical;
        public DamageType DamageType;
    }
}
