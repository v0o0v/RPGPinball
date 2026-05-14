using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 의뢰 정의 (Daily/Weekly/Bounty). Game_Design_Spec.md §3 주점 의뢰.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Quest", fileName = "Quest")]
    public class QuestData : ScriptableObject
    {
        [Header("식별")]
        public string questId;
        public QuestKind kind = QuestKind.Daily;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;
        public Sprite iconSprite;

        [Header("조건")]
        public QuestObjectiveKind objective = QuestObjectiveKind.None;
        public int targetValue;
        public string[] optionalArgs; // 액트 ID·재질 ID·시간 제한 등

        [Header("보상")]
        public int goldReward;
        public int manaCrystalReward;
        public int bossSoulReward;
        public CoreId coreFragmentRewardCoreId = CoreId.None;
        public int coreFragmentRewardCount;
        public RuneId runeRewardId = RuneId.None;
        public RuneGrade runeRewardGrade = RuneGrade.Normal;
        public int blueprintFragmentReward;
        public bool respecScrollReward;

        [Header("현상금 전용 — 입장 조건")]
        public BossId requiredActBossDefeated = BossId.None;
        public EliteId bountyTargetEliteId = EliteId.None;
    }
}
