using System;
using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 보스 패턴 메타데이터. BossData.patterns[]에 들어가며
    /// BossStateMachine이 가중치 추첨으로 다음 패턴 선택 시 사용.
    /// Boss_Patterns.md 각 보스 표의 예고/실행/후딜레이/빈도 컬럼 1:1.
    /// </summary>
    [Serializable]
    public struct BossPatternMetadata
    {
        [Tooltip("보스 내부 P1~P12 식별자")]
        public string patternId;

        [Tooltip("어느 Phase부터 활성")]
        public BossPhase availableFromPhase;

        [Tooltip("이 Phase에서만 활성 (true) / 이후 페이즈도 활성 (false)")]
        public bool exclusiveToPhase;

        [Tooltip("예고 시간 (초)")]
        public float telegraphSeconds;

        [Tooltip("실행 지속 시간 (초)")]
        public float executeSeconds;

        [Tooltip("후딜레이 (초)")]
        public float recoverySeconds;

        [Tooltip("Phase 내 선택 가중치 (%)")]
        public float weightPercent;

        [Tooltip("이 패턴 재사용 최소 간격 (초)")]
        public float minIntervalSeconds;
    }
}
