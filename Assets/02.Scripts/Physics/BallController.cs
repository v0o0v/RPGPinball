using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 핀볼 공 제어. 속도 클램핑, 낙사 감지, 리스폰 처리를 담당한다.
    /// 마일스톤 3: 멀티볼 카운트 통보, A전환(BallTransformation) 상태, 강제 속도(헤비 액셀러레이터)를 지원.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private int ballIndex;
        [SerializeField] private float respawnY = -8f;

        [Header("재질 / 변신")]
        [SerializeField] private BallMaterial ballMaterial = BallMaterial.Wood;
        [SerializeField] private BallTransformation transformation = BallTransformation.None;

        [Header("플래그")]
        [SerializeField] private bool isSplitBall;

        private Rigidbody2D rb;
        private bool isDead;
        private float invincibleTimer;
        private float transformationRemaining;

        // 강제 속도 (헤비 액셀러레이터 사용)
        private bool forcedSpeedActive;
        private float forcedSpeed;

        public bool IsInvincible => invincibleTimer > 0f;
        public BallMaterial Material { get => ballMaterial; set => ballMaterial = value; }
        public BallTransformation Transformation => transformation;
        public bool IsSplitBall
        {
            get => isSplitBall;
            set
            {
                if (isSplitBall == value) return;
                bool became = value;
                isSplitBall = value;
                // Instantiate 후 OnEnable이 먼저 호출되고 그 시점엔 false인 케이스를 보완:
                // setter 호출 시점에 활성화돼 있으면 즉시 카메라에 통보한다.
                if (gameObject.activeInHierarchy && CameraController.Instance != null)
                {
                    if (became) CameraController.Instance.NotifyBallAdded(1);
                    else CameraController.Instance.NotifyBallRemoved(1);
                }
            }
        }
        public Rigidbody2D Rb => rb;
        public int BallIndex => ballIndex;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.angularDamping = Constants.BallAngularDrag;
            rb.linearDamping = Constants.BallLinearDrag;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void OnEnable()
        {
            // 멀티볼 카운트 통보 (분열 공만 — 본체는 처음부터 1로 카운트)
            if (CameraController.Instance != null && isSplitBall)
            {
                CameraController.Instance.NotifyBallAdded(1);
            }
        }

        private void OnDisable()
        {
            if (CameraController.Instance != null && isSplitBall)
            {
                CameraController.Instance.NotifyBallRemoved(1);
            }
        }

        private void FixedUpdate()
        {
            if (isDead) return;
            if (forcedSpeedActive)
            {
                if (rb.linearVelocity.sqrMagnitude > 0.01f)
                    rb.linearVelocity = rb.linearVelocity.normalized * forcedSpeed;
                else
                    rb.linearVelocity = Vector2.down * forcedSpeed;
            }
            else
            {
                ClampSpeed();
            }
            CheckFallDead();
        }

        private void Update()
        {
            if (invincibleTimer > 0f)
                invincibleTimer -= Time.deltaTime;

            if (transformation != BallTransformation.None && transformationRemaining > 0f)
            {
                transformationRemaining -= Time.deltaTime;
                if (transformationRemaining <= 0f)
                {
                    transformation = BallTransformation.None;
                }
            }
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

        // ── 강제 속도 (헤비 액셀러레이터) ─────────────────────

        public void SetForcedSpeed(float speed)
        {
            forcedSpeedActive = true;
            forcedSpeed = speed;
        }

        public void ClearForcedSpeed()
        {
            forcedSpeedActive = false;
        }

        // ── 변신 (A전환) ──────────────────────────────────────

        public void SetTransformation(BallTransformation t, float duration)
        {
            transformation = t;
            transformationRemaining = duration;
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

            // 분열 공은 리스폰 없이 그대로 소멸
            if (isSplitBall)
            {
                Destroy(gameObject);
                return;
            }

            Invoke(nameof(Respawn), Constants.RespawnDelay);
        }

        // ── 리스폰 ────────────────────────────────────────────

        private void Respawn()
        {
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
