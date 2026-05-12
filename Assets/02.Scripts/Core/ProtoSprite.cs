using UnityEngine;

namespace RPGPinball.Core
{
    /// <summary>
    /// 프로토타입용 단색 스프라이트 빌더. 벽 등 런타임 생성 오브젝트의 폴백 비주얼로 사용.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ProtoSprite : MonoBehaviour
    {
        public enum Shape { Circle, Square }

        [SerializeField] private Shape shape = Shape.Square;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private int textureSize = 64;

        private void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null)
                sr.sprite = Build(shape, color, textureSize);
        }

        public static Sprite Build(Shape shape, Color color, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float c = size * 0.5f;
            float r2 = c * c;

            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % size;
                int y = i / size;
                if (shape == Shape.Circle)
                {
                    float dx = x - c + 0.5f;
                    float dy = y - c + 0.5f;
                    pixels[i] = (dx * dx + dy * dy <= r2) ? color : Color.clear;
                }
                else
                {
                    pixels[i] = color;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
