using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;
using RPGPinball.Physics;

namespace RPGPinball.Enemy.BossAI.Act4
{
    /// <summary>
    /// Act 4 보스 4-2: 시계탑 파수꾼 (스테이지 20).
    /// 시간 가속/감속·시계 바늘 회전 / HP 22,400.
    /// 패턴: 시간 가속 / 시간 감속 / 시계 바늘 / 시간 역행 탄막 / 종소리 충격파.
    /// </summary>
    public class ClockTowerSentinelBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new TimeAccelerate(),
                new TimeDecelerate(),
                new ClockHand(),
                new ReverseBullets(this, smallBullet),
                new BellShockwave(this, smallBullet)
            };
        }

        private sealed class TimeAccelerate : IBossPattern
        {
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 모든 공에 ×2 가속 5초
                foreach (var ball in Object.FindObjectsByType<BallController>(FindObjectsSortMode.None))
                {
                    if (ball != null) ball.ApplyForcedSpeedMultiplier(2f, 5f);
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class TimeDecelerate : IBossPattern
        {
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                foreach (var ball in Object.FindObjectsByType<BallController>(FindObjectsSortMode.None))
                {
                    if (ball != null) ball.ApplyForcedSpeedMultiplier(0.3f, 5f);
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class ClockHand : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 시계 바늘 회전 임시 표시
                TelegraphRenderer.ShowArrow(b.transform.position, Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.up, 4f, 3f, new Color(0.9f, 0.9f, 0.6f, 0.7f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class ReverseBullets : IBossPattern
        {
            private readonly ClockTowerSentinelBoss boss; private readonly ProjectileData bullet;
            public ReverseBullets(ClockTowerSentinelBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 6, 270f, 60f);
                opts.burstIntervalSec = 3f; // 3초 후 역방향
                await BulletEmitter.Emit(BulletPatternId.Reverse, boss, opts, ct);
            }
        }

        private sealed class BellShockwave : IBossPattern
        {
            private readonly ClockTowerSentinelBoss boss; private readonly ProjectileData bullet;
            public BellShockwave(ClockTowerSentinelBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P5";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 16, 0f, 0f);
                opts.burstCount = 3;
                opts.burstIntervalSec = 0.5f;
                await BulletEmitter.Emit(BulletPatternId.Concentric, boss, opts, ct);
            }
        }
    }
}
