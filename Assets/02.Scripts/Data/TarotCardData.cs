using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 타로카드 38장 정의 (Common 10 / Rare 10 / Legendary 10 / Mythic 8).
    /// Tarot_Card_List.md 그대로 매핑.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Village/Tarot Card", fileName = "Tarot")]
    public class TarotCardData : ScriptableObject
    {
        [Header("식별")]
        public TarotCardId cardId = TarotCardId.None;
        public TarotGrade grade = TarotGrade.Common;
        public string displayNameKo;
        public string arcanaMotif;
        [TextArea(2, 5)] public string descriptionKo;

        [Header("비주얼 (§0-A.2 Kenney 매핑)")]
        public Sprite faceSprite;
        public Sprite backSprite;
        public Color frameColor = Color.white;

        [Header("효과 슬롯 — 카드별 유효 필드만 사용")]
        public int startingManaBonus;                // C-01 달빛의 샘
        public float goldGainPercent;                // C-02 황금 나침반
        public float manaChargeEfficiencyPercent;    // C-03 견습생의 지팡이
        public float ballSpeedPercent;               // C-04 여행자의 장화
        public float rareDropChancePercent;          // C-05 행운의 동전
        public float shieldRegenTimeDelta;           // C-06 대지의 가호
        public float physicalDamagePercent;          // C-07 전사의 팔찌
        public float forcedResetPenaltyDelta;        // C-08 바람의 깃털
        public float comboHoldDurationDelta;         // C-09 집중의 수정
        public float debuffImmuneStartSeconds;       // C-10 축복받은 부적

        public bool firstForcedResetNullified;       // R-01 불사조의 날개
        public float weakPointCritBonus;             // R-02 사냥꾼의 눈
        public bool reduceMonsterDefense;            // R-03 쇠약의 저주 (-15%)
        public float monsterDefDebuffPercent;
        public float flipperCooldownDelta;           // R-04 플리퍼 마스터
        public float magicDamagePercent;             // R-05 원소 증폭기
        public float startingTimeBonusSeconds;       // R-06 시간의 편린
        public int comboManaBonusPer10;              // R-07 콤보 연금술 (10콤보당 +5)
        public float adversityDamageBonus;           // R-08 강철의 의지 (시간 60↓ +20%)
        public float monsterKillTimeRecoverChance;   // R-09 영혼 수확자
        public float blockReflectPercent;            // R-10 거울 방패

        public float multiBallAutoStartSeconds;      // L-01 쌍둥이의 별
        public bool materialAdvantageMerge;          // L-02 대지의 심장
        public float bossLowHpDamageBonus;           // L-03 파멸의 인장 (HP ≤ 20% +50%)
        public float lowTimeDamageBonus;             // L-04 시간의 군주
        public bool permanentLightning;              // L-05 폭풍의 왕관
        public float attributeConvertDurationDelta;  // L-06 정령왕의 축복
        public int shieldStackCount;                 // L-07 강철 요새
        public bool gradeUpgradeOneStep;             // L-08 별의 유언
        public bool permanentFlameTrail;             // L-09 지옥불 코어
        public float justParryWindowDelta;           // L-10 저스트 타이밍

        [Header("신화 (M-01~M-08) — 각 분기 플래그")]
        public bool mythicEffectFlag;                // 각 신화 카드 분기는 cardId로 식별

        [Header("확률 (Mythic 5%, Legendary 10%, Rare 25%, Common 60%)")]
        public float dropWeightPercent;

        [Header("영구 카드 승급 (동일 10장 + 5,000골드)")]
        public int permanentUpgradeRequiredCount = 10;
        public int permanentUpgradeGoldCost = 5000;
    }
}
