using System.Security.Cryptography;
using System.Text;

namespace RPGPinball.Security
{
    /// <summary>
    /// 에디터/단위 테스트 디버그 솔트. 고정 문자열을 SHA256으로 늘려 32바이트 생성.
    /// 실제 배포는 RuntimeSaltProvider (StreamingAssets/salt.bin) 사용.
    /// </summary>
    public class DebugSaltProvider : ISaltProvider
    {
        private readonly byte[] cachedSalt;
        private readonly string version;

        public DebugSaltProvider(string saltSeed = "RPGPinball_Debug_Salt_v1", string version = "debug_v1")
        {
            this.version = version;
            using var sha = SHA256.Create();
            cachedSalt = sha.ComputeHash(Encoding.UTF8.GetBytes(saltSeed));
        }

        public byte[] GetAppSalt() => (byte[])cachedSalt.Clone();
        public string GetSaltVersion() => version;
    }
}
