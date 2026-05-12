using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 9.0×12.0 Unit 플레이필드 경계 벽과 범퍼를 런타임에 생성한다.
    /// </summary>
    public class PlayfieldBuilder : MonoBehaviour
    {
        [Header("프리팹")]
        [SerializeField] private GameObject bumperPrefab;

        [Header("벽 머티리얼 (마찰 0, 반발 0 권장)")]
        [SerializeField] private PhysicsMaterial2D wallMaterial;

        // 플레이필드 중심 x=0 기준 대칭 배치 (가용 범위: ±4.5)
        [Header("범퍼 배치")]
        [SerializeField] private Vector2[] bumperPositions = new[]
        {
            new Vector2(-2.25f, 4f),
            new Vector2( 0.00f, 6f),
            new Vector2( 2.25f, 4f),
        };

        private void Start()
        {
            BuildWalls();
            PlaceBumpers();
        }

        // ── 경계 벽 ───────────────────────────────────────────

        private void BuildWalls()
        {
            float hw = Constants.PlayfieldWidth / 2f;
            float hh = Constants.SegmentHeight / 2f;
            float t = Constants.WallThickness;

            // 좌벽, 우벽, 상단
            CreateWall("WallLeft",  new Vector2(-hw - t / 2f, 0f),        new Vector2(t, Constants.SegmentHeight + t * 2f));
            CreateWall("WallRight", new Vector2(hw + t / 2f, 0f),         new Vector2(t, Constants.SegmentHeight + t * 2f));
            CreateWall("WallTop",   new Vector2(0f, hh + t / 2f),         new Vector2(Constants.PlayfieldWidth + t * 2f, t));
        }

        private void CreateWall(string wallName, Vector2 position, Vector2 size)
        {
            var go = new GameObject(wallName);
            go.transform.SetParent(transform);
            go.transform.localPosition = position;
            go.tag = Constants.TagWall;
            go.layer = LayerMask.NameToLayer("Default");

            // scale로 크기를 통일하고 col.size는 1x1로 유지
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            if (wallMaterial != null) col.sharedMaterial = wallMaterial;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Core.ProtoSprite.Build(Core.ProtoSprite.Shape.Square, new Color(0.3f, 0.4f, 0.55f), 64);
        }

        // ── 범퍼 ──────────────────────────────────────────────

        private void PlaceBumpers()
        {
            if (bumperPrefab == null) return;
            foreach (var pos in bumperPositions)
            {
                var go = Instantiate(bumperPrefab, pos, Quaternion.identity, transform);
                go.name = "Bumper";
            }
        }
    }
}
