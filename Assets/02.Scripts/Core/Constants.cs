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
        public const float FlipperSwingTime = 0.115f; // 2026-05-14: 0.15 → 0.115 (스윙 속도 30% 증가, 사용자 요청)
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

        // 범퍼 (데드존 페널티는 2026-05-13 제거 — 외벽 닫힌 통)
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
        // BossDeadzonePenalty(-20f)는 데드존 제거로 2026-05-13 삭제.
        // 보스가 직접 발행하는 강제 시간 페널티는 BossForcedTimePenalty 로 대체.
        public const float BossForcedTimePenalty = -15f;

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

        // ── 시드 (마일스톤 5) ──────────────────────────────────
        public const int SeedSaltActKey = 17;
        public const int SeedSaltStageKey = 53;
        public const int SeedSaltRetryKey = 131;
        public const int SeedSaltDailyKey = 2027;
        public const int SeedKstOffsetHours = 9;

        // ── 세그먼트 (마일스톤 5) ──────────────────────────────
        // 2026-05-15: 9.0 → 13.0 — 핀볼판 비율(가로 넓힘) 참조 이미지 매칭. 격자 col 4점이 ±4.5/±1.5로 spread
        // 2026-05-14: 13.0 → 16.9 — 가로 30% 추가 확장 (사용자 요청). 격자 col 4점이 ±6.45/±2.15로 spread, 외벽 ±8.45
        public const float SegPlayfieldWidth = 16.9f;
        public const float SegStageVerticalScreenCount = 3.0f;
        public const float SegMiddleHeightDefault = 3.5f;
        public const float SegHeightTolerance = 0.5f;
        public const float SegTopHeightDefault = 4.0f;
        public const float SegBottomHeightDefault = 6.0f;

        // ── 절차 생성 (마일스톤 5) ──────────────────────────────
        public const float ProcMutationChance = 0.05f;
        public const float ProcThemeRatioMin = 0.4f;
        public const int ProcRewardOrBuffMin = 1;
        public const int ProcTrialOverloadThreshold = 3;
        public const float ProcBudgetVarianceMin = -0.1f;
        public const float ProcBudgetVarianceMax = 0.1f;
        public const int ProcBudgetBase = 800;     // 2026-05-14: 100→240→800, 화면 50% 충진 + 좌우 대칭 페어 배치 보장
        public const int ProcBudgetPerStage = 60;  // 2026-05-14: 20→30→60

        // ── 기믹 가중치 (마일스톤 5) ───────────────────────────
        public const float GimmickDuplicateWeightDecay = 0.5f;
        public const float GimmickThemeWeightBonus = 2.0f;
        public const float GimmickSynergyWeightBonus = 1.5f;

        // ── 웨이브 (마일스톤 5) ────────────────────────────────
        public const int WaveCountBase = 1;
        public const int WaveCountPer5Stages = 1;
        public const int WaveCountMax = 7;
        public const float EliteSpawnRatePerStage = 0.05f;
        public const float EliteSpawnRateMax = 0.5f;
        public const int MassRushMinCount = 8;
        public const int MassRushMaxCount = 12;
        public const int EliteMinorityMinCount = 3;
        public const int EliteMinorityMaxCount = 5;
        public const int BossEscortMinCount = 4;
        public const int BossEscortMaxCount = 6;

        // ── 모디파이어 (마일스톤 5) ────────────────────────────
        public const float ModifierProbDevelopment = 0.5f;
        public const float ModifierProbClimaxExtra = 0.3f;

        // ── 휴식 / 이벤트 (마일스톤 5) ─────────────────────────
        public const float RestNodeBonusTime = 20f;
        public const float MysticAltarTimeCost = 30f;
        public const int MysticAltarSpReward = 1;
        public const float EventRestRatio = 0.7f;

        // ── 돌연변이 (마일스톤 5) ──────────────────────────────
        public const float MutationGoldMultiplier = 2.0f;
        public const float MutationRareRuneDelta = 0.15f;
        public const float MutationMiniaturePlayfieldScale = 0.6f;
        public const float MutationBossRushHpRatio = 0.5f;
        public const float MutationTimeRushTimeLimit = 60f;

        // ── 노드 유형 목표 비율 (30개 노드 중) ─────────────────
        public const float NodeRatioNormal = 0.7f;
        public const float NodeRatioElite = 0.1f;
        public const float NodeRatioEvent = 0.1f;
        public const float NodeRatioRest = 0.05f;
        public const float NodeRatioHidden = 0.05f;
        public const float HiddenNodeBaseChance = 0.05f;
        public const float EliteConvertChance = 0.10f;

        // ─────────────────────────────────────────────────────────
        // 마일스톤 6: 마을 시설 & 메타 시스템
        // ─────────────────────────────────────────────────────────

        // ── §대장간 ────────────────────────────────────────────
        public const int MaterialSwapGoldCost = 100;
        public const int FlipperVariantChangeGoldCost = 3000;
        public const int FlipperMaxLevel = 10;
        public const int CoreMaxLevel = 5;
        public const int CoreSlotsMain = 1;
        public const int CoreSlotsSub = 2;
        public const int FlipperVariantUnlockLevel = 4;

        // ── §룬 ────────────────────────────────────────────────
        public const int RuneFuseRequiredCount = 3;
        public const float RuneGradeNormalMul = 1.0f;
        public const float RuneGradeRareMul = 1.5f;
        public const float RuneGradeLegendaryMul = 2.25f;
        public const int RuneFuseGoldNormalToRare = 200;
        public const int RuneFuseGoldRareToLegendary = 500;
        public const int RuneSocketTier1 = 0;
        public const int RuneSocketTier2to4 = 1;
        public const int RuneSocketTier5to6 = 2;

        // ── §타로카드 ──────────────────────────────────────────
        public const int TarotPullGoldCost = 500;
        public const int TarotPullBossSoulCost = 3;
        public const int TarotEquipSlots = 3;
        public const int TarotPermanentRequiredCount = 10;
        public const int TarotPermanentGoldCost = 5000;
        public const float TarotDropCommonPct = 60f;
        public const float TarotDropRarePct = 25f;
        public const float TarotDropLegendaryPct = 10f;
        public const float TarotDropMythicPct = 5f;

        // ── §의뢰 ──────────────────────────────────────────────
        public const int DailyQuestSlotCount = 3;
        public const int DailyQuestRefreshHourKst = 0;
        public const int BountySlotCount = 3;
        public const int WeeklyQuestRewardGold = 2000;
        public const int DailyQuestRewardGoldPerOne = 300;

        // ── §열기구 ────────────────────────────────────────────
        public const int BalloonUpgrade1Gold = 1000;
        public const int BalloonUpgrade1Mana = 30;
        public const int BalloonUpgrade2Gold = 3000;
        public const int BalloonUpgrade2Mana = 80;
        public const int BalloonUpgrade3Gold = 8000;
        public const int BalloonUpgrade3Mana = 200;
        public const int BalloonStartingManaBonus = 20;
        public const float BalloonBossStartingTimeBonus = 10f;
        public const float BalloonHiddenChanceBonus = 0.15f;

        // ── §용병단 ────────────────────────────────────────────
        public const int MercenarySlotCount = 2;
        public const float EmergencyShieldDuration = 5f;
        public const float SandsOfTimeBonusSeconds = 15f;
        public const float FrenzyCharmDamageMultiplier = 2.0f;
        public const float FrenzyCharmSpeedMultiplier = 1.5f;
        public const float FrenzyCharmDuration = 10f;

        // ── §수련장 ────────────────────────────────────────────
        public const int SkillResetCost1 = 0;
        public const int SkillResetCost2 = 1000;
        public const int SkillResetCost3 = 3000;
        public const int SkillResetCost4 = 5000;
        public const int SkillDeckSlotCount = 4;
        public const int UltimateSkillMaxInDeck = 1;

        // ── §도감 ──────────────────────────────────────────────
        public const int GimmickResistThreshold1 = 1;
        public const int GimmickResistThreshold2 = 3;
        public const int GimmickResistThreshold3 = 7;
        public const int GimmickResistThreshold4 = 15;
        public const int GimmickResistThreshold5 = 30;
        public const float GimmickResistReductionPerLevel = 0.1f;
        public const int CollectionMilestone10Gold = 1000;
        public const int CollectionMilestone40Gold = 0;
        public const int CollectionMilestone80LegendaryTarot = 1;

        // ── §경제 ──────────────────────────────────────────────
        public const int BossGoldBase = 300;
        public const float ActMultiplier1 = 1.0f;
        public const float ActMultiplier2 = 1.8f;
        public const float ActMultiplier3 = 2.5f;
        public const float ActMultiplier4 = 3.5f;
        public const int BossSoulNormal = 2;
        public const int BossSoulFinal = 5;
        public const int EliteGoldMin = 30;
        public const int EliteGoldMax = 50;
        public const float GradeBonusS = 1.3f;
        public const float GradeBonusA = 1.0f;
        public const float GradeBonusB = 0.8f;
        public const float GradeBonusC = 0.5f;
        public const int StageGoldBase = 50;
        public const int StageGoldPerStage = 10;
        public const int StageManaCrystalBase = 5;
        public const int StageManaCrystalPerFiveStages = 1;

        // ── §해상도 (2026-05-15 픽스) ──────────────────────────
        // Design/Resolution_Spec.md 와 1:1 동기화. 변경 시 Player Settings 도 같이 수정.
        public const int ScreenWidthPx = 1080;
        public const int ScreenHeightPx = 1920;
        public const float ScreenAspect = 9f / 16f;        // 0.5625 (width/height)
        public const int UIReferenceWidth = 1080;          // Canvas Scaler Reference Resolution
        public const int UIReferenceHeight = 1920;
        public const float UICanvasMatchWidthOrHeight = 0f; // 0 = Width, 1 = Height
        // Village 씬 카메라 권장 ortho
        public const float CameraOrthoVillage = 10f;
        // Title/Result 씬 카메라 권장 ortho
        public const float CameraOrthoMenu = 5.625f;

        // ── §자산 경로 ─────────────────────────────────────────
        public const string KennyRunePackRoot = "Assets/50. External Assets/kenny/kenney_rune-pack/PNG";
        public const string KennyMedalsRoot = "Assets/50. External Assets/kenny/kenney_medals/PNG";
        public const string KennyPlatformerItemsRoot = "Assets/50. External Assets/kenny/kenney_platformer-art-deluxe/Base pack/Items";
        public const string KennyBlockPackRoot = "Assets/50. External Assets/kenny/kenney_block-pack/PNG/Default (64px)";
        public const string KennyBallAssetsRoot = "Assets/50. External Assets/kenny/kenney_rolling-ball-assets/PNG/Default";
        public const string KennyShapeCharsRoot = "Assets/50. External Assets/kenny/kenney_shape-characters/PNG/Default";

        // 영구 카드: 색상 (TarotGrade frameColor 매핑)
        public static readonly Color TarotFrameColorCommon = new Color(0.69f, 0.69f, 0.69f, 1f);    // #B0B0B0
        public static readonly Color TarotFrameColorRare = new Color(0.29f, 0.56f, 0.89f, 1f);     // #4A90E2
        public static readonly Color TarotFrameColorLegendary = new Color(0.61f, 0.35f, 0.71f, 1f); // #9B59B6
        public static readonly Color TarotFrameColorMythic = new Color(0.95f, 0.77f, 0.25f, 1f);   // #F1C40F
    }
}
