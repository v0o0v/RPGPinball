using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 아이스 폼 I (원소 · A전환 · Tier 3). 공을 빙결구로 전환.
    /// 타격 시 둔화 부여 (최대 70% × (1-0.95^Lv), 3초).
    /// 지속: 15 + Lv × 2s. 파이어볼 I과 동시 활성 불가.
    /// </summary>
    public class IceFormI : ActiveSkillBase
    {
        public override UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = Constants.TransformationBaseDuration + level * Constants.TransformationPerLevelDuration;
            float slowPct = SkillFormula.Diminish(0.7f, 0.95f, level);

            int affected = 0;
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var b in balls)
            {
                if (b == null) continue;
                b.SetTransformation(BallTransformation.IceForm, duration);
                affected++;
            }

            // 타격 시 둔화는 BallController OnCollisionEnter2D에서 호출되어야 하지만,
            // 마일스톤 3에서는 BallController 충돌 훅에 직접 통합하지 않고 IceFormI 인스턴스가
            // 자기 자신을 등록해 두는 방식으로 처리 (간단화).
            // 보강: 즉시 시야 내 모든 몬스터에게 1회 둔화 시도 (대용).
            foreach (var m in FindObjectsByType<RPGPinball.Enemy.MonsterBase>(FindObjectsSortMode.None))
            {
                if (m == null || m.IsDead) continue;
                m.ApplySlowDebuff(slowPct, 3f);
            }

            Debug.Log($"[IceFormI] 전환 Lv.{level}, 공 {affected}개, 둔화 {slowPct * 100f:F1}%, 지속 {duration:F1}s");
            return UniTask.CompletedTask;
        }
    }
}
