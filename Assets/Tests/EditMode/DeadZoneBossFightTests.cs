using NUnit.Framework;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// BossFightContext.Enter/Exit 토글에 따른 DeadZone 페널티 분기 검증.
    /// 실제 DeadZone Trigger는 PlayMode에서만 호출되므로, 여기서는 컨텍스트 + 상수 검증.
    /// </summary>
    public class DeadZoneBossFightTests
    {
        [SetUp]
        public void SetUp()
        {
            BossFightContext.ForceClear();
        }

        [TearDown]
        public void TearDown()
        {
            BossFightContext.ForceClear();
        }

        [Test]
        public void DefaultContext_IsInactive()
        {
            Assert.IsFalse(BossFightContext.IsActive);
            Assert.AreEqual(BossId.None, BossFightContext.CurrentBossId);
        }

        [Test]
        public void Enter_SetsActiveAndBossId()
        {
            BossFightContext.Enter(null, BossId.Act4_WinterQueen);
            Assert.IsTrue(BossFightContext.IsActive);
            Assert.AreEqual(BossId.Act4_WinterQueen, BossFightContext.CurrentBossId);
        }

        [Test]
        public void Exit_ClearsActive()
        {
            BossFightContext.Enter(null, BossId.Act1_FleshPlant);
            BossFightContext.Exit();
            Assert.IsFalse(BossFightContext.IsActive);
            Assert.AreEqual(BossId.None, BossFightContext.CurrentBossId);
        }

        [Test]
        public void BossPenalty_IsNegative20()
        {
            Assert.AreEqual(-20f, Constants.BossDeadzonePenalty, 0.001f);
        }

        [Test]
        public void NormalPenalty_IsNegative10()
        {
            Assert.AreEqual(-10f, Constants.DeadzonePenalty, 0.001f);
        }

        [Test]
        public void EnragedRecoveryMul_Is07x()
        {
            Assert.AreEqual(0.7f, Constants.BossEnragedRecoveryMul, 0.001f);
        }
    }
}
