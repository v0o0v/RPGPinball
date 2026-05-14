using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 기믹 SO 풀. Resources/Gimmicks/* 에서 모든 GimmickData 로드.
    /// 충돌 방지 / 시너지는 GimmickData 필드에서 양방향 자동 적용.
    /// </summary>
    public class GimmickPool
    {
        private static GimmickPool instance;
        public static GimmickPool Instance => instance ??= new GimmickPool();

        private readonly List<GimmickData> all = new();
        private readonly Dictionary<GimmickId, GimmickData> byId = new();
        private bool loaded;

        public IReadOnlyList<GimmickData> All
        {
            get { EnsureLoaded(); return all; }
        }

        public void EnsureLoaded()
        {
            if (loaded) return;
            all.Clear(); byId.Clear();

            var loadedAssets = Resources.LoadAll<GimmickData>("Gimmicks");
            foreach (var g in loadedAssets) Register(g);
            loaded = true;
        }

        public void OverrideForTest(IEnumerable<GimmickData> data)
        {
            all.Clear(); byId.Clear();
            if (data != null) foreach (var g in data) Register(g);
            loaded = true;
        }

        private void Register(GimmickData g)
        {
            if (g == null) return;
            all.Add(g);
            if (g.gimmickId != GimmickId.None && !byId.ContainsKey(g.gimmickId))
                byId.Add(g.gimmickId, g);
        }

        public GimmickData Get(GimmickId id)
        {
            EnsureLoaded();
            return byId.TryGetValue(id, out var v) ? v : null;
        }

        /// <summary>
        /// 절차 생성 후보군. 테마 필터 + bossOnly 제외 + placement flags 일치.
        /// </summary>
        public List<GimmickData> GetCandidates(ActId actId, GimmickPlacement allowedPlacements, bool excludeBossOnly = true)
        {
            EnsureLoaded();
            var result = new List<GimmickData>();
            for (int i = 0; i < all.Count; i++)
            {
                var g = all[i];
                if (g == null) continue;
                if (excludeBossOnly && g.bossOnly) continue;
                if ((g.placement & allowedPlacements) == 0) continue;
                if (g.themeOwner != ActId.None && g.themeOwner != actId) continue;
                result.Add(g);
            }
            return result;
        }

        /// <summary>
        /// 두 기믹의 충돌 여부. data 모두에서 conflictingIds 양방향 검사.
        /// </summary>
        public static bool HasConflict(GimmickData a, GimmickData b)
        {
            if (a == null || b == null) return false;
            if (Contains(a.conflictingIds, b.gimmickId)) return true;
            if (Contains(b.conflictingIds, a.gimmickId)) return true;
            return false;
        }

        public static bool HasSynergy(GimmickData a, GimmickData b)
        {
            if (a == null || b == null) return false;
            if (Contains(a.synergyIds, b.gimmickId)) return true;
            if (Contains(b.synergyIds, a.gimmickId)) return true;
            return false;
        }

        private static bool Contains(GimmickId[] arr, GimmickId target)
        {
            if (arr == null) return false;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == target) return true;
            return false;
        }
    }
}
