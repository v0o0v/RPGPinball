using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 매스 리콜 (제어 · A · Tier 5). 활성 모든 공을 플리퍼 상단 안전지대(0, 2)로 즉시 이동.
    /// 데미지 없음. 분열 공도 함께 회수.
    /// </summary>
    public class MassRecall : ActiveSkillBase
    {
        public override UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Vector2 safeSpot = new Vector2(0f, 2f);
            int moved = 0;
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var b in balls)
            {
                if (b == null) continue;
                b.transform.position = safeSpot;
                if (b.Rb != null) b.Rb.linearVelocity = Vector2.zero;
                moved++;
            }
            Debug.Log($"[MassRecall] 공 {moved}개 회수 → ({safeSpot.x}, {safeSpot.y})");
            return UniTask.CompletedTask;
        }
    }
}
