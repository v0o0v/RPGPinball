using RPGPinball.Combat;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Common
{
    /// <summary>
    /// 11. 체력/시간 구슬 — 접촉 시 StageTimer 회복.
    /// SO.timePenaltySeconds 가 양수면 회복, 음수면 페널티.
    /// </summary>
    public class TimeOrbGimmick : GimmickBase
    {
        protected override void HandleBallContact(BallController ball)
        {
            if (data == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            if (StageTimer.Instance != null && data.timePenaltySeconds != 0f)
            {
                if (data.timePenaltySeconds >= 0f) StageTimer.Instance.AddTime(data.timePenaltySeconds);
                else StageTimer.Instance.Penalize(-data.timePenaltySeconds);
            }

            if (data.triggerOnceOnly) ConsumeAndDespawn();
        }
    }
}
