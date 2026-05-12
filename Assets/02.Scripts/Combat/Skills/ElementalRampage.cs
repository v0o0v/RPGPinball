using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>원소 폭주 (원소 · A · Tier 6 궁극기). 스텁.</summary>
    public class ElementalRampage : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[ElementalRampage:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
