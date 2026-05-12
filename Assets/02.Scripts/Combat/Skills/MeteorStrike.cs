using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>메테오 스트라이크 (파괴 · A · Tier 6 궁극기). 스텁.</summary>
    public class MeteorStrike : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[MeteorStrike:STUB] @ {targetPos}");
            await UniTask.Yield(ct);
        }
    }
}
