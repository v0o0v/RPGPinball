using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks
{
    /// <summary>
    /// 80종 기믹 추상 베이스. GimmickData SO에서 모든 수치를 로드.
    /// 공통 라이프사이클: OnSpawn → (Trigger by Contact/Periodic) → OnDespawn.
    /// </summary>
    public abstract class GimmickBase : MonoBehaviour
    {
        [SerializeField] protected GimmickData data;
        [SerializeField] protected bool consumed;
        protected float lastTriggerTime;
        protected float periodicTimer;

        public GimmickData Data => data;
        public bool IsConsumed => consumed;

        public virtual void OnSpawn(GimmickData gimmickData)
        {
            data = gimmickData;
            consumed = false;
            lastTriggerTime = -999f;
            periodicTimer = 0f;
            EventBus.Publish(new OnGimmickActivated { GimmickId = data != null ? data.gimmickId : GimmickId.None, Position = transform.position });
        }

        public virtual void OnDespawn()
        {
            if (data != null)
                EventBus.Publish(new OnGimmickDespawned { GimmickId = data.gimmickId });
            consumed = true;
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            // 일부 기믹(텔레포트 영역, 함정 zone 등)은 여전히 trigger 콜라이더 사용. 본 메서드는 그 호환성.
            if (data == null || consumed) return;
            if (data.triggerKind != GimmickTriggerKind.BallContact) return;
            if (!other.CompareTag(Constants.TagBall)) return;
            var ball = other.GetComponent<BallController>() ?? other.GetComponentInParent<BallController>();
            if (ball == null) return;
            HandleBallContact(ball);
        }

        protected virtual void OnCollisionEnter2D(Collision2D col)
        {
            // 2026-05-14: 핀볼 동작을 위해 기믹 콜라이더가 isTrigger=false 로 동작.
            // 공이 부딪히면 물리 반발 + 효과 발행 동시 처리.
            if (data == null || consumed) return;
            if (data.triggerKind != GimmickTriggerKind.BallContact) return;
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;
            var ball = col.gameObject.GetComponent<BallController>() ?? col.gameObject.GetComponentInParent<BallController>();
            if (ball == null) return;
            HandleBallContact(ball);
        }

        protected virtual void Update()
        {
            if (data == null || consumed) return;
            if (data.triggerKind == GimmickTriggerKind.Periodic && data.triggerIntervalSeconds > 0f)
            {
                periodicTimer += Time.deltaTime;
                if (periodicTimer >= data.triggerIntervalSeconds)
                {
                    periodicTimer = 0f;
                    HandlePeriodicTick();
                }
            }
        }

        protected virtual void HandleBallContact(BallController ball) { }
        protected virtual void HandlePeriodicTick() { }

        /// <summary>
        /// 도감 저항력 감쇠 적용 (마일스톤 5는 항상 원본 반환).
        /// 마일스톤 6 점성술사 시설에서 PlayerData 기반 0~40% 감쇠로 본격화.
        /// </summary>
        protected float GetEffectiveDurationSeconds(float originalSeconds)
        {
            if (data == null || !data.respectsResistance) return originalSeconds;
            // M6 인계: PlayerData.resistLevels[data.gimmickId] 참조해 0/10/20/30/40% 감쇠.
            return originalSeconds;
        }

        /// <summary>
        /// 플리퍼/공으로 이 기믹을 무력화할 수 있는지. blockable 기믹만 true 반환.
        /// </summary>
        public virtual bool TryBlock()
        {
            if (data == null || !data.isBlockable || consumed) return false;
            ConsumeAndDespawn();
            return true;
        }

        protected void ConsumeAndDespawn()
        {
            consumed = true;
            OnDespawn();
            // 풀링 도입 시 Despawn으로 교체. 마일스톤 5는 즉시 Destroy.
            Destroy(gameObject);
        }

        protected bool IsOnCooldown()
        {
            if (data == null || data.cooldownSeconds <= 0f) return false;
            return Time.time - lastTriggerTime < data.cooldownSeconds;
        }

        protected void StampCooldown() => lastTriggerTime = Time.time;
    }

    // ── 기믹 이벤트 ─────────────────────────────────────────────
    public struct OnGimmickActivated
    {
        public GimmickId GimmickId;
        public Vector3 Position;
    }

    public struct OnGimmickDespawned
    {
        public GimmickId GimmickId;
    }
}
