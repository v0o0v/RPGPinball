using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 제로 블레이드 (파괴 · A궁 · Tier 6). 공 궤적에 칼날 잔상.
    /// 0.1초 틱마다 공 위치 ±0.4 사각 판정 → 공격력 40%. 지속 2 + Lv × 0.2s.
    /// </summary>
    public class ZeroBlade : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 2f + level * 0.2f;
            float tick = 0.1f;
            int totalHits = 0;
            float endTime = Time.time + duration;
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            while (Time.time < endTime)
            {
                foreach (var b in balls)
                {
                    if (b == null || !b.gameObject.activeInHierarchy) continue;
                    Vector2 pos = b.transform.position;
                    int hits = SpawnAOEBox(pos, new Vector2(0.8f, 0.8f), 0f, 0.4f, 0f, KnockbackTier.None, 0f, true);
                    totalHits += hits;
                }
                try { await UniTask.Delay(System.TimeSpan.FromSeconds(tick), cancellationToken: ct); }
                catch (System.OperationCanceledException) { break; }
            }
            Debug.Log($"[ZeroBlade] 발동 Lv.{level} | 총 {totalHits}회 히트 (예상 22~25회/공)");
        }
    }
}
