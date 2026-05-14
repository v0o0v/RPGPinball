using System;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 스테이지 시드 합성 팩토리.
    /// - 최초 진입: PlayerUID + 날짜(KST) + actId + stageIndex
    /// - 재도전: 기존 시드 + retryCount
    /// - 일일 도전: 날짜 해시 단독 (플레이어 무관)
    /// </summary>
    public static class StageSeedFactory
    {
        /// <summary>
        /// 최초 진입용 시드. 같은 날짜·같은 플레이어 → 같은 결과.
        /// </summary>
        public static ulong BuildSeed(string playerUid, DateTime nowKst, ActId actId, int stageIndex)
        {
            ulong baseSeed = DeterministicRng.HashStableString(playerUid ?? string.Empty);
            int dateHash = ToDateHash(nowKst);
            return DeterministicRng.CombineSeed(
                baseSeed,
                dateHash,
                ((int)actId) * Constants.SeedSaltActKey,
                stageIndex * Constants.SeedSaltStageKey);
        }

        /// <summary>
        /// 재도전용 시드. retryCount 가 다르면 다른 레이아웃.
        /// </summary>
        public static ulong BuildSeedForRetry(ulong baseSeed, int retryCount)
        {
            return DeterministicRng.CombineSeed(baseSeed, retryCount * Constants.SeedSaltRetryKey);
        }

        /// <summary>
        /// 일일 도전 시드. 같은 날짜 → 모든 플레이어 동일 결과.
        /// </summary>
        public static ulong BuildDailyChallengeSeed(DateTime nowKst, ActId actId, int stageIndex)
        {
            int dateHash = ToDateHash(nowKst);
            return DeterministicRng.CombineSeed(
                (ulong)Constants.SeedSaltDailyKey,
                dateHash,
                ((int)actId) * Constants.SeedSaltActKey,
                stageIndex * Constants.SeedSaltStageKey);
        }

        /// <summary>
        /// 현재 KST(UTC+9) 시각. 자정(UTC+9) 갱신 기준.
        /// </summary>
        public static DateTime NowKst()
        {
            return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(Constants.SeedKstOffsetHours)).DateTime;
        }

        private static int ToDateHash(DateTime kst)
        {
            return kst.Year * 10000 + kst.Month * 100 + kst.Day;
        }
    }
}
