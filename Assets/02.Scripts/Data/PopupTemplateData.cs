using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 5종 팝업 템플릿 프리팹 매핑(코드 빌드 fallback 사용 가능). M8 정식 프리팹 교체 인계.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Popup Template Data", fileName = "PopupTemplateData")]
    public class PopupTemplateData : ScriptableObject
    {
        public GameObject confirmPrefab;
        public GameObject alertPrefab;
        public GameObject rewardPrefab;
        public GameObject guidePrefab;
        public GameObject settingsPrefab;
    }
}
