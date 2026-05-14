using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// 룬 인벤토리 인스턴스. 스킬에 1:1 매핑 (장착되지 않으면 equippedOnSkillId == null).
    /// </summary>
    [System.Serializable]
    public class RuneInstance
    {
        public RuneId id;
        public RuneGrade grade;
        public string equippedOnSkillId; // null = 미장착

        public RuneInstance(RuneId id, RuneGrade grade)
        {
            this.id = id;
            this.grade = grade;
            this.equippedOnSkillId = null;
        }

        public Sprite GetIcon(RuneData data)
        {
            return data != null ? data.GetIcon(grade) : null;
        }
    }

    /// <summary>
    /// 마법 부여소. 룬 9종 × 3등급 인벤토리·합성·장착 관리.
    /// </summary>
    public class EnchanterManager : MonoBehaviour
    {
        public static EnchanterManager Instance { get; private set; }

        [SerializeField] private RuneData[] runeCatalog;
        [SerializeField] private List<RuneInstance> inventory = new();

        public IReadOnlyList<RuneInstance> Inventory => inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDisable() { if (Instance == this) Instance = null; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public static void ResetInstance() { Instance = null; }

        /// <summary>테스트 / 외부 코드용 — EditMode 에서 Awake 가 호출되지 않을 때 강제로 Instance 등록.</summary>
        public void InitializeForTest()
        {
            if (Instance == null) Instance = this;
        }

        public RuneData GetRuneData(RuneId id)
        {
            if (runeCatalog == null) return null;
            foreach (var r in runeCatalog) if (r != null && r.runeId == id) return r;
            return null;
        }

        public void AddRune(RuneId id, RuneGrade grade)
        {
            if (id == RuneId.None) return;
            inventory.Add(new RuneInstance(id, grade));
        }

        public RuneInstance FindUnequipped(RuneId id, RuneGrade grade)
        {
            foreach (var r in inventory)
                if (r.id == id && r.grade == grade && string.IsNullOrEmpty(r.equippedOnSkillId)) return r;
            return null;
        }

        public bool EquipRune(RuneId id, RuneGrade grade, string skillId, int socketCount)
        {
            if (string.IsNullOrEmpty(skillId)) return false;
            int alreadyOn = 0;
            foreach (var r in inventory) if (r.equippedOnSkillId == skillId) alreadyOn++;
            if (alreadyOn >= socketCount) return false;

            var inst = FindUnequipped(id, grade);
            if (inst == null) return false;
            inst.equippedOnSkillId = skillId;
            EventBus.Publish(new OnRuneEquipped { RuneId = id, Grade = grade, EquippedOnSkillId = skillId });
            return true;
        }

        public bool UnequipRune(RuneInstance inst)
        {
            if (inst == null || string.IsNullOrEmpty(inst.equippedOnSkillId)) return false;
            var skillId = inst.equippedOnSkillId;
            inst.equippedOnSkillId = null;
            EventBus.Publish(new OnRuneUnequipped { RuneId = inst.id, Grade = inst.grade, EquippedOnSkillId = skillId });
            return true;
        }

        public List<RuneInstance> GetEquippedRunes(string skillId)
        {
            var list = new List<RuneInstance>();
            if (string.IsNullOrEmpty(skillId)) return list;
            foreach (var r in inventory)
                if (r.equippedOnSkillId == skillId) list.Add(r);
            return list;
        }

        /// <summary>동일 룬 3개 합성 → 상위 등급 1개. 골드 차감. Legendary 는 불가.</summary>
        public bool FuseRune(RuneId id, RuneGrade fromGrade)
        {
            if (fromGrade == RuneGrade.Legendary) return false;

            int count = 0;
            foreach (var r in inventory)
                if (r.id == id && r.grade == fromGrade && string.IsNullOrEmpty(r.equippedOnSkillId)) count++;
            if (count < Constants.RuneFuseRequiredCount) return false;

            int goldCost = fromGrade == RuneGrade.Normal
                ? Constants.RuneFuseGoldNormalToRare
                : Constants.RuneFuseGoldRareToLegendary;
            if (EconomyManager.Instance == null || !EconomyManager.Instance.Has(CurrencyId.Gold, goldCost)) return false;
            EconomyManager.Instance.Spend(CurrencyId.Gold, goldCost, "RuneFuse");

            // 3개 제거
            int removed = 0;
            for (int i = inventory.Count - 1; i >= 0 && removed < Constants.RuneFuseRequiredCount; i--)
            {
                var r = inventory[i];
                if (r.id == id && r.grade == fromGrade && string.IsNullOrEmpty(r.equippedOnSkillId))
                {
                    inventory.RemoveAt(i);
                    removed++;
                }
            }
            var newGrade = fromGrade + 1;
            inventory.Add(new RuneInstance(id, newGrade));
            EventBus.Publish(new OnRuneFused { RuneId = id, FromGrade = fromGrade, ToGrade = newGrade });
            return true;
        }
    }
}
