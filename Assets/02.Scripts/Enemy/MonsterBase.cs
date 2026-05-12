using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Security;

namespace RPGPinball.Enemy
{
    /// <summary>
    /// 몬스터 기본 클래스. 공 충돌 시 DamageCalculator로 데미지 계산 → HP 감소 → 처치 처리.
    /// 마일스톤 4에서 BossAI/EliteAI로 상속 확장.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MonsterBase : MonoBehaviour
    {
        [SerializeField] private MonsterData data;
        [SerializeField] private int playerLevelOverride = 1; // 테스트용. 마일스톤 3에서 PlayerData 연동

        private SafeInt hp;
        public bool IsDead { get; private set; }

        public MonsterData Data => data;
        public int Hp => hp.Value;

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

        protected virtual void OnCollisionEnter2D(Collision2D col)
        {
            if (IsDead) return;
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;

            var ctx = DamageContext.Default(playerLevelOverride, DamageType.Physical);
            ctx.TargetDefense = data.defense;
            ctx.TargetMagicResist = data.magicResist;

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
            gameObject.SetActive(false);
        }
    }
}
