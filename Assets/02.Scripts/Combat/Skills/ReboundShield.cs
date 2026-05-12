using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>리바운드 실드 (제어 · A · Tier 5). 스텁.</summary>
    public class ReboundShield : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[ReboundShield:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
