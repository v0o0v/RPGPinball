using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Combat;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Stage;

namespace RPGPinball.Village
{
    /// <summary>
    /// 수련장. 스킬 트리 UI · 덱 4칸 세팅 · 리셋 · 보스 연습 모드 진입점.
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        public static TrainingManager Instance { get; private set; }

        [SerializeField] private SkillResetCostTable resetCostTable;
        [SerializeField] private PlayerData playerData;
        [SerializeField] private List<BossId> practiceModeBossesUnlocked = new();

        public IReadOnlyList<BossId> PracticeModeBossesUnlocked => practiceModeBossesUnlocked;

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

        private void OnEnable()
        {
            EventBus.Subscribe<OnBossDefeated>(HandleBossDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnBossDefeated>(HandleBossDefeated);
            if (Instance == this) Instance = null;
        }

        private void HandleBossDefeated(OnBossDefeated e)
        {
            if (e.BossId != BossId.None && !practiceModeBossesUnlocked.Contains(e.BossId))
                practiceModeBossesUnlocked.Add(e.BossId);
        }

        // ── 스킬 덱 ──────────────────────────────────────────

        public bool EquipDeckSlot(int slotIndex, SkillData skill, int level = 1)
        {
            if (SkillDeck.Instance == null) return false;
            bool ok = SkillDeck.Instance.Equip(slotIndex, skill, level);
            if (ok) EventBus.Publish(new OnSkillDeckEquipped { SlotIndex = slotIndex, SkillId = skill != null ? skill.id : 0 });
            return ok;
        }

        public bool ClearDeckSlot(int slotIndex)
        {
            if (SkillDeck.Instance == null) return false;
            return SkillDeck.Instance.Equip(slotIndex, null);
        }

        // ── 스킬 리셋 ────────────────────────────────────────

        public int GetResetCost()
        {
            int count = playerData != null ? playerData.resetCount : 0;
            return resetCostTable != null ? resetCostTable.GetCost(count) : 0;
        }

        public bool ResetAllSkills(bool useScroll = false)
        {
            var econ = EconomyManager.Instance;
            if (econ == null) return false;

            if (useScroll)
            {
                if (!econ.Has(CurrencyId.RespecScroll, 1)) return false;
                econ.Spend(CurrencyId.RespecScroll, 1, "SkillReset");
            }
            else
            {
                int cost = GetResetCost();
                if (cost > 0)
                {
                    if (!econ.Has(CurrencyId.Gold, cost)) return false;
                    econ.Spend(CurrencyId.Gold, cost, "SkillReset");
                }
            }

            if (SkillTreeManager.Instance != null) SkillTreeManager.Instance.ResetAll();
            if (playerData != null) playerData.resetCount++;
            return true;
        }

        // ── 보스 연습 모드 ───────────────────────────────────

        public bool OpenBossPractice(BossId bossId)
        {
            if (!practiceModeBossesUnlocked.Contains(bossId)) return false;
            BossPracticeStageBuilder.Build(bossId);
            return true;
        }
    }
}
