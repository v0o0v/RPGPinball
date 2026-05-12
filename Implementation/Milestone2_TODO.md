# 마일스톤 2 TODO — 전투 시스템

> **목표**: 데미지 파이프라인 · 콤보 · 마나 · 스킬 덱 · 더미 몬스터 · 스테이지 타이머 완성
> **기간**: 2~3주 (누적 5주)
> **상위 문서**: [Implementation_Plan.md §마일스톤 2](Implementation_Plan.md)
> **검증 기준**: 더미 몬스터 배치 → 공 타격 → 데미지 계산 → 콤보 → 마나 충전 → 스킬 발동 루프 동작, `Damage_Formula.md` 시뮬레이션 수치 일치
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 사용자 직접 확인 필요 또는 후속 마일스톤
>
> **갱신일**: 2026-05-12

---

## 0. 선행 조건 (마일스톤 1 산출물 재사용)

마일스톤 1에서 이미 작성·정착된 자산. 마일스톤 2에서는 **확장만** 하고 시그니처 변경은 회피.

| 자산 | 재사용 포인트 |
|---|---|
| `Core/EventBus.cs` | `OnBallHit`, `OnComboChange`, `OnManaChange`, `OnTimePenalty` 이미 선언됨. 추가 이벤트(`OnDamageDealt`, `OnMonsterKilled`, `OnSkillCast`, `OnTimerChanged`)를 이 파일에 **append** |
| `Core/Constants.cs` | 물리 상수만 존재. **§데미지/§콤보/§마나/§타이머** 섹션을 새로 추가 (`Damage_Formula.md`·`Physics_Parameters.md` 기준값) |
| `Core/GameManager.cs` | 상태 전환에 `Result` 추가 검토 (마일스톤 7 본격, 여기서는 임시 훅만) |
| `Security/SafeInt`·`SafeFloat`·`SafeLong` | HP/마나/콤보/타이머/누적 데미지 등 민감 수치에 **반드시** 사용 |
| `Physics/BallController.cs` | 충돌 검출 위치. `OnCollisionEnter2D`에서 `MonsterBase`에 데미지 위임하는 분기 추가 |

---

## 1. 데이터 정의 (ScriptableObject) — **가장 먼저**

> 코드보다 데이터 슬롯을 먼저 정의해야 인스펙터 노출/테스트가 수월. 마일스톤 3에서 60종으로 확장될 형태를 미리 잡되, 여기서는 16종 스킬 + 몇몇 더미 데이터만 채움.

- [~] enum 5종 — 단일 파일 [SkillEnums.cs](../Assets/02.Scripts/Data/SkillEnums.cs)로 통합 (`SkillCategory`, `SkillType`, `SkillShape`, `DamageType`, `KnockbackTier`)
- [x] [SkillData.cs](../Assets/02.Scripts/Data/SkillData.cs) (SO, `CreateAssetMenu("RPG Pinball/Skill")`)
- [x] [MonsterData.cs](../Assets/02.Scripts/Data/MonsterData.cs) (SO, `CreateAssetMenu("RPG Pinball/Monster")`)
- [x] [ProjectileData.cs](../Assets/02.Scripts/Data/ProjectileData.cs) (SO)
- [~] `04.Data/Skills/` — **17개** 생성 (`Active_Skill_Judgment.md`에 원소 7종이 정의되어 있어 "원소 폭주" 추가; 표 값 그대로 반영)
- [x] `04.Data/Monsters/DummyMonster.asset` (HP 100, DEF 5, MRes 5)
- [x] `04.Data/Projectiles/SmallProjectile.asset` (검증용 소형 탄막 1종)

---

## 2. 데미지 시스템

### 2.1 `02.Scripts/Combat/DamageCalculator.cs`

`Damage_Formula.md` 10단계 파이프라인을 **순서대로** 적용하는 static 유틸 또는 싱글톤.

- [~] `DamageContext` 구조체 — 현재 슬롯: PlayerLevel · DamageType · 합연산[] · 곱연산[] · IsMithrilBall · Core/Flipper/Rune/Tarot 배율 · Crit · DEF · MRes. **공 속도·콤보 수·타격 직후 플래그는 후속 마일스톤에서 추가 예정** (DamageContext 단순화)
- [x] `Calculate(in DamageContext ctx) → DamageResult` 10단계 구현 ([DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs))
- [x] 단계 3~7 패스스루 (배율 1.0 슬롯만 유지)
- [x] EditMode 단위 테스트 9건 ([DamageCalculatorTests.cs](../Assets/Tests/EditMode/DamageCalculatorTests.cs))

