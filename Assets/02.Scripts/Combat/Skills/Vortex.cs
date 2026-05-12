using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 볼텍스 (원소 · A · Tier 5). 맵 중앙(0, 1)에 블랙홀 소환.
    /// 흡인 반경: 최대 10 × (1 - 0.9^Lv). 데미지 반경: 흡인의 50%.
    /// 0.5초 틱 데미지 공격력 30%. 지속 3초 고정.
    /// </summary>
    public class Vortex : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = 3f;
            float attractRadius = SkillFormula.Diminish(10f, 0.9f, level);
            float damageRadius = attractRadius * 0.5f;
            float attractForce = 8f;
            Vector2 center = new Vector2(0f, 1f);
            float tick = 0.5f;
            int totalHits = 0;

            float endTime = Time.time + duration;
            float nextDamageTick = Time.time;
            while (Time.time < endTime)
            {
                // 흡인 (몬스터+공)
                var hits = UnityEngine.Physics2D.OverlapCircleAll(center, attractRadius);
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    if (h.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        Vector2 dir = (center - (Vector2)h.transform.position);
                        if (dir.sqrMagnitude > 0.01f)
                        {
                            rb.AddForce(dir.normalized * attractForce, ForceMode2D.Force);
                        }
                    }
                }

                // 데미지 틱
                if (Time.time >= nextDamageTick)
                {
                    int hitCount = SpawnAOECircle(center, damageRadius, 0.3f, 0f, KnockbackTier.None, 0f, false);
                    totalHits += hitCount;
                    nextDamageTick = Time.time + tick;
                }

                try { await UniTask.Delay(System.TimeSpan.FromSeconds(0.05f), cancellationToken: ct); }
                catch (System.OperationCanceledException) { break; }
            }

            Debug.Log($"[Vortex] 발동 Lv.{level} | 흡인 R={attractRadius:F1}u, 데미지 R={damageRadius:F1}u, 총 히트 {totalHits}");
        }
    }
}
