using UnityEngine;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Bases
{
    /// <summary>
    /// 영역 진입 시 디버프/버프 공통 (미스트 존 / 소용돌이 / 빙결 오라).
    /// 영역 안에 머무는 동안 누적 효과.
    /// </summary>
    public abstract class AreaEffectGimmickBase : GimmickBase
    {
        protected bool ballInside;

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (data == null) return;
            if (!other.CompareTag(Core.Constants.TagBall)) return;
            var ball = other.GetComponent<BallController>() ?? other.GetComponentInParent<BallController>();
            if (ball == null) return;
            ballInside = true;
            OnBallEnterArea(ball);
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(Core.Constants.TagBall)) return;
            var ball = other.GetComponent<BallController>() ?? other.GetComponentInParent<BallController>();
            if (ball == null) return;
            ballInside = false;
            OnBallExitArea(ball);
        }

        protected virtual void OnBallEnterArea(BallController ball) { }
        protected virtual void OnBallExitArea(BallController ball) { }
    }
}
