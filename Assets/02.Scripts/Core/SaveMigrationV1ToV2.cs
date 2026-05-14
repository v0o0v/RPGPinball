using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Core
{
    /// <summary>
    /// M6 PlayerPrefsSaveAdapter 키 → M7 SaveData JSON 1회성 마이그레이션.
    /// PlayerPrefsSaveAdapter 키 prefix: "Economy.*", "Forge.*", "Enchanter.*", "Astrologer.*".
    /// </summary>
    public static class SaveMigrationV1ToV2
    {
        public static bool HasLegacyPrefs()
        {
            return PlayerPrefs.HasKey("Economy.Gold");
        }

        public static SaveData Migrate(SaveData defaultData = null)
        {
            var data = defaultData ?? SaveData.CreateDefault();

            // ── 통화 ────────────────────────────────────────────
            data.player.gold = PlayerPrefs.GetInt("Economy.Gold", 0);
            data.player.manaCrystal = PlayerPrefs.GetInt("Economy.ManaCrystal", 0);
            data.player.bossSoul = PlayerPrefs.GetInt("Economy.BossSoul", 0);
            data.player.respecScrollCount = PlayerPrefs.GetInt("Economy.RespecScroll", 0);

            // ── 대장간 ──────────────────────────────────────────
            data.inventory.equippedBallMaterial = PlayerPrefs.GetInt("Forge.Material", 0);
            data.inventory.equippedMainCore = PlayerPrefs.GetInt("Forge.MainCore", 0);
            data.inventory.equippedMainCoreLevel = PlayerPrefs.GetInt("Forge.MainCoreLv", 1);
            data.inventory.equippedFlipperVariant = PlayerPrefs.GetInt("Forge.FlipperVariant", 0);
            data.inventory.equippedFlipperLevel = PlayerPrefs.GetInt("Forge.FlipperLevel", 1);
            data.village.forge.materialId = data.inventory.equippedBallMaterial;
            data.village.forge.mainCoreId = data.inventory.equippedMainCore;
            data.village.forge.mainCoreLevel = data.inventory.equippedMainCoreLevel;
            data.village.forge.flipperVariantId = data.inventory.equippedFlipperVariant;
            data.village.forge.flipperLevel = data.inventory.equippedFlipperLevel;

            // ── 점성술사 ────────────────────────────────────────
            data.village.astrologer.totalPullCount = PlayerPrefs.GetInt("Astrologer.PullCount", 0);

            // ── 열기구 ─────────────────────────────────────────
            data.village.balloon.upgradeLevel = PlayerPrefs.GetInt("Balloon.UpgradeLevel", 0);

            return data;
        }

        public static void DeleteLegacyPrefs()
        {
            string[] keys = {
                "Economy.Gold", "Economy.ManaCrystal", "Economy.BossSoul", "Economy.RespecScroll",
                "Forge.Material", "Forge.MainCore", "Forge.MainCoreLv", "Forge.FlipperVariant", "Forge.FlipperLevel",
                "Astrologer.PullCount", "Balloon.UpgradeLevel"
            };
            foreach (var k in keys) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }
    }
}
