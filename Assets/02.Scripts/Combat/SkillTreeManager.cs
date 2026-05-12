using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 60종 스킬 트리 관리자. Skill_Tree_Formulas.md 4가지 연산 규칙 적용.
    /// 해금 조건: 선행 스킬이 prerequisiteMinLevel 이상.
    /// 패시브 효과는 캐시하여 DamageCalculator / ManaSystem / FlipperController에 공급.
    /// 공식 유틸은 SkillFormula 클래스 참조.
    /// </summary>
    public class SkillTreeManager : MonoBehaviour
    {
        public static SkillTreeManager Instance { get; private set; }

        [Header("스킬 카탈로그")]
        [SerializeField] private List<SkillData> allSkills = new();

        // 스킬 ID → 투자 레벨
        private readonly Dictionary<int, int> investedLevels = new();
        // 스킬 ID → SkillData 즉시 조회
        private readonly Dictionary<int, SkillData> skillById = new();

        // ── 패시브 효과 캐시 (RecalculatePassives()에서 갱신) ──
        // 데미지 보너스
        private float damageAddPhysical;   // 합연산 % 누적
        private float damageAddMagic;
        private readonly List<float> damageMulPhysical = new();
        private readonly List<float> damageMulMagic = new();
        // 크리티컬
        private float critChanceBonus;
        // 플리퍼
        private float flipperCooldownReduction1; // 플리퍼 경량화 I
        private float flipperCooldownReduction2; // 플리퍼 경량화 II
        private float flipperImpulseBonus;       // 탄성 강화
        private int flipperMaxStack = 1;
        // 멀티볼
        private int multiBallMaxCount = 1;
        // 마나
        private float manaChargeBonus; // "마나 충전" 패시브 점감 결과
        // 콤보
        private int comboMaxStack = 10; // 하이퍼 콤보 기본 한도
        // 방어구 깎기
        private float armorReduction;
        // 시간 회복 (타임 브레이커)
        private float timeRecoverPerKill;
        // 분노의 일격
        private float furyStrikeBonus;
        // 약점 포착
        private float weakPointCritBonus;
        // 콤보 스트라이크
        private float comboStrikePerStack;
        // 관성 돌파 I 곱연산 계수
        private float inertiaBreak1Factor;
        // 플리퍼 스매시
        private float flipperSmashFactor;
        // 묵직한 타격
        private float heavyBlowBonus;
        // 강철구 I (합)
        private float steelBall1Bonus;
        // 강철구 II (곱)
        private float steelBall2Factor;
        // 원소 친화 I (합) - 마법 데미지
        private float elemAffinity1Bonus;

        // ── 초기화 ──────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllSkills();
            BuildIndex();
            RecalculatePassives();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnSkillInvested>(HandleSkillInvested);
            EventBus.Subscribe<OnSkillReset>(HandleSkillReset);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnSkillInvested>(HandleSkillInvested);
            EventBus.Unsubscribe<OnSkillReset>(HandleSkillReset);
            if (Instance == this) Instance = null;
        }

        private void LoadAllSkills()
        {
            // Inspector 슬롯이 비어있으면 Resources에서 일괄 로드
            if (allSkills == null || allSkills.Count == 0)
            {
                var loaded = Resources.LoadAll<SkillData>("Skills");
                if (loaded != null && loaded.Length > 0)
                {
                    allSkills = new List<SkillData>(loaded);
                }
            }
        }

        private void BuildIndex()
        {
            skillById.Clear();
            if (allSkills == null) return;
            foreach (var s in allSkills)
            {
                if (s == null) continue;
                if (!skillById.ContainsKey(s.id))
                {
                    skillById[s.id] = s;
                }
                if (!investedLevels.ContainsKey(s.id))
                {
                    investedLevels[s.id] = 0;
                }
            }
        }

        // ── 카탈로그 조회 ────────────────────────────────────

        public SkillData GetSkillData(int skillId) =>
            skillById.TryGetValue(skillId, out var s) ? s : null;

        public int GetLevel(int skillId) =>
            investedLevels.TryGetValue(skillId, out var lv) ? lv : 0;

        public IReadOnlyDictionary<int, int> InvestedLevels => investedLevels;

        public bool IsUnlocked(int skillId) => GetLevel(skillId) > 0;

        // ── 해금 / 투자 ──────────────────────────────────────

        public bool CanInvest(int skillId, out string reason)
        {
            reason = null;
            var data = GetSkillData(skillId);
            if (data == null) { reason = $"SkillData {skillId} not found"; return false; }

            int currentLv = GetLevel(skillId);
            if (currentLv >= data.maxLevel) { reason = "MaxLevel reached"; return false; }

            // 선행 검사
            if (data.prerequisiteIds != null)
            {
                foreach (var preId in data.prerequisiteIds)
                {
                    if (preId == 0) continue;
                    if (GetLevel(preId) < data.prerequisiteMinLevel)
                    {
                        reason = $"Prerequisite {preId} Lv.{data.prerequisiteMinLevel}+ required";
                        return false;
                    }
                }
            }

            // SP 검사
            if (LevelSystem.Instance != null && LevelSystem.Instance.AvailableSP < 1)
            {
                reason = "Not enough SP";
                return false;
            }

            return true;
        }

        public bool Invest(int skillId)
        {
            if (!CanInvest(skillId, out _)) return false;

            // SP 소모
            if (LevelSystem.Instance != null && !LevelSystem.Instance.TryConsumeSP(1))
                return false;

            int newLv = GetLevel(skillId) + 1;
            investedLevels[skillId] = newLv;

            EventBus.Publish(new OnSkillInvested { SkillId = skillId, NewLevel = newLv });
            return true;
        }

        public void ResetAll()
        {
            // 모든 투자 레벨 0으로
            var keys = new List<int>(investedLevels.Keys);
            foreach (var k in keys) investedLevels[k] = 0;

            // SP 환원
            if (LevelSystem.Instance != null) LevelSystem.Instance.RefundAllSP();
            else EventBus.Publish(new OnSkillReset { RefundedSP = 0 });

            RecalculatePassives();
        }

        // ── 패시브 합산 ──────────────────────────────────────

        public void RecalculatePassives()
        {
            // 초기화
            damageAddPhysical = 0f;
            damageAddMagic = 0f;
            damageMulPhysical.Clear();
            damageMulMagic.Clear();
            critChanceBonus = 0f;
            flipperCooldownReduction1 = 0f;
            flipperCooldownReduction2 = 0f;
            flipperImpulseBonus = 0f;
            flipperMaxStack = 1;
            multiBallMaxCount = 1;
            manaChargeBonus = 0f;
            comboMaxStack = 10;
            armorReduction = 0f;
            timeRecoverPerKill = 0f;
            furyStrikeBonus = 0f;
            weakPointCritBonus = 0f;
            comboStrikePerStack = 0f;
            inertiaBreak1Factor = 1f;
            flipperSmashFactor = 1f;
            heavyBlowBonus = 0f;
            steelBall1Bonus = 0f;
            steelBall2Factor = 1f;
            elemAffinity1Bonus = 0f;

            // 60종 순회. 투자 레벨 > 0 인 패시브만 처리.
            foreach (var kv in investedLevels)
            {
                if (kv.Value <= 0) continue;
                var d = GetSkillData(kv.Key);
                if (d == null || !d.IsPassive) continue;

                ApplyPassive(d, kv.Value);
            }
        }

        private void ApplyPassive(SkillData d, int lv)
        {
            var sid = (SkillId)d.id;
            switch (sid)
            {
                // 🟢 제어 ────────────────────────────────────
                case SkillId.ControlFlipperLight1:
                    flipperCooldownReduction1 = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.ControlFlipperLight2:
                    flipperCooldownReduction2 = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.ControlElasticBoost:
                    flipperImpulseBonus = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    break;
                case SkillId.ControlFastDraw:
                    flipperMaxStack = SkillFormula.StackLimit(2, d.stackPerLevel, lv, d.stackLevelStep);
                    break;
                case SkillId.ControlTripleDraw:
                    flipperMaxStack = SkillFormula.StackLimit(3, d.stackPerLevel, lv, d.stackLevelStep);
                    break;

                // 🔴 파괴 ────────────────────────────────────
                case SkillId.DestSteelBall1:
                    steelBall1Bonus = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    damageAddPhysical += steelBall1Bonus;
                    break;
                case SkillId.DestSteelBall2:
                    steelBall2Factor = 1f + SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    damageMulPhysical.Add(steelBall2Factor);
                    break;
                case SkillId.DestInertiaBreak1:
                    // 곱연산 1.2 + Lv × 0.03 (BallSpeed 비례 적용은 DamageContext 사용 시점)
                    inertiaBreak1Factor = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    damageMulPhysical.Add(inertiaBreak1Factor);
                    break;
                case SkillId.DestComboStrike:
                    comboStrikePerStack = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    break;
                case SkillId.DestWeakPoint:
                    weakPointCritBonus = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.DestHeavyBlow:
                    heavyBlowBonus = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    damageAddPhysical += heavyBlowBonus;
                    break;
                case SkillId.DestTimeBreaker:
                    timeRecoverPerKill = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.DestHyperCombo:
                    comboMaxStack = SkillFormula.StackLimit(10, d.stackPerLevel, lv, d.stackLevelStep);
                    break;
                case SkillId.DestFlipperSmash:
                    flipperSmashFactor = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    break;
                case SkillId.DestArmorCrash:
                    armorReduction = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.DestFuryStrike:
                    furyStrikeBonus = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    break;

                // 🔵 원소 ────────────────────────────────────
                case SkillId.ElemAffinity1:
                    elemAffinity1Bonus = SkillFormula.Linear(d.linearBase, d.linearPerLevel, lv);
                    damageAddMagic += elemAffinity1Bonus;
                    break;
                case SkillId.ElemManaCharge:
                    manaChargeBonus = SkillFormula.Diminish(d.diminishMax, d.diminishRate, lv);
                    break;
                case SkillId.ElemMultiBallI:
                    multiBallMaxCount = Mathf.Max(multiBallMaxCount, 2);
                    break;
                case SkillId.ElemMultiBallII:
                    multiBallMaxCount = Mathf.Min(SkillFormula.StackLimit(3, d.stackPerLevel, lv, d.stackLevelStep), Constants.MultiBallHardCap);
                    break;
                default:
                    // 미처리 패시브는 무시 (마일스톤 4~6에서 추가)
                    break;
            }
        }

        // ── 공급 API (DamageCalculator/ManaSystem 등에서 호출) ──

        public float GetDamageAddPercent(DamageType type)
        {
            return type == DamageType.Magic ? damageAddMagic : damageAddPhysical;
        }

        public IReadOnlyList<float> GetDamageMultiplierFactors(DamageType type)
        {
            return type == DamageType.Magic ? damageMulMagic : damageMulPhysical;
        }

        public float GetCritChanceBonus() => critChanceBonus + weakPointCritBonus;
        public float GetArmorReductionPercent() => armorReduction;
        public float GetComboStrikePerStack() => comboStrikePerStack;
        public int GetMaxComboStack() => comboMaxStack;
        public float GetFuryStrikeBonus() => furyStrikeBonus;
        public float GetInertiaBreak1Factor() => inertiaBreak1Factor;
        public float GetFlipperSmashFactor() => flipperSmashFactor;

        /// <summary>플리퍼 쿨타임 배율 (1.0이 기본, 0.5가 50% 감소). 곱연산 후 하드캡 적용.</summary>
        public float GetFlipperCooldownMultiplier()
        {
            float mult = (1f - flipperCooldownReduction1) * (1f - flipperCooldownReduction2);
            return mult;
        }

        /// <summary>플리퍼 충격 보너스 (탄성 강화). 1.0 = 기본.</summary>
        public float GetFlipperImpulseMultiplier() => 1f + flipperImpulseBonus;

        public int GetMaxFlipperStack() => flipperMaxStack;
        public int GetMaxMultiBallCount() => multiBallMaxCount;

        /// <summary>마나 충전 효율 배율 (1.0이 기본).</summary>
        public float GetManaChargeMultiplier() => 1f + manaChargeBonus;

        /// <summary>몬스터 처치 시 시간 회복 보너스 (초).</summary>
        public float GetTimeRecoverPerKill() => timeRecoverPerKill;

        // ── 이벤트 핸들러 ────────────────────────────────────

        private void HandleSkillInvested(OnSkillInvested e) => RecalculatePassives();
        private void HandleSkillReset(OnSkillReset e) => RecalculatePassives();

        // ── 디버그/테스트 ────────────────────────────────────

        /// <summary>테스트용 — SP 검사 없이 직접 투자.</summary>
        public void DebugSetLevel(int skillId, int level)
        {
            var d = GetSkillData(skillId);
            if (d == null) return;
            investedLevels[skillId] = Mathf.Clamp(level, 0, d.maxLevel);
            RecalculatePassives();
        }

        public void DebugRegisterSkill(SkillData s)
        {
            if (s == null) return;
            if (!allSkills.Contains(s)) allSkills.Add(s);
            skillById[s.id] = s;
            if (!investedLevels.ContainsKey(s.id)) investedLevels[s.id] = 0;
        }
    }
}
