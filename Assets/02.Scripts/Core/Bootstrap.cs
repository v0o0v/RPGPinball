using UnityEngine;
using RPGPinball.Security;
using RPGPinball.UI;

namespace RPGPinball.Core
{
    /// <summary>
    /// 게임 부팅 시 1회 초기화. 모든 매니저 EnsureInstance + SaveSystem Initialize + 자동 로드.
    /// SceneManager 진입 직전 RuntimeInitializeOnLoadMethod 로 호출.
    /// </summary>
    public static class Bootstrap
    {
        public static bool Initialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void OnGameStart()
        {
            if (Initialized) return;
            Initialized = true;

            ISaltProvider provider;
#if UNITY_EDITOR
            provider = new DebugSaltProvider();
#else
            provider = new RuntimeSaltProvider();
#endif
            SaveSystem.Instance.Initialize(provider);
            if (SaveSystem.Instance.HasSave())
            {
                var result = SaveSystem.Instance.Load(out _);
                if (result != LoadResult.Success)
                    Debug.LogWarning($"[Bootstrap] 세이브 로드 실패: {result}");
            }
            else if (SaveMigrationV1ToV2.HasLegacyPrefs())
            {
                SaveSystem.Instance.TryMigrateFromV1();
            }

            GameManager.EnsureInstance();
            PauseManager.EnsureInstance();
            SceneFader.EnsureInstance();
            SaveAutoTrigger.EnsureInstance();
            UI.PopupManager.EnsureInstance();
            UI.PauseMenuUI.EnsureInstance();
            UI.ContinueAdPopup.EnsureInstance();
        }

        /// <summary>테스트용 강제 재초기화.</summary>
        public static void ForceReinitForTest()
        {
            Initialized = false;
        }
    }
}
