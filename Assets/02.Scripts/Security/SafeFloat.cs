using System;
using UnityEngine;

namespace RPGPinball.Security
{
    // float 래퍼. XOR 난독화로 메모리 스캐너 방지.
    [Serializable]
    public struct SafeFloat : IEquatable<SafeFloat>
    {
        private int obfuscated;
        private int key;

        public static SafeFloat Create(float value)
        {
            var s = new SafeFloat();
            s.key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            s.obfuscated = FloatToInt(value) ^ s.key;
            return s;
        }

        public float Value
        {
            get => IntToFloat(obfuscated ^ key);
            set
            {
                key = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                obfuscated = FloatToInt(value) ^ key;
            }
        }

        private static int FloatToInt(float f) => BitConverter.SingleToInt32Bits(f);

        private static float IntToFloat(int i) => BitConverter.Int32BitsToSingle(i);

        public static implicit operator float(SafeFloat s) => s.Value;

        public static implicit operator SafeFloat(float v) => Create(v);

        public static SafeFloat operator +(SafeFloat a, float b) => Create(a.Value + b);

        public static SafeFloat operator -(SafeFloat a, float b) => Create(a.Value - b);

        public static SafeFloat operator *(SafeFloat a, float b) => Create(a.Value * b);

        public static bool operator ==(SafeFloat a, SafeFloat b) => Mathf.Approximately(a.Value, b.Value);

        public static bool operator !=(SafeFloat a, SafeFloat b) => !Mathf.Approximately(a.Value, b.Value);

        public bool Equals(SafeFloat other) => Mathf.Approximately(Value, other.Value);

        public override bool Equals(object obj) => obj is SafeFloat s && Equals(s);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
    }
}
