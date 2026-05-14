using UnityEngine;

namespace RPGPinball.Core
{
    /// <summary>
    /// 주요 이벤트 발생 시 SaveSystem.RequestSave 자동 호출. 5초 인터벌 제한 적용.
    /// Bootstrap 에서 한 번 EnsureInstance 후 영구 활성.
    /// </summary>
    public class SaveAutoTrigger : MonoBehaviour
    {
        public static SaveAutoTrigger Instance { get; private set; }

        public static SaveAutoTrigger EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SaveAutoTrigger");
            return go.AddComponent<SaveAutoTrigger>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnStageCleared>(OnStageCleared);
            EventBus.Subscribe<OnBossDefeated>(OnBossDefeated);
            EventBus.Subscribe<OnLevelUp>(OnLevelUp);
            EventBus.Subscribe<OnCurrencyChanged>(OnCurrencyChanged);
            EventBus.Subscribe<OnForgeBallChanged>(OnForgeChanged);
            EventBus.Subscribe<OnFlipperUpgraded>(OnFlipperUpgraded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnStageCleared>(OnStageCleared);
            EventBus.Unsubscribe<OnBossDefeated>(OnBossDefeated);
            EventBus.Unsubscribe<OnLevelUp>(OnLevelUp);
            EventBus.Unsubscribe<OnCurrencyChanged>(OnCurrencyChanged);
            EventBus.Unsubscribe<OnForgeBallChanged>(OnForgeChanged);
            EventBus.Unsubscribe<OnFlipperUpgraded>(OnFlipperUpgraded);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnStageCleared(OnStageCleared _) => Request();
        private void OnBossDefeated(OnBossDefeated _) => Request();
        private void OnLevelUp(OnLevelUp _) => Request();
        private void OnCurrencyChanged(OnCurrencyChanged _) => Request();
        private void OnForgeChanged(OnForgeBallChanged _) => Request();
        private void OnFlipperUpgraded(OnFlipperUpgraded _) => Request();

        private void Request()
        {
            if (SaveSystem.Instance == null) return;
            if (SaveSystem.Instance.Encryption == null) return; // 미초기화 시 무시
            SaveSystem.Instance.RequestSave();
        }
    }
}
