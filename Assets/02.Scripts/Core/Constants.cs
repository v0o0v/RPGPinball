using UnityEngine;

namespace RPGPinball.Core
{
    public static class Constants
    {
        // 월드 물리
        public static readonly Vector2 Gravity = new Vector2(0f, -15f);
        public const float FixedTimestep = 0.01f;
        public const float MaxAngularVelocity = 1000f;

        // 플레이필드
        public const float PlayfieldWidth = 9.0f;
        public const float SegmentHeight = 12.0f;
        public const float FlipperZoneHeight = 3.0f;
        public const float DeadZoneHeight = 1.5f;
        public const float WallThickness = 0.5f;

        // 공
        public const float BallRadius = 0.25f;
        public const float BallDefaultMass = 1.0f;
        public const float BallLaunchSpeed = 18.0f;
        public const float BallMaxSpeed = 40.0f;
        public const float BallMinSpeed = 2.0f;
        public const float BallAngularDrag = 0.05f;
        public const float BallLinearDrag = 0.0f;

        // 플리퍼
        public const float FlipperLength = 2.5f;
        public const float FlipperThickness = 0.3f;
        public const float FlipperSpawnAnimTime = 0.08f;
        public const float FlipperActiveTime = 0.5f;
        public const float FlipperSwingTime = 0.15f;
        public const float FlipperDespawnAnimTime = 0.12f;
        public const float FlipperCooldown = 1.5f;
        public const float FlipperCooldownMin = 0.5f;
        public const float FlipperSwingImpulse = 25.0f;
        public const float FlipperStaticImpulse = 12.0f;
        public const float FlipperMaxAngle = 75f;
        // 스윙 회전 (왼쪽 플리퍼 기준; 오른쪽은 부호 반전)
        // 시작각 음수: 패들 끝이 아래로 처져 있음 → 끝각 양수: 위로 휘둘러 올림
        public const float FlipperSwingStartAngle = -35f;
        public const float FlipperSwingEndAngle = 25f;
        public const float FlipperTouchRadius = 1.0f;
        public const float FlipperMinSpawnGap = 0.8f;
        public const float FlipperBlockCooldownBonus = 0.3f;

        // 낙사 / 리스폰
        public const float RespawnDelay = 1.0f;
        public const float RespawnInvincibleTime = 1.5f;
        public const float RespawnLaunchSpeed = 18.0f;

        // 카메라
        public const float CameraSmoothing = 0.15f;
        public const float CameraVerticalOffset = 2.0f;
        public const float CameraBossZoom = 1.2f;
        public const float CameraMultiballZoomPerBall = 0.1f;

        // 탄막
        public const float ProjectileDefaultSpeed = 8.0f;

        // 데드존 / 범퍼
        public const float DeadzonePenalty = -10.0f;
        public const float BumperImpulse = 18.0f;

        // 태그
        public const string TagBall = "Ball";
        public const string TagDeadZone = "DeadZone";
        public const string TagBoss = "Boss";
        public const string TagMonster = "Monster";
        public const string TagFlipper = "Flipper";
        public const string TagProjectile = "Projectile";
        public const string TagBumper = "Bumper";
        public const string TagWall = "Wall";
    }
}
