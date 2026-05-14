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
        private bool isResetting;
        private float invincibleTimer;
        private float transformationRemaining;

        // 강제 속도 (헤비 액셀러레이터 사용)
        private bool forcedSpeedActive;
        private float forcedSpeed;

        // 마일스톤 4: 강제 감속/시간 가속·감속 (탄막 슬로우 / 시계탑 보스)
        private float forcedSlowMultiplier = 1f;
        private float forcedSlowEndTime;
        private float forcedSpeedMultiplier = 1f;
        private float forcedSpeedMultiplierEndTime;
        // 절대 영도 (겨울 여왕 P5)
        private float lastCollisionTime;

        public bool IsInvincible => invincibleTimer > 0f;
        public float LastCollisionTime => lastCollisionTime;
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

            // 공이 항상 다른 모든 오브젝트 위에 그려지도록 sortingOrder 최상위 강제.
            // 기믹 prefab=5, 세그먼트=-1 ~ 3 범위에서 100이면 충분히 위.
            var sprites = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            foreach (var sr in sprites) sr.sortingOrder = 100;
        }

        private void OnEnable()
        {
            // 멀티볼 카운트 통보 (분열 공만 — 본체는 처음부터 1로 카운트)
            if (CameraController.Instance != null && isSplitBall)
            {
                CameraController.Instance.NotifyBallAdded(1);
            }
            // 카메라 추적은 Start()에서 등록 — Awake 순서 보장 위해 (CameraController.Instance가 null일 수 있음).
            TryRegisterCameraFollow();
        }

        private void Start()
        {
            // Awake 모두 끝난 후 호출되므로 CameraController.Instance가 보장됨.
            TryRegisterCameraFollow();
        }

        private void TryRegisterCameraFollow()
        {
            if (CameraController.Instance == null) return;
            // 좌·우 외벽이 화면 가장자리에 닿는 폭으로 카메라가 고정되므로 X 추적 0, Y만 추적.
            float vInfluence = isSplitBall ? 0.5f : 1f;
            CameraController.Instance.RegisterFollow(transform, 0f, vInfluence);
        }

        private void OnDisable()
        {
            if (CameraController.Instance != null && isSplitBall)
            {
                CameraController.Instance.NotifyBallRemoved(1);
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.UnregisterFollow(transform);
            }
        }

        private void FixedUpdate()
        {
            if (isResetting) return;
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
                ApplyForcedSlowModifiers();
                ApplyForcedSpeedMultiplierModifiers();
            }
            // 데드존 제거 (2026-05-13) — 외벽 닫힌 통으로 공이 절대 떨어지지 않음.
            // CheckFallDead() 호출 제거. BallController.OnDead 는 보스 강제 reset 패턴용으로 유지.
        }

        private void ApplyForcedSlowModifiers()
        {
            if (Time.time >= forcedSlowEndTime) { forcedSlowMultiplier = 1f; return; }
            if (rb.linearVelocity.sqrMagnitude > 0.0001f)
                rb.linearVelocity *= forcedSlowMultiplier;
        }

        private void ApplyForcedSpeedMultiplierModifiers()
        {
            if (Time.time >= forcedSpeedMultiplierEndTime) { forcedSpeedMultiplier = 1f; return; }
            if (Mathf.Approximately(forcedSpeedMultiplier, 1f)) return;
            // 1.0이 아닐 때만 클램프 후 곱
            float current = rb.linearVelocity.magnitude;
            if (current <= 0.01f) return;
            float target = Mathf.Clamp(current * forcedSpeedMultiplier, Constants.BallMinSpeed, Constants.BallMaxSpeed);
            rb.linearVelocity = rb.linearVelocity.normalized * target;
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

        private float stuckSinceTime = -1f;
        private const float StuckGraceSeconds = 0.5f;

        private void ClampSpeed()
        {
            var speed = rb.linearVelocity.magnitude;
            if (speed > Constants.BallMaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * Constants.BallMaxSpeed;
                stuckSinceTime = -1f;
                return;
            }
            // 자유낙하 정점에서 속도가 일시적으로 0에 가까워질 때 ClampSpeed가 위로 재가속하는 버그를 회피.
            // 일정 시간 이상(0.5s) min 미만 상태가 유지될 때만 stuck으로 간주해 임펄스 강제.
            if (speed < Constants.BallMinSpeed)
            {
                if (stuckSinceTime < 0f) stuckSinceTime = Time.time;
                else if (Time.time - stuckSinceTime > StuckGraceSeconds && speed < 0.3f)
                {
                    Vector2 dir = speed > 0.001f ? rb.linearVelocity.normalized : Vector2.down;
                    rb.linearVelocity = dir * Constants.BallMinSpeed;
                    stuckSinceTime = -1f;
                }
            }
            else
            {
                stuckSinceTime = -1f;
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

        // ── 마일스톤 4: 강제 감속 / 시간 가속·감속 ─────────────

        /// <summary>탄막 접촉, 빙결 장판 등에서 일정 기간 공 속도 ×multiplier.</summary>
        public void ApplyForcedSlow(float multiplier, float duration)
        {
            forcedSlowMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
            forcedSlowEndTime = Time.time + duration;
            // 즉시 1회 적용
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
                rb.linearVelocity *= forcedSlowMultiplier;
        }

        /// <summary>시계탑 시간 가속/감속. duration 동안 공 속도 ×multiplier.</summary>
        public void ApplyForcedSpeedMultiplier(float multiplier, float duration)
        {
            forcedSpeedMultiplier = Mathf.Max(0.01f, multiplier);
            forcedSpeedMultiplierEndTime = Time.time + duration;
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                float target = Mathf.Clamp(rb.linearVelocity.magnitude * forcedSpeedMultiplier, Constants.BallMinSpeed, Constants.BallMaxSpeed);
                rb.linearVelocity = rb.linearVelocity.normalized * target;
            }
        }

        // ── 변신 (A전환) ──────────────────────────────────────

        public void SetTransformation(BallTransformation t, float duration)
        {
            transformation = t;
            transformationRemaining = duration;
        }

        // ── 강제 리셋 (보스 메커닉 전용) ──────────────────────
        // 데드존 제거(2026-05-13) 이후 자연 낙사는 없음. ForceReset은 보스 강제 reset 패턴 전용.

        public void ForceReset()
        {
            if (isResetting) return;
            isResetting = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            gameObject.SetActive(false);

            EventBus.Publish(new OnBallReset { BallIndex = ballIndex });

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
            isResetting = false;
            invincibleTimer = Constants.RespawnInvincibleTime;

            EventBus.Publish(new OnBallSpawned { BallIndex = ballIndex });
        }

        // ── 충돌 이벤트 ───────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D col)
        {
            // 절대 영도 추적: 벽/범퍼/적과의 충돌 시각 기록
            string tag = col.gameObject.tag;
            if (tag == Constants.TagWall || tag == Constants.TagBumper
                || tag == Constants.TagMonster || tag == Constants.TagBoss)
            {
                lastCollisionTime = Time.time;
            }

            // 벽 충돌 직후 자동 반사 보장: PhysicsMaterial2D 반발만으로 부족한 경우
            // (저각 슬라이딩 등) 충돌 법선 방향으로 최소 속도 임펄스 부여 → 공이 벽에 박혀
            // 멈추거나 미끄러져 흐르는 현상을 방지.
            if (tag == Constants.TagWall && col.contactCount > 0)
            {
                Vector2 normal = col.GetContact(0).normal;
                float postSpeed = rb.linearVelocity.magnitude;
                if (postSpeed < Constants.BallMinSpeed)
                {
                    rb.linearVelocity = normal * Constants.BallMinSpeed;
                    stuckSinceTime = -1f;
                }
            }

            EventBus.Publish(new OnBallHit
            {
                Speed = rb.linearVelocity.magnitude,
                TargetTag = tag
            });
        }
    }
}
