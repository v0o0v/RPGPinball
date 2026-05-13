using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Enemy.EliteAI
{
    /// <summary>
    /// 가을 액트 엘리트: 황금 고블린 왕. HP 15,000 / DEF 10% / 이동 20 U/s 지그재그.
    /// 도주형 — 10초 첫 타격 / 60초 처치 / 타격당 1초 ×2 가속 / 황금 폭탄 + 연막 + 함정.
    /// </summary>
    public class GoldenGoblinKingElite : EliteBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData largeBullet;

        [Header("이동")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float zigzagInterval = 0.5f;
        private Vector3 zigzagDir = Vector2.left;
        private float nextZigzagAt;
        private float speedBoostUntil;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new GoldBombThrow(this, largeBullet),
                new SmokeBomb(),
                new TreasureTrap()
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || HasFled) return;
            if (Time.time >= nextZigzagAt)
            {
                nextZigzagAt = Time.time + zigzagInterval;
                float angle = Random.Range(-30f, 30f);
                zigzagDir = Quaternion.Euler(0, 0, angle) * zigzagDir.normalized;
            }
            float speed = (Time.time < speedBoostUntil ? moveSpeed * 2f : moveSpeed);
            transform.position += zigzagDir.normalized * speed * Time.deltaTime;

            // 화면 경계 반사 (간단 클램프)
            Vector3 pos = transform.position;
            if (pos.x < -4f || pos.x > 4f) zigzagDir.x = -zigzagDir.x;
            if (pos.y < -2f || pos.y > 4f) zigzagDir.y = -zigzagDir.y;
            pos.x = Mathf.Clamp(pos.x, -4f, 4f);
            pos.y = Mathf.Clamp(pos.y, -2f, 4f);
            transform.position = pos;
        }

        public new void ApplyDamage(RPGPinball.Combat.DamageResult result)
        {
            // 타격당 1초 이동 속도 2배 (도주 가속)
            speedBoostUntil = Time.time + 1f;
            base.ApplyDamage(result);
        }

        private sealed class GoldBombThrow : IBossPattern
        {
            private readonly GoldenGoblinKingElite boss; private readonly ProjectileData bullet;
            public GoldBombThrow(GoldenGoblinKingElite b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P1";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return UniTask.CompletedTask;
                Vector3 pos = boss.transform.position;
                for (int i = 0; i < 2; i++)
                {
                    Vector2 dir = new Vector2(Random.Range(-1f, 1f), -1f).normalized;
                    BulletEmitter.SpawnOne(bullet, pos, dir);
                }
                return UniTask.CompletedTask;
            }
        }

        private sealed class SmokeBomb : IBossPattern
        {
            public string Id => "P2";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                TelegraphRenderer.ShowCircle(b.transform.position, 2f, 3f, new Color(0.4f, 0.4f, 0.4f, 0.5f));
                return UniTask.CompletedTask;
            }
        }

        private sealed class TreasureTrap : IBossPattern
        {
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                Vector3 pos = new Vector3(Random.Range(-3f, 3f), Random.Range(-2f, 3f), 0f);
                TelegraphRenderer.ShowCircle(pos, 0.4f, 10f, new Color(1f, 0.9f, 0.2f, 0.6f));
                return UniTask.CompletedTask;
            }
        }
    }
}
