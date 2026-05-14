using RPGPinball.Combat;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Common
{
    /// <summary>
    /// 15. 마나 제단 — 접촉 시 마나 충전 즉시 +N.
    /// </summary>
    public class ManaShrineGimmick : GimmickBase
    {
        protected override void HandleBallContact(BallController ball)
        {
            if (data == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            if (data.manaDelta != 0 && ManaSystem.Instance != null)
                ManaSystem.Instance.Charge(data.manaDelta);

            if (data.triggerOnceOnly) ConsumeAndDespawn();
        }
    }
}
