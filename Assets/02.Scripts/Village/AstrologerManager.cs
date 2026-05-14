using System;
using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// 보유 타로카드 인스턴스. 영구 카드는 isPermanent=true 이고 duplicateCount 가 사용 횟수.
    /// </summary>
    [System.Serializable]
    public class TarotInstance
    {
        public TarotCardId id;
        public TarotGrade grade;
        public bool isPermanent;
        public int duplicateCount;

        public TarotInstance(TarotCardId id, TarotGrade grade)
        {
            this.id = id;
            this.grade = grade;
            this.duplicateCount = 1;
        }
    }

    /// <summary>
    /// 점성술사. 38장 타로카드 뽑기·장착(3슬롯)·영구 승급(10장+5,000골드).
    /// </summary>
    public class AstrologerManager : MonoBehaviour
    {
        public static AstrologerManager Instance { get; private set; }

        [SerializeField] private TarotCardData[] cardCatalog;
        [SerializeField] private List<TarotInstance> inventory = new();
        [SerializeField] private TarotCardId[] equippedSlots = new TarotCardId[Constants.TarotEquipSlots];

        private System.Random rng = new System.Random();

        public IReadOnlyList<TarotInstance> Inventory => inventory;
        public IReadOnlyList<TarotCardId> EquippedSlots => equippedSlots;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
            for (int i = 0; i < equippedSlots.Length; i++)
                if (equippedSlots[i] == TarotCardId.None) equippedSlots[i] = TarotCardId.None;
        }

        private void OnDisable() { if (Instance == this) Instance = null; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ResetInstance() { Instance = null; }

        public void InitializeForTest()
        {
            if (Instance == null) Instance = this;
        }

        public TarotCardData GetCardData(TarotCardId id)
        {
            if (cardCatalog == null) return null;
            foreach (var c in cardCatalog) if (c != null && c.cardId == id) return c;
            return null;
        }

        /// <summary>테스트용 RNG 주입 (시드 고정).</summary>
        public void SetSeed(int seed) { rng = new System.Random(seed); }

        // ── 뽑기 ──────────────────────────────────────────────

        /// <summary>1회 뽑기 (500골드 또는 보스 영혼 3). 비용 차감 + 인벤토리 추가.</summary>
        public TarotInstance Pull(bool useBossSoul = false)
        {
            if (cardCatalog == null || cardCatalog.Length == 0) return null;
            var econ = EconomyManager.Instance;
            if (econ == null) return null;

            if (useBossSoul)
            {
                if (!econ.Spend(CurrencyId.BossSoul, Constants.TarotPullBossSoulCost, "TarotPull")) return null;
            }
            else
            {
                if (!econ.Spend(CurrencyId.Gold, Constants.TarotPullGoldCost, "TarotPull")) return null;
            }

            // 등급 추첨 (가중치 60/25/10/5)
            float roll = (float)rng.NextDouble() * 100f;
            TarotGrade grade;
            if (roll < Constants.TarotDropCommonPct) grade = TarotGrade.Common;
            else if (roll < Constants.TarotDropCommonPct + Constants.TarotDropRarePct) grade = TarotGrade.Rare;
            else if (roll < Constants.TarotDropCommonPct + Constants.TarotDropRarePct + Constants.TarotDropLegendaryPct) grade = TarotGrade.Legendary;
            else grade = TarotGrade.Mythic;

            // 해당 등급 풀에서 균일 추첨
            var pool = new List<TarotCardData>();
            foreach (var c in cardCatalog) if (c != null && c.grade == grade) pool.Add(c);
            if (pool.Count == 0)
            {
                // 폴백: 사용 가능한 카드 풀에서 균일
                foreach (var c in cardCatalog) if (c != null) pool.Add(c);
                if (pool.Count == 0) return null;
            }
            var pick = pool[rng.Next(0, pool.Count)];

            var inst = AddOrIncrement(pick.cardId, pick.grade);
            EventBus.Publish(new OnTarotPulled
            {
                CardIds = new[] { pick.cardId },
                Grades = new[] { pick.grade }
            });
            return inst;
        }

        private TarotInstance AddOrIncrement(TarotCardId id, TarotGrade grade)
        {
            foreach (var t in inventory)
            {
                if (t.id == id && !t.isPermanent)
                {
                    t.duplicateCount++;
                    return t;
                }
            }
            var newInst = new TarotInstance(id, grade);
            inventory.Add(newInst);
            return newInst;
        }

        public TarotInstance Find(TarotCardId id)
        {
            foreach (var t in inventory) if (t.id == id) return t;
            return null;
        }

        public int CountOf(TarotCardId id)
        {
            int n = 0;
            foreach (var t in inventory) if (t.id == id) n += Math.Max(1, t.duplicateCount);
            return n;
        }

        // ── 장착 ──────────────────────────────────────────────

        public bool Equip(TarotCardId id, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Length) return false;
            if (id == TarotCardId.None) return false;
            // 중복 장착 차단
            for (int i = 0; i < equippedSlots.Length; i++)
                if (i != slotIndex && equippedSlots[i] == id) return false;
            // 보유 여부 확인
            if (Find(id) == null) return false;
            equippedSlots[slotIndex] = id;
            EventBus.Publish(new OnTarotEquipped { CardId = id, SlotIndex = slotIndex });
            return true;
        }

        public void Unequip(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedSlots.Length) return;
            var prev = equippedSlots[slotIndex];
            equippedSlots[slotIndex] = TarotCardId.None;
            EventBus.Publish(new OnTarotUnequipped { CardId = prev, SlotIndex = slotIndex });
        }

        // ── 영구 카드 승급 (10장 + 5,000골드) ────────────────

        public bool UpgradeToPermanent(TarotCardId id)
        {
            int have = CountOf(id);
            if (have < Constants.TarotPermanentRequiredCount) return false;
            var econ = EconomyManager.Instance;
            if (econ == null || !econ.Has(CurrencyId.Gold, Constants.TarotPermanentGoldCost)) return false;
            econ.Spend(CurrencyId.Gold, Constants.TarotPermanentGoldCost, "TarotPermanent");

            // 동일 카드 9장 소진 + 1장만 영구로 변환
            int toRemove = Constants.TarotPermanentRequiredCount - 1;
            for (int i = inventory.Count - 1; i >= 0 && toRemove > 0; i--)
            {
                if (inventory[i].id == id && !inventory[i].isPermanent)
                {
                    int reduce = Mathf.Min(toRemove, Mathf.Max(1, inventory[i].duplicateCount));
                    inventory[i].duplicateCount -= reduce;
                    toRemove -= reduce;
                    if (inventory[i].duplicateCount <= 0) inventory.RemoveAt(i);
                }
            }
            // 남은 인스턴스 중 하나를 영구로 변환
            foreach (var t in inventory)
            {
                if (t.id == id)
                {
                    t.isPermanent = true;
                    t.duplicateCount = 1;
                    EventBus.Publish(new OnTarotPermanentUpgraded { CardId = id });
                    return true;
                }
            }
            // 남은 인스턴스가 없으면 (예외 케이스) 새로 추가
            var data = GetCardData(id);
            var grade = data != null ? data.grade : TarotGrade.Common;
            var inst = new TarotInstance(id, grade) { isPermanent = true };
            inventory.Add(inst);
            EventBus.Publish(new OnTarotPermanentUpgraded { CardId = id });
            return true;
        }
    }
}
