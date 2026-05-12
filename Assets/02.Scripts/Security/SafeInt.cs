using System;
using UnityEngine;

namespace RPGPinball.Security
{
    // int 래퍼. XOR 난독화로 메모리 스캐너(GameGuardian 등) 방지.
    [Serializable]
    public struct SafeInt : IEquatable<SafeInt>
    {
        private int obfuscated;
        private int key;

        public static SafeInt Create(int value)
        {
            var s = new SafeInt();
            s.key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            s.obfuscated = value ^ s.key;
            return s;
        }

        public int Value
        {
            get => obfuscated ^ key;
            set
            {
                key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                obfuscated = value ^ key;
            }
        }

        public static implicit operator int(SafeInt s) => s.Value;

        public static implicit operator SafeInt(int v) => Create(v);

        public static SafeInt operator +(SafeInt a, int b) => Create(a.Value + b);

        public static SafeInt operator -(SafeInt a, int b) => Create(a.Value - b);

        public static SafeInt operator *(SafeInt a, int b) => Create(a.Value * b);

        public static bool operator ==(SafeInt a, SafeInt b) => a.Value == b.Value;

        public static bool operator !=(SafeInt a, SafeInt b) => a.Value != b.Value;

        public static bool operator >(SafeInt a, SafeInt b) => a.Value > b.Value;

        public static bool operator <(SafeInt a, SafeInt b) => a.Value < b.Value;

        public bool Equals(SafeInt other) => Value == other.Value;

        public override bool Equals(object obj) => obj is SafeInt s && Equals(s);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
    }
}
