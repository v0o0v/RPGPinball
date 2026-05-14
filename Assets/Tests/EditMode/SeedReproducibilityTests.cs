using NUnit.Framework;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 시드 결정론 검증.
    /// 같은 시드 → 항상 같은 시퀀스. DeterministicRng 단독.
    /// </summary>
    [TestFixture]
    public class SeedReproducibilityTests
    {
        [Test]
        public void Rng_SameSeed_ProducesSameSequence()
        {
            var a = new DeterministicRng(123UL);
            var b = new DeterministicRng(123UL);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextUInt64(), b.NextUInt64(), $"diverged at i={i}");
        }

        [Test]
        public void Rng_DifferentSeed_ProducesDifferentSequence()
        {
            var a = new DeterministicRng(123UL);
            var b = new DeterministicRng(124UL);
            int diff = 0;
            for (int i = 0; i < 100; i++)
                if (a.NextUInt64() != b.NextUInt64()) diff++;
            Assert.Greater(diff, 90); // 거의 모두 다른 값이어야
        }

        [Test]
        public void Rng_ZeroSeed_AvoidsAllZeroState()
        {
            var rng = new DeterministicRng(0UL);
            ulong first = rng.NextUInt64();
            Assert.AreNotEqual(0UL, first);
        }

        [Test]
        public void CombineSeed_IsCommutativelyOrderDependent()
        {
            // 순서가 다르면 결과도 달라야 함 (좋은 mix)
            ulong a = DeterministicRng.CombineSeed(0, 1, 2, 3);
            ulong b = DeterministicRng.CombineSeed(0, 3, 2, 1);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void HashStableString_SameStringSameHash()
        {
            ulong a = DeterministicRng.HashStableString("player_abc");
            ulong b = DeterministicRng.HashStableString("player_abc");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void HashStableString_DifferentString_DifferentHash()
        {
            ulong a = DeterministicRng.HashStableString("player_a");
            ulong b = DeterministicRng.HashStableString("player_b");
            Assert.AreNotEqual(a, b);
        }
    }
}
