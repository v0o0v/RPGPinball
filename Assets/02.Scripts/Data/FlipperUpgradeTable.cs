using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 플리퍼 강화 Lv.1~10 + 파생형 3종(가시/연성/충격파) 비용/효과 단일 참조 SO.
    /// Game_Design_Spec.md §3 플리퍼 강화 표 1:1.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Flipper Upgrade Table", fileName = "FlipperUpgradeTable")]
    public class FlipperUpgradeTable : ScriptableObject
    {
        [Header("Lv.1~10 (인덱스 0=Lv.1, 인덱스 9=Lv.10)")]
        public FlipperUpgradeLevel[] levels = new FlipperUpgradeLevel[10];

        [Header("파생형 (Lv.4에서 1회 선택, 변경 시 3,000골드)")]
        public int variantChangeGoldCost = 3000;
        public FlipperVariantData[] variants; // Spike/Ductile/Shockwave 3개

        public FlipperVariantData GetVariant(FlipperVariantId id)
        {
            if (variants == null) return null;
            foreach (var v in variants) if (v != null && v.variantId == id) return v;
            return null;
        }
    }

    [System.Serializable]
    public struct FlipperUpgradeLevel
    {
        [Range(0f, 1f)] public float cooldownReductionPercent;  // 0/3/6/9/12/15/16/17/18/20 %
        [Range(0f, 1f)] public float reboundBonusPercent;        // 0/5/10/15/20/25/30/35/40/50 %
        public int manaCrystalCost;                              // 0/50/80/120/180/250/320/380/450/500
        public int bossSoulCost;                                 // 0/3/5/8/10/12/15/16/18/20
        public bool unlocksVariantChoice;                        // Lv.4 만 true
    }
}
