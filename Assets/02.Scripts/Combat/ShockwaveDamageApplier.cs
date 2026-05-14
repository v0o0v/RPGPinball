using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Enemy;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 충격파 플리퍼 2차 광역 데미지 디스패처. 본체 0.5x 마법 + 크리티컬 미적용.
    /// 풀링 가능한 임시 GameObject (M8 풀링 전까지는 즉시 Destroy).
    /// </summary>
    public static class ShockwaveDamageApplier
    {
        /// <summary>
        /// 위치 기준 반경 내 적에게 마법 광역 데미지 발행.
        /// </summary>
        public static int Trigger(Vector2 position, float radius, float damageMultiplier, bool isMagic, int playerLevel)
        {
            int hits = 0;
            var found = Physics2D.OverlapCircleAll(position, radius);
            for (int i = 0; i < found.Length; i++)
            {
                var mb = found[i].GetComponent<MonsterBase>() ?? found[i].GetComponentInParent<MonsterBase>();
                if (mb == null || mb.IsDead) continue;

                var ctx = DamageContext.Default(playerLevel, isMagic ? DamageType.Magic : DamageType.Physical);
                ctx.TargetDefense = mb.GetEffectiveDefense();
                ctx.TargetMagicResist = mb.GetEffectiveMagicResist();
                ctx.TargetCurrentHpRatio = mb.CurrentHpRatio;
                ctx.IsShockwaveSecondaryHit = true;
                ctx.FlipperDerivativeMultiplier = damageMultiplier;

                var result = DamageCalculator.Calculate(ctx);
                mb.ApplyDamage(result);
                hits++;
            }
            return hits;
        }
    }
}
