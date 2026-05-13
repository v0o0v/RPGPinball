using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act2
{
    /// <summary>
    /// Act 2 보스 2-1: 거대 무장 게 (스테이지 10).
    /// 등껍질 무적·배 노출 시에만 데미지 / HP 8,100 / 좌우 왕복 4 U/s.
    /// 패턴: 집게 강타 / 거품 탄막 / 등껍질 돌진 / 배 노출.
    /// </summary>
    public class ArmoredCrabBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData largeBubble;

        [Header("이동")]
        [SerializeField] private float patrolRangeX = 3.5f;
        [SerializeField] private float patrolSpeed = 4f;
        private int dir = 1;

        // 배 노출 상태
        private bool exposingBelly;
        public bool IsExposingBelly => exposingBelly;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new ClawSmash(this),
                new BubbleVolley(this, largeBubble),
                new ShellDash(this),
                new BellyExpose(this)
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;
            // 좌우 왕복
            Vector3 pos = transform.position;
            pos.x += dir * patrolSpeed * Time.deltaTime;
            if (pos.x > patrolRangeX) { pos.x = patrolRangeX; dir = -1; }
            else if (pos.x < -patrolRangeX) { pos.x = -patrolRangeX; dir = 1; }
            transform.position = pos;
        }

        public override int GetEffectiveDefense()
        {
            // 배 노출 중에는 DEF 0
            return exposingBelly ? 0 : base.GetEffectiveDefense();
        }

        // ───── P1 집게 강타 (영역 한정 소환 차단) ─────
        private sealed class ClawSmash : IBossPattern
        {
            private readonly ArmoredCrabBoss boss;
            public ClawSmash(ArmoredCrabBoss b) { boss = b; }
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                Rect area = new Rect(side * 2.5f - 1.5f, -3f, 3f, 6f);
                EventBus.Publish(new OnFlipperSpawnBlocked { Duration = 2f, Area = area });
                TelegraphRenderer.ShowCircle(new Vector3(side * 2.5f, 0f, 0f), 1.5f, 2f, new Color(1f, 0.4f, 0.2f, 0.5f));
                if (boss.IsEnraged)
                {
                    // 분노 시 양쪽 동시
                    Rect other = new Rect(-side * 2.5f - 1.5f, -3f, 3f, 6f);
                    EventBus.Publish(new OnFlipperSpawnBlocked { Duration = 2f, Area = other });
                    TelegraphRenderer.ShowCircle(new Vector3(-side * 2.5f, 0f, 0f), 1.5f, 2f, new Color(1f, 0.4f, 0.2f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        // ───── P2 거품 탄막 ─────
        private sealed class BubbleVolley : IBossPattern
        {
            private readonly ArmoredCrabBoss boss;
            private readonly ProjectileData bubble;
            public BubbleVolley(ArmoredCrabBoss b, ProjectileData p) { boss = b; bubble = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bubble == null) return;
                int count = boss.IsEnraged ? 5 : 3;
                var opts = BulletPatternOptions.Default(bubble, count, 270f, 60f);
                opts.speed = 4f;
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        // ───── P3 등껍질 돌진 ─────
        private sealed class ShellDash : IBossPattern
        {
            private readonly ArmoredCrabBoss boss;
            public ShellDash(ArmoredCrabBoss b) { boss = b; }
            public string Id => "P3";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                Vector3 start = boss.transform.position;
                int side = boss.dir;
                Vector3 end = start + new Vector3(side * 7f, 0f, 0f);
                float dur = 1.0f;
                float elapsed = 0f;
                while (elapsed < dur && !ct.IsCancellationRequested && !boss.IsDead)
                {
                    boss.transform.position = Vector3.Lerp(start, end, elapsed / dur);
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
        }

        // ───── P4 배 노출 (P3 직후) ─────
        private sealed class BellyExpose : IBossPattern
        {
            private readonly ArmoredCrabBoss boss;
            public BellyExpose(ArmoredCrabBoss b) { boss = b; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                boss.exposingBelly = true;
                float exposeDur = boss.IsEnraged ? 2f : 3f;
                await UniTask.Delay(System.TimeSpan.FromSeconds(exposeDur), cancellationToken: ct);
                if (boss != null) boss.exposingBelly = false;
            }
        }
    }
}
