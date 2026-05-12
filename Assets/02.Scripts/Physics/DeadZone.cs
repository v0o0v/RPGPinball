using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 낙사 판정 트리거. 공이 진입하면 시간 페널티 이벤트를 발행하고 BallController에 낙사 통보.
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

            // 시간 페널티 발행 (기본 -10초, Gimmick_List.md §개요)
            EventBus.Publish(new OnTimePenalty { Delta = Constants.DeadzonePenalty });

            // 공에 낙사 처리 위임
            var ball = col.GetComponent<BallController>();
            ball?.OnDead();
        }
    }
}
