using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using RPGPinball.UI;
using RPGPinball.Data;

namespace RPGPinball.Core
{
    /// <summary>
    /// 싱글턴 게임 매니저. 게임 상태(Playing/Paused/Result)와 씬 전환을 관리한다.
    /// DontDestroyOnLoad로 씬 간 유지됨.
    /// M7: 5종 씬 전환 API + UniTask 페이드 + StageBlueprint 전달.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [field: SerializeField]
        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>다음 Stage 씬 진입 시 사용될 StageBlueprint. ActMap UI 가 [출격] 직전에 설정.</summary>
        public StageBlueprint PendingStageBlueprint { get; private set; }

        /// <summary>가장 최근 결과. Result UI 가 표시할 때 참조.</summary>
        public StageResultContext LastStageResult { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyPhysicsSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static GameManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        // ── 상태 관리 ─────────────────────────────────────────

        public void SetState(GameState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            EventBus.Publish(new OnGameStateChanged { Previous = prev, Current = next });
        }

        public void Pause() => SetState(GameState.Paused);
        public void Resume() => SetState(GameState.Playing);
        public void EndGame() => SetState(GameState.Result);

        // ── 씬 전환 (M7 정식 API) ─────────────────────────────

        public UniTask LoadTitle() => LoadSceneInternal(Constants.SceneNameTitle);
        public UniTask LoadVillage() => LoadSceneInternal(Constants.SceneNameVillage);
        public UniTask LoadActMap() => LoadSceneInternal(Constants.SceneNameActMap);
        public UniTask LoadResult(StageResultContext result)
        {
            LastStageResult = result;
            return LoadSceneInternal(Constants.SceneNameResult);
        }
        public UniTask LoadStage(StageBlueprint blueprint)
        {
            PendingStageBlueprint = blueprint;
            return LoadSceneInternal(Constants.SceneNameStage);
        }

        /// <summary>임시 — 튜토리얼은 M8 인계. 본 마일스톤은 빈 Blueprint 로 Stage 진입.</summary>
        public UniTask LoadTutorial() => LoadStage(null);

        private async UniTask LoadSceneInternal(string sceneName)
        {
            EventBus.Publish(new OnSceneLoadStart { SceneName = sceneName });
            // 씬 전환 직전 활성 팝업 정리 — 파괴된 RectTransform 에 DOTween setter 가 접근하는 race 방지
            if (PopupManager.Instance != null) PopupManager.Instance.CloseAll();
            var fader = SceneFader.EnsureInstance();
            await fader.FadeOut(Constants.SceneFadeOutSec);

            SetState(GameState.Paused);
            // 씬이 빌드 세팅에 등록되지 않은 경우 안전하게 처리 (개발 중)
            try
            {
                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                {
                    Debug.LogWarning($"[GameManager] 씬 '{sceneName}' 로드 실패 — Build Settings 에 등록되지 않았을 수 있음.");
                }
                else
                {
                    op.allowSceneActivation = false;
                    while (op.progress < 0.9f)
                        await UniTask.Yield();
                    op.allowSceneActivation = true;
                    await UniTask.WaitUntil(() => op.isDone);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] 씬 로드 예외: {e.Message}");
            }

            SetState(GameState.Playing);
            await fader.FadeIn(Constants.SceneFadeInSec);
            EventBus.Publish(new OnSceneLoadComplete { SceneName = sceneName });
        }

        // ── 호환: 기존 LoadSceneAsync (M5 이하) ────────────────
        public async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            await LoadSceneInternal(sceneName);
        }

        // ── 물리 설정 적용 ────────────────────────────────────

        private static void ApplyPhysicsSettings()
        {
            Physics2D.gravity = Constants.Gravity;
            Time.fixedDeltaTime = Constants.FixedTimestep;
        }
    }

    /// <summary>
    /// Stage 클리어/실패 결과. ResultScreen 이 표시.
    /// </summary>
    [System.Serializable]
    public class StageResultContext
    {
        public bool cleared;
        public int actId;
        public int stageIndex;
        public string grade = "B";           // S/A/B/C
        public float clearTimeSec;
        public float totalTimeSec;
        public int maxCombo;
        public int xpReward;
        public int goldReward;
        public int continueCount;
    }
}
