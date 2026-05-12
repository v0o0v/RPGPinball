using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Enemy;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 리바운드 실드 (제어 · A · Tier 5). 플리퍼 전방 반원 4u 내 투사체 반사.
    /// 반사 데미지: 적 투사체 × (1 + Lv × 0.3)
    /// 지속: 2.0 + Lv × 0.2s
    /// </summary>
    public class ReboundShield : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 2f + level * 0.2f;
            float reflectMultiplier = 1f + level * 0.3f;
            float radius = data != null ? data.radius : 4f;

            int totalReflected = 0;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                var hits = UnityEngine.Physics2D.OverlapCircleAll(targetPos, radius);
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    var proj = h.GetComponent<ProjectileBase>();
                    if (proj == null) continue;
                    if (proj.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        rb.linearVelocity = -rb.linearVelocity * reflectMultiplier;
                    }
                    totalReflected++;
                }
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), cancellationToken: ct);
            }

            Debug.Log($"[ReboundShield] 발동 Lv.{level}, 반사 {totalReflected}회, 배율 ×{reflectMultiplier:F1}");
        }
    }
}
