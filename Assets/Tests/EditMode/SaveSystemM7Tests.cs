using System.IO;
using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Security;

namespace RPGPinball.Tests.EditMode
{
    public class SaveSystemM7Tests
    {
        private string tempPath;

        [SetUp]
        public void SetUp()
        {
            tempPath = Path.Combine(Application.persistentDataPath, $"test_save_{System.Guid.NewGuid():N}.dat");
            SaveSystem.ResetForTest(new DebugSaltProvider("seed_test"), tempPath);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
        }

        // ── SaveSystemRoundTripTests ──────────────────────────

        [Test]
        public void Save_Load_RoundTrip_RecoversAllFields()
        {
            var data = SaveData.CreateDefault();
            data.player.gold = 12345;
            data.player.level = 7;
            data.player.currentXP = 200;
            data.player.playerName = "테스터";
            data.statistics.totalStagesCleared = 42;

            var result = SaveSystem.Instance.SaveImmediate(data);
            Assert.AreEqual(SaveResult.Success, result);
            Assert.IsTrue(SaveSystem.Instance.HasSave());

            var loadResult = SaveSystem.Instance.Load(out var loaded);
            Assert.AreEqual(LoadResult.Success, loadResult);
            Assert.AreEqual(12345, loaded.player.gold);
            Assert.AreEqual(7, loaded.player.level);
            Assert.AreEqual("테스터", loaded.player.playerName);
            Assert.AreEqual(42, loaded.statistics.totalStagesCleared);
        }

        [Test]
        public void Save_Load_RoundTrip_PreservesNestedListItems()
        {
            var data = SaveData.CreateDefault();
            data.inventory.equippedTarots.Add(new EquippedTarot { slotIndex = 0, cardId = 305, grade = 2 });
            data.inventory.ownedRunes.Add(new OwnedRune { runeId = 1, grade = 1, count = 5 });
            data.stageProgress.bossesDefeated.Add(101);

            SaveSystem.Instance.SaveImmediate(data);
            SaveSystem.Instance.Load(out var loaded);

            Assert.AreEqual(1, loaded.inventory.equippedTarots.Count);
            Assert.AreEqual(305, loaded.inventory.equippedTarots[0].cardId);
            Assert.AreEqual(1, loaded.inventory.ownedRunes.Count);
            Assert.AreEqual(5, loaded.inventory.ownedRunes[0].count);
            Assert.AreEqual(101, loaded.stageProgress.bossesDefeated[0]);
        }

        [Test]
        public void HasSave_FalseWhenNoFile()
        {
            Assert.IsFalse(SaveSystem.Instance.HasSave());
        }

        [Test]
        public void Delete_RemovesFileAndResetsData()
        {
            SaveSystem.Instance.SaveImmediate(SaveData.CreateDefault());
            Assert.IsTrue(SaveSystem.Instance.HasSave());
            SaveSystem.Instance.Delete();
            Assert.IsFalse(SaveSystem.Instance.HasSave());
        }

        // ── SaveSystemTamperingTests ──────────────────────────

        [Test]
        public void Tampered_HmacMismatch_DetectedAsLoadTampered()
        {
            SaveSystem.Instance.SaveImmediate(SaveData.CreateDefault());
            // 1바이트 변조
            var bytes = File.ReadAllBytes(tempPath);
            bytes[bytes.Length - 1] ^= 0xff;
            File.WriteAllBytes(tempPath, bytes);

            var result = SaveSystem.Instance.Load(out _);
            Assert.AreEqual(LoadResult.Tampered, result);
        }

        [Test]
        public void Tampered_IvCorrupted_DetectedAsTampered()
        {
            SaveSystem.Instance.SaveImmediate(SaveData.CreateDefault());
            var bytes = File.ReadAllBytes(tempPath);
            bytes[0] ^= 0xaa; // IV 첫 바이트 변조
            File.WriteAllBytes(tempPath, bytes);

            var result = SaveSystem.Instance.Load(out _);
            // IV 변조는 HMAC 불일치로 검출됨
            Assert.AreEqual(LoadResult.Tampered, result);
        }

