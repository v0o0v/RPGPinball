using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy;

namespace RPGPinball.Tests.EditMode
{
    public class MonsterHealTests
    {
        private GameObject host;
        private MonsterBase mb;
        private MonsterData md;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("Monster");
            host.AddComponent<BoxCollider2D>();
            mb = host.AddComponent<MonsterBase>();
            md = ScriptableObject.CreateInstance<MonsterData>();
            md.maxHp = 1000;
            md.defense = 0;
            md.magicResist = 0;
            md.isBoss = false;
            mb.InjectData(md);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (md != null) Object.DestroyImmediate(md);
        }

        [Test]
        public void Heal_IncreasesHp()
        {
            // 데미지 적용으로 HP 감소시킨 후 회복 검증
            mb.ApplyDamage(new RPGPinball.Combat.DamageResult { FinalDamage = 500 });
            int hpBefore = mb.Hp;
            mb.Heal(200, HealSource.BossPhotosynthesis);
            Assert.That(mb.Hp, Is.EqualTo(hpBefore + 200));
        }

        [Test]
        public void Heal_ClampedToMaxHp()
        {
            mb.ApplyDamage(new RPGPinball.Combat.DamageResult { FinalDamage = 100 });
            mb.Heal(99999, HealSource.LeviathanSelfHeal);
            Assert.AreEqual(md.maxHp, mb.Hp);
        }

        [Test]
        public void Heal_PublishesEvent()
        {
            int captured = -1;
            HealSource src = HealSource.None;
            System.Action<OnMonsterHealed> handler = e =>
            {
                captured = e.Amount;
                src = e.Source;
            };
            EventBus.Subscribe<OnMonsterHealed>(handler);
            try
            {
                mb.ApplyDamage(new RPGPinball.Combat.DamageResult { FinalDamage = 300 });
                mb.Heal(50, HealSource.KrakenTentacleRegen);
                Assert.AreEqual(50, captured);
                Assert.AreEqual(HealSource.KrakenTentacleRegen, src);
            }
            finally
            {
                EventBus.Unsubscribe<OnMonsterHealed>(handler);
            }
        }

        [Test]
        public void Heal_DoesNothingIfDead()
        {
            mb.ApplyDamage(new RPGPinball.Combat.DamageResult { FinalDamage = 99999 });
            Assert.IsTrue(mb.IsDead);
            int hpBefore = mb.Hp;
            mb.Heal(1000, HealSource.Other);
            Assert.AreEqual(hpBefore, mb.Hp);
        }
    }
}
