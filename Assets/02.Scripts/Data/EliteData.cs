using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 엘리트 몬스터 스탯 정의. MonsterData를 상속하여 고유 능력 슬롯 추가.
    /// Elite_Bounty_Spec.md의 4종 엘리트 표 1:1.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Enemy/Elite", fileName = "EliteData")]
    public class EliteData : MonsterData
    {
        [Header("엘리트 식별")]
        public EliteId eliteId;
        public int actNumber = 1;

        [Header("분노 모드")]
        [Range(0f, 1f)] public float enragedHpRatio = Core.Constants.EliteEnragedHpRatio;

        [Header("도주 (황금 고블린 왕)")]
        [Tooltip("0 이상이면 첫 타격 후 N초 내 처치 실패 시 도주")]
        public float fleeTimerSeconds;
        [Tooltip("0 이상이면 N초 내 첫 타격 실패 시 도주")]
        public float firstHitTimeoutSeconds;

        [Header("정면 방패 (서리 파수꾼)")]
        public bool hasFrontalShield;
        [Tooltip("정면 무적 호 각도 (180 = 절반)")]
        public float frontalShieldArcDeg = 180f;
        [Tooltip("후방 타격 시 DEF 비율 (정면 DEF 대비)")]
        public float backDefenseRatio = 0.15f;

        [Header("자힐 (심해 리바이어선)")]
        [Tooltip("0 초과면 타격 시 데미지의 N%를 HP로 회복")]
        public float lifeStealRatio;
        public int lifeStealCap = 200;

        [Header("패턴")]
        public BossPatternMetadata[] patterns;

        [Header("약점 부위")]
        public WeakPointSpec[] weakPoints;

        [Header("보상")]
        public RewardTable rewardTable;

        [Header("제한 시간 (초). 엘리트 기본 120")]
        public float stageTimeLimit = 120f;

        protected void OnEnable()
        {
            // 엘리트는 isBoss=true로 취급 (마나/SP 보상 등에서 보스와 동일 처리)
            isBoss = true;
        }
    }
}
