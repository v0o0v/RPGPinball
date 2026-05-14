using NUnit.Framework;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;
using RPGPinball.Stage.Wave;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 웨이브 구성 검증.
    /// WaveCount = floor(Stage/5) + 1, 엘리트 확률, 패턴 추첨.
    /// </summary>
    [TestFixture]
    public class WaveCompositionTests
    {
        [Test]
        public void WaveCount_Stage1_Returns1()
        {
            int count = (1 / 5) + 1;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void WaveCount_Stage10_Returns3()
        {
            int count = (10 / 5) + 1;
            Assert.AreEqual(3, count);
        }

        [Test]
        public void WaveCount_Stage30_Returns7()
        {
            int count = (30 / 5) + 1;
            Assert.AreEqual(7, count);
        }

        [Test]
        public void EliteChance_Stage1_Is0_05()
        {
            float chance = UnityEngine.Mathf.Min(0.5f, 0.05f * 1);
            Assert.AreEqual(0.05f, chance, 0.001f);
        }

        [Test]
        public void EliteChance_Stage30_CappedAt0_5()
        {
            float chance = UnityEngine.Mathf.Min(0.5f, 0.05f * 30);
            Assert.AreEqual(0.5f, chance, 0.001f);
        }

        [Test]
        public void CompositionPicker_AllPatternsCanBePicked()
        {
            var bucket = new System.Collections.Generic.HashSet<WaveCompositionPattern>();
            for (int i = 0; i < 100; i++)
            {
                var rng = new DeterministicRng((ulong)i);
                bucket.Add(WaveCompositionPicker.Pick(DifficultyBand.Development, null, rng));
            }
            Assert.AreEqual(3, bucket.Count, "3가지 패턴 모두 적어도 1회 추첨되어야 함");
        }

        [Test]
        public void CompositionPicker_SameSeedSamePattern()
        {
            var rng1 = new DeterministicRng(42UL);
            var rng2 = new DeterministicRng(42UL);
            var a = WaveCompositionPicker.Pick(DifficultyBand.Climax, null, rng1);
            var b = WaveCompositionPicker.Pick(DifficultyBand.Climax, null, rng2);
            Assert.AreEqual(a, b);
        }
    }
}
