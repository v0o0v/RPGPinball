using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 스테이지 모디파이어(특성) 18종 통합 SO 스키마.
    /// Procedural_Stage_Gen.md §7 공통 10 + 테마 8.
    /// 모든 파라미터를 한 SO에 통합해 dispatcher가 단일 진입점으로 처리.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Modifier", fileName = "StageModifierData")]
    public class StageModifierData : ScriptableObject
    {
        [Header("식별")]
        public ModifierId modifierId;
        public string displayNameKo;
        [Tooltip("None(=공통)이면 어느 액트에서도 적용 가능. Act1~4 지정 시 테마 전용.")]
        public ActId themeOwner = ActId.None;
        [Tooltip("난이도 별점 1~5.")]
        public int tier = 1;

        // ── 시간 / 페널티 ──
        [Header("시간 / 페널티")]
        public float timeLimitDeltaSeconds; // 쾌속전 -30
        public float deadzonePenaltyMultiplier = 1f; // 사신의 손길 ×2

        // ── 몬스터 / 엘리트 ──
        [Header("몬스터 / 엘리트")]
        public float monsterHpMultiplier = 1f; // 쾌속전 0.8 / 황금 열풍 1.3
        public float monsterDefMultiplier = 1f; // 철벽 요새 1.5
        public float eliteSpawnMultiplier = 1f; // 사신의 손길 2.0

        // ── 공 / 플리퍼 ──
        [Header("공 / 플리퍼")]
        public float ballBaseSpeedMultiplier = 1f; // 광란의 피치 1.5
        public float flipperCooldownDelta; // 광란의 피치 +0.3
        public float flipperFailChance; // 꽃가루 알레르기 0.3

        // ── 중력 / 환경 ──
        [Header("중력 / 환경")]
        public float gravityChaosIntervalSeconds; // 혼돈의 중력 5
        public bool vegetationGrowthEnabled; // 만개의 숲
        public float tideCycleSeconds; // 밀물과 썰물 30
        public bool rampGimmickToggleEnabled; // 기계 오작동
        public float phaseShiftIntervalSeconds; // 유령의 농담 5
        public float visionReductionPercent; // 블리자드 0.4
        public bool timeWarpEnabled; // 시간 왜곡

        // ── 골드 / 크리티컬 / 빙결 ──
        [Header("골드 / 크리티컬 / 빙결")]
        public float goldDropMultiplier = 1f; // 황금 열풍 3.0 / 해적의 저주 0.5
        public float criticalChanceDelta; // 약점 노출 +0.2
        public float ballFreezeTriggerSeconds; // 영겁의 서리 2.0
        public float gimbalRandomBuffChance; // 도박사의 밤 0.5

        // ── 마나 / 스킬 ──
        [Header("마나 / 스킬")]
        public float manaChargeMultiplier = 1f; // 마력 폭풍 2.0
        public float skillDamageDelta; // 마력 폭풍 +0.3
        public float skillCostMultiplier = 1f; // 마력 폭풍 1.5

        // ── 보상 보정 ──
        [Header("보상 보정")]
        public float goldRewardDelta;
        public float xpRewardDelta;
        public float runeDropChanceDelta;
        public float manaCrystalDelta;
        public float tarotShardDelta;
        public float coreShardChanceDelta;
        public int spRewardDelta;
        public float comboBonusDelta;

        [TextArea(2, 4)]
        public string descriptionKo;
    }
}
