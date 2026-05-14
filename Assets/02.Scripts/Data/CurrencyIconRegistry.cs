using System.Collections.Generic;
using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 10종 통화 ↔ Sprite/Tint 단일 참조점. EconomyManager / HUD / 결과 화면이 모두 본 SO 를 조회.
    /// §0-A.4 Kenney 매핑.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Currency Icon Registry", fileName = "CurrencyIconRegistry")]
    public class CurrencyIconRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public CurrencyId id;
            public Sprite icon;
            public Color tint;
        }

        public Entry[] entries;

        private Dictionary<CurrencyId, Entry> cache;

        public bool TryGetIcon(CurrencyId id, out Sprite sprite, out Color tint)
        {
            EnsureCache();
            if (cache.TryGetValue(id, out var e))
            {
                sprite = e.icon;
                tint = e.tint;
                return true;
            }
            sprite = null;
            tint = Color.white;
            return false;
        }

        private void EnsureCache()
        {
            if (cache != null) return;
            cache = new Dictionary<CurrencyId, Entry>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e.id == CurrencyId.None) continue;
                cache[e.id] = e;
            }
        }

        private void OnEnable() { cache = null; }
        private void OnValidate() { cache = null; }
    }
}
