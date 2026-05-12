using NUnit.Framework;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;

namespace RPGPinball.Tests.EditMode
{
    public class StageTimerTests
    {
        private GameObject hostGO;
        private StageTimer timer;

        [SetUp]
        public void SetUp()
        {
            hostGO = new GameObject("StageTimer_Host");
            timer = hostGO.AddComponent<StageTimer>();
            // EditMode에서는 Awake/OnEnable이 보장되지 않으므로 명시적으로 초기화.
            timer.ResetTimer(Constants.StageDefaultTime);
        }

        [TearDown]
        public void TearDown()
        {
            if (hostGO != null) Object.DestroyImmediate(hostGO);
        }

        [Test]
        public void Initial_TimerHasDefaultDuration()
        {
            Assert.AreEqual(Constants.StageDefaultTime, timer.Remaining, 0.01f);
        }

        [Test]
        public void Penalize_DeductsTime()
        {
            timer.Penalize(15f);
            Assert.AreEqual(Constants.StageDefaultTime - 15f, timer.Remaining, 0.01f);
        }

        [Test]
        public void Penalize_DoesNotGoNegative()
        {
            timer.Penalize(999f);
            Assert.AreEqual(0f, timer.Remaining, 0.01f);
        }

        [Test]
        public void AddTime_RespectsCap()
        {
            // 처음에 30초 페널티 적용 → 150초
            timer.Penalize(30f);
            // 누적 +20 × 3 = 60 (상한)
            timer.AddTime(20f);
            timer.AddTime(20f);
            timer.AddTime(20f);
            // 상한 도달 — 추가 회복 거부
            timer.AddTime(10f);
            // 150 + 60 = 210 (180을 넘어도 회복 상한이 누적 기준이므로 가능)
            Assert.AreEqual(210f, timer.Remaining, 0.01f);
        }

        [Test]
        public void AddTime_IgnoresZeroOrNegative()
        {
            float before = timer.Remaining;
            timer.AddTime(0f);
            timer.AddTime(-5f);
            Assert.AreEqual(before, timer.Remaining, 0.01f);
        }
    }
}
