using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Security;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 콤보 카운터. 몬스터/보스 타격 시 +1, 3초 내 미타격 시 0, 낙사 시 즉시 0.
    /// 10/30 콤보에서 마나 획득 배율 1.5/2.0배.
    /// </summary>
    public class ComboSystem : MonoBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        private SafeInt combo;
        private float lastHitTime;

        public int Combo => combo.Value;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            combo = SafeInt.Create(0);
        }

        private void OnEnable()
        {
            // 데드존 제거(2026-05-13) — 자연 낙사 없음. 콤보 리셋은 3초 무타격 타이머가 담당.
            // 보스 강제 reset 등에서 콤보를 끊고 싶으면 ComboSystem.Instance.ResetCombo()를 직접 호출.
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (combo.Value > 0 && Time.time - lastHitTime > Constants.ComboResetSeconds)
            {
                SetCombo(0);
            }
        }

        /// <summary>몬스터/보스 타격 시 호출.</summary>
        public void RegisterHit()
        {
            lastHitTime = Time.time;
            SetCombo(combo.Value + 1);
        }

        public void ResetCombo() => SetCombo(0);

        /// <summary>현재 콤보 기준 마나 획득 배율.</summary>
        public float ManaMultiplier => GetManaMultiplier(combo.Value);

        public static float GetManaMultiplier(int comboValue)
        {
            if (comboValue >= Constants.ComboTier2) return Constants.ComboMultTier2;
            if (comboValue >= Constants.ComboTier1) return Constants.ComboMultTier1;
            return 1f;
        }

        private void SetCombo(int newValue)
        {
            int prev = combo.Value;
            combo = SafeInt.Create(newValue);
            if (prev != newValue)
                EventBus.Publish(new OnComboChange { Combo = newValue });

            // M7 #9 인계: 콤보 마일스톤 (10/30/50/100)
            if (newValue > prev) PublishMilestoneIfCrossed(prev, newValue);
        }

        private static void PublishMilestoneIfCrossed(int prev, int next)
        {
            int[] milestones = { Constants.ComboMilestone10, Constants.ComboMilestone30,
                                 Constants.ComboMilestone50, Constants.ComboMilestone100 };
            foreach (var m in milestones)
            {
                if (prev < m && next >= m)
                    EventBus.Publish(new OnComboMilestone { Milestone = m });
            }
        }

    }
}
