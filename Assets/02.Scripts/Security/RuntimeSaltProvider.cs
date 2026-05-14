using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Security
{
    /// <summary>
    /// 런타임 솔트 제공자. StreamingAssets/salt.bin (난독화 처리)에서 32바이트 솔트 + 버전 헤더 로드.
    /// 파일 포맷: [4바이트 버전 길이][N바이트 버전 문자열][32바이트 솔트]
    /// 파일이 없거나 손상 시 DebugSaltProvider fallback.
    /// </summary>
    public class RuntimeSaltProvider : ISaltProvider
    {
        private byte[] saltBytes;
        private string saltVersion = Constants.SaveDebugSaltVersion;
        private bool loaded;

        public byte[] GetAppSalt()
        {
            EnsureLoaded();
            return (byte[])saltBytes.Clone();
        }

        public string GetSaltVersion()
        {
            EnsureLoaded();
            return saltVersion;
        }

        private void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, Constants.SaltFileName);
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    if (TryParse(bytes, out var v, out var s))
                    {
                        saltVersion = v;
                        saltBytes = s;
                        return;
                    }
                    Debug.LogWarning("[RuntimeSaltProvider] salt.bin 포맷 오류 — 디버그 솔트 fallback.");
                }
                else
                {
                    Debug.LogWarning("[RuntimeSaltProvider] StreamingAssets/salt.bin 없음 — 디버그 솔트 fallback.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RuntimeSaltProvider] 로드 실패: {e.Message}");
            }

            using var sha = SHA256.Create();
            saltBytes = sha.ComputeHash(Encoding.UTF8.GetBytes("RPGPinball_RuntimeFallback_v1"));
            saltVersion = Constants.SaveDebugSaltVersion;
        }

        public static bool TryParse(byte[] raw, out string version, out byte[] salt)
        {
            version = null; salt = null;
            if (raw == null || raw.Length < 4 + 1 + Constants.SaveSaltLength) return false;
            int vLen = (raw[0] << 24) | (raw[1] << 16) | (raw[2] << 8) | raw[3];
            if (vLen <= 0 || vLen > 64) return false;
            if (raw.Length != 4 + vLen + Constants.SaveSaltLength) return false;
            version = Encoding.UTF8.GetString(raw, 4, vLen);
            salt = new byte[Constants.SaveSaltLength];
            System.Buffer.BlockCopy(raw, 4 + vLen, salt, 0, Constants.SaveSaltLength);
            return true;
        }

        public static byte[] BuildBlob(string version, byte[] salt)
        {
            if (salt == null || salt.Length != Constants.SaveSaltLength)
                throw new System.ArgumentException("salt length must be 32");
            var verBytes = Encoding.UTF8.GetBytes(version);
            var blob = new byte[4 + verBytes.Length + Constants.SaveSaltLength];
            blob[0] = (byte)((verBytes.Length >> 24) & 0xff);
            blob[1] = (byte)((verBytes.Length >> 16) & 0xff);
            blob[2] = (byte)((verBytes.Length >> 8) & 0xff);
            blob[3] = (byte)(verBytes.Length & 0xff);
            System.Buffer.BlockCopy(verBytes, 0, blob, 4, verBytes.Length);
            System.Buffer.BlockCopy(salt, 0, blob, 4 + verBytes.Length, Constants.SaveSaltLength);
            return blob;
        }
    }
}
