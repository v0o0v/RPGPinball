using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// 모디파이어 효과를 단일 합성본(Composite)으로 병합하고 Scene에 적용.
    /// 곱셈은 곱적분, 덧셈은 합적분 — 데이터 일관성을 위해 항목별 결정.
    /// </summary>
    public static class ModifierApplier
    {
        public static StageModifierData BuildComposite(IList<ModifierId> ids, ModifierPool pool)
        {
            var composite = ScriptableObject.CreateInstance<StageModifierData>();
            composite.displayNameKo = "Composite";
            composite.modifierId = ModifierId.None;
            composite.monsterHpMultiplier = 1f;
            composite.monsterDefMultiplier = 1f;
            composite.eliteSpawnMultiplier = 1f;
            composite.ballBaseSpeedMultiplier = 1f;
            composite.goldDropMultiplier = 1f;
            composite.manaChargeMultiplier = 1f;
            composite.skillCostMultiplier = 1f;
            composite.deadzonePenaltyMultiplier = 1f;

            if (ids == null || ids.Count == 0) return composite;

            for (int i = 0; i < ids.Count; i++)
            {
                var m = pool.Get(ids[i]);
                if (m == null) continue;

                // 곱셈은 곱적분
                composite.monsterHpMultiplier *= m.monsterHpMultiplier;
                composite.monsterDefMultiplier *= m.monsterDefMultiplier;
                composite.eliteSpawnMultiplier *= m.eliteSpawnMultiplier;
                composite.ballBaseSpeedMultiplier *= m.ballBaseSpeedMultiplier;
                composite.goldDropMultiplier *= m.goldDropMultiplier;
                composite.manaChargeMultiplier *= m.manaChargeMultiplier;
                composite.skillCostMultiplier *= m.skillCostMultiplier;
                composite.deadzonePenaltyMultiplier *= m.deadzonePenaltyMultiplier;

                // 덧셈은 합적분
                composite.timeLimitDeltaSeconds += m.timeLimitDeltaSeconds;
                composite.flipperCooldownDelta += m.flipperCooldownDelta;
                composite.criticalChanceDelta += m.criticalChanceDelta;
                composite.skillDamageDelta += m.skillDamageDelta;
                composite.flipperFailChance += m.flipperFailChance;

                composite.goldRewardDelta += m.goldRewardDelta;
                composite.xpRewardDelta += m.xpRewardDelta;
                composite.runeDropChanceDelta += m.runeDropChanceDelta;
                composite.manaCrystalDelta += m.manaCrystalDelta;
                composite.tarotShardDelta += m.tarotShardDelta;
                composite.coreShardChanceDelta += m.coreShardChanceDelta;
                composite.spRewardDelta += m.spRewardDelta;
                composite.comboBonusDelta += m.comboBonusDelta;

                // 환경 플래그(OR)
                composite.vegetationGrowthEnabled |= m.vegetationGrowthEnabled;
                composite.rampGimmickToggleEnabled |= m.rampGimmickToggleEnabled;
                composite.timeWarpEnabled |= m.timeWarpEnabled;

                // 첫 값 우선 (충돌 시 마지막이 우선이 더 명확)
                if (m.gravityChaosIntervalSeconds > 0f) composite.gravityChaosIntervalSeconds = m.gravityChaosIntervalSeconds;
                if (m.tideCycleSeconds > 0f) composite.tideCycleSeconds = m.tideCycleSeconds;
                if (m.phaseShiftIntervalSeconds > 0f) composite.phaseShiftIntervalSeconds = m.phaseShiftIntervalSeconds;
                if (m.ballFreezeTriggerSeconds > 0f) composite.ballFreezeTriggerSeconds = m.ballFreezeTriggerSeconds;
                if (m.gimbalRandomBuffChance > 0f) composite.gimbalRandomBuffChance = m.gimbalRandomBuffChance;
                if (m.visionReductionPercent > 0f) composite.visionReductionPercent = m.visionReductionPercent;
            }

            return composite;
        }

        /// <summary>
        /// 실제 Scene에 적용 — 카메라 / 플리퍼 / 공 / 타이머.
        /// 후속 마일스톤(7/8)에서 추가 dispatcher 확장 예정.
        /// </summary>
        public static void Apply(StageModifierData composite)
        {
            if (composite == null) return;

            if (Combat.StageTimer.Instance != null && composite.timeLimitDeltaSeconds != 0f)
            {
                if (composite.timeLimitDeltaSeconds > 0f) Combat.StageTimer.Instance.AddTime(composite.timeLimitDeltaSeconds);
                else Combat.StageTimer.Instance.Penalize(-composite.timeLimitDeltaSeconds);
            }

            // 카메라 시야 감소 — API 스텁 호출 (M8 셰이더로 본격화).
            if (Physics.CameraController.Instance != null && composite.visionReductionPercent > 0f)
                Physics.CameraController.Instance.SetVisionReduction(composite.visionReductionPercent);
        }
    }
}
