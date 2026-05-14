using NUnit.Framework;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 난이도 예산 공식 검증.
    /// Procedural_Stage_Gen.md §3 표 일치.
    /// </summary>
    [TestFixture]
    public class DifficultyBudgetTests
    {
        // 2026-05-14: 화면 50% 충진 + 좌우 대칭 배치를 위해 budget 공식 800 + N*60 으로 상향.
        [Test]
        public void BaseBudget_Stage1_Returns860()
        {
            Assert.AreEqual(860, DifficultyBudget.ComputeBaseBudget(1));
        }

        [Test]
        public void BaseBudget_Stage9_Returns1340()
        {
            Assert.AreEqual(1340, DifficultyBudget.ComputeBaseBudget(9));
        }

        [Test]
        public void BaseBudget_Stage29_Returns2540()
        {
            Assert.AreEqual(2540, DifficultyBudget.ComputeBaseBudget(29));
        }

        [Test]
        public void FinalBudget_Act1Stage1_WithinExpectedRange()
        {
            // Act1 ×1.0 + Stage1 = 860 ± 10% → 774 ~ 946
            var rng = new DeterministicRng(42UL);
            int budget = DifficultyBudget.ComputeFinalBudget(1, ActId.Act1_Spring, rng);
            Assert.GreaterOrEqual(budget, 774);
            Assert.LessOrEqual(budget, 946);
        }

        [Test]
        public void FinalBudget_Act4Stage29_WithinExpectedRange()
        {
            // Act4 ×3.5 + Stage29 = 2540*3.5 = 8890 ± 10% → 8001 ~ 9779
            var rng = new DeterministicRng(42UL);
            int budget = DifficultyBudget.ComputeFinalBudget(29, ActId.Act4_Winter, rng);
            Assert.GreaterOrEqual(budget, 8001);
            Assert.LessOrEqual(budget, 9779);
        }

        [Test]
        public void RecommendedLevel_Act1Stage1_Returns1()
        {
            Assert.AreEqual(1, DifficultyBudget.ComputeRecommendedLevel(1, ActId.Act1_Spring));
        }

        [Test]
        public void RecommendedLevel_Act1Stage15_Returns13()
        {
            // 1 + floor(15 * 24/30) = 1 + 12 = 13
            Assert.AreEqual(13, DifficultyBudget.ComputeRecommendedLevel(15, ActId.Act1_Spring));
        }

        [Test]
        public void RecommendedLevel_Act4Stage30_Returns90()
        {
            // 72 + floor(30 * 18/30) = 72 + 18 = 90
            Assert.AreEqual(90, DifficultyBudget.ComputeRecommendedLevel(30, ActId.Act4_Winter));
        }

        [Test]
        public void BandFor_BoundariesCorrect()
        {
            Assert.AreEqual(DifficultyBand.Prologue, DifficultyBudget.BandFor(1));
            Assert.AreEqual(DifficultyBand.Prologue, DifficultyBudget.BandFor(9));
            Assert.AreEqual(DifficultyBand.Development, DifficultyBudget.BandFor(11));
            Assert.AreEqual(DifficultyBand.Development, DifficultyBudget.BandFor(19));
            Assert.AreEqual(DifficultyBand.Climax, DifficultyBudget.BandFor(21));
            Assert.AreEqual(DifficultyBand.Climax, DifficultyBudget.BandFor(29));
        }

        [Test]
        public void FinalBudget_1000Iterations_AllWithinTolerance()
        {
            // 분포 검증: stage15 base = 800 + 15*60 = 1700, ±10% → 1530 ~ 1870
            for (int i = 0; i < 1000; i++)
            {
                var rng = new DeterministicRng((ulong)(100 + i));
                int b = DifficultyBudget.ComputeFinalBudget(15, ActId.Act1_Spring, rng);
                Assert.GreaterOrEqual(b, 1530);
                Assert.LessOrEqual(b, 1870);
            }
        }
    }
}
