using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>제로 블레이드 (파괴 · A · Tier 6 궁극기). 스텁.</summary>
    public class ZeroBlade : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[ZeroBlade:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
