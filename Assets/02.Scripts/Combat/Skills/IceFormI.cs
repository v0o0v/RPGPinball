using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>아이스 폼 I (원소 · A전환 · Tier 2). 스텁.</summary>
    public class IceFormI : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[IceFormI:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
