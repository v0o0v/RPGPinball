using UnityEngine;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// Village 씬 진입 시 모든 매니저 싱글톤이 활성 상태인지 확인하고
    /// QuestManager 갱신 트리거. M7 SaveSystem 도입 전까지 PlayerPrefs 어댑터로 로드.
    /// </summary>
    public class VillageBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject managersRoot;

        private void Start()
        {
            EnsureManager<EconomyManager>(nameof(EconomyManager));
            EnsureManager<QuestManager>(nameof(QuestManager));
            EnsureManager<ForgeManager>(nameof(ForgeManager));
            EnsureManager<EnchanterManager>(nameof(EnchanterManager));
            EnsureManager<AstrologerManager>(nameof(AstrologerManager));
            EnsureManager<CollectionManager>(nameof(CollectionManager));
            EnsureManager<TavernManager>(nameof(TavernManager));
            EnsureManager<BalloonManager>(nameof(BalloonManager));
            EnsureManager<MercenaryManager>(nameof(MercenaryManager));
            EnsureManager<TrainingManager>(nameof(TrainingManager));

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.RefreshDailyIfExpired();
                QuestManager.Instance.RefreshWeeklyIfExpired();
                QuestManager.Instance.RefreshBountyIfExpired();
            }
        }

        private T EnsureManager<T>(string name) where T : MonoBehaviour
        {
            var existing = FindFirstObjectByType<T>();
            if (existing != null) return existing;
            // 없으면 managersRoot 하위에 자동 생성 (디버그용)
            var parent = managersRoot != null ? managersRoot.transform : transform;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
