#if UNITY_EDITOR
using System.IO;
using RPGPinball.Core;
using UnityEditor;
using UnityEngine;

namespace RPGPinball.Editor
{
    /// <summary>
    /// 프로토타입용 스프라이트를 코드로 생성해 Assets/03.Sprites/Proto/ 에 저장하고
    /// Ball·Flipper·Bumper 프리팹에 자동으로 할당한다.
    /// </summary>
    [InitializeOnLoad]
    public static class SpriteGenerator
    {
        private const string Dir = "Assets/03.Sprites/Proto";

        static SpriteGenerator()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "03.Sprites/Proto/Ball.png")))
                EditorApplication.delayCall += GenerateAll;
        }

        [MenuItem("RPG Pinball/Generate Proto Sprites")]
        public static void GenerateAll()
        {
            string absDir = Path.Combine(Application.dataPath, "03.Sprites/Proto");
            Directory.CreateDirectory(absDir);

            Save(Path.Combine(absDir, "Ball.png"),    MakeBall(128));
            Save(Path.Combine(absDir, "Flipper.png"), MakeFlipper(250, 40));
            Save(Path.Combine(absDir, "Bumper.png"),  MakeBumper(128));

            AssetDatabase.Refresh();
            // Refresh 후 임포터가 준비됐으면 바로 할당 시도, 안 되면 delayCall로 재시도
            if (AssetImporter.GetAtPath($"{Dir}/Ball.png") != null)
                AssignSprites();
            else
                EditorApplication.delayCall += AssignSprites;
        }

        // ── 프리팹 할당 ────────────────────────────────────────────────

        static void AssignSprites()
        {
            Configure($"{Dir}/Ball.png",    128);
            Configure($"{Dir}/Flipper.png", 100);
            Configure($"{Dir}/Bumper.png",  128);

            ApplyToPrefab("Assets/05.Prefabs/Ball.prefab",    $"{Dir}/Ball.png");
            ApplyToPrefab("Assets/05.Prefabs/Flipper.prefab", $"{Dir}/Flipper.png",
                newScale: new Vector3(1f, 1f, 1f), collider2DSize: new Vector2(2.5f, 0.4f));
            ApplyToPrefab("Assets/05.Prefabs/Bumper.prefab",  $"{Dir}/Bumper.png");

            Debug.Log("[SpriteGenerator] 스프라이트 할당 완료.");
        }

        static void Configure(string assetPath, int ppu)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = ppu;
            imp.alphaIsTransparency = true;
            imp.filterMode          = FilterMode.Bilinear;
            imp.SaveAndReimport();
        }

        static void ApplyToPrefab(string prefabPath, string spritePath,
            Vector3? newScale = null, Vector2? collider2DSize = null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) { Debug.LogWarning($"[SpriteGenerator] 스프라이트 없음: {spritePath}"); return; }

            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var root = scope.prefabContentsRoot;

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;

            var proto = root.GetComponent<ProtoSprite>();
            if (proto != null) Object.DestroyImmediate(proto);

            if (newScale.HasValue)
                root.transform.localScale = newScale.Value;

            if (collider2DSize.HasValue)
            {
                var col = root.GetComponent<BoxCollider2D>();
                if (col != null) col.size = collider2DSize.Value;
            }
        }

        static void Save(string path, Texture2D tex)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        // ── 스프라이트 생성 ────────────────────────────────────────────

        /// <summary>퐁 조명 모델로 구체감 있는 핀볼 공 스프라이트 생성</summary>
        static Texture2D MakeBall(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r  = size * 0.46f;
            float cx = size * 0.5f, cy = size * 0.5f;

            // 왼쪽 위에서 오는 광원
            Vector3 L = Vector3.Normalize(new Vector3(-0.45f, 0.65f, 0.62f));
            Color baseCol = new Color(0.55f, 0.70f, 0.92f);
            Color darkCol = new Color(0.12f, 0.20f, 0.38f);
            Color rimCol  = new Color(0.06f, 0.10f, 0.20f);

            for (int i = 0; i < pixels.Length; i++)
            {
                float px = i % size, py = i / size;
                float dx = px - cx, dy = py - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r + 0.5f) { pixels[i] = Color.clear; continue; }

                float alpha = Mathf.Clamp01(r + 0.5f - dist);
                float nx = dx / r, ny = dy / r;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                Vector3 N = new Vector3(nx, ny, nz);

                float diffuse = Mathf.Clamp01(Vector3.Dot(N, L));
                // 스페큘러: 반사벡터의 z 성분(뷰 방향)
                Vector3 Rv = 2f * Vector3.Dot(N, L) * N - L;
                float spec = Mathf.Pow(Mathf.Clamp01(Rv.z), 28) * 0.90f;
                float rim  = Mathf.SmoothStep(0.65f, 1.0f, dist / r);

                Color col = Color.Lerp(darkCol, baseCol, diffuse * 0.85f + 0.15f);
                col = Color.Lerp(col, Color.white, spec);
                col = Color.Lerp(col, rimCol, rim * 0.70f);
                col.a = alpha;
                pixels[i] = col;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>왼쪽이 넓고 오른쪽이 좁아지는 테이퍼 플리퍼 스프라이트 생성</summary>
        static Texture2D MakeFlipper(int w, int h)
        {
            var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            float cy     = h * 0.5f;
            float bigR   = h * 0.50f;   // 왼쪽 캡 반지름
            float smallR = h * 0.22f;   // 오른쪽 캡 반지름
            float bodyStart = bigR, bodyEnd = w - smallR;

            Color topCol  = new Color(0.45f, 0.65f, 0.95f);
            Color botCol  = new Color(0.15f, 0.28f, 0.55f);
            Color edgeCol = new Color(0.08f, 0.15f, 0.32f);

            for (int i = 0; i < pixels.Length; i++)
            {
                float px = i % w, py = (float)(i / w);
                float alpha = 0f;

                if (px < bodyStart)
                {
                    float dx = px - bodyStart, dy = py - cy;
                    alpha = Mathf.Clamp01(bigR + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                }
                else if (px > bodyEnd)
                {
                    float dx = px - bodyEnd, dy = py - cy;
                    alpha = Mathf.Clamp01(smallR + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                }
                else
                {
                    float t    = (px - bodyStart) / (bodyEnd - bodyStart);
                    float halfH = Mathf.Lerp(bigR, smallR, Mathf.Pow(t, 0.6f));
                    alpha = Mathf.Clamp01(halfH + 0.5f - Mathf.Abs(py - cy));
                }

                if (alpha <= 0f) { pixels[i] = Color.clear; continue; }

                float yFactor = py / h;
                Color col = Color.Lerp(botCol, topCol, yFactor);
                // 엣지 다크닝
                col = Color.Lerp(edgeCol, col, Mathf.SmoothStep(0f, 0.35f, alpha));
                col.a = alpha;
                pixels[i] = col;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>외부 링 + 내부 원으로 핀볼 범퍼 스프라이트 생성</summary>
        static Texture2D MakeBumper(int size)
        {
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float outerR = size * 0.46f;
            float innerR = size * 0.28f;

            Color ringOuter = new Color(0.90f, 0.55f, 0.05f);
            Color ringInner = new Color(1.00f, 0.88f, 0.45f);
            Color fillCol   = new Color(1.00f, 0.96f, 0.70f);
            Color centerCol = new Color(0.95f, 0.80f, 0.25f);
            Color rimDark   = new Color(0.40f, 0.22f, 0.02f);

            for (int i = 0; i < pixels.Length; i++)
            {
                float px = i % size, py = i / size;
                float dx = px - cx, dy = py - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > outerR + 0.5f) { pixels[i] = Color.clear; continue; }

                float alpha = Mathf.Clamp01(outerR + 0.5f - dist);
                Color col;

                if (dist <= innerR)
                {
                    col = Color.Lerp(centerCol, fillCol,
                                     Mathf.SmoothStep(0f, 1f, dist / innerR));
                }
                else
                {
                    float t = (dist - innerR) / (outerR - innerR); // 0=내부, 1=외부
                    col = Color.Lerp(ringInner, ringOuter, t);
                    col = Color.Lerp(col, rimDark, Mathf.SmoothStep(0.75f, 1f, t) * 0.55f);

                    // 왼쪽 위 하이라이트
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float diff  = Mathf.Abs(Mathf.DeltaAngle(angle, 120f));
                    float hl    = Mathf.Clamp01(1f - diff / 50f) * 0.45f * (1f - t);
                    col = Color.Lerp(col, Color.white, hl);
                }

                col.a = alpha;
                pixels[i] = col;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
#endif
