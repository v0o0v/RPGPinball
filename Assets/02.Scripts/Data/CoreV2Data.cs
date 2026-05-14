using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 마일스톤 6 코어 데이터 (Lv.1~5 배열). 기존 CoreData.cs (M3) 는 단일 인스턴스용으로 유지되고,
    /// 본 SO 는 대장간(ForgeManager)에서 사용하는 6종 코어의 레벨별 효과 풀.
    /// Game_Design_Spec.md §3 대장간 코어 표 1:1.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Core (V2 Lv.1~5)", fileName = "CoreV2_")]
    public class CoreV2Data : ScriptableObject
    {
        [Header("식별")]
        public CoreId coreId = CoreId.Acceleration;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;
        public Sprite iconSprite; // §0-A.5 kenney_medals

        /// <summary>Lv.1~5 효과 슬롯. 각 코어가 사용하는 필드는 coreId별로 다름.</summary>
        public CoreLevelEffect[] levelEffects = new CoreLevelEffect[5];

        /// <summary>Lv.1→2 ~ Lv.4→5 강화 비용. 길이 4.</summary>
        public CoreLevelUpCost[] levelUpCosts = new CoreLevelUpCost[4];
    }

    [System.Serializable]
    public struct CoreLevelEffect
    {
        // 가속 코어 (Acceleration)
        public float bounceSpeedBoost;       // 5/8/10/12/15%
        public int maxStackBonus;            // 누적 한계
        public float damagePerSpeed10Percent; // 속도 10% 당 데미지 +X%

        // 자력 코어 (Magnetic — Predator로 매핑된 경우)
        public float magneticRadius;         // 1.5/2.0/2.5/3.0/3.5U

        // 분열 코어 (Split)
        public int comboThreshold;           // 5/4/3 콤보당 1회
        public float duplicateDurationSeconds; // 1.5~3초
        public float duplicateDamageRatio;   // 0.3/0.4/0.5

        // 크로노 코어 (Chrono)
        public float timePerHit;             // 0.5/0.8/1.0/1.2/1.5 초

        // 수호 코어 (Guardian)
        public float penaltyReductionPercent;  // 5/10/15/20/25%
        public float shieldCooldownReductionPercent;

        // 포식 코어 (Predator)
        public float procChance;             // 5/8/10/12/15%
        public float timeRecoverPercent;     // 5/10/15%
    }

    [System.Serializable]
    public struct CoreLevelUpCost
    {
        public int coreFragments;  // 3/5/8/12
        public int gold;           // 500/1,000/2,000/3,500
    }
}
