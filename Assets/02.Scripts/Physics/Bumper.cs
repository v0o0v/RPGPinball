using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 핀볼 범퍼. 공 충돌 시 중심에서 바깥 방향으로 고정 임펄스를 추가해 강하게 튕겨낸다.
    /// Physics_Parameters.md §"공 ↔ 범퍼" : 고정 반발력, Gimmick_List.md §35: 18 N.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Bumper : MonoBehaviour
    {
        [Tooltip("기본값은 Constants.BumperImpulse(18N). 개별 오브젝트별 오버라이드 시에만 변경.")]
        [SerializeField] private float impulseOverride = -1f;

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;

            var rb = col.rigidbody;
            if (rb == null) return;

            // 범퍼 중심 → 공 방향으로 임펄스
            var dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

            var impulse = impulseOverride > 0f ? impulseOverride : Constants.BumperImpulse;
            rb.AddForce(dir * impulse, ForceMode2D.Impulse);

            EventBus.Publish(new OnBallHit
            {
                Speed = rb.linearVelocity.magnitude,
                TargetTag = Constants.TagBumper,
            });
        }
    }
}
