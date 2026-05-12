using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>매스 리콜 (제어 · A · Tier 5). 스텁.</summary>
    public class MassRecall : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[MassRecall:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
