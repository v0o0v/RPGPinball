using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>존 오브 컨트롤 (제어 · A · Tier 6 궁극기). 스텁.</summary>
    public class ZoneOfControl : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[ZoneOfControl:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
