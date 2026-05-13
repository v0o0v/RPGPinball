using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act1
{
    /// <summary>
    /// Act 1 보스 1-1: 거대 식충식물 (스테이지 10).
    /// 화면 중앙 고정 / 약점=꽃봉오리(본체 상단) / HP 4,500 / DEF 20%.
    /// 4종 패턴: 덩굴 채찍 / 포자 산탄 / 덩굴 장벽 / 꽃가루 안개.
    /// </summary>
    public class FleshPlantBoss : BossBase
    {
        [Header("탄막 데이터 (Inspector에서 할당)")]
        [SerializeField] private ProjectileData smallBullet;

        [Header("덩굴 채찍 (P1) 옵션")]
        [SerializeField] private float vineKnockbackForce = 4f;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new VineWhip(this),
                new SporeShot(this, smallBullet),
                new VineBarrier(this),
                new PollenMist(this)
            };
        }

        // ───── P1 덩굴 채찍 ─────
        private sealed class VineWhip : IBossPattern
        {
            private readonly FleshPlantBoss boss;
            public VineWhip(FleshPlantBoss b) { boss = b; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                Vector2 origin = boss.transform.position;
                int side = Random.value < 0.5f ? -1 : 1;
                Vector3 telegraphPos = origin + new Vector2(side * 2f, -2f);
                TelegraphRenderer.ShowArrow(origin, Vector3.right * side, 4f, 0.5f, new Color(0.4f, 0.8f, 0.4f, 0.6f));
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: ct);

                // 수평 스윕 — 공 접촉 검사 (간소화: 원형 영역 검사)
                var hits = UnityEngine.Physics2D.OverlapBoxAll(telegraphPos, new Vector2(4f, 1f), 0f);
                foreach (var h in hits)
                {
                    if (h.CompareTag(Constants.TagBall))
                    {
                        var rb = h.attachedRigidbody;
                        if (rb != null)
                        {
                            Vector2 dir = new Vector2(side, 0.3f).normalized;
                            rb.AddForce(dir * boss.vineKnockbackForce, ForceMode2D.Impulse);
                        }
                    }
                }
            }
        }

        // ───── P2 포자 산탄 ─────
        private sealed class SporeShot : IBossPattern
        {
            private readonly FleshPlantBoss boss;
            private readonly ProjectileData bullet;
            public SporeShot(FleshPlantBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null || bullet == null) return;
                int count = boss.IsEnraged ? 8 : 5;
                float arc = boss.IsEnraged ? 90f : 60f;
                var opts = BulletPatternOptions.Default(bullet, count, 270f, arc); // 270° = 아래
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
                await UniTask.Yield();
            }
        }

        // ───── P3 덩굴 장벽 ─────
        private sealed class VineBarrier : IBossPattern
        {
            private readonly FleshPlantBoss boss;
            public VineBarrier(FleshPlantBoss b) { boss = b; }
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return UniTask.CompletedTask;
                int count = boss.IsEnraged ? 3 : 2;
                Vector3 origin = boss.transform.position;
                // 중단 세그먼트(y=0 부근)에 덩굴 배치 (임시 — Telegraph 원으로 표시)
                for (int i = 0; i < count; i++)
                {
                    float x = -2f + 2f * i;
                    TelegraphRenderer.ShowCircle(new Vector3(x, 0f, 0f), 0.5f, 5f, new Color(0.2f, 0.6f, 0.2f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        // ───── P4 꽃가루 안개 ─────
        private sealed class PollenMist : IBossPattern
        {
            private readonly FleshPlantBoss boss;
            public PollenMist(FleshPlantBoss b) { boss = b; }
            public string Id => "P4";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return UniTask.CompletedTask;
                // 약점(상단) 주변 시야 차단 — Sprite 알파로 임시 표시
                Vector3 weakPos = boss.transform.position + Vector3.up * 1.0f;
                TelegraphRenderer.ShowCircle(weakPos, 2f, 3f, new Color(0.8f, 0.8f, 0.5f, 0.5f));
                return UniTask.CompletedTask;
            }
        }
    }
}
