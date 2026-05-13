using UnityEngine;
using DG.Tweening;

namespace RPGPinball.Enemy.BossAI
{
    /// <summary>
    /// 보스 패턴 텔레그래프 임시 시각화 헬퍼.
    /// SpriteRenderer + DOTween 페이드 인/아웃. 본격 VFX는 마일스톤 8 인계.
    /// </summary>
    public static class TelegraphRenderer
    {
        private static Sprite circleSprite;

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            // 32×32 원형 텍스처 동적 생성
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = dist <= r ? 1f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }

        /// <summary>특정 위치에 반지름 radius 원형 표시 (alpha 0.4 → 0). duration 후 자가 파괴.</summary>
        public static GameObject ShowCircle(Vector3 worldPos, float radius, float duration, Color? color = null)
        {
            var go = new GameObject("Telegraph_Circle");
            go.transform.position = worldPos;
            float diameter = Mathf.Max(0.1f, radius * 2f);
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetCircleSprite();
            sr.color = (color ?? new Color(1f, 0.2f, 0.2f, 0.4f));
            sr.sortingOrder = 10;

            // 페이드 시퀀스 (Color → SpriteRenderer.color 직접 트윈)
            var startColor = sr.color;
            var endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            float fadeStart = duration * 0.7f;
            float fadeDur = Mathf.Max(0.05f, duration * 0.3f);
            DOVirtual.DelayedCall(fadeStart, () =>
            {
                if (sr == null) return;
                DOTween.To(() => sr.color, c => sr.color = c, endColor, fadeDur)
                    .OnComplete(() => { if (go != null) Object.Destroy(go); });
            });

            return go;
        }

        /// <summary>방향 화살표 (LineRenderer 기반).</summary>
        public static GameObject ShowArrow(Vector3 origin, Vector3 direction, float length, float duration, Color? color = null)
        {
            var go = new GameObject("Telegraph_Arrow");
            go.transform.position = origin;
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, direction.normalized * Mathf.Max(0.1f, length));
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = (color ?? new Color(1f, 0.6f, 0.2f, 0.6f));
            lr.endColor = (color ?? new Color(1f, 0.6f, 0.2f, 0.6f));
            lr.sortingOrder = 10;

            // 페이드
            DOVirtual.DelayedCall(duration, () => { if (go != null) Object.Destroy(go); });
            return go;
        }
    }
}
