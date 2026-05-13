using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;
using RPGPinball.Physics;
using RPGPinball.Security;

namespace RPGPinball.Enemy.BossAI.Act4
{
    /// <summary>
    /// Act 4 보스 4-3: 겨울 여왕 (스테이지 30, 최종 보스).
    /// HP 56,000 / 3페이즈 / 권장 Lv.88~92 / 필요 DPS ≥ 311/초.
    /// Phase 1: 빙결 탄막 / 얼음 기둥 / 서리 화살(유도) / 냉기 파동(동심원).
    /// Phase 2: 절대 영도(3초 무충돌 즉시 낙사) / 빙하 감옥 / 눈보라.
    /// Phase 3: 시간 정지 / 전방위 빙결 폭발 / 빙결 왕관(30% 1초 동결) / 빙결 재생.
    /// </summary>
    public class WinterQueenBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData largeBullet;
        [SerializeField] private ProjectileData homingArrow; // homing=true

        [Header("Phase 2: 절대 영도")]
        [SerializeField] private float absoluteZeroNoCollisionSeconds = 3f;
        public bool AbsoluteZeroFieldActive { get; private set; }

        [Header("Phase 3: 시간 정지")]
        [SerializeField] private float timeStopDurationSec = 5f;
        [SerializeField] private float timeStopCooldownSec = 15f;
        private float lastTimeStopAt = -100f;
        private bool isTimeStopped;

        [Header("Phase 3: HP 회복 (초당 280)")]
        [SerializeField] private int phase3HpRegen = 280;
        private float regenAccum;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new FrostBullet(this, smallBullet),
                new IcePillarBarrier(),
                new FrostArrow(this, homingArrow),
                new ColdWave(this, smallBullet),
                new AbsoluteZeroField(this),
                new IcePrison(),
                new Blizzard(this, smallBullet),
                new TimeStop(this),
                new OmniFrostBurst(this, largeBullet),
                new FrostCrown()
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;

            // Phase 2 진입: 절대 영도 필드 활성
            if (CurrentPhase == BossPhase.P2 && !AbsoluteZeroFieldActive) AbsoluteZeroFieldActive = true;
            if (CurrentPhase == BossPhase.P1 && AbsoluteZeroFieldActive) AbsoluteZeroFieldActive = false;

            // 절대 영도 - 모든 공 검사
            if (AbsoluteZeroFieldActive)
            {
                foreach (var ball in Object.FindObjectsByType<BallController>(FindObjectsSortMode.None))
                {
                    if (ball == null) continue;
                    if (Time.time - ball.LastCollisionTime > absoluteZeroNoCollisionSeconds)
                    {
                        ball.OnDead();
                    }
                }
            }

