using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RPGPinball.Core;

namespace RPGPinball.UI
{
    /// <summary>
    /// 화면 페이드 인/아웃. DontDestroyOnLoad — 모든 씬 전환에서 공용.
    /// GameManager.LoadSceneAsync 가 호출.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        private Canvas canvas;
        private CanvasGroup group;
        private Image image;

        public static SceneFader EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SceneFader");
            return go.AddComponent<SceneFader>();
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
            BuildCanvas();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            scaler.matchWidthOrHeight = Constants.UICanvasMatchWidthOrHeight;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var imgGo = new GameObject("Fader_Image", typeof(RectTransform));
            imgGo.transform.SetParent(transform, false);
            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            image = imgGo.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            group = imgGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public async UniTask FadeOut(float seconds)
        {
            if (group == null) BuildCanvas();
            group.blocksRaycasts = true;
            float t = 0f;
            float start = group.alpha;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 1f, t / seconds);
                await UniTask.Yield();
            }
            group.alpha = 1f;
        }

        public async UniTask FadeIn(float seconds)
        {
            if (group == null) BuildCanvas();
            float t = 0f;
            float start = group.alpha;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, t / seconds);
                await UniTask.Yield();
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void SetAlphaImmediate(float alpha)
        {
            if (group == null) BuildCanvas();
            group.alpha = Mathf.Clamp01(alpha);
            group.blocksRaycasts = group.alpha > 0.01f;
        }
    }
}
