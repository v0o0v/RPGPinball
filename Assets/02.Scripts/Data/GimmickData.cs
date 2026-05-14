using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 기믹 80종 공통 SO 스키마. 모든 수치는 Gimmick_List.md 표 기반.
    /// 실제 동작 로직은 Stage/Gimmicks/* 의 GimmickBase 상속 컴포넌트가 담당.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Gimmick", fileName = "GimmickData")]
    public class GimmickData : ScriptableObject
    {
        [Header("식별")]
        public GimmickId gimmickId;
        public string displayNameKo;
        public GimmickCategory category;
        public GimmickPlacement placement = GimmickPlacement.MidSegment;

        [Header("테마")]
        [Tooltip("null(=None) 이면 공통 풀. Act1~4 지정 시 해당 액트 전용.")]
        public ActId themeOwner = ActId.None;

        [Header("판정 형태")]
        public Vector2 size = new Vector2(0.5f, 0.5f); // 원형이면 size.x 를 radius로 사용
        public bool isCircularShape = true;

        [Header("트리거")]
        public GimmickTriggerKind triggerKind = GimmickTriggerKind.BallContact;
        public float triggerIntervalSeconds;
        public bool triggerOnceOnly;

        [Header("효과 슬롯")]
        public float damagePercent;        // 공격력 N% 단위 (몬스터/기믹 보너스)
        public int absoluteHpDamage;
        public float impulseN;             // 임펄스 (히든 범퍼=20N, 점프패드=30N 등)
        public float timePenaltySeconds;   // 음수 가능 (시간 페널티)
        public int manaDelta;
        public int goldDelta;
        public int xpDelta;
        public float buffDurationSeconds;
        public float debuffDurationSeconds;
        public float cooldownSeconds;

        [Header("메타")]
        public bool isBlockable;
        [Tooltip("도감 저항력(Data_Schema.md §7) 적용 여부. M5는 헬퍼만 정의, 실제 누적은 M6 점성술사에서 본격화.")]
        public bool respectsResistance;
        [Tooltip("난이도 예산 비용. 음수면 보상 환급.")]
        public int budgetCost;
        public bool bossOnly;

        [Header("충돌 방지 / 시너지")]
        [Tooltip("동일 스테이지 동시 배치 금지 (양방향 자동 적용).")]
        public GimmickId[] conflictingIds;
        [Tooltip("시너지 가중치 ×1.5 적용 대상.")]
        public GimmickId[] synergyIds;

        [Header("프리팹")]
        public GameObject prefab;

        [TextArea(2, 4)]
        public string descriptionKo;
    }
}
