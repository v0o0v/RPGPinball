using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RPGPinball.Security
{
    /// <summary>
    /// AES-256-CBC 암호화 + HMAC-SHA256 서명으로 세이브 데이터를 보호한다.
    /// 디바이스 고유 키와 앱 내장 Salt를 조합해 키를 파생한다.
    /// </summary>
    public static class SaveEncryption
    {
        // 실제 배포 시 하드코딩 대신 서버 또는 빌드 파이프라인에서 주입해야 함
        private const string AppSalt = "RPGPinball_Salt_v1";

        private static byte[] DeriveKey()
        {
            var raw = SystemInfo.deviceUniqueIdentifier + AppSalt;
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        }

        public static string Encrypt(string plaintext)
        {
            var key = DeriveKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IV(16바이트) + 암호문을 Base64로 직렬화
            var combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            var cipherB64 = Convert.ToBase64String(combined);
            var hmac = ComputeHmac(cipherB64, key);
            return cipherB64 + "." + hmac;
        }

        public static bool TryDecrypt(string payload, out string plaintext)
        {
            plaintext = null;
            try
            {
                var parts = payload.Split('.');
                if (parts.Length != 2) return false;

                var cipherB64 = parts[0];
                var hmacStored = parts[1];
                var key = DeriveKey();

                if (ComputeHmac(cipherB64, key) != hmacStored)
                {
                    Debug.LogWarning("[SaveEncryption] HMAC 검증 실패 — 파일 변조 가능성.");
                    return false;
                }

                var combined = Convert.FromBase64String(cipherB64);
                using var aes = Aes.Create();
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var iv = new byte[16];
                Buffer.BlockCopy(combined, 0, iv, 0, 16);
                aes.IV = iv;

                var cipherBytes = new byte[combined.Length - 16];
                Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                plaintext = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveEncryption] 복호화 실패: {e.Message}");
                return false;
            }
        }

        private static string ComputeHmac(string data, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}
