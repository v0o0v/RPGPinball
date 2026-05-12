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
            EventBus.Subscribe<OnBallDead>(HandleBallDead);
            EventBus.Subscribe<OnProjectilePenalty>(HandleProjectilePenalty);
            EventBus.Subscribe<OnTimePenalty>(HandleTimePenalty);
            if (autoStart) StartTimer();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnBallDead>(HandleBallDead);
            EventBus.Unsubscribe<OnProjectilePenalty>(HandleProjectilePenalty);
            EventBus.Unsubscribe<OnTimePenalty>(HandleTimePenalty);
            if (Instance == this) Instance = null;
        }

        public void StartTimer() { running = true; gameOverFired = false; }
        public void StopTimer() => running = false;

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

        private void HandleBallDead(OnBallDead _)
        {
            // DeadZone에서 이미 OnTimePenalty 발행하므로 여기서는 중복 방지.
            // 보스전 -20초 분기는 마일스톤 4에서 추가.
        }

        private void HandleProjectilePenalty(OnProjectilePenalty e) => Penalize(e.Delta);

        private void HandleTimePenalty(OnTimePenalty e) => Penalize(e.Delta);
    }
}
