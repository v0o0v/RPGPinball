using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Security;

namespace RPGPinball.Core
{
    /// <summary>
    /// 정식 SaveSystem (M7). AES-256-CBC + HMAC-SHA256 + 클라우드 동기화 + 자동 저장.
    /// 싱글턴이지만 GameObject 가 아닌 정적 컨테이너 — 테스트에서 ResetForTest 로 초기화.
    /// </summary>
    public class SaveSystem
    {
        public static SaveSystem Instance { get; private set; } = new SaveSystem();

        public ISaltProvider SaltProvider { get; private set; }
        public SaveEncryption Encryption { get; private set; }

        public SaveData CurrentData { get; private set; }

        private string saveFilePath;
        private float lastSaveRealtime = -100f;
        private bool queuedSave;
        private readonly SemaphoreSlim ioLock = new SemaphoreSlim(1, 1);
        // 테스트 친화: 실제 시각 대신 누적 시간 주입 가능
        public Func<float> NowProvider { get; set; } = () => Time.realtimeSinceStartup;

        private SaveSystem() { }

        public static SaveSystem ResetForTest(ISaltProvider provider = null, string overrideFilePath = null)
        {
            Instance = new SaveSystem();
            Instance.Initialize(provider ?? new DebugSaltProvider(), overrideFilePath);
            return Instance;
        }

        public void Initialize(ISaltProvider provider, string overrideFilePath = null)
        {
            SaltProvider = provider;
            Encryption = new SaveEncryption(provider);
            SaveEncryption.SetDefaultProvider(provider);
            saveFilePath = overrideFilePath
                ?? Path.Combine(Application.persistentDataPath, Constants.SaveFileName);
            CurrentData ??= SaveData.CreateDefault();
        }

        public string FilePath => saveFilePath;

        public bool HasSave()
        {
            return !string.IsNullOrEmpty(saveFilePath) && File.Exists(saveFilePath);
        }

        /// <summary>5초 인터벌 제한 적용 저장. 짧으면 큐 적재 후 5초 후 1회만 실행.</summary>
        public SaveResult RequestSave(SaveData data = null)
        {
            if (data != null) CurrentData = data;
            float now = NowProvider();
            if (now - lastSaveRealtime < Constants.SaveAutoIntervalSec)
            {
                if (!queuedSave)
                {
                    queuedSave = true;
                    ScheduleDeferredSave().Forget();
                }
                return SaveResult.Throttled;
            }
            return SaveImmediate(CurrentData);
        }

        private async UniTaskVoid ScheduleDeferredSave()
        {
            float waitSec = Mathf.Max(0.05f, Constants.SaveAutoIntervalSec - (NowProvider() - lastSaveRealtime));
            await UniTask.Delay(TimeSpan.FromSeconds(waitSec));
            queuedSave = false;
            SaveImmediate(CurrentData);
        }

        public SaveResult SaveImmediate(SaveData data)
        {
            try
            {
                ioLock.Wait();
                if (data == null) data = CurrentData;
                if (data == null) return SaveResult.IOError;
                if (Encryption == null) Initialize(new DebugSaltProvider());

                data.lastSaveTime = DateTime.UtcNow.ToString("o");
                data.version = Constants.SaveVersion;

                var json = JsonUtility.ToJson(data);
                var bytes = Encryption.EncryptToBytes(json);
                var dir = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // 원자적 쓰기: tmp → move
                var tmp = saveFilePath + ".tmp";
                File.WriteAllBytes(tmp, bytes);
                if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
                File.Move(tmp, saveFilePath);

                lastSaveRealtime = NowProvider();
                CurrentData = data;
                EventBus.Publish(new OnSaveCompleted { Success = true });
                return SaveResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 저장 실패: {e.Message}");
                EventBus.Publish(new OnSaveFailed { ErrorReason = e.Message });
                return SaveResult.IOError;
            }
            finally
            {
                if (ioLock.CurrentCount == 0) ioLock.Release();
            }
        }

        public LoadResult Load(out SaveData data)
        {
            data = null;
            try
            {
                ioLock.Wait();
                if (Encryption == null) Initialize(new DebugSaltProvider());
                if (!HasSave()) return LoadResult.NotFound;

                var bytes = File.ReadAllBytes(saveFilePath);
                var decrypted = Encryption.TryDecryptFromBytes(bytes, out var json);
                if (decrypted == DecryptResult.Tampered) return LoadResult.Tampered;
                if (decrypted == DecryptResult.Corrupted) return LoadResult.Corrupted;

                var loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null) return LoadResult.Corrupted;
                if (loaded.version != Constants.SaveVersion)
                {
                    Debug.LogWarning($"[SaveSystem] 버전 불일치: {loaded.version} ≠ {Constants.SaveVersion}");
                    return LoadResult.VersionMismatch;
                }
                data = loaded;
                CurrentData = loaded;
                EventBus.Publish(new OnLoadCompleted { Success = true, ErrorReason = null });
                return LoadResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 로드 실패: {e.Message}");
                EventBus.Publish(new OnLoadCompleted { Success = false, ErrorReason = e.Message });
                return LoadResult.IOError;
            }
            finally
            {
                if (ioLock.CurrentCount == 0) ioLock.Release();
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 삭제 실패: {e.Message}");
            }
            CurrentData = SaveData.CreateDefault();
        }

        /// <summary>구버전(PlayerPrefs) 마이그레이션 시도. 성공 시 SaveSystem 저장 + 구 키 삭제.</summary>
        public bool TryMigrateFromV1()
        {
            if (HasSave()) return false;
            if (!SaveMigrationV1ToV2.HasLegacyPrefs()) return false;

            var migrated = SaveMigrationV1ToV2.Migrate();
            if (SaveImmediate(migrated) == SaveResult.Success)
            {
                SaveMigrationV1ToV2.DeleteLegacyPrefs();
                return true;
            }
            return false;
        }

        // ── 클라우드 동기화 (CloudSaveAdapter 위임) ────────────
        public async UniTask<CloudSyncResult> UploadToCloudAsync()
        {
            EventBus.Publish(new OnCloudSyncStarted { });
            var result = await CloudSaveAdapter.Instance.UploadAsync(CurrentData);
            EventBus.Publish(new OnCloudSyncCompleted { Success = result == CloudSyncResult.Success, ErrorReason = result.ToString() });
            return result;
        }

        public async UniTask<CloudSyncResult> DownloadFromCloudAsync()
        {
            EventBus.Publish(new OnCloudSyncStarted { });
            var (result, remote) = await CloudSaveAdapter.Instance.DownloadAsync();
            if (result == CloudSyncResult.Success && remote != null)
            {
                var conflict = CloudSaveAdapter.Instance.ResolveConflict(CurrentData, remote, out var chosen);
                CurrentData = chosen;
                SaveImmediate(CurrentData);
                EventBus.Publish(new OnCloudSyncCompleted { Success = true, ErrorReason = conflict.ToString() });
                return conflict;
            }
            EventBus.Publish(new OnCloudSyncCompleted { Success = false, ErrorReason = result.ToString() });
            return result;
        }
    }
}
