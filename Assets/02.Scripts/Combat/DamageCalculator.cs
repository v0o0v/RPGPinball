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

            // [5] 플리퍼 파생형
            damage *= ctx.FlipperDerivativeMultiplier;
            // 충격파 플리퍼 2차 광역 데미지: 본체 0.5x 곱연산은 외부 ShockwaveDamageApplier 가 컨텍스트 빌드 시 직접 곱해서 넣음
            // (여기서는 슬롯 값 그대로 반영)

            // [6] 룬 효과 — 처형자(HP≤30%), 역경(시간≤60), 등급 multiplier 합성
            float runeMul = ctx.RuneMultiplier;
            if (ctx.EquippedRuneIds != null && ctx.EquippedRuneIds.Length > 0)
            {
                for (int i = 0; i < ctx.EquippedRuneIds.Length; i++)
                {
                    var rId = ctx.EquippedRuneIds[i];
                    var grade = (ctx.EquippedRuneGrades != null && i < ctx.EquippedRuneGrades.Length)
                        ? ctx.EquippedRuneGrades[i]
                        : RuneGrade.Normal;
                    float gradeMul = grade == RuneGrade.Normal ? 1.0f
                                   : grade == RuneGrade.Rare ? 1.5f
                                   : 2.25f;

                    switch (rId)
                    {
                        case RuneId.Executioner:
                            if (ctx.TargetCurrentHpRatio > 0f && ctx.TargetCurrentHpRatio <= 0.3f)
                                runeMul *= 2.0f * gradeMul;
                            break;
                        case RuneId.Adversity:
                            if (ctx.RemainingTimeSeconds <= 60f)
                                runeMul *= 1.5f * gradeMul;
                            break;
                        // Chain/Spread/Pierce/Homing/Element 전환은 ActiveSkillBase 측 합성
                        default: break;
                    }
                }
            }
            damage *= runeMul;

            // [7] 타로카드 — 단계 분기
            float tarotMul = ctx.TarotMultiplier;
            if (ctx.EquippedTarotCardIds != null && ctx.EquippedTarotCardIds.Length > 0)
            {
                foreach (var cId in ctx.EquippedTarotCardIds)
                {
                    switch (cId)
                    {
                        case TarotCardId.C07_WarriorBracelet:
                            if (ctx.DamageType == DamageType.Physical) damage *= 1.08f;
                            break;
                        case TarotCardId.R05_ElementalAmplifier:
                            if (ctx.DamageType == DamageType.Magic) tarotMul *= 1.15f;
                            break;
                        case TarotCardId.R08_SteelWill:
                            if (ctx.RemainingTimeSeconds <= 60f) tarotMul *= 1.20f;
                            break;
                        case TarotCardId.L03_DoomSeal:
                            if (ctx.TargetCurrentHpRatio > 0f && ctx.TargetCurrentHpRatio <= 0.2f) tarotMul *= 1.50f;
                            break;
                        // R02/R04 등은 별도 시스템 적용 (크리티컬·플리퍼 쿨타임 등)
                        default: break;
                    }
                }
            }
            damage *= tarotMul;

            // 전역 데미지 배율 (광란의 부적 등)
            if (ctx.GlobalDamageMultiplier > 0f && ctx.GlobalDamageMultiplier != 1f)
            {
                damage *= ctx.GlobalDamageMultiplier;
            }

            // [8] 크리티컬 판정 (충격파 2차 데미지는 미적용)
            bool isCritical = false;
            if (!ctx.IsShockwaveSecondaryHit)
            {
                float critChance = Mathf.Clamp01(ctx.CritChance + ctx.CritChanceBonus);
                // 약점 부위 타격 시 크리티컬 확률 추가
                if (ctx.IsWeakPointHit) critChance += ctx.WeakPointCritBonus;
                critChance = Mathf.Clamp01(critChance);

                if (critChance > 0f && Random.value < critChance)
                {
                    damage *= ctx.CritMultiplier > 0f ? ctx.CritMultiplier : Constants.CritMultiplierDefault;
                    isCritical = true;
                }
            }

            // [9] 방어력 / 마법 저항력 감산 (아머 크래시·가시 플리퍼 DEF 디버프 적용)
            int reduction = ctx.DamageType == DamageType.Magic ? ctx.TargetMagicResist : ctx.TargetDefense;
            float armorReduction = Mathf.Clamp01(ctx.ArmorReductionPercent);
            if (ctx.IsSpikeDebuffActive)
            {
                // 가시 플리퍼 디버프: DEF -10% (방어력 감산값의 10% 추가 감산)
                armorReduction = Mathf.Clamp01(armorReduction + 0.10f);
            }
            float effectiveReduction = reduction * (1f - armorReduction);
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

        // 플리퍼 파생형
        public FlipperVariantId FlipperVariantId;
        public bool IsSpikeDebuffActive;           // 가시 플리퍼 디버프 5초 적용 중인지
        public bool IsShockwaveSecondaryHit;       // 충격파로 발생한 2차 데미지(크리티컬 미적용)
        // 룬/타로 ID 목록 (DamageCalculator/ActiveSkillBase가 합성용으로 사용)
        public RuneId[] EquippedRuneIds;
        public RuneGrade[] EquippedRuneGrades;
        public TarotCardId[] EquippedTarotCardIds;
        // 룬 효과로 채워지는 원소 오버라이드 / 마나 비용 배율 (스킬 ActiveSkillBase가 사용)
        public ElementOverride ElementOverride;
        public float ManaCostMultiplier;
        // 타이머 잔여 시간 (역경의 룬 / R-08 강철의 의지 등)
        public float RemainingTimeSeconds;
        // 전역 데미지 배율 (광란의 부적 등)
        public float GlobalDamageMultiplier;

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
                TargetMagicResist = 0,
                FlipperVariantId = FlipperVariantId.Basic,
                IsSpikeDebuffActive = false,
                IsShockwaveSecondaryHit = false,
                EquippedRuneIds = System.Array.Empty<RuneId>(),
                EquippedRuneGrades = System.Array.Empty<RuneGrade>(),
                EquippedTarotCardIds = System.Array.Empty<TarotCardId>(),
                ElementOverride = ElementOverride.None,
                ManaCostMultiplier = 1f,
                RemainingTimeSeconds = 999f,
                GlobalDamageMultiplier = 1f
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
