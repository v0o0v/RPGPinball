#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Stage.Gimmicks;
using RPGPinball.Stage.Gimmicks.Common;
using RPGPinball.Stage.Segments;

namespace RPGPinball.EditorTools
{
    /// <summary>
    /// 마일스톤 5: 세그먼트 / 기믹 / 몬스터 프리팹을 형태 맞춤 ProtoSprite 와 함께 자동 생성.
    /// 메뉴: RPG Pinball > Bootstrap > Generate Milestone 5 Prefabs
    /// </summary>
    public static class Stage5PrefabGenerator
    {
        private const string SPRITE_DIR = "Assets/03.Sprites/StageProto";
        private const string PREFAB_DIR = "Assets/05.Prefabs/Stage";
        private const int SPRITE_SIZE = 128;

        // 카테고리별 컬러 키
        private static readonly Color RewardColor = new Color(1.0f, 0.85f, 0.2f); // 금색
        private static readonly Color BuffColor = new Color(0.4f, 0.8f, 1.0f);    // 하늘
        private static readonly Color TrialColor = new Color(0.95f, 0.25f, 0.25f); // 빨강
        private static readonly Color DebuffColor = new Color(0.7f, 0.3f, 0.9f);  // 보라
        private static readonly Color EnvColor = new Color(0.55f, 0.55f, 0.55f);   // 회색

        // 테마별 컬러
        private static readonly Color SpringTint = new Color(0.7f, 1f, 0.7f);
        private static readonly Color SummerTint = new Color(0.5f, 0.8f, 1f);
        private static readonly Color AutumnTint = new Color(1f, 0.7f, 0.4f);
        private static readonly Color WinterTint = new Color(0.85f, 0.95f, 1f);
        private static readonly Color CommonTint = new Color(0.85f, 0.85f, 0.85f);

        // Kenney 자산 경로 (커뮤니티 가이드: kenney 폴더 우선 활용)
        private const string KENNEY_BALL_DIR = "Assets/50. External Assets/kenny/kenney_rolling-ball-assets/PNG/Default";

