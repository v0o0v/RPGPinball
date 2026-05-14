using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    public class BountyEntryTests
    {
        private GameObject qmHost;
        private GameObject tvHost;
        private QuestManager qm;
        private TavernManager tv;

        [SetUp]
        public void SetUp()
        {
            TestSingletonReset.ClearAllManagers();
            qmHost = new GameObject("QM");
            qm = qmHost.AddComponent<QuestManager>();
            qm.InitializeForTest();
            tvHost = new GameObject("TV");
            tv = tvHost.AddComponent<TavernManager>();
            tv.InitializeForTest();

            var bounty = ScriptableObject.CreateInstance<QuestData>();
            bounty.questId = "bounty_storm_elemental";
            bounty.kind = QuestKind.Bounty;
            bounty.requiredActBossDefeated = BossId.Act1_WorldTreeSpirit;
            bounty.bountyTargetEliteId = EliteId.StormElemental;

            typeof(QuestManager).GetField("bountyPool",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(qm, new[] { bounty });

            qm.SetSeed(42);
            qm.RefreshBountyIfExpired();
        }

        [TearDown]
        public void TearDown()
        {
            if (qmHost != null) Object.DestroyImmediate(qmHost);
            if (tvHost != null) Object.DestroyImmediate(tvHost);
        }

        [Test]
        public void EnterBounty_BlockedIfActBossNotDefeated()
        {
            var bounty = qm.BountyTargets[0];
            Assert.IsFalse(tv.EnterBountyStage(bounty));
        }

        [Test]
        public void EnterBounty_OkAfterActBossDefeated()
        {
            // TavernManager.defeatedBosses 에 직접 추가 (EventBus 누적 회피)
            var f = typeof(TavernManager).GetField("defeatedBosses",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = f.GetValue(tv) as System.Collections.Generic.List<BossId>;
            list.Add(BossId.Act1_WorldTreeSpirit);

            var bounty = qm.BountyTargets[0];
            Assert.IsTrue(tv.EnterBountyStage(bounty));
        }
    }
}
