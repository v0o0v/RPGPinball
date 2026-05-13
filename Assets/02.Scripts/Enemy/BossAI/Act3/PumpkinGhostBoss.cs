using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act3
{
    /// <summary>
    /// Act 3 보스 3-2: 호박머리 유령 (스테이지 20).
    /// 투명 3초 → 실체화 1.5초 / HP 14,500. 실체화 중에만 데미지.
    /// </summary>
    public class PumpkinGhostBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        [Header("투명/실체화 주기")]
        [SerializeField] private float invisSeconds = 3f;
        [SerializeField] private float visibleSeconds = 1.5f;

        private bool isVisible;
        private Collider2D[] bodyColliders;
        public bool IsVisible => isVisible;

        protected override void Awake()
        {
            base.Awake();
            bodyColliders = GetComponents<Collider2D>();
        }

        protected override void Start()
        {
            base.Start();
            StartPhaseCycleLoop().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid StartPhaseCycleLoop()
        {
            // 투명/실체화 주기 — 별도 코루틴으로 진행
            await Cysharp.Threading.Tasks.UniTask.Delay(System.TimeSpan.FromSeconds(0.5f));
            while (!IsDead && this != null)
            {
                SetVisible(false);
                float invDur = IsEnraged ? 4f : invisSeconds;
                await Cysharp.Threading.Tasks.UniTask.Delay(System.TimeSpan.FromSeconds(invDur));
                if (IsDead) break;
                SetVisible(true);
                float visDur = IsEnraged ? 1f : visibleSeconds;
                await Cysharp.Threading.Tasks.UniTask.Delay(System.TimeSpan.FromSeconds(visDur));
            }
        }

        private void SetVisible(bool v)
        {
            isVisible = v;
            if (bodyColliders != null)
            {
                foreach (var c in bodyColliders)
                    if (c != null) c.enabled = v;
            }
            // 자식 약점/일반 콜라이더는 별도 처리 없음 (간소화)
        }

        public override int GetEffectiveDefense()
        {
            // 투명 중에는 사실상 무적 (콜라이더 비활성으로 충돌 안 함, 이중 방어)
            return isVisible ? base.GetEffectiveDefense() : 9999;
        }

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new GhostFireSpiral(this, smallBullet),
                new GhostDash(this),
                new PumpkinBoom(this),
                new GhostSummon(this)
            };
        }

        private sealed class GhostFireSpiral : IBossPattern
        {
            private readonly PumpkinGhostBoss boss; private readonly ProjectileData bullet;
            public GhostFireSpiral(PumpkinGhostBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 6, 0f, 0f);
                opts.burstCount = 6;
                opts.burstIntervalSec = 0.3f;
                opts.rotationSpeedDegPerSec = 45f;
                await BulletEmitter.Emit(BulletPatternId.Spiral, boss, opts, ct);
            }
        }

        private sealed class GhostDash : IBossPattern
        {
            private readonly PumpkinGhostBoss boss;
            public GhostDash(PumpkinGhostBoss b) { boss = b; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                Vector3 start = boss.transform.position;
                Vector3 end = start + Vector3.down * 5f;
                float t = 0f;
                while (t < 0.6f && !ct.IsCancellationRequested)
                {
                    boss.transform.position = Vector3.Lerp(start, end, t / 0.6f);
                    t += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
        }

        private sealed class PumpkinBoom : IBossPattern
        {
            private readonly PumpkinGhostBoss boss;
            public PumpkinBoom(PumpkinGhostBoss b) { boss = b; }
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowCircle(boss.transform.position, 2f, 1f, new Color(1f, 0.5f, 0.1f, 0.5f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class GhostSummon : IBossPattern
        {
            private readonly PumpkinGhostBoss boss;
            public GhostSummon(PumpkinGhostBoss b) { boss = b; }
            public string Id => "P4";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 pos = new Vector3(Random.Range(-2f, 2f), -1f, 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.35f, 4f, new Color(0.5f, 0.5f, 1f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
