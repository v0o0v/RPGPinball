using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Physics;

namespace RPGPinball.Stage.Gimmicks
{
    /// <summary>
    /// 80종 중 전용 컴포넌트가 없는 기믹의 본 구현 (M5 후속 폴리싱, 2026-05-14).
    /// GimmickData SO의 효과 슬롯을 일괄 발행:
    ///   - impulseN: 공 반발 임펄스 (Bumper류)
    ///   - manaDelta: 즉시 마나 충전·소모
    ///   - timePenaltySeconds: 음수 → 시간 페널티, 양수 → 시간 보너스
    ///   - absoluteHpDamage: 공 강제 reset(보스 강제 reset 메커닉 재활용)
    /// 카테고리별 특수 거동(중력 반전·거울 반전·거인 포션 변신 등)은 M8 폴리싱에서 전용 컴포넌트로 확장.
    /// 클래스명은 prefab 자산 호환성을 위해 유지 — 의미상 GenericGimmick에 가깝다.
    /// </summary>
    public class PlaceholderGimmick : GimmickBase
    {
        protected override void HandleBallContact(BallController ball)
        {
            if (data == null) return;
            if (IsOnCooldown()) return;
            StampCooldown();

            ApplyDataEffects(ball);

            if (data.triggerOnceOnly) ConsumeAndDespawn();
        }

        protected override void HandlePeriodicTick()
        {
            if (data == null) return;
            // 주기 트리거(예: 강풍 경보, 시계탑 함정)는 공 접촉 없이도 효과 발행.
            // 임펄스는 대상이 없으므로 스킵, 글로벌 효과(시간/마나)만 적용.
            ApplyDataEffects(ball: null);
        }

        protected virtual void ApplyDataEffects(BallController ball)
        {
            // 1) 임펄스 (반발) — 공이 있을 때만
            if (data.impulseN > 0f && ball != null && ball.Rb != null)
            {
                Vector2 dir = ((Vector2)ball.transform.position - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                ball.Rb.AddForce(dir * data.impulseN, ForceMode2D.Impulse);
            }

            // 2) 마나 충전·소모
            if (data.manaDelta != 0 && ManaSystem.Instance != null)
                ManaSystem.Instance.Charge(data.manaDelta);

            // 3) 시간 변동
            if (data.timePenaltySeconds < 0f)
            {
                EventBus.Publish(new OnTimePenalty { Delta = data.timePenaltySeconds });
            }
            else if (data.timePenaltySeconds > 0f)
            {
                EventBus.Publish(new OnTimeBonusAdded
                {
                    Seconds = data.timePenaltySeconds,
                    Source = $"Gimmick:{data.gimmickId}"
                });
            }

            // 4) 절대 HP 데미지 → 공 강제 reset (덩굴/가시 함정/얼음 가시 류)
            //    데드존 제거(2026-05-13) 이후 트랩 페널티는 강제 reset + 시간 페널티로 표현.
            if (data.absoluteHpDamage > 0 && ball != null)
            {
                // 시간 페널티는 위에서 이미 발행됐으므로 reset만 추가.
                ball.ForceReset();
            }

            // 5) XP/Gold/Buff/Debuff 지속은 M6(EconomyManager) / M8(StatusEffectSystem) 이후 본격화.
            //    현재는 슬롯 값만 검증해 두고 동작은 N/A.
        }
    }
}
