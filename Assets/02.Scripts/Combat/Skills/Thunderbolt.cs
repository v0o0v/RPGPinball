using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>썬더볼트 (원소 · A · Tier 5). 스텁.</summary>
    public class Thunderbolt : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[Thunderbolt:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
