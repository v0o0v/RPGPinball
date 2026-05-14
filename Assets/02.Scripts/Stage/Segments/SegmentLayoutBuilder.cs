using System.Collections.Generic;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;
using UnityEngine;

namespace RPGPinball.Stage.Segments
{
    /// <summary>
    /// 세그먼트 레이아웃 빌더. band별 중단 수 결정 + 세로 합 = 화면 세로 × 3 강제.
    /// 우선순위: ① 중단 height 변형 ② 중단 개수 ±1 ③ 재추첨.
    /// </summary>
    public static class SegmentLayoutBuilder
    {
        /// <summary>
        /// 카메라 시야 세로 × Constants.SegStageVerticalScreenCount 를 계산.
        /// Camera.main 없으면 fallback OrthoSize=9 사용.
        /// </summary>
        public static float ComputeTargetStageHeight()
        {
            float orthoSize = 9f;
            if (Camera.main != null && Camera.main.orthographic)
                orthoSize = Camera.main.orthographicSize;
            return orthoSize * 2f * Constants.SegStageVerticalScreenCount;
        }

        public static SegmentLayout Build(ActId actId, DifficultyBand band, DeterministicRng rng, SegmentPool pool)
        {
            var layout = new SegmentLayout
            {
                targetStageHeight = ComputeTargetStageHeight()
            };

            // 상단·하단 픽
            layout.top = pool.PickTop(actId, rng);
            layout.bottom = pool.PickBottom();
            layout.topHeight = layout.top != null ? layout.top.height : Constants.SegTopHeightDefault;
            layout.bottomHeight = layout.bottom != null ? layout.bottom.height : Constants.SegBottomHeightDefault;

            // band별 중단 개수
            int middleCount = ResolveMiddleCount(band, rng);

            // 2026-05-14: 핀볼 정석 구역화 — middle 은 정확히 3개 (PinballZone 컨벤션 의존).
            // height 매칭 실패해도 ±1 fallback 금지. 대신 재추첨·강제 fit 으로 3개 유지.
            if (TryFitMiddles(actId, rng, pool, layout, middleCount)) return Finalize(layout, pool);

            // 2차: 다른 세그먼트로 재추첨 (Pool 가중치 디케이 활용)
            pool.MarkUsed(layout);
            if (TryFitMiddles(actId, rng, pool, layout, middleCount)) return Finalize(layout, pool);

            // 3차: 강제 fit — heightMin/Max 클램프를 무시하고 균등 분배 (시각적으로 살짝 어색해도 zone 컨벤션 우선)
            ForceFitMiddles(actId, rng, pool, layout, middleCount);
            return Finalize(layout, pool);
        }

        private static int ResolveMiddleCount(DifficultyBand band, DeterministicRng rng)
        {
            // 2026-05-14 핀볼 정석 구역화: middle = 정확히 3 (SlingshotZone / MidPlay / BumperCluster).
            // PinballZoneMap.SegmentIndexFor 가 이 컨벤션에 의존하므로 변경 불가.
            return 3;
        }

        private static bool TryFitMiddles(ActId actId, DeterministicRng rng, SegmentPool pool, SegmentLayout layout, int count)
        {
            layout.middles.Clear();
            layout.middleHeights.Clear();
            var middles = pool.PickMiddles(actId, rng, count);
            if (middles.Count != count) return false;

            float remaining = layout.targetStageHeight - layout.topHeight - layout.bottomHeight;
            if (remaining <= 0f) return false;

            // 균등 분배 후 heightMin/Max 범위로 클램프
            float targetEach = remaining / count;
            float allocated = 0f;
            for (int i = 0; i < count; i++)
            {
                var seg = middles[i];
                float h = Mathf.Clamp(targetEach, seg.heightMin, seg.heightMax);
                layout.middles.Add(seg);
                layout.middleHeights.Add(h);
                allocated += h;
            }

            // 잔여 오차를 비례 재분배 (heightMin/Max 클램프 재적용)
            float gap = remaining - allocated;
            if (Mathf.Abs(gap) > Constants.SegHeightTolerance)
            {
                bool adjusted = TryRedistribute(layout, gap);
                if (!adjusted) return false;
            }

            layout.totalHeight = layout.topHeight + layout.bottomHeight;
            for (int i = 0; i < layout.middleHeights.Count; i++)
                layout.totalHeight += layout.middleHeights[i];

            return layout.IsHeightWithinTolerance(Constants.SegHeightTolerance);
        }

        /// <summary>
        /// 누적 오차를 각 중단에 비례 분배. 클램프 후 한계치 도달 시 false.
        /// </summary>
        private static bool TryRedistribute(SegmentLayout layout, float gap)
        {
            for (int pass = 0; pass < 4 && Mathf.Abs(gap) > Constants.SegHeightTolerance; pass++)
            {
                int n = layout.middles.Count;
                float share = gap / n;
                float consumed = 0f;
                for (int i = 0; i < n; i++)
                {
                    float curr = layout.middleHeights[i];
                    float desired = curr + share;
                    float clamped = Mathf.Clamp(desired, layout.middles[i].heightMin, layout.middles[i].heightMax);
                    consumed += (clamped - curr);
                    layout.middleHeights[i] = clamped;
                }
                gap -= consumed;
                if (Mathf.Abs(consumed) < 0.001f) break; // 모두 클램프에 닿음
            }
            return Mathf.Abs(gap) <= Constants.SegHeightTolerance;
        }

        /// <summary>
        /// heightMin/Max 클램프 무시한 강제 균등 분배. 마지막 fallback.
        /// middle 세그먼트가 정확히 count 개로 들어오는 것을 보장.
        /// </summary>
        private static void ForceFitMiddles(ActId actId, DeterministicRng rng, SegmentPool pool, SegmentLayout layout, int count)
        {
            layout.middles.Clear();
            layout.middleHeights.Clear();
            var middles = pool.PickMiddles(actId, rng, count);
            // pool 이 부족하면 mid 풀에서 채워넣기 (중복 허용)
            while (middles.Count < count && pool != null)
            {
                var fallback = pool.PickMiddles(actId, rng, 1);
                if (fallback.Count == 0) break;
                middles.Add(fallback[0]);
            }
            float remaining = layout.targetStageHeight - layout.topHeight - layout.bottomHeight;
            float each = Mathf.Max(2f, remaining / Mathf.Max(1, middles.Count));
            for (int i = 0; i < middles.Count; i++)
            {
                layout.middles.Add(middles[i]);
                layout.middleHeights.Add(each);
            }
            layout.totalHeight = layout.topHeight + layout.bottomHeight + each * middles.Count;
        }

        private static SegmentLayout Finalize(SegmentLayout layout, SegmentPool pool)
        {
            // 마지막 정합: 연결 통로 검증은 후속 로직(StageRuntimeBuilder)에서 진행하는 게 안전.
            // 빌더는 데이터 정합성만 보장.
            layout.totalHeight = layout.topHeight + layout.bottomHeight;
            for (int i = 0; i < layout.middleHeights.Count; i++)
                layout.totalHeight += layout.middleHeights[i];
            pool.MarkUsed(layout);
            return layout;
        }
    }
}
