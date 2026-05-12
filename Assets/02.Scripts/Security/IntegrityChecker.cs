using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Security
{
    /// <summary>
    /// 런타임 핵심 수치의 체크섬을 주기적으로 검증한다.
    /// 불일치 감지 시 경고 로그와 함께 값을 복원한다.
    /// </summary>
    public class IntegrityChecker : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 5f;

        private float timer;

        // 검증 대상은 외부에서 프로퍼티로 등록
        public System.Func<int> GetHP;
        public System.Action<int> RestoreHP;
        private int hpChecksum;

        public System.Func<float> GetTimer;
        public System.Action<float> RestoreTimer;
        private float timerChecksum;

        public void RegisterHP(System.Func<int> getter, System.Action<int> restorer)
        {
            GetHP = getter;
            RestoreHP = restorer;
            hpChecksum = getter();
        }

        public void RegisterTimer(System.Func<float> getter, System.Action<float> restorer)
        {
            GetTimer = getter;
            RestoreTimer = restorer;
            timerChecksum = getter();
        }

        public void UpdateChecksum()
        {
            if (GetHP != null) hpChecksum = GetHP();
            if (GetTimer != null) timerChecksum = GetTimer();
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < checkInterval) return;
            timer = 0f;
            Validate();
        }

        private void Validate()
        {
            if (GetHP != null && GetHP() != hpChecksum)
            {
                Debug.LogWarning("[IntegrityChecker] HP 체크섬 불일치 감지. 값 복원.");
                RestoreHP?.Invoke(hpChecksum);
            }

            if (GetTimer != null && Mathf.Abs(GetTimer() - timerChecksum) > 1f)
            {
                Debug.LogWarning("[IntegrityChecker] Timer 체크섬 불일치 감지. 값 복원.");
                RestoreTimer?.Invoke(timerChecksum);
            }
        }
    }
}
