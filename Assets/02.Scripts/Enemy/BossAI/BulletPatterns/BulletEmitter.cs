using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Enemy.Pool;

namespace RPGPinball.Enemy.BossAI.BulletPatterns
{
    /// <summary>
    /// 탄막 패턴 디스패처. Emit(BulletPatternId, ...) 한 줄로 패턴 발사.
    /// 모든 발사는 ProjectilePool.Spawn 경유.
    /// </summary>
    public static class BulletEmitter
    {
        public static UniTask Emit(BulletPatternId pattern, BossBase emitter, BulletPatternOptions opts, CancellationToken ct)
        {
            switch (pattern)
            {
                case BulletPatternId.FanShot:        return FanShot.Execute(emitter, opts, ct);
                case BulletPatternId.Spiral:         return SpiralShot.Execute(emitter, opts, ct);
                case BulletPatternId.StraightBurst:  return StraightBurst.Execute(emitter, opts, ct);
                case BulletPatternId.RotatingRay:    return RotatingRay.Execute(emitter, opts, ct);
                case BulletPatternId.Homing:         return HomingShot.Execute(emitter, opts, ct);
                case BulletPatternId.Concentric:     return ConcentricShockwave.Execute(emitter, opts, ct);
                case BulletPatternId.Radial:         return RadialBurst.Execute(emitter, opts, ct);
                case BulletPatternId.Reverse:        return ReverseShot.Execute(emitter, opts, ct);
                default: return UniTask.CompletedTask;
            }
        }

        /// <summary>각도(deg) → 단위 벡터.</summary>
        public static Vector2 DirFromAngle(float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>풀에서 1발 발사. 풀 없으면 무시.</summary>
        public static ProjectileBase SpawnOne(ProjectileData data, Vector2 pos, Vector2 dir, float speedOverride = 0f)
        {
            if (ProjectilePool.Instance == null || data == null) return null;
            var pj = ProjectilePool.Instance.Spawn(data, pos, dir);
            if (pj != null && speedOverride > 0f)
            {
                var rb = pj.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = dir.normalized * speedOverride;
            }
            return pj;
        }
    }
}
