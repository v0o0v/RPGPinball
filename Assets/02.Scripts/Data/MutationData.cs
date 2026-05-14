using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 돌연변이 스테이지 5종 SO. Procedural_Stage_Gen.md §8.
    /// 5% 확률로 발생. requiredDifficultyBand 필터로 일부는 클라이맥스 한정.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Stage/Mutation", fileName = "MutationData")]
    public class MutationData : ScriptableObject
    {
        [Header("식별")]
        public MutationId mutationId;
        public string displayNameKo;

        [Header("출현 조건")]
        public bool allowPrologue = true;     // 서막에서 허용
        public bool allowDevelopment = true;  // 전개에서 허용
        public bool allowClimax = true;       // 클라이맥스에서 허용

        // ── 테마 침식 (전개 이후) ──
        [Header("테마 침식")]
        [Range(0, 4)]
        public int crossThemeGimmickCount; // 1~2
        public ActId[] crossThemeSourceActs;

        // ── 거울 세계 ──
        [Header("거울 세계")]
        public bool mirrorLayoutHorizontal;

        // ── 미니어처 (클라이맥스만) ──
        [Header("미니어처")]
        public float playfieldScaleMultiplier = 1f; // 0.6
        public float wallElasticityMultiplier = 1f; // 1.2

        // ── 타임 러시 ──
        [Header("타임 러시")]
        public bool forceTimeLimit;
        public float forcedTimeLimitSeconds = 60f;
        public float trMonsterHpMultiplier = 1f; // 0.5
        public float trRewardMultiplier = 1f;    // 3.0

        // ── 보스 러시 (클라이맥스만) ──
        [Header("보스 러시")]
        public float recurringBossHpRatio = 0.5f;
        public BossId[] bossPoolForAct;

        // ── 공통 보상 ──
        [Header("공통 보상 보정")]
        public float goldMultiplier = 2f;
        public float rareRuneChanceDelta = 0.15f;
        public string iconKind = "⚠️";

        [TextArea(2, 4)]
        public string descriptionKo;
    }
}
