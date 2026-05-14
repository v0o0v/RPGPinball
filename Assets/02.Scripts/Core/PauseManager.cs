using System.Collections.Generic;
using UnityEngine;

namespace RPGPinball.Core
{
    public enum PauseReason
    {
        None = 0,
        UserRequest = 1,
        ApplicationBackground = 2,
        SystemNotification = 3,
        IncomingCall = 4,
        PopupOpen = 5
    }

    /// <summary>
    /// 게임 일시정지 + 백그라운드 복귀. Time.timeScale=0 적용 + 인게임 객체 직렬화 트리거.
    /// 중첩 호출 안전: 활성 reason 스택. 모든 reason 해제 시 Resume.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        public bool IsPaused => activeReasons.Count > 0;
        public PauseReason TopReason => activeReasons.Count > 0 ? activeReasons[activeReasons.Count - 1] : PauseReason.None;

        private readonly List<PauseReason> activeReasons = new List<PauseReason>();
        private float backgroundEnteredAt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static PauseManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PauseManager");
            return go.AddComponent<PauseManager>();
        }

        public void Pause(PauseReason reason)
        {
            if (activeReasons.Contains(reason)) return;
            activeReasons.Add(reason);
            ApplyPauseState();
            EventBus.Publish(new OnApplicationPaused { Reason = reason.ToString() });
        }

        public void Resume(PauseReason reason)
        {
            if (activeReasons.Remove(reason))
                ApplyPauseState();
        }

        public void ForceResumeAll()
        {
            activeReasons.Clear();
            ApplyPauseState();
        }

        private void ApplyPauseState()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                backgroundEnteredAt = Time.realtimeSinceStartup;
                Pause(PauseReason.ApplicationBackground);
                if (Constants.PauseAllowBackgroundAutoSave && SaveSystem.Instance != null && SaveSystem.Instance.Encryption != null)
                {
                    SaveSystem.Instance.SaveImmediate(SaveSystem.Instance.CurrentData);
                    RuntimeStageSerializer.SnapshotToDisk();
                }
            }
            else
            {
                float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - backgroundEnteredAt);
                EventBus.Publish(new OnApplicationResumed { ElapsedSecondsBackground = elapsed });
                // 백그라운드로 들어간 reason만 해제 — UserRequest 가 동시에 있었으면 사용자 입력 대기.
                Resume(PauseReason.ApplicationBackground);
            }
        }
    }
}
