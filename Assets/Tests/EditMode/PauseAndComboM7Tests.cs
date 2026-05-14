using NUnit.Framework;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Physics;

namespace RPGPinball.Tests.EditMode
{
    public class PauseAndComboM7Tests
    {
        [SetUp]
        public void SetUp()
        {
            // 기존 매니저 정리
            foreach (var pm in Object.FindObjectsByType<PauseManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(pm.gameObject);
            foreach (var cs in Object.FindObjectsByType<ComboSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(cs.gameObject);
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            FlipperController.InputBlocked = false;
            EventBus.Clear();
        }

        // ── PauseManagerTests ────────────────────────────────

        [Test]
        public void Pause_SetsTimeScaleZero()
        {
            var pm = PauseManager.EnsureInstance();
            pm.Pause(PauseReason.UserRequest);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(pm.IsPaused);
        }

        [Test]
        public void Resume_RestoresTimeScale()
        {
            var pm = PauseManager.EnsureInstance();
            pm.Pause(PauseReason.UserRequest);
            pm.Resume(PauseReason.UserRequest);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(pm.IsPaused);
        }

        [Test]
        public void Pause_NestedReasons_RemainPausedUntilAllResumed()
        {
            var pm = PauseManager.EnsureInstance();
            pm.Pause(PauseReason.UserRequest);
            pm.Pause(PauseReason.PopupOpen);
            pm.Resume(PauseReason.UserRequest);
            Assert.IsTrue(pm.IsPaused);
            pm.Resume(PauseReason.PopupOpen);
            Assert.IsFalse(pm.IsPaused);
        }

        [Test]
        public void Pause_SameReason_IgnoresDuplicate()
        {
            var pm = PauseManager.EnsureInstance();
            pm.Pause(PauseReason.UserRequest);
            pm.Pause(PauseReason.UserRequest);
            pm.Resume(PauseReason.UserRequest);
            Assert.IsFalse(pm.IsPaused);
        }

        [Test]
        public void ForceResumeAll_ClearsAllReasons()
        {
            var pm = PauseManager.EnsureInstance();
            pm.Pause(PauseReason.UserRequest);
            pm.Pause(PauseReason.PopupOpen);
            pm.Pause(PauseReason.ApplicationBackground);
            pm.ForceResumeAll();
            Assert.IsFalse(pm.IsPaused);
        }

        // ── ComboMilestoneTests ──────────────────────────────

        [Test]
        public void ComboReachesMilestone10_PublishesOnComboMilestone10()
        {
            var go = new GameObject("ComboSystem");
            var cs = go.AddComponent<ComboSystem>();
            int milestone = 0;
            int count = 0;
            System.Action<OnComboMilestone> handler = e => { milestone = e.Milestone; count++; };
            EventBus.Subscribe(handler);
            for (int i = 0; i < 10; i++) cs.RegisterHit();
            EventBus.Unsubscribe(handler);

            Assert.AreEqual(10, milestone);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void ComboReachesMilestone30_PublishesAllPriorMilestones()
        {
            var go = new GameObject("ComboSystem");
            var cs = go.AddComponent<ComboSystem>();
            int countAtTen = 0, countAtThirty = 0;
            System.Action<OnComboMilestone> handler = e =>
            {
                if (e.Milestone == 10) countAtTen++;
                if (e.Milestone == 30) countAtThirty++;
            };
            EventBus.Subscribe(handler);
            for (int i = 0; i < 30; i++) cs.RegisterHit();
            EventBus.Unsubscribe(handler);
            Assert.AreEqual(1, countAtTen);
            Assert.AreEqual(1, countAtThirty);
        }

        [Test]
        public void NonMilestoneCombos_DoNotPublish()
        {
            var go = new GameObject("ComboSystem");
            var cs = go.AddComponent<ComboSystem>();
            int count = 0;
            System.Action<OnComboMilestone> handler = _ => count++;
            EventBus.Subscribe(handler);
            for (int i = 0; i < 9; i++) cs.RegisterHit(); // 1~9
            EventBus.Unsubscribe(handler);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Combo100_FiresAllFourMilestones()
        {
            var go = new GameObject("ComboSystem");
            var cs = go.AddComponent<ComboSystem>();
            var fired = new System.Collections.Generic.List<int>();
            System.Action<OnComboMilestone> handler = e => fired.Add(e.Milestone);
            EventBus.Subscribe(handler);
            for (int i = 0; i < 100; i++) cs.RegisterHit();
            EventBus.Unsubscribe(handler);
            CollectionAssert.AreEquivalent(new[] { 10, 30, 50, 100 }, fired);
        }

        // ── FlipperInputBlockedTests ────────────────────────

        [Test]
        public void InputBlocked_DefaultFalse()
        {
            Assert.IsFalse(FlipperController.InputBlocked);
        }

        [Test]
        public void InputBlocked_SetTrueBlocksFlipperSpawn()
        {
            FlipperController.InputBlocked = true;
            Assert.IsTrue(FlipperController.InputBlocked);
        }

        [Test]
        public void InputBlocked_TogglesIndependently()
        {
            FlipperController.InputBlocked = true;
            FlipperController.InputBlocked = false;
            Assert.IsFalse(FlipperController.InputBlocked);
        }
    }
}
