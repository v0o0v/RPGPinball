using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage
{
    /// <summary>
    /// 고정 이정표(stage 5/10/15/20/25/30) 분기 로직.
    /// 5/15/25 = 휴식(70%) 또는 이벤트(30%)
    /// 10/20   = 중간 보스
    /// 30      = 최종 보스
    /// Act 1 1~3 = 튜토리얼 (M8 강제 콘텐츠)
    /// </summary>
    public static class MilestoneManager
    {
        /// <summary>
        /// 시드 기반으로 이정표 노드 종류 결정. null이면 절차 생성 대상.
        /// </summary>
        public static NodeKind? GetNodeKind(ActId actId, int stageIndex, DeterministicRng rng)
        {
            // Act 1 1~3은 튜토리얼 (강제)
            if (actId == ActId.Act1_Spring && stageIndex >= 1 && stageIndex <= 3)
                return NodeKind.Tutorial;

            // 5/15/25 휴식·이벤트
            if (stageIndex == 5 || stageIndex == 15 || stageIndex == 25)
                return rng.RollChance(0.7f) ? NodeKind.Rest : NodeKind.Event;

            // 10/20/30 보스
            if (stageIndex == 10 || stageIndex == 20 || stageIndex == 30)
                return NodeKind.Boss;

            return null;
        }

        public static bool IsMilestone(int stageIndex)
        {
            return stageIndex == 5 || stageIndex == 10 || stageIndex == 15 ||
                   stageIndex == 20 || stageIndex == 25 || stageIndex == 30;
        }

        public static BossId? GetMilestoneBossId(ActId actId, int stageIndex)
        {
            switch (actId)
            {
                case ActId.Act1_Spring:
                    if (stageIndex == 10) return BossId.Act1_FleshPlant;
                    if (stageIndex == 20) return BossId.Act1_FallenFairy;
                    if (stageIndex == 30) return BossId.Act1_WorldTreeSpirit;
                    break;
                case ActId.Act2_Summer:
                    if (stageIndex == 10) return BossId.Act2_ArmoredCrab;
                    if (stageIndex == 20) return BossId.Act2_PirateGhost;
                    if (stageIndex == 30) return BossId.Act2_Kraken;
                    break;
                case ActId.Act3_Autumn:
                    if (stageIndex == 10) return BossId.Act3_MadInventor;
                    if (stageIndex == 20) return BossId.Act3_PumpkinGhost;
                    if (stageIndex == 30) return BossId.Act3_ClockworkDragon;
                    break;
                case ActId.Act4_Winter:
                    if (stageIndex == 10) return BossId.Act4_FrostGiant;
                    if (stageIndex == 20) return BossId.Act4_ClockTowerSentinel;
                    if (stageIndex == 30) return BossId.Act4_WinterQueen;
                    break;
            }
            return null;
        }
    }
}
