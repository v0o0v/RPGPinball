using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 플리퍼 파생형 3종(가시/연성/충격파) 정의.
    /// Game_Design_Spec.md §3 플리퍼 파생형 표 그대로.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Flipper Variant", fileName = "FlipperVariant")]
    public class FlipperVariantData : ScriptableObject
    {
        [Header("식별")]
        public FlipperVariantId variantId = FlipperVariantId.Basic;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;
        public Sprite iconSprite; // §0-A.5 kenney_medals flatshadow_medal7~9

        [Header("가시 플리퍼 (DEF -10% / 출혈 2%/s / 5초)")]
        public float spikeDefDebuffPercent = 0.10f;
        public float spikeBleedDotPercentPerSecond = 0.02f;
        public float spikeDebuffDuration = 5f;
        public float spikeLevelBonusPerLevel = 0.005f; // 레벨업 보너스

        [Header("연성 플리퍼 (폭 ×1.2 / 액티브 지속 ×0.5→1.2 토글)")]
        public float ductileFlipperWidthMultiplier = 1.2f;
        public float ductileActiveDurationShort = 0.5f;
        public float ductileActiveDurationLong = 1.2f;

        [Header("충격파 플리퍼 (반경 5U 마법 광역 0.5×)")]
        public float shockwaveRadiusUnits = 5f;
        public float shockwaveDamageMultiplier = 0.5f;
        public bool shockwaveIsMagic = true;
        public float shockwaveLevelBonusPerLevel = 0.05f; // 레벨업 보너스
    }
}
