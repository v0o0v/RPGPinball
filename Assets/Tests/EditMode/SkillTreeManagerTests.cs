using NUnit.Framework;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Tests.EditMode
{
    public class SkillTreeManagerTests
    {
        private GameObject hostGO;
        private SkillTreeManager stm;
        private LevelSystem ls;

        private SkillData MakeSkill(int id, SkillType type, int tier, int maxLv, int[] prereq)
        {
            var s = ScriptableObject.CreateInstance<SkillData>();
            s.id = id; s.type = type; s.tier = tier; s.maxLevel = maxLv;
            s.prerequisiteIds = prereq ?? System.Array.Empty<int>();
            s.prerequisiteMinLevel = 1;
            s.damageType = DamageType.Physical;
            return s;
        }

        [SetUp]
        public void SetUp()
        {
            hostGO = new GameObject("SkillTree_Host");
            ls = hostGO.AddComponent<LevelSystem>();
            stm = hostGO.AddComponent<SkillTreeManager>();
            ls.DebugSetSP(100, 0);
        }

        [TearDown]
        public void TearDown()
        {
            if (hostGO != null) Object.DestroyImmediate(hostGO);
        }

        [Test]
        public void Invest_T1_Succeeds()
        {
            var s = MakeSkill(101, SkillType.Passive, 1, 5, null);
            stm.DebugRegisterSkill(s);
            bool ok = stm.Invest(101);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, stm.GetLevel(101));
        }

        [Test]
        public void Invest_MaxLevel_ReturnsFalse()
        {
            var s = MakeSkill(101, SkillType.Passive, 1, 2, null);
            stm.DebugRegisterSkill(s);
            stm.Invest(101);
            stm.Invest(101);
            bool ok = stm.Invest(101);
            Assert.IsFalse(ok);
            Assert.AreEqual(2, stm.GetLevel(101));
        }

        [Test]
        public void Invest_MissingPrerequisite_ReturnsFalse()
        {
            var s1 = MakeSkill(101, SkillType.Passive, 1, 5, null);
            var s2 = MakeSkill(104, SkillType.Passive, 2, 5, new[] { 101 });
            stm.DebugRegisterSkill(s1);
            stm.DebugRegisterSkill(s2);
            bool ok = stm.Invest(104);
            Assert.IsFalse(ok);
        }

        [Test]
        public void Invest_PrerequisiteUnlocked_Succeeds()
        {
            var s1 = MakeSkill(101, SkillType.Passive, 1, 5, null);
            var s2 = MakeSkill(104, SkillType.Passive, 2, 5, new[] { 101 });
            stm.DebugRegisterSkill(s1);
            stm.DebugRegisterSkill(s2);
            stm.Invest(101);
            bool ok = stm.Invest(104);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, stm.GetLevel(104));
        }

        [Test]
        public void ResetAll_ClearsAllLevels()
        {
            var s = MakeSkill(101, SkillType.Passive, 1, 5, null);
            stm.DebugRegisterSkill(s);
            stm.Invest(101);
            stm.Invest(101);
            stm.ResetAll();
            Assert.AreEqual(0, stm.GetLevel(101));
        }
    }
}
