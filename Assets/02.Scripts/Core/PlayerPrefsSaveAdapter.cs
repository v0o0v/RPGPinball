using UnityEngine;

namespace RPGPinball.Core
{
    /// <summary>
    /// 마일스톤 6 임시 SaveSystem 어댑터. JsonUtility + PlayerPrefs 기반.
    /// 마일스톤 7에서 AES-256 + HMAC 정식 SaveSystem 도입 후 마이그레이션.
    ///
    /// 키 네임스페이스:
    ///   RPGPinball.Player.*        — 프로필/성장/재화
    ///   RPGPinball.Inventory.*     — 룬/타로/소모품 인벤토리
    ///   RPGPinball.Village.*       — 시설 상태(코어 레벨, 플리퍼 레벨, 파생형 등)
    ///   RPGPinball.Collection.*    — 도감 카운트
    ///   RPGPinball.Quests.*        — 의뢰 상태
    /// </summary>
    public static class PlayerPrefsSaveAdapter
    {
        public const string KeyPrefixPlayer = "RPGPinball.Player.";
        public const string KeyPrefixInventory = "RPGPinball.Inventory.";
        public const string KeyPrefixVillage = "RPGPinball.Village.";
        public const string KeyPrefixCollection = "RPGPinball.Collection.";
        public const string KeyPrefixQuests = "RPGPinball.Quests.";

        public static void Save<T>(string key, T data) where T : class
        {
            if (string.IsNullOrEmpty(key) || data == null) return;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public static T Load<T>(string key) where T : class, new()
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!PlayerPrefs.HasKey(key)) return null;
            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void Delete(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
        }

        public static bool Has(string key) => PlayerPrefs.HasKey(key);
    }
}
