using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// 18종 스테이지 모디파이어 SO 풀. Resources/Modifiers/* 에서 로드.
    /// </summary>
    public class ModifierPool
    {
        private static ModifierPool instance;
        public static ModifierPool Instance => instance ??= new ModifierPool();

        private readonly List<StageModifierData> all = new();
        private readonly Dictionary<ModifierId, StageModifierData> byId = new();
        private bool loaded;

        public IReadOnlyList<StageModifierData> All
        {
            get { EnsureLoaded(); return all; }
        }

        public void EnsureLoaded()
        {
            if (loaded) return;
            all.Clear(); byId.Clear();
            var loaded2 = Resources.LoadAll<StageModifierData>("Modifiers");
            for (int i = 0; i < loaded2.Length; i++)
            {
                if (loaded2[i] != null) Register(loaded2[i]);
            }
            loaded = true;
        }

        public void OverrideForTest(IEnumerable<StageModifierData> data)
        {
            all.Clear(); byId.Clear();
            if (data != null) foreach (var m in data) if (m != null) Register(m);
            loaded = true;
        }

        public StageModifierData Get(ModifierId id)
        {
            EnsureLoaded();
            return byId.TryGetValue(id, out var v) ? v : null;
        }

        private void Register(StageModifierData m)
        {
            all.Add(m);
            if (!byId.ContainsKey(m.modifierId)) byId.Add(m.modifierId, m);
        }
    }
}
