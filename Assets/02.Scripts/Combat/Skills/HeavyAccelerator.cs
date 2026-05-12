using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 헤비 액셀러레이터 (파괴 · A · Tier 5). 공 속도 최고치 40 강제 고정.
    /// 지속: 2.0 + Lv × 0.15s. 종료 후 ClampSpeed 자연 복귀.
    /// </summary>
    public class HeavyAccelerator : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 2f + level * 0.15f;
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var b in balls)
            {
                b.SetForcedSpeed(Constants.BallMaxSpeed);
            }
            Debug.Log($"[HeavyAccelerator] 발동 Lv.{level}, 강제 속도 {Constants.BallMaxSpeed}, {duration:F1}s ({balls.Length}공)");

            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { /* 취소 정상 */ }
            finally
            {
                foreach (var b in balls)
                {
                    if (b != null) b.ClearForcedSpeed();
                }
            }
        }
    }
}
