using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 넉백 적용. KnockbackTier에 따라 강도 조정. 궁극기는 Absolute 외 전부 관통.
    /// (Implementation_Plan.md 표기는 NockbackSystem이나 오타로 판단해 KnockbackSystem으로 작성.)
    /// </summary>
    public static class KnockbackSystem
    {
        public static void Apply(Rigidbody2D target, Vector2 direction, float force, KnockbackTier tier, bool isUltimate)
        {
            if (target == null) return;
            float multiplier = GetMultiplier(tier, isUltimate);
            if (multiplier <= 0f) return;
            target.AddForce(direction.normalized * force * multiplier, ForceMode2D.Impulse);
        }

        public static float GetMultiplier(KnockbackTier tier, bool isUltimate)
        {
            switch (tier)
            {
                case KnockbackTier.None: return 1f;
                case KnockbackTier.Resist: return 0.5f;
                case KnockbackTier.Immune: return isUltimate ? 1f : 0f;
                case KnockbackTier.Absolute: return 0f;
                default: return 1f;
            }
        }
    }
}
