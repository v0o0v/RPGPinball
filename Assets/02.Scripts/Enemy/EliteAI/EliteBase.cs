using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI;

namespace RPGPinball.Enemy.EliteAI
{
    /// <summary>
    /// 엘리트 공통 부모. BossBase 상속하여 도주 타이머와 첫 타격 타임아웃 추가.
    /// Elite_Bounty_Spec.md 공통 규칙 일치.
    /// </summary>
    public class EliteBase : BossBase
    {
        protected float fleeAt; // > 0이면 도주 절대 시각
        protected float firstHitTimeoutAt;
        protected bool hasBeenHit;
        protected bool fled;

        public EliteData EliteData => Data as EliteData;
        public bool HasFled => fled;

        protected override void Start()
        {
            base.Start();
            var ed = EliteData;
            if (ed != null && ed.firstHitTimeoutSeconds > 0f)
            {
                firstHitTimeoutAt = Time.time + ed.firstHitTimeoutSeconds;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;
            var ed = EliteData;
            if (ed == null) return;

            // 첫 타격 타임아웃 — 아직 안 맞았으면 도주
            if (!hasBeenHit && ed.firstHitTimeoutSeconds > 0f && Time.time >= firstHitTimeoutAt)
            {
                Flee("FirstHitTimeout");
                return;
            }
            // 처치 타이머 — 첫 타격 후 N초 내 처치 실패 시 도주
            if (hasBeenHit && fleeAt > 0f && Time.time >= fleeAt)
            {
                Flee("FleeTimer");
                return;
            }
        }

        // 첫 타격 시점에 fleeTimerSeconds 시작
        public new void ApplyDamage(RPGPinball.Combat.DamageResult result)
        {
            if (!hasBeenHit)
            {
                hasBeenHit = true;
                var ed = EliteData;
                if (ed != null && ed.fleeTimerSeconds > 0f) fleeAt = Time.time + ed.fleeTimerSeconds;
            }
            base.ApplyDamage(result);
        }

        protected void Flee(string reason)
        {
            if (fled) return;
            fled = true;
            var ed = EliteData;
            EventBus.Publish(new OnEliteFlee
            {
                Elite = gameObject,
                EliteId = ed != null ? ed.eliteId : EliteId.None,
                Reason = reason
            });
            // 도주: 보상 없이 비활성
            gameObject.SetActive(false);
        }
    }
}
