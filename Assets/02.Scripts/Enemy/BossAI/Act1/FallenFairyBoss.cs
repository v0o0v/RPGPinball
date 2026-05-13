using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act1
{
    /// <summary>
    /// Act 1 보스 1-2: 타락한 요정 (스테이지 20).
    /// 고속 텔레포트형 / HP 5,800 / 3페이즈.
    /// Phase 1: 순간이동 난사 + 환각 버섯 + 요정 먼지.
    /// Phase 2: 거울 복제 (분신 페이크).
    /// Phase 3: 분신 3체 분열 (총 HP 3등분).
    /// </summary>
    public class FallenFairyBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        [Header("이동")]
        [SerializeField] private float teleportRangeX = 3.5f;
        [SerializeField] private float teleportYTop = 4f;
        [SerializeField] private float teleportYBottom = 2f;

        // 분신 분열 상태
        private bool clonesSpawned;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new TeleportShot(this, smallBullet),
                new IllusionMushroom(this),
                new FairyDustSpiral(this, smallBullet),
                new MirrorClone(this),
                new CloneSplit(this)
            };
        }

        // ───── P1 순간이동 난사 (Phase 1+) ─────
        private sealed class TeleportShot : IBossPattern
        {
            private readonly FallenFairyBoss boss;
            private readonly ProjectileData bullet;
            public TeleportShot(FallenFairyBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                Vector3 newPos = new Vector3(
                    Random.Range(-boss.teleportRangeX, boss.teleportRangeX),
                    Random.Range(boss.teleportYBottom, boss.teleportYTop),
                    boss.transform.position.z);
                boss.transform.position = newPos;
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 3, 270f, 30f);
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        // ───── P2 환각 버섯 (Phase 1+) ─────
        private sealed class IllusionMushroom : IBossPattern
        {
            private readonly FallenFairyBoss boss;
            public IllusionMushroom(FallenFairyBoss b) { boss = b; }
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return UniTask.CompletedTask;
                int count = boss.CurrentPhase == BossPhase.P1 ? 2 : 3;
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = new Vector3(Random.Range(-3f, 3f), Random.Range(-1f, 2f), 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.6f, 5f, new Color(0.7f, 0.4f, 0.9f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        // ───── P3 요정 먼지 나선 (Phase 1+) ─────
        private sealed class FairyDustSpiral : IBossPattern
        {
            private readonly FallenFairyBoss boss;
            private readonly ProjectileData bullet;
            public FairyDustSpiral(FallenFairyBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P3";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null || bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 8, 270f, 0f);
                opts.burstCount = 3;
                opts.burstIntervalSec = 0.4f;
                opts.rotationSpeedDegPerSec = 30f;
                await BulletEmitter.Emit(BulletPatternId.Spiral, boss, opts, ct);
            }
        }

        // ───── P4 거울 복제 (Phase 2+) ─────
        private sealed class MirrorClone : IBossPattern
        {
            private readonly FallenFairyBoss boss;
            public MirrorClone(FallenFairyBoss b) { boss = b; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                // 0.5초 정지 후 잔상 2개 (페이크)
                Vector3 origin = boss.transform.position;
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
                TelegraphRenderer.ShowCircle(origin + Vector3.left * 1.5f, 0.5f, 1.5f, new Color(1f, 1f, 1f, 0.3f));
                TelegraphRenderer.ShowCircle(origin + Vector3.right * 1.5f, 0.5f, 1.5f, new Color(1f, 1f, 1f, 0.3f));
            }
        }

        // ───── P5 분신 분열 (Phase 3 전용, 1회) ─────
        private sealed class CloneSplit : IBossPattern
        {
            private readonly FallenFairyBoss boss;
            public CloneSplit(FallenFairyBoss b) { boss = b; }
            public string Id => "P5";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null || boss.clonesSpawned) return UniTask.CompletedTask;
                boss.clonesSpawned = true;
                // 분신 표시만 (실 게임플레이는 마일스톤 4 인계로 간소화)
                for (int i = 0; i < 3; i++)
                {
                    Vector3 offset = new Vector3(-2f + i * 2f, 0f, 0f);
                    TelegraphRenderer.ShowCircle(boss.transform.position + offset, 0.6f, 5f, new Color(0.6f, 0.3f, 1f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