### 2.2 검증 시나리오 (EditMode 테스트로 자동 검증됨)

| 케이스 | 입력 | 기대 결과 | 상태 |
|---|---|---|---|
| Lv.0 베이스 | base=10, 패시브 0, DEF=0 | 10 | ✅ |
| Lv.1 베이스 (식 적용) | 10 × (1 + 0.02) | 10.2 | ✅ |
| Lv.90 기본 타격 | 10 × (1 + 90×0.02) | 28 | ✅ |
| 합연산 +25% | 28 × 1.25 | 35 | ✅ |
| DEF 10 적 | 28 - 10 | 18 | ✅ |
| 미스릴 + 마법 | 28 × 1.15 | 32.2 | ✅ |
| 곱연산 4중첩 (3번째부터 합산 전환) | 10 × 1.5 × 1.5 × (1 + 0.5 + 0.5) | 45 | ✅ |
| 최소 데미지 보장 | 임의 - DEF 999 | 1 | ✅ |

> ℹ️ Lv.1 기본 데미지는 식 `10 × (1 + 1×0.02) = 10.2`로 [Damage_Formula.md](../Design/Damage_Formula.md) 수정됨 (마일스톤 2 진행 중 정정).

---

## 3. 콤보 시스템

### 3.1 `02.Scripts/Combat/ComboSystem.cs`

- [x] 싱글톤 MonoBehaviour + `SafeInt` 백킹 ([ComboSystem.cs](../Assets/02.Scripts/Combat/ComboSystem.cs))
- [x] 몬스터/보스 +1, 벽·범퍼·기믹 카운트 X (MonsterBase에서 `RegisterHit()` 호출, 벽/범퍼는 ManaSystem만 트리거)
- [x] 3초 미타격 시 리셋 (Update에서 `Time.time - lastHitTime` 검사)
- [x] `OnBallDead` 구독 → 즉시 0
- [x] `GetManaMultiplier(combo)` static 함수 — 0/1.5/2.0 분기
- [x] `OnComboChange` 이벤트 발행
- [ ] `OnComboMilestone(10/30/50/100)` 이벤트 훅 — **미구현** (이펙트 시스템 부재로 마일스톤 7 UI/UX 단계에서 추가 예정)

---

## 4. 마나 시스템

### 4.1 `02.Scripts/Combat/ManaSystem.cs`

- [x] 싱글톤 MonoBehaviour + `SafeInt` 0~100 + `ChargeEfficiency` ([ManaSystem.cs](../Assets/02.Scripts/Combat/ManaSystem.cs))
- [x] 충전원 (벽/범퍼 +3, 몬스터 +8, 보스 +15) — `OnBallHit.TargetTag`로 분기, 콤보 배율 자동 적용
- [x] `TrySpend(int cost)` API
- [x] `OnManaChange` 이벤트 발행
- [~] **`OnWallHit`/`OnMonsterHit` 별도 이벤트 추가는 취소**. 기존 [BallController.OnCollisionEnter2D](../Assets/02.Scripts/Physics/BallController.cs)가 `OnBallHit.TargetTag`에 충돌 태그를 실어 보내므로 그것을 그대로 사용. 마일스톤 1과 호환성 100% 유지

---

## 5. 적 시스템 (더미 수준)

### 5.1 `02.Scripts/Enemy/MonsterBase.cs`

- [x] MonoBehaviour + `MonsterData` 직렬화 슬롯 ([MonsterBase.cs](../Assets/02.Scripts/Enemy/MonsterBase.cs))
- [x] `Hp`(`SafeInt`), `IsDead` 필드
- [x] `OnCollisionEnter2D` → Ball 태그 검사 → DamageCalculator → HP 감소 → 0 이하 시 `Die()`
- [x] `Die()` → `OnMonsterKilled` 이벤트 (XP/Gold/IsBoss 페이로드), ComboSystem.RegisterHit() 호출
- [x] Collision 방식 (반사 자연스러움)

### 5.2 `02.Scripts/Enemy/ProjectileBase.cs`

- [x] MonoBehaviour + `ProjectileData` 직렬화 슬롯 ([ProjectileBase.cs](../Assets/02.Scripts/Enemy/ProjectileBase.cs))
- [x] Rigidbody2D 직선 이동 (`Launch(dir)` API)
- [x] 데드존 통과 → `OnProjectilePenalty` 이벤트 발행 후 자가 파괴
- [x] 플리퍼 접촉 → `OnFlipperBlocked` 발행 후 자가 파괴
- [~] 단순 `Instantiate/Destroy` (수명 6초 자동 파괴) — 풀링은 마일스톤 4 인계
- [ ] 공 접촉 시 강제 감속/넉백 — 마일스톤 4 보스 패턴에서 구현 예정

