using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>타임 딜레이전 (제어 · A · Tier 6 궁극기). 스텁.</summary>
    public class TimeDilation : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[TimeDilation:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
