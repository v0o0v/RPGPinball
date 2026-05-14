using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Security;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 스테이지 타이머. 기본 180초 카운트다운. 회복 상한 60초/스테이지.
    /// 낙사·탄막 페널티는 EventBus를 통해 자동 차감.
    /// </summary>
    public class StageTimer : MonoBehaviour
    {
        public static StageTimer Instance { get; private set; }

        [SerializeField] private bool autoStart = true;
        [SerializeField] private float startTimeOverride = -1f; // -1이면 Constants 기본값 사용

        private SafeFloat remaining;
        private SafeFloat totalRecovered;
        private float total;
        private bool running;
        private bool gameOverFired;

        public float Remaining => remaining.Value;
        public float Total => total;
        public bool IsRunning => running;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            total = startTimeOverride > 0f ? startTimeOverride : Constants.StageDefaultTime;
            remaining = SafeFloat.Create(total);
            totalRecovered = SafeFloat.Create(0f);
        }

        private void OnEnable()
        {
            // 데드존 제거(2026-05-13) — 자동 페널티 없음. 보스/탄막/기믹은 직접 OnTimePenalty/OnProjectilePenalty 발행.
            EventBus.Subscribe<OnProjectilePenalty>(HandleProjectilePenalty);
            EventBus.Subscribe<OnTimePenalty>(HandleTimePenalty);
            if (autoStart) StartTimer();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnProjectilePenalty>(HandleProjectilePenalty);
            EventBus.Unsubscribe<OnTimePenalty>(HandleTimePenalty);
            if (Instance == this) Instance = null;
        }

        public void StartTimer() { running = true; gameOverFired = false; }
        public void StopTimer() => running = false;

        /// <summary>테스트/디버그용 — 타이머를 명시적으로 startTime으로 초기화. EditMode 테스트 등에서 Awake가 호출되지 않은 경우 사용.</summary>
        public void ResetTimer(float startTime)
        {
            total = startTime;
            remaining = SafeFloat.Create(startTime);
            totalRecovered = SafeFloat.Create(0f);
            running = false;
            gameOverFired = false;
        }

        private void Update()
        {
            if (!running) return;
            float r = remaining.Value - Time.deltaTime;
            if (r <= 0f)
            {
                r = 0f;
                running = false;
                if (!gameOverFired)
                {
                    gameOverFired = true;
                    EventBus.Publish(new OnGameStateChanged
                    {
                        Previous = GameState.Playing,
                        Current = GameState.Result
                    });
                }
            }
            remaining = SafeFloat.Create(r);
            EventBus.Publish(new OnTimerChanged { Remaining = r, Total = total });
        }

        /// <summary>시간 회복. 스테이지당 누적 상한 60초.</summary>
        public void AddTime(float seconds)
        {
            if (seconds <= 0f) return;
            float allowed = Mathf.Min(seconds, Constants.TimeRecoverCapPerStage - totalRecovered.Value);
            if (allowed <= 0f) return;

            remaining = SafeFloat.Create(remaining.Value + allowed);
            totalRecovered = SafeFloat.Create(totalRecovered.Value + allowed);
        }

        /// <summary>시간 페널티. seconds는 양수로 받아 음수 차감 처리.</summary>
        public void Penalize(float seconds)
        {
            float abs = Mathf.Abs(seconds);
            float r = Mathf.Max(0f, remaining.Value - abs);
            remaining = SafeFloat.Create(r);
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────

        private void HandleProjectilePenalty(OnProjectilePenalty e) => Penalize(e.Delta);

        private void HandleTimePenalty(OnTimePenalty e) => Penalize(e.Delta);
    }
}