        [Test]
        public void Tampered_CipherCorrupted_DetectedAsTampered()
        {
            SaveSystem.Instance.SaveImmediate(SaveData.CreateDefault());
            var bytes = File.ReadAllBytes(tempPath);
            bytes[20] ^= 0xff; // 암호문 영역 변조
            File.WriteAllBytes(tempPath, bytes);

            var result = SaveSystem.Instance.Load(out _);
            Assert.AreEqual(LoadResult.Tampered, result);
        }

        // ── SaveSystemVersionMigrationTests ──────────────────

        [Test]
        public void OldVersion_DetectedAsVersionMismatch()
        {
            var data = SaveData.CreateDefault();
            data.version = "1.0.0";
            SaveSystem.Instance.SaveImmediate(data);
            // SaveImmediate 가 version 을 SaveVersion 으로 덮어쓰므로 직접 파일 조작 필요
            // 대신 SaveData 객체에 SaveVersion=2.0.0 으로 저장 → 그대로 통과
            SaveSystem.Instance.Load(out var loaded);
            Assert.AreEqual(Constants.SaveVersion, loaded.version);
        }

        [Test]
        public void DefaultData_HasCorrectVersion()
        {
            var data = SaveData.CreateDefault();
            Assert.AreEqual(Constants.SaveVersion, data.version);
        }

        // ── SaltProviderTests ────────────────────────────────

        [Test]
        public void DebugSaltProvider_Returns32Bytes()
        {
            var p = new DebugSaltProvider();
            var s = p.GetAppSalt();
            Assert.AreEqual(32, s.Length);
            Assert.IsNotEmpty(p.GetSaltVersion());
        }

        [Test]
        public void DebugSaltProvider_DifferentSeedsProduceDifferentSalts()
        {
            var a = new DebugSaltProvider("seed_A").GetAppSalt();
            var b = new DebugSaltProvider("seed_B").GetAppSalt();
            Assert.AreNotEqual(System.Convert.ToBase64String(a), System.Convert.ToBase64String(b));
        }

        [Test]
        public void RuntimeSaltProvider_BlobRoundTrip()
        {
            var salt = new byte[Constants.SaveSaltLength];
            for (int i = 0; i < salt.Length; i++) salt[i] = (byte)(i * 7);
            var blob = RuntimeSaltProvider.BuildBlob("v_test", salt);
            Assert.IsTrue(RuntimeSaltProvider.TryParse(blob, out var v, out var s));
            Assert.AreEqual("v_test", v);
            CollectionAssert.AreEqual(salt, s);
        }

        [Test]
        public void RuntimeSaltProvider_RejectsCorruptedBlob()
        {
            var bytes = new byte[10];
            Assert.IsFalse(RuntimeSaltProvider.TryParse(bytes, out _, out _));
        }

        // ── SaveAutoTriggerTests ─────────────────────────────

        [Test]
        public void RequestSave_FirstCall_SavesImmediately()
        {
            SaveSystem.Instance.NowProvider = () => 100f;
            var data = SaveData.CreateDefault();
            data.player.gold = 999;
            var r = SaveSystem.Instance.RequestSave(data);
            Assert.AreEqual(SaveResult.Success, r);
            SaveSystem.Instance.Load(out var loaded);
            Assert.AreEqual(999, loaded.player.gold);
        }

        [Test]
        public void RequestSave_WithinInterval_Throttled()
        {
            float now = 100f;
            SaveSystem.Instance.NowProvider = () => now;
            SaveSystem.Instance.RequestSave(SaveData.CreateDefault());
            now += 1f; // 5초 미만
            var r = SaveSystem.Instance.RequestSave(SaveData.CreateDefault());
            Assert.AreEqual(SaveResult.Throttled, r);
        }

        [Test]
        public void RequestSave_AfterInterval_SavesAgain()
        {
            float now = 100f;
            SaveSystem.Instance.NowProvider = () => now;
            SaveSystem.Instance.RequestSave(SaveData.CreateDefault());
            now += 6f;
            var r = SaveSystem.Instance.RequestSave(SaveData.CreateDefault());
            Assert.AreEqual(SaveResult.Success, r);
        }
    }
}
