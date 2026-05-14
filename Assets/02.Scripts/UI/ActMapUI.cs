using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.UI
{
    /// <summary>
    /// ActMap 씬 컨트롤러. 30 노드 (5×6) 격자 배치 + 노드 클릭 → NodeInfoPopup → [출격].
    /// 본 마일스톤은 World Space 노드(Sprite) 대신 UI Image 그리드로 단순화.
    /// </summary>
    public class ActMapUI : MonoBehaviour
    {
        [Header("팔레트")]
        [SerializeField] private MapTilePalette palette;
        [SerializeField] private ActId currentAct = ActId.Act1_Spring;

        [Header("UI 참조")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RectTransform nodeContainer;

        private readonly List<NodeButton> nodes = new List<NodeButton>();

        public ActId CurrentAct => currentAct;
        public IReadOnlyList<NodeButton> Nodes => nodes;

        private void Awake()
        {
            if (worldCamera != null) worldCamera.orthographicSize = Constants.CameraActMapOrtho;
            if (nodeContainer == null) BuildContainer();
            BuildNodes();
            BuildTabs();
            BuildBackButton();
        }

        private void BuildContainer()
        {
            var canvasGo = new GameObject("ActMapCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Constants.UIReferenceWidth, Constants.UIReferenceHeight);
            scaler.matchWidthOrHeight = Constants.UICanvasMatchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();

            var container = new GameObject("NodeContainer", typeof(RectTransform));
            container.transform.SetParent(canvasGo.transform, false);
            nodeContainer = container.GetComponent<RectTransform>();
            nodeContainer.anchorMin = new Vector2(0.5f, 0.5f);
            nodeContainer.anchorMax = new Vector2(0.5f, 0.5f);
            nodeContainer.sizeDelta = new Vector2(900, 1500);
        }

        private void BuildNodes()
        {
            // 30 노드 — 5열×6행. 좌표 분포 (Resolution_Spec.md UIRef 기준 1080×1920)
            int rows = Constants.ActMapNodeRows;
            int cols = Constants.ActMapNodeColumns;
            float spacingX = 180f;
            float spacingY = 230f;
            float startX = -spacingX * (cols - 1) * 0.5f;
            float startY = -spacingY * (rows - 1) * 0.5f;

            // 30개 노드 종류 가중치: 일반 0.7 / 엘리트 0.1 / 보스 (10/20/30) / 휴식 / 이벤트 / 히든
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int idx = r * cols + c;
                    int stageIndex = idx + 1;
                    NodeKind kind = ClassifyNode(stageIndex);
                    var pos = new Vector2(startX + c * spacingX, startY + r * spacingY);
                    var node = BuildNode(stageIndex, kind, pos);
                    nodes.Add(node);
                }
            }
        }

        private NodeKind ClassifyNode(int stageIndex)
        {
            if (stageIndex == 10 || stageIndex == 20 || stageIndex == 30) return NodeKind.Boss;
            if (stageIndex % 7 == 0) return NodeKind.EliteBattle;
            if (stageIndex % 5 == 0) return NodeKind.Rest;
            if (stageIndex % 6 == 0) return NodeKind.Event;
            if (stageIndex == 13 || stageIndex == 23) return NodeKind.Hidden;
            return NodeKind.NormalBattle;
        }

        private NodeButton BuildNode(int stageIndex, NodeKind kind, Vector2 anchored)
        {
            var go = new GameObject($"Node_{stageIndex}_{kind}", typeof(RectTransform));
            go.transform.SetParent(nodeContainer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(140, 140);
            rt.anchoredPosition = anchored;
            var img = go.AddComponent<Image>();
            img.color = kind switch
            {
                NodeKind.Boss => new Color(0.85f, 0.20f, 0.20f),
                NodeKind.EliteBattle => new Color(0.70f, 0.30f, 0.85f),
                NodeKind.Rest => new Color(0.30f, 0.69f, 0.31f),
                NodeKind.Event => new Color(0.95f, 0.77f, 0.25f),
                NodeKind.Hidden => new Color(0.40f, 0.40f, 0.40f),
                _ => new Color(0.29f, 0.56f, 0.89f)
            };

            var palettePack = palette != null ? palette.GetPack(currentAct) : null;
            if (palettePack != null)
            {
                Sprite spr = kind switch
                {
                    NodeKind.Boss => palettePack.nodeBoss,
                    NodeKind.EliteBattle => palettePack.nodeElite,
                    NodeKind.Rest => palettePack.nodeRest,
                    NodeKind.Event => palettePack.nodeEvent,
                    NodeKind.Hidden => palettePack.nodeHidden,
                    _ => palettePack.nodeNormal
                };
                if (spr != null) img.sprite = spr;
            }

            var btn = go.AddComponent<Button>();
            int idx = stageIndex;
            NodeKind k = kind;
            btn.onClick.AddListener(() => OnNodeClicked(idx, k));

            // 라벨
            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.text = stageIndex.ToString();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 50;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;

            return new NodeButton { StageIndex = stageIndex, Kind = kind, GameObject = go, Button = btn };
        }

        private void BuildTabs()
        {
            var tabBar = new GameObject("ActTabs", typeof(RectTransform));
            tabBar.transform.SetParent(nodeContainer.parent, false);
            var rt = tabBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0, 130);
            rt.anchoredPosition = new Vector2(0, -Constants.UISafeAreaTopPx);

            string[] names = { "봄", "여름", "가을", "겨울" };
            for (int i = 0; i < 4; i++)
            {
                int actId = i + 1;
                var btnGo = new GameObject($"Act{actId}", typeof(RectTransform));
                btnGo.transform.SetParent(tabBar.transform, false);
                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.25f * i, 0);
                brt.anchorMax = new Vector2(0.25f * (i + 1), 1);
                brt.offsetMin = new Vector2(20, 10);
                brt.offsetMax = new Vector2(-20, -10);
                var img = btnGo.AddComponent<Image>();
                img.color = ((int)currentAct) == actId ? new Color(0.85f, 0.55f, 0.20f) : new Color(0.40f, 0.40f, 0.40f);
                var btn = btnGo.AddComponent<Button>();
                int targetAct = actId;
                btn.onClick.AddListener(() => SwitchAct((ActId)targetAct));
                var lblGo = new GameObject("Label", typeof(RectTransform));
                lblGo.transform.SetParent(btnGo.transform, false);
                var lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
                var t = lblGo.AddComponent<Text>();
                t.text = names[i];
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 50;
                t.color = Color.white;
                t.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void BuildBackButton()
        {
            var go = new GameObject("BackBtn", typeof(RectTransform));
            go.transform.SetParent(nodeContainer.parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(40, 80);
            rt.sizeDelta = new Vector2(220, 120);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.40f, 0.40f, 0.40f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => GameManager.Instance?.LoadVillage().Forget());
            var lblGo = new GameObject("Label", typeof(RectTransform));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.text = "← 마을";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 44;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
        }

        public void SwitchAct(ActId actId)
        {
            if (currentAct == actId) return;
            currentAct = actId;
            // 노드 재빌드
            foreach (var n in nodes) if (n.GameObject != null) Destroy(n.GameObject);
            nodes.Clear();
            BuildNodes();
        }

        private void OnNodeClicked(int stageIndex, NodeKind kind)
        {
            PopupManager.Instance?.OpenConfirm(
                $"Stage {stageIndex} - {kind}",
                $"이 노드로 출격하시겠습니까?\n액트: {currentAct}\n유형: {kind}",
                onConfirm: () =>
                {
                    // 절차 생성: 시드 → ProceduralStageGenerator.Generate → GameManager.LoadStage(blueprint)
                    string uid = SaveSystem.Instance?.CurrentData?.player?.playerUID ?? "actmap";
                    ulong seed = StageSeedFactory.BuildSeed(uid, StageSeedFactory.NowKst(), currentAct, stageIndex);
                    ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
                    var blueprint = ProceduralStageGenerator.Generate(currentAct, stageIndex, seed);
                    Debug.Log($"[ActMapUI] 출격 → {currentAct} S{stageIndex:00} kind={kind} seed={seed}");
                    GameManager.Instance?.LoadStage(blueprint).Forget();
                });
        }

        public class NodeButton
        {
            public int StageIndex;
            public NodeKind Kind;
            public GameObject GameObject;
            public Button Button;
        }
    }
}
