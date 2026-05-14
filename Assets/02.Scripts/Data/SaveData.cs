using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 세이브 데이터 최상위 컨테이너. Data_Schema.md §1~§10 1:1 대응.
    /// JsonUtility 직렬화 가능하도록 [Serializable] + public 필드 + Dictionary 대신 List+Key 패턴.
    /// 변경 시 SaveVersion 갱신 + SaveMigration 작성 필요.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string version = RPGPinball.Core.Constants.SaveVersion;
        public string lastSaveTime = string.Empty; // ISO-8601 UTC
        public PlayerSaveData player = new PlayerSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public SkillTreeSaveData skillTree = new SkillTreeSaveData();
        public StageProgressSaveData stageProgress = new StageProgressSaveData();
        public VillageSaveData village = new VillageSaveData();
        public CollectionSaveData collection = new CollectionSaveData();
        public QuestsSaveData quests = new QuestsSaveData();
        public SettingsSaveData settings = new SettingsSaveData();
        public StatisticsSaveData statistics = new StatisticsSaveData();

        public static SaveData CreateDefault()
        {
            var data = new SaveData();
            data.lastSaveTime = DateTime.UtcNow.ToString("o");
            return data;
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string playerUID = "local-debug";
        public string playerName = "Player";
        public int level = 1;
        public int currentXP;
        public int totalSP;
        public int usedSP;
        public long gold;
        public long manaCrystal;
        public long bossSoul;
        public int respecScrollCount;
        public int resetCount;
        public int adContinueUsedToday;
        public string lastLoginDate = string.Empty;
        public int totalPlayTimeSec;
    }

    [Serializable]
    public class InventorySaveData
    {
        // 장착 슬롯
        public int equippedBallMaterial; // BallMaterialId
        public int equippedFlipperVariant; // FlipperVariantId
        public int equippedFlipperLevel;
        public int equippedMainCore; // CoreId(int)
        public int equippedMainCoreLevel;
        public List<EquippedSubCore> equippedSubCores = new List<EquippedSubCore>();
        public List<EquippedTarot> equippedTarots = new List<EquippedTarot>();
        public List<EquippedConsumable> equippedConsumables = new List<EquippedConsumable>();
        public List<EquippedRune> equippedRunes = new List<EquippedRune>();
        public List<int> skillDeckSkillIds = new List<int>(); // 4슬롯

        // 인벤토리
        public List<int> ownedFlipperVariants = new List<int>();
        public List<int> ownedCores = new List<int>();
        public List<OwnedRune> ownedRunes = new List<OwnedRune>();
        public List<OwnedTarot> ownedTarots = new List<OwnedTarot>();
        public List<OwnedConsumable> ownedConsumables = new List<OwnedConsumable>();
    }

    [Serializable]
    public struct EquippedSubCore { public int slotIndex; public int coreId; public int level; }

    [Serializable]
    public struct EquippedTarot { public int slotIndex; public int cardId; public int grade; }

    [Serializable]
    public struct EquippedConsumable { public int slotIndex; public int consumableId; public int remaining; }

    [Serializable]
    public struct EquippedRune { public int socketIndex; public string equippedOnSkillKey; public int runeId; public int grade; }

    [Serializable]
    public struct OwnedRune { public int runeId; public int grade; public int count; }

    [Serializable]
    public struct OwnedTarot { public int cardId; public int grade; public int count; public bool isPermanent; }

    [Serializable]
    public struct OwnedConsumable { public int consumableId; public int count; }

    [Serializable]
    public class SkillTreeSaveData
    {
        // 3분기 (Physical / Magical / Utility) -> 각 dictionary 대체용 List
        public List<SkillTreeEntry> physical = new List<SkillTreeEntry>();
        public List<SkillTreeEntry> magical = new List<SkillTreeEntry>();
        public List<SkillTreeEntry> utility = new List<SkillTreeEntry>();
    }

    [Serializable]
    public struct SkillTreeEntry { public int skillId; public int level; }

    [Serializable]
    public class StageProgressSaveData
    {
        public List<ActProgress> acts = new List<ActProgress>();
        public List<int> bossesDefeated = new List<int>(); // BossId
        public List<HiddenNodeRevealed> hiddenNodesRevealed = new List<HiddenNodeRevealed>();
        public List<EventNodeHistory> eventHistory = new List<EventNodeHistory>();
    }

    [Serializable]
    public class ActProgress
    {
        public int actId; // 1~4
        public bool unlocked;
        public List<StageProgressEntry> stages = new List<StageProgressEntry>();
    }

    [Serializable]
    public struct StageProgressEntry
    {
        public int stageIndex;     // 1~30
        public bool cleared;
        public string bestGrade;   // "S"/"A"/"B"/"C"
        public float bestTimeSec;
        public int continueCount;
    }

    [Serializable]
    public struct HiddenNodeRevealed { public int actId; public int stageIndex; }

    [Serializable]
    public struct EventNodeHistory { public int actId; public int stageIndex; public int rewardKind; public int amount; public string timestamp; }

    [Serializable]
    public class VillageSaveData
    {
        public ForgeVillageData forge = new ForgeVillageData();
        public EnchanterVillageData enchanter = new EnchanterVillageData();
        public AstrologerVillageData astrologer = new AstrologerVillageData();
        public TavernVillageData tavern = new TavernVillageData();
        public BalloonVillageData balloon = new BalloonVillageData();
        public TrainingVillageData training = new TrainingVillageData();
    }

    [Serializable]
    public class ForgeVillageData { public int materialId; public int mainCoreId; public int mainCoreLevel; public int flipperVariantId; public int flipperLevel; }

    [Serializable]
    public class EnchanterVillageData { public List<OwnedRune> inventory = new List<OwnedRune>(); }

    [Serializable]
    public class AstrologerVillageData
    {
        public int totalPullCount;
        public List<int> permanentCardIds = new List<int>();
    }

    [Serializable]
    public class TavernVillageData { public List<string> acceptedBountyIds = new List<string>(); }

    [Serializable]
    public class BalloonVillageData { public int upgradeLevel; public List<int> ownedUpgrades = new List<int>(); }

    [Serializable]
    public class TrainingVillageData
    {
        public int currentResetTier;
        public int skillResetCount;
        public List<int> deckPresetSkillIds = new List<int>();
    }

    [Serializable]
    public class CollectionSaveData
    {
        public List<GimmickEncounter> gimmicks = new List<GimmickEncounter>();
        public List<string> achievements = new List<string>();
        public List<string> titles = new List<string>();
        public List<string> skins = new List<string>();
    }

    [Serializable]
    public struct GimmickEncounter { public int gimmickId; public int encounterCount; public int resistLevel; }

    [Serializable]
    public class QuestsSaveData
    {
        public List<QuestEntry> daily = new List<QuestEntry>();
        public List<QuestEntry> weekly = new List<QuestEntry>();
        public List<QuestEntry> bountyBoard = new List<QuestEntry>();
        public string lastDailyRefreshIso = string.Empty;
        public string lastWeeklyRefreshIso = string.Empty;
    }

    [Serializable]
    public struct QuestEntry
    {
        public string questId;
        public int progress;
        public int target;
        public bool completed;
        public bool claimed;
    }

    [Serializable]
    public class SettingsSaveData
    {
        public float bgmVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public bool hapticEnabled = true;
        public int graphicsQuality = 2; // 0=Low,1=Med,2=High,3=Ultra
        public bool cloudSaveEnabled = true;
        // 접근성
        public int colorBlindMode;     // 0=Off
        public float touchSensitivity = 1.0f;
        public float screenShakeIntensity = 1.0f;
        public bool flashEffectReduction;
        public bool largeUIMode;
        public string languageCode = "ko";
    }

    [Serializable]
    public class StatisticsSaveData
    {
        public int totalStagesCleared;
        public int totalBossesDefeated;
        public int totalElitesDefeated;
        public int totalMonstersKilled;
        public int highestCombo;
        public float fastestBossKillSec;
        public long totalGoldEarned;
        public long totalManaCrystalEarned;
        public int totalContinueCount;
    }

    // ── §11 인게임 런타임 직렬화 (Data_Schema.md §11) ─────────────

    [Serializable]
    public class RuntimeStageSnapshot
    {
        public int actId;
        public int stageIndex;
        public ulong seed;
        public float remainingTimeSec;
        public int manaGauge;
        public int comboCount;
        public BallSnapshot ballState = new BallSnapshot();
        public List<BallSnapshot> multiBalls = new List<BallSnapshot>();
        public BossSnapshot bossState = new BossSnapshot();
        public List<MonsterSnapshot> monstersAlive = new List<MonsterSnapshot>();
        public List<GimmickSnapshot> activatedGimmicks = new List<GimmickSnapshot>();
        public List<SkillCooldownEntry> skillCooldowns = new List<SkillCooldownEntry>();
        public List<EquippedConsumable> consumablesRemaining = new List<EquippedConsumable>();
        public List<DroppedItem> droppedItems = new List<DroppedItem>();
        public string stageGrade = "B";
        public int continueCount;
        public string snapshotTimestampIso = string.Empty;
    }

    [Serializable]
    public struct BallSnapshot
    {
        public Vector2 position;
        public Vector2 velocity;
        public float angularVelocity;
        public int materialId;
    }

    [Serializable]
    public struct BossSnapshot
    {
        public int bossId;
        public int currentHP;
        public int maxHP;
        public int currentPhase;
        public Vector2 position;
        public List<int> activeBuffs;
        public List<int> activeDebuffs;
    }

    [Serializable]
    public struct MonsterSnapshot
    {
        public int monsterId;
        public Vector2 position;
        public int currentHP;
    }

    [Serializable]
    public struct GimmickSnapshot
    {
        public int gimmickId;
        public Vector2 position;
        public int chargeLevel;
        public float cooldown;
    }

    [Serializable]
    public struct SkillCooldownEntry { public int slotIndex; public float remaining; }

    [Serializable]
    public struct DroppedItem { public int currencyId; public Vector2 position; public int amount; }
}
