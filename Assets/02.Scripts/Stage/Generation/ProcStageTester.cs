using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Stage.Segments;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// PlayMode 테스트용 컴포넌트. Inspector에서 Act/Stage/Seed 지정 후 Start 또는 우클릭 메뉴로 절차 생성 + GameObject 빌드.
    /// 마일스톤 5 인게임 검증 전용. 정식 GameManager 통합은 M7 ActMap.
    /// </summary>
    public class ProcStageTester : MonoBehaviour
    {
        [Header("스테이지 파라미터")]
        public ActId actId = ActId.Act1_Spring;
        [Range(1, 30)] public int stageIndex = 7;
        [Tooltip("0이면 자동(Stage·Act·DateTime 기반), 그 외 값이면 시드 고정.")]
        public ulong seedOverride = 0UL;
        public string playerUid = "tester";

        [Header("동작")]
        [Tooltip("Start 호출 시 자동 빌드.")]
        public bool buildOnStart = true;
        [Tooltip("기존 빌드된 stage 루트를 다시 빌드 시 자동 제거.")]
        public bool destroyPreviousOnRebuild = true;

        [Header("디버그 정보 (런타임 표시)")]
        [SerializeField] private string lastBlueprintInfo;
        [SerializeField] private StageRuntimeBuilder.StageRuntime currentRuntime;

        private void Start()
        {
            if (buildOnStart) Rebuild();
        }

        [ContextMenu("Rebuild Stage")]
        public void Rebuild()
        {
            if (destroyPreviousOnRebuild && currentRuntime != null)
            {
                currentRuntime.Dispose();
                currentRuntime = null;
            }

            // 풀 강제 재로드 (에디터에서 SO 추가 후 즉시 반영)
            SegmentPool.Instance.OverrideForTest(Resources.LoadAll<SegmentData>("Segments"));
            GimmickPool.Instance.OverrideForTest(Resources.LoadAll<GimmickData>("Gimmicks"));
            RPGPinball.Stage.Wave.MonsterPool.Instance.OverrideForTest(Resources.LoadAll<MonsterData>("Monsters"));
            RPGPinball.Stage.Modifiers.ModifierPool.Instance.OverrideForTest(Resources.LoadAll<StageModifierData>("Modifiers"));
            RPGPinball.Stage.Modifiers.MutationPool.Instance.OverrideForTest(Resources.LoadAll<MutationData>("Mutations"));

            ulong seed = seedOverride;
            if (seed == 0UL)
                seed = StageSeedFactory.BuildSeed(playerUid, StageSeedFactory.NowKst(), actId, stageIndex);

            ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
            var blueprint = ProceduralStageGenerator.Generate(actId, stageIndex, seed);
            var layout = SegmentLayoutBuilder.Build(blueprint.actId, blueprint.band, new DeterministicRng(seed), SegmentPool.Instance);

            var builder = new StageRuntimeBuilder();
            currentRuntime = builder.Build(blueprint, layout, transform);

            lastBlueprintInfo = $"seed={blueprint.seed} | band={blueprint.band} | budget={blueprint.finalBudget} | gimmicks={blueprint.gimmickPlacements.Count} | waves={blueprint.waves.Count} | modifiers={blueprint.modifierIds.Count} | mutation={blueprint.mutationId} | targetH={blueprint.targetStageHeight:F1}U";
            Debug.Log($"[ProcStageTester] {actId} S{stageIndex:00} → {lastBlueprintInfo}");
            foreach (var p in blueprint.gimmickPlacements)
            {
                var g = GimmickPool.Instance.Get(p.id);
                Debug.Log($"  └ {p.id} ({g?.category}) seg={p.segmentIndex} slot={p.slotIndex}");
            }
        }

        [ContextMenu("Cycle Seed")]
        public void CycleSeed()
        {
            seedOverride = seedOverride == 0UL ? 1UL : seedOverride + 1UL;
            Rebuild();
        }

        [ContextMenu("Next Stage")]
        public void NextStage()
        {
            stageIndex = (stageIndex % 30) + 1;
            Rebuild();
        }

        private void OnGUI()
        {
            const int H = 22;
            GUI.Box(new Rect(10, 10, 460, H * 4 + 12), "ProcStage 테스터");
            GUI.Label(new Rect(20, 30, 440, H), $"{actId} · Stage {stageIndex} · Seed {(seedOverride == 0UL ? "auto" : seedOverride.ToString())}");
            GUI.Label(new Rect(20, 30 + H, 440, H), lastBlueprintInfo ?? "(미생성)");
            if (GUI.Button(new Rect(20, 30 + H * 2, 100, H), "재생성")) Rebuild();
            if (GUI.Button(new Rect(130, 30 + H * 2, 100, H), "시드 +1")) CycleSeed();
            if (GUI.Button(new Rect(240, 30 + H * 2, 100, H), "다음 스테이지")) NextStage();
            if (currentRuntime != null)
                GUI.Label(new Rect(20, 30 + H * 3, 440, H),
                    $"GameObjects: top={(currentRuntime.topSegment != null)} bot={(currentRuntime.bottomSegment != null)} mids={currentRuntime.middleSegments.Count} gimmicks={currentRuntime.spawnedGimmicks.Count}");
        }
    }
}
