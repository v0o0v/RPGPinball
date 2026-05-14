using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Combat;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Stage
{
    /// <summary>
    /// 보스 연습 모드 진입점. 절차 생성을 우회하고 고정 보스 + 무제한 타이머 + 보상 미지급 환경 구성.
    /// 실제 보스 GameObject 인스턴스화는 M7 GameManager 진입 시 호출.
    /// </summary>
    public static class BossPracticeStageBuilder
    {
        public static bool IsActive { get; private set; }

        public static void Build(BossId bossId)
        {
            IsActive = true;

            // 타이머 무제한 (StageTimer 가 SetUnlimited API 노출하지 않는 경우 큰 값 설정)
            if (StageTimer.Instance != null)
            {
                StageTimer.Instance.ResetTimer(99999f);
            }

            // 보상 미지급
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Suppress(true);
            }
        }

        public static void Exit()
        {
            IsActive = false;
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.Suppress(false);
            }
        }
    }
}
