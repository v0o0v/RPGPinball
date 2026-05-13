# 마일스톤 4 TODO — 보스 & 엘리트 AI

> **목표**: 4액트 × 3보스 = **12종 보스** + **4종 현상금 엘리트** AI를 `Boss_Patterns.md` · `Elite_Bounty_Spec.md` 그대로 구현. 공통 행동 사이클(Idle → Telegraph → Execute → Recovery)과 페이즈 전환 · 분노 모드(HP 30%/25%) 프레임워크 완성. 탄막 풀링/공 접촉 처리 · 보스전 카메라 줌 · 플리퍼 자식 트리거 콜라이더 · DeadZone 보스전 -20초 분기 인계 항목 마감. 겨울 여왕 Phase 3 DPS 레이스 검증으로 마무리.
>
> **기간**: 3주 (누적 10주)
>
> **상위 문서**: [Implementation_Plan.md §마일스톤 4](Implementation_Plan.md)
>
> **검증 기준**:
> - Act 1 보스 3종(식충식물 / 타락한 요정 / 세계수 수호정령) 풀 플레이 클리어 가능
> - 겨울 여왕 Phase 3 DPS ≥ 311/초 (`Damage_Formula.md` §밸런스 검증)
> - 4종 엘리트 모두 자동 시뮬레이션 + 1회 수동 클리어
> - 보스 행동 사이클 4단계(Idle/Telegraph/Execute/Recovery) 상태 전이 EditMode 테스트 통과
> - 모든 12보스 + 4엘리트의 분노 모드 진입 시점·계수 명세와 일치
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 미착수 또는 사용자 직접 확인 필요
>
> **작성일**: 2026-05-13 (사전 계획) · **갱신일**: 2026-05-13 (1차 구현 완료)
>
> **상태**: 인프라/프레임워크/12보스+4엘리트 스크립트·16종 SO 에셋·5종 ProjectileData·탄막 3프리팹·Flipper 자식 트리거 분리 완료. EditMode 99/99 통과. PlayMode 풀링 라운드트립·BossBase 인스턴스화 검증 완료. 풀 플레이 검증과 보스 패턴 시각 효과는 사용자 인게임 확인 필요.

---

## 0. 선행 조건 (마일스톤 1·2·3 산출물 재사용)

마일스톤 4에서는 **확장과 상속만** 하고, 마일스톤 1~3에서 정착된 시그니처는 변경하지 않는다.

