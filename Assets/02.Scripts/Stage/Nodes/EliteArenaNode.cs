using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Nodes
{
    /// <summary>
    /// 엘리트 전용 투기장 노드 — [M4 #1] 인계.
    /// ArenaLayoutData 로드 → 고정 세그먼트 + 잠금 기믹 + 금지 기믹.
    /// EliteBase 인스턴스화는 M4 산출물이 처리.
    /// </summary>
    public static class EliteArenaNode
    {
        /// <summary>
        /// 입장 조건 — 해당 액트 최종 보스 처치 여부.
        /// 마일스톤 5는 스텁(true) — 마일스톤 6 TavernManager + PlayerData.clearedBossIds 도입 시 본격화.
        /// </summary>
        public static bool CanEnter(EliteId eliteId, object playerData)
        {
            // M6 인계: playerData를 PlayerData로 캐스팅해서 clearedBossIds 검증.
            return true;
        }

        public static ArenaLayoutData Load(EliteId eliteId)
        {
            var all = Resources.LoadAll<ArenaLayoutData>("Arenas");
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].eliteId == eliteId) return all[i];
            return null;
        }
    }
}
