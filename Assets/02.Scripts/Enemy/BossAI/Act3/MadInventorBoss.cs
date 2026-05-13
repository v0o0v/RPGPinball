using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.BossAI.Act3
{
    /// <summary>
    /// Act 3 보스 3-1: 미치광이 발명가 (스테이지 10).
    /// 톱니바퀴 방패·터렛 소환·반사 탄막 / HP 12,500.
    /// 패턴: 톱니 방패 회전 / 터렛 설치 / 반사 탄막 / 기어 폭탄.
    /// </summary>
    public class MadInventorBoss : BossBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;
        [SerializeField] private ProjectileData reflectBullet;   // wallBounceLimit ≥ 1
        [SerializeField] private ProjectileData largeBullet;

        // 톱니 방패 활성 시 DEF 보정
        private bool shieldActive;

        public override int GetEffectiveDefense()
        {
            int baseDef = base.GetEffectiveDefense();
            return shieldActive ? baseDef + 20 : baseDef;
        }

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new GearShield(this),
                new TurretDeploy(this),
                new ReflectShot(this, reflectBullet),
                new GearBomb(this, largeBullet)
            };
        }

        private sealed class GearShield : IBossPattern
        {
            private readonly MadInventorBoss boss;
            public GearShield(MadInventorBoss b) { boss = b; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (boss == null) return;
                boss.shieldActive = true;
                await UniTask.Delay(System.TimeSpan.FromSeconds(4f), cancellationToken: ct);
                if (boss != null) boss.shieldActive = false;
            }
        }

        private sealed class TurretDeploy : IBossPattern
        {
            private readonly MadInventorBoss boss;
            public TurretDeploy(MadInventorBoss b) { boss = b; }
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                int count = boss.IsEnraged ? 3 : 2;
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = new Vector3(Random.Range(-3f, 3f), Random.Range(-1f, 2f), 0f);
                    TelegraphRenderer.ShowCircle(pos, 0.4f, 8f, new Color(0.7f, 0.5f, 0.2f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class ReflectShot : IBossPattern
        {
            private readonly MadInventorBoss boss;
            private readonly ProjectileData bullet;
            public ReflectShot(MadInventorBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P3";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 4, 270f, 90f);
                await BulletEmitter.Emit(BulletPatternId.FanShot, boss, opts, ct);
            }
        }

        private sealed class GearBomb : IBossPattern
        {
            private readonly MadInventorBoss boss;
            private readonly ProjectileData bullet;
            public GearBomb(MadInventorBoss b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P4";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 dropAt = new Vector3(Random.Range(-3f, 3f), -2f, 0f);
                    TelegraphRenderer.ShowCircle(dropAt, 1.5f, 1.5f, new Color(1f, 0.4f, 0.1f, 0.5f));
                    await UniTask.Delay(System.TimeSpan.FromSeconds(0.6f), cancellationToken: ct);
                }
            }
        }
    }
}
