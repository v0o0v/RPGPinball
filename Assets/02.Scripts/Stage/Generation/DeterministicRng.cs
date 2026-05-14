using System.Collections.Generic;
using UnityEngine;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 결정론적 PRNG. xoshiro256** 알고리즘.
    /// UnityEngine.Random / System.Random 모두 사용 금지 — 전역 상태 공유 / .NET 버전 의존성 회피.
    /// 같은 시드 → 항상 같은 시퀀스.
    /// </summary>
    public sealed class DeterministicRng
    {
        private ulong s0, s1, s2, s3;

        public DeterministicRng(ulong seed)
        {
            // SplitMix64로 4개 상태 초기화
            s0 = SplitMix64Step(ref seed);
            s1 = SplitMix64Step(ref seed);
            s2 = SplitMix64Step(ref seed);
            s3 = SplitMix64Step(ref seed);

            // 모두 0이면 무효 → 보정
            if ((s0 | s1 | s2 | s3) == 0UL) s0 = 0xDEADBEEFCAFEBABEUL;
        }

        public ulong NextUInt64()
        {
            ulong result = Rotl(s1 * 5UL, 7) * 9UL;
            ulong t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = Rotl(s3, 45);
            return result;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            ulong range = (ulong)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt64() % range);
        }

        public float NextFloat(float minInclusive, float maxExclusive)
        {
            double u = (NextUInt64() >> 11) * (1.0 / (1UL << 53));
            return minInclusive + (float)(u * (maxExclusive - minInclusive));
        }

        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }

        public bool RollChance(float p)
        {
            if (p <= 0f) return false;
            if (p >= 1f) return true;
            return NextDouble() < p;
        }

        public T Pick<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0) return default;
            return source[NextInt(0, source.Count)];
        }

        /// <summary>
        /// 가중치 기반 추첨. 가중치 합 0 이하면 균등 분포로 fallback.
        /// </summary>
        public T WeightedPick<T>(IReadOnlyList<(T item, float weight)> entries)
        {
            if (entries == null || entries.Count == 0) return default;

            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
                total += Mathf.Max(0f, entries[i].weight);

            if (total <= 0f)
                return entries[NextInt(0, entries.Count)].item;

            float roll = NextFloat(0f, total);
            float acc = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                acc += Mathf.Max(0f, entries[i].weight);
                if (roll < acc) return entries[i].item;
            }
            return entries[entries.Count - 1].item;
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 결정론적 시드 합성. XOR + 황금비 상수 + 시프트 (Boost::hash_combine 변형).
        /// </summary>
        public static ulong CombineSeed(ulong baseSeed, params int[] salts)
        {
            ulong h = baseSeed;
            if (salts == null) return h;
            for (int i = 0; i < salts.Length; i++)
            {
                ulong v = (ulong)(uint)salts[i];
                h ^= v + 0x9E3779B97F4A7C15UL + (h << 6) + (h >> 2);
            }
            return h;
        }

        /// <summary>
        /// 문자열 → 결정론적 해시 (FNV-1a 64bit). UnityEngine 의존 없음.
        /// </summary>
        public static ulong HashStableString(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0xCBF29CE484222325UL;
            ulong hash = 0xCBF29CE484222325UL;
            for (int i = 0; i < s.Length; i++)
            {
                hash ^= s[i];
                hash *= 0x100000001B3UL;
            }
            return hash;
        }

        private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        private static ulong SplitMix64Step(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
