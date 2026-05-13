using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Enemy.BossAI.BulletPatterns
{
    /// <summary>
    /// 부채꼴 N발 발사. baseAngleDeg를 중심으로 arcDeg 폭에 등간격 배치.
    /// </summary>
    public static class FanShot
    {
        public static UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null || opts.count <= 0) return UniTask.CompletedTask;
            float halfArc = opts.arcDeg * 0.5f;
            float start = opts.baseAngleDeg - halfArc;
            float step = opts.count > 1 ? opts.arcDeg / (opts.count - 1) : 0f;
            Vector2 pos = emitter.transform.position;
            for (int i = 0; i < opts.count; i++)
            {
                float angle = start + step * i;
                var dir = BulletEmitter.DirFromAngle(angle);
                BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
            }
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 본체 중심 회전 나선형. count발이 한 번에 발사되고, executeSeconds 동안 회전.
    /// 단순화: 한 번에 N발 발사 → 외부에서 burst 사용 권장.
    /// </summary>
    public static class SpiralShot
    {
        public static async UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null || opts.count <= 0) return;
            int bursts = Mathf.Max(1, opts.burstCount);
            float interval = Mathf.Max(0.05f, opts.burstIntervalSec);
            float rotation = 0f;
            float perBurstStep = bursts > 1 ? opts.arcDeg / (bursts - 1) : 0f;

            for (int b = 0; b < bursts; b++)
            {
                Vector2 pos = emitter.transform.position;
                for (int i = 0; i < opts.count; i++)
                {
                    float angle = opts.baseAngleDeg + rotation + i * (360f / opts.count);
                    var dir = BulletEmitter.DirFromAngle(angle);
                    BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
                }
                rotation += perBurstStep + opts.rotationSpeedDegPerSec * interval;
                if (b < bursts - 1)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: ct);
            }
        }
    }

    /// <summary>
    /// 직선 연사. baseAngleDeg 방향으로 burstCount발 burstIntervalSec 간격 발사.
    /// </summary>
    public static class StraightBurst
    {
        public static async UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null) return;
            int bursts = Mathf.Max(1, opts.burstCount);
            float interval = Mathf.Max(0f, opts.burstIntervalSec);
            for (int i = 0; i < bursts; i++)
            {
                if (emitter == null) return;
                Vector2 pos = emitter.transform.position;
                var dir = BulletEmitter.DirFromAngle(opts.baseAngleDeg);
                BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
                if (interval > 0f && i < bursts - 1)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: ct);
            }
        }
    }

    /// <summary>
    /// 회전 광선/탄막. count개의 탄막이 360° 등간격으로 동시 발사되며, 외부 회전(rotationSpeedDegPerSec)은
    /// 발사 시점 각도만 회전시키는 단순화 구현. (실 회전 빔은 보스별 커스텀 처리)
    /// </summary>
    public static class RotatingRay
    {
        public static async UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null || opts.count <= 0) return;
            int bursts = Mathf.Max(1, opts.burstCount);
            float interval = Mathf.Max(0.05f, opts.burstIntervalSec);
            float currentBase = opts.baseAngleDeg;
            for (int b = 0; b < bursts; b++)
            {
                Vector2 pos = emitter.transform.position;
                for (int i = 0; i < opts.count; i++)
                {
                    float angle = currentBase + i * (360f / opts.count);
                    var dir = BulletEmitter.DirFromAngle(angle);
                    BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
                }
                currentBase += opts.rotationSpeedDegPerSec * interval;
                if (b < bursts - 1)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: ct);
            }
        }
    }

    /// <summary>
    /// 공 추적 유도 1발. targetForHoming이 없으면 baseAngleDeg 방향 직선 1발.
    /// ProjectileData.homing=true 필수.
    /// </summary>
    public static class HomingShot
    {
        public static UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null) return UniTask.CompletedTask;
            Vector2 pos = emitter.transform.position;
            Vector2 dir;
            if (opts.targetForHoming != null)
            {
                dir = ((Vector2)opts.targetForHoming.position - pos).normalized;
                if (dir.sqrMagnitude < 0.0001f) dir = BulletEmitter.DirFromAngle(opts.baseAngleDeg);
            }
            else
            {
                dir = BulletEmitter.DirFromAngle(opts.baseAngleDeg);
            }
            var pj = BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
            if (pj != null && opts.targetForHoming != null)
                pj.SetHomingTarget(opts.targetForHoming, opts.rotationSpeedDegPerSec > 0f ? opts.rotationSpeedDegPerSec : 240f);
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 동심원 충격파. count(=동심원 단계 수) × 360° 방사. burstIntervalSec 간격으로 단계별 확산.
    /// 외곽으로 갈수록 탄막 수 증가.
    /// </summary>
    public static class ConcentricShockwave
    {
        public static async UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null) return;
            int stages = Mathf.Max(1, opts.burstCount > 0 ? opts.burstCount : opts.count);
            int countPerStage = opts.count > 0 ? opts.count : 12;
            float interval = Mathf.Max(0.1f, opts.burstIntervalSec);
            for (int s = 0; s < stages; s++)
            {
                Vector2 pos = emitter.transform.position;
                for (int i = 0; i < countPerStage; i++)
                {
                    float angle = opts.baseAngleDeg + i * (360f / countPerStage);
                    var dir = BulletEmitter.DirFromAngle(angle);
                    BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
                }
                if (s < stages - 1)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: ct);
            }
        }
    }

    /// <summary>
    /// 360° 등간격 방사 1회. count발이 동시에 발사됨.
    /// </summary>
    public static class RadialBurst
    {
        public static UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null || opts.count <= 0) return UniTask.CompletedTask;
            Vector2 pos = emitter.transform.position;
            for (int i = 0; i < opts.count; i++)
            {
                float angle = opts.baseAngleDeg + i * (360f / opts.count);
                var dir = BulletEmitter.DirFromAngle(angle);
                BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
            }
            return UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// 시간 역행. count발을 baseAngleDeg 방향으로 발사 후 burstIntervalSec(기본 3s) 뒤 역방향 복귀.
    /// 시계탑 P4 패턴 전용.
    /// </summary>
    public static class ReverseShot
    {
        public static async UniTask Execute(BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            if (emitter == null || opts.projectile == null || opts.count <= 0) return;
            float reverseDelay = opts.burstIntervalSec > 0f ? opts.burstIntervalSec : 3.0f;
            var spawned = new System.Collections.Generic.List<ProjectileBase>(opts.count);
            float halfArc = opts.arcDeg * 0.5f;
            float start = opts.baseAngleDeg - halfArc;
            float step = opts.count > 1 ? opts.arcDeg / (opts.count - 1) : 0f;
            Vector2 pos = emitter.transform.position;

            for (int i = 0; i < opts.count; i++)
            {
                float angle = start + step * i;
                var dir = BulletEmitter.DirFromAngle(angle);
                var pj = BulletEmitter.SpawnOne(opts.projectile, pos, dir, opts.speed);
                if (pj != null) spawned.Add(pj);
            }

            // 일정 시간 후 모든 활성 탄막 역방향 전환
            try { await UniTask.Delay(System.TimeSpan.FromSeconds(reverseDelay), cancellationToken: ct); }
            catch (System.OperationCanceledException) { return; }

            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null || !spawned[i].gameObject.activeSelf) continue;
                var rb = spawned[i].GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = -rb.linearVelocity;
            }
        }
    }
}
