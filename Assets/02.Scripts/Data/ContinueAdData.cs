using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 광고 ID (AdMob/UnityAds) 플레이스홀더 + 일일 한도. M8 광고 SDK 통합 시 본 구현.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Continue Ad Data", fileName = "ContinueAdData")]
    public class ContinueAdData : ScriptableObject
    {
        public string admobUnitIdAndroid = "ca-app-pub-3940256099942544/5224354917"; // 테스트 ID
        public string admobUnitIdIos = "ca-app-pub-3940256099942544/1712485313";       // 테스트 ID
        public int dailyLimit = 3;
        public float timeBonusSec = 30f;
        public int manaRestore = 100;
    }
}
