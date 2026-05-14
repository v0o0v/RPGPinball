using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;
using RPGPinball.Stage.Segments;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 세그먼트 레이아웃 빌더 검증.
    /// - band별 중단 개수
    /// - 스테이지 세로 총합 = 카메라 시야 세로 × 3 (±0.5U)
    /// </summary>
    [TestFixture]
    public class SegmentLayoutTests
    {
        private SegmentData MakeSeg(string id, SegmentSlot slot, float height, float min = 2f, float max = 8f)
        {
            var s = ScriptableObject.CreateInstance<SegmentData>();
            s.segmentId = id;
            s.slot = slot;
            s.height = height;
            s.heightMin = min;
            s.heightMax = max;
            return s;
        }

        private SegmentPool MakePool()
        {
            var pool = SegmentPool.Instance;
            // 풍부한 후보군
            pool.OverrideForTest(new[]
            {
                MakeSeg("top1", SegmentSlot.Top, Constants.SegTopHeightDefault),
                MakeSeg("top2", SegmentSlot.Top, Constants.SegTopHeightDefault),
                MakeSeg("mid1", SegmentSlot.Middle, 4f, 3f, 12f),
                MakeSeg("mid2", SegmentSlot.Middle, 4f, 3f, 12f),
                MakeSeg("mid3", SegmentSlot.Middle, 4f, 3f, 12f),
                MakeSeg("mid4", SegmentSlot.Middle, 4f, 3f, 12f),
                MakeSeg("mid5", SegmentSlot.Middle, 4f, 3f, 12f),
                MakeSeg("bot1", SegmentSlot.Bottom, Constants.SegBottomHeightDefault)
            });
            return pool;
        }

        // 2026-05-14: 핀볼 정석 구역화 — middle 은 모든 band 에서 정확히 3개로 강제.
        // (SlingshotZone / MidPlay / BumperCluster)
        [Test]
        public void Build_Prologue_HasThreeMiddleSegments()
        {
            var rng = new DeterministicRng(99UL);
            var pool = MakePool();
            var layout = SegmentLayoutBuilder.Build(ActId.Act1_Spring, DifficultyBand.Prologue, rng, pool);
            Assert.AreEqual(3, layout.middles.Count);
        }

        [Test]
        public void Build_Development_HasThreeMiddleSegments()
        {
            var rng = new DeterministicRng(7UL);
            var pool = MakePool();
            var layout = SegmentLayoutBuilder.Build(ActId.Act1_Spring, DifficultyBand.Development, rng, pool);
            Assert.AreEqual(3, layout.middles.Count);
        }

        [Test]
        public void Build_Climax_HasThreeMiddleSegments()
        {
            var rng = new DeterministicRng(13UL);
            var pool = MakePool();
            var layout = SegmentLayoutBuilder.Build(ActId.Act1_Spring, DifficultyBand.Climax, rng, pool);
            Assert.AreEqual(3, layout.middles.Count);
        }

        [Test]
        public void Build_TotalHeightMatchesTarget()
        {
            // OrthoSize 무관 — target × 6 = orthoSize × 2 × 3 를 빌더가 자동 추종해야 함
            var rng = new DeterministicRng(123UL);
            var pool = MakePool();
            var layout = SegmentLayoutBuilder.Build(ActId.Act1_Spring, DifficultyBand.Development, rng, pool);
            Assert.Greater(layout.targetStageHeight, 0f);
            Assert.That(Mathf.Abs(layout.totalHeight - layout.targetStageHeight), Is.LessThanOrEqualTo(Constants.SegHeightTolerance),
                $"세로 합 {layout.totalHeight} vs target {layout.targetStageHeight}");
        }

        [Test]
        public void Build_FuzzMultipleSeeds_AllWithinTolerance()
        {
            var pool = MakePool();
            for (int i = 0; i < 100; i++)
            {
                var rng = new DeterministicRng((ulong)(1000 + i));
                var band = (DifficultyBand)(i % 3);
                var layout = SegmentLayoutBuilder.Build(ActId.Act1_Spring, band, rng, pool);
                Assert.That(Mathf.Abs(layout.totalHeight - layout.targetStageHeight),
                    Is.LessThanOrEqualTo(Constants.SegHeightTolerance),
                    $"seed={1000 + i} band={band} 세로 합 {layout.totalHeight} vs target {layout.targetStageHeight}");
            }
        }

        [Test]
        public void ComputeTargetStageHeight_EqualsOrthoSizeTimes6()
        {
            // 카메라 존재 여부 무관 — orthoSize × 2 × Constants.SegStageVerticalScreenCount
            float target = SegmentLayoutBuilder.ComputeTargetStageHeight();
            float expectedOrtho = (Camera.main != null && Camera.main.orthographic) ? Camera.main.orthographicSize : 9f;
            float expected = expectedOrtho * 2f * Constants.SegStageVerticalScreenCount;
            Assert.AreEqual(expected, target, 0.001f);
        }
    }
}
