using UnityEngine;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks.Bases
{
    /// <summary>
    /// 위치 이동 + 속도 보존 공통 (블랙홀 웜홀 / 텔레포트 패널).
    /// 짝 패널은 동일 SO 인스턴스 + paired 슬롯으로 등록.
    /// </summary>
    public abstract class TeleportGimmickBase : GimmickBase
    {
        [SerializeField] protected Transform pairedExit;
        [SerializeField] protected float reentryLockoutSeconds = 0.5f;

        public void SetPairedExit(Transform exit) => pairedExit = exit;

        protected override void HandleBallContact(BallController ball)
        {
            if (data == null || ball == null || pairedExit == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            Vector2 keptVelocity = ball.Rb != null ? ball.Rb.linearVelocity : Vector2.zero;
            ball.transform.position = pairedExit.position;
            if (ball.Rb != null) ball.Rb.linearVelocity = keptVelocity;
        }

        protected override void Update()
        {
            base.Update();
            // 재진입 잠금은 cooldownSeconds 가 데이터에서 0.5s로 들어오므로 IsOnCooldown으로 충분.
        }
    }
}
