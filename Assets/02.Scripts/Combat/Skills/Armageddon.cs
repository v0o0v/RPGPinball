using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>아마겟돈 (원소 · A · Tier 6 궁극기). 스텁.</summary>
    public class Armageddon : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[Armageddon:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
