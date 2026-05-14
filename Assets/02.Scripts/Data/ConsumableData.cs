using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 소모품 3종 (긴급 방패 / 시간의 모래 / 광란의 부적) 정의.
    /// Game_Design_Spec.md §3 열기구 선착장 + 용병단 창고.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Consumable", fileName = "Consumable")]
    public class ConsumableData : ScriptableObject
    {
        [Header("식별")]
        public ConsumableId consumableId = ConsumableId.None;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;
        public Sprite iconSprite;

        [Header("효과 — 카드별 유효 필드만 사용")]
        public float shieldDurationSeconds;   // 긴급 방패 5초
        public float timeBonusSeconds;        // 시간의 모래 15초
        public float damageMultiplier;        // 광란의 부적 2.0
        public float ballSpeedMultiplier;     // 광란의 부적 1.5
        public float durationSeconds;         // 광란의 부적 10초

        [Header("제작 비용")]
        public int goldCost;
        public CurrencyId specialOreId = CurrencyId.None; // Act별 매핑
        public int specialOreCount;
    }
}
