using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>볼텍스 (원소 · A · Tier 5). 스텁.</summary>
    public class Vortex : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[Vortex:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
