using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 코어(가속/분열/크로노/가디언/집중/축복) 정의. Game_Design_Spec.md §3 대장간 코어 목록 기반.
    /// 마일스톤 3에서는 Acceleration/Split/Chrono만 효과 활성. 나머지는 슬롯만 정의.
    /// DamageCalculator 단계 [3]에서 분기 처리.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Core", fileName = "CoreData")]
    public class CoreData : ScriptableObject
    {
        [Header("식별")]
        public CoreId coreId = CoreId.Acceleration;
        public CoreGrade grade = CoreGrade.Normal;
        public string displayName;
        [TextArea(2, 4)] public string descriptionKo;

        [Header("강화")]
        [Range(1, 10)] public int level = 1;
        public int requiredFragments = 5; // 다음 레벨 강화에 필요한 조각

        [Header("배율 (마일스톤 3 활성: Acceleration/Split/Chrono)")]
        // 가속 코어: 데미지 *= 1 + (BallSpeed / BallMaxSpeed) * accelerationCoefficient
        public float accelerationCoefficient = 0.2f;
        // 분열 코어: 분열 공 데미지 페널티 완화. 0.8 → 0.8 + splitDamageRelief
        public float splitDamageRelief = 0.02f;
        // 크로노 코어: 몬스터 처치 시 시간 +chronoTimeBonus초
        public float chronoTimeBonus = 1.0f;

        [Header("범용 배율 (다른 코어용 슬롯, 마일스톤 6 이후)")]
        public float defenseMultiplier = 1.0f;
        public float critChanceBonus = 0f;
        public float manaEfficiencyMultiplier = 1.0f;

        [Header("등급 보너스")]
        public float gradeMultiplier = 1.0f; // 일반 1.0, 레어 1.2, 에픽 1.5, 전설 2.0

        public bool IsActive(CoreId queryId) => coreId == queryId;
    }
}
