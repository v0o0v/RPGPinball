using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 노드 유형별 미클리어/클리어 아이콘 + 클릭음 + 강조 색. ActMapUI 가 참조.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/ActMap Node Data", fileName = "ActMapNodeData")]
    public class ActMapNodeData : ScriptableObject
    {
        public NodeKind kind;
        public Sprite iconLocked;
        public Sprite iconUncleared;
        public Sprite iconClearedSilver;
        public Sprite iconClearedGold;
        public Sprite iconClearedBest;
        public AudioClip clickSound;
        public Color accentColor = Color.white;
    }
}
