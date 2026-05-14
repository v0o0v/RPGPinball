using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Stage.Segments;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// StageBlueprint → 실제 Scene 단위 GameObject 인스턴스화 빌더.
    /// 세그먼트 프리팹 → 기믹 슬롯 앵커 배치 → 모디파이어 적용 → 카메라 바인딩.
    /// 마일스톤 5는 데이터 정합성 우선 — 시각 폴리싱은 마일스톤 8 인계.
    /// </summary>
    public class StageRuntimeBuilder
    {
        public class StageRuntime
        {
            public StageBlueprint blueprint;
            public Transform root;
            public SegmentRuntime topSegment;
            public SegmentRuntime bottomSegment;
            public readonly List<SegmentRuntime> middleSegments = new();
            public readonly List<GameObject> spawnedGimmicks = new();

            public void Dispose()
            {
                if (root != null) Object.Destroy(root.gameObject);
            }
        }

        /// <summary>
        /// Blueprint + SegmentLayout 으로 실제 GameObject 트리 구축.
        /// SegmentPool / GimmickPool 의 SO 프리팹이 존재할 경우에만 실제 인스턴스화.
        /// SO 프리팹이 없으면 빈 컨테이너만 생성 (테스트/플레이스홀더 모드).
        /// </summary>
        public StageRuntime Build(StageBlueprint blueprint, SegmentLayout layout, Transform parent = null)
        {
            var rt = new StageRuntime { blueprint = blueprint };

            var rootGo = new GameObject($"Stage_{blueprint.actId}_S{blueprint.stageIndex:00}");
            if (parent != null) rootGo.transform.SetParent(parent, false);
            rt.root = rootGo.transform;

            // 세그먼트 배치 — 하단을 y=0 기준으로 위로 누적
            float cursorY = 0f;
            rt.bottomSegment = InstantiateSegment(layout.bottom, layout.bottomHeight, rt.root, cursorY, "Bottom");
            cursorY += layout.bottomHeight;

            for (int i = 0; i < layout.middles.Count; i++)
            {
                var seg = layout.middles[i];
                float h = i < layout.middleHeights.Count ? layout.middleHeights[i] : seg != null ? seg.height : 3.5f;
                var inst = InstantiateSegment(seg, h, rt.root, cursorY, $"Mid_{i}");
                // middle[0]=Slingshot, middle[2]=BumperCluster — 외측 X 영역에 정적 형상이 있어 격자 outer col(±6.45) 사용 불가.
                // inner col(±2.15) 만 사용해 침투 방지. middle[1]=MidPlay 는 중앙 화살표만이라 outer col OK.
                if (inst != null && (i == 0 || i == 2)) inst.UseInnerColsOnly = true;
                rt.middleSegments.Add(inst);
                cursorY += h;
            }

            rt.topSegment = InstantiateSegment(layout.top, layout.topHeight, rt.root, cursorY, "Top");

            // 스테이지 외곽 좌·우 벽 — 통짜로 만들어 세그먼트 사이 끊김 방지
            BuildPerimeterWalls(rt, blueprint, layout);

            // 핀볼 정석 시각·구조 강조 요소 (2026-05-15): 슬링샷·중앙 가이드·강조 범퍼.
            // 격자 기믹 위에 덧붙이는 정적 형상이므로 SpawnGimmicks 이전에 배치해 sortingOrder 정합.
            PinballFeatureBuilder.Build(rt);

            // 기믹 배치 — segmentIndex 0=top, 1..N=middle, N+1=bottom (블루프린트 컨벤션)
            SpawnGimmicks(rt, blueprint);

            // 카메라 폭을 스테이지 폭에 맞춤 + 상·하 외벽까지 화면에 보이도록 Y 클램프 등록.
            // 2026-05-15: 상단 아치(반원) 추가로 화면 상한이 totalHeight + halfW + cornerOverlap.
            if (RPGPinball.Physics.CameraController.Instance != null)
            {
                float stageWidth = RPGPinball.Core.Constants.SegPlayfieldWidth;
                const float wallThickness = 0.5f; // BuildPerimeterWalls 와 동기화
                const float cornerOverlap = 1f;
                float archTop = layout.totalHeight + cornerOverlap + stageWidth * 0.5f + wallThickness;
                RPGPinball.Physics.CameraController.Instance.FitToStageBounds(
                    stageWidth, centerX: 0f,
                    stageBottomY: -wallThickness,
                    stageTopY: archTop);
            }

            return rt;
        }

        /// <summary>
        /// Stage 외곽 좌·우·상·하 4면 벽을 한 번에 생성. 액트 sprite는 StageWallPalette에서 로드.
        /// 데드존 제거(2026-05-13) 이후 외벽이 통째로 닫힌 통 구조를 보장한다.
        /// 좌·우 벽은 코너 여유(+1U)를 두어 모서리 빈틈으로 빠져나가는 사례를 방지.
        /// </summary>
        private void BuildPerimeterWalls(StageRuntime rt, StageBlueprint blueprint, SegmentLayout layout)
        {
            float w = RPGPinball.Core.Constants.SegPlayfieldWidth;
            float h = layout.totalHeight;
            if (h <= 0f) return;

            const float wallThickness = 0.5f;
            const float cornerOverlap = 1f; // 좌·우 벽을 위·아래로 약간 더 길게 빼 코너 빈틈 방지
            float midY = h * 0.5f;

            var palette = Resources.Load<RPGPinball.Data.StageWallPalette>("StageWallPalette");
            Sprite sprite = palette != null ? palette.GetForAct(blueprint.actId) : null;
            var material = Resources.Load<PhysicsMaterial2D>("WallBouncy");

            // 좌·우 외벽 — totalHeight + cornerOverlap 만큼 위·아래로 여유
            float sideHeight = h + cornerOverlap * 2f;
            CreatePerimeterWall(rt.root, "PerimeterWall_Left",
                new Vector2(-w * 0.5f + wallThickness * 0.5f, midY),
                new Vector2(wallThickness, sideHeight), sprite, material);
            CreatePerimeterWall(rt.root, "PerimeterWall_Right",
                new Vector2(w * 0.5f - wallThickness * 0.5f, midY),
                new Vector2(wallThickness, sideHeight), sprite, material);

            // 하단 닫는 가로 벽 — 양 끝 좌·우 벽과 겹치도록 너비를 w로 잡음
            CreatePerimeterWall(rt.root, "PerimeterWall_Bottom",
                new Vector2(0f, -wallThickness * 0.5f),
                new Vector2(w, wallThickness), sprite, material);

            // 상단 아치 — 직선 벽 대신 반원으로 둥글게 (참조 핀볼판 매칭, 2026-05-15)
            BuildTopArch(rt.root, w, h + cornerOverlap, wallThickness, sprite, material);
        }

        /// <summary>
        /// 상단 외벽을 반원으로 분절 생성. archBaseY 에서 시작해 좌·우 외벽 끝과 정확히 맞물림.
        /// 각 segment 는 BoxCollider2D + SpriteRenderer(Sliced) 로 회전 배치.
        /// </summary>
        private void BuildTopArch(Transform parent, float playfieldWidth, float archBaseY, float wallThickness, Sprite sprite, PhysicsMaterial2D material)
        {
            const int archSegments = 18;
            float halfW = playfieldWidth * 0.5f;
            float radius = halfW;
            // 반원 중심을 archBaseY 보다 wallThickness/2 안쪽에 두면 collider 가 직선 외벽과 매끄럽게 이어짐
            float centerY = archBaseY;

            for (int i = 0; i < archSegments; i++)
            {
                float t1 = Mathf.PI - (i / (float)archSegments) * Mathf.PI;
                float t2 = Mathf.PI - ((i + 1) / (float)archSegments) * Mathf.PI;
                Vector2 p1 = new(Mathf.Cos(t1) * radius, centerY + Mathf.Sin(t1) * radius);
                Vector2 p2 = new(Mathf.Cos(t2) * radius, centerY + Mathf.Sin(t2) * radius);
                Vector2 mid = (p1 + p2) * 0.5f;
                Vector2 delta = p2 - p1;
                float len = delta.magnitude + 0.08f; // 인접 segment 와 살짝 겹쳐 빈틈 방지
                float angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                CreateArchSegment(parent, $"PerimeterWall_Arch_{i:00}",
                    mid, angleDeg, new Vector2(len, wallThickness), sprite, material);
            }
        }

        private void CreateArchSegment(Transform parent, string name, Vector2 pos, float angleDeg, Vector2 size, Sprite sprite, PhysicsMaterial2D material)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            wall.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            wall.tag = RPGPinball.Core.Constants.TagWall;

            var bc = wall.AddComponent<BoxCollider2D>();
            bc.size = size;
            if (material != null) bc.sharedMaterial = material;

            var sr = wall.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = -1;
        }

        private void CreatePerimeterWall(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, PhysicsMaterial2D material)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            wall.tag = RPGPinball.Core.Constants.TagWall;

            var bc = wall.AddComponent<BoxCollider2D>();
            bc.size = size;
            if (material != null) bc.sharedMaterial = material;

            var sr = wall.AddComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            sr.color = Color.white;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = -1; // 본체·천장/바닥보다 위
        }

        private SegmentRuntime InstantiateSegment(SegmentData data, float height, Transform parent, float y, string label)
        {
            var go = new GameObject($"Segment_{label}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);

            // SO 프리팹이 있으면 자식으로 인스턴스화 (시각 표현)
            if (data != null && data.prefab != null)
            {
                var child = Object.Instantiate(data.prefab, go.transform);
                child.transform.localPosition = Vector3.zero;
            }

            var rt = go.AddComponent<SegmentRuntime>();
            rt.Initialize(data, height);
            return rt;
        }

        private void SpawnGimmicks(StageRuntime rt, StageBlueprint blueprint)
        {
            var pool = GimmickPool.Instance;
            pool.EnsureLoaded();

            for (int i = 0; i < blueprint.gimmickPlacements.Count; i++)
            {
                var entry = blueprint.gimmickPlacements[i];
                var data = pool.Get(entry.id);
                if (data == null) continue;

                var segRt = ResolveSegment(rt, entry.segmentIndex);
                if (segRt == null) continue;

                Vector2 worldPos = segRt.GetSlotAnchorWorld(entry.slotIndex);
                GameObject gimmick;
                if (data.prefab != null)
                {
                    gimmick = Object.Instantiate(data.prefab, worldPos, Quaternion.identity, segRt.transform);
                }
                else
                {
                    // 프리팹 없을 때 빈 placeholder
                    gimmick = new GameObject($"Gimmick_{data.gimmickId}");
                    gimmick.transform.SetParent(segRt.transform, true);
                    gimmick.transform.position = worldPos;
                }
                segRt.RegisterGimmick(gimmick);
                rt.spawnedGimmicks.Add(gimmick);
            }
        }

        private SegmentRuntime ResolveSegment(StageRuntime rt, int segmentIndex)
        {
            // 컨벤션: 0=top, 1..N=middle (1-based), N+1=bottom
            if (segmentIndex == 0) return rt.topSegment;
            int midIndex = segmentIndex - 1;
            if (midIndex >= 0 && midIndex < rt.middleSegments.Count) return rt.middleSegments[midIndex];
            return rt.bottomSegment;
        }
    }
}
