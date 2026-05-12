using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Meta;
using RPGPinball.Data;

namespace RPGPinball.Tests.EditMode
{
    public class LevelSystemTests
    {
        private GameObject hostGO;
        private LevelSystem sys;

        [SetUp]
        public void SetUp()
        {
            hostGO = new GameObject("LevelSystem_Host");
            sys = hostGO.AddComponent<LevelSystem>();
            // Awake에서 PlayerData 자동 생성됨
        }

        [TearDown]
        public void TearDown()
        {
            if (hostGO != null) Object.DestroyImmediate(hostGO);
        }

        // ── RequiredXP 공식 검증 ──
        [Test]
        public void RequiredXP_Lv1_FormulaMatches()
        {
            // 80 + 1*12 + 1*0.5 = 92.5 → Mathf.RoundToInt는 IEEE 754 짝수 반올림(banker's rounding) 사용
            // 92.5 → 92 (가장 가까운 짝수)
            int expected = Mathf.RoundToInt(80f + 1f * 12f + 1f * 1f * 0.5f);
            Assert.AreEqual(expected, LevelSystem.RequiredXP(1));
        }

        [Test]
        public void RequiredXP_Lv50_FormulaMatches()
        {
            // 80 + 50*12 + 50*50*0.5 = 80 + 600 + 1250 = 1930
            Assert.AreEqual(1930, LevelSystem.RequiredXP(50));
        }

        [Test]
        public void RequiredXP_Lv99_FormulaMatches()
        {
            // 80 + 99*12 + 99*99*0.5 = 80 + 1188 + 4900.5 = 6168.5 → 6169 (Mathf.RoundToInt 반올림)
            int expected = Mathf.RoundToInt(80f + 99f * 12f + 99f * 99f * 0.5f);
            Assert.AreEqual(expected, LevelSystem.RequiredXP(99));
        }

        // ── 오버레벨링 페널티 ──
        [Test]
        public void OverlevelPenalty_DiffWithin5_NoReduction()
        {
            sys.DebugSetLevel(10);
            int xp = sys.ApplyOverlevelPenalty(100, 8); // diff=2 (<5)
            Assert.AreEqual(100, xp);
        }

        [Test]
        public void OverlevelPenalty_DiffOver5_HalfReduction()
        {
            sys.DebugSetLevel(10);
            int xp = sys.ApplyOverlevelPenalty(100, 3); // diff=7 (>5)
            Assert.AreEqual(50, xp);
        }

        [Test]
        public void OverlevelPenalty_DiffOver10_HeavyReduction()
        {
            sys.DebugSetLevel(20);
            int xp = sys.ApplyOverlevelPenalty(100, 3); // diff=17 (>10)
            Assert.AreEqual(20, xp);
        }

        // ── XP 획득 / 레벨업 ──
        [Test]
        public void GainXP_BelowRequired_NoLevelUp()
        {
            sys.DebugSetLevel(1);
            sys.GainXP(50, 1);
            Assert.AreEqual(1, sys.Level);
            Assert.AreEqual(50, sys.CurrentXP);
        }

        [Test]
        public void GainXP_AboveRequired_LevelsUp()
        {
            sys.DebugSetLevel(1);
            int req = LevelSystem.RequiredXP(1);
            sys.GainXP(req + 7, 1);
            Assert.AreEqual(2, sys.Level);
            Assert.AreEqual(7, sys.CurrentXP);
        }

        [Test]
        public void GainXP_LargeAmount_MultipleLevelUps()
        {
            sys.DebugSetLevel(1);
            sys.GainXP(500, 1); // Lv.1 → 2 (93), Lv.2 → 3 (107), Lv.3 → 4 (122), ...
            Assert.Greater(sys.Level, 2);
        }

        [Test]
        public void GainXP_AtLevelCap_NoFurtherLevel()
        {
            sys.DebugSetLevel(Constants.LevelCap);
            sys.GainXP(99999, 1);
            Assert.AreEqual(Constants.LevelCap, sys.Level);
            Assert.AreEqual(0, sys.CurrentXP);
        }

        // ── SP 보상 ──
        [Test]
        public void LevelUp_GrantsOneSP()
        {
            sys.DebugSetLevel(1);
            sys.DebugSetSP(0, 0);
            sys.GainXP(100, 1);
            Assert.AreEqual(1, sys.TotalSP);
        }

        [Test]
        public void AwardBossSP_AddsOne()
        {
            sys.DebugSetSP(10, 0);
            sys.AwardBossSP();
            Assert.AreEqual(11, sys.TotalSP);
        }

        [Test]
        public void AwardActClearSP_AddsFive()
        {
            sys.DebugSetSP(10, 0);
            sys.AwardActClearSP();
            Assert.AreEqual(15, sys.TotalSP);
        }

        // ── SP 소비/환원 ──
        [Test]
        public void TryConsumeSP_Success_DecreasesAvailable()
        {
            sys.DebugSetSP(5, 0);
            bool ok = sys.TryConsumeSP(3);
            Assert.IsTrue(ok);
            Assert.AreEqual(3, sys.UsedSP);
            Assert.AreEqual(2, sys.AvailableSP);
        }

        [Test]
        public void TryConsumeSP_Insufficient_ReturnsFalse()
        {
            sys.DebugSetSP(2, 0);
            bool ok = sys.TryConsumeSP(5);
            Assert.IsFalse(ok);
            Assert.AreEqual(0, sys.UsedSP);
        }

        [Test]
        public void RefundAllSP_ResetsUsed()
        {
            sys.DebugSetSP(10, 4);
            sys.RefundAllSP();
            Assert.AreEqual(0, sys.UsedSP);
            Assert.AreEqual(10, sys.AvailableSP);
        }
    }
}
