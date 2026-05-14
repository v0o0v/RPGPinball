using System;
using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// Act 4종 × {지형/경로/노드마커 6종} 슬롯 매핑. M7 §0-A.2 참조.
    /// 본 마일스톤은 슬롯 1차 매핑까지. 시각 검증 후 [Map_Tile_Index.md] 와 동기화 필요.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Map Tile Palette", fileName = "MapTilePalette")]
    public class MapTilePalette : ScriptableObject
    {
        public ActTilePack[] acts = new ActTilePack[4];

        [Serializable]
        public class ActTilePack
        {
            public ActId actId;
            public Sprite terrainBase;  // 풀밭/모래/황토/눈
            public Sprite pathTile;     // 흙길/바위/자갈/얼음
            public Sprite landmark;     // 액트별 보스 영역 표시
            // 노드 마커 6종
            public Sprite nodeNormal;
            public Sprite nodeElite;
            public Sprite nodeBoss;
            public Sprite nodeRest;
            public Sprite nodeEvent;
            public Sprite nodeHidden;
        }

        public ActTilePack GetPack(ActId actId)
        {
            if (acts == null) return null;
            foreach (var p in acts)
                if (p != null && p.actId == actId) return p;
            return null;
        }

        public bool IsFullyMapped()
        {
            if (acts == null || acts.Length < 4) return false;
            foreach (var p in acts)
            {
                if (p == null) return false;
                if (p.nodeNormal == null || p.nodeElite == null || p.nodeBoss == null
                    || p.nodeRest == null || p.nodeEvent == null || p.nodeHidden == null) return false;
            }
            return true;
        }
    }
}
