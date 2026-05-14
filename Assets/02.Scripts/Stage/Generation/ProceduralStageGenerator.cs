using System.Collections.Generic;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Modifiers;
using RPGPinball.Stage.Nodes;
using RPGPinball.Stage.Segments;
using RPGPinball.Stage.Wave;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 절차 생성 메인 진입점. Procedural_Stage_Gen.md §10 10단계 파이프라인.
    /// </summary>
    public static class ProceduralStageGenerator
    {
        /// <summary>
        /// 직전 스테이지의 기믹 ID (가중치 디케이용 — 외부에서 주입).
        /// 마일스톤 7 SaveSystem 도입 시 PlayerData에 영구 저장 인계.
        /// </summary>
        public static HashSet<GimmickId> PreviousStageGimmickIds = new();

        public static StageBlueprint Generate(ActId actId, int stageIndex, ulong seed)
        {
            var rng = new DeterministicRng(seed);
            var blueprint = new StageBlueprint
            {
                seed = seed,
                actId = actId,
                stageIndex = stageIndex,
                band = DifficultyBudget.BandFor(stageIndex),
                timeLimitSeconds = Constants.StageDefaultTime
            };

            // 1. 고정 이정표 검사
            var milestoneKind = MilestoneManager.GetNodeKind(actId, stageIndex, rng);
            if (milestoneKind.HasValue)
            {
                blueprint.nodeKind = milestoneKind.Value;
                FillMilestone(blueprint, rng);
                return blueprint;
            }

            // 2. (rng는 이미 생성됨)

            // 3. 난이도 예산 + 권장 레벨
            blueprint.finalBudget = DifficultyBudget.ComputeFinalBudget(stageIndex, actId, rng);
            blueprint.recommendedLevel = DifficultyBudget.ComputeRecommendedLevel(stageIndex, actId);

            // 4. 세그먼트 조합 (세로 합 = 화면 세로 × 3 강제)
            var segLayout = SegmentLayoutBuilder.Build(actId, blueprint.band, rng, SegmentPool.Instance);
            blueprint.targetStageHeight = segLayout.targetStageHeight;
            blueprint.topSegmentId = segLayout.top != null ? segLayout.top.segmentId : null;
            blueprint.bottomSegmentId = segLayout.bottom != null ? segLayout.bottom.segmentId : null;
            blueprint.middleSegmentIds.Clear();
            for (int i = 0; i < segLayout.middles.Count; i++)
                blueprint.middleSegmentIds.Add(segLayout.middles[i] != null ? segLayout.middles[i].segmentId : null);

            // 5. 기믹 풀에서 추출
            var budget = new BudgetConsumer(blueprint.finalBudget);
            blueprint.gimmickPlacements = GimmickSelector.Select(
                actId, blueprint.band, budget, rng, GimmickPool.Instance,
                PreviousStageGimmickIds, segLayout.middles.Count);

            // 6. 몬스터 웨이브 생성
            blueprint.waves = WaveBuilder.Build(actId, stageIndex, rng, MonsterPool.Instance);

            // 7. 돌연변이 판정 (5%)
            blueprint.mutationId = MutationPicker.TryPick(blueprint.band, rng, MutationPool.Instance);
            MutationApplier.ApplyToBlueprint(blueprint, MutationPool.Instance);

            // 8. 스테이지 특성 (band 기반)
            blueprint.modifierIds = ModifierPicker.Pick(actId, blueprint.band, rng, ModifierPool.Instance);

            // 9. 노드 종류 — 절차 생성 결과는 기본 일반 전투
            blueprint.nodeKind = NodeKind.NormalBattle;
            // 5% 히든 변환 (열기구 +15% 보너스는 M6 인계)
            if (rng.RollChance(0.05f)) blueprint.nodeKind = NodeKind.Hidden;
            // 엘리트 변환 — 절차 생성 노드 중 일부를 EliteBattle로 (10% 목표 비율)
            else if (rng.RollChance(0.1f)) blueprint.nodeKind = NodeKind.EliteBattle;

            // 10. 직렬화 결과 반환
            UpdatePreviousIds(blueprint);
            return blueprint;
        }

        /// <summary>
        /// 같은 PlayerUID + 같은 날짜 + 같은 액트·스테이지 → 같은 시드.
        /// </summary>
        public static StageBlueprint GenerateFirstEntry(string playerUid, ActId actId, int stageIndex)
        {
            ulong seed = StageSeedFactory.BuildSeed(playerUid, StageSeedFactory.NowKst(), actId, stageIndex);
            return Generate(actId, stageIndex, seed);
        }

        public static StageBlueprint GenerateRetry(ulong baseSeed, int retryCount, ActId actId, int stageIndex)
        {
            ulong seed = StageSeedFactory.BuildSeedForRetry(baseSeed, retryCount);
            return Generate(actId, stageIndex, seed);
        }

        public static StageBlueprint GenerateDailyChallenge(ActId actId, int stageIndex)
        {
            ulong seed = StageSeedFactory.BuildDailyChallengeSeed(StageSeedFactory.NowKst(), actId, stageIndex);
            return Generate(actId, stageIndex, seed);
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────

        private static void FillMilestone(StageBlueprint blueprint, DeterministicRng rng)
        {
            switch (blueprint.nodeKind)
            {
                case NodeKind.Tutorial:
                    // M8 TutorialManager가 강제 콘텐츠 채움 — 마일스톤 5는 자리표시.
                    blueprint.recommendedLevel = blueprint.stageIndex;
                    break;
                case NodeKind.Rest:
                    // 휴식 — 효과는 노드 진입 시 RestNode.Apply 호출로.
                    break;
                case NodeKind.Event:
                    var kind = EventNode.Pick(rng);
                    EventNode.Apply(kind, rng, blueprint);
                    break;
                case NodeKind.Boss:
                    // BossBase 인스턴스화는 후속 단계(StageRuntimeBuilder 또는 외부 호출자)에서.
                    blueprint.recommendedLevel = DifficultyBudget.ComputeRecommendedLevel(blueprint.stageIndex, blueprint.actId);
                    break;
            }
        }

        private static void UpdatePreviousIds(StageBlueprint blueprint)
        {
            PreviousStageGimmickIds.Clear();
            for (int i = 0; i < blueprint.gimmickPlacements.Count; i++)
                PreviousStageGimmickIds.Add(blueprint.gimmickPlacements[i].id);
        }
    }
}
