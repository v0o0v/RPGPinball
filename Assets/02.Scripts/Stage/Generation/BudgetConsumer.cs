using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 예산 소비 추적기. RewardOrBuff 최소 1개 / Trial 3개 이상 시 보상 자동 추가 규칙 enforce.
    /// </summary>
    public class BudgetConsumer
    {
        public int Remaining { get; private set; }
        public int InitialBudget { get; }
        public int TrialCount { get; private set; }
        public int RewardOrBuffCount { get; private set; }
        public int TotalPlaced { get; private set; }

        public BudgetConsumer(int initialBudget)
        {
            InitialBudget = initialBudget;
            Remaining = initialBudget;
        }

        /// <summary>
        /// cost 양수 → 디버프/시련 등(예산 소비). 음수 → 보상(예산 환급).
        /// 양수 cost가 잔여 예산보다 크면 거부.
        /// </summary>
        public bool TryConsume(int cost)
        {
            if (cost > 0 && Remaining < cost) return false;
            Remaining -= cost; // 음수 cost는 환급
            return true;
        }

        public bool CanAfford(int cost)
        {
            if (cost <= 0) return true; // 보상은 항상 가능
            return Remaining >= cost;
        }

        public void Refund(int amount)
        {
            if (amount <= 0) return;
            Remaining += amount;
        }

        public void TrackPlacement(GimmickCategory category)
        {
            TotalPlaced++;
            if (category == GimmickCategory.Reward || category == GimmickCategory.Buff)
                RewardOrBuffCount++;
            else if (category == GimmickCategory.Trial || category == GimmickCategory.Debuff)
                TrialCount++;
        }

        /// <summary>스테이지에 보상/버프가 최소 1개 강제 — 부족하면 true.</summary>
        public bool NeedsRewardOrBuff()
            => RewardOrBuffCount < Core.Constants.ProcRewardOrBuffMin;

        /// <summary>시련 3개 이상 + 보상 0 → 구제 보상 자동 추가 필요.</summary>
        public bool NeedsRescueReward()
            => TrialCount >= Core.Constants.ProcTrialOverloadThreshold && RewardOrBuffCount == 0;
    }
}
