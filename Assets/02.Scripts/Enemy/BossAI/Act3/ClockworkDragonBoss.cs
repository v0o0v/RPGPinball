using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act3
{
    /// <summary>
    /// Act 3 보스 3-3: 태엽장치 드래곤 (스테이지 30, 최종).
    /// HP 35,000 / 3페이즈 / 톱니바퀴 3개(HP 1,500).
    /// Phase 1: 기계 화염 브레스 / 기어 런처 / 꼬리 스윕.
    /// Phase 2: 비행 모드 / 기어 폭탄 / 레이저 펜스.
    /// Phase 3: 과열 폭주 / 15초마다 5초 행동 불능(DEF 0%) / 자폭 톱니.
    /// </summary>
    public class ClockworkDragonBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData largeBullet;
        [SerializeField] private ProjectileData reflectBullet; // 벽 3회 반사

        [Header("Phase 3 과열")]
        [SerializeField] private float overheatIntervalSec = 15f;
        [SerializeField] private float overheatDurationSec = 5f;
        private bool isOverheated;
        public bool IsOverheated => isOverheated;
        private float lastOverheatTime;

        public override int GetEffectiveDefense()
        {
            return isOverheated ? 0 : base.GetEffectiveDefense();
        }

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new FireBreath(this),
                new GearLauncher(this, reflectBullet),
                new TailSweep(),
                new FlightMode(this),
                new GearBombDrop(this, largeBullet),
                new LaserFence(),
                new OverheatBurst(this),
                new OverheatStun(this),
                new SelfDestructGears(this, smallBullet)
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;
            // Phase 3 진입 후 주기적으로 과열
            if (CurrentPhase == BossPhase.P3 && !isOverheated && Time.time - lastOverheatTime >= overheatIntervalSec)
            {
                lastOverheatTime = Time.time;
                EnterOverheat().Forget();
            }
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid EnterOverheat()
        {
            isOverheated = true;
            await Cysharp.Threading.Tasks.UniTask.Delay(System.TimeSpan.FromSeconds(overheatDurationSec));
            isOverheated = false;
        }

        // ───── Phase 1 ─────
        private sealed class FireBreath : IBossPattern
        {
            private readonly ClockworkDragonBoss boss;
            public FireBreath(ClockworkDragonBoss b) { boss = b; }
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 폭 2.0U 화염빔 (수직 아래 방향)
                TelegraphRenderer.ShowArrow(boss.transform.position, Vector3.down, 8f, 3f, new Color(1f, 0.4f, 0.1f, 0.7f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class GearLauncher : IBossPattern
        {
            private readonly ClockworkDragonBoss boss; private readonly ProjectileData bullet;
            public GearLauncher(ClockworkDragonBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 2, 250f, 40f);
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        private sealed class TailSweep : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowArrow(b.transform.position, Vector3.right, 6f, 0.8f);
                return UniTask.CompletedTask;
            }
        }

        // ───── Phase 2 ─────
        private sealed class FlightMode : IBossPattern
        {
            private readonly ClockworkDragonBoss boss;
            public FlightMode(ClockworkDragonBoss b) { boss = b; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 상공 비행 — 임시: 잠깐 위로 이동
                Vector3 origin = boss.transform.position;
                Vector3 high = origin + Vector3.up * 1.5f;
                float t = 0f;
                while (t < 1.5f && !ct.IsCancellationRequested)
                {
                    boss.transform.position = Vector3.Lerp(origin, high, t / 1.5f);
                    t += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
        }

        private sealed class GearBombDrop : IBossPattern
        {
            private readonly ClockworkDragonBoss boss; private readonly ProjectileData bullet;
            public GearBombDrop(ClockworkDragonBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P5";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 dropAt = new Vector3(Random.Range(-3f, 3f), -2f, 0f);
                    TelegraphRenderer.ShowCircle(dropAt, 1.5f, 1.2f, new Color(1f, 0.4f, 0.1f, 0.5f));
                    await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f), cancellationToken: ct);
                }
            }
        }

        private sealed class LaserFence : IBossPattern
        {
            public string Id => "P6";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowArrow(new Vector3(-4f, 0f, 0f), Vector3.right, 8f, 2f, new Color(1f, 0.2f, 0.2f, 0.7f));
                return UniTask.CompletedTask;
            }
        }

        // ───── Phase 3 ─────
        private sealed class OverheatBurst : IBossPattern
        {
            private readonly ClockworkDragonBoss boss;
            public OverheatBurst(ClockworkDragonBoss b) { boss = b; }
            public string Id => "P7";
            public UniTask Execute(BossBase b, CancellationToken ct) => UniTask.CompletedTask; // 단순 보조 패턴
        }

        private sealed class OverheatStun : IBossPattern
        {
            private readonly ClockworkDragonBoss boss;
            public OverheatStun(ClockworkDragonBoss b) { boss = b; }
            public string Id => "P8";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 과열 트리거: Update에서 자동 활성화. 패턴 자체는 안내용.
                return UniTask.CompletedTask;
            }
        }

        private sealed class SelfDestructGears : IBossPattern
        {
            private readonly ClockworkDragonBoss boss; private readonly ProjectileData bullet;
            public SelfDestructGears(ClockworkDragonBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P9";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 4, 0f, 0f);
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }
    }
}
