using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 썬더볼트 (원소 · A · Tier 5). 보스 머리 위 하늘에서 거대 벼락 직격.
    /// 데미지: 100% + Lv × 15%. 직격 폭 0.5 + 착탄 반경 2.5 단일 판정.
    /// 5% 확률 스턴 0.5s. 넉백 거리 2.0u (보스 제외).
    /// </summary>
    public class Thunderbolt : ActiveSkillBase
    {
        public override UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Vector2 strike = FindBossOrNearestMonster(targetPos);

            // 착탄 폭발 반경 2.5
            int hits = SpawnAOECircle(strike, 2.5f, 1.0f, 0.15f, KnockbackTier.Resist, Constants.KnockbackDistanceNormal, false);

            // 5% 확률 스턴
            if (UnityEngine.Random.value < 0.05f)
            {
                foreach (var m in OverlapCircleMonsters(strike, 2.5f))
                {
                    m.ApplyStun(0.5f);
                }
            }

            Debug.Log($"[Thunderbolt] 발동 Lv.{level} | 직격 ({strike.x:F1},{strike.y:F1}) | {hits}대 피격");
            return UniTask.CompletedTask;
        }
    }
}
