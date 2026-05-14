using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RPGPinball.Security
{
    /// <summary>
    /// AES-256-CBC 암호화 + HMAC-SHA256 서명. ISaltProvider 주입형.
    /// 정적 API(Encrypt/TryDecrypt)는 DefaultProvider(Debug)로 작동 — 하위 호환용.
    /// 본 마일스톤 7부터는 SaveSystem 에서 인스턴스 방식으로 사용 권장.
    /// </summary>
    public class SaveEncryption
    {
        public const int IvLength = 16;
        public const int HmacLength = 32;
        public const string HeaderToken = "RPGP2"; // 5바이트 매직 (M7 포맷 식별)

        private readonly ISaltProvider saltProvider;

        public SaveEncryption(ISaltProvider provider)
        {
            saltProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public string SaltVersion => saltProvider.GetSaltVersion();

        private byte[] DeriveKey()
        {
            var saltBytes = saltProvider.GetAppSalt();
            var deviceBytes = Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier ?? "no_device");
            var combined = new byte[deviceBytes.Length + saltBytes.Length];
            Buffer.BlockCopy(deviceBytes, 0, combined, 0, deviceBytes.Length);
            Buffer.BlockCopy(saltBytes, 0, combined, deviceBytes.Length, saltBytes.Length);
            using var sha = SHA256.Create();
            return sha.ComputeHash(combined);
        }

        /// <summary>
        /// 평문 → 바이너리 포맷.
        /// 포맷: [16 IV][N 암호문][32 HMAC]
        /// HMAC은 IV || 암호문 위에서 계산.
        /// </summary>
        public byte[] EncryptToBytes(string plaintext)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
            var key = DeriveKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            using var hmac = new HMACSHA256(key);
            var ivPlusCipher = new byte[IvLength + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, ivPlusCipher, 0, IvLength);
            Buffer.BlockCopy(cipherBytes, 0, ivPlusCipher, IvLength, cipherBytes.Length);
            var hmacBytes = hmac.ComputeHash(ivPlusCipher);

            var output = new byte[ivPlusCipher.Length + HmacLength];
            Buffer.BlockCopy(ivPlusCipher, 0, output, 0, ivPlusCipher.Length);
            Buffer.BlockCopy(hmacBytes, 0, output, ivPlusCipher.Length, HmacLength);
            return output;
        }

        public DecryptResult TryDecryptFromBytes(byte[] payload, out string plaintext)
        {
            plaintext = null;
            if (payload == null || payload.Length < IvLength + HmacLength + 1)
                return DecryptResult.Corrupted;
            try
            {
                var key = DeriveKey();
                int cipherLen = payload.Length - IvLength - HmacLength;
                var iv = new byte[IvLength];
                var cipher = new byte[cipherLen];
                var hmacStored = new byte[HmacLength];
                Buffer.BlockCopy(payload, 0, iv, 0, IvLength);
                Buffer.BlockCopy(payload, IvLength, cipher, 0, cipherLen);
                Buffer.BlockCopy(payload, IvLength + cipherLen, hmacStored, 0, HmacLength);

                using var hmac = new HMACSHA256(key);
                var ivPlusCipher = new byte[IvLength + cipherLen];
                Buffer.BlockCopy(iv, 0, ivPlusCipher, 0, IvLength);
                Buffer.BlockCopy(cipher, 0, ivPlusCipher, IvLength, cipherLen);
                var hmacComputed = hmac.ComputeHash(ivPlusCipher);

                if (!ConstantTimeEquals(hmacStored, hmacComputed))
                    return DecryptResult.Tampered;

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                plaintext = Encoding.UTF8.GetString(plainBytes);
                return DecryptResult.Success;
            }
            catch (CryptographicException)
            {
                return DecryptResult.Tampered;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveEncryption] 복호화 예외: {e.Message}");
                return DecryptResult.Corrupted;
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ── 정적 API (하위 호환) ────────────────────────────────
        private static SaveEncryption defaultInstance;
        private static readonly object defaultLock = new object();

        private static SaveEncryption Default
        {
            get
            {
                if (defaultInstance != null) return defaultInstance;
                lock (defaultLock)
                {
                    defaultInstance ??= new SaveEncryption(new DebugSaltProvider());
                }
                return defaultInstance;
            }
        }

        public static void SetDefaultProvider(ISaltProvider provider)
        {
            lock (defaultLock)
            {
                defaultInstance = new SaveEncryption(provider);
            }
        }

        public static string Encrypt(string plaintext)
        {
            var bytes = Default.EncryptToBytes(plaintext);
            return Convert.ToBase64String(bytes);
        }

        public static bool TryDecrypt(string payload, out string plaintext)
        {
            plaintext = null;
            try
            {
                var bytes = Convert.FromBase64String(payload);
                var result = Default.TryDecryptFromBytes(bytes, out plaintext);
                return result == DecryptResult.Success;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public enum DecryptResult
    {
        Success = 0,
        Tampered = 1,
        Corrupted = 2
    }
}
