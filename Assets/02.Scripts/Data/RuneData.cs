using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 룬 9종 정의(3계열 × 3등급). 등급은 SO 인스턴스에 저장하지 않고
    /// 인벤토리의 RuneInstance.runeGrade 필드로 표현. SO 는 효과 슬롯 + 3개 아이콘만 보유.
    /// Game_Design_Spec.md §3 마법 부여소 룬 표.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Rune", fileName = "Rune")]
    public class RuneData : ScriptableObject
    {
        [Header("식별")]
        public RuneId runeId = RuneId.Spread;
        public RuneFamily family = RuneFamily.Shape;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;

        [Header("등급별 아이콘 (§0-A.1 Kenney 매핑)")]
        public Sprite iconNormal;
        public Sprite iconRare;
        public Sprite iconLegendary;

        [Header("효과 파라미터 (등급별 ×1.0/×1.5/×2.25 multiplier 적용)")]
        // 확산 (Spread)
        public int splitCount = 3;
        public float damagePerSplit = 0.5f;
        // 관통 (Pierce)
        public float pierceDamagePenaltyPerHit = 0.3f;
        // 추적 (Homing)
        public float homingTurnRate = 90f;
        // 화염 전환 (FireConvert)
        public float fireBurnDotPercent = 0.05f;
        // 빙결 전환 (IceConvert)
        public float iceSlowPercent = 0.3f;
        // 번개 전환 (LightningConvert)
        [Range(0f, 1f)] public float lightningStunChance = 0.05f;
        public float lightningStunDuration = 0.5f;
        // 처형자 (Executioner)
        public float executionerHpThreshold = 0.3f;
        public float executionerDamageMultiplier = 2.0f;
        // 연쇄 (Chain)
        public int chainComboThreshold = 20;
        public float chainManaCostMultiplier = 0.5f;
        // 역경 (Adversity)
        public float adversityTimeThreshold = 60f;
        public float adversityDamageMultiplier = 1.5f;

        [Header("합성 비용")]
        public int fuseRequiredCount = 3;
        public int fuseGoldNormalToRare = 200;
        public int fuseGoldRareToLegendary = 500;

        public Sprite GetIcon(RuneGrade grade)
        {
            switch (grade)
            {
                case RuneGrade.Normal: return iconNormal;
                case RuneGrade.Rare: return iconRare;
                case RuneGrade.Legendary: return iconLegendary;
                default: return iconNormal;
            }
        }

        public static float GradeMultiplier(RuneGrade grade)
        {
            switch (grade)
            {
                case RuneGrade.Normal: return RPGPinball.Core.Constants.RuneGradeNormalMul;
                case RuneGrade.Rare: return RPGPinball.Core.Constants.RuneGradeRareMul;
                case RuneGrade.Legendary: return RPGPinball.Core.Constants.RuneGradeLegendaryMul;
                default: return 1f;
            }
        }
    }
}
