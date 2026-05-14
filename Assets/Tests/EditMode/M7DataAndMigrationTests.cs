using System.IO;
using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Security;
using RPGPinball.UI;

namespace RPGPinball.Tests.EditMode
{
    public class M7DataAndMigrationTests
    {
        private string tempPath;

        [SetUp]
        public void SetUp()
        {
            tempPath = Path.Combine(Application.persistentDataPath, $"test_m7_{System.Guid.NewGuid():N}.dat");
            SaveSystem.ResetForTest(new DebugSaltProvider("m7_test"), tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            // PlayerPrefs 정리
            SaveMigrationV1ToV2.DeleteLegacyPrefs();
        }

        // ── SaveMigrationV1ToV2Tests ────────────────────────

        [Test]
        public void HasLegacyPrefs_FalseWhenEmpty()
        {
            Assert.IsFalse(SaveMigrationV1ToV2.HasLegacyPrefs());
        }

        [Test]
        public void HasLegacyPrefs_TrueAfterSettingGold()
        {
            PlayerPrefs.SetInt("Economy.Gold", 100);
            Assert.IsTrue(SaveMigrationV1ToV2.HasLegacyPrefs());
        }

        [Test]
        public void Migrate_ExtractsAllLegacyFields()
        {
            PlayerPrefs.SetInt("Economy.Gold", 5000);
            PlayerPrefs.SetInt("Economy.ManaCrystal", 200);
            PlayerPrefs.SetInt("Economy.BossSoul", 10);
            PlayerPrefs.SetInt("Forge.Material", 2);
            PlayerPrefs.SetInt("Forge.FlipperLevel", 7);
            PlayerPrefs.SetInt("Astrologer.PullCount", 15);
            PlayerPrefs.SetInt("Balloon.UpgradeLevel", 2);

            var data = SaveMigrationV1ToV2.Migrate();
            Assert.AreEqual(5000, data.player.gold);
            Assert.AreEqual(200, data.player.manaCrystal);
            Assert.AreEqual(10, data.player.bossSoul);
            Assert.AreEqual(2, data.inventory.equippedBallMaterial);
            Assert.AreEqual(7, data.inventory.equippedFlipperLevel);
            Assert.AreEqual(15, data.village.astrologer.totalPullCount);
            Assert.AreEqual(2, data.village.balloon.upgradeLevel);
        }

        [Test]
        public void TryMigrateFromV1_ConsumesLegacyPrefs()
        {
            PlayerPrefs.SetInt("Economy.Gold", 1234);
            bool migrated = SaveSystem.Instance.TryMigrateFromV1();
            Assert.IsTrue(migrated);
            Assert.IsFalse(SaveMigrationV1ToV2.HasLegacyPrefs());
            Assert.IsTrue(SaveSystem.Instance.HasSave());
            SaveSystem.Instance.Load(out var loaded);
            Assert.AreEqual(1234, loaded.player.gold);
        }

        [Test]
        public void TryMigrateFromV1_SkipsWhenSaveExists()
        {
            SaveSystem.Instance.SaveImmediate(SaveData.CreateDefault());
            PlayerPrefs.SetInt("Economy.Gold", 9999);
            bool migrated = SaveSystem.Instance.TryMigrateFromV1();
            Assert.IsFalse(migrated);
        }

        // ── RuntimeStageSnapshotRoundTripTests ───────────────

        [Test]
        public void RuntimeStageSnapshot_JsonRoundTrip_PreservesFields()
        {
            var snap = new RuntimeStageSnapshot
            {
                actId = 2,
                stageIndex = 17,
                seed = 12345678UL,
                remainingTimeSec = 78.5f,
                manaGauge = 65,
                comboCount = 23,
                stageGrade = "A",
                continueCount = 1
            };
            snap.ballState = new BallSnapshot
            {
                position = new Vector2(1.5f, -2.0f),
                velocity = new Vector2(10f, 5f),
                angularVelocity = 200f,
                materialId = 1
            };
            var json = JsonUtility.ToJson(snap);
            var restored = JsonUtility.FromJson<RuntimeStageSnapshot>(json);
            Assert.AreEqual(2, restored.actId);
            Assert.AreEqual(17, restored.stageIndex);
            Assert.AreEqual(78.5f, restored.remainingTimeSec, 0.001f);
            Assert.AreEqual(65, restored.manaGauge);
            Assert.AreEqual(23, restored.comboCount);
            Assert.AreEqual("A", restored.stageGrade);
            Assert.AreEqual(1, restored.continueCount);
            Assert.AreEqual(1.5f, restored.ballState.position.x, 0.001f);
            Assert.AreEqual(10f, restored.ballState.velocity.x, 0.001f);
            Assert.AreEqual(200f, restored.ballState.angularVelocity, 0.001f);
        }

        [Test]
        public void RuntimeStageSnapshot_EmptyDefault_PassesJsonRoundTrip()
        {
            var snap = new RuntimeStageSnapshot();
            var json = JsonUtility.ToJson(snap);
            var restored = JsonUtility.FromJson<RuntimeStageSnapshot>(json);
            Assert.AreEqual(0, restored.stageIndex);
            Assert.IsNotNull(restored.multiBalls);
            Assert.IsNotNull(restored.monstersAlive);
        }

        // ── ResultScreen ActProgress 갱신 테스트 ────────────

        [Test]
        public void ActProgress_Apply_AddsNewStageEntry()
        {
            var ap = new ActProgress { actId = 1, unlocked = true };
            ap.Apply(new StageResultContext { cleared = true, stageIndex = 5, grade = "A", clearTimeSec = 100f });
            Assert.AreEqual(1, ap.stages.Count);
            Assert.AreEqual("A", ap.stages[0].bestGrade);
            Assert.AreEqual(100f, ap.stages[0].bestTimeSec, 0.001f);
        }

        [Test]
        public void ActProgress_Apply_UpgradesBestGradeOnly()
        {
            var ap = new ActProgress { actId = 1, unlocked = true };
            ap.Apply(new StageResultContext { cleared = true, stageIndex = 5, grade = "B", clearTimeSec = 150f });
            ap.Apply(new StageResultContext { cleared = true, stageIndex = 5, grade = "S", clearTimeSec = 80f });
            Assert.AreEqual("S", ap.stages[0].bestGrade);
            Assert.AreEqual(80f, ap.stages[0].bestTimeSec, 0.001f);
        }

        [Test]
        public void ActProgress_Apply_DoesNotDowngradeOnSubsequentLowGrade()
        {
            var ap = new ActProgress { actId = 1, unlocked = true };
            ap.Apply(new StageResultContext { cleared = true, stageIndex = 5, grade = "S", clearTimeSec = 80f });
            ap.Apply(new StageResultContext { cleared = true, stageIndex = 5, grade = "C", clearTimeSec = 200f });
            Assert.AreEqual("S", ap.stages[0].bestGrade);
            Assert.AreEqual(80f, ap.stages[0].bestTimeSec, 0.001f);
        }

        // ── MapTilePalette ────────────────────────────────

        [Test]
        public void MapTilePalette_EmptyByDefault_NotFullyMapped()
        {
            var p = ScriptableObject.CreateInstance<MapTilePalette>();
            Assert.IsFalse(p.IsFullyMapped());
            Object.DestroyImmediate(p);
        }

        [Test]
        public void MapTilePalette_GetPack_ReturnsNullForUnmappedAct()
        {
            var p = ScriptableObject.CreateInstance<MapTilePalette>();
            Assert.IsNull(p.GetPack(ActId.Act4_Winter));
            Object.DestroyImmediate(p);
        }

        // ── Constants 검증 ────────────────────────────────

        [Test]
        public void Constants_M7_CameraOrtho_MatchesResolutionSpec()
        {
            Assert.AreEqual(5.625f, Constants.CameraTitleOrtho);
            Assert.AreEqual(5.625f, Constants.CameraResultOrtho);
            Assert.AreEqual(10.0f, Constants.CameraVillageOrtho);
            Assert.AreEqual(10.0f, Constants.CameraActMapOrtho);
        }

        [Test]
        public void Constants_M7_SaveAndContinue_Coherent()
        {
            Assert.AreEqual("2.0.0", Constants.SaveVersion);
            Assert.AreEqual(30f, Constants.ContinueTimeBonusSec);
            Assert.AreEqual(3, Constants.ContinueDailyLimit);
            Assert.AreEqual(100, Constants.ContinueManaRestore);
        }

        // ── PopupManager 기본 동작 ────────────────────────

        [Test]
        public void PopupManager_EnsureInstance_CreatesGameObject()
        {
            // EditMode 에서는 AddComponent 시 Awake 가 호출되지 않으므로
            // EnsureInstance 결과의 GameObject 존재 여부만 검증.
            foreach (var p in Object.FindObjectsByType<PopupManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(p.gameObject);
            var pm = PopupManager.EnsureInstance();
            Assert.IsNotNull(pm);
            Assert.IsNotNull(pm.gameObject);
            Object.DestroyImmediate(pm.gameObject);
        }
    }
}
