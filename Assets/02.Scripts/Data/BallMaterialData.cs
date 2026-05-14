using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 공 재질 4종 정의 (Wood / Steel / Mithril / Volcanic).
    /// Game_Design_Spec.md §3 대장간 재질 표를 1:1로 반영.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Ball Material", fileName = "BallMaterial")]
    public class BallMaterialData : ScriptableObject
    {
        [Header("식별")]
        public BallMaterialId materialId = BallMaterialId.Wood;
        public string displayNameKo;
        [TextArea(2, 4)] public string descriptionKo;

        [Header("물리")]
        public float mass = 1.0f;
        [Range(0f, 1f)] public float bounciness = 0.5f;
        [Range(0f, 1f)] public float friction = 0.2f;
        public float flipperCooldownMultiplier = 1.0f;

        [Header("효과 (재질별 특성)")]
        // 나무: 바람 기믹 효과 +50%
        public float windGimmickMultiplier = 1.0f;
        // 강철: 장애물 관통 +1
        public int obstacleBreakthroughBonus = 0;
        // 미스릴: 마법 데미지 ×1.15 (Constants.MithrilMagicMultiplier)
        public float magicDamageMultiplier = 1.0f;
        // 화산암: 불꽃 자취 + 화상 도트
        public bool leavesFireTrail;
        public bool burnDotEnabled;

        [Header("해금 조건")]
        public BossId requiresBossDefeat = BossId.None;
        public int requiresBlueprintFragments;
        public string requiresHiddenStageId;

        [Header("제작 비용")]
        public int goldCost;
        public CurrencyId specialOreId = CurrencyId.None;
        public int specialOreCount;

        [Header("교체 비용 (기본 100골드)")]
        public int swapGoldCost = 100;

        [Header("비주얼 (§0-A.3 Kenney 매핑)")]
        public Sprite iconSprite;
        public Color tintColor = Color.white;
    }
}
