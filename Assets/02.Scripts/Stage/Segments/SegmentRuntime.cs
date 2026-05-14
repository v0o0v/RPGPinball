using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Segments
{
    /// <summary>
    /// 세그먼트 인스턴스의 런타임 컨테이너. 슬롯 앵커·기믹·연결 통로를 노출.
    /// OnDestroy 시 자식 정리.
    /// </summary>
    public class SegmentRuntime : MonoBehaviour
    {
        [SerializeField] private SegmentData data;
        [SerializeField] private SegmentSlot slot;
        [SerializeField] private float effectiveHeight;
        // 2026-05-14: middle[0]=Slingshot, middle[2]=BumperCluster 처럼 외측 X 영역이 정적 형상(슬링샷·사이드범퍼)에
        // 점유된 zone 에서는 격자 outer col(±6.45) 사용 불가. true 면 inner col 2개(±2.15) 만 사용해 회피.
        [SerializeField] private bool useInnerColsOnly;
        private readonly List<GameObject> activeGimmicks = new();

        public SegmentData Data => data;
        public SegmentSlot Slot => slot;
        public float EffectiveHeight => effectiveHeight;
        public IReadOnlyList<GameObject> ActiveGimmicks => activeGimmicks;
        public bool UseInnerColsOnly { get => useInnerColsOnly; set => useInnerColsOnly = value; }

        public void Initialize(SegmentData segData, float assignedHeight)
        {
            data = segData;
            slot = segData != null ? segData.slot : SegmentSlot.Middle;
            effectiveHeight = assignedHeight;
            ApplyEffectiveHeight();
        }

        /// <summary>
        /// 좌우 벽 / 본체 SpriteRenderer 의 크기를 effectiveHeight 에 맞춰 갱신.
        /// 빌더가 height 변형으로 세그먼트마다 다른 높이를 할당하므로 동적 적용 필수.
        /// </summary>
        private void ApplyEffectiveHeight()
        {
            float w = RPGPinball.Core.Constants.SegPlayfieldWidth;
            float h = Mathf.Max(0.5f, effectiveHeight);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(w, h);
            }

            // 좌·우 외벽은 StageRuntimeBuilder가 stage 단위 통짜로 생성하므로 여기서 다루지 않음.
            // 천장(Top 세그먼트) / 바닥(Bottom 세그먼트)만 effectiveHeight에 맞춰 정렬.
            ResizeWall("WallCeiling", new Vector2(0f, h * 0.5f - 0.25f), new Vector2(w, 0.5f));
            ResizeWall("WallFloor", new Vector2(0f, -h * 0.5f + 0.25f), new Vector2(w, 0.5f));
        }

        private void ResizeWall(string childName, Vector2 localPos, Vector2 size)
        {
            var child = transform.Find(childName);
            if (child == null) return;
            child.localPosition = new Vector3(localPos.x, localPos.y, 0f);
            var bc = child.GetComponent<BoxCollider2D>();
            if (bc != null) bc.size = size;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
        }

        public void RegisterGimmick(GameObject gimmick)
        {
            if (gimmick != null) activeGimmicks.Add(gimmick);
        }

        public void SetGimmicksActive(bool active)
        {
            for (int i = 0; i < activeGimmicks.Count; i++)
                if (activeGimmicks[i] != null) activeGimmicks[i].SetActive(active);
        }

        // 격자 정렬 (2026-05-14): anchor 무시. cols × rows 격자로 좌우 대칭 + 가로·세로 정렬 보장.
        // 셀 간격 2.5U 이상 → 공(반지름 0.25)이 통과할 통로 확보.
        // 2026-05-15: stage 가로 13U로 확장 → 격자 X = ±4.5, ±1.5 (margin 2.0U 유지)
        // 2026-05-14: 13U → 16.9U (가로 30% 추가 확장) → 격자 X = ±6.45, ±2.15 (margin 2.0U 유지)
        // 2026-05-14: 겹침 해소 — row 간격 2.6→3.2, lap jitter colStep/3→colStep/2 로 X 거리 1.43→2.15U 확보
        private const int GridCols = 4;                  // X 페어 4점
        private const float GridCellHeight = 3.2f;       // Y 간격 (큰 sprite visualH 1.5U + 여유)
        private const float GridSideMargin = 2.0f;       // 좌·우 외벽 이격 (벽 뚫음 방지)
        private const float GridTopBottomMargin = 1.5f;  // 세그먼트 위·아래 이격

        public Vector2 GetSlotAnchorWorld(int slotIndex)
        {
            // 가용 영역 (segment local 기준): X [-(w/2 - margin), +(w/2 - margin)], Y [-(h/2 - margin), +(h/2 - margin)]
            float w = RPGPinball.Core.Constants.SegPlayfieldWidth;
            float h = Mathf.Max(GridCellHeight, effectiveHeight);
            float xUsable = w - GridSideMargin * 2f;
            float yUsable = h - GridTopBottomMargin * 2f;

            int rawCols = GridCols;                       // 4 (전체 col 좌표 계산용)
            int activeCols = useInnerColsOnly ? 2 : rawCols; // 실제 사용 col 수
            int colShift = useInnerColsOnly ? 1 : 0;      // inner-only 모드: colInRow 0,1 → col 1,2
            int rows = Mathf.Max(1, Mathf.FloorToInt(yUsable / GridCellHeight) + 1);

            float colStep = xUsable / (rawCols - 1);
            float rowStep = rows > 1 ? yUsable / (rows - 1) : 0f;
            float xStart = -xUsable * 0.5f;
            float yStart = -yUsable * 0.5f;

            int totalCells = activeCols * rows;
            int wrapped = slotIndex % totalCells;
            int lap = slotIndex / totalCells;

            int row = wrapped / activeCols;
            int colInRow = wrapped % activeCols;

            int col = colInRow + colShift;               // useInnerColsOnly 면 col 1, 2 만
            float localX = xStart + col * colStep;
            float localY = yStart + row * rowStep;

            // lap 별 Y 오프셋 — alternating sign + 1/2^pair magnitude.
            //   lap 1 → +halfStep, lap 2 → -halfStep (row 사이 정중앙 위/아래)
            //   lap 3 → +halfStep/2, lap 4 → -halfStep/2
            //   lap 5 → +halfStep/4, ...
            // halfStep = rowStep/2 ≥ CellH/2 = 1.6U > visualR 합(~1.2U) → lap 0·1·2 사이 시각 안전.
            // GimmickSelector capacity cap 으로 lap 2 이하만 발생하도록 보장된다.
            if (lap > 0)
            {
                float halfStep = (rows > 1 ? rowStep : GridCellHeight) * 0.5f;
                int pairIdx = (lap - 1) / 2;                  // 0, 0, 1, 1, 2, 2, ...
                float magnitude = halfStep / (1 << pairIdx);   // halfStep, halfStep/2, halfStep/4, ...
                float sign = (lap % 2 == 1) ? +1f : -1f;
                localY += sign * magnitude;
            }

            return (Vector2)transform.position + new Vector2(localX, localY);
        }

        private void OnDestroy()
        {
            for (int i = activeGimmicks.Count - 1; i >= 0; i--)
                if (activeGimmicks[i] != null) Destroy(activeGimmicks[i]);
            activeGimmicks.Clear();
        }
    }
}
