using System;
using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Enemy.BossAI.BulletPatterns
{
    /// <summary>
    /// 탄막 패턴 발사 파라미터 묶음. BulletEmitter.Emit에 전달.
    /// </summary>
    [Serializable]
    public struct BulletPatternOptions
    {
        public ProjectileData projectile;
        public int count;
        public float baseAngleDeg;      // 부채꼴/방사 중심 각도 (0=오른쪽, 90=위, 180=왼쪽, 270=아래)
        public float arcDeg;            // 부채꼴 폭
        public float speed;             // 0이면 projectile.speed 사용
        public float rotationSpeedDegPerSec;
        public float burstIntervalSec;
        public int burstCount;
        public Transform targetForHoming;
        public bool spiralOutward;

        public float ResolveSpeed()
        {
            if (speed > 0f) return speed;
            return projectile != null ? projectile.speed : 8f;
        }

        public static BulletPatternOptions Default(ProjectileData pj, int count, float baseAngleDeg, float arcDeg)
        {
            return new BulletPatternOptions
            {
                projectile = pj,
                count = Mathf.Max(1, count),
                baseAngleDeg = baseAngleDeg,
                arcDeg = arcDeg,
                speed = 0f,
                rotationSpeedDegPerSec = 0f,
                burstIntervalSec = 0f,
                burstCount = 1
            };
        }
    }
}
