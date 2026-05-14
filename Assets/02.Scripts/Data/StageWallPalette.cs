using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 액트별 벽 sprite 매핑. Resources/StageWallPalette.asset 에 단일 인스턴스.
    /// StageRuntimeBuilder 가 액트 기반으로 sprite 선택해 외곽 벽 생성.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Wall Palette", fileName = "StageWallPalette")]
    public class StageWallPalette : ScriptableObject
    {
        public Sprite common;
        public Sprite spring;
        public Sprite summer;
        public Sprite autumn;
        public Sprite winter;

        public Sprite GetForAct(ActId act)
        {
            switch (act)
            {
                case ActId.Act1_Spring: return spring != null ? spring : common;
                case ActId.Act2_Summer: return summer != null ? summer : common;
                case ActId.Act3_Autumn: return autumn != null ? autumn : common;
                case ActId.Act4_Winter: return winter != null ? winter : common;
                default: return common;
            }
        }
    }
}
