using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace RPGPinball.Core
{
    /// <summary>
    /// 싱글턴 게임 매니저. 게임 상태(Playing/Paused/Result)와 씬 전환을 관리한다.
    /// DontDestroyOnLoad로 씬 간 유지됨.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [field: SerializeField]
        public GameState State { get; private set; } = GameState.Playing;

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
            if (Instance == this)
                Instance = null;
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

        // ── 씬 전환 (UniTask 비동기) ──────────────────────────

        public async UniTaskVoid LoadSceneAsync(string sceneName)
        {
            SetState(GameState.Paused);
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                await UniTask.Yield();

            op.allowSceneActivation = true;
            await UniTask.WaitUntil(() => op.isDone);
            SetState(GameState.Playing);
        }

        // ── 물리 설정 적용 ────────────────────────────────────

        private static void ApplyPhysicsSettings()
        {
            Physics2D.gravity = Constants.Gravity;
            Time.fixedDeltaTime = Constants.FixedTimestep;
        }
    }
}
