using UnityEngine;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 보스 강제 reset 패턴 무효화 카운트다운(긴급 방패).
    /// FrostGiant/WinterQueen 등의 강제 reset 패턴에서 IsActive 검사 후 무시.
    /// </summary>
    public static class BossForcedResetGuard
    {
        private static float activeUntil = -1f;

        public static bool IsActive => Time.time < activeUntil;

        public static void Activate(float duration)
        {
            float t = Time.time + Mathf.Max(0f, duration);
            if (t > activeUntil) activeUntil = t;
        }

        public static void Deactivate() => activeUntil = -1f;
    }
}
