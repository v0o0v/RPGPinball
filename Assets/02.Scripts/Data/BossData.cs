using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 보스 스탯 정의. MonsterData를 상속하여 페이즈/약점/패턴 메타데이터 추가.
    /// Boss_Patterns.md의 12종 보스 표를 1:1로 SO 인스턴스에 반영.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Enemy/Boss", fileName = "BossData")]
    public class BossData : MonsterData
    {
        [Header("보스 식별")]
        public BossId bossId;
        public int actNumber = 1;       // 1~4
        public int stageNumber = 10;    // 10/20/30

        [Header("페이즈 (선택)")]
        public bool hasPhase2;
        public bool hasPhase3;
        [Range(0f, 1f)] public float phase2HpRatio = Core.Constants.BossPhase2HpRatioDefault;
        [Range(0f, 1f)] public float phase3HpRatio = Core.Constants.BossPhase3HpRatioDefault;

        [Header("분노 모드")]
        [Range(0f, 1f)] public float enragedHpRatio = Core.Constants.BossEnragedHpRatio;

        [Header("페이즈별 DEF 비율 (배열 길이 = 활성 페이즈 수)")]
        [Tooltip("[0]=Phase1 DEF (0~1), [1]=Phase2 DEF, [2]=Phase3 DEF")]
        public float[] phaseDefenseRatios = new float[] { 0.2f };

        [Header("패턴")]
        public BossPatternMetadata[] patterns;

        [Header("약점 부위")]
        public WeakPointSpec[] weakPoints;

        [Header("보상")]
        public RewardTable rewardTable;

        [Header("제한 시간 (초). 기본 180")]
        public float stageTimeLimit = 180f;

        public BossData()
        {
            // MonsterData.isBoss를 기본 true로 설정
            // (Awake 호출 전 직렬화 단계에서 적용되도록 OnEnable에서도 강제)
        }

        protected void OnEnable()
        {
            isBoss = true;
        }
    }
}
