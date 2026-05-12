using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 몬스터 스탯 정의. 마일스톤 2에서는 더미 몬스터 1종만 사용.
    /// 마일스톤 4에서 보스/엘리트로 확장 예정.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Monster", fileName = "MonsterData")]
    public class MonsterData : ScriptableObject
    {
        [Header("식별")]
        public int id;
        public string displayName;
        public int level = 1; // 마일스톤 3: 오버레벨링 페널티 계산용

        [Header("스탯")]
        public int maxHp = 100;
        public int defense;
        public int magicResist;

        [Header("보상")]
        public int xpReward;
        public int goldReward;
        public int manaOnHit = Core.Constants.ManaPerMonster;

        [Header("판정")]
        public KnockbackTier knockbackTier = KnockbackTier.None;
        public bool isBoss;
    }
}
