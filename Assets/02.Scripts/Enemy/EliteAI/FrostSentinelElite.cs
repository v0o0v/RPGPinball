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
    /// 겨울 액트 엘리트: 서리 파수꾼. HP 30,000 / DEF 35%(후방 15%) / 대형 / 이동 1.5 U/s.
    /// 정면 무적 방패 + 빙결 오라 + 방패 돌진 + 빙결 파동 + 고드름 방벽.
    /// </summary>
    public class FrostSentinelElite : EliteBase
    {
        [Header("탄막")]
        [SerializeField] private ProjectileData smallBullet;

        [Header("회전")]
        [SerializeField] private float facingTurnRate = 90f; // deg/s, 분노 시 150
        private Vector2 facingDir = Vector2.right;

        [Header("빙결 오라")]
        [SerializeField] private float auraRadius = 3f;
        [SerializeField] private float auraTriggerSec = 1.5f;
        private float ballInsideAuraSince = -1f;
        private Transform trackedBall;

        protected override IBossPattern[] BuildPatterns()
        {
            return new IBossPattern[]
            {
                new ShieldDash(this),
                new FrostWave(this, smallBullet),
                new IcicleBarrier(this)
            };
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || HasFled) return;

            var ball = Object.FindFirstObjectByType<BallController>();
            trackedBall = ball != null ? ball.transform : null;

            // 항상 공을 향함
            if (trackedBall != null)
            {
                Vector2 toBall = ((Vector2)trackedBall.position - (Vector2)transform.position).normalized;
                if (toBall.sqrMagnitude > 0.001f)
                {
                    float currentAngle = Mathf.Atan2(facingDir.y, facingDir.x) * Mathf.Rad2Deg;
                    float targetAngle = Mathf.Atan2(toBall.y, toBall.x) * Mathf.Rad2Deg;
                    float rate = IsEnraged ? 150f : facingTurnRate;
                    float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rate * Time.deltaTime);
                    facingDir = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));
                }
            }

            // 빙결 오라
            if (trackedBall != null)
            {
                float r = IsEnraged ? 4f : auraRadius;
                float dist = Vector2.Distance(trackedBall.position, transform.position);
                if (dist <= r)
                {
                    if (ballInsideAuraSince < 0f) ballInsideAuraSince = Time.time;
                    float threshold = IsEnraged ? 1.0f : auraTriggerSec;
                    if (Time.time - ballInsideAuraSince >= threshold)
                    {
                        ball.ApplyForcedSlow(0f, 2f);
                        EventBus.Publish(new OnTimePenalty { Delta = -5f });
                        ballInsideAuraSince = Time.time + 99f; // 일시 중복 방지
                    }
                }
                else
                {
                    ballInsideAuraSince = -1f;
                }
            }
        }

        public override int GetEffectiveDefense()
        {
            // 공이 정면 180° 안에 있으면 무적(9999), 아니면 후방 DEF 15
            var ed = EliteData;
            int rear = ed != null ? Mathf.RoundToInt(ed.backDefenseRatio * 100f) : 15;
            if (trackedBall == null) return rear;
            Vector2 toBall = ((Vector2)trackedBall.position - (Vector2)transform.position).normalized;
            float dot = Vector2.Dot(facingDir.normalized, toBall);
            return dot > 0f ? 9999 : rear;
        }

        private sealed class ShieldDash : IBossPattern
        {
            private readonly FrostSentinelElite boss;
            public ShieldDash(FrostSentinelElite b) { boss = b; }
            public string Id => "P1";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                var ball = Object.FindFirstObjectByType<BallController>();
                if (boss == null) return;
                Vector3 start = boss.transform.position;
                Vector3 target = ball != null ? ball.transform.position : start + Vector3.down * 4f;
                Vector3 dir = (target - start).normalized;
                Vector3 end = start + dir * 6f;
                float t = 0f;
                while (t < 0.75f && !ct.IsCancellationRequested)
                {
                    boss.transform.position = Vector3.Lerp(start, end, t / 0.75f);
                    t += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                // 빙결 장판 5초 (Telegraph 원으로 표시)
                TelegraphRenderer.ShowCircle(boss.transform.position, 1f, 5f, new Color(0.6f, 0.9f, 1f, 0.5f));
            }
        }

        private sealed class FrostWave : IBossPattern
        {
            private readonly FrostSentinelElite boss; private readonly ProjectileData bullet;
            public FrostWave(FrostSentinelElite b, ProjectileData p) { boss = b; bullet = p; }
            public string Id => "P2";
            public async UniTask Execute(BossBase b, CancellationToken ct)
            {
                if (bullet == null) return;
                var opts = BulletPatternOptions.Default(bullet, 12, 0f, 0f);
                opts.burstCount = 2;
                opts.burstIntervalSec = 0.8f;
                await BulletEmitter.Emit(BulletPatternId.Concentric, boss, opts, ct);
            }
        }

        private sealed class IcicleBarrier : IBossPattern
        {
            private readonly FrostSentinelElite boss;
            public IcicleBarrier(FrostSentinelElite b) { boss = b; }
            public string Id => "P3";
            public UniTask Execute(BossBase b, CancellationToken ct)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f);
                    TelegraphRenderer.ShowCircle(boss.transform.position + offset * 1.5f, 0.3f, 6f, new Color(0.6f, 0.9f, 1f, 0.5f));
                }
                return UniTask.CompletedTask;
            }
        }
    }
}
