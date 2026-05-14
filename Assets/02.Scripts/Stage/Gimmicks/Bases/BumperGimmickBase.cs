using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Bases
{
    /// <summary>
    /// 임펄스 발생 공통 (히든 범퍼 / HP 범퍼 / 카지노 범퍼 / 유령 범퍼).
    /// 공 접촉 시 SO 정의 임펄스를 반발 방향으로 적용.
    /// </summary>
    public abstract class BumperGimmickBase : GimmickBase
    {
        protected override void HandleBallContact(BallController ball)
        {
            if (data == null || ball == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            ApplyBumperImpulse(ball);
            ApplyReward(ball);

            if (data.triggerOnceOnly) ConsumeAndDespawn();
        }

        protected virtual void ApplyBumperImpulse(BallController ball)
        {
            if (ball.Rb == null) return;
            Vector2 dir = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            ball.Rb.AddForce(dir * data.impulseN, ForceMode2D.Impulse);
        }

        protected virtual void ApplyReward(BallController ball)
        {
            if (data.manaDelta != 0 && ManaSystem.Instance != null)
                ManaSystem.Instance.Charge(data.manaDelta);
            // XP / 골드 즉시 지급은 마일스톤 6 EconomyManager 도입 후 본격화.
        }
    }
}
