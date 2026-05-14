using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Core
{
    /// <summary>
    /// Google Play Games Saved Games API 래핑(스텁). M7 본 구현은 인증/업로드/다운로드/자동 충돌 해결까지.
    /// 실제 GPG SDK 통합은 M8 인계. 본 어댑터는 메모리 기반 mock 으로 동작 + 인터페이스 제공.
    /// </summary>
    public class CloudSaveAdapter
    {
        public static CloudSaveAdapter Instance { get; private set; } = new CloudSaveAdapter();

        public bool IsAuthenticated { get; private set; }
        private SaveData mockRemote;

        public void ResetForTest()
        {
            IsAuthenticated = false;
            mockRemote = null;
        }

        public async UniTask<bool> AuthenticateAsync()
        {
            // M8 본 구현: PlayGamesPlatform.Authenticate
            await UniTask.DelayFrame(1);
            IsAuthenticated = true;
            return true;
        }

        public void Logout()
        {
            IsAuthenticated = false;
        }

        public async UniTask<CloudSyncResult> UploadAsync(SaveData data)
        {
            if (!IsAuthenticated) return CloudSyncResult.NotAuthenticated;
            await UniTask.DelayFrame(1);
            mockRemote = CloneViaJson(data);
            return CloudSyncResult.Success;
        }

        public async UniTask<(CloudSyncResult result, SaveData remoteData)> DownloadAsync()
        {
            if (!IsAuthenticated) return (CloudSyncResult.NotAuthenticated, null);
            await UniTask.DelayFrame(1);
            return mockRemote != null
                ? (CloudSyncResult.Success, CloneViaJson(mockRemote))
                : (CloudSyncResult.IOError, null);
        }

        /// <summary>
        /// 자동 충돌 해결 (M7 범위): lastSaveTime 비교 후 최신 자동 선택.
        /// 5분 이상 차이 시 Conflict 반환 — UI 표시는 M8 인계.
        /// </summary>
        public CloudSyncResult ResolveConflict(SaveData local, SaveData remote, out SaveData chosen)
        {
            chosen = local;
            if (remote == null) return CloudSyncResult.Success;
            if (!DateTime.TryParse(local.lastSaveTime, out var localTime)) localTime = DateTime.MinValue;
            if (!DateTime.TryParse(remote.lastSaveTime, out var remoteTime)) remoteTime = DateTime.MinValue;

            var diff = (localTime - remoteTime).Duration();
            if (diff > TimeSpan.FromMinutes(5))
            {
                chosen = localTime >= remoteTime ? local : remote;
                return CloudSyncResult.Conflict;
            }
            chosen = localTime >= remoteTime ? local : remote;
            return CloudSyncResult.Success;
        }

        private static SaveData CloneViaJson(SaveData src)
        {
            var json = JsonUtility.ToJson(src);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}
