using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 개별 플리퍼 오브젝트 동작.
    /// 소환 직후 0~FlipperSwingTime 동안 시작각→끝각으로 스윙 회전(핀볼식 모션),
    /// 그 이후엔 정적 상태. 충돌 시 스윙/정적 구간에 따라 반발 임펄스 차등.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Flipper : MonoBehaviour
    {
        private float elapsed;
        private float swingStartAngle;
        private float swingEndAngle;

        private bool isSwinging => elapsed < Constants.FlipperSwingTime;

        /// <summary>
        /// 소환 직후 FlipperController가 호출. 위치에 따라 좌/우 플리퍼로 결정되며
        /// 회전 시작각을 설정한다.
        /// </summary>
        // 2026-05-14: 플리퍼 시각·콜라이더 크기 50% 확대 (사용자 요청). prefab 은 그대로 두고 런타임 scale 로 적용.
        private const float SizeMultiplier = 1.5f;

        public void InitializeSwing(bool isLeftSide)
        {
            // 회전축은 부모 transform(=두꺼운 쪽 끝, 피벗). 자식 Sprite/콜라이더가 우측 offset.
            // 좌우 거울 대칭: scale.x 반전 + 회전 각도 부호 반전 함께 적용.
            float sign = isLeftSide ? 1f : -1f;
            transform.localScale = new Vector3(sign * SizeMultiplier, SizeMultiplier, 1f);
            swingStartAngle = Constants.FlipperSwingStartAngle * sign;
            swingEndAngle = Constants.FlipperSwingEndAngle * sign;
            transform.localRotation = Quaternion.Euler(0f, 0f, swingStartAngle);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed < Constants.FlipperSwingTime)
            {
                float t = elapsed / Constants.FlipperSwingTime;
                // SmoothStep 으로 시작/끝에서 느려지고 중간에서 빠른 핀볼식 가속
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float angle = Mathf.Lerp(swingStartAngle, swingEndAngle, smoothT);
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            else if (transform.localRotation.eulerAngles.z != swingEndAngle)
            {
                // 스윙 종료 시점에 정확히 끝각으로 스냅
                transform.localRotation = Quaternion.Euler(0f, 0f, swingEndAngle);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;

            var rb = col.gameObject.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            var impulse = isSwinging ? Constants.FlipperSwingImpulse : Constants.FlipperStaticImpulse;
            var dir = (col.transform.position - transform.position).normalized;

            // 최대 타격 각도 클램핑
            var angle = Vector2.SignedAngle(Vector2.up, dir);
            angle = Mathf.Clamp(angle, -Constants.FlipperMaxAngle, Constants.FlipperMaxAngle);
            dir = Quaternion.Euler(0, 0, angle) * Vector2.up;

            rb.AddForce(dir * impulse, ForceMode2D.Impulse);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.CompareTag(Constants.TagProjectile)) return;

            // 탄막 블로킹: 탄막 소멸 + 쿨타임 감소 이벤트
            Destroy(col.gameObject);
            EventBus.Publish(new OnFlipperBlocked
            {
                CooldownReduction = Constants.FlipperBlockCooldownBonus
            });
        }
    }
}
