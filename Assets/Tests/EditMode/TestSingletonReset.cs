using UnityEngine;
using RPGPinball.Meta;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 6 매니저들의 Singleton 패턴 충돌 해결용 헬퍼.
    /// SetUp 첫줄에서 호출하여 이전 테스트/씬의 잔존 Instance 제거.
    /// </summary>
    public static class TestSingletonReset
    {
        public static void ClearAllManagers()
        {
            ClearByType<EconomyManager>();
            ClearByType<QuestManager>();
            ClearByType<ForgeManager>();
            ClearByType<EnchanterManager>();
            ClearByType<AstrologerManager>();
            ClearByType<CollectionManager>();
            ClearByType<TavernManager>();
            ClearByType<BalloonManager>();
            ClearByType<MercenaryManager>();
            ClearByType<TrainingManager>();

            EconomyManager.ResetInstance();
            QuestManager.ResetInstance();
            EnchanterManager.ResetInstance();
            AstrologerManager.ResetInstance();
            CollectionManager.ResetInstance();
            TavernManager.ResetInstance();
        }

        private static void ClearByType<T>() where T : Component
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (c != null && c.gameObject != null) Object.DestroyImmediate(c.gameObject);
            }
        }
    }
}
