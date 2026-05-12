using UnityEngine;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 스킬 트리 공식 정적 유틸. Skill_Tree_Formulas.md 4가지 연산 규칙 구현.
    /// 1) 합연산(Linear), 2) 점감(Diminish), 3) 중첩 한도(StackLimit), 4) 하드캡(HardCap*).
    /// </summary>
    public static class SkillFormula
    {
        /// <summary>
        /// 합연산: baseVal + perLv × level
        /// 예) 강철구 I: `5% + (Lv × 1.5%)` → Linear(0.05, 0.015, Lv)
        /// </summary>
        public static float Linear(float baseVal, float perLv, int level)
        {
            if (level < 0) level = 0;
            return baseVal + perLv * level;
        }

        /// <summary>
        /// 점감: max × (1 - rate^Lv)
        /// 예) 플리퍼 경량화 I: `최대 40% × (1 - 0.95^Lv)` → Diminish(0.4, 0.95, Lv)
        /// </summary>
        public static float Diminish(float max, float rate, int level)
        {
            if (level <= 0) return 0f;
            return max * (1f - Mathf.Pow(rate, level));
        }

        /// <summary>
        /// 중첩 한도: baseStack + (level / levelStep) × perLevel
        /// 예) 트리플 드로우: `기본 3 + Lv 2당 +1` → StackLimit(3, 1, Lv, 2)
        /// </summary>
        public static int StackLimit(int baseStack, int perLevel, int level, int levelStep = 1)
        {
            if (level <= 0 || levelStep <= 0) return baseStack;
            int steps = level / levelStep;
            return baseStack + steps * perLevel;
        }

        /// <summary>하드캡 하한: 계산값이 minHardCap보다 작아지지 않도록 클램프.</summary>
        public static float HardCapMin(float computed, float minHardCap)
        {
            return Mathf.Max(computed, minHardCap);
        }

        /// <summary>하드캡 상한.</summary>
        public static int HardCapMax(int computed, int maxHardCap)
        {
            return Mathf.Min(computed, maxHardCap);
        }

        /// <summary>쿨타임 감소율 합산 (곱연산): ∏(1 - r_i).</summary>
        public static float CooldownMultiplier(float[] reductionRatios)
        {
            if (reductionRatios == null || reductionRatios.Length == 0) return 1f;
            float mult = 1f;
            for (int i = 0; i < reductionRatios.Length; i++)
            {
                mult *= (1f - Mathf.Clamp01(reductionRatios[i]));
            }
            return mult;
        }
    }
}
