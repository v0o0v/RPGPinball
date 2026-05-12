using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 액티브/패시브 스킬 정의. Active_Skill_Judgment.md 표를 SO로 직렬화한 형태.
    /// 60종 스킬은 마일스톤 3에서 채워지며, 마일스톤 2에서는 16종(제어 6 · 파괴 4 · 원소 6)만 사용.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Skill", fileName = "SkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("식별")]
        public int id;
        public string displayName;
        public SkillCategory category;
        public SkillType type;

        [Header("스킬 트리")]
        [Range(1, 6)] public int tier = 1;
        [Range(1, 5)] public int maxLevel = 1;

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

        [Header("데미지 공식 파라미터")]
        public float baseMultiplier = 1.0f; // 기본 데미지 배율 (Lv.1 기준)
        public float perLevelMultiplier;    // Lv.당 추가 배율
    }
}