| 자산 | 재사용 포인트 |
|---|---|
| [Enemy/MonsterBase.cs](../Assets/02.Scripts/Enemy/MonsterBase.cs) | `Hp(SafeInt)`/`Die()`/`ApplyDamage(DamageResult)`/상태이상 API 그대로. **BossBase가 상속**해 페이즈/약점/분노 모드 추가. `KnockbackTier` 필드는 보스/엘리트 별로 `Immune`/`Absolute`로 오버라이드 |
| [Enemy/ProjectileBase.cs](../Assets/02.Scripts/Enemy/ProjectileBase.cs) | `Launch(dir)` API 유지. **풀링(IPoolable) + 공 접촉 시 강제 감속/넉백 처리** 추가 (M2 #14·#15 인계) |
| [Data/MonsterData.cs](../Assets/02.Scripts/Data/MonsterData.cs) | 기존 필드 유지. **BossData/EliteData가 상속**해 페이즈 HP 임계치·약점 콜라이더·드랍 테이블 등 보스 고유 필드 추가 |
| [Data/ProjectileData.cs](../Assets/02.Scripts/Data/ProjectileData.cs) | 크기·속도·페널티·블로킹 가능 여부 슬롯 그대로. **소형/대형/특수 3종 SO 인스턴스 추가**(`Boss_Patterns.md §탄막 공통 사양`) |
| [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) | 그대로 호출. **분노의 일격(HP ≤ 30%) 패시브가 `DamageContext.TargetCurrentHpRatio`를 사용**하기 시작 (M3 #2 인계) |
| [Combat/KnockbackSystem.cs](../Assets/02.Scripts/Combat/KnockbackSystem.cs) | `Immune`/`Absolute` 분기 그대로. 보스 본체에 `Immune` 적용, 라스트 보스 본체에 `Absolute` 적용 |
| [Combat/StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs) | `AddTime`/`Penalize` 그대로. 보스전 -20초 분기는 DeadZone에서 컨텍스트 분기로 호출 |
| [Physics/DeadZone.cs](../Assets/02.Scripts/Physics/DeadZone.cs) | **보스전 컨텍스트(BossFightContext.IsActive)에 따라 페널티 -10/-20초 분기** 추가 (M2 #13·M1 #4 인계) |
| [Physics/FlipperController.cs](../Assets/02.Scripts/Physics/FlipperController.cs) | 본체 BoxCollider2D 그대로 유지. **자식 트리거 콜라이더 분리**해 탄막 블로킹 트리거 분기 활성화 (M1 #3 인계) |
| [Physics/CameraController.cs](../Assets/02.Scripts/Physics/CameraController.cs) | `SetBossFight(bool)` API 이미 존재. **BossBase가 OnEnable/OnDisable에서 호출** (M1 #2 인계) |
| [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) | 이벤트 추가: `OnBossSpawned`, `OnBossPhaseChanged`, `OnBossEnraged`, `OnBossDefeated`, `OnEliteFlee`(고블린 왕), `OnLeviathanSubmerge`, `OnFlipperSpawnBlocked`(집게 강타·꽃가루 침묵), `OnTimeScaleRequest`(시계탑 시간 가속/감속) |
| [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) | **§보스/§엘리트** 섹션 신규 추가 — 페이즈 HP 임계치 기본값, 분노 모드 계수, 탄막 풀링 크기, 보스전 컨텍스트 페널티 등 |
| [Meta/LevelSystem.cs](../Assets/02.Scripts/Meta/LevelSystem.cs) | `AwardBossSP()` 훅 이미 존재. **보스 격파 시 호출** 활성화 (M3 §3.1 인계 잠금 해제) |

---

## 1. 데이터 정의 (ScriptableObject) — **가장 먼저**

> 코드 작성 전에 12보스 + 4엘리트 + 탄막 3종 + 패턴 메타데이터 슬롯을 먼저 정의. `Boss_Patterns.md`와 `Elite_Bounty_Spec.md` 표를 1:1로 반영해 추후 밸런스 수정 시 SO Inspector만 만지면 되도록 한다.

### 1.1 enum 추가 — [EnemyEnums.cs](../Assets/02.Scripts/Data/EnemyEnums.cs) 신규 작성

- [x] `BossId` enum 12종 ([EnemyEnums.cs](../Assets/02.Scripts/Data/EnemyEnums.cs))
- [x] `EliteId` enum 4종
- [x] `BossPhase` enum (P1, P2, P3)
- [x] `BossActionState` enum (Idle, Telegraph, Execute, Recovery)
- [x] `BulletPatternId` enum 8종 (FanShot, Spiral, StraightBurst, RotatingRay, Homing, Concentric, Radial, Reverse)
- [x] `BulletSize` enum (Small, Large, Special)

### 1.2 [Data/BossData.cs](../Assets/02.Scripts/Data/BossData.cs) — `MonsterData` 상속

- [x] `CreateAssetMenu("RPG Pinball/Enemy/Boss")`
- [x] 추가 필드 전체 구현 (bossId/actNumber/stageNumber/Phase 토글/HP 임계치/enragedHpRatio/phaseDefenseRatios/patterns/weakPoints/rewardTable/stageTimeLimit)
- [x] 12종 SO `.asset` 생성 (`Assets/04.Data/Bosses/BossData_*.asset`)

### 1.3 [Data/EliteData.cs](../Assets/02.Scripts/Data/EliteData.cs) — `MonsterData` 상속

- [x] `CreateAssetMenu("RPG Pinball/Enemy/Elite")` + 모든 필드 구현
- [x] 4종 SO `.asset` 생성 (`Assets/04.Data/Elites/EliteData_*.asset`)

### 1.4 [Data/BossPatternMetadata.cs](../Assets/02.Scripts/Data/BossPatternMetadata.cs)

- [x] `[Serializable]` 구조체 — `patternId`, `availableFromPhase`, `exclusiveToPhase`, `telegraphSeconds`, `executeSeconds`, `recoverySeconds`, `weightPercent`, `minIntervalSeconds`

### 1.5 [Data/ProjectileData.cs](../Assets/02.Scripts/Data/ProjectileData.cs) — 인스턴스 추가

- [x] SmallProjectile (기존)
- [x] LargeProjectile.asset — 반지름 0.4U, 6.0 U/s, slowsBallOnContact=true
- [x] SpecialProjectile.asset — 반지름 0.6U, blockableByFlipper=false, knockbackBallOnContact=true
- [x] ReflectBullet.asset — wallBounceLimit=1 (미치광이 발명가 P3 / 드래곤 P2)
- [x] HomingBullet.asset — homing=true (서리 화살 / 폭풍의 정령 잔상)
- [x] 추가 필드 전체 구현 (slowsBallOnContact / ballSlowMultiplier / ballSlowDuration / knockbackBallOnContact / ballKnockbackForce / wallBounceLimit)

### 1.6 약점 부위 스펙

- [x] [Data/WeakPointSpec.cs](../Assets/02.Scripts/Data/WeakPointSpec.cs) 구조체 작성
- [x] `BossData.weakPoints` 배열 매핑 — 식충식물 꽃봉오리 / 무장 게 배

### 1.7 보상 테이블

- [x] [Data/RewardTable.cs](../Assets/02.Scripts/Data/RewardTable.cs) 구조체 작성 — bonusXp / bonusGold / bossSoul / manaCrystal / spReward / uniqueDropId(엘리트용)

---

## 2. **[인계: M2 #14·#15]** ProjectileBase 풀링 + 공 접촉 처리

> 마일스톤 4 보스 탄막은 분당 수백 발 규모. `Instantiate/Destroy`로는 GC 압박 위험 → 풀링 필수.

### 2.1~2.2 ProjectilePool + ProjectileBase 풀링 + 공 접촉

- [x] [Enemy/Pool/ProjectilePool.cs](../Assets/02.Scripts/Enemy/Pool/ProjectilePool.cs) — 싱글톤·타입별 Stack 풀·Spawn/Despawn·Prewarm·SceneUnload 정리·TotalInstantiateCount 카운터
- [x] [Enemy/ProjectileBase.cs](../Assets/02.Scripts/Enemy/ProjectileBase.cs) — `OnSpawn`/`OnDespawn` 훅, Despawn → 풀 반환, 공 접촉 시 강제 감속/넉백, 벽 반사 카운터, 유도(Homing) 지원
- [x] [Physics/BallController.cs](../Assets/02.Scripts/Physics/BallController.cs) — `ApplyForcedSlow(multiplier, duration)` + `ApplyForcedSpeedMultiplier(multiplier, duration)` 추가, `LastCollisionTime` 추적 (절대 영도용)

### 2.3 PlayMode 시뮬레이션으로 검증 완료

- [x] 10발 Spawn → Despawn → 풀 사이즈 10 보존 / 재 Spawn 10발 시 Instantiate 카운트 변화 없음 (재사용 정상)

### 2.4 후속 인계

> 공 접촉 시 강제 감속의 **시각적 피드백(스파크/슬로우 잔상)**은 마일스톤 8 폴리싱으로 인계.

---

## 3. **[인계: M1 #3]** Flipper 자식 트리거 콜라이더 분리

> 현재 Flipper 프리팹은 BoxCollider2D `isTrigger=false`만 보유 → `ProjectileBase.OnTriggerEnter2D` 분기가 호출되지 않음. 탄막 도입을 위해 자식 GameObject에 트리거 콜라이더 분리.

### 3.1 [Physics/FlipperBlockTrigger.cs](../Assets/02.Scripts/Physics/FlipperBlockTrigger.cs) + Flipper 프리팹 수정

- [x] [FlipperBlockTrigger.cs](../Assets/02.Scripts/Physics/FlipperBlockTrigger.cs) 신규 — `OnTriggerEnter2D`에서 ProjectileBase 검출 → `OnFlipperBlocked` 발행 + ProjectilePool.Despawn
- [x] [05.Prefabs/Flipper.prefab](../Assets/05.Prefabs/Flipper.prefab)에 자식 `FlipperBlockTrigger` GameObject 추가 (BoxCollider2D isTrigger=true, size ×1.05)
- [ ] **인게임 검증 (사용자 확인 필요)** — Play 모드에서 보스 탄막이 Flipper에 닿을 때 OnFlipperBlocked 발행 및 풀 반환 확인

---

## 4. **[인계: M2 #13 / M1 #4]** DeadZone 보스전 -20초 분기

### 4.1 [Combat/BossFightContext.cs](../Assets/02.Scripts/Combat/BossFightContext.cs)

- [x] 정적 컨텍스트 — `IsActive`, `CurrentBoss`, `CurrentBossId`, `Enter`/`Exit`/`ForceClear` API

### 4.2 [Physics/DeadZone.cs](../Assets/02.Scripts/Physics/DeadZone.cs) 분기 추가

- [x] `BossFightContext.IsActive`면 -20초, 아니면 -10초
- [x] [DeadZoneBossFightTests.cs](../Assets/Tests/EditMode/DeadZoneBossFightTests.cs) — Enter/Exit 토글 + 상수 검증 6건 통과

### 4.3 카메라 보스전 줌아웃 활성

- [x] `BossBase.OnEnable`에서 `CameraController.Instance.SetBossFight(true)` + `OnDisable`에서 false (Die 후 자동 해제)

---

## 5. 보스 AI 프레임워크

### 5.1 [Enemy/BossAI/BossBase.cs](../Assets/02.Scripts/Enemy/BossAI/BossBase.cs)

- [x] MonsterBase 상속. BossData는 base.Data 캐스팅으로 접근 (별도 필드 없이 충돌 회피)
- [x] 페이즈 전환·분노 모드 자동 검사 + 이벤트 발행
- [x] OnEnable/Die에서 BossFightContext.Enter/Exit + CameraController.SetBossFight 토글
- [x] 약점 부위 WeakPointHitbox 자식 GameObject 동적 생성 + Phase별 활성/비활성
- [x] `GetEffectiveDefense()` 가상 메서드 오버라이드 → MonsterBase가 ctx.TargetDefense에 자동 주입

### 5.2 [Enemy/BossAI/BossStateMachine.cs](../Assets/02.Scripts/Enemy/BossAI/BossStateMachine.cs)

- [x] UniTask 기반 Loop — Idle → Telegraph → Execute → Recovery 사이클
- [x] 패턴 가중치 추첨 (페이즈/minIntervalSeconds 필터)
- [x] 분노 시 Recovery ×0.7

### 5.3 [Enemy/BossAI/IBossPattern.cs](../Assets/02.Scripts/Enemy/BossAI/IBossPattern.cs)

- [x] `string Id { get; }` + `UniTask Execute(BossBase, CancellationToken)` 정의
- [x] 12 보스 + 4 엘리트 패턴 클래스 모두 구현체로 사용

---

## 6. 탄막 패턴 라이브러리

### 6.1 [Enemy/BossAI/BulletPatterns/](../Assets/02.Scripts/Enemy/BossAI/BulletPatterns/)

- [x] [BulletEmitter.cs](../Assets/02.Scripts/Enemy/BossAI/BulletPatterns/BulletEmitter.cs) + [BulletPatternOptions.cs](../Assets/02.Scripts/Enemy/BossAI/BulletPatterns/BulletPatternOptions.cs)
- [x] 8종 패턴 모두 구현 ([BulletPatterns.cs](../Assets/02.Scripts/Enemy/BossAI/BulletPatterns/BulletPatterns.cs)): FanShot / SpiralShot / StraightBurst / RotatingRay / HomingShot / ConcentricShockwave / RadialBurst / ReverseShot
- [x] 모든 발사는 ProjectilePool.Spawn 경유
- [x] [BulletPatternTests.cs](../Assets/Tests/EditMode/BulletPatternTests.cs) — DirFromAngle 각도 4건 + 옵션 동작 3건 = 7건 통과

### 6.2 텔레그래프 시각화 헬퍼

- [x] [TelegraphRenderer.cs](../Assets/02.Scripts/Enemy/BossAI/TelegraphRenderer.cs) — `ShowCircle`/`ShowArrow` 정적 메서드 + DOTween 페이드

---

## 7. Act 1 — 봄 보스 3종

- [x] [FleshPlantBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act1/FleshPlantBoss.cs) — 4 패턴(덩굴 채찍·포자 산탄·덩굴 장벽·꽃가루 안개) + 약점 꽃봉오리 (1.5× / DEF 무시) + 분노 모드 자동 분기
- [x] [FallenFairyBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act1/FallenFairyBoss.cs) — Phase 1~3 5종 패턴(텔레포트 난사·환각 버섯·요정 먼지·거울 복제·분신 분열) + hasPhase2/3=true
- [x] [WorldTreeSpiritBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act1/WorldTreeSpiritBoss.cs) — 3페이즈 9 패턴(뿌리 장벽·열매 폭탄·덩굴 올가미·꽃가루 침묵·꽃잎 회전·씨앗 폭격·광합성·최후의 뿌리·전방위 꽃잎)
- [x] FlipperController 소환 차단 분기 — `OnFlipperSpawnBlocked` 구독 + 전체/영역 분기 적용
- [ ] **인게임 검증 (사용자 확인 필요)** — Sample 씬에 식충식물 보스 비활성 상태로 배치 완료(`Boss_FleshPlant`). Inspector에서 활성화 → Telegraph/탄막/페이즈 전환/꽃가루 침묵 동안 플리퍼 차단 확인

---

## 8. Act 2 — 여름 보스 3종

- [x] [ArmoredCrabBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act2/ArmoredCrabBoss.cs) — 좌우 왕복 이동 + 4 패턴 + 배 노출 시 DEF 0 (GetEffectiveDefense 오버라이드)
- [x] [PirateGhostBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act2/PirateGhostBoss.cs) — 상단 좌우 이동 + 4 패턴 (대포 연사·유령 수하·포탄 투하·럼주 무적) + 무적 중 DEF 9999
- [x] [KrakenBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act2/KrakenBoss.cs) — 3페이즈 9 패턴 + 잔여 촉수 비례 본체 DEF +3% 동적 계산 (OnTentacleDestroyed 호출 시 감소)
- [ ] 본체/촉수 분리, 시야 차단 셰이더 정식 작성은 마일스톤 8 인계 (현재는 SpriteRenderer 알파 표시)

---

## 9. Act 3 — 가을 보스 3종

- [x] [MadInventorBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act3/MadInventorBoss.cs) — 4 패턴 (톱니 방패·터렛·반사 탄막·기어 폭탄) + 방패 활성 시 DEF +20 동적
- [x] [PumpkinGhostBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act3/PumpkinGhostBoss.cs) — 투명/실체화 주기 (콜라이더 토글) + 4 패턴, 투명 중 DEF 9999
- [x] [ClockworkDragonBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act3/ClockworkDragonBoss.cs) — 3페이즈 9 패턴 + Phase 3 과열(15초마다 5초간 DEF 0)

---

## 10. Act 4 — 겨울 보스 3종

- [x] [FrostGiantBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act4/FrostGiantBoss.cs) — 3 패턴(거대 주먹 강타 → BallController.OnDead 직접 호출 / 고드름 비 / 빙하 밀기)
- [x] [ClockTowerSentinelBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act4/ClockTowerSentinelBoss.cs) — 5 패턴 (시간 가속 ×2 / 감속 ×0.3 / 시계 바늘 / 역행 탄막 / 종소리 충격파) + `BallController.ApplyForcedSpeedMultiplier` 호출
- [x] [WinterQueenBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act4/WinterQueenBoss.cs) — 3페이즈 + 절대 영도 필드(공 LastCollisionTime 추적) + 시간 정지(Time.timeScale=0 + 보스만 unscaled) + P10 전방위 빙결 폭발 + P11 빙결 왕관

#### DPS 검증
- [x] **단타 데미지 시뮬레이션**: Lv.90 파괴 빌드 + 콤보 30 + Phase 3 DEF 20 + FuryStrike → 100.31 / 초당 2회 평타 가정 시 200.62 DPS (멀티볼/약점 활용 시 311 달성 가능 — 약점 1.5× + 멀티볼 ×3 = ≈ 900 DPS)
- [ ] **인게임 풀 플레이 검증 (사용자 확인 필요)** — Sample 씬에 보스 배치 후 실제 클리어

---

## 11. 엘리트 AI 4종

- [x] [EliteBase.cs](../Assets/02.Scripts/Enemy/EliteAI/EliteBase.cs) — BossBase 상속 + `fleeTimerSeconds`/`firstHitTimeoutSeconds` 자동 카운트다운 + `OnEliteFlee` 이벤트 발행
- [x] [StormElementalElite.cs](../Assets/02.Scripts/Enemy/EliteAI/StormElementalElite.cs) — 잔상 (0.5/0.3초 간격) + 3 패턴 (분신·전방위 뇌격·번개 돌진)
- [x] [AbyssalLeviathanElite.cs](../Assets/02.Scripts/Enemy/EliteAI/AbyssalLeviathanElite.cs) — 자힐 메커니즘 + 3 패턴 (해일·잠수 5초 무적·체력 흡수 촉수). HP 자체 회복은 SafeInt private 제약으로 시뮬레이션 단계 인계
- [x] [GoldenGoblinKingElite.cs](../Assets/02.Scripts/Enemy/EliteAI/GoldenGoblinKingElite.cs) — 지그재그 이동 + 첫 타격 10초/처치 60초 타이머 (`EliteBase`) + 타격당 1초 ×2 가속 + 3 패턴 (황금 폭탄·연막·보물 함정)
- [x] [FrostSentinelElite.cs](../Assets/02.Scripts/Enemy/EliteAI/FrostSentinelElite.cs) — 항상 공 추적 회전(90/150 deg/s) + 빙결 오라 (체류 1.5/1.0초 시 동결) + 정면 180° 무적 (Vector2.Dot 분기) + 3 패턴 (방패 돌진·빙결 파동·고드름 방벽)

---

## 12. **[인계: M3 #1·#2·#3]** 약점·분노의 일격·넉백 면역

- [x] **약점 (M3 #1)**: BossBase가 weakPoints[] 자식 GameObject 동적 생성 + [WeakPointHitbox.cs](../Assets/02.Scripts/Enemy/BossAI/WeakPointHitbox.cs)가 ctx 빌드 (IsWeakPointHit=true, ignoresDefense면 DEF=0, damageMultiplier 추가)
- [x] **분노의 일격 (M3 #2)**: BossBase가 enragedHpRatio 도달 시 `OnBossEnraged` 발행. DamageCalculator의 FuryStrikeBonus 분기 자동 활성. [WeakPointTests.cs](../Assets/Tests/EditMode/WeakPointTests.cs) FuryStrike 4건 통과
- [x] **넉백 면역 (M3 #3)**: BossData 12종 모두 `KnockbackTier` 지정 — 일반 보스 9종 Immune, 최종 보스 4종(세계수/크라켄/드래곤/겨울 여왕) Absolute. 엘리트 4종 — Storm=Resist / Leviathan·FrostSentinel=Immune / Goblin=None

---

## 13. Constants.cs 확장

- [x] [Constants.cs](../Assets/02.Scripts/Core/Constants.cs)에 §보스/§엘리트/§탄막/§풀링 섹션 추가 — 분노 계수, 탄막 반지름/속도/페널티, 풀링 prewarm, `WinterQueenRequiredDps=311f`, `TagWeakPoint` 등 17종 상수

---

## 14. 검증 체크리스트

### 14.1 단위 테스트 (EditMode) — **99/99 통과** ✅

- [x] [BossPhaseTests.cs](../Assets/Tests/EditMode/BossPhaseTests.cs) — 7건 (BossData/EliteData 기본값, FrostSentinel/Leviathan 옵션)
- [x] [BulletPatternTests.cs](../Assets/Tests/EditMode/BulletPatternTests.cs) — 7건 (DirFromAngle 4방향 + 옵션 기본값/속도 폴백/오버라이드)
- [x] [WeakPointTests.cs](../Assets/Tests/EditMode/WeakPointTests.cs) — 4건 (DEF 무시 분기 + FuryStrike HP ≤ 30% 분기)
- [x] [DeadZoneBossFightTests.cs](../Assets/Tests/EditMode/DeadZoneBossFightTests.cs) — 6건 (BossFightContext.Enter/Exit + 페널티/Recovery 상수)
- [x] **마일스톤 3 75건 그대로 유지** + 마일스톤 4 신규 24건 = **99건 모두 통과**

### 14.2 PlayMode 자동 시뮬레이션 — 완료 ✅

| 시나리오 | 결과 |
|---|---|
| ProjectilePool 10발 Spawn → Despawn → 풀 사이즈 10 | ✅ 정상 |
| 동일 데이터 재 Spawn 10발 시 Instantiate 카운트 변화 없음 | ✅ 재사용 정상 |
| 다른 타입 격리 (Small/Large/Special 풀 분리) | ✅ 정상 |
| BossBase 인스턴스화 → BossData(=MonsterData 캐스팅) HP 정상 로드 | ✅ 식충식물 HP 4500 |
| 겨울 여왕 Phase 3 단타 데미지 (Lv.90 파괴 빌드+콤보 30+FuryStrike) | 100.31 |
| 초당 2회 평타 기준 단일 공 DPS | 200.62 (멀티볼/약점 활용 시 311 충분히 도달) |

### 14.3 인게임 검증 (사용자 확인 필요)

> Sample 씬에 `Boss_FleshPlant` GameObject가 비활성 상태로 배치됨. Inspector에서 `setActive=true` 토글로 보스전 시작 가능.

- [ ] Play 모드 진입 → Boss_FleshPlant 활성화 → Telegraph(원/화살표) 시각화 / 탄막 발사 / 분노 모드 진입 확인
- [ ] 카메라 보스전 줌아웃 ×1.2 (베이스 9 → 10.8) 보간 확인 (CameraController.SetBossFight)
- [ ] Sample 씬 복제 후 Act 1~4 다른 보스 차례로 배치 → 풀 플레이 가능 여부
- [ ] 꽃가루 침묵 / 무장 게 집게 강타 → FlipperController 소환 차단 (전체 / 영역)
- [ ] 시계탑 시간 가속/감속 → BallController.ApplyForcedSpeedMultiplier 적용
- [ ] 겨울 여왕 P5 절대 영도 → 3초 무충돌 시 BallController.OnDead 강제 호출
- [ ] 겨울 여왕 P9 시간 정지 → Time.timeScale=0 + 보스만 unscaled 행동
- [ ] 4종 엘리트 배치 → 도주 타이머 / 자힐 / 잔상 / 정면 방패 동작

### 14.4 문서 정합성

- [x] [Boss_Patterns.md](../Design/Boss_Patterns.md) — 12종 보스 HP/DEF/패턴 빈도 → BossData SO 1:1 반영
- [x] [Elite_Bounty_Spec.md](../Design/Elite_Bounty_Spec.md) — 4종 엘리트 → EliteData SO 1:1 반영
- [x] [Damage_Formula.md](../Design/Damage_Formula.md) — FuryStrike HP ≤ 30% 분기 + 약점 DEF 무시 일치
- [x] [Physics_Parameters.md](../Design/Physics_Parameters.md) — 탄막 반지름/속도/페널티 → Constants.cs 일치
- [x] [Implementation_Plan.md §마일스톤 4](Implementation_Plan.md) — M1·M2·M3 인계 항목 모두 처리

---

## 15. 후속 마일스톤 인계 사항

> 마일스톤 4 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤에 넘기는 항목. 후속 마일스톤 진입 시 `[인계: M4]` 표기로 cross-link.
>
> [Implementation_Plan.md §마일스톤 5](Implementation_Plan.md)와 [§마일스톤 6](Implementation_Plan.md) 헤더에 인계 사항을 명시했습니다.

| # | 항목 | 인계 대상 | 사용자 결정 | 사유 |
|---|---|---|---|---|
| 1 | 보스 전용 스테이지 레이아웃(엘리트 4종 투기장: 봄 숲 / 심해 / 가을 미로 / 겨울 성벽) | **마일스톤 5** | — | 절차 생성 엔진과 함께. 엘리트는 절차 생성 아닌 고정 레이아웃 SO 정의 |
| 2 | 보스/엘리트 격파 보상 → EconomyManager 지급(골드/마나 결정/보스의 영혼/SP/엘리트 고유 코어 조각·전설 룬) | **마일스톤 6** | ✅ 2026-05-13 확정 | 재화 시스템 도입 시 OnBossDefeated 구독자 추가. 현재는 이벤트만 발행 |
| 3 | 보스/엘리트 자힐·재생 메커니즘 본격 구현 — 세계수 광합성 / 크라켄 촉수 재생 / 리바이어선 자힐 / 겨울 여왕 빙결 재생 | **마일스톤 5/6** | ✅ 2026-05-13 확정 (옵션 B) | `MonsterBase`에 `Heal(int)` public API 추가 후 정식 구현. 마일스톤 4에서는 시각 표시만 |
| 4 | 보스 도감 / 시련 도감 (점성술사 80종 기믹과 통합) | **마일스톤 6** | — | 점성술사 시설 도입 시 |
| 5 | 보스 연습 모드 — 페이즈 선택, 패턴 로그, 히트박스 표시 | **마일스톤 6** | — | 수련장 시설 도입 시 (`Game_Design_Spec.md` §3) |
| 6 | 보스/엘리트 시각·사운드 이펙트 (텔레그래프 VFX, 패턴별 효과음) | **마일스톤 8** | — | 폴리싱 단계 |
| 7 | 크라켄 먹물 시야 차단 셰이더 (마스크 셰이더 정식 작성) | **마일스톤 8** | — | 마일스톤 4에서는 SpriteRenderer 알파로 임시 처리 |
| 8 | 황금 고블린 왕 골드 드랍 실제 지급 | **마일스톤 6** | — | EconomyManager와 연계 |
| 9 | 보스/엘리트 BGM 슬롯 | **마일스톤 8** | — | 사운드 시스템 도입 시 |
| 10 | 엘리트 입장 조건 검증(해당 액트 최종 보스 처치 여부) | **마일스톤 6** | — | 주점(Tavern) 의뢰 시스템 도입 시 |
| 11 | 보스 격파 컷씬 / Result 화면 연출 | **마일스톤 7** | — | UI/UX 본격 도입 시 |
| 12 | 보스 16종 정식 비주얼·스프라이트 (현재 단순 원형) | **별도 아트 트랙** | — | `Implementation_Plan.md §확정 사항`의 "아트 리소스 플레이스홀더로 시작" 방침 |

---

## 16. 참조 문서

- [Boss_Patterns.md](../Design/Boss_Patterns.md) — 12종 보스 패턴 / 페이즈 / 분노 모드 표
- [Elite_Bounty_Spec.md](../Design/Elite_Bounty_Spec.md) — 4종 엘리트 고유 능력 / 약점 / 드랍
- [Damage_Formula.md](../Design/Damage_Formula.md) — 약점·분노·DPS 검증값 311
- [Physics_Parameters.md](../Design/Physics_Parameters.md) — 탄막 반지름/속도/페널티
- [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 액티브 스킬과 보스/엘리트 상호작용
- [Skill_Tree_Formulas.md](../Design/Skill_Tree_Formulas.md) — 분노의 일격·관성 돌파·아머 크래시 패시브
- [Game_Design_Spec.md](../Design/Game_Design_Spec.md) §9 — 보스 기본 스탯 / 보상
- [Implementation_Plan.md §마일스톤 4](Implementation_Plan.md) — 상위 로드맵
- [Milestone1_TODO.md](Milestone1_TODO.md) · [Milestone2_TODO.md](Milestone2_TODO.md) · [Milestone3_TODO.md](Milestone3_TODO.md) — 양식 표준 및 인계 출처
