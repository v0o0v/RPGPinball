using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 스테이지/보스/엘리트 보상 공식 단일 참조점. Game_Design_Spec.md §8.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("스테이지 클리어")]
        public int stageGoldBase = 50;
        public int stageGoldPerStage = 10;
        public int stageManaCrystalBase = 5;
        public int stageManaCrystalPerFiveStages = 1;

        [Header("보스 처치")]
        public int bossGoldBase = 300;
        public float[] actMultipliers = new[] { 1.0f, 1.8f, 2.5f, 3.5f };
        public int bossSoulNormal = 2;
        public int bossSoulFinal = 5;
        public int spReward = 2;

        [Header("엘리트 처치")]
        public int eliteGoldMin = 30;
        public int eliteGoldMax = 50;
        public int eliteCoreFragmentReward = 3;

        [Header("등급 보너스 배율")]
        public float gradeBonusS = 1.3f;
        public float gradeBonusA = 1.0f;
        public float gradeBonusB = 0.8f;
        public float gradeBonusC = 0.5f;

        [Header("의뢰 보상")]
        public int dailyQuestRewardGoldPerOne = 300;
        public int weeklyQuestRewardGold = 2000;

        public float GetActMultiplier(ActId actId)
        {
            int idx = ((int)actId) - 1;
            if (idx < 0 || actMultipliers == null || idx >= actMultipliers.Length) return 1f;
            return actMultipliers[idx];
        }

        public float GetGradeBonus(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return gradeBonusA;
            switch (grade)
            {
                case "S": return gradeBonusS;
                case "A": return gradeBonusA;
                case "B": return gradeBonusB;
                case "C": return gradeBonusC;
                default: return gradeBonusA;
            }
        }

        public int CalculateStageGold(int stageIndex)
        {
            return stageGoldBase + stageIndex * stageGoldPerStage;
        }

        public int CalculateStageManaCrystal(int stageIndex)
        {
            return stageManaCrystalBase + (stageIndex / 5);
        }

        public int CalculateBossGold(ActId actId, bool isFinalBoss)
        {
            float mul = GetActMultiplier(actId);
            int baseGold = Mathf.RoundToInt(bossGoldBase * mul);
            return isFinalBoss ? Mathf.RoundToInt(baseGold * 1.5f) : baseGold;
        }
    }
}
