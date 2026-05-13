using NUnit.Framework;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Enemy.BossAI.BulletPatterns;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 탄막 패턴 옵션의 기본 동작 검증 (수치 계산 위주).
    /// 실제 발사는 ProjectilePool 필요 → PlayMode에서 검증.
    /// </summary>
    public class BulletPatternTests
    {
        [Test]
        public void BulletPatternOptions_Default_ClampsCountToOne()
        {
            var pj = ScriptableObject.CreateInstance<ProjectileData>();
            var opts = BulletPatternOptions.Default(pj, 0, 0f, 0f);
            Assert.AreEqual(1, opts.count);
        }

        [Test]
        public void BulletPatternOptions_ResolveSpeed_FallsBackToProjectileSpeed()
        {
            var pj = ScriptableObject.CreateInstance<ProjectileData>();
            pj.speed = 7.5f;
            var opts = BulletPatternOptions.Default(pj, 5, 0f, 0f);
            Assert.AreEqual(7.5f, opts.ResolveSpeed(), 0.001f);
        }

        [Test]
        public void BulletPatternOptions_ResolveSpeed_RespectsOverride()
        {
            var pj = ScriptableObject.CreateInstance<ProjectileData>();
            pj.speed = 7.5f;
            var opts = BulletPatternOptions.Default(pj, 5, 0f, 0f);
            opts.speed = 12f;
            Assert.AreEqual(12f, opts.ResolveSpeed(), 0.001f);
        }

        [Test]
        public void DirFromAngle_0Deg_PointsRight()
        {
            var v = BulletEmitter.DirFromAngle(0f);
            Assert.AreEqual(1f, v.x, 0.001f);
            Assert.AreEqual(0f, v.y, 0.001f);
        }

        [Test]
        public void DirFromAngle_90Deg_PointsUp()
        {
            var v = BulletEmitter.DirFromAngle(90f);
            Assert.AreEqual(0f, v.x, 0.001f);
            Assert.AreEqual(1f, v.y, 0.001f);
        }

        [Test]
        public void DirFromAngle_270Deg_PointsDown()
        {
            var v = BulletEmitter.DirFromAngle(270f);
            Assert.AreEqual(0f, v.x, 0.001f);
            Assert.AreEqual(-1f, v.y, 0.001f);
        }

        [Test]
        public void DirFromAngle_180Deg_PointsLeft()
        {
            var v = BulletEmitter.DirFromAngle(180f);
            Assert.AreEqual(-1f, v.x, 0.001f);
            Assert.AreEqual(0f, v.y, 0.001f);
        }
    }
}
