using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 액티브/패시브 스킬 정의. Active_Skill_Judgment.md + Skill_Tree_Formulas.md를 SO로 직렬화.
    /// 마일스톤 3에서 60종으로 확장. id는 SkillId enum 값과 일치(int 캐스팅).
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Skill", fileName = "SkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("식별")]
        public int id; // SkillId enum 값 (int)SkillId.X
        public string displayName;
        public SkillCategory category;
        public SkillBranch branch;
        public SkillType type;

        [Header("스킬 트리")]
        [Range(1, 6)] public int tier = 1;
        [Range(1, 5)] public int maxLevel = 1;
        public int[] prerequisiteIds = System.Array.Empty<int>();
        public int prerequisiteMinLevel = 1;

        [Header("자원")]
        public int manaCost;
        public float cooldown;

        [Header("판정")]
        public SkillShape shape = SkillShape.Circle;
        public float radius;        // Circle / Sector
        public Vector2 size;        // Rectangle (가로, 세로)
        public float sectorAngle;   // Sector 각도(도)
        public int maxHits = 1;     // 0 = 데미지 없음(인력·반사 등), -1 = 무제한

        [Header("효과")]
        public DamageType damageType = DamageType.Physical;
        public bool dealsKnockback;
        public float knockbackForce;
        public bool isUltimate; // Tier 6 궁극기. 덱 1개 제한.

        [Header("데미지 공식 파라미터 (마일스톤 2 호환)")]
        public float baseMultiplier = 1.0f; // 기본 데미지 배율 (Lv.1 기준)
        public float perLevelMultiplier;    // Lv.당 추가 배율

        [Header("공식 파라미터 (Skill_Tree_Formulas.md)")]
        // 합연산: Linear(baseVal + perLv × Lv)
        public float linearBase;
        public float linearPerLevel;
        // 점감: Diminish(max × (1 - rate^Lv))
        public float diminishMax;
        public float diminishRate = 0.95f;
        // 중첩 한도: trip 드로우(Lv 2당 +1) / 하이퍼 콤보(Lv 1당 +2) / 파이어볼 II(Lv 2당 +1)
        public int stackBase;
        public int stackPerLevel;
        public int stackLevelStep = 1; // Lv 몇 당 +stackPerLevel 적용

        [Header("지속시간 (액티브 전용)")]
        public float durationBase;
        public float durationPerLevel;

        [Header("툴팁")]
        [TextArea(3, 6)] public string descriptionKo;

        // ── 헬퍼 ────────────────────────────────────────────
        public bool IsPassive => type == SkillType.Passive;
        public bool IsActiveSwitch => type == SkillType.ActiveSwitch;
        public SkillId SkillId => (SkillId)id;

        /// <summary>레벨에 따른 지속시간 계산 (액티브 전용).</summary>
        public float GetDuration(int level)
        {
            return durationBase + durationPerLevel * level;
        }
    }
}
