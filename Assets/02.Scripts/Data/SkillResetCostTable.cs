using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 스킬 리셋 비용 시퀀스. 1회차 무료 / 2회차 1,000 / 3회차 3,000 / 4회차+ 5,000(고정).
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Skill Reset Cost", fileName = "SkillResetCostTable")]
    public class SkillResetCostTable : ScriptableObject
    {
        public int[] costs = new int[]
        {
            0, 1000, 3000, 5000
        };

        public int GetCost(int resetCount)
        {
            if (costs == null || costs.Length == 0) return 0;
            int idx = Mathf.Min(Mathf.Max(0, resetCount), costs.Length - 1);
            return costs[idx];
        }
    }
}