### 5.3 더미 프리팹

- [x] [DummyMonster.prefab](../Assets/05.Prefabs/Monsters/DummyMonster.prefab) — SpriteRenderer + CircleCollider2D(r=0.45) + Rigidbody2D(Kinematic) + MonsterBase, scale 0.8
- [x] [DummyMonster.png](../Assets/03.Sprites/Proto/DummyMonster.png) — 빨간 슬라임 디자인 128×128 PPU=128 (사용자 메모리 "오브젝트에 맞는 스프라이트 생성" 반영, 단색 ProtoSprite 미사용)
- [x] Sample 씬에 더미 몬스터 3개 배치: (-2.5, 4), (0, 5), (2.5, 4)

---

## 6. 스킬 시스템

### 6.1 `02.Scripts/Combat/SkillDeck.cs`

- [x] 4개 슬롯 + 동일 스킬 중복 거부 + 궁극기 1개 제한 ([SkillDeck.cs](../Assets/02.Scripts/Combat/SkillDeck.cs))
- [x] 0.3초 캐스트 딜레이 + `IsCasting` 플래그
- [x] `Equip(slot, skill, level)`, `Use(slot, worldPos)` API
- [x] `Use` 시 마나 사전 차감 → `ActiveSkillBase.Execute()` 호출
- [ ] **인게임 입력부 미구현** — 슬롯 선택 UI 버튼, 터치 좌표 캡처가 없어 현재는 코드 호출(`SkillDeck.Instance.Use()`)로만 발동 가능. 임시 디버그 입력 또는 마일스톤 7 정식 UI에서 결정 필요
- [ ] **FlipperController 입력 충돌 차단 플래그** — 입력부 미구현으로 보류

### 6.2 `02.Scripts/Combat/ActiveSkillBase.cs`

- [x] `abstract class ActiveSkillBase : MonoBehaviour` ([ActiveSkillBase.cs](../Assets/02.Scripts/Combat/ActiveSkillBase.cs))
- [x] `data` + `level` 필드, `Initialize(skillData, level)` 진입점
- [x] `abstract UniTask Execute(Vector2 targetPos, CancellationToken ct)`
- [x] `OverlapCircleMonsters`, `OverlapBoxMonsters` 헬퍼 + `ComputeSkillDamage(monster, lv)` 데미지 계산
- [x] 마나 사전 차감 SkillDeck.Use에서만 처리

### 6.3 `02.Scripts/Combat/Skills/` (16종)

> 각 파일은 `ActiveSkillBase` 상속. **마일스톤 2에서는 가장 단순한 2~3개만 실제 구현하고 나머지는 빈 스텁(TODO 주석)으로 둬도 됨.** 그러나 파일과 SkillData 에셋은 모두 만들어둘 것 (마일스톤 3에서 채움).

**제어 6종** (control)
- [x] [MagneticField.cs](../Assets/02.Scripts/Combat/Skills/MagneticField.cs) (T2) — 우선 구현 ✅ (공 인력 지속 효과)
- [ ] [SafetyWall.cs](../Assets/02.Scripts/Combat/Skills/SafetyWall.cs) — 스텁 (마일스톤 3)
- [ ] [ReboundShield.cs](../Assets/02.Scripts/Combat/Skills/ReboundShield.cs) — 스텁
- [ ] [MassRecall.cs](../Assets/02.Scripts/Combat/Skills/MassRecall.cs) — 스텁
- [ ] [TimeDilation.cs](../Assets/02.Scripts/Combat/Skills/TimeDilation.cs) — 스텁 (Tier 6 궁극기)
- [ ] [ZoneOfControl.cs](../Assets/02.Scripts/Combat/Skills/ZoneOfControl.cs) — 스텁 (Tier 6 궁극기)

**파괴 4종** (destruction)
- [x] [GiantBall.cs](../Assets/02.Scripts/Combat/Skills/GiantBall.cs) (T4) — 우선 구현 ✅ (원형 광역 + 넉백)
- [ ] [HeavyAccelerator.cs](../Assets/02.Scripts/Combat/Skills/HeavyAccelerator.cs) — 스텁
- [ ] [MeteorStrike.cs](../Assets/02.Scripts/Combat/Skills/MeteorStrike.cs) — 스텁 (Tier 6 궁극기)
- [ ] [ZeroBlade.cs](../Assets/02.Scripts/Combat/Skills/ZeroBlade.cs) — 스텁 (Tier 6 궁극기)

