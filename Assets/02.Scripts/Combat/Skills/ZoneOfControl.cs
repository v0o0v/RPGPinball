using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 존 오브 컨트롤 (제어 · A궁 · Tier 6). 플리퍼 쿨타임 0 + 스택 무한.
    /// 지속: 3.0 + Lv × 0.2s. 하드캡 0.5초를 무시하는 유일한 경로.
    /// </summary>
    public class ZoneOfControl : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 3f + level * 0.2f;

            var fc = FindAnyObjectByType<FlipperController>();
            if (fc == null)
            {
                Debug.LogWarning("[ZoneOfControl] FlipperController를 찾을 수 없음");
                return;
            }
            fc.OverrideCooldown(0f);
            fc.SetUnlimitedStack(true);

            Debug.Log($"[ZoneOfControl] 발동 Lv.{level}, 쿨타임 0 + 무한 스택, {duration:F1}s");

            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { /* 취소 정상 */ }
            finally
            {
                fc.OverrideCooldown(-1f);
                fc.SetUnlimitedStack(false);
            }
        }
    }
}
