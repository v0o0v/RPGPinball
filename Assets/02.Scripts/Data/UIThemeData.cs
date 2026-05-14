using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// UI 컬러 팔레트 + 폰트 사이즈 + 9-Slice 마진. 모든 위젯이 참조.
    /// M8 다크 모드 추가 시 1곳만 수정 가능하도록 SO 분리.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/UI Theme Data", fileName = "UIThemeData")]
    public class UIThemeData : ScriptableObject
    {
        [Header("컬러")]
        public Color primary = new Color(0.29f, 0.56f, 0.89f);   // 파랑
        public Color secondary = new Color(0.85f, 0.55f, 0.20f);  // 주황
        public Color success = new Color(0.30f, 0.69f, 0.31f);    // 초록
        public Color warning = new Color(0.95f, 0.77f, 0.25f);    // 노랑
        public Color danger = new Color(0.85f, 0.20f, 0.20f);     // 빨강
        public Color textMain = new Color(0.20f, 0.18f, 0.15f);
        public Color textInverse = Color.white;

        [Header("폰트 사이즈 (6단계)")]
        public int fontSizeXXL = 100;
        public int fontSizeXL = 72;
        public int fontSizeL = 56;
        public int fontSizeM = 40;
        public int fontSizeS = 32;
        public int fontSizeXS = 24;

        [Header("9-Slice 마진")]
        public int sliceBorderColored = 6;
        public int sliceBorderAncient = 8;
        public int sliceBorderOutline = 4;

        [Header("애니메이션")]
        public float popupFadeIn = 0.25f;
        public float popupFadeOut = 0.20f;
    }
}
