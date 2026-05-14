using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.UI
{
    /// <summary>
    /// Result 씬 표시 컨트롤러. GameManager.LastStageResult 참조.
    /// 클리어/실패 분기 + 등급 메달 DOTween 연출 + 자동 저장 트리거.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private Camera resultCamera;

        private Canvas canvas;
        private Image medalImage;
        private Text headlineText, detailText;
        private GameObject panel;

        private void Awake()
        {
            if (resultCamera != null)
                resultCamera.orthographicSize = Constants.CameraResultOrtho;
            BuildCanvas();
        }

        private void Start()
        {
            var result = GameManager.Instance?.LastStageResult ?? new StageResultContext { cleared = false, grade = "C" };
            DisplayResult(result);
            PublishAutoSaveTrigger(result);
        }

        private void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            scaler.matchWidthOrHeight = Constants.UICanvasMatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(transform, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);

            headlineText = NewText(panel.transform, "Headline", "", 100, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(900, 200));
            headlineText.color = Color.white;
            headlineText.alignment = TextAnchor.MiddleCenter;

            // 메달
            var medalGo = new GameObject("Medal", typeof(RectTransform));
            medalGo.transform.SetParent(panel.transform, false);
            var mrt = medalGo.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.anchoredPosition = new Vector2(0, 200);
            mrt.sizeDelta = new Vector2(400, 400);
            medalImage = medalGo.AddComponent<Image>();
            medalImage.color = Color.white;

            detailText = NewText(panel.transform, "Detail", "", 40, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(900, 400));
            detailText.color = Color.white;
            detailText.alignment = TextAnchor.MiddleCenter;

            AddButton(panel.transform, "MAP", "🗺 액트맵", new Vector2(-260, -750), () => GameManager.Instance?.LoadActMap().Forget(), new Color(0.29f, 0.56f, 0.89f));
            AddButton(panel.transform, "VILLAGE", "🏰 마을", new Vector2(0, -750), () => GameManager.Instance?.LoadVillage().Forget(), new Color(0.30f, 0.69f, 0.31f));
            AddButton(panel.transform, "RETRY", "↻ 재도전", new Vector2(260, -750), () => GameManager.Instance?.LoadStage(GameManager.Instance.PendingStageBlueprint).Forget(), new Color(0.85f, 0.55f, 0.20f));
        }

        public void DisplayResult(StageResultContext ctx)
        {
            if (ctx.cleared)
            {
                headlineText.text = $"클리어! ({ctx.grade})";
                medalImage.color = ctx.grade switch
                {
                    "S" => new Color(1f, 0.4f, 0.8f),
                    "A" => new Color(1f, 0.85f, 0.2f),
                    "B" => new Color(0.85f, 0.85f, 0.85f),
                    _ => new Color(0.7f, 0.5f, 0.3f)
                };
                medalImage.transform.localScale = Vector3.zero;
                medalImage.transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
                medalImage.transform.DORotate(new Vector3(0, 0, 360f), 0.5f, RotateMode.FastBeyond360).SetUpdate(true);

                detailText.text = $"클리어 시간: {ctx.clearTimeSec:F1}s / {ctx.totalTimeSec:F0}s\n최대 콤보: {ctx.maxCombo}\nXP +{ctx.xpReward}    Gold +{ctx.goldReward}";
            }
            else
            {
                headlineText.text = "시간 초과 — 실패";
                headlineText.color = new Color(1f, 0.3f, 0.3f, 1f);
                medalImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                detailText.text = $"보상 30% 지급\nXP +{ctx.xpReward}    Gold +{ctx.goldReward}";
            }
        }

        private void PublishAutoSaveTrigger(StageResultContext ctx)
        {
            // OnStageCleared (자동 저장 트리거)
            EventBus.Publish(new OnStageCleared { ActId = (ActId)ctx.actId, StageIndex = ctx.stageIndex, Grade = ctx.grade });
            if (SaveSystem.Instance != null && SaveSystem.Instance.CurrentData != null)
            {
                // bestGrade 갱신
                var save = SaveSystem.Instance.CurrentData;
                EnsureActProgress(save, ctx.actId).Apply(ctx);
                SaveSystem.Instance.RequestSave(save);
            }
        }

        private static ActProgress EnsureActProgress(SaveData save, int actId)
        {
            foreach (var ap in save.stageProgress.acts)
                if (ap.actId == actId) return ap;
            var fresh = new ActProgress { actId = actId, unlocked = true };
            save.stageProgress.acts.Add(fresh);
            return fresh;
        }

        // ── 유틸 ──
        private static Text NewText(Transform parent, string name, string txt, int size, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz;
            var t = go.AddComponent<Text>();
            t.text = txt;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            return t;
        }

        private static void AddButton(Transform parent, string name, string label, Vector2 anchored, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(240, 110);
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
            t.fontSize = 38;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }
    }

    /// <summary>
    /// ActProgress 확장 — Result 직후 stage 진행 상태 업데이트.
    /// </summary>
    public static class ActProgressExtensions
    {
        public static void Apply(this ActProgress ap, StageResultContext ctx)
        {
            for (int i = 0; i < ap.stages.Count; i++)
            {
                if (ap.stages[i].stageIndex == ctx.stageIndex)
                {
                    var s = ap.stages[i];
                    s.cleared = ctx.cleared || s.cleared;
                    if (BetterGrade(ctx.grade, s.bestGrade)) s.bestGrade = ctx.grade;
                    if (ctx.cleared && (s.bestTimeSec <= 0f || ctx.clearTimeSec < s.bestTimeSec))
                        s.bestTimeSec = ctx.clearTimeSec;
                    s.continueCount += ctx.continueCount;
                    ap.stages[i] = s;
                    return;
                }
            }
            ap.stages.Add(new StageProgressEntry
            {
                stageIndex = ctx.stageIndex,
                cleared = ctx.cleared,
                bestGrade = ctx.grade,
                bestTimeSec = ctx.cleared ? ctx.clearTimeSec : 0f,
                continueCount = ctx.continueCount
            });
        }

        private static int GradeRank(string g) => g switch { "S" => 4, "A" => 3, "B" => 2, "C" => 1, _ => 0 };
        public static bool BetterGrade(string a, string b) => GradeRank(a) > GradeRank(b);
    }
}
