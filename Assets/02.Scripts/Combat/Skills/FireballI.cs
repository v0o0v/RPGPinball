using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 파이어볼 I (원소 · A전환 · Tier 1).
    /// 터치 지점 중심 원형 마법 데미지 1회. 미스릴 공 보유 시 ×1.15.
    /// </summary>
    public class FireballI : ActiveSkillBase
    {
        [SerializeField] private int playerLevel = 1; // 마일스톤 3에서 PlayerData 연동

        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float radius = data != null ? data.radius : 1.5f;
            int hitsLeft = data != null && data.maxHits > 0 ? data.maxHits : 1;
            int totalHits = 0;

            foreach (var m in OverlapCircleMonsters(targetPos, radius))
            {
                if (hitsLeft <= 0) break;
                var result = ComputeSkillDamage(m, playerLevel);
                m.ApplyDamage(result);
                hitsLeft--;
                totalHits++;
            }

            Debug.Log($"[FireballI] 발동 @ {targetPos}, 반경 {radius}, 적중 {totalHits}");
            await UniTask.Yield(ct);
        }
    }
}
