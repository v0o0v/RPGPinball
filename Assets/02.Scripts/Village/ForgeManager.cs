using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// 대장간 시설. 공 재질 제작·교체, 6코어 강화·장착, 플리퍼 강화(Lv.1~10)·파생형 선택.
    /// Game_Design_Spec.md §3 대장간 1:1.
    /// </summary>
    public class ForgeManager : MonoBehaviour
    {
        public static ForgeManager Instance { get; private set; }

        [Header("재질")]
        [SerializeField] private BallMaterialData[] materials;
        [SerializeField] private BallMaterialId currentMaterial = BallMaterialId.Wood;
        private readonly HashSet<BallMaterialId> unlockedMaterials = new();

        [Header("코어")]
        [SerializeField] private CoreV2Data[] coreCatalog;
        [SerializeField] private CoreId mainCore = CoreId.None;
        [SerializeField] private CoreId[] subCores = new CoreId[Constants.CoreSlotsSub];
        private readonly Dictionary<CoreId, int> coreLevels = new();

        [Header("플리퍼")]
        [SerializeField] private FlipperUpgradeTable flipperUpgradeTable;
        [SerializeField] private int flipperUpgradeLevel = 1;
        [SerializeField] private FlipperVariantId selectedVariant = FlipperVariantId.Basic;
        private bool variantSelectedOnce;

        [Header("상태")]
        [SerializeField] private bool isInStage; // 스테이지 중에는 재질 교체 차단

        public BallMaterialId CurrentMaterial => currentMaterial;
        public CoreId MainCore => mainCore;
        public IReadOnlyList<CoreId> SubCores => subCores;
        public int FlipperUpgradeLevel => flipperUpgradeLevel;
        public FlipperVariantId CurrentVariant => selectedVariant;
        public bool IsVariantUnlocked => flipperUpgradeLevel >= Constants.FlipperVariantUnlockLevel;
        public bool VariantSelectedOnce => variantSelectedOnce;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
            unlockedMaterials.Add(BallMaterialId.Wood); // 시작 시 나무는 항상 해금
        }

        private void OnDisable() { if (Instance == this) Instance = null; }

        public void SetInStage(bool on) { isInStage = on; }

        public int GetCoreLevel(CoreId id) => coreLevels.TryGetValue(id, out var lv) ? lv : 1;

        // ── 재질 ──────────────────────────────────────────────

        public BallMaterialData GetMaterial(BallMaterialId id)
        {
            if (materials == null) return null;
            foreach (var m in materials) if (m != null && m.materialId == id) return m;
            return null;
        }

        public bool IsMaterialUnlocked(BallMaterialId id) => unlockedMaterials.Contains(id);

        public bool CraftMaterial(BallMaterialId id)
        {
            var m = GetMaterial(id);
            if (m == null) return false;
            if (unlockedMaterials.Contains(id)) return false;
            if (EconomyManager.Instance == null) return false;
            if (m.goldCost > 0 && !EconomyManager.Instance.Has(CurrencyId.Gold, m.goldCost)) return false;
            if (m.specialOreCount > 0 && m.specialOreId != CurrencyId.None
                && !EconomyManager.Instance.Has(m.specialOreId, m.specialOreCount)) return false;

            if (m.goldCost > 0) EconomyManager.Instance.Spend(CurrencyId.Gold, m.goldCost, "MaterialCraft");
            if (m.specialOreCount > 0 && m.specialOreId != CurrencyId.None)
                EconomyManager.Instance.Spend(m.specialOreId, m.specialOreCount, "MaterialCraft");

            unlockedMaterials.Add(id);
            return true;
        }

        public bool EquipMaterial(BallMaterialId id)
        {
            if (isInStage) return false;
            if (!unlockedMaterials.Contains(id)) return false;
            var m = GetMaterial(id);
            if (m == null) return false;

            // 교체 비용 (기본 100골드)
            int swapCost = m.swapGoldCost > 0 ? m.swapGoldCost : Constants.MaterialSwapGoldCost;
            if (swapCost > 0 && currentMaterial != id)
            {
                if (EconomyManager.Instance == null || !EconomyManager.Instance.Has(CurrencyId.Gold, swapCost)) return false;
                EconomyManager.Instance.Spend(CurrencyId.Gold, swapCost, "MaterialSwap");
            }
            currentMaterial = id;
            EventBus.Publish(new OnForgeBallChanged { MaterialId = id });
            return true;
        }

        // ── 코어 ──────────────────────────────────────────────

        public CoreV2Data GetCoreData(CoreId id)
        {
            if (coreCatalog == null) return null;
            foreach (var c in coreCatalog) if (c != null && c.coreId == id) return c;
            return null;
        }

        public bool LevelUpCore(CoreId id)
        {
            var c = GetCoreData(id);
            if (c == null) return false;
            int curLv = GetCoreLevel(id);
            if (curLv >= Constants.CoreMaxLevel) return false;
            if (c.levelUpCosts == null || curLv - 1 >= c.levelUpCosts.Length) return false;
            var cost = c.levelUpCosts[curLv - 1];
            if (EconomyManager.Instance == null) return false;
            if (cost.gold > 0 && !EconomyManager.Instance.Has(CurrencyId.Gold, cost.gold)) return false;
            if (cost.coreFragments > 0 && !EconomyManager.Instance.Has(CurrencyId.CoreFragment, cost.coreFragments)) return false;

            if (cost.gold > 0) EconomyManager.Instance.Spend(CurrencyId.Gold, cost.gold, "CoreLevelUp");
            if (cost.coreFragments > 0) EconomyManager.Instance.Spend(CurrencyId.CoreFragment, cost.coreFragments, "CoreLevelUp");

            coreLevels[id] = curLv + 1;
            return true;
        }

        public bool EquipCore(CoreId id, CoreSlotKind slot, int slotIndex = 0)
        {
            if (id == CoreId.None) return false;
            if (slot == CoreSlotKind.Main)
            {
                mainCore = id;
                return true;
            }
            if (slotIndex < 0 || slotIndex >= subCores.Length) return false;
            // 중복 방지 (Main/Sub 다른 슬롯에 동일 코어 차단)
            if (mainCore == id) return false;
            for (int i = 0; i < subCores.Length; i++)
                if (i != slotIndex && subCores[i] == id) return false;
            subCores[slotIndex] = id;
            return true;
        }

        // ── 플리퍼 강화 ──────────────────────────────────────

        public bool UpgradeFlipper()
        {
            if (flipperUpgradeTable == null) return false;
            if (flipperUpgradeLevel >= Constants.FlipperMaxLevel) return false;
            int idx = flipperUpgradeLevel; // Lv.1 → 인덱스 1 (Lv.2 비용)
            if (idx < 0 || idx >= flipperUpgradeTable.levels.Length) return false;
            var lv = flipperUpgradeTable.levels[idx];

            if (EconomyManager.Instance == null) return false;
            if (lv.manaCrystalCost > 0 && !EconomyManager.Instance.Has(CurrencyId.ManaCrystal, lv.manaCrystalCost)) return false;
            if (lv.bossSoulCost > 0 && !EconomyManager.Instance.Has(CurrencyId.BossSoul, lv.bossSoulCost)) return false;

            if (lv.manaCrystalCost > 0) EconomyManager.Instance.Spend(CurrencyId.ManaCrystal, lv.manaCrystalCost, "FlipperUpgrade");
            if (lv.bossSoulCost > 0) EconomyManager.Instance.Spend(CurrencyId.BossSoul, lv.bossSoulCost, "FlipperUpgrade");

            flipperUpgradeLevel++;
            EventBus.Publish(new OnFlipperUpgraded { NewLevel = flipperUpgradeLevel });
            return true;
        }

        public bool SelectFlipperVariant(FlipperVariantId variantId)
        {
            if (!IsVariantUnlocked) return false;
            if (variantId == FlipperVariantId.Basic) return false;
            // 첫 선택은 무료, 변경 시 3,000골드
            if (variantSelectedOnce && variantId != selectedVariant)
            {
                int cost = Constants.FlipperVariantChangeGoldCost;
                if (EconomyManager.Instance == null || !EconomyManager.Instance.Has(CurrencyId.Gold, cost)) return false;
                EconomyManager.Instance.Spend(CurrencyId.Gold, cost, "FlipperVariantChange");
            }
            selectedVariant = variantId;
            variantSelectedOnce = true;
            EventBus.Publish(new OnFlipperVariantSelected { VariantId = variantId });
            return true;
        }

        public FlipperVariantData GetVariantData(FlipperVariantId id)
        {
            return flipperUpgradeTable != null ? flipperUpgradeTable.GetVariant(id) : null;
        }
    }
}
