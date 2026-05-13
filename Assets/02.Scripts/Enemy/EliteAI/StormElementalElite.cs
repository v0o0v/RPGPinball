using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI;
using RPGPinball.Enemy.BossAI.BulletPatterns;
using RPGPinball.Physics;

namespace RPGPinball.Enemy.EliteAI
{
    /// <summary>
    /// 봄 액트 엘리트: 폭풍의 정령. HP 20,000 / DEF 15% / 이동 20 U/s.
    /// 뇌신의 잔상 + 번개 분신 + 전방위 뇌격 + 번개 돌진.
    /// </summary>
    public class StormElementalElite : EliteBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        [Header("이동")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float patrolRadius = 4f;
        private Vector3 patrolTarget;
        private float nextTargetAt;

        [Header("잔상")]
        [SerializeField] private float afterimageIntervalSec = 0.5f;
        private float lastAfterimageAt;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new LightningClones(),
                new OmniThunder(this, smallBullet),
                new ThunderDash(this)
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || HasFled) return;

            // 무작위 patrol
            if (Time.time >= nextTargetAt)
            {
                patrolTarget = new Vector3(Random.Range(-patrolRadius, patrolRadius), Random.Range(-1f, 4f), 0f);
                nextTargetAt = Time.time + 0.8f;
            }
            transform.position = Vector3.MoveTowards(transform.position, patrolTarget, moveSpeed * Time.deltaTime);

            // 잔상 (감전 필드)
            float interval = IsEnraged ? 0.3f : afterimageIntervalSec;
            if (Time.time - lastAfterimageAt >= interval)
            {
                lastAfterimageAt = Time.time;
                float radius = IsEnraged ? 1.2f : 0.8f;
                TelegraphRenderer.ShowCircle(transform.position, radius, 3f, new Color(0.5f, 0.5f, 1f, 0.4f));
            }
        }

        private sealed class LightningClones : IBossPattern
        {
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = b.transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 1f), 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.4f, 8f, new Color(0.7f, 0.7f, 1f, 0.4f));
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class OmniThunder : IBossPattern
        {
            private readonly StormElementalElite boss; private readonly ProjectileData bullet;
            public OmniThunder(StormElementalElite b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 12, 0f, 0f);
                opts.speed = 12f;
                await BulletEmitter.Emit(BulletPatternId.Radial, boss, opts, ct);
            }
        }

        private sealed class ThunderDash : IBossPattern
        {
            private readonly StormElementalElite boss;
            public ThunderDash(StormElementalElite b) { boss = b; }
            public string Id => "P3";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                var ball = Object.FindFirstObjectByType<BallController>();
                Vector3 target = ball != null ? ball.transform.position : Vector3.zero;
                Vector3 start = boss.transform.position;
                Vector3 dir = (target - start).normalized;
                Vector3 end = start + dir * 8f;
                float dur = 0.4f;
                float t = 0f;
                while (t < dur && !ct.IsCancellationRequested)
                {
                    boss.transform.position = Vector3.Lerp(start, end, t / dur);
                    t += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            }
        }
    }
}