**원소 7종** (element — `Active_Skill_Judgment.md` 그대로)
- [x] [FireballI.cs](../Assets/02.Scripts/Combat/Skills/FireballI.cs) (T2) — 우선 구현 ✅ (마법 데미지 / 미스릴 ×1.15 검증)
- [ ] [IceFormI.cs](../Assets/02.Scripts/Combat/Skills/IceFormI.cs) — 스텁
- [ ] [MultiBallI.cs](../Assets/02.Scripts/Combat/Skills/MultiBallI.cs) — 스텁
- [ ] [Thunderbolt.cs](../Assets/02.Scripts/Combat/Skills/Thunderbolt.cs) — 스텁
- [ ] [Vortex.cs](../Assets/02.Scripts/Combat/Skills/Vortex.cs) — 스텁
- [ ] [ElementalRampage.cs](../Assets/02.Scripts/Combat/Skills/ElementalRampage.cs) — 스텁 (Tier 6 궁극기)
- [ ] [Armageddon.cs](../Assets/02.Scripts/Combat/Skills/Armageddon.cs) — 스텁 (Tier 6 궁극기)

> ✅ 문서 정합 — 17종 SkillData SO 모두 `Active_Skill_Judgment.md` 표 그대로 마나·반경·히트·넉백 거리 반영됨. ID는 제어 10~15, 파괴 20~23, 원소 30~36.

### 6.4 `02.Scripts/Combat/KnockbackSystem.cs`

- [x] `Apply(Rigidbody2D, Vector2, force, tier, isUltimate)` static API ([KnockbackSystem.cs](../Assets/02.Scripts/Combat/KnockbackSystem.cs))
- [x] None=1.0, Resist=0.5, Immune=0/궁극기 1.0, Absolute=0 (궁극기 포함 차단)
- [x] DummyMonster는 `None` 시작
- [x] EditMode 단위 테스트 5건 ([KnockbackSystemTests.cs](../Assets/Tests/EditMode/KnockbackSystemTests.cs))

---

## 7. 스테이지 타이머

### 7.1 `02.Scripts/Combat/StageTimer.cs`

- [x] 싱글톤 MonoBehaviour + `SafeFloat` 백킹 (`remaining`, `totalRecovered`) ([StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs))
- [x] 시작값 180초 (`Constants.StageDefaultTime`)
- [x] Update에서 `Time.deltaTime` 감소, 0 도달 시 `OnGameStateChanged(Playing → Result)` 발행
- [x] `AddTime(seconds)` — 회복 상한 60초/스테이지
- [x] `Penalize(seconds)` — 절댓값 차감, 0 미만 방지
- [x] `OnProjectilePenalty`·`OnTimePenalty` 구독 → Penalize
- [~] `OnBallDead` 구독은 의도적으로 비워둠 — 낙사 페널티는 [DeadZone.cs](../Assets/02.Scripts/Physics/DeadZone.cs)가 `OnTimePenalty` 발행으로 처리. 보스전 -20초 분기는 마일스톤 4 인계
- [x] `OnTimerChanged(remaining, total)` 이벤트 매 프레임 발행

---

## 8. UI 연동 (최소)

> 본격 HUD는 마일스톤 7. 여기서는 검증용 임시 UGUI 텍스트로 충분.

- [x] [DebugHud.cs](../Assets/02.Scripts/UI/DebugHud.cs) — `OnManaChange`/`OnComboChange`/`OnTimerChanged`/`OnDamageDealt` 4종 구독
- [x] Sample 씬 `DebugCanvas` (좌상단 4줄 UGUI Text, 외곽선, 폰트 36) + `EventSystem`

---

## 9. Constants.cs 확장 항목

`Damage_Formula.md` · `Physics_Parameters.md` 기준값을 새 섹션으로 추가.

- [x] `PlayerBaseDamage = 10f`
- [x] `LevelDamageScale = 0.02f`
- [x] `CritChanceDefault = 0.05f`, `CritMultiplierDefault = 1.5f`
- [x] `MithrilMagicMultiplier = 1.15f`, `MultiplierStackLimit = 2` (3번째부터 합산 전환)
- [x] `ComboResetSeconds = 3.0f`
- [x] `ManaMax = 100`, `ManaPerWall = 3`, `ManaPerMonster = 8`, `ManaPerBoss = 15`
- [x] `ComboTier1 = 10`(`ComboMultTier1 = 1.5f`), `ComboTier2 = 30`(`ComboMultTier2 = 2.0f`)
- [x] `StageDefaultTime = 180f`, `TimeRecoverCapPerStage = 60f`
- [x] `ProjectilePenetratePenalty = -5f`, `BossDeadzonePenalty = -20f`
- [x] `SkillCastDelay = 0.3f`, `SkillDeckSize = 4`

