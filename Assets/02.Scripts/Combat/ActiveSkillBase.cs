using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Enemy;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 모든 액티브 스킬의 추상 베이스. SkillDeck이 Execute를 호출.
    /// 마나 사전 차감은 SkillDeck에서 처리. 본 클래스는 발동/지속 효과만 책임.
    /// </summary>
    public abstract class ActiveSkillBase : MonoBehaviour
    {
        [SerializeField] protected SkillData data;
        [SerializeField] protected int level = 1;

        public SkillData Data => data;
        public int Level
        {
            get => level;
            set => level = Mathf.Clamp(value, 1, data != null ? data.maxLevel : 1);
        }

        public void Initialize(SkillData skillData, int skillLevel)
        {
            data = skillData;
            Level = skillLevel;
        }

        /// <summary>발동. targetPos는 터치 월드 좌표. ct로 취소 가능.</summary>
        public abstract UniTask Execute(Vector2 targetPos, CancellationToken ct);

        // ── 공통 판정 헬퍼 ────────────────────────────────────

        protected static IEnumerable<MonsterBase> OverlapCircleMonsters(Vector2 center, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(center, radius);
            foreach (var h in hits)
            {
                var m = h.GetComponent<MonsterBase>();
                if (m != null && !m.IsDead) yield return m;
            }
        }

        protected static IEnumerable<MonsterBase> OverlapBoxMonsters(Vector2 center, Vector2 size, float angle)
        {
            var hits = Physics2D.OverlapBoxAll(center, size, angle);
            foreach (var h in hits)
            {
                var m = h.GetComponent<MonsterBase>();
                if (m != null && !m.IsDead) yield return m;
            }
        }

        /// <summary>스킬 데미지 계산. baseDmg를 입력받아 DamageCalculator의 단순화된 형태를 적용.</summary>
        protected DamageResult ComputeSkillDamage(MonsterBase target, int playerLevel)
        {
            var ctx = DamageContext.Default(playerLevel, data != null ? data.damageType : DamageType.Physical);
            ctx.TargetDefense = target.Data.defense;
            ctx.TargetMagicResist = target.Data.magicResist;

            // 스킬 자체 배율을 곱연산 한 칸에 주입
            float skillMult = (data != null ? data.baseMultiplier : 1f) + (level - 1) * (data != null ? data.perLevelMultiplier : 0f);
            ctx.MultiplierFactors = new[] { skillMult };

            return DamageCalculator.Calculate(ctx);
        }
    }
}
