using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act2
{
    /// <summary>
    /// Act 2 보스 2-2: 유령 해적선장 (스테이지 20).
    /// 상단 좌우 이동 / 럼주 무적 / 유령 졸개 소환 / HP 10,400.
    /// 패턴: 대포 연사 / 유령 수하 / 포탄 투하 / 럼주 무적.
    /// </summary>
    public class PirateGhostBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData largeBullet;

        [Header("럼주 무적")]
        [SerializeField] private float rumImmuneSeconds = 4f;
        [SerializeField] private float rumStaggerSeconds = 1.5f;
        private bool isRumImmune;
        public bool IsRumImmune => isRumImmune;

        [Header("이동")]
        [SerializeField] private float topY = 4.5f;
        [SerializeField] private float rangeX = 3.5f;
        [SerializeField] private float patrolSpeed = 5f;
        private int dir = 1;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new CannonBurst(this, smallBullet),
                new GhostSummon(),
                new CannonballDrop(this, largeBullet),
                new RumImmunity(this)
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;
            Vector3 pos = transform.position;
            pos.y = topY;
            pos.x += dir * patrolSpeed * Time.deltaTime;
            if (pos.x > rangeX) { pos.x = rangeX; dir = -1; }
            else if (pos.x < -rangeX) { pos.x = -rangeX; dir = 1; }
            transform.position = pos;
        }

        public override int GetEffectiveDefense()
        {
            // 럼주 무적 중에는 데미지 0 (DEF를 매우 크게)
            return isRumImmune ? 9999 : base.GetEffectiveDefense();
        }

        private sealed class CannonBurst : IBossPattern
        {
            private readonly PirateGhostBoss boss;
            private readonly ProjectileData bullet;
            public CannonBurst(PirateGhostBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 1, 270f, 0f);
                opts.burstCount = 6;
                opts.burstIntervalSec = 0.2f;
                await BulletEmitter.Emit(BulletPatternId.StraightBurst, boss, opts, ct);
            }
        }

        private sealed class GhostSummon : IBossPattern
        {
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                // 졸개 소환은 마일스톤 4 단순화: Telegraph 원으로 표시
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = b.transform.position + new Vector3(-2f + i * 2f, -2f, 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.4f, 5f, new Color(0.6f, 0.6f, 1f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class CannonballDrop : IBossPattern
        {
            private readonly PirateGhostBoss boss;
            private readonly ProjectileData bullet;
            public CannonballDrop(PirateGhostBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P3";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                for (int i = 0; i < 2; i++)
                {
                    Vector3 dropAt = new Vector3(Random.Range(-3f, 3f), -1f, 0f);
                    TelegraphRenderer.ShowCircle(dropAt, 2f, 1.2f, new Color(1f, 0.4f, 0.2f, 0.4f));
                    await UniTask.Delay(System.TimeSpan.FromSeconds(0.5f), cancellationToken: ct);
                }
            }
        }

        private sealed class RumImmunity : IBossPattern
        {
            private readonly PirateGhostBoss boss;
            public RumImmunity(PirateGhostBoss b) { boss = b; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                float dur = boss.IsEnraged ? 3f : boss.rumImmuneSeconds;
                boss.isRumImmune = true;
                await UniTask.Delay(System.TimeSpan.FromSeconds(dur), cancellationToken: ct);
                if (boss != null)
                {
                    boss.isRumImmune = false;
                    // 경직 (Recovery에서 표현 — 마일스톤 4는 추가 처리 없음)
                }
            }
        }
    }
}
