using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Security;

namespace RPGPinball.Enemy
{
    /// <summary>
    /// 몬스터 기본 클래스. 공 충돌 시 DamageCalculator로 데미지 계산 → HP 감소 → 처치 처리.
    /// 마일스톤 3: 상태이상(둔화/스턴/화상), 처치 시 XP 지급, 크로노 코어 시간 회복 훅 추가.
    /// 마일스톤 4에서 BossAI/EliteAI로 상속 확장.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MonsterBase : MonoBehaviour
    {
        [SerializeField] private MonsterData data;
        [SerializeField] private int playerLevelOverride = 1; // 테스트용. LevelSystem이 있으면 그쪽이 우선.

        [Header("넉백 등급 (마일스톤 4 보스에서 오버라이드)")]
        [SerializeField] private KnockbackTier knockbackTier = KnockbackTier.None;

        private SafeInt hp;
        public bool IsDead { get; private set; }

        // ── 상태이상 ──
        private float slowEndTime;
        private float slowPercent;
        private float stunEndTime;
        private float burnEndTime;
        private int burnStacks;
        private float burnDamagePerTick;
        private float nextBurnTick;

        public MonsterData Data => data;
        public int Hp => hp.Value;
        public float CurrentHpRatio => data != null && data.maxHp > 0 ? Mathf.Clamp01((float)hp.Value / data.maxHp) : 0f;
        public bool IsSlowed => Time.time < slowEndTime;
        public bool IsStunned => Time.time < stunEndTime;
        public bool IsBurning => Time.time < burnEndTime;
        public KnockbackTier KnockbackTier => knockbackTier;
        // 보스가 페이즈별 DEF 주입할 수 있도록 가상 메서드로 노출
        public virtual int GetEffectiveDefense() { return data != null ? data.defense : 0; }
        public virtual int GetEffectiveMagicResist() { return data != null ? data.magicResist : 0; }

        protected virtual void Awake()
        {
            if (data == null)
            {
                Debug.LogError($"[MonsterBase] MonsterData가 비어있음: {name}", this);
                enabled = false;
                return;
            }
            hp = SafeInt.Create(data.maxHp);
            gameObject.tag = data.isBoss ? Constants.TagBoss : Constants.TagMonster;
        }

        protected virtual void Update()
        {
            if (IsDead) return;
            // 화상 도트
            if (IsBurning && Time.time >= nextBurnTick)
            {
                int dmg = Mathf.Max(1, Mathf.RoundToInt(burnDamagePerTick * burnStacks));
                hp = SafeInt.Create(hp.Value - dmg);
                EventBus.Publish(new OnDamageDealt
                {
                    Target = gameObject,
                    Damage = dmg,
                    IsCritical = false,
                    IsMagic = true
                });
                nextBurnTick = Time.time + 1f;
                if (hp.Value <= 0) Die();
            }
        }

        protected virtual void OnCollisionEnter2D(Collision2D col)
        {
            if (IsDead) return;
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;

            int playerLv = LevelSystem.Instance != null ? LevelSystem.Instance.Level : playerLevelOverride;
            var ctx = DamageContext.Default(playerLv, DamageType.Physical);
            ctx.TargetDefense = GetEffectiveDefense();
            ctx.TargetMagicResist = GetEffectiveMagicResist();
            ctx.BallSpeed = col.relativeVelocity.magnitude;
            ctx.TargetCurrentHpRatio = CurrentHpRatio;

            // SkillTreeManager 패시브 주입
            if (SkillTreeManager.Instance != null)
            {
                var stm = SkillTreeManager.Instance;
                ctx.AdditivePercents = new[] { stm.GetDamageAddPercent(DamageType.Physical) };
                ctx.MultiplierFactors = stm.GetDamageMultiplierFactors(DamageType.Physical);
                ctx.ComboStrikePerStack = stm.GetComboStrikePerStack();
                ctx.ComboMaxStack = stm.GetMaxComboStack();
                ctx.FuryStrikeBonus = stm.GetFuryStrikeBonus();
                ctx.InertiaBreakFactor = stm.GetInertiaBreak1Factor();
                ctx.FlipperSmashFactor = stm.GetFlipperSmashFactor();
                ctx.CritChanceBonus = stm.GetCritChanceBonus();
                ctx.ArmorReductionPercent = stm.GetArmorReductionPercent();
            }
            if (ComboSystem.Instance != null)
            {
                ctx.ComboCount = ComboSystem.Instance.Combo;
            }
            // BallController에서 재질 / 분열 플래그 가져오기
            var ball = col.gameObject.GetComponent<Physics.BallController>();
            if (ball != null)
            {
                ctx.BallMaterial = ball.Material;
                ctx.IsMithrilBall = ball.Material == BallMaterial.Mithril;
                ctx.IsSplitBall = ball.IsSplitBall;
            }

            var result = DamageCalculator.Calculate(ctx);
            ApplyDamage(result);
        }

        public void ApplyDamage(DamageResult result)
        {
            if (IsDead) return;
            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(result.FinalDamage));
            hp = SafeInt.Create(hp.Value - dmgInt);

            EventBus.Publish(new OnDamageDealt
            {
                Target = gameObject,
                Damage = result.FinalDamage,
                IsCritical = result.IsCritical,
                IsMagic = result.DamageType == DamageType.Magic
            });

            // 콤보 +1
            if (ComboSystem.Instance != null) ComboSystem.Instance.RegisterHit();

            if (hp.Value <= 0) Die();
        }

        // ── 상태이상 적용 (스킬에서 호출) ─────────────────────

        public void ApplySlowDebuff(float percent, float duration)
        {
            slowPercent = Mathf.Clamp01(percent);
            slowEndTime = Time.time + duration;
        }

        public void ApplyStun(float duration)
        {
            stunEndTime = Time.time + duration;
        }

        public void ApplyBurn(float damagePerTick, float duration, int maxStacks)
        {
            burnDamagePerTick = damagePerTick;
            burnEndTime = Time.time + duration;
            burnStacks = Mathf.Clamp(burnStacks + 1, 1, Mathf.Max(1, maxStacks));
            if (nextBurnTick <= Time.time) nextBurnTick = Time.time + 1f;
        }

        /// <summary>아마겟돈 등에서 무작위 상태이상 부여.</summary>
        public void ApplyRandomStatus()
        {
            int pick = UnityEngine.Random.Range(0, 3);
            switch (pick)
            {
                case 0: ApplyBurn(2f, 3f, 3); break;
                case 1: ApplySlowDebuff(0.5f, 2f); break;
                case 2: ApplyStun(0.5f); break;
            }
        }

        // ── 처치 ────────────────────────────────────────────

        protected virtual void Die()
        {
            IsDead = true;
            EventBus.Publish(new OnMonsterKilled
            {
                Monster = gameObject,
                XpReward = data.xpReward,
                GoldReward = data.goldReward,
                IsBoss = data.isBoss
            });

            // XP 지급
            if (LevelSystem.Instance != null && data.xpReward > 0)
            {
                LevelSystem.Instance.GainXP(data.xpReward, data.level > 0 ? data.level : 1);
            }

            // 크로노 코어 / 타임 브레이커 시간 회복 (장애물 파괴 = 몬스터 처치로 간주)
            if (StageTimer.Instance != null && SkillTreeManager.Instance != null)
            {
                float bonus = SkillTreeManager.Instance.GetTimeRecoverPerKill();
                if (bonus > 0f) StageTimer.Instance.AddTime(bonus);
            }

            gameObject.SetActive(false);
        }
    }
}
