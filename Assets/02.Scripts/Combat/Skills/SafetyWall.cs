using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>세이프티 월 (제어 · A · Tier 4). 스텁 — 마일스톤 3에서 구현.</summary>
    public class SafetyWall : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            Debug.Log($"[SafetyWall:STUB] @ {targetPos} — 본 구현은 마일스톤 3");
            await UniTask.Yield(ct);
        }
    }
}
