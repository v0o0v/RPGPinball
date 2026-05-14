using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 열기구 3단계 개조 비용·효과. 단일 SO에 3단계 배열 보관.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Balloon Upgrade", fileName = "BalloonUpgrade")]
    public class BalloonUpgradeData : ScriptableObject
    {
        public BalloonStage[] stages = new BalloonStage[3];

        [Header("아이콘 (§0-A.8 cart.png)")]
        public Sprite iconSprite;
    }

    [System.Serializable]
    public struct BalloonStage
    {
        public BalloonUpgradeId upgradeId;
        public int goldCost;
        public int manaCrystalCost;

        // Lv.1 효과
        public int startingManaBonus;
        // Lv.2 효과
        public float bossStartingTimeBonus;
        // Lv.3 효과
        public float hiddenNodeChanceBonus;
    }
}
