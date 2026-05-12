using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 타임 딜레이전 (제어 · A궁 · Tier 6). 전체 시간 ×0.25 슬로우.
    /// 지속: 3.0 + Lv × 0.2s. GameManager.State는 Playing 유지, Time.timeScale 직접 조작.
    /// </summary>
    public class TimeDilation : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 3f + level * 0.2f;
            float originalScale = Time.timeScale;
            Time.timeScale = Constants.TimeDilationScale;

            Debug.Log($"[TimeDilation] 발동 Lv.{level}, ×{Constants.TimeDilationScale}, {duration:F1}s");

            try
            {
                // duration은 실시간 기준으로 대기 (DelayType.Realtime)
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), DelayType.Realtime, cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { /* 취소 정상 */ }
            finally
            {
                Time.timeScale = originalScale;
            }
        }
    }
}
