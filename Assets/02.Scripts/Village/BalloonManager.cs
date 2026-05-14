using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// 열기구 선착장 — 3단계 개조 (시작 마나 +20 / 보스 시간 +10 / 히든 노드 확률 +15%).
    /// ActMap 진입 시 GameManager 가 본 매니저를 조회해 일회성 버프 적용.
    /// </summary>
    public class BalloonManager : MonoBehaviour
    {
        public static BalloonManager Instance { get; private set; }

        [SerializeField] private BalloonUpgradeData balloonData;
        [SerializeField] private int currentUpgradeLevel; // 0~3

        public int CurrentUpgradeLevel => currentUpgradeLevel;

        public int StartingManaBonus => currentUpgradeLevel >= 1 ? Constants.BalloonStartingManaBonus : 0;
        public float BossStartingTimeBonus => currentUpgradeLevel >= 2 ? Constants.BalloonBossStartingTimeBonus : 0f;
        public float HiddenNodeChanceBonus => currentUpgradeLevel >= 3 ? Constants.BalloonHiddenChanceBonus : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDisable() { if (Instance == this) Instance = null; }

        public bool Upgrade()
        {
            if (currentUpgradeLevel >= 3) return false;
            if (balloonData == null || balloonData.stages == null || currentUpgradeLevel >= balloonData.stages.Length) return false;
            var s = balloonData.stages[currentUpgradeLevel];
            var econ = EconomyManager.Instance;
            if (econ == null) return false;
            if (s.goldCost > 0 && !econ.Has(CurrencyId.Gold, s.goldCost)) return false;
            if (s.manaCrystalCost > 0 && !econ.Has(CurrencyId.ManaCrystal, s.manaCrystalCost)) return false;

            if (s.goldCost > 0) econ.Spend(CurrencyId.Gold, s.goldCost, "BalloonUpgrade");
            if (s.manaCrystalCost > 0) econ.Spend(CurrencyId.ManaCrystal, s.manaCrystalCost, "BalloonUpgrade");

            currentUpgradeLevel++;
            EventBus.Publish(new OnBalloonUpgraded { NewLevel = currentUpgradeLevel, UpgradeId = s.upgradeId });
            return true;
        }
    }
}