            // Phase 3: 시간 정지 자동 발동
            if (CurrentPhase == BossPhase.P3 && !isTimeStopped && Time.time - lastTimeStopAt >= timeStopCooldownSec)
            {
                lastTimeStopAt = Time.time;
                TriggerTimeStop().Forget();
            }
        }

        private async UniTaskVoid TriggerTimeStop()
        {
            isTimeStopped = true;
            float prev = Time.timeScale;
            Time.timeScale = 0f;
            // 보스 본체는 unscaledDeltaTime으로 행동 (간소화: timeStopDurationSec만큼 대기)
            await UniTask.Delay(System.TimeSpan.FromSeconds(timeStopDurationSec), DelayType.UnscaledDeltaTime);
            Time.timeScale = prev;
            isTimeStopped = false;
            // 정지 해제 직후 전방위 빙결 폭발 1회
            if (largeBullet != null)
            {
                var opts = BulletPatternOptions.Default(largeBullet, 16, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, this, opts, default);
            }
        }

        // ───── 빙결 왕관 메커니즘 (HP 변화 감지로 30% 확률 동결) ─────
        // ApplyDamage를 오버라이드해 보스가 타격받을 때 30% 확률로 공 1초 동결
        // MonsterBase.ApplyDamage는 public. 별도 처리 위해 OnCollisionEnter2D 흐름 확장 필요.
        // 마일스톤 4 단순화: Phase 3 진입 시 BallController.ApplyForcedSlow를 패턴 단위로 발동.

        // ───── 패턴 클래스들 ─────

        private sealed class FrostBullet : IBossPattern
        {
            private readonly WinterQueenBoss boss; private readonly ProjectileData bullet;
            public FrostBullet(WinterQueenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 8, 270f, 90f);
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        private sealed class IcePillarBarrier : IBossPattern
        {
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = new Vector3(-3f + 3f * i, 0f, 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.4f, 8f, new Color(0.6f, 0.9f, 1f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class FrostArrow : IBossPattern
        {
            private readonly WinterQueenBoss boss; private readonly ProjectileData bullet;
            public FrostArrow(WinterQueenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return UniTask.CompletedTask;
                // 첫 번째 공을 타겟으로 유도
                var ball = Object.FindFirstObjectByType<BallController>();
                Vector2 dir = ball != null ? ((Vector2)ball.transform.position - (Vector2)boss.transform.position).normalized : Vector2.down;
                var pj = BulletEmitter.SpawnOne(bullet, boss.transform.position, dir, 10f);
                if (pj != null && ball != null) pj.SetHomingTarget(ball.transform, 180f);
                return UniTask.CompletedTask;
            }
        }

        private sealed class ColdWave : IBossPattern
        {
            private readonly WinterQueenBoss boss; private readonly ProjectileData bullet;
            public ColdWave(WinterQueenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 12, 0f, 0f);
                opts.burstCount = 2;
                opts.burstIntervalSec = 0.8f;
                await BulletEmitter.Emit(BulletPatternId.Concentric, boss, opts, ct);
            }
        }

        private sealed class AbsoluteZeroField : IBossPattern
        {
            private readonly WinterQueenBoss boss;
            public AbsoluteZeroField(WinterQueenBoss b) { boss = b; }
            public string Id => "P5";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 활성화는 Update가 처리. 패턴은 시각 안내용.
                TelegraphRenderer.ShowCircle(Vector3.zero, 5f, 2f, new Color(0.4f, 0.6f, 1f, 0.2f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class IcePrison : IBossPattern
        {
            public string Id => "P6";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                var ball = Object.FindFirstObjectByType<BallController>();
                if (ball != null)
                {
                    Vector3 pos = ball.transform.position;
                    TelegraphRenderer.ShowCircle(pos, 0.8f, 1.5f, new Color(0.6f, 0.9f, 1f, 0.6f));
                    DG.Tweening.DOVirtual.DelayedCall(1.0f, () =>
                    {
                        if (ball != null) ball.ApplyForcedSlow(0f, 2f);
                    });
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class Blizzard : IBossPattern
        {
            private readonly WinterQueenBoss boss; private readonly ProjectileData bullet;
            public Blizzard(WinterQueenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P7";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 10, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }

        private sealed class TimeStop : IBossPattern
        {
            private readonly WinterQueenBoss boss;
            public TimeStop(WinterQueenBoss b) { boss = b; }
            public string Id => "P9";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 시간 정지는 Update가 자동 처리. 패턴 자체는 안내용.
                return UniTask.CompletedTask;
            }
        }

        private sealed class OmniFrostBurst : IBossPattern
        {
            private readonly WinterQueenBoss boss; private readonly ProjectileData bullet;
            public OmniFrostBurst(WinterQueenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P10";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 16, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }

        private sealed class FrostCrown : IBossPattern
        {
            public string Id => "P11";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 30% 확률 공 1초 동결 (보스 타격 시점에 처리하는 것이 정석이나 마일스톤 4 단순화)
                if (Random.value < 0.3f)
                {
                    var ball = Object.FindFirstObjectByType<BallController>();
                    if (ball != null) ball.ApplyForcedSlow(0f, 1f);
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
