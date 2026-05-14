using RPGPinball.Combat;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Nodes
{
    /// <summary>
    /// 이벤트 노드 — 4종 중 1종.
    /// SuspiciousTraveler/TreasureRoom/MysticAltar/WanderersGamble.
    /// </summary>
    public static class EventNode
    {
        public static EventNodeKind Pick(DeterministicRng rng) => (EventNodeKind)rng.NextInt(0, 4);

        public static void Apply(EventNodeKind kind, DeterministicRng rng, StageBlueprint blueprint)
        {
            blueprint.eventNodeKind = kind;
            switch (kind)
            {
                case EventNodeKind.SuspiciousTraveler:
                    blueprint.eventOutcomeId = rng.RollChance(0.5f) ? "next_monster_hp_minus_20" : "next_reward_double";
                    break;
                case EventNodeKind.TreasureRoom:
                    blueprint.eventOutcomeId = "treasure_3_chests";
                    // 골드 즉시 지급은 M6 EconomyManager에서.
                    break;
                case EventNodeKind.MysticAltar:
                    // 시간 -30초 ↔ SP +1 선택. M7 UI에서 선택, M5는 SP +1 기본 적용 + 시간 -30초.
                    if (StageTimer.Instance != null) StageTimer.Instance.Penalize(30f);
                    if (LevelSystem.Instance != null) LevelSystem.Instance.AwardBonusSP(1);
                    blueprint.eventOutcomeId = "altar_sp_plus_1_time_minus_30";
                    break;
                case EventNodeKind.WanderersGamble:
                    blueprint.eventOutcomeId = rng.RollChance(0.5f) ? "gamble_jackpot" : "gamble_lose";
                    break;
            }
        }
    }
}
