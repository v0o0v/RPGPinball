using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.UI
{
    /// <summary>
    /// 시간 초과 시 광고 시청 → 이어하기 팝업. M8 광고 SDK 연동 전까지 5초 모킹.
    /// 일일 한도 3회 — SaveData.player.adContinueUsedToday.
    /// </summary>
    public class ContinueAdPopup : MonoBehaviour
    {
        public static ContinueAdPopup Instance { get; private set; }

        private GameObject popup;
        private Canvas canvas;
        private bool active;

        public static ContinueAdPopup EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("ContinueAdPopup");
            return go.AddComponent<ContinueAdPopup>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnGameStateChanged>(OnGameState);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnGameStateChanged>(OnGameState);
        }

        private void OnGameState(OnGameStateChanged e)
        {
            // 타이머가 Result 전환을 발행 → 시간 초과 검출
            if (e.Current == GameState.Result && StageTimer.Instance != null && StageTimer.Instance.Remaining <= 0f && !active)
            {
                Show();
            }
        }

        public void Show()
        {
            if (popup == null) BuildCanvas();
            popup.SetActive(true);
            active = true;
            PauseManager.Instance?.Pause(PauseReason.PopupOpen);
            EventBus.Publish(new OnContinueRequested { });
        }

        public void Hide()
        {
            if (popup != null) popup.SetActive(false);
            active = false;
            PauseManager.Instance?.Resume(PauseReason.PopupOpen);
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 800;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            gameObject.AddComponent<GraphicRaycaster>();

            popup = new GameObject("Popup", typeof(RectTransform));
            popup.transform.SetParent(transform, false);
            var rt = popup.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = popup.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(popup.transform, false);
            var prt = panelGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(900, 700);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.95f, 0.92f, 0.85f, 1f);

            AddText(panelGo.transform, "⏱ 시간 초과!", 70, new Vector2(0, 220));
            AddText(panelGo.transform, "광고를 시청하고 +30초 시간 보너스\n마나 100 / 콤보 0 / 쿨타임 0 복원", 40, new Vector2(0, 50));

            AddButton(panelGo.transform, "WATCH_AD", "📺 광고 시청 (+30초)", new Vector2(0, -130), OnWatchAd, new Color(0.30f, 0.69f, 0.31f));
            AddButton(panelGo.transform, "GIVE_UP", "포기", new Vector2(0, -280), OnGiveUp, new Color(0.70f, 0.70f, 0.70f));

            popup.SetActive(false);
        }

        private async void OnWatchAd()
        {
            var save = SaveSystem.Instance?.CurrentData;
            if (save != null && save.player.adContinueUsedToday >= Constants.ContinueDailyLimit)
            {
                PopupManager.Instance?.OpenAlert("오늘 이어하기 한도(3회)를 모두 사용했습니다.");
                return;
            }

            // 5초 모킹 — M8 광고 SDK 본 구현
            PopupManager.Instance?.OpenAlert("광고 시청 중... (5초 모킹)", 5f);
            await UniTask.Delay(TimeSpan.FromSeconds(5), ignoreTimeScale: true);

            // 복원
            if (StageTimer.Instance != null) StageTimer.Instance.ContinueRestoreTime(Constants.ContinueTimeBonusSec);
            if (ManaSystem.Instance != null) ManaSystem.Instance.SetManaDirect(Constants.ContinueManaRestore);
            ComboSystem.Instance?.ResetCombo();

            int newCount = 1;
            if (save != null)
            {
                save.player.adContinueUsedToday++;
                newCount = save.player.adContinueUsedToday;
                SaveSystem.Instance?.SaveImmediate(save);
            }

            EventBus.Publish(new OnContinueGranted
            {
                RestoredSeconds = Constants.ContinueTimeBonusSec,
                RestoredMana = Constants.ContinueManaRestore,
                ContinueCount = newCount
            });
            Hide();
            GameManager.Instance?.Resume();
        }

        private void OnGiveUp()
        {
            Hide();
            // Result 씬으로 전환 (실패)
            var ctx = new StageResultContext
            {
                cleared = false,
                grade = "C",
                clearTimeSec = 0f,
                totalTimeSec = StageTimer.Instance != null ? StageTimer.Instance.Total : Constants.StageDefaultTime
            };
            GameManager.Instance?.LoadResult(ctx).Forget();
        }

        private void AddText(Transform parent, string text, int size, Vector2 anchored)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(820, 200);
            rt.anchoredPosition = anchored;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = new Color(0.20f, 0.18f, 0.15f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void AddButton(Transform parent, string name, string label, Vector2 anchored, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700, 110);
            rt.anchoredPosition = anchored;
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
            t.fontSize = 44;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }
    }
}
