using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Enemy;
using RPGPinball.Enemy.Pool;

namespace RPGPinball.Physics
{
    /// <summary>
    /// Flipper 프리팹의 자식 GameObject에 부착. 탄막 전용 트리거 콜라이더.
    /// 부모 BoxCollider2D(isTrigger=false)는 공 반사 담당,
    /// 이 자식 BoxCollider2D(isTrigger=true)는 ProjectileBase OnTriggerEnter2D 분기 활성화.
    /// (M1 #3 인계)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlipperBlockTrigger : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            // 부모 또는 자체 tag가 Flipper여야 ProjectileBase가 탐지
            if (gameObject.tag != Constants.TagFlipper) gameObject.tag = Constants.TagFlipper;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(Constants.TagProjectile)) return;

            var pj = other.GetComponent<ProjectileBase>();
            if (pj == null) return;
            if (pj.Data == null || !pj.Data.blockableByFlipper) return;

            EventBus.Publish(new OnFlipperBlocked { CooldownReduction = Constants.FlipperBlockCooldownBonus });

            if (ProjectilePool.Instance != null)
                ProjectilePool.Instance.Despawn(pj);
            else
                Destroy(pj.gameObject);
        }
    }
}
