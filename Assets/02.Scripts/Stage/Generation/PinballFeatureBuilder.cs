using UnityEngine;
using RPGPinball.Stage.Segments;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 핀볼 정석 시각·구조 강조 요소(슬링샷 삼각형, 중앙 가이드 삼각형, 중앙 강조 범퍼)를
    /// 절차 생성 결과 위에 덧붙이는 빌더.
    /// SegmentLayoutBuilder 가 middle 을 정확히 3개로 보장하므로 (Sling=middle[0], Mid=middle[1], Bumper=middle[2])
    /// 각 zone 의 가운데에 정적 형상을 추가한다.
    /// </summary>
    public static class PinballFeatureBuilder
    {
        // ── 슬링샷 ──────────────────────────────────────────────
        // 2026-05-14: 외벽 안쪽 여유 3.0U 유지 (13U → 16.9U 확장 대응). 슬링샷 도형 크기는 절대값 유지.
        private const float SlingHalfW = 2.0f;            // 한 슬링샷 가로 절반
        private const float SlingHalfH = 1.5f;            // 한 슬링샷 세로 절반
        private const float SlingCenterX = 5.45f;         // 슬링샷 중심 x (외벽 ±8.45, 안쪽 3.0U 여유)
        private const float SlingYBias = -0.5f;           // segment 중심에서 약간 아래

        // ── 중앙 가이드 삼각형 (MidPlay 가운데 세로 3개) ───────
        private const float CenterArrowHalfW = 0.55f;
        private const float CenterArrowHalfH = 0.6f;
        private const float CenterArrowSpacingY = 1.5f;

        // ── 중앙 강조 범퍼 (BumperCluster) ───────────────────────
        private const float CenterBumperRadius = 1.1f;
        private const float SideBumperRadius = 0.7f;
        // 2026-05-14: 외벽 안쪽 여유 2.9U 유지 (13U → 16.9U 확장 대응)
        private const float SideBumperOffsetX = 5.55f;

        // ── 색 (참조 이미지 톤) ─────────────────────────────────
        private static readonly Color SlingFillColor = new(0.99f, 0.32f, 0.51f, 1f);   // 분홍
        private static readonly Color SlingRimColor  = new(1.00f, 1.00f, 1.00f, 1f);   // 흰 외곽선
        private static readonly Color ArrowColor     = new(1.00f, 0.78f, 0.20f, 1f);   // 노랑
        private static readonly Color ArrowRimColor  = new(0.55f, 0.35f, 0.05f, 1f);
        private static readonly Color BumperColor    = new(0.95f, 0.20f, 0.20f, 1f);   // 빨강
        private static readonly Color BumperSideColor = new(0.20f, 0.55f, 0.95f, 1f);  // 파랑
        private static readonly Color BumperRimColor  = new(1.00f, 1.00f, 1.00f, 1f);

        private const float OutlineThickness = 0.18f;

        public static void Build(StageRuntimeBuilder.StageRuntime rt)
        {
            if (rt == null) return;
            var mat = Resources.Load<PhysicsMaterial2D>("WallBouncy");

            // middle[0] = SlingshotZone
            if (rt.middleSegments.Count >= 1)
            {
                BuildSlingshots(rt.middleSegments[0], mat);
            }
            // middle[1] = MidPlay
            if (rt.middleSegments.Count >= 2)
            {
                BuildCenterArrows(rt.middleSegments[1], mat);
            }
            // middle[2] = BumperCluster
            if (rt.middleSegments.Count >= 3)
            {
                BuildCenterBumpers(rt.middleSegments[2], mat);
            }
        }

        private static void BuildSlingshots(SegmentRuntime seg, PhysicsMaterial2D mat)
        {
            var parent = seg.transform;
            // 좌측 — 직각이 좌하, 빗변이 좌상↘우하 (공이 위에서 떨어지면 우상으로 반사)
            Vector2[] left = {
                new(-SlingHalfW, -SlingHalfH),
                new(-SlingHalfW, +SlingHalfH),
                new(+SlingHalfW, -SlingHalfH),
            };
            CreateTriangleWithOutline(parent, "Slingshot_Left",
                new Vector3(-SlingCenterX, SlingYBias, 0f),
                left, SlingFillColor, SlingRimColor, mat, sortingOrder: 3);

            // 우측 — 거울 대칭
            Vector2[] right = {
                new(+SlingHalfW, -SlingHalfH),
                new(+SlingHalfW, +SlingHalfH),
                new(-SlingHalfW, -SlingHalfH),
            };
            CreateTriangleWithOutline(parent, "Slingshot_Right",
                new Vector3(+SlingCenterX, SlingYBias, 0f),
                right, SlingFillColor, SlingRimColor, mat, sortingOrder: 3);
        }

        private static void BuildCenterArrows(SegmentRuntime seg, PhysicsMaterial2D mat)
        {
            var parent = seg.transform;
            // 위쪽이 뾰족한 작은 삼각형 3개, segment 중앙 세로축에 일정 간격
            Vector2[] verts = {
                new(-CenterArrowHalfW, -CenterArrowHalfH),
                new(+CenterArrowHalfW, -CenterArrowHalfH),
                new(0f, +CenterArrowHalfH),
            };
            for (int i = -1; i <= 1; i++)
            {
                float y = i * CenterArrowSpacingY;
                CreateTriangleWithOutline(parent, $"CenterArrow_{i + 1}",
                    new Vector3(0f, y, 0f),
                    verts, ArrowColor, ArrowRimColor, mat, sortingOrder: 4);
            }
        }

        private static void BuildCenterBumpers(SegmentRuntime seg, PhysicsMaterial2D mat)
        {
            var parent = seg.transform;
            // 중앙 큰 빨간 범퍼 + 좌·우 작은 파란 범퍼 (참조 25/100 동그라미)
            CreateCircleWithRing(parent, "CenterBumper",
                new Vector3(0f, 0f, 0f), CenterBumperRadius,
                BumperColor, BumperRimColor, mat, sortingOrder: 4);
            CreateCircleWithRing(parent, "SideBumper_Left",
                new Vector3(-SideBumperOffsetX, -0.4f, 0f), SideBumperRadius,
                BumperSideColor, BumperRimColor, mat, sortingOrder: 4);
            CreateCircleWithRing(parent, "SideBumper_Right",
                new Vector3(+SideBumperOffsetX, -0.4f, 0f), SideBumperRadius,
                BumperSideColor, BumperRimColor, mat, sortingOrder: 4);
        }

        // ── 도형 생성 헬퍼 ──────────────────────────────────────

        /// <summary>외곽선 삼각형(아래 sortingOrder) + 채움 삼각형(위 sortingOrder).</summary>
        private static GameObject CreateTriangleWithOutline(Transform parent, string name, Vector3 localPos,
            Vector2[] verts, Color fill, Color rim, PhysicsMaterial2D mat, int sortingOrder)
        {
            // 외곽선 — 같은 중심에서 약간 확장한 삼각형 (vertex 평균 → 바깥 방향 OutlineThickness)
            Vector2 centroid = (verts[0] + verts[1] + verts[2]) / 3f;
            var outlineVerts = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                Vector2 dir = (verts[i] - centroid).normalized;
                outlineVerts[i] = verts[i] + dir * OutlineThickness;
            }
            // 외곽선은 collider 없이 시각만 (충돌은 채움 삼각형의 PolygonCollider2D 가 담당)
            var outline = new GameObject(name + "_Outline");
            outline.transform.SetParent(parent, false);
            outline.transform.localPosition = localPos;
            AttachTriangleMesh(outline, outlineVerts, rim, sortingOrder - 1);

            return CreateTriangleShape(parent, name, localPos, verts, fill, rim, mat, sortingOrder);
        }

        private static GameObject CreateTriangleShape(Transform parent, string name, Vector3 localPos,
            Vector2[] verts, Color fill, Color rim, PhysicsMaterial2D mat, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.tag = RPGPinball.Core.Constants.TagWall;

            // 충돌
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.points = verts;
            if (mat != null) poly.sharedMaterial = mat;

            AttachTriangleMesh(go, verts, fill, sortingOrder);
            return go;
        }

        private static void AttachTriangleMesh(GameObject go, Vector2[] verts, Color fill, int sortingOrder)
        {
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = go.name + "_Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(verts[0].x, verts[0].y, 0),
                new Vector3(verts[1].x, verts[1].y, 0),
                new Vector3(verts[2].x, verts[2].y, 0),
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.colors = new[] { fill, fill, fill };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            var renderMat = new Material(Shader.Find("Sprites/Default"));
            renderMat.color = fill;
            mr.sharedMaterial = renderMat;
            mr.sortingOrder = sortingOrder;
        }

        /// <summary>흰 링(아래) + 채움 원반(위) — 핀볼 범퍼 스타일.</summary>
        private static GameObject CreateCircleWithRing(Transform parent, string name, Vector3 localPos,
            float radius, Color fill, Color rim, PhysicsMaterial2D mat, int sortingOrder)
        {
            // 링 — 같은 위치에 살짝 큰 원 (시각만)
            var ringGo = new GameObject(name + "_Ring");
            ringGo.transform.SetParent(parent, false);
            ringGo.transform.localPosition = localPos;
            AttachCircleMesh(ringGo, radius + OutlineThickness, rim, sortingOrder - 1);
            return CreateCircleShape(parent, name, localPos, radius, fill, mat, sortingOrder);
        }

        private static GameObject CreateCircleShape(Transform parent, string name, Vector3 localPos,
            float radius, Color fill, PhysicsMaterial2D mat, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.tag = RPGPinball.Core.Constants.TagWall;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius;
            if (mat != null) col.sharedMaterial = mat;

            AttachCircleMesh(go, radius, fill, sortingOrder);
            return go;
        }

        private static void AttachCircleMesh(GameObject go, float radius, Color fill, int sortingOrder)
        {
            const int segments = 32;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            var mesh = new Mesh { name = go.name + "_Mesh" };
            var verts = new Vector3[segments + 1];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % segments + 1;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            var renderMat = new Material(Shader.Find("Sprites/Default"));
            renderMat.color = fill;
            mr.sharedMaterial = renderMat;
            mr.sortingOrder = sortingOrder;
        }
    }
}
