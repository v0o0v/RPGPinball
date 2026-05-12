using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy;
using RPGPinball.Meta;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 모든 액티브 스킬의 추상 베이스. SkillDeck이 Execute를 호출.
    /// 마나 사전 차감은 SkillDeck에서 처리. 본 클래스는 발동/지속 효과만 책임.
    /// 마일스톤 3에서 광역/넉백/궁극기 공통 헬퍼 보강.
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
            var hits = UnityEngine.Physics2D.OverlapCircleAll(center, radius);
            foreach (var h in hits)
            {
                var m = h.GetComponent<MonsterBase>();
                if (m != null && !m.IsDead) yield return m;
            }
        }

        protected static IEnumerable<MonsterBase> OverlapBoxMonsters(Vector2 center, Vector2 size, float angle)
        {
            var hits = UnityEngine.Physics2D.OverlapBoxAll(center, size, angle);
            foreach (var h in hits)
            {
                var m = h.GetComponent<MonsterBase>();
                if (m != null && !m.IsDead) yield return m;
            }
        }

        // ── 데미지 계산 ──────────────────────────────────────

        protected int GetPlayerLevel()
        {
            return LevelSystem.Instance != null ? LevelSystem.Instance.Level : 1;
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

        /// <summary>궁극기 등 공격력 비율 기반 데미지 계산. percentOfBase + perLvPercent × Lv.</summary>
        protected DamageResult ComputeUltimateDamage(MonsterBase target, float basePercent, float perLvPercent)
        {
            int playerLevel = GetPlayerLevel();
            var ctx = DamageContext.Default(playerLevel, data != null ? data.damageType : DamageType.Physical);
            ctx.TargetDefense = target.Data.defense;
            ctx.TargetMagicResist = target.Data.magicResist;

            // 궁극기 배율 = base + perLv × (level)
            float ultMult = basePercent + perLvPercent * level;
            ctx.MultiplierFactors = new[] { ultMult };

            return DamageCalculator.Calculate(ctx);
        }

        // ── 광역 + 넉백 일괄 처리 ───────────────────────────

        /// <summary>원형 광역. 반경 내 몬스터에게 데미지 적용 + 넉백.</summary>
        protected int SpawnAOECircle(Vector2 center, float radius, float basePercent, float perLvPercent, KnockbackTier knockbackTier, float knockbackDistance, bool isUltimate)
        {
            int hits = 0;
            foreach (var m in OverlapCircleMonsters(center, radius))
            {
                var dmg = ComputeUltimateDamage(m, basePercent, perLvPercent);
                m.ApplyDamage(dmg);
                hits++;

                if (knockbackDistance > 0f && m.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 dir = ((Vector2)m.transform.position - center).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
                    KnockbackSystem.Apply(rb, dir, knockbackDistance, knockbackTier, isUltimate);
                }
            }
            return hits;
        }

        /// <summary>사각형 광역. 박스 내 몬스터에게 데미지 적용.</summary>
        protected int SpawnAOEBox(Vector2 center, Vector2 size, float angle, float basePercent, float perLvPercent, KnockbackTier knockbackTier, float knockbackDistance, bool isUltimate)
        {
            int hits = 0;
            foreach (var m in OverlapBoxMonsters(center, size, angle))
            {
                var dmg = ComputeUltimateDamage(m, basePercent, perLvPercent);
                m.ApplyDamage(dmg);
                hits++;

                if (knockbackDistance > 0f && m.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    Vector2 dir = ((Vector2)m.transform.position - center).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
                    KnockbackSystem.Apply(rb, dir, knockbackDistance, knockbackTier, isUltimate);
                }
            }
            return hits;
        }

        /// <summary>스킬 발동 로그. 검증용.</summary>
        protected void LogCast(int hits, float totalDamage = 0f, float knockbackDist = 0f)
        {
            string skillName = data != null ? data.displayName : GetType().Name;
            Debug.Log($"[Skill] {skillName} Lv.{level} dealt total {totalDamage:F1} → {hits} targets, knockback={knockbackDist:F1}u");
        }

        protected Vector2 FindBossOrNearestMonster(Vector2 fallbackPos)
        {
            // 보스 태그 우선
            var bosses = GameObject.FindGameObjectsWithTag(Constants.TagBoss);
            if (bosses != null && bosses.Length > 0)
            {
                return bosses[0].transform.position;
            }
            // 가장 가까운 몬스터
            var monsters = FindObjectsByType<MonsterBase>(FindObjectsSortMode.None);
            if (monsters != null && monsters.Length > 0)
            {
                MonsterBase nearest = null;
                float bestSq = float.MaxValue;
                foreach (var m in monsters)
                {
                    if (m == null || m.IsDead) continue;
                    float sq = ((Vector2)m.transform.position - fallbackPos).sqrMagnitude;
                    if (sq < bestSq) { bestSq = sq; nearest = m; }
                }
                if (nearest != null) return nearest.transform.position;
            }
            return fallbackPos;
        }
    }
}
