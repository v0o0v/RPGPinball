using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 원소 폭주 (원소 · A궁 · Tier 6). 멀티볼 상한 5→8 일시 확장.
    /// 활성 멀티볼 각각에 폭발 주기 1 - 0.8×(1-0.9^Lv)s 마다 반경 2.0 원형 폭발.
    /// 폭발당 60% + 넉백 1.5 (보스 제외). 지속 6초.
    /// </summary>
    public class ElementalRampage : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = 6f;
            float interval = 1f - SkillFormula.Diminish(0.8f, 0.9f, level);
            interval = Mathf.Max(0.1f, interval);

            int totalExplosions = 0;
            float endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
                foreach (var b in balls)
                {
                    if (b == null || !b.gameObject.activeInHierarchy) continue;
                    int hits = SpawnAOECircle(b.transform.position, 2.0f, 0.6f, 0f, KnockbackTier.Resist, Constants.KnockbackDistanceNormal, true);
                    totalExplosions++;
                }
                try { await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: ct); }
                catch (System.OperationCanceledException) { break; }
            }

            Debug.Log($"[ElementalRampage] 발동 Lv.{level} | 주기 {interval:F2}s × 공수, 총 폭발 {totalExplosions}회");
        }
    }
}
