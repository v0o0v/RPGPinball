using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 메테오 스트라이크 (파괴 · A궁 · Tier 6). 화면 상단에서 보스로 직선 낙하 + 착탄 폭발.
    /// 히트 2회: 관통 200% + 폭발 300% + Lv × 50%. 보스 포함 넉백 5u.
    /// </summary>
    public class MeteorStrike : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Vector2 startPos = new Vector2(targetPos.x, 6f);
            Vector2 impact = FindBossOrNearestMonster(targetPos);

            // 낙하 경로: 폭 1.0 × 높이 12 (세그먼트 전체) → OverlapBox 한 번
            Vector2 corridorCenter = new Vector2(impact.x, (startPos.y + impact.y) * 0.5f);
            Vector2 corridorSize = new Vector2(1.0f, Mathf.Abs(startPos.y - impact.y) + 0.5f);
            int penetrationHits = SpawnAOEBox(corridorCenter, corridorSize, 0f, 2.0f, 0f, KnockbackTier.Resist, Constants.KnockbackDistanceStrong, true);

            // 짧은 시각 지연 (0.1초)
            try { await UniTask.Delay(System.TimeSpan.FromSeconds(0.1f), cancellationToken: ct); }
            catch (System.OperationCanceledException) { return; }

            // 착탄 폭발: 반경 3.5
            int explosionHits = SpawnAOECircle(impact, 3.5f, 3.0f, 0.5f, KnockbackTier.Resist, Constants.KnockbackDistanceUltimate, true);

            Debug.Log($"[MeteorStrike] 발동 Lv.{level} | 관통 {penetrationHits}대 + 폭발 {explosionHits}대 → 충격점 ({impact.x:F1},{impact.y:F1})");
        }
    }
}
