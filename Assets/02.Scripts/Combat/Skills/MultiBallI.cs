using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>멀티볼 I (원소 · A · Tier 4). 스텁.</summary>
    public class MultiBallI : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[MultiBallI:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
