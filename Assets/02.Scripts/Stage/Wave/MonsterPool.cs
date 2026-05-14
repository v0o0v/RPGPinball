using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Wave
{
    /// <summary>
    /// 일반 몬스터 SO 풀 (보스/엘리트 제외).
    /// Resources/Monsters/* 에서 MonsterData 로드.
    /// </summary>
    public class MonsterPool
    {
        private static MonsterPool instance;
        public static MonsterPool Instance => instance ??= new MonsterPool();

        private readonly List<MonsterData> all = new();
        private bool loaded;

        public void EnsureLoaded()
        {
            if (loaded) return;
            all.Clear();
            var loaded2 = Resources.LoadAll<MonsterData>("Monsters");
            for (int i = 0; i < loaded2.Length; i++)
            {
                if (loaded2[i] != null && !loaded2[i].isBoss) all.Add(loaded2[i]);
            }
            loaded = true;
        }

        public void OverrideForTest(IEnumerable<MonsterData> data)
        {
            all.Clear();
            if (data != null) foreach (var m in data) if (m != null && !m.isBoss) all.Add(m);
            loaded = true;
        }

        public MonsterData FindByAssetName(string assetName)
        {
            EnsureLoaded();
            for (int i = 0; i < all.Count; i++)
                if (all[i].name == assetName) return all[i];
            return null;
        }

        public List<MonsterData> Filter(ActId actId, MonsterSizeCategory sizeCategory)
        {
            EnsureLoaded();
            var result = new List<MonsterData>();
            for (int i = 0; i < all.Count; i++)
            {
                var m = all[i];
                if (m.sizeCategory != sizeCategory) continue;
                if (m.themeOwner != ActId.None && m.themeOwner != actId) continue;
                result.Add(m);
            }
            return result;
        }
    }
}
