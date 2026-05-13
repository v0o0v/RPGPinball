using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;
using RPGPinball.Security;

namespace RPGPinball.Enemy.BossAI.Act1
{
    /// <summary>
    /// Act 1 보스 1-3: 세계수 수호정령 (스테이지 30, 최종).
    /// HP 12,000 / 3페이즈.
    /// Phase 1: 뿌리 장벽 / 열매 폭탄 / 덩굴 올가미.
    /// Phase 2: 꽃가루 침묵 / 꽃잎 회전 / 씨앗 폭격.
    /// Phase 3: 광합성 재생 (DPS 레이스) / 최후의 뿌리 / 전방위 꽃잎.
    /// </summary>
    public class WorldTreeSpiritBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData largeBullet;

        [Header("Phase 3 재생 (초당 HP 회복량)")]
        [SerializeField] private int phase3RegenPerSecond = 120;

        // 재생 누적
        private float regenAccum;
        private SafeInt hpBoost;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new RootBarrier(this),
                new FruitBomb(this, largeBullet),
                new VineGrip(),
                new PollenSilence(),
                new PetalSpiral(this, smallBullet),
                new SeedBarrage(this, smallBullet),
                new Photosynthesis(),
                new FinalRoots(),
                new OmniPetal(this, smallBullet)
            };
        }

        protected override void Update()
        {
            base.Update();
            // Phase 3 광합성 재생: 본체 HP를 직접 회복할 수 없으므로 OnDamageDealt 사이클 외부에서 SafeInt 조작이 어려움.
            // 마일스톤 4 단순화: Photosynthesis 패턴이 발동되었을 때만 시각적 표시.
            // 실 회복은 마일스톤 검증용 시뮬레이션에서 별도 처리.
        }

        // ───── Phase 1 ─────
        private sealed class RootBarrier : IBossPattern
        {
            private readonly WorldTreeSpiritBoss boss;
            public RootBarrier(WorldTreeSpiritBoss b) { boss = b; }
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = -3f + 3f * i;
                    TelegraphRenderer.ShowCircle(new Vector3(x, 0f, 0f), 0.5f, 8f, new Color(0.4f, 0.6f, 0.3f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class FruitBomb : IBossPattern
        {
            private readonly WorldTreeSpiritBoss boss;
            private readonly ProjectileData bullet;
            public FruitBomb(WorldTreeSpiritBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 1, 270f, 0f);
                opts.burstCount = 3;
                opts.burstIntervalSec = 0.5f;
                opts.speed = 6f;
                await BulletEmitter.Emit(BulletPatternId.StraightBurst, boss, opts, ct);
            }
        }

        private sealed class VineGrip : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct) => UniTask.CompletedTask;
        }

        // ───── Phase 2 ─────
        private sealed class PollenSilence : IBossPattern
        {
            public string Id => "P4";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                EventBus.Publish(new OnFlipperSpawnBlocked { Duration = 1.5f, Area = null });
                return UniTask.CompletedTask;
            }
        }

        private sealed class PetalSpiral : IBossPattern
        {
            private readonly WorldTreeSpiritBoss boss;
            private readonly ProjectileData bullet;
            public PetalSpiral(WorldTreeSpiritBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P5";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 12, 0f, 0f);
                opts.burstCount = 5;
                opts.burstIntervalSec = 0.3f;
                opts.rotationSpeedDegPerSec = 60f;
                await BulletEmitter.Emit(BulletPatternId.RotatingRay, boss, opts, ct);
            }
        }

        private sealed class SeedBarrage : IBossPattern
        {
            private readonly WorldTreeSpiritBoss boss;
            private readonly ProjectileData bullet;
            public SeedBarrage(WorldTreeSpiritBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P6";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 1, 270f, 0f);
                opts.burstCount = 6;
                opts.burstIntervalSec = 0.3f;
                await BulletEmitter.Emit(BulletPatternId.StraightBurst, boss, opts, ct);
            }
        }

        // ───── Phase 3 ─────
        private sealed class Photosynthesis : IBossPattern
        {
            public string Id => "P7";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 재생 시각화 (실제 HP 복원은 시뮬레이션 단계에서)
                TelegraphRenderer.ShowCircle(b.transform.position, 1.5f, 1.5f, new Color(0.6f, 1f, 0.6f, 0.4f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class FinalRoots : IBossPattern
        {
            public string Id => "P8";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowCircle(b.transform.position + Vector3.left, 0.5f, 8f, new Color(0.4f, 0.6f, 0.3f, 0.6f));
                TelegraphRenderer.ShowCircle(b.transform.position + Vector3.right, 0.5f, 8f, new Color(0.4f, 0.6f, 0.3f, 0.6f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class OmniPetal : IBossPattern
        {
            private readonly WorldTreeSpiritBoss boss;
            private readonly ProjectileData bullet;
            public OmniPetal(WorldTreeSpiritBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P9";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 16, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }
    }
}
