using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Physics;

namespace RPGPinball.Enemy.BossAI
{
    /// <summary>
    /// 보스 약점 부위 콜라이더. BossBase가 weakPoints[]만큼 자식으로 생성.
    /// 공 충돌 시 부모 BossBase에 약점 타격 알림.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class WeakPointHitbox : MonoBehaviour
    {
        private WeakPointSpec spec;
        private BossBase parentBoss;

        public WeakPointSpec Spec => spec;

        public void Initialize(BossBase boss, WeakPointSpec s)
        {
            parentBoss = boss;
            spec = s;
            var col = GetComponent<CircleCollider2D>();
            col.radius = Mathf.Max(0.05f, s.radius);
            col.isTrigger = false; // 공이 반사되도록 비트리거
            transform.localPosition = s.localOffset;
            gameObject.tag = Constants.TagWeakPoint;
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (parentBoss == null || parentBoss.IsDead) return;
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;

            // 현재 페이즈 활성 여부 확인
            if (!parentBoss.IsWeakPointActive(spec)) return;

            // 본체 데미지 컨텍스트 빌드 (BossBase가 약점 가중치 적용)
            int playerLv = LevelSystem.Instance != null ? LevelSystem.Instance.Level : 1;
            var ctx = DamageContext.Default(playerLv, DamageType.Physical);
            ctx.TargetDefense = spec.ignoresDefense ? 0 : parentBoss.GetEffectiveDefense();
            ctx.TargetMagicResist = parentBoss.Data != null ? parentBoss.Data.magicResist : 0;
            ctx.BallSpeed = col.relativeVelocity.magnitude;
            ctx.TargetCurrentHpRatio = parentBoss.CurrentHpRatio;
            ctx.IsWeakPointHit = true;

            // SkillTreeManager 패시브 주입
            if (SkillTreeManager.Instance != null)
            {
                var stm = SkillTreeManager.Instance;
                ctx.AdditivePercents = new[] { stm.GetDamageAddPercent(DamageType.Physical) };
                var baseFactors = stm.GetDamageMultiplierFactors(DamageType.Physical);
                // 약점 가중치를 추가 배율로 삽입
                if (spec.damageMultiplier > 0f && !Mathf.Approximately(spec.damageMultiplier, 1f))
                {
                    int n = baseFactors != null ? baseFactors.Count : 0;
                    var merged = new float[n + 1];
                    for (int i = 0; i < n; i++) merged[i] = baseFactors[i];
                    merged[n] = spec.damageMultiplier;
                    ctx.MultiplierFactors = merged;
                }
                else
                {
                    ctx.MultiplierFactors = baseFactors;
                }
                ctx.ComboStrikePerStack = stm.GetComboStrikePerStack();
                ctx.ComboMaxStack = stm.GetMaxComboStack();
                ctx.FuryStrikeBonus = stm.GetFuryStrikeBonus();
                ctx.InertiaBreakFactor = stm.GetInertiaBreak1Factor();
                ctx.FlipperSmashFactor = stm.GetFlipperSmashFactor();
                ctx.CritChanceBonus = stm.GetCritChanceBonus();
                ctx.ArmorReductionPercent = stm.GetArmorReductionPercent();
            }
            if (ComboSystem.Instance != null)
            {
                ctx.ComboCount = ComboSystem.Instance.Combo;
            }
            var ball = col.gameObject.GetComponent<BallController>();
            if (ball != null)
            {
                ctx.BallMaterial = ball.Material;
                ctx.IsMithrilBall = ball.Material == BallMaterial.Mithril;
                ctx.IsSplitBall = ball.IsSplitBall;
            }

            var result = DamageCalculator.Calculate(ctx);
            parentBoss.ApplyDamage(result);
        }
    }
}
