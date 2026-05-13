using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 현재 보스전 진행 여부를 글로벌하게 노출.
    /// BossBase가 OnEnable/Die에서 Enter/Exit 호출.
    /// DeadZone, FlipperController 등이 분기 조건으로 참조.
    /// </summary>
    public static class BossFightContext
    {
        public static bool IsActive { get; private set; }
        public static MonoBehaviour CurrentBoss { get; private set; }
        public static Data.BossId CurrentBossId { get; private set; }

        public static void Enter(MonoBehaviour boss, Data.BossId bossId)
        {
            IsActive = true;
            CurrentBoss = boss;
            CurrentBossId = bossId;
            EventBus.Publish(new OnBossSpawned { Boss = boss != null ? boss.gameObject : null, BossId = bossId });
        }

        public static void Exit()
        {
            IsActive = false;
            CurrentBoss = null;
            CurrentBossId = Data.BossId.None;
        }

        public static void ForceClear()
        {
            // 씬 전환 등에서 명시적 정리
            IsActive = false;
            CurrentBoss = null;
            CurrentBossId = Data.BossId.None;
        }
    }
}
