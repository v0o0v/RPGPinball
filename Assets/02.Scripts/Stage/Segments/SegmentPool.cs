using System.Collections.Generic;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;
using UnityEngine;

namespace RPGPinball.Stage.Segments
{
    /// <summary>
    /// 세그먼트 SO 풀. Resources/Segments/* 에서 모든 SegmentData를 로드.
    /// 직전 스테이지에서 사용된 세그먼트는 가중치 ×0.5 (중복 방지).
    /// </summary>
    public class SegmentPool
    {
        private static SegmentPool instance;
        public static SegmentPool Instance => instance ??= new SegmentPool();

        private readonly List<SegmentData> top = new();
        private readonly List<SegmentData> middle = new();
        private readonly List<SegmentData> bottom = new();

        // 직전 스테이지에서 사용된 세그먼트 ID (가중치 디케이)
        private readonly HashSet<string> previouslyUsedIds = new();

        private bool loaded;

        public bool HasAny => top.Count + middle.Count + bottom.Count > 0;

        public void EnsureLoaded()
        {
            if (loaded) return;

            top.Clear();
            middle.Clear();
            bottom.Clear();

            var all = Resources.LoadAll<SegmentData>("Segments");
            for (int i = 0; i < all.Length; i++)
            {
                switch (all[i].slot)
                {
                    case SegmentSlot.Top: top.Add(all[i]); break;
                    case SegmentSlot.Middle: middle.Add(all[i]); break;
                    case SegmentSlot.Bottom: bottom.Add(all[i]); break;
                }
            }
            loaded = true;
        }

        /// <summary>
        /// 외부에서 풀을 강제 주입 (EditMode 테스트용 / Resources 우회).
        /// </summary>
        public void OverrideForTest(IEnumerable<SegmentData> all)
        {
            top.Clear(); middle.Clear(); bottom.Clear();
            previouslyUsedIds.Clear();
            foreach (var s in all)
            {
                if (s == null) continue;
                switch (s.slot)
                {
                    case SegmentSlot.Top: top.Add(s); break;
                    case SegmentSlot.Middle: middle.Add(s); break;
                    case SegmentSlot.Bottom: bottom.Add(s); break;
                }
            }
            loaded = true;
        }

        public SegmentData PickTop(ActId actId, DeterministicRng rng)
        {
            EnsureLoaded();
            return PickFromList(top, actId, rng);
        }

        public SegmentData PickBottom()
        {
            EnsureLoaded();
            return bottom.Count > 0 ? bottom[0] : null;
        }

        public List<SegmentData> PickMiddles(ActId actId, DeterministicRng rng, int count)
        {
            EnsureLoaded();
            var result = new List<SegmentData>(count);
            // 한 스테이지 내에서도 동일 세그먼트가 연속 추첨되지 않도록 휘발성 카피
            var usedThisStage = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                var s = PickFromList(middle, actId, rng, usedThisStage);
                if (s == null) break;
                result.Add(s);
                if (!string.IsNullOrEmpty(s.segmentId)) usedThisStage.Add(s.segmentId);
            }
            return result;
        }

        /// <summary>
        /// 빌더가 빌드 완료 시 호출 — 다음 스테이지에서 가중치 ×0.5 적용.
        /// </summary>
        public void MarkUsed(SegmentLayout layout)
        {
            previouslyUsedIds.Clear();
            if (layout == null) return;
            if (layout.top != null && !string.IsNullOrEmpty(layout.top.segmentId)) previouslyUsedIds.Add(layout.top.segmentId);
            if (layout.bottom != null && !string.IsNullOrEmpty(layout.bottom.segmentId)) previouslyUsedIds.Add(layout.bottom.segmentId);
            for (int i = 0; i < layout.middles.Count; i++)
            {
                var m = layout.middles[i];
                if (m != null && !string.IsNullOrEmpty(m.segmentId)) previouslyUsedIds.Add(m.segmentId);
            }
        }

        public void ClearUsageHistory() => previouslyUsedIds.Clear();

        private SegmentData PickFromList(List<SegmentData> source, ActId actId, DeterministicRng rng, HashSet<string> exclude = null)
        {
            if (source.Count == 0) return null;
            var entries = new List<(SegmentData, float)>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var s = source[i];
                if (exclude != null && !string.IsNullOrEmpty(s.segmentId) && exclude.Contains(s.segmentId)) continue;

                // 테마 필터 — 공통(None) 또는 같은 액트만 허용
                if (s.theme != ActId.None && s.theme != actId) continue;

                float weight = 1f;
                if (s.theme == actId && actId != ActId.None) weight *= 2f; // 테마 가중치
                if (!string.IsNullOrEmpty(s.segmentId) && previouslyUsedIds.Contains(s.segmentId)) weight *= 0.5f;
                entries.Add((s, weight));
            }
            if (entries.Count == 0) return null;
            return rng.WeightedPick(entries);
        }
    }
}
