using NUnit.Framework;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 4: 보스 페이즈/분노 기본 로직 검증.
    /// 실제 BossBase는 MonoBehaviour라 인스턴스화에 GameObject가 필요하므로,
    /// 여기서는 BossData/EliteData SO 슬롯과 phaseDefenseRatios 값 자체를 검증.
    /// </summary>
    public class BossPhaseTests
    {
        [Test]
        public void BossData_DefaultEnragedHpRatio_Is30Percent()
        {
            var bd = ScriptableObject.CreateInstance<BossData>();
            Assert.AreEqual(0.3f, bd.enragedHpRatio, 0.001f);
        }

        [Test]
        public void EliteData_DefaultEnragedHpRatio_Is25Percent()
        {
            var ed = ScriptableObject.CreateInstance<EliteData>();
            Assert.AreEqual(0.25f, ed.enragedHpRatio, 0.001f);
        }

        [Test]
        public void BossData_PhaseDefenseRatios_DefaultsToPhase1Only()
        {
            var bd = ScriptableObject.CreateInstance<BossData>();
            Assert.AreEqual(1, bd.phaseDefenseRatios.Length);
            Assert.AreEqual(0.2f, bd.phaseDefenseRatios[0], 0.001f);
        }

        [Test]
        public void BossData_DefaultStageTimeLimit_Is180Seconds()
        {
            var bd = ScriptableObject.CreateInstance<BossData>();
            Assert.AreEqual(180f, bd.stageTimeLimit, 0.001f);
        }

        [Test]
        public void EliteData_DefaultStageTimeLimit_Is120Seconds()
        {
            var ed = ScriptableObject.CreateInstance<EliteData>();
            Assert.AreEqual(120f, ed.stageTimeLimit, 0.001f);
        }

        [Test]
        public void EliteData_FrostSentinelDefaults_HasFrontalShield_ArcIs180()
        {
            var ed = ScriptableObject.CreateInstance<EliteData>();
            ed.hasFrontalShield = true;
            Assert.IsTrue(ed.hasFrontalShield);
            Assert.AreEqual(180f, ed.frontalShieldArcDeg, 0.001f);
            Assert.AreEqual(0.15f, ed.backDefenseRatio, 0.001f);
        }

        [Test]
        public void EliteData_LeviathanDefault_LifeStealCap_Is200()
        {
            var ed = ScriptableObject.CreateInstance<EliteData>();
            Assert.AreEqual(200, ed.lifeStealCap);
        }
    }
}
