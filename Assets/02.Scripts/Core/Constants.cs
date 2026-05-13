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

        // ── 데미지 (Damage_Formula.md) ─────────────────────────
        public const float PlayerBaseDamage = 10f;
        public const float LevelDamageScale = 0.02f;
        public const float CritChanceDefault = 0.05f;
        public const float CritMultiplierDefault = 1.5f;
        public const float MithrilMagicMultiplier = 1.15f;
        public const int MultiplierStackLimit = 2; // 3개째부터 합연산 전환

        // ── 콤보 ────────────────────────────────────────────────
        public const float ComboResetSeconds = 3.0f;
        public const int ComboTier1 = 10; // 마나 ×1.5
        public const int ComboTier2 = 30; // 마나 ×2.0
        public const float ComboMultTier1 = 1.5f;
        public const float ComboMultTier2 = 2.0f;

        // ── 마나 ────────────────────────────────────────────────
        public const int ManaMax = 100;
        public const int ManaPerWall = 3;
        public const int ManaPerMonster = 8;
        public const int ManaPerBoss = 15;

        // ── 타이머 (Physics_Parameters.md) ─────────────────────
        public const float StageDefaultTime = 180f;
        public const float TimeRecoverCapPerStage = 60f;
        public const float ProjectilePenetratePenalty = -5f;
        public const float BossDeadzonePenalty = -20f;

        // ── 스킬 ────────────────────────────────────────────────
        public const float SkillCastDelay = 0.3f;
        public const int SkillDeckSize = 4;

        // ── 성장 시스템 (Skill_Tree_Formulas.md) ───────────────
        // 필요 XP = XPBase + level * XPPerLevel + level^2 * XPLevelSquared
        public const float XPBase = 80f;
        public const float XPPerLevel = 12f;
        public const float XPLevelSquared = 0.5f;
        public const int LevelCap = 100;
        // 오버레벨링 페널티: 자신 Lv - 적 Lv 차이로 XP 배율 적용
        public const int OverlevelThreshold1 = 5;
        public const float OverlevelMul1 = 0.5f;
        public const int OverlevelThreshold2 = 10;
        public const float OverlevelMul2 = 0.2f;

        // ── SP 경제 ────────────────────────────────────────────
        public const int SPPerLevel = 1;
        public const int SPPerBoss = 1;          // 보스 24마리 → 24 SP
        public const int SPPerActClear = 5;      // 4액트 × 5 = 20 SP
        public const int TotalSPGoal = 144;      // 99 + 24 + 20 = 143, 약간의 여유 포함

        // ── 멀티볼 하드캡 ──────────────────────────────────────
        public const int MultiBallHardCap = 5;
        public const int MultiBallUltimateCap = 8; // 원소 폭주 발동 시 일시 확장

        // ── A전환 공통 ─────────────────────────────────────────
        public const int TransformationManaCost = 40;
        public const float TransformationBaseDuration = 15f;
        public const float TransformationPerLevelDuration = 2f;

        // ── 궁극기 공통 ────────────────────────────────────────
        public const int UltimateManaCost = 100;
        public const float TimeDilationScale = 0.25f;

        // ── 넉백 거리 기본 (스킬별 SO에서 오버라이드 가능) ─────
        public const float KnockbackDistanceNormal = 1.5f;
        public const float KnockbackDistanceStrong = 3.0f;
        public const float KnockbackDistanceUltimate = 5.0f;
        public const float KnockbackDuration = 0.3f;

        // ── 점감 기본 계수 (참고값, 각 스킬 SO에서 개별 지정) ──
        public const float DiminishRateDefault = 0.95f;

        // ── 보스 공통 (Boss_Patterns.md §공통 규칙) ────────────
        public const float BossEnragedHpRatio = 0.3f;
        public const float BossEnragedAttackSpeedMul = 1.3f;
        public const float BossEnragedDensityMul = 1.5f;
        public const float BossEnragedRecoveryMul = 0.7f;
        public const float BossTelegraphMin = 0.5f;
        public const float BossTelegraphMax = 1.5f;
        public const float BossRecoveryMin = 1.0f;
        public const float BossRecoveryMax = 2.0f;

        // ── 엘리트 공통 (Elite_Bounty_Spec.md §공통 규칙) ──────
        public const float EliteEnragedHpRatio = 0.25f;
        public const float EliteEnragedAttackSpeedMul = 1.5f;
        public const float EliteRecoveryMin = 2.0f;
        public const float EliteRecoveryMax = 3.0f;

        // ── 탄막 공통 사양 (Boss_Patterns.md §탄막 공통 사양) ──
        public const float BulletSmallRadius = 0.15f;
        public const float BulletLargeRadius = 0.4f;
        public const float BulletSpecialRadius = 0.6f;
        public const float BulletSmallSpeed = 8.0f;
        public const float BulletLargeSpeed = 6.0f;
        public const float BulletSmallPenalty = -5f;
        public const float BulletLargePenalty = -8f;
        public const float BulletSpecialPenalty = -10f;

        // ── 탄막 풀링 ──────────────────────────────────────────
        public const int ProjectilePoolPrewarmSmall = 200;
        public const int ProjectilePoolPrewarmLarge = 60;
        public const int ProjectilePoolPrewarmSpecial = 20;

        // ── 최종 보스 검증 (Damage_Formula.md §밸런스 검증) ────
        public const float WinterQueenRequiredDps = 311f;

        // ── 보스 페이즈 임계치 기본 ────────────────────────────
        public const float BossPhase2HpRatioDefault = 0.6f;
        public const float BossPhase3HpRatioDefault = 0.3f;

        // ── 태그 (마일스톤 4) ──────────────────────────────────
        public const string TagWeakPoint = "WeakPoint";
    }
}
