using RPGPinball.Data;

namespace RPGPinball.Stage.Nodes
{
    /// <summary>
    /// 보스 노드 — 10/20/30 고정 이정표.
    /// 실제 BossBase 인스턴스화 / BossFightContext.Enter는 M4 산출물이 처리.
    /// 마일스톤 5는 BossId 매핑만 담당 (MilestoneManager 위임).
    /// </summary>
    public static class BossNode
    {
        public static BossId? Resolve(ActId actId, int stageIndex)
            => MilestoneManager.GetMilestoneBossId(actId, stageIndex);
    }
}
