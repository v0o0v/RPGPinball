using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.UI
{
    /// <summary>
    /// 5종 팝업(확인/알림/보상/가이드/설정) 통합 관리자. 싱글턴, DontDestroyOnLoad.
    /// 본 마일스톤 7: 코드 기반 UI 생성. 외부 프리팹 미사용 — Editor 작업 부담 최소화.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        private Canvas rootCanvas;
        // PauseMenuUI(500) / ContinueAdPopup(800) 보다 위에 그려져야 하므로 1100 부터 시작.
        // 각 팝업 인스턴스는 overrideSorting=true 라 자체 sortingOrder 로 그려진다.
        private int sortOrderCounter = 1100;
        private readonly Stack<PopupHandle> stack = new Stack<PopupHandle>();

        public int OpenCount => stack.Count;

        public static PopupManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PopupManager");
            return go.AddComponent<PopupManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildRootCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildRootCanvas()
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 1000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            scaler.matchWidthOrHeight = Constants.UICanvasMatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // ── 5종 API ──────────────────────────────────────────

        public PopupHandle OpenConfirm(string title, string message, Action onConfirm, Action onCancel = null)
        {
            var go = BuildBasePopup("ConfirmPopup");
            var panel = AttachPanel(go.transform, 800, 500, new Color(0.93f, 0.87f, 0.70f, 1f));
            AddTitle(panel.transform, title, 60);
            AddMessage(panel.transform, message, 60, -50, 700, 200);
            AddButton(panel.transform, "확인", new Vector2(-160, -180), () => { onConfirm?.Invoke(); Close(go); }, new Color(0.30f, 0.69f, 0.31f));
            AddButton(panel.transform, "취소", new Vector2(160, -180), () => { onCancel?.Invoke(); Close(go); }, new Color(0.70f, 0.70f, 0.70f));
            return Push(go, "Confirm");
        }

        public PopupHandle OpenAlert(string message, float autoCloseSec = -1f)
        {
            var go = BuildBasePopup("AlertPopup");
            var panel = AttachPanel(go.transform, 800, 400, new Color(0.95f, 0.95f, 0.95f, 1f));
            AddMessage(panel.transform, message, 60, 0, 700, 300);
            var handle = Push(go, "Alert");
            float t = autoCloseSec > 0 ? autoCloseSec : Constants.UIAlertAutoCloseSec;
            AutoClose(go, t);
            return handle;
        }

        public PopupHandle OpenReward(IList<(int currencyId, int amount)> rewards)
        {
            var go = BuildBasePopup("RewardPopup");
            var panel = AttachPanel(go.transform, 900, 700, new Color(0.99f, 0.98f, 0.92f, 1f));
            AddTitle(panel.transform, "보상 획득", 60);
            string body = string.Empty;
            if (rewards != null)
            {
                foreach (var r in rewards)
                {
                    body += $"{((CurrencyId)r.currencyId)}: +{r.amount}\n";
                }
            }
            AddMessage(panel.transform, body, 60, -50, 800, 400);
            AddButton(panel.transform, "수령", new Vector2(0, -280), () => Close(go), new Color(0.30f, 0.69f, 0.31f));
            return Push(go, "Reward");
        }

        public PopupHandle OpenGuide(string guideId, string message, bool hideForever = true)
        {
            var go = BuildBasePopup("GuidePopup");
            var panel = AttachPanel(go.transform, 800, 600, new Color(0.90f, 0.95f, 1.0f, 1f));
            AddTitle(panel.transform, "가이드", 60);
            AddMessage(panel.transform, message, 60, -30, 700, 360);
            AddButton(panel.transform, "확인", new Vector2(0, -250), () => Close(go), new Color(0.29f, 0.56f, 0.89f));
            return Push(go, "Guide");
        }

        public PopupHandle OpenSettings()
        {
            var go = BuildBasePopup("SettingsPopup");
            var panel = AttachPanel(go.transform, 900, 1200, new Color(0.85f, 0.82f, 0.74f, 1f));
            AddTitle(panel.transform, "설정", 60);
            AddMessage(panel.transform, "BGM / SFX 슬라이더 / 그래픽 품질 / Haptic / 접근성 5종 / 계정 연동 / 클라우드 동기화\n\n(본 마일스톤은 골격 표시만 — M8 본 구현 인계)", 60, -50, 800, 700);
            AddButton(panel.transform, "닫기", new Vector2(0, -500), () => Close(go), new Color(0.70f, 0.70f, 0.70f));
            return Push(go, "Settings");
        }

        // ── 내부 빌더 ────────────────────────────────────────

        private GameObject BuildBasePopup(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // 반투명 배경
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrderCounter++;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private GameObject AttachPanel(Transform parent, float w, float h, Color color)
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            var img = panelGo.AddComponent<Image>();
            img.color = color;
            // 등장 애니메이션 — SetTarget 등록으로 CloseAll/Destroy 시 Kill 매칭 가능
            panelGo.transform.localScale = Vector3.one * 0.8f;
            panelGo.transform.DOScale(1f, Constants.UIPopupFadeInSec)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetTarget(panelGo.transform);
            return panelGo;
        }

        private void AddTitle(Transform parent, string text, float topMargin)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -topMargin);
            rt.sizeDelta = new Vector2(700, 70);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 48;
            t.color = new Color(0.20f, 0.18f, 0.15f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
        }

        private void AddMessage(Transform parent, string text, float topMargin, float vert, float w, float h)
        {
            var go = new GameObject("Message", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, vert);
            rt.sizeDelta = new Vector2(w, h);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 32;
            t.color = new Color(0.20f, 0.18f, 0.15f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void AddButton(Transform parent, string label, Vector2 anchoredPos, Action onClick, Color tint)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(anchoredPos.x, -anchoredPos.y + 30); // 하단 기준 30 마진
            rt.sizeDelta = new Vector2(260, 90);
            var img = go.AddComponent<Image>();
            img.color = tint;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = labelGo.AddComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 36;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }

        private PopupHandle Push(GameObject popup, string popupId)
        {
            var handle = new PopupHandle(popup, popupId);
            stack.Push(handle);
            EventBus.Publish(new OnPopupOpened { PopupId = popupId });
            return handle;
        }

        private void Close(GameObject popup)
        {
            if (popup == null) return;
            var id = popup.name;
            // 등장 트윈이 있다면 즉시 종료 — 씬 전환과 사라짐 트윈 race 방지를 위해 즉시 Destroy
            var panel = popup.transform.Find("Panel");
            if (panel != null) DOTween.Kill(panel);
            DOTween.Kill(popup.transform);
            // 스택에서 제거 (최상단 가정)
            if (stack.Count > 0 && stack.Peek().GameObject == popup) stack.Pop();
            EventBus.Publish(new OnPopupClosed { PopupId = id });
            Destroy(popup);
        }

        public void CloseAll()
        {
            foreach (var h in stack) {
                if (h.GameObject == null) continue;
                // 자식 panel 의 DOScale 등장 트윈을 먼저 Kill (씬 전환 후 setter 가 파괴된 transform 접근 방지)
                var panel = h.GameObject.transform.Find("Panel");
                if (panel != null) DOTween.Kill(panel);
                DOTween.Kill(h.GameObject.transform);
                Destroy(h.GameObject);
            }
            stack.Clear();
        }

        private async void AutoClose(GameObject go, float delaySec)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(delaySec), ignoreTimeScale: true);
            if (go != null) Close(go);
        }
    }

    public class PopupHandle
    {
        public GameObject GameObject { get; }
        public string PopupId { get; }
        public PopupHandle(GameObject go, string id) { GameObject = go; PopupId = id; }
    }
}
