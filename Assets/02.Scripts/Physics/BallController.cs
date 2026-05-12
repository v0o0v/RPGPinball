using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 핀볼 공 제어. 속도 클램핑, 낙사 감지, 리스폰 처리를 담당한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private int ballIndex;
        [SerializeField] private float respawnY = -8f;

        private Rigidbody2D rb;
        private bool isDead;
        private float invincibleTimer;

        public bool IsInvincible => invincibleTimer > 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.angularDamping = Constants.BallAngularDrag;
            rb.linearDamping = Constants.BallLinearDrag;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void FixedUpdate()
        {
            if (isDead) return;
            ClampSpeed();
            CheckFallDead();
        }

        private void Update()
        {
            if (invincibleTimer > 0f)
                invincibleTimer -= Time.deltaTime;
        }

        // ── 속도 클램핑 ───────────────────────────────────────

        private void ClampSpeed()
        {
            var speed = rb.linearVelocity.magnitude;
            if (speed > Constants.BallMaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * Constants.BallMaxSpeed;
            }
            else if (speed < Constants.BallMinSpeed && speed > 0.01f)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * Constants.BallMinSpeed;
            }
        }

        // ── 낙사 감지 (DeadZone Trigger 보조용) ──────────────

        private void CheckFallDead()
        {
            if (transform.position.y < respawnY)
                OnDead();
        }

        public void OnDead()
        {
            if (isDead) return;
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            gameObject.SetActive(false);

            EventBus.Publish(new OnBallDead { BallIndex = ballIndex });
            Invoke(nameof(Respawn), Constants.RespawnDelay);
        }

        // ── 리스폰 ────────────────────────────────────────────

        private void Respawn()
        {
            // 플레이필드는 x=0 중심. 상단 중앙에서 발사
            var spawnPos = new Vector2(0f, Constants.SegmentHeight / 2f - 1f);
            transform.position = spawnPos;
            rb.linearVelocity = Vector2.down * Constants.RespawnLaunchSpeed;
            gameObject.SetActive(true);
            isDead = false;
            invincibleTimer = Constants.RespawnInvincibleTime;

            EventBus.Publish(new OnBallRespawned { BallIndex = ballIndex });
        }

        // ── 충돌 이벤트 ───────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            EventBus.Publish(new OnBallHit
            {
                Speed = rb.linearVelocity.magnitude,
                TargetTag = col.gameObject.tag
            });
        }
    }
}
