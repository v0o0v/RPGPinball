using System;
using UnityEngine;

namespace RPGPinball.Security
{
    // long 래퍼. 골드, XP 등 큰 수치용 XOR 난독화.
    [Serializable]
    public struct SafeLong : IEquatable<SafeLong>
    {
        private long obfuscated;
        private long key;

        public static SafeLong Create(long value)
        {
            var s = new SafeLong();
            s.key = GenerateKey();
            s.obfuscated = value ^ s.key;
            return s;
        }

        public long Value
        {
            get => obfuscated ^ key;
            set
            {
                key = GenerateKey();
                obfuscated = value ^ key;
            }
        }

        private static long GenerateKey()
        {
            unchecked
            {
                long hi = (long)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 32;
                long lo = (long)(uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                return hi | lo;
            }
        }

        public static implicit operator long(SafeLong s) => s.Value;

        public static implicit operator SafeLong(long v) => Create(v);

        public static SafeLong operator +(SafeLong a, long b) => Create(a.Value + b);

        public static SafeLong operator -(SafeLong a, long b) => Create(a.Value - b);

        public static SafeLong operator *(SafeLong a, long b) => Create(a.Value * b);

        public static bool operator ==(SafeLong a, SafeLong b) => a.Value == b.Value;

        public static bool operator !=(SafeLong a, SafeLong b) => a.Value != b.Value;

        public static bool operator >(SafeLong a, SafeLong b) => a.Value > b.Value;

        public static bool operator <(SafeLong a, SafeLong b) => a.Value < b.Value;

        public bool Equals(SafeLong other) => Value == other.Value;

        public override bool Equals(object obj) => obj is SafeLong s && Equals(s);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
    }
}
