using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Security;

namespace RPGPinball.Meta
{
    /// <summary>
    /// 레벨 및 SP 시스템.
    /// 공식: RequiredXP = XPBase + level × XPPerLevel + level² × XPLevelSquared (Skill_Tree_Formulas.md)
    /// 오버레벨링 페널티: 자신 Lv - 적 Lv > 5 → ×0.5, > 10 → ×0.2
    /// SP 획득: 레벨업 +1, 보스 처치 +1, 액트 클리어 +5
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem Instance { get; private set; }

        [SerializeField] private PlayerData playerData;

        private SafeInt level;
        private SafeInt currentXP;
        private SafeInt totalSP;
        private SafeInt usedSP;

        public PlayerData Data => playerData;
        public int Level => level.Value;
        public int CurrentXP => currentXP.Value;
        public int TotalSP => totalSP.Value;
        public int UsedSP => usedSP.Value;
        public int AvailableSP => Mathf.Max(0, totalSP.Value - usedSP.Value);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // PlayerData가 비어있으면 임시 인스턴스 생성 (테스트/디버그 환경)
            if (playerData == null)
            {
                playerData = ScriptableObject.CreateInstance<PlayerData>();
                playerData.ResetToDefault();
            }

            LoadFromData();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveToData();
                Instance = null;
            }
        }

        // ── 영구 데이터 동기화 ───────────────────────────────

        public void LoadFromData()
        {
            level = SafeInt.Create(Mathf.Clamp(playerData.level, 1, Constants.LevelCap));
            currentXP = SafeInt.Create(Mathf.Max(0, playerData.currentXP));
            totalSP = SafeInt.Create(Mathf.Max(0, playerData.totalSP));
            usedSP = SafeInt.Create(Mathf.Max(0, playerData.usedSP));
        }

        public void SaveToData()
        {
            if (playerData == null) return;
            playerData.level = level.Value;
            playerData.currentXP = currentXP.Value;
            playerData.totalSP = totalSP.Value;
            playerData.usedSP = usedSP.Value;
        }

        // ── XP 공식 ──────────────────────────────────────────

        public static int RequiredXP(int level)
        {
            if (level < 1) level = 1;
            float xp = Constants.XPBase + level * Constants.XPPerLevel + level * level * Constants.XPLevelSquared;
            return Mathf.RoundToInt(xp);
        }

        // ── XP 획득 / 레벨업 ─────────────────────────────────

        /// <summary>적 처치 등으로 XP 획득. 오버레벨링 페널티 자동 적용.</summary>
        public void GainXP(int rawAmount, int enemyLevel)
        {
            if (rawAmount <= 0 || level.Value >= Constants.LevelCap) return;

            int adjusted = ApplyOverlevelPenalty(rawAmount, enemyLevel);
            if (adjusted <= 0) return;

            int newXP = currentXP.Value + adjusted;

            // 다중 레벨업 가능 — while 루프
            int req = RequiredXP(level.Value);
            while (newXP >= req && level.Value < Constants.LevelCap)
            {
                newXP -= req;
                LevelUp();
                req = RequiredXP(level.Value);
            }

            // Lv.100 도달 시 XP 절단
            if (level.Value >= Constants.LevelCap)
            {
                newXP = 0;
            }

            currentXP = SafeInt.Create(newXP);
            EventBus.Publish(new OnXPGained
            {
                Amount = adjusted,
                CurrentXP = currentXP.Value,
                RequiredXP = level.Value >= Constants.LevelCap ? 0 : RequiredXP(level.Value)
            });
            SaveToData();
        }

        public int ApplyOverlevelPenalty(int amount, int enemyLevel)
        {
            int diff = level.Value - enemyLevel;
            float mul = 1f;
            if (diff > Constants.OverlevelThreshold2) mul = Constants.OverlevelMul2;
            else if (diff > Constants.OverlevelThreshold1) mul = Constants.OverlevelMul1;
            return Mathf.RoundToInt(amount * mul);
        }

        private void LevelUp()
        {
            if (level.Value >= Constants.LevelCap) return;
            int prev = level.Value;
            level = SafeInt.Create(prev + 1);

            // SP +1
            totalSP = SafeInt.Create(totalSP.Value + Constants.SPPerLevel);

            EventBus.Publish(new OnLevelUp { PreviousLevel = prev, NewLevel = level.Value });
            EventBus.Publish(new OnSkillPointGained
            {
                Delta = Constants.SPPerLevel,
                TotalSP = totalSP.Value,
                Reason = "LevelUp"
            });
        }

        // ── 보스/액트 SP 보상 ────────────────────────────────

        /// <summary>보스 처치 시 SP +1. 마일스톤 4 BossAI에서 호출.</summary>
        public void AwardBossSP()
        {
            totalSP = SafeInt.Create(totalSP.Value + Constants.SPPerBoss);
            EventBus.Publish(new OnSkillPointGained
            {
                Delta = Constants.SPPerBoss,
                TotalSP = totalSP.Value,
                Reason = "BossKill"
            });
            SaveToData();
        }

        /// <summary>액트(1~4) 클리어 시 SP +5. 마일스톤 5 액트맵에서 호출.</summary>
        public void AwardActClearSP()
        {
            totalSP = SafeInt.Create(totalSP.Value + Constants.SPPerActClear);
            EventBus.Publish(new OnSkillPointGained
            {
                Delta = Constants.SPPerActClear,
                TotalSP = totalSP.Value,
                Reason = "ActClear"
            });
            SaveToData();
        }

        /// <summary>SP 사용 (SkillTreeManager에서 호출).</summary>
        public bool TryConsumeSP(int amount)
        {
            if (AvailableSP < amount) return false;
            usedSP = SafeInt.Create(usedSP.Value + amount);
            SaveToData();
            return true;
        }

        /// <summary>SP 환원 (스킬 리셋).</summary>
        public void RefundAllSP()
        {
            int refund = usedSP.Value;
            usedSP = SafeInt.Create(0);
            EventBus.Publish(new OnSkillReset { RefundedSP = refund });
            SaveToData();
        }

        // ── 디버그/테스트 ────────────────────────────────────

        /// <summary>테스트용 — 직접 레벨 설정. 운영 코드에서 호출 금지.</summary>
        public void DebugSetLevel(int newLevel)
        {
            level = SafeInt.Create(Mathf.Clamp(newLevel, 1, Constants.LevelCap));
            currentXP = SafeInt.Create(0);
            SaveToData();
        }

        /// <summary>테스트용 — 직접 SP 설정.</summary>
        public void DebugSetSP(int newTotal, int newUsed)
        {
            totalSP = SafeInt.Create(Mathf.Max(0, newTotal));
            usedSP = SafeInt.Create(Mathf.Max(0, Mathf.Min(newUsed, newTotal)));
            SaveToData();
        }
    }
}
