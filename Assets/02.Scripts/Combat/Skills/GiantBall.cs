using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 자이언트 볼 (파괴 · A · Tier 4).
    /// 공이 일시적으로 거대화하여 충돌 시 광역 물리 데미지.
    /// 마일스톤 2에서는 단순 폭발 형태로만 구현. 본 효과는 마일스톤 3 이후 보강.
    /// </summary>
    public class GiantBall : ActiveSkillBase
    {
        [SerializeField] private int playerLevel = 1;

        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float radius = data != null && data.radius > 0f ? data.radius : 2.0f;
            int hitsLeft = data != null && data.maxHits > 0 ? data.maxHits : 3;
            int totalHits = 0;

            foreach (var m in OverlapCircleMonsters(targetPos, radius))
            {
                if (hitsLeft <= 0) break;
                var result = ComputeSkillDamage(m, playerLevel);
                m.ApplyDamage(result);

                // 넉백 적용
                if (data != null && data.dealsKnockback)
                {
                    var rb = m.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        Vector2 dir = (Vector2)m.transform.position - targetPos;
                        KnockbackSystem.Apply(rb, dir, data.knockbackForce, m.Data.knockbackTier, data.isUltimate);
                    }
                }
                hitsLeft--;
                totalHits++;
            }
            Debug.Log($"[GiantBall] 발동 @ {targetPos}, 반경 {radius}, 적중 {totalHits}");
            await UniTask.Yield(ct);
        }
    }
}
