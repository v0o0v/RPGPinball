using RPGPinball.Combat;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Nodes
{
    /// <summary>
    /// 휴식 노드 — 3종 효과 중 1종 선택 또는 모두 제시 (UI는 M7 인계).
    /// 마일스톤 5는 API 호출까지: StageTimer.AddBonusTime(20), ManaSystem.Charge, ShopPurchase 트리거.
    /// </summary>
    public static class RestNode
    {
        public enum Effect
        {
            BonusTime,    // 시간 회복 +20초 (상한 외)
            ManaCharge,   // 마나 50% 회복
            ShopPurchase  // 소모품 구매 (M6 인계)
        }

        public static Effect Pick(DeterministicRng rng) => (Effect)rng.NextInt(0, 3);

        public static void Apply(Effect effect)
        {
            switch (effect)
            {
                case Effect.BonusTime:
                    if (StageTimer.Instance != null) StageTimer.Instance.AddTime(20f);
                    break;
                case Effect.ManaCharge:
                    if (ManaSystem.Instance != null) ManaSystem.Instance.Charge(Core.Constants.ManaMax / 2);
                    break;
                case Effect.ShopPurchase:
                    // 마일스톤 6 MercenaryManager 인계 — 마일스톤 5는 노출만.
                    break;
            }
        }
    }
}
