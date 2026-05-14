using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 핀볼 보드 세그먼트 프리셋. 상단/중단/하단 슬롯 중 하나에 배치.
    /// 세그먼트의 누적 세로 합 = 카메라 시야 세로 × 3 강제 (Procedural_Stage_Gen.md §2 + Milestone5_TODO §3.2).
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Segment", fileName = "SegmentData")]
    public class SegmentData : ScriptableObject
    {
        [Header("식별")]
        public string segmentId;
        public SegmentSlot slot = SegmentSlot.Middle;

        [Header("테마")]
        [Tooltip("None(=공통)이면 어느 액트에서도 사용 가능. Act1~4 지정 시 해당 액트만.")]
        public ActId theme = ActId.None;

        [Header("프리팹")]
        public GameObject prefab;

        [Header("구조")]
        [Tooltip("플레이필드 가로 (9.0 Unit 고정)")]
        public float width = 9.0f;
        [Tooltip("기본 세로 길이. 변형 가능 범위는 heightMin/Max.")]
        public float height = 3.5f;
        [Tooltip("빌더가 세로 합 맞춤 시 변형 허용 최소값.")]
        public float heightMin = 3.0f;
        [Tooltip("빌더가 세로 합 맞춤 시 변형 허용 최대값.")]
        public float heightMax = 5.0f;

        [Header("기믹 슬롯")]
        [Tooltip("중단 세그먼트 기준 2~4개. 빌더가 기믹을 이 앵커에 배치.")]
        public Vector2[] gimmickSlotAnchors;

        [Header("연결 통로")]
        [Tooltip("인접 세그먼트와 정렬되어야 할 출입구 좌표(로컬). ±0.5U 오차 이내 정합.")]
        public Vector2[] connectionPorts;

        [Header("분류 태그")]
        [Tooltip("\"DeadEnd\"/\"Open\"/\"RailHeavy\" 등 필터링용.")]
        public string[] placementTags;

        [TextArea(2, 4)]
        public string descriptionKo;
    }
}
