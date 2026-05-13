using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act2
{
    /// <summary>
    /// Act 2 보스 2-3: 크라켄 (스테이지 30, 최종).
    /// HP 21,600 / 3페이즈 / 촉수 8개(각 HP 800) / 잔여 촉수 비례 본체 DEF.
    /// 본체는 촉수 4개 이상 파괴 시 활성화.
    /// </summary>
    public class KrakenBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData largeBullet;

        [Header("촉수 설정")]
        [SerializeField] private int totalTentacles = 8;
        [SerializeField] private int tentaclesDestroyedToExposeBody = 4;
        private int tentaclesDestroyed;
        public int TentaclesAlive => Mathf.Max(0, totalTentacles - tentaclesDestroyed);
        public bool IsBodyExposed => tentaclesDestroyed >= tentaclesDestroyedToExposeBody;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new TentacleSweep(),
                new InkSpray(this, smallBullet),
                new TentacleSqueeze(),
                new BlackInkBlind(),
                new Whirlpool(),
                new TentacleSpear(),
                new TentacleRegen(this),
                new LaserSpin(this, smallBullet),
                new OmniInk(this, largeBullet)
            };
        }

        public override int GetEffectiveDefense()
        {
            int baseDef = base.GetEffectiveDefense();
            // 촉수 1개당 +3% (0~8 → 0~24%p)
            int bonus = TentaclesAlive * 3;
            return baseDef + bonus;
        }

        public void OnTentacleDestroyed() => tentaclesDestroyed++;

        // ───── Phase 1 ─────
        private sealed class TentacleSweep : IBossPattern
        {
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowArrow(b.transform.position, Vector3.left, 6f, 0.8f);
                return UniTask.CompletedTask;
            }
        }

        private sealed class InkSpray : IBossPattern
        {
            private readonly KrakenBoss boss; private readonly ProjectileData bullet;
            public InkSpray(KrakenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 8, 270f, 180f);
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        private sealed class TentacleSqueeze : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowCircle(new Vector3(-3f, 0f, 0f), 0.5f, 4f);
                TelegraphRenderer.ShowCircle(new Vector3(3f, 0f, 0f), 0.5f, 4f);
                return UniTask.CompletedTask;
            }
        }

        // ───── Phase 2 ─────
        private sealed class BlackInkBlind : IBossPattern
        {
            public string Id => "P4";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 시야 차단 임시 표시 (정식 셰이더는 마일스톤 8 인계)
                TelegraphRenderer.ShowCircle(Vector3.zero, 6f, 8f, new Color(0.1f, 0.1f, 0.1f, 0.6f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class Whirlpool : IBossPattern
        {
            public string Id => "P5";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowCircle(Vector3.zero, 2f, 6f, new Color(0.4f, 0.6f, 0.9f, 0.5f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class TentacleSpear : IBossPattern
        {
            public string Id => "P6";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowArrow(new Vector3(Random.Range(-3f, 3f), -3f, 0f), Vector3.up, 5f, 1f);
                return UniTask.CompletedTask;
            }
        }

        // ───── Phase 3 ─────
        private sealed class TentacleRegen : IBossPattern
        {
            private readonly KrakenBoss boss;
            public TentacleRegen(KrakenBoss b) { boss = b; }
            public string Id => "P7";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 촉수 2개 재생
                boss.tentaclesDestroyed = Mathf.Max(0, boss.tentaclesDestroyed - 2);
                return UniTask.CompletedTask;
            }
        }

        private sealed class LaserSpin : IBossPattern
        {
            private readonly KrakenBoss boss; private readonly ProjectileData bullet;
            public LaserSpin(KrakenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P8";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                // 블로킹 불가 — 별도 ProjectileData(blockableByFlipper=false) 권장. 임시로 같은 bullet 사용
                var opts = BulletPatternOptions.Default(bullet, 1, 0f, 0f);
                opts.burstCount = 8;
                opts.burstIntervalSec = 0.5f;
                opts.rotationSpeedDegPerSec = 90f;
                await BulletEmitter.Emit(BulletPatternId.RotatingRay, boss, opts, ct);
            }
        }

        private sealed class OmniInk : IBossPattern
        {
            private readonly KrakenBoss boss; private readonly ProjectileData bullet;
            public OmniInk(KrakenBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P9";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 12, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }
    }
}
