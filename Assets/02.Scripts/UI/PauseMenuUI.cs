using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using RPGPinball.Core;

namespace RPGPinball.UI
{
    /// <summary>
    /// 일시정지 메뉴 (Pause Canvas). PauseManager 가 UserRequest 로 Pause 시 표시.
    /// [계속하기] / [설정] / [포기]
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public static PauseMenuUI Instance { get; private set; }

        private Canvas canvas;
        private GameObject panel;

        public static PauseMenuUI EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PauseMenuUI");
            return go.AddComponent<PauseMenuUI>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnApplicationPaused>(OnPaused);
            EventBus.Subscribe<OnApplicationResumed>(OnResumed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnApplicationPaused>(OnPaused);
            EventBus.Unsubscribe<OnApplicationResumed>(OnResumed);
        }

        private void OnPaused(OnApplicationPaused e)
        {
            if (e.Reason == PauseReason.UserRequest.ToString())
                panel.SetActive(true);
        }

        private void OnResumed(OnApplicationResumed e)
        {
            if (PauseManager.Instance != null && !PauseManager.Instance.IsPaused)
                panel.SetActive(false);
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            scaler.matchWidthOrHeight = Constants.UICanvasMatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("PausePanel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            AddButton("CONTINUE", "▶ 계속하기", new Vector2(0, 200), OnContinue, new Color(0.30f, 0.69f, 0.31f));
            AddButton("SETTINGS", "⚙ 설정", new Vector2(0, 0), OnSettings, new Color(0.29f, 0.56f, 0.89f));
            AddButton("QUIT", "✕ 포기", new Vector2(0, -200), OnQuit, new Color(0.85f, 0.20f, 0.20f));
        }

        private void AddButton(string name, string label, Vector2 anchored, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(500, 150);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = lbl.AddComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 60;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }

        private void OnContinue()
        {
            panel.SetActive(false);
            PauseManager.Instance?.Resume(PauseReason.UserRequest);
        }

        private void OnSettings()
        {
            PopupManager.Instance?.OpenSettings();
        }

        private void OnQuit()
        {
            PopupManager.Instance?.OpenConfirm("포기하시겠습니까?", "현재 진행 상황이 손실됩니다.",
                onConfirm: () =>
                {
                    panel.SetActive(false);
                    PauseManager.Instance?.ForceResumeAll();
                    GameManager.Instance?.LoadVillage().Forget();
                });
        }
    }
}
