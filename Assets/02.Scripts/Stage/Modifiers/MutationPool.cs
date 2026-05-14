using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// 5종 돌연변이 SO 풀. Resources/Mutations/* 에서 로드.
    /// </summary>
    public class MutationPool
    {
        private static MutationPool instance;
        public static MutationPool Instance => instance ??= new MutationPool();

        private readonly List<MutationData> all = new();
        private readonly Dictionary<MutationId, MutationData> byId = new();
        private bool loaded;

        public IReadOnlyList<MutationData> All
        {
            get { EnsureLoaded(); return all; }
        }

        public void EnsureLoaded()
        {
            if (loaded) return;
            all.Clear(); byId.Clear();
            var loaded2 = Resources.LoadAll<MutationData>("Mutations");
            for (int i = 0; i < loaded2.Length; i++)
                if (loaded2[i] != null) Register(loaded2[i]);
            loaded = true;
        }

        public void OverrideForTest(IEnumerable<MutationData> data)
        {
            all.Clear(); byId.Clear();
            if (data != null) foreach (var m in data) if (m != null) Register(m);
            loaded = true;
        }

        public MutationData Get(MutationId id)
        {
            EnsureLoaded();
            return byId.TryGetValue(id, out var v) ? v : null;
        }

        private void Register(MutationData m)
        {
            all.Add(m);
            if (!byId.ContainsKey(m.mutationId)) byId.Add(m.mutationId, m);
        }
    }
}
