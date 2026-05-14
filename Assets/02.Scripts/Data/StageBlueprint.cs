using System;
using System.Collections.Generic;

namespace RPGPinball.Data
{
    /// <summary>
    /// 절차 생성 결과의 직렬화 가능 표현. 시드 재현·일일 도전·세이브 양쪽에서 사용.
    /// ScriptableObject가 아닌 일반 [Serializable] 클래스.
    /// </summary>
    [Serializable]
    public class StageBlueprint
    {
        // 메타
        public ulong seed;
        public ActId actId;
        public int stageIndex; // 1~30
        public NodeKind nodeKind;
        public DifficultyBand band;
        public int finalBudget;
        public int recommendedLevel;
        public float timeLimitSeconds = 180f;
        public float targetStageHeight; // 카메라 시야 세로 × 3 (빌드 시점 OrthoSize 기반)

        // 레이아웃
        public string topSegmentId;
        public List<string> middleSegmentIds = new();
        public string bottomSegmentId;

        // 기믹 배치
        public List<GimmickPlacementEntry> gimmickPlacements = new();

        // 웨이브
        public List<WaveEntry> waves = new();

        // 특성·돌연변이
        public List<ModifierId> modifierIds = new();
        public MutationId mutationId = MutationId.None;

        // 이벤트 노드 결과 (이벤트 진입 시 채워짐)
        public EventNodeKind? eventNodeKind;
        public string eventOutcomeId;

        // 직전 스테이지 정보 (가중치 디케이용 — 외부에서 주입)
        public ulong previousStageSeed;

        [Serializable]
        public struct GimmickPlacementEntry
        {
            public GimmickId id;
            public int segmentIndex; // 0=top, 1..N=middle, N+1=bottom
            public int slotIndex;
        }

        [Serializable]
        public struct WaveEntry
        {
            public WaveCompositionPattern pattern;
            public string[] monsterIds;
            public bool hasElite;
        }
    }
}