        [MenuItem("RPG Pinball/Bootstrap/Convert Gimmick Colliders To Solid")]
        public static void ConvertGimmickCollidersToSolid()
        {
            int converted = 0;
            // Bouncy material 미리 로드 (벽이랑 같은 머티리얼 재사용 — bounciness 1.0)
            var bouncy = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Resources/WallBouncy.physicsMaterial2D");

            foreach (var guid in AssetDatabase.FindAssets("GimmickPrefab_", new[] { PREFAB_DIR }))
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                var col = root.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = false;
                    if (bouncy != null) col.sharedMaterial = bouncy;
                }
                // 정적 Rigidbody2D 추가 (반발 계산을 위해 필요)
                var rb = root.GetComponent<Rigidbody2D>();
                if (rb == null) rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                converted++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage5PrefabGenerator] {converted}건 기믹 prefab 솔리드 콜라이더 + Static Rigidbody2D 적용.");
        }

        [MenuItem("RPG Pinball/Bootstrap/Apply Kenney Sprites To Prefabs")]
        public static void ApplyKenneySprites()
        {
            // 카테고리별 prefab → kenney sprite 매핑
            var map = new System.Collections.Generic.Dictionary<string, string>
            {
                { "GimmickPrefab_Reward",      "star" },
                { "GimmickPrefab_Buff",        "button_yellow" },
                { "GimmickPrefab_Trial",       "block_locked_small" },
                { "GimmickPrefab_Debuff",      "hole" },
                { "GimmickPrefab_Environment", "block_rotate_narrow" },
                // 본 구현 6종 — 의미에 맞춰 매핑
                { "GimmickPrefab_HiddenBumper", "ball_red_small_alt" },     // 숨겨진 범퍼 = 작은 빨간 공
                { "GimmickPrefab_JumpPad",      "button_blue" },             // 점프 패드 = 파란 버튼
                { "GimmickPrefab_ManaShrine",   "ball_blue_small_alt" },     // 마나 제단 = 작은 파란 공
                { "GimmickPrefab_TimeOrb",      "star_outline" },            // 시간 구슬 = 별 윤곽
                { "GimmickPrefab_AccelRail",    "laser" },                   // 가속 레일 = 레이저
                { "GimmickPrefab_Wormhole",     "hole_large" },              // 웜홀 = 큰 구멍
            };

            int applied = 0, skipped = 0;
            foreach (var kv in map)
            {
                string prefabPath = $"{PREFAB_DIR}/{kv.Key}.prefab";
                string spritePath = $"{KENNEY_BALL_DIR}/{kv.Value}.png";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (prefab == null || sprite == null) { skipped++; continue; }

                // PrefabUtility로 직접 prefab asset 수정
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                var sr = root.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = sprite;
                    sr.color = Color.white; // 원본 색상 유지
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    applied++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Ball prefab (Sample/ProcStage_Test 씬 안 GameObject 'Ball' 의 SpriteRenderer 도 별도 처리 필요)
            // 본 메뉴는 prefab 자산만 갱신. 씬 인스턴스는 ApplyKenneyToBall 별도 메뉴.

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Stage5PrefabGenerator] Kenney sprite 적용 완료: {applied}건 적용, {skipped}건 스킵");
        }

        [MenuItem("RPG Pinball/Bootstrap/Apply Kenney Sprite To Ball")]
        public static void ApplyKenneySpriteToBall()
        {
            // 씬의 Ball GameObject SpriteRenderer 갱신
            var ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{KENNEY_BALL_DIR}/ball_blue_large.png");
            if (ballSprite == null) { Debug.LogError("Kenney ball sprite not found"); return; }

            int hit = 0;
            foreach (var go in GameObject.FindGameObjectsWithTag("Ball"))
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.sprite = ballSprite; sr.color = Color.white; hit++; }
            }
            Debug.Log($"[Stage5PrefabGenerator] Ball sprite 적용: 씬 인스턴스 {hit}개 갱신");
        }

        [MenuItem("RPG Pinball/Bootstrap/Generate Milestone 5 Prefabs")]
        public static void Run()
        {
            EnsureDir(SPRITE_DIR);
            EnsureDir(PREFAB_DIR);
            EnsureDir(RESOURCES_DIR);

            GenerateAllSprites();
            AssetDatabase.Refresh();

            GenerateSegmentPrefabs();
            GenerateGimmickPrefabs();
            GenerateMonsterPrefabs();
            GenerateWallPalette();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssignPrefabsToData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Stage5PrefabGenerator] 프로토 스프라이트·프리팹·벽 팔레트 생성 + SO 할당 완료.");
        }

        // ── 1. ProtoSprite PNG 생성 (형태 맞춤) ─────────────────

        private static void GenerateAllSprites()
        {
            // 세그먼트용 — 윤곽선 있는 단순 사각
            Save(SPRITE_DIR, "Seg_Top.png", MakeFramedRect(SPRITE_SIZE, SPRITE_SIZE, 8, Color.white));
            Save(SPRITE_DIR, "Seg_Mid.png", MakeFramedRect(SPRITE_SIZE, SPRITE_SIZE, 6, Color.white));
            Save(SPRITE_DIR, "Seg_Bot.png", MakeFramedRect(SPRITE_SIZE, SPRITE_SIZE, 10, Color.white));
            // 벽 / 범퍼 테두리 — 액트별 컨셉 + 공통 1
            Save(SPRITE_DIR, "Wall_Common.png", MakeWallPattern(SPRITE_SIZE, WallTheme.Common));
            Save(SPRITE_DIR, "Wall_Spring.png", MakeWallPattern(SPRITE_SIZE, WallTheme.Spring));
            Save(SPRITE_DIR, "Wall_Summer.png", MakeWallPattern(SPRITE_SIZE, WallTheme.Summer));
            Save(SPRITE_DIR, "Wall_Autumn.png", MakeWallPattern(SPRITE_SIZE, WallTheme.Autumn));
            Save(SPRITE_DIR, "Wall_Winter.png", MakeWallPattern(SPRITE_SIZE, WallTheme.Winter));

            // 기믹 카테고리별 형태
            Save(SPRITE_DIR, "Gim_Reward.png", MakeStar(SPRITE_SIZE));
            Save(SPRITE_DIR, "Gim_Buff.png", MakePlus(SPRITE_SIZE));
            Save(SPRITE_DIR, "Gim_Trial.png", MakeCross(SPRITE_SIZE));
            Save(SPRITE_DIR, "Gim_Debuff.png", MakeMinus(SPRITE_SIZE));
            Save(SPRITE_DIR, "Gim_Env.png", MakeGear(SPRITE_SIZE));

            // 몬스터 — 작은/중간 원형 + 눈 1개
            Save(SPRITE_DIR, "Mon_Small.png", MakeMonster(SPRITE_SIZE, true));
            Save(SPRITE_DIR, "Mon_Medium.png", MakeMonster(SPRITE_SIZE, false));
        }

        private static void Save(string dir, string name, Texture2D tex)
        {
            string path = Path.Combine(dir, name);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = SPRITE_SIZE;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private enum WallTheme { Common, Spring, Summer, Autumn, Winter }

        /// <summary>액트별 컨셉이 들어간 벽 텍스처 — 윤곽 + 테마별 채움 패턴.</summary>
        private static Texture2D MakeWallPattern(int size, WallTheme theme)
        {
            var tex = NewTransparent(size, size);
            int frame = size / 16;
            (Color outline, Color baseC, Color accentC) = theme switch
            {
                WallTheme.Spring => (new Color(0.15f, 0.25f, 0.10f), new Color(0.45f, 0.65f, 0.30f), new Color(0.75f, 0.90f, 0.45f)),
                WallTheme.Summer => (new Color(0.05f, 0.15f, 0.30f), new Color(0.20f, 0.45f, 0.70f), new Color(0.55f, 0.85f, 1.00f)),
                WallTheme.Autumn => (new Color(0.30f, 0.12f, 0.05f), new Color(0.75f, 0.40f, 0.15f), new Color(1.00f, 0.65f, 0.25f)),
                WallTheme.Winter => (new Color(0.30f, 0.40f, 0.55f), new Color(0.75f, 0.85f, 0.95f), new Color(1.00f, 1.00f, 1.00f)),
                _ => (new Color(0.08f, 0.06f, 0.05f), new Color(0.65f, 0.55f, 0.40f), new Color(0.85f, 0.75f, 0.55f))
            };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool isFrame = x < frame || x >= size - frame || y < frame || y >= size - frame;
                if (isFrame) { tex.SetPixel(x, y, outline); continue; }

                Color px = baseC;
                bool accent = false;
                switch (theme)
                {
                    case WallTheme.Spring:
                        // 잎사귀: 8x8 격자에 둥근 점 + 줄기
                        int gx = x % 32, gy = y % 32;
                        float ld = Mathf.Sqrt((gx - 16) * (gx - 16) + (gy - 16) * (gy - 16));
                        if (ld < 6 || (gx > 14 && gx < 18 && gy > 4 && gy < 28)) accent = true;
                        break;
                    case WallTheme.Summer:
                        // 파도: 사인파 라인
                        int waveY = 16 + (int)(Mathf.Sin(x * 0.2f) * 8);
                        if (Mathf.Abs((y % 32) - waveY) < 3) accent = true;
                        break;
                    case WallTheme.Autumn:
                        // 단풍잎 점들: 노이즈 + 큰 점
                        int ax = x % 24, ay = y % 24;
                        if ((ax - 12) * (ax - 12) + (ay - 12) * (ay - 12) < 18) accent = true;
                        break;
                    case WallTheme.Winter:
                        // 다이아몬드 결정: 마름모 격자
                        int dx = x % 28, dy = y % 28;
                        if (Mathf.Abs(dx - 14) + Mathf.Abs(dy - 14) < 6) accent = true;
                        else if (Mathf.Abs(dx - 14) + Mathf.Abs(dy - 14) == 6) px = outline;
                        break;
                    default:
                        // 공통: 사선 하이라이트
                        accent = ((x + y) / 8) % 2 == 0;
                        break;
                }
                tex.SetPixel(x, y, accent ? accentC : px);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeFramedRect(int w, int h, int frame, Color fill)
        {
            var tex = NewTransparent(w, h);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool isFrame = x < frame || x >= w - frame || y < frame || y >= h - frame;
                tex.SetPixel(x, y, isFrame ? new Color(0.15f, 0.15f, 0.15f, 1f) : new Color(fill.r, fill.g, fill.b, 0.85f));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeStar(int size)
        {
            var tex = NewTransparent(size, size);
            float cx = size / 2f, cy = size / 2f;
            float outerR = size * 0.45f, innerR = size * 0.2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                // 5-pointed star — 각 36도 분기로 외경/내경 변환
                float a = (ang + Mathf.PI) / (Mathf.PI * 2f) * 10f;
                int sec = Mathf.FloorToInt(a) % 10;
                float frac = a - Mathf.FloorToInt(a);
                float interpR = sec % 2 == 0 ? Mathf.Lerp(outerR, innerR, frac) : Mathf.Lerp(innerR, outerR, frac);
                if (r <= interpR) tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakePlus(int size)
        {
            var tex = NewTransparent(size, size);
            int armW = size / 5;
            int center = size / 2;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inH = Mathf.Abs(y - center) <= armW && x >= armW && x < size - armW;
                bool inV = Mathf.Abs(x - center) <= armW && y >= armW && y < size - armW;
                if (inH || inV) tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeCross(int size)
        {
            var tex = NewTransparent(size, size);
            int thick = size / 8;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool d1 = Mathf.Abs(x - y) <= thick;
                bool d2 = Mathf.Abs(x + y - size) <= thick;
                int margin = size / 8;
                if ((d1 || d2) && x >= margin && x < size - margin && y >= margin && y < size - margin)
                    tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMinus(int size)
        {
            var tex = NewTransparent(size, size);
            int armW = size / 5;
            int center = size / 2;
            int margin = size / 6;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inH = Mathf.Abs(y - center) <= armW && x >= margin && x < size - margin;
                if (inH) tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeGear(int size)
        {
            var tex = NewTransparent(size, size);
            float cx = size / 2f, cy = size / 2f;
            float outerR = size * 0.42f, innerR = size * 0.32f, holeR = size * 0.15f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                int teeth = 8;
                float aa = (ang + Mathf.PI) * teeth / (Mathf.PI * 2f);
                float frac = aa - Mathf.FloorToInt(aa);
                float teethR = (frac < 0.5f) ? outerR : innerR;
                if (r > holeR && r <= teethR) tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeMonster(int size, bool small)
        {
            var tex = NewTransparent(size, size);
            float cx = size / 2f, cy = size / 2f;
            float bodyR = small ? size * 0.34f : size * 0.45f;
            float eyeR = size * 0.08f;
            float eyeOff = bodyR * 0.4f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx, dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r <= bodyR) tex.SetPixel(x, y, Color.white);
                // 눈 (어두운 색)
                float ex1 = x - (cx - eyeOff), ey1 = y - (cy + eyeOff * 0.5f);
                float ex2 = x - (cx + eyeOff), ey2 = y - (cy + eyeOff * 0.5f);
                if (Mathf.Sqrt(ex1 * ex1 + ey1 * ey1) <= eyeR) tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, 1f));
                if (Mathf.Sqrt(ex2 * ex2 + ey2 * ey2) <= eyeR) tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, 1f));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D NewTransparent(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);
            tex.SetPixels(pixels);
            return tex;
        }

        // ── 2. 세그먼트 프리팹 생성 ─────────────────────────────

        private static void GenerateSegmentPrefabs()
        {
            CreateSegmentPrefab("SegmentPrefab_Top_Common", SegmentSlot.Top, ActId.None);
            CreateSegmentPrefab("SegmentPrefab_Bottom_Common", SegmentSlot.Bottom, ActId.None);
            for (int act = 0; act <= 4; act++)
            {
                var actId = (ActId)act;
                CreateSegmentPrefab($"SegmentPrefab_Mid_{actId}", SegmentSlot.Middle, actId);
            }
        }

        private const string RESOURCES_DIR = "Assets/Resources";
        private static PhysicsMaterial2D wallMaterialCache;
        private static PhysicsMaterial2D GetWallMaterial()
        {
            if (wallMaterialCache != null) return wallMaterialCache;
            // StageRuntimeBuilder가 Resources.Load<PhysicsMaterial2D>("WallBouncy")로 로드
            EnsureDir(RESOURCES_DIR);
            string matPath = $"{RESOURCES_DIR}/WallBouncy.physicsMaterial2D";
            wallMaterialCache = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (wallMaterialCache == null)
            {
                wallMaterialCache = new PhysicsMaterial2D("WallBouncy") { friction = 0.05f, bounciness = 0.7f };
                AssetDatabase.CreateAsset(wallMaterialCache, matPath);
            }
            return wallMaterialCache;
        }

        private static void GenerateWallPalette()
        {
            string path = $"{RESOURCES_DIR}/StageWallPalette.asset";
            var palette = AssetDatabase.LoadAssetAtPath<RPGPinball.Data.StageWallPalette>(path);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<RPGPinball.Data.StageWallPalette>();
                AssetDatabase.CreateAsset(palette, path);
            }
            palette.common = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/Wall_Common.png");
            palette.spring = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/Wall_Spring.png");
            palette.summer = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/Wall_Summer.png");
            palette.autumn = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/Wall_Autumn.png");
            palette.winter = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/Wall_Winter.png");
            EditorUtility.SetDirty(palette);
        }

        private static GameObject CreateSegmentPrefab(string name, SegmentSlot slot, ActId theme)
        {
            string path = $"{PREFAB_DIR}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name);
            var sr = root.AddComponent<SpriteRenderer>();
            string spriteName = slot == SegmentSlot.Top ? "Seg_Top" : slot == SegmentSlot.Bottom ? "Seg_Bot" : "Seg_Mid";
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/{spriteName}.png");
            sr.color = TintFor(theme);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(9f, 3.5f); // 기본; SegmentRuntime이 effectiveHeight로 덮어쓸 수 있음
            sr.sortingOrder = -10;

            // 좌우 벽은 stage 단위로 StageRuntimeBuilder가 생성 (끊김 없는 통짜 외벽).
            // 세그먼트 prefab에는 천장·바닥만 (Top·Bottom 한정).
            if (slot == SegmentSlot.Top) CreateWall(root, "WallCeiling", new Vector2(0, 2f), new Vector2(9f, 0.5f), theme);
            if (slot == SegmentSlot.Bottom) CreateWall(root, "WallFloor", new Vector2(0, -3f), new Vector2(9f, 0.5f), theme);

            // 슬롯 앵커는 더 이상 생성하지 않음 (2026-05-14 격자 정렬 도입 후 미사용 — SegmentRuntime.GetSlotAnchorWorld 가 격자로 계산).

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateWall(GameObject parent, string name, Vector2 pos, Vector2 size, ActId theme)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform, false);
            wall.transform.localPosition = pos;
            wall.tag = RPGPinball.Core.Constants.TagWall;

            var bc = wall.AddComponent<BoxCollider2D>();
            bc.size = size;
            bc.sharedMaterial = GetWallMaterial();

            var wsr = wall.AddComponent<SpriteRenderer>();
            wsr.sprite = LoadWallSprite(theme);
            wsr.color = Color.white; // 액트 sprite 자체에 컨셉 컬러가 들어있음
            wsr.drawMode = SpriteDrawMode.Sliced;
            wsr.size = size;
            wsr.sortingOrder = -2; // 본체 위, 기믹 아래
        }

        /// <summary>액트별 벽 sprite 로드. 외부에서 StageRuntimeBuilder 도 호출 가능하도록 public.</summary>
        public static Sprite LoadWallSprite(ActId theme)
        {
            string key = theme switch
            {
                ActId.Act1_Spring => "Wall_Spring",
                ActId.Act2_Summer => "Wall_Summer",
                ActId.Act3_Autumn => "Wall_Autumn",
                ActId.Act4_Winter => "Wall_Winter",
                _ => "Wall_Common"
            };
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/{key}.png");
        }

        [MenuItem("RPG Pinball/Bootstrap/Force Regenerate Segment Prefabs")]
        public static void ForceRegenerateSegmentPrefabs()
        {
            int deleted = 0;
            foreach (var guid in AssetDatabase.FindAssets("SegmentPrefab_", new[] { PREFAB_DIR }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(p);
                deleted++;
            }
            AssetDatabase.SaveAssets();
            GenerateSegmentPrefabs();
            AssignPrefabsToData();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage5PrefabGenerator] {deleted}건 세그먼트 prefab 재생성 완료 (SlotAnchor 제거됨).");
        }

        [MenuItem("RPG Pinball/Bootstrap/Force Regenerate Gimmick Prefabs")]
        public static void ForceRegenerateGimmickPrefabs()
        {
            int deleted = 0;
            foreach (var guid in AssetDatabase.FindAssets("GimmickPrefab_", new[] { PREFAB_DIR }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(p);
                deleted++;
            }
            AssetDatabase.SaveAssets();
            GenerateGimmickPrefabs();
            AssignPrefabsToData();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage5PrefabGenerator] {deleted}건 기믹 prefab 재생성 완료.");
        }

        // ── 3. 기믹 프리팹 생성 (카테고리별 1개) ─────────────────

        private static void GenerateGimmickPrefabs()
        {
            CreateGimmickPrefab("GimmickPrefab_Reward", "Gim_Reward", RewardColor);
            CreateGimmickPrefab("GimmickPrefab_Buff", "Gim_Buff", BuffColor);
            CreateGimmickPrefab("GimmickPrefab_Trial", "Gim_Trial", TrialColor);
            CreateGimmickPrefab("GimmickPrefab_Debuff", "Gim_Debuff", DebuffColor);
            CreateGimmickPrefab("GimmickPrefab_Environment", "Gim_Env", EnvColor);

            // 본 구현된 6종은 개별 프리팹 (전용 컴포넌트 부착)
            CreateSpecificGimmickPrefab("GimmickPrefab_HiddenBumper", "Gim_Reward", RewardColor, typeof(HiddenBumperGimmick));
            CreateSpecificGimmickPrefab("GimmickPrefab_JumpPad", "Gim_Reward", new Color(0.4f, 1f, 0.4f), typeof(JumpPadGimmick));
            CreateSpecificGimmickPrefab("GimmickPrefab_ManaShrine", "Gim_Buff", new Color(0.5f, 0.5f, 1f), typeof(ManaShrineGimmick));
            CreateSpecificGimmickPrefab("GimmickPrefab_TimeOrb", "Gim_Reward", new Color(1f, 1f, 0.5f), typeof(TimeOrbGimmick));
            CreateSpecificGimmickPrefab("GimmickPrefab_AccelRail", "Gim_Env", new Color(0.3f, 0.6f, 1f), typeof(AccelRailGimmick));
            CreateSpecificGimmickPrefab("GimmickPrefab_Wormhole", "Gim_Env", new Color(0.2f, 0.1f, 0.4f), typeof(WormholeGimmick));
        }

        private static GameObject CreateGimmickPrefab(string name, string spriteName, Color tint)
        {
            return CreateSpecificGimmickPrefab(name, spriteName, tint, typeof(PlaceholderGimmick));
        }

        private static GameObject CreateSpecificGimmickPrefab(string name, string spriteName, Color tint, System.Type componentType)
        {
            string path = $"{PREFAB_DIR}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name);
            // 2026-05-14 (스테이지 30% 확장 + 겹침 해소): button_yellow/button_blue 같은 Kenney 스프라이트가
            // native 1.28U 이라 scale 2.0 시 2.56U → 격자 lap-jitter(1.43U)와 충돌. scale 1.2로 축소해 visualW ≈ 1.54U.
            root.transform.localScale = Vector3.one * 1.2f;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/{spriteName}.png");
            sr.color = tint;
            sr.sortingOrder = 5;

            var col = root.AddComponent<CircleCollider2D>();
            // 핀볼 동작: 공과 실제 충돌(반발)이 일어나도록 isTrigger=false. 효과 발행은 GimmickBase.OnCollisionEnter2D 에서.
            col.isTrigger = false;
            col.radius = 0.5f; // localScale 1.2 → effective 0.6U (공 0.25보다 큼)

            root.AddComponent(componentType);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ── 4. 몬스터 프리팹 생성 ───────────────────────────────

        private static void GenerateMonsterPrefabs()
        {
            CreateMonsterPrefab("MonsterPrefab_Small", "Mon_Small", 0.35f, 0.85f);
            CreateMonsterPrefab("MonsterPrefab_Medium", "Mon_Medium", 0.55f, 1f);
        }

        private static GameObject CreateMonsterPrefab(string name, string spriteName, float radius, float scale)
        {
            string path = $"{PREFAB_DIR}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(name);
            root.transform.localScale = Vector3.one * scale;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITE_DIR}/{spriteName}.png");
            sr.color = new Color(0.9f, 0.4f, 0.4f);
            sr.sortingOrder = 3;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = radius;

            root.AddComponent<RPGPinball.Enemy.MonsterBase>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ── 5. SO에 prefab 할당 ─────────────────────────────────

        private static void AssignPrefabsToData()
        {
            // 세그먼트
            foreach (var seg in LoadAll<SegmentData>("Assets/Resources/Segments"))
            {
                string key;
                if (seg.slot == SegmentSlot.Top) key = "SegmentPrefab_Top_Common";
                else if (seg.slot == SegmentSlot.Bottom) key = "SegmentPrefab_Bottom_Common";
                else key = $"SegmentPrefab_Mid_{seg.theme}";
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/{key}.prefab");
                if (p != null) seg.prefab = p;
                EditorUtility.SetDirty(seg);
            }

            // 기믹 — 본 구현 6종은 전용, 나머지는 카테고리 프리팹
            foreach (var g in LoadAll<GimmickData>("Assets/Resources/Gimmicks"))
            {
                GameObject p = TryLoadSpecificGimmick(g.gimmickId);
                if (p == null) p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/GimmickPrefab_{g.category}.prefab");
                if (p != null) g.prefab = p;
                EditorUtility.SetDirty(g);
            }

            // 몬스터
            foreach (var m in LoadAll<MonsterData>("Assets/Resources/Monsters"))
            {
                string key = m.sizeCategory == MonsterSizeCategory.Small ? "MonsterPrefab_Small" : "MonsterPrefab_Medium";
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/{key}.prefab");
                if (p != null) m.prefab = p;
                EditorUtility.SetDirty(m);
            }
        }

        private static GameObject TryLoadSpecificGimmick(GimmickId id)
        {
            string name = id switch
            {
                GimmickId.HiddenBumper => "GimmickPrefab_HiddenBumper",
                GimmickId.JumpPad => "GimmickPrefab_JumpPad",
                GimmickId.ManaShrine => "GimmickPrefab_ManaShrine",
                GimmickId.TimeOrb => "GimmickPrefab_TimeOrb",
                GimmickId.AccelRail => "GimmickPrefab_AccelRail",
                GimmickId.Wormhole => "GimmickPrefab_Wormhole",
                _ => null
            };
            return name != null ? AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/{name}.prefab") : null;
        }

        private static T[] LoadAll<T>(string folder) where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            var result = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        private static Color TintFor(ActId act) => act switch
        {
            ActId.Act1_Spring => SpringTint,
            ActId.Act2_Summer => SummerTint,
            ActId.Act3_Autumn => AutumnTint,
            ActId.Act4_Winter => WinterTint,
            _ => CommonTint
        };

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
