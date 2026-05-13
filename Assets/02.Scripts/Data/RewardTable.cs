using System;

namespace RPGPinball.Data
{
    /// <summary>
    /// 보스/엘리트 격파 보상 테이블. 마일스톤 4에서는 OnBossDefeated 이벤트 페이로드로만 사용.
    /// 실제 재화 지급은 마일스톤 6 EconomyManager 인계.
    /// 엘리트 고유 코어 조각/전설 룬은 Elite_Bounty_Spec.md 참조.
    /// </summary>
    [Serializable]
    public struct RewardTable
    {
        public int bonusXp;
        public int bonusGold;
        public int bossSoul;
        public int manaCrystal;
        public int spReward;

        /// <summary>엘리트 고유 드랍 ID. 0이면 미사용 (코어 조각 ID 또는 전설 룬 ID).</summary>
        public int uniqueDropId;
    }
}
