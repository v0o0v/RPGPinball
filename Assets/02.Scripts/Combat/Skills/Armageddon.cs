using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 아마겟돈 (원소 · A궁 · Tier 6). 화면 중앙 반경 6 원형 폭발 0.5초 간격 3파.
    /// 각 파 데미지 267% + Lv × 27% (총합 ≈ 800% + Lv × 80%).
    /// 보스 포함 모든 적 넉백 4u (궁극기 한정 관통). 각 파마다 무작위 상태이상.
    /// </summary>
    public class Armageddon : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Vector2 center = new Vector2(0f, 1f);
            int totalHits = 0;
            for (int wave = 0; wave < 3; wave++)
            {
                int hits = SpawnAOECircle(center, 6.0f, 2.67f, 0.27f, KnockbackTier.Immune, Constants.KnockbackDistanceUltimate, true);
                totalHits += hits;
                // 무작위 상태이상
                foreach (var m in OverlapCircleMonsters(center, 6.0f))
                {
                    m.ApplyRandomStatus();
                }
                if (wave < 2)
                {
                    try { await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: ct); }
                    catch (System.OperationCanceledException) { break; }
                }
            }
            Debug.Log($"[Armageddon] 발동 Lv.{level} | 3파 폭발, 총 {totalHits}히트 (8u 반경 4u 넉백)");
        }
    }
}
