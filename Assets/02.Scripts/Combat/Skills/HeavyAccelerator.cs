using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>헤비 액셀러레이터 (파괴 · A · Tier 5). 스텁.</summary>
    public class HeavyAccelerator : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[HeavyAccelerator:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
