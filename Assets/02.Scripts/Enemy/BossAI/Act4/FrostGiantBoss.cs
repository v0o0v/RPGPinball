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
    /// Act 4 보스 4-1: 서리거인 (스테이지 10).
    /// 얼음 바닥(마찰 0.1) + 거대 주먹 / HP 19,600 / DEF 30%.
    /// 패턴: 얼음 바닥(상시) / 거대 주먹 강타 / 빙결 장판 / 고드름 비 / 빙하 밀기.
    /// </summary>
    public class FrostGiantBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new GiantFistSmash(),
                new IcicleRain(this, smallBullet),
                new GlacierPush()
            };
        }

        private sealed class GiantFistSmash : IBossPattern
        {
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                // 화면 좌/우 절반 강타
                Rect area = new Rect(side * 0f - 4.5f * (side > 0 ? 0f : 1f), -3f, 4.5f, 6f);
                EventBus.Publish(new OnFlipperSpawnBlocked { Duration = 1.5f, Area = area });
                Vector3 center = new Vector3(side * 2.25f, 0f, 0f);
                TelegraphRenderer.ShowCircle(center, 2.5f, 1.5f, new Color(0.6f, 0.8f, 1f, 0.5f));
                // 영역 내 공 강제 reset + 시간 페널티 (데드존 제거 2026-05-13 — 낙사 대신 강제 reset 의미)
                var hits = UnityEngine.Physics2D.OverlapBoxAll(center, new Vector2(4.5f, 6f), 0f);
                bool anyHit = false;
                foreach (var h in hits)
                {
                    if (h.CompareTag(Constants.TagBall))
                    {
                        var ball = h.GetComponent<BallController>();
                        if (ball != null) { ball.ForceReset(); anyHit = true; }
                    }
                }
                if (anyHit)
                    EventBus.Publish(new OnTimePenalty { Delta = Constants.BossForcedTimePenalty });
                return UniTask.CompletedTask;
            }
        }

        private sealed class IcicleRain : IBossPattern
        {
            private readonly FrostGiantBoss boss; private readonly ProjectileData bullet;
            public IcicleRain(FrostGiantBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P4";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return UniTask.CompletedTask;
                int count = boss.IsEnraged ? 8 : 5;
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = new Vector3(Random.Range(-4f, 4f), 4f, 0f);
                    BulletEmitter.SpawnOne(bullet, pos, Vector2.down);
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class GlacierPush : IBossPattern
        {
            public string Id => "P5";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                Rect area = new Rect(-4.5f, -3f, 9f, 2f);
                TelegraphRenderer.ShowArrow(new Vector3(0f, -2f, 0f), Vector3.down, 2f, 1.2f, new Color(0.6f, 0.8f, 1f, 0.6f));
                // 하단 1/3 영역 강제 넉백
                var hits = UnityEngine.Physics2D.OverlapBoxAll(area.center, area.size, 0f);
                foreach (var h in hits)
                {
                    if (h.CompareTag(Constants.TagBall))
                    {
                        var rb = h.attachedRigidbody;
                        if (rb != null) rb.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);
                    }
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
