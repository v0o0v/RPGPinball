using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat
{
    /// <summary>
    /// Damage_Formula.md 10단계 데미지 파이프라인.
    /// 마일스톤 2에서는 단계 [3] 코어, [5] 플리퍼 파생형, [6] 룬, [7] 타로를 패스스루로 둠.
    /// 슬롯만 미리 마련하고 마일스톤 3·6에서 채움.
    /// </summary>
    public static class DamageCalculator
    {
        public static DamageResult Calculate(in DamageContext ctx)
        {
            // [1] 기본 물리 데미지
            float damage = Constants.PlayerBaseDamage * (1f + ctx.PlayerLevel * Constants.LevelDamageScale);

            // [2] 스킬 트리 패시브 (합연산 → 곱연산, 3개째부터 합산 전환)
            float addPct = 0f;
            foreach (var a in ctx.AdditivePercents) addPct += a;
            damage *= 1f + addPct;

            float mulProduct = ApplyMultipliersWithStackLimit(ctx.MultiplierFactors);
            damage *= mulProduct;

            // [3] 코어 효과 — 마일스톤 3 이후 채움 (현재 패스스루)
            damage *= ctx.CoreMultiplier;

            // [4] 재질 보너스 — 미스릴 공은 마법만 ×1.15, 화산암 도트는 별도 계산이므로 여기서는 미스릴만 적용
            if (ctx.IsMithrilBall && ctx.DamageType == DamageType.Magic)
                damage *= Constants.MithrilMagicMultiplier;

            // [5] 플리퍼 파생형 — 마일스톤 6에서 채움
            damage *= ctx.FlipperDerivativeMultiplier;

            // [6] 룬 효과 — 마일스톤 6에서 채움
            damage *= ctx.RuneMultiplier;

            // [7] 타로카드 — 마일스톤 6에서 채움
            damage *= ctx.TarotMultiplier;

            // [8] 크리티컬 판정
            bool isCritical = false;
            float critChance = Mathf.Clamp01(ctx.CritChance);
            if (critChance > 0f && Random.value < critChance)
            {
                damage *= ctx.CritMultiplier > 0f ? ctx.CritMultiplier : Constants.CritMultiplierDefault;
                isCritical = true;
            }

            // [9] 방어력 / 마법 저항력 감산 (단순 차감 방식 — 향후 비례식으로 변경 가능)
            int reduction = ctx.DamageType == DamageType.Magic ? ctx.TargetMagicResist : ctx.TargetDefense;
            damage -= reduction;

            // [10] 최종 클램프
            if (damage < 1f) damage = 1f; // 최소 1 보장

            return new DamageResult
            {
                FinalDamage = damage,
                IsCritical = isCritical,
                DamageType = ctx.DamageType
            };
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
    /// 데미지 계산 입력. 마일스톤 2에서는 [1][2][4][8][9]만 활성. 나머지는 1.0 패스스루.
    /// </summary>
    public struct DamageContext
    {
        public int PlayerLevel;
        public DamageType DamageType;

        // 합연산 % 누계. 예: 0.25f = +25%
        public IReadOnlyList<float> AdditivePercents;
        // 곱연산 배율. 예: 1.3f = ×1.3
        public IReadOnlyList<float> MultiplierFactors;

        public bool IsMithrilBall;

        public float CoreMultiplier;            // 기본 1.0 (마일스톤 3)
        public float FlipperDerivativeMultiplier; // 기본 1.0 (마일스톤 6)
        public float RuneMultiplier;            // 기본 1.0 (마일스톤 6)
        public float TarotMultiplier;           // 기본 1.0 (마일스톤 6)

        public float CritChance;
        public float CritMultiplier;

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
                IsMithrilBall = false,
                CoreMultiplier = 1f,
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
