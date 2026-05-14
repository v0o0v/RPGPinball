using UnityEngine;
using RPGPinball.Physics;
using RPGPinball.Core;

namespace RPGPinball.Stage.Gimmicks.Bases
{
    /// <summary>
    /// 강제 가속 + 방향 정렬 공통 (가속 레일 / 해류 에스컬레이터 / 컨베이어).
    /// 영역 진입 시 진행 방향으로 강제 속도 정렬.
    /// </summary>
    public abstract class RailGimmickBase : GimmickBase
    {
        [SerializeField] protected Vector2 railDirection = Vector2.up;
        [SerializeField] protected float speedMultiplier = 1.5f;

        protected override void HandleBallContact(BallController ball)
        {
            if (data == null || ball == null || ball.Rb == null) return;

            Vector2 dir = railDirection.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;

            float currentSpeed = ball.Rb.linearVelocity.magnitude;
            float newSpeed = Mathf.Min(currentSpeed * speedMultiplier, Constants.BallMaxSpeed);
            ball.Rb.linearVelocity = dir * newSpeed;
        }
    }
}