---

## 10. 검증 체크리스트

### 10.1 단위 테스트 (EditMode) — 21/21 통과
- [x] [DamageCalculatorTests.cs](../Assets/Tests/EditMode/DamageCalculatorTests.cs) — 9건 (Lv.0/1/90, 합연산, 곱연산 3중첩 전환, 미스릴, DEF, 최소 1 보장)
- [x] [ComboSystemTests.cs](../Assets/Tests/EditMode/ComboSystemTests.cs) — 3건 (마나 배율 분기 1.0/1.5/2.0)
- [x] [KnockbackSystemTests.cs](../Assets/Tests/EditMode/KnockbackSystemTests.cs) — 5건 (등급별 + 궁극기 관통)
- [x] [SafeTypesTests.cs](../Assets/Tests/EditMode/SafeTypesTests.cs) — 4건 (Int/Float/Long 라운드트립 + 산술)
- [ ] `ManaSystemTests.cs` — **미작성**. 0~100 클램프와 TrySpend는 execute_code 시뮬로 검증
- [ ] `StageTimerTests.cs` — **미작성**. 회복 상한 60초/스테이지는 향후 추가 권장

### 10.2 통합 검증 (PlayMode 시뮬레이션은 통과 / 실 플레이 미확인)

**자동 시뮬레이션 (execute_code) — 통과**
- [x] 벽 충돌 마나 +3, 몬스터 타격 마나 +8 + 콤보 +1
- [x] 콤보 10 도달 시 마나 배율 1.5배 (3 × 1.5 = 4.5 → 4 반올림)
- [x] 낙사 → 콤보 0, 타이머 -10초 (DeadZone이 OnTimePenalty 발행)
- [x] 탄막 데드존 통과 → 타이머 -5초
- [x] 더미 몬스터 처치 → `OnMonsterKilled` 이벤트 발행, GameObject inactive
- [x] 스킬 마나 부족 거부, 동일 스킬 중복 장착 거부

**실 플레이 검증 — 사용자 확인 필요**
- [ ] Sample 씬을 실제로 Play 모드에서 플레이 → HUD 갱신 / 슬라임 타격 / 콤보·마나·DMG 표시
- [ ] 미스릴 공 재질 적용 후 마법 스킬 실 데미지 ×1.15 확인 — DamageCalculator 단위 테스트로 식 검증은 통과, 인게임 검증은 미스릴 공 머티리얼 사용 시점에 가능 (현재 기본 공은 BallWood)
- [ ] 스킬 인게임 발동 — **입력부 미구현**으로 현재 코드 호출(`SkillDeck.Instance.Use()`)로만 발동 가능

### 10.3 문서 정합성
- [x] [Damage_Formula.md](../Design/Damage_Formula.md) — Lv.0/1/90 식 결과 일치, 마일스톤 2 중 표기 정정됨
- [x] [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 17종 SkillData SO 표 그대로 반영
- [x] [Physics_Parameters.md](../Design/Physics_Parameters.md) — 페널티 -10/-5/-20 모두 Constants에 정의됨

---

## 11. 후속 마일스톤 인계 사항

| 항목 | 인계 |
|---|---|
| 마일스톤 3 | DamageCalculator 단계 [2]·[3]·[7]에 스킬 트리 패시브·코어·타로 실제 결과 주입. SkillData를 60종으로 확장 |
| 마일스톤 4 | ProjectileBase를 풀링 기반으로 전환. MonsterBase → BossAI로 상속 확장 |
| 마일스톤 6 | 룬(단계 [6]) 실제 결과 주입. ManaSystem `ChargeEfficiency`를 강화 데이터에서 갱신 |
| 마일스톤 7 | DebugHud → 정식 InGameHUD 교체 |

---

## 참조 문서

- [Damage_Formula.md](../Design/Damage_Formula.md) — 10단계 파이프라인
- [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 16종 액티브 스킬 판정 표
- [Skill_Tree_Formulas.md](../Design/Skill_Tree_Formulas.md) — 스킬 공식, 곱연산 중첩 제한
- [Skill_Tree_Diagram.md](../Design/Skill_Tree_Diagram.md) — 60종 스킬 트리 구조
- [Physics_Parameters.md](../Design/Physics_Parameters.md) — 페널티·시간 상수
- [Data_Schema.md](../Design/Data_Schema.md) — SO 직렬화 구조 참고
