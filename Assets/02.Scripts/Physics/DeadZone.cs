using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 낙사 판정 트리거. 공이 진입하면 시간 페널티 이벤트를 발행하고 BallController에 낙사 통보.
    /// 마일스톤 4: BossFightContext.IsActive면 보스전 페널티(-20초) 적용.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeadZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.CompareTag(Constants.TagBall)) return;

            // 보스전 진행 시 -20초, 평시 -10초
            float penalty = BossFightContext.IsActive
                ? Constants.BossDeadzonePenalty
                : Constants.DeadzonePenalty;
            EventBus.Publish(new OnTimePenalty { Delta = penalty });

            // 공에 낙사 처리 위임
            var ball = col.GetComponent<BallController>();
            ball?.OnDead();
        }
    }
}
