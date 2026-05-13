using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.Pool;
using RPGPinball.Physics;

namespace RPGPinball.Enemy
{
    /// <summary>
    /// 적/보스 탄막. 마일스톤 4: 풀링 + 공 접촉 시 강제 감속/넉백 + 벽 반사 + 유도.
    /// (Instantiate/Destroy 직접 호출 금지 — ProjectilePool.Spawn/Despawn 경유)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class ProjectileBase : MonoBehaviour
    {
        [SerializeField] private ProjectileData data;
        [SerializeField] private float lifetime = 6f;

        private Rigidbody2D rb;
        private float age;
        private int wallBouncesRemaining;
        private Transform homingTarget;
        private float homingTurnRateDegPerSec = 240f;

        public ProjectileData Data => data;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            gameObject.tag = Constants.TagProjectile;
        }

        /// <summary>ProjectilePool에서 데이터 주입 (Instantiate 직후 1회).</summary>
        public void AssignData(ProjectileData d)
        {
            data = d;
        }

        public void Launch(Vector2 dir)
        {
            if (data == null) { Debug.LogError($"[ProjectileBase] ProjectileData 누락: {name}"); return; }
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = dir.normalized * data.speed;
            age = 0f;
            wallBouncesRemaining = data.wallBounceLimit;
        }

        /// <summary>유도 대상 설정 (HomingShot 패턴용).</summary>
        public void SetHomingTarget(Transform target, float turnRateDegPerSec = 240f)
        {
            homingTarget = target;
            homingTurnRateDegPerSec = turnRateDegPerSec;
        }

        /// <summary>풀에서 꺼내질 때 호출.</summary>
        public virtual void OnSpawn()
        {
            age = 0f;
            wallBouncesRemaining = data != null ? data.wallBounceLimit : 0;
            homingTarget = null;
        }

        /// <summary>풀로 반환될 때 호출.</summary>
        public virtual void OnDespawn()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Despawn();
                return;
            }

            // 유도
            if (data != null && data.homing && homingTarget != null && rb != null)
            {
                Vector2 toTarget = (Vector2)homingTarget.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    float currentAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                    float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                    float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, homingTurnRateDegPerSec * Time.deltaTime);
                    rb.linearVelocity = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad)) * data.speed;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other.gameObject, null);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            HandleHit(col.gameObject, col);
        }

        private void HandleHit(GameObject target, Collision2D col)
        {
            if (data == null) return;

            // 데드존 통과 → 시간 페널티 + 소멸
            if (target.CompareTag(Constants.TagDeadZone))
            {
                EventBus.Publish(new OnProjectilePenalty { Delta = data.deadZonePenalty });
                Despawn();
                return;
            }

            // 플리퍼 접촉 → 블로킹 가능 시 소멸 + 쿨감 보너스
            if (target.CompareTag(Constants.TagFlipper))
            {
                if (data.blockableByFlipper)
                {
                    EventBus.Publish(new OnFlipperBlocked { CooldownReduction = Constants.FlipperBlockCooldownBonus });
                    Despawn();
                }
                return;
            }

            // 공 접촉 → 강제 감속/넉백 (M2 #15 인계)
            if (target.CompareTag(Constants.TagBall))
            {
                var ball = target.GetComponent<BallController>();
                if (ball != null)
                {
                    if (data.slowsBallOnContact)
                    {
                        ball.ApplyForcedSlow(data.ballSlowMultiplier, data.ballSlowDuration);
                    }
                    if (data.knockbackBallOnContact)
                    {
                        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
                        if (dir.sqrMagnitude < 0.001f) dir = Vector2.down;
                        var ballRb = target.GetComponent<Rigidbody2D>();
                        if (ballRb != null) ballRb.AddForce(dir * data.ballKnockbackForce, ForceMode2D.Impulse);
                    }
                }
                Despawn();
                return;
            }

            // 벽 충돌 → 반사 카운터 감소
            if (target.CompareTag(Constants.TagWall))
            {
                if (wallBouncesRemaining > 0)
                {
                    wallBouncesRemaining--;
                    // 물리 반사는 Unity Collision이 알아서 처리. 카운터만 감소.
                    return;
                }
                Despawn();
                return;
            }
        }

        private void Despawn()
        {
            if (ProjectilePool.Instance != null)
            {
                ProjectilePool.Instance.Despawn(this);
            }
            else
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }
    }
}
