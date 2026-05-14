using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Combat;

namespace RPGPinball.Village
{
    /// <summary>
    /// 용병단 창고. 3종 소모품(긴급 방패 / 시간의 모래 / 광란의 부적) 제작·장착·사용.
    /// </summary>
    public class MercenaryManager : MonoBehaviour
    {
        public static MercenaryManager Instance { get; private set; }

        [SerializeField] private ConsumableData[] consumableCatalog;
        [SerializeField] private Dictionary<ConsumableId, int> inventory;
        [SerializeField] private ConsumableId[] equippedSlots = new ConsumableId[Constants.MercenarySlotCount];

        public IReadOnlyDictionary<ConsumableId, int> Inventory => inventory;
        public IReadOnlyList<ConsumableId> EquippedSlots => equippedSlots;

        // 광란의 부적 활성 상태 (DamageCalculator/BallController가 조회)
        private float frenzyEndTime = -1f;
        public bool IsFrenzyActive => Time.time < frenzyEndTime;
        public float FrenzyDamageMultiplier { get; private set; } = 1f;
        public float FrenzySpeedMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
            if (inventory == null) inventory = new Dictionary<ConsumableId, int>();
        }

        private void OnDisable() { if (Instance == this) Instance = null; }

        public ConsumableData GetData(ConsumableId id)
        {
            if (consumableCatalog == null) return null;
            foreach (var c in consumableCatalog) if (c != null && c.consumableId == id) return c;
            return null;
        }

        public int GetCount(ConsumableId id) => inventory.TryGetValue(id, out var v) ? v : 0;

        public bool Craft(ConsumableId id)
        {
            var d = GetData(id);
            if (d == null) return false;
            var econ = EconomyManager.Instance;
            if (econ == null) return false;
            if (d.goldCost > 0 && !econ.Has(CurrencyId.Gold, d.goldCost)) return false;
            if (d.specialOreCount > 0 && d.specialOreId != CurrencyId.None && !econ.Has(d.specialOreId, d.specialOreCount)) return false;

            if (d.goldCost > 0) econ.Spend(CurrencyId.Gold, d.goldCost, "ConsumableCraft");
            if (d.specialOreCount > 0 && d.specialOreId != CurrencyId.None)
                econ.Spend(d.specialOreId, d.specialOreCount, "ConsumableCraft");

            inventory[id] = GetCount(id) + 1;
            EventBus.Publish(new OnConsumableCrafted { ConsumableId = id });
            return true;
        }

        public bool Equip(ConsumableId id, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Length) return false;
            if (GetCount(id) <= 0) return false;
            equippedSlots[slotIndex] = id;
            return true;
        }

        public bool Use(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Length) return false;
            var id = equippedSlots[slotIndex];
            if (id == ConsumableId.None) return false;
            if (GetCount(id) <= 0) return false;
            var d = GetData(id);
            if (d == null) return false;

            inventory[id] = GetCount(id) - 1;
            EventBus.Publish(new OnConsumableUsed { ConsumableId = id });

            switch (id)
            {
                case ConsumableId.EmergencyShield:
                    float shieldDur = d.shieldDurationSeconds > 0f ? d.shieldDurationSeconds : Constants.EmergencyShieldDuration;
                    BossForcedResetGuard.Activate(shieldDur);
                    break;
                case ConsumableId.SandsOfTime:
                    float bonus = d.timeBonusSeconds > 0f ? d.timeBonusSeconds : Constants.SandsOfTimeBonusSeconds;
                    if (StageTimer.Instance != null) StageTimer.Instance.AddTime(bonus);
                    break;
                case ConsumableId.FrenzyCharm:
                    float fdur = d.durationSeconds > 0f ? d.durationSeconds : Constants.FrenzyCharmDuration;
                    FrenzyDamageMultiplier = d.damageMultiplier > 0f ? d.damageMultiplier : Constants.FrenzyCharmDamageMultiplier;
                    FrenzySpeedMultiplier = d.ballSpeedMultiplier > 0f ? d.ballSpeedMultiplier : Constants.FrenzyCharmSpeedMultiplier;
                    frenzyEndTime = Time.time + fdur;
                    break;
            }
            return true;
        }
    }
}
