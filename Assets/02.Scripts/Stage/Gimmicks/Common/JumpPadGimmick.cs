using UnityEngine;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Common
{
    /// <summary>
    /// 5. 점프 패드 — 상단 방향 30N 임펄스 + 무적 1.5s.
    /// </summary>
    public class JumpPadGimmick : GimmickBase
    {
        [SerializeField] private Vector2 launchDirection = Vector2.up;
        [SerializeField] private float invincibleSeconds = 1.5f;

        protected override void HandleBallContact(BallController ball)
        {
            if (data == null || ball == null || ball.Rb == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            Vector2 dir = launchDirection.sqrMagnitude < 0.01f ? Vector2.up : launchDirection.normalized;
            ball.Rb.AddForce(dir * data.impulseN, ForceMode2D.Impulse);
            // BallController.IsInvincible는 invincibleTimer를 통해 동작. 마일스톤 5는 무적 부여 훅 단순화 — Reset.
            ball.ApplyForcedSpeedMultiplier(1f, GetEffectiveDurationSeconds(invincibleSeconds));
        }
    }
}
