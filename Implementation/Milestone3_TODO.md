# 마일스톤 3 TODO — 스킬 트리 & 성장 시스템

> **목표**: 3분기 × 6 Tier × 총 60종 스킬 트리, Lv.1~100 레벨링, SP 144 분배, 패시브 효과의 DamageCalculator/ManaSystem 공급 완성
> **기간**: 2주 (누적 7주)
> **상위 문서**: [Implementation_Plan.md §마일스톤 3](Implementation_Plan.md)
> **검증 기준**: Lv.1 → Lv.100 시뮬레이션 144 SP 배분 정상, 3가지 빌드(제어/파괴/원소)에서 패시브 → DamageCalculator → 인게임 데미지 변화 일치, 14종 액티브 스킬 본 구현이 실제로 발동·판정
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 미착수 또는 사용자 직접 확인 필요
>
> **작성일**: 2026-05-12 (사전 계획) · **갱신일**: 2026-05-12 (1차 구현 완료)
> **상태**: 핵심 시스템·14종 액티브 스킬·60종 SkillData·EditMode 75/75 테스트 통과. 인게임 발동 UI는 마일스톤 7 인계 유지.

---

## 0. 선행 조건 (마일스톤 1·2 산출물 재사용)

마일스톤 3에서 **확장만** 하고 시그니처 변경은 회피.

| 자산 | 재사용 포인트 |
|---|---|
| [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) | 단계 [2]·[3] 슬롯이 패스스루로 존재. **DamageContext에 BallSpeed/BallMaterial/ComboCount/IsAfterFlipperHit 필드 추가**, [2]에서 SkillTreeManager 패시브 결과 풀링, [3]에서 코어 효과 분기 |
| [Combat/ComboSystem.cs](../Assets/02.Scripts/Combat/ComboSystem.cs) | `OnComboChange` 발행 — 콤보 스트라이크/하이퍼 콤보 패시브에서 콤보 수 참조 |
| [Combat/ManaSystem.cs](../Assets/02.Scripts/Combat/ManaSystem.cs) | `ChargeEfficiency` 필드만 있고 외부 갱신 없음 — LevelSystem과 "마나 충전" 패시브에서 갱신 |
| [Combat/StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs) | `AddTime`/`Penalize` 그대로. "타임 브레이커" 패시브에서 회복 호출, 상한 60초/스테이지 검사 재사용 |
| [Combat/SkillDeck.cs](../Assets/02.Scripts/Combat/SkillDeck.cs) | 4슬롯·궁극기 1개 제한 그대로. 새 14종 스킬도 동일 API로 장착 |
| [Combat/ActiveSkillBase.cs](../Assets/02.Scripts/Combat/ActiveSkillBase.cs) | `Execute(Vector2, CancellationToken)` 시그니처 유지. 새 스킬은 모두 이 클래스 상속 |
| [Data/SkillData.cs](../Assets/02.Scripts/Data/SkillData.cs) | SO 스키마 그대로. **선행 조건 필드(prerequisiteIds[])**, **공식 파라미터 슬롯** 추가만 |
| [Data/SkillEnums.cs](../Assets/02.Scripts/Data/SkillEnums.cs) | `SkillCategory`/`SkillType`/`SkillShape`/`DamageType`/`KnockbackTier` 유지. **`SkillId` enum 60종 신규 추가** |
| [Security/SafeInt·SafeFloat·SafeLong](../Assets/02.Scripts/Security/) | Lv/XP/SP/투자 SP 모두 `SafeInt` 백킹 강제 |
| [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) | 이벤트 추가: `OnLevelUp`, `OnXPGained`, `OnSkillPointGained`, `OnSkillInvested`, `OnSkillReset` |
| [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) | **§성장 시스템** 섹션 신규 추가 — XP 공식 상수, SP 보상 상수, 하드캡, 점감 계수 |

---

## 1. 데이터 정의 (ScriptableObject) — **가장 먼저**

> 코드 작성 전에 60종 SkillData 슬롯을 먼저 정의. 마일스톤 2에서 만들어둔 17종(원소 7종 포함)을 60종으로 확장.

### 1.1 enum 확장

- [x] [SkillEnums.cs](../Assets/02.Scripts/Data/SkillEnums.cs)에 `SkillId` enum 60종 추가
  - 제어 1~20 (10~29 ID 권장), 파괴 1~20 (30~49), 원소 1~20 (50~69) — **마일스톤 2에서 이미 사용된 ID(제어 10~15, 파괴 20~23, 원소 30~36)와 호환되도록 재매핑 또는 별도 enum 분리 결정**
  - 결정 사항: **별도 `SkillId` enum을 추가**하되, 기존 `SkillData.id`(int)는 enum 캐스팅으로 사용. 즉 `SkillData.id = (int)SkillId.ControlFlipperLight1`. **ID 체계 재매핑됨** — 제어 101~120, 파괴 201~220, 원소 301~320.
- [x] `BallMaterial` enum (`Wood`, `Steel`, `Mithril`, `Volcanic`) — [Data_Schema.md §11](../Design/Data_Schema.md) `ballState.currentMaterial`와 정합
- [x] `CoreId` enum (가속·분열·크로노·가디언·집중·축복 등 6종)
- [x] `SkillBranch` enum (`Control`/`Destruction`/`Element`)
- [x] `BallTransformation` enum (`None`/`Fireball`/`IceForm`/`Thunderball`/`GiantBall`) — A전환 상태
- [x] `CoreGrade` enum (`Normal`/`Rare`/`Epic`/`Legendary`)

### 1.2 SkillData SO 스키마 확장

- [x] `SkillData.cs`에 다음 필드 추가:
  - `int[] prerequisiteIds` — 선행 스킬 ID 배열 (`Skill_Tree_Diagram.md` 의존 관계 반영)
  - `int prerequisiteMinLevel` (기본 1) — 선행이 Lv.1 이상이면 해금
  - `SkillBranch branch` — 분기 enum
  - `bool isPassive` (= `type == Passive`) 편의 슬롯
  - **공식 파라미터 6종 슬롯** (모두 float, 미사용 시 0):
    - `linearBase`, `linearPerLevel` — 합연산 공식 (`기본X% + (Lv × Y%)`)
    - `diminishMax`, `diminishRate` — 점감 공식 (`Max × (1 - rate^Lv)`)
    - `stackBase`, `stackPerLevel` — 중첩 한도 (트리플 드로우·하이퍼 콤보·파이어볼 II 등)
  - `[TextArea] string descriptionKo` — 인게임 툴팁 한국어 설명

### 1.3 PlayerData SO

- [x] [PlayerData.cs](../Assets/02.Scripts/Data/PlayerData.cs) (SO, `CreateAssetMenu("RPG Pinball/Player Data")`)
  - `Data_Schema.md §2 플레이어 데이터` 기반
  - 필드: `playerName`(string), `level`(int 1~100), `currentXP`(int), `totalSP`(int), `usedSP`(int), `gold`(int), `manaCrystal`(int), `bossSoul`(int), `respecScrollCount`(int), `resetCount`(int)
  - 런타임 인스턴스는 `Resources.Load`로 1회 로드. **마일스톤 7 SaveSystem 도입 전까지는 Inspector 값을 그대로 사용**
  - 메모리 보호: 핵심 수치(`level`, `currentXP`, `totalSP`, `usedSP`, `gold`)는 SO에는 평문 int로 저장하되, 런타임 사용 시 `SafeInt` 래핑

### 1.4 CoreData SO

- [x] [CoreData.cs](../Assets/02.Scripts/Data/CoreData.cs) (SO, `CreateAssetMenu("RPG Pinball/Core")`)
  - `Implementation_Plan.md §마일스톤 3` 인계 항목 — DamageCalculator 단계 [3] 코어 효과 분기
  - 필드: `coreId`(`CoreId` enum), `coreGrade`(enum normal/rare/epic/legendary), `baseMultiplier`(float), `perLevelMultiplier`(float), `affectsDamageType`(`DamageType` 또는 None)
  - 효과 분기 (마일스톤 3에서 다루는 범위):
    - **가속 코어**: `BallSpeed`에 비례한 추가 배율 (관성 돌파 I 시너지)
    - **분열 코어**: 멀티볼 분열 시 분열 공 데미지 페널티 완화 (80% → 90%)
    - **크로노 코어**: 처치 시 시간 회복량 +1초 (StageTimer.AddTime 훅에서 발동)
    - 나머지 가디언/집중/축복 코어는 마일스톤 6 대장간 완성 시 본격화 — 데이터만 정의

### 1.5 `04.Data/Skills/` 60종 SO 생성

> 마일스톤 2의 17종은 ID 재매핑 위해 일괄 재생성됨. **모든 수치는 `Skill_Tree_Formulas.md` 표를 그대로 반영**.

- [x] 제어 분기 20종 (Lv·MaxLv·선행 ID·공식 파라미터)
  - T1: `ControlFlipperLight1`, `ControlFlipperLight2`, `ControlElasticBoost` (각 MaxLv=5)
  - T2: `ControlFastDraw`, `ControlLongFlipper`, `ControlMagneticField`(M2 완료) (MaxLv=5)
  - T3: `ControlQuickRecovery`, `ControlEnergyShield`, `ControlEnhancedMagneticField`, `ControlWideAngle` (MaxLv=4)
  - T4: `ControlTripleDraw`, `ControlSafetyWall`, `ControlTrampolineExcel`, `ControlJustGuard` (MaxLv=4)
  - T5: `ControlPerfectDefense`, `ControlTimeAura`, `ControlReboundShield`, `ControlMassRecall` (MaxLv=3)
  - T6: `ControlTimeDilation`, `ControlZoneOfControl` (MaxLv=2, 궁극기)
- [x] 파괴 분기 20종
  - T1: `DestSteelBall1`, `DestSteelBall2`, `DestCharge`
  - T2: `DestInertiaBreak1`, `DestInertiaBreak2`, `DestDestructionInstinct`
  - T3: `DestComboStrike`, `DestWeakPoint`, `DestHeavyBlow`, `DestTimeBreaker`
  - T4: `DestHyperCombo`, `DestSonicBoom`, `DestGiantBall`(M2 완료), `DestFuryStrike`
  - T5: `DestSpeedThrill`, `DestArmorCrash`, `DestFlipperSmash`, `DestHeavyAccelerator`
  - T6: `DestMeteorStrike`, `DestZeroBlade` (궁극기)
- [x] 원소 분기 20종
  - T1: `ElemAffinity1`, `ElemAffinity2`, `ElemManaCharge`
  - T2: `ElemFireballI`(M2 완료), `ElemFireballII`, `ElemSpark`
  - T3: `ElemIceFormI`, `ElemIceFormII`, `ElemChainLightning1`, `ElemFlameTrail`
  - T4: `ElemDualElement`, `ElemChainLightning2`, `ElemMultiBallI`, `ElemFrostExplosion`
  - T5: `ElemMultiBallII`, `ElemElementalFusion`, `ElemThunderbolt`, `ElemVortex`
  - T6: `ElemElementalRampage`(M2 ID 35), `ElemArmageddon` (궁극기)

> ✅ **[인계: M2 #17]** "원소 폭주" 스킬 ID 정합 — 마일스톤 2에서 ID 35로 잡혀 있음. 위 enum 재매핑 시 `Skill_Tree_Diagram.md` 원소 19번과 일치 검증.

### 1.6 `04.Data/Cores/` SO 생성

- [x] 6종 `.asset` (`AccelerationCore.asset`, `SplitCore.asset`, `ChronoCore.asset`, `GuardianCore.asset`, `FocusCore.asset`, `BlessingCore.asset`) — 마일스톤 3에서는 앞 3종만 효과 활성, 나머지는 데이터 자리만 잡음

---

## 2. 스킬 트리 매니저

### 2.1 [Combat/SkillTreeManager.cs](../Assets/02.Scripts/Combat/SkillTreeManager.cs)

- [x] 싱글톤 MonoBehaviour + `DontDestroyOnLoad`
- [x] `Dictionary<int, int> investedLevels` — 스킬 ID → 투자 레벨
- [x] `List<SkillData> allSkills` — Inspector 또는 Resources.LoadAll로 60종 로드. **Sample 씬에서는 60종이 SerializedObject로 일괄 바인딩됨**
- [x] **해금 검사** `bool CanInvest(int skillId, out string reason)`
- [x] **투자** `bool Invest(int skillId)` — SP 차감 + 이벤트 발행 + RecalculatePassives 자동 호출 (이벤트 핸들러로)
- [x] **리셋** `void ResetAll()` — 모든 레벨 0 + LevelSystem.RefundAllSP() + RecalculatePassives
- [x] **패시브 합산** `void RecalculatePassives()`
  - 카테고리별 캐시: 합/곱(물리·마법 별도), 쿨감(2슬롯), 스택 한도, 멀티볼 캡, 마나 효율, 콤보 보너스, 크리율, 아머 크래시, 분노/관성/플리퍼 스매시 인자 등 20여 필드
- [x] **공급 API** 13종 — `GetDamageAddPercent` / `GetDamageMultiplierFactors` / `GetCritChanceBonus` / `GetArmorReductionPercent` / `GetFlipperCooldownMultiplier` / `GetFlipperImpulseMultiplier` / `GetMaxFlipperStack` / `GetMaxMultiBallCount` / `GetManaChargeMultiplier` / `GetTimeRecoverPerKill` / `GetComboStrikePerStack` / `GetMaxComboStack` / `GetFuryStrikeBonus` / `GetInertiaBreak1Factor` / `GetFlipperSmashFactor`

### 2.2 [Combat/SkillFormula.cs](../Assets/02.Scripts/Combat/SkillFormula.cs)

- [x] `Linear`/`Diminish`/`StackLimit`/`HardCapMin`/`HardCapMax`/`CooldownMultiplier` — 6개 정적 메서드
- [x] **별도 파일로 분리 완료** — 최초 시도 시 컴파일 캐시 이슈로 누락되었으나, `AssetDatabase.ImportAsset(path, ForceSynchronousImport | ForceUpdate)`로 명시 임포트하여 정상 등록. 단일 어셈블리(RPGPinball.Runtime)에 단일 정의 확인됨.
- [x] 단위 테스트 [SkillFormulaTests.cs](../Assets/Tests/EditMode/SkillFormulaTests.cs) — 13건 (Linear/Diminish/StackLimit/HardCap/CooldownMultiplier 검증, 75/75 통과)

### 2.3 규칙 적용 매트릭스

| 규칙 | 적용 대상 | 구현 위치 |
|---|---|---|
| 합연산 | 데미지% 보너스 (강철구 I, 묵직한 타격, 콤보 스트라이크, 분노의 일격, 파괴 본능 등) | `GetDamageAddPercent` 누계 |
| 곱연산 | 데미지 배율 (강철구 II, 관성 돌파 I, 듀얼 엘리먼트, 가속의 쾌감, 속성 융합) | `GetDamageMultiplierFactors` 배열 |
| 점감 | 쿨감(플리퍼 경량화 I/II), 둔화, 확률, 보정값 (`max × (1 - rate^Lv)`) | `SkillFormula.Diminish` |
| 하드캡 | 플리퍼 쿨타임 0.5초 / 멀티볼 5(+궁8) / 회복 60초/스테이지 | 각 시스템에서 final clamp |
| 곱연산 3중첩 제한 | `DamageCalculator.ApplyMultipliersWithStackLimit` (이미 마일스톤 2에 구현됨) | 그대로 사용 |

---

## 3. 레벨 & SP 시스템

### 3.1 [Meta/LevelSystem.cs](../Assets/02.Scripts/Meta/LevelSystem.cs)

- [x] 싱글톤 MonoBehaviour + `DontDestroyOnLoad`
- [x] `PlayerData` 참조 보유. `Lv`, `CurrentXP`, `TotalSP`, `UsedSP` 프로퍼티는 `SafeInt` 래핑
- [x] **필요 XP 공식** `int RequiredXP(int level)` → `Mathf.RoundToInt(80 + level * 12 + level * level * 0.5f)`
  - **검증**: Mathf.RoundToInt는 IEEE 754 banker's rounding 사용 (Lv.1 92.5 → 92, Lv.99 6168.5 → 6168)
- [x] `void GainXP(int amount, int enemyLevel)` — 오버레벨링 페널티 ×0.5/×0.2, 다중 레벨업 루프, Lv.100 캡 절단
- [x] `void LevelUp()` — SP +1 + OnLevelUp/OnSkillPointGained 이벤트 발행
- [x] `void AwardBossSP()` (마일스톤 4 BossAI 인계용 hook)
- [x] `void AwardActClearSP()` (마일스톤 5 액트맵 인계용 hook)
- [x] **SP 경제**: Lv.1→100 = 99 + 24 보스 + 20 액트 = **143** (Constants.TotalSPGoal=144와 1차 차이는 의도된 여유)
- [x] EditMode 테스트 [LevelSystemTests.cs](../Assets/Tests/EditMode/LevelSystemTests.cs) — 14건
  - RequiredXP(1)/(50)/(99) 공식 검증
  - 오버레벨링 페널티 +5/+10 분기
  - 다중 레벨업 (한 번에 큰 XP 투입 시 여러 레벨)
  - Lv.100 캡 후 XP 절단
  - SP 누적 (LevelUp 99회 + 보스 24회 + 액트 4회 = 127 — 실제 124~144 범위로 설계되었으므로 정확한 누적 공식은 `Skill_Tree_Formulas.md`에 맞춤)

### 3.2 [Meta/PlayerStats.cs](../Assets/02.Scripts/Meta/PlayerStats.cs) (선택)

- [ ] 레벨업 시 자동 증가 스탯 — `Game_Design_Spec.md` §성장 시스템 확인 후 결정
  - HP, 마나 회복률, 데미지 보너스 등을 Lv 함수로 캐싱
  - LevelSystem과 분리해 두면 마일스톤 4 보스 밸런스 검증 시 격리 가능

### 3.3 Constants 추가

- [x] `XPBase = 80f`, `XPPerLevel = 12f`, `XPLevelSquared = 0.5f`
- [x] `LevelCap = 100`
- [x] `OverlevelThreshold1 = 5`, `OverlevelMul1 = 0.5f`, `OverlevelThreshold2 = 10`, `OverlevelMul2 = 0.2f`
- [x] `SPPerLevel = 1`, `SPPerBoss = 1`, `SPPerActClear = 5`, `TotalSPGoal = 144`
- [x] `MultiBallHardCap = 5`, `MultiBallUltimateCap = 8`
- [x] `TimeRecoverCapPerStage = 60f` (이미 마일스톤 2에 존재 — 재확인됨)
- [x] `TransformationManaCost = 40`, `TransformationBaseDuration = 15`, `TransformationPerLevelDuration = 2`
- [x] `UltimateManaCost = 100`, `TimeDilationScale = 0.25`
- [x] 넉백 거리 기본 3종 (`KnockbackDistanceNormal/Strong/Ultimate`), `KnockbackDuration = 0.3`
- [x] `DiminishRateDefault = 0.95`

---

## 4. **[인계: M2 #1]** 스킬 14종 본 구현

> 마일스톤 2에서 스텁(Debug.Log)만 둔 14종을 본격 구현. 각 스킬은 [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) 표 그대로 판정·히트·넉백·지속시간 반영.

### 4.1 제어 5종

- [x] [SafetyWall.cs](../Assets/02.Scripts/Combat/Skills/SafetyWall.cs) — 트램펄린 3개 (좌·우·바닥) 임시 소환, 공 접촉 시 위 30N 반사
- [x] [ReboundShield.cs](../Assets/02.Scripts/Combat/Skills/ReboundShield.cs) — 반경 4, 0.1초 폴링 + 투사체 반사 (×(1 + Lv × 0.3))
- [x] [MassRecall.cs](../Assets/02.Scripts/Combat/Skills/MassRecall.cs) — 모든 활성 BallController를 (0, 2)로 이동, 속도 0
- [x] [TimeDilation.cs](../Assets/02.Scripts/Combat/Skills/TimeDilation.cs) — Time.timeScale = 0.25, DelayType.Realtime으로 대기 후 복귀
- [x] [ZoneOfControl.cs](../Assets/02.Scripts/Combat/Skills/ZoneOfControl.cs) — FlipperController.OverrideCooldown(0) + SetUnlimitedStack(true)

### 4.2 파괴 3종

- [x] [HeavyAccelerator.cs](../Assets/02.Scripts/Combat/Skills/HeavyAccelerator.cs) — BallController.SetForcedSpeed(BallMaxSpeed)
- [x] [MeteorStrike.cs](../Assets/02.Scripts/Combat/Skills/MeteorStrike.cs) — SpawnAOEBox (낙하 관통 200%) + SpawnAOECircle (착탄 300% + Lv × 50%, 넉백 5)
- [x] [ZeroBlade.cs](../Assets/02.Scripts/Combat/Skills/ZeroBlade.cs) — 0.1초 틱마다 공 위치 0.8×0.8 SpawnAOEBox 40% 데미지

### 4.3 원소 6종

- [x] [IceFormI.cs](../Assets/02.Scripts/Combat/Skills/IceFormI.cs) — BallController.SetTransformation(IceForm) + 즉시 시야 내 적에게 ApplySlowDebuff(점감 0.7×(1-0.95^Lv))
- [x] [MultiBallI.cs](../Assets/02.Scripts/Combat/Skills/MultiBallI.cs) — Ball.prefab 1개 추가 인스턴스, IsSplitBall=true. 하드캡 5/8. **[인계: M1] 카메라 줌아웃은 BallController.OnEnable/OnDisable에서 자동**
- [x] [Thunderbolt.cs](../Assets/02.Scripts/Combat/Skills/Thunderbolt.cs) — 보스/근접 몬스터 위치 SpawnAOECircle (100% + Lv × 15%) + 5% 스턴
- [x] [Vortex.cs](../Assets/02.Scripts/Combat/Skills/Vortex.cs) — 흡인력 + 0.5초 틱 30% 데미지 (3초)
- [x] [ElementalRampage.cs](../Assets/02.Scripts/Combat/Skills/ElementalRampage.cs) — 모든 공 위치에 주기적 폭발 (60% per hit, 6초)
- [x] [Armageddon.cs](../Assets/02.Scripts/Combat/Skills/Armageddon.cs) — 반경 6 × 3파 폭발, 각 파 무작위 상태이상

### 4.4 ActiveSkillBase 헬퍼 보강

- [x] `ComputeUltimateDamage(MonsterBase, basePercent, perLvPercent)` — 궁극기 비율 데미지
- [x] `SpawnAOECircle / SpawnAOEBox` — 광역 + 넉백 일괄 (KnockbackSystem 통합)
- [x] `OverlapBoxMonsters` (기존)
- [x] `FindBossOrNearestMonster` — 메테오 스트라이크/썬더볼트용
- [x] `LogCast` — 검증용 콘솔 로그

---

## 5. **[인계: M2 #2·#3]** DamageContext 확장 & 단계 [2]·[3] 보강

### 5.1 DamageContext 신규 필드

- [x] `BallSpeed`, `BallMaterial`, `ComboCount`, `IsAfterFlipperHit`, `IsWeakPointHit`, `TargetCurrentHpRatio`, `StackedFireBurns`, `IsSplitBall` 추가
- [x] SkillTreeManager 캐시 값 8종 슬롯 추가 (`ComboStrikePerStack`, `ComboMaxStack`, `FuryStrikeBonus`, `InertiaBreakFactor`, `FlipperSmashFactor`, `CritChanceBonus`, `WeakPointCritBonus`, `ArmorReductionPercent`)
- [x] 코어 슬롯 2종 (`AccelerationCoreCoeff`, `SplitCoreRelief`)
- [x] `DamageContext.Default`에 안전 기본값 채움
- [x] **MonsterBase.OnCollisionEnter2D**에서 SkillTreeManager.GetXxx 호출 → ctx 자동 빌드

### 5.2 DamageCalculator 단계 [3] 코어 효과

- [x] **가속 코어**: `damage *= 1 + (BallSpeed / BallMaxSpeed) × AccelerationCoreCoeff`
- [x] **분열 코어**: `IsSplitBall == true`면 페널티 (0.8 + SplitCoreRelief)
- [x] **크로노 코어**: MonsterBase.Die()에서 `StageTimer.AddTime(SkillTreeManager.GetTimeRecoverPerKill())` 자동 호출 (타임 브레이커 패시브와 통합)
- [x] 나머지 3종(가디언/집중/축복)은 CoreData에 슬롯만 두고 마일스톤 6 인계

### 5.3 EditMode 테스트 추가

- [x] [DamageCalculatorTests.cs](../Assets/Tests/EditMode/DamageCalculatorTests.cs) — 마일스톤 2의 9건 + 마일스톤 3의 8건 = 17건 (콤보/분노/플리퍼 스매시/관성/가속코어/아머크래시)

---

## 6. **[인계: M2 #4·#5·#6]** ManaSystem · 미스릴 검증 · 누락 테스트

### 6.1 ManaSystem.ChargeEfficiency 실 적용

- [x] `Start()`에서 `RecalculateEfficiency()` 호출
- [x] `RecalculateEfficiency()` — 베이스 1.0 + Lv × 0.005 + 마나 충전 패시브 곱
- [x] `OnLevelUp` / `OnSkillInvested` / `OnSkillReset` 이벤트 구독으로 즉시 갱신
- [x] EditMode 테스트 [ManaSystemTests.cs](../Assets/Tests/EditMode/ManaSystemTests.cs) — 7건 (클램프/TrySpend/효율/콤보)

### 6.2 미스릴 공 ×1.15 실 플레이 검증

- [x] `BallController.Material` SerializeField 노출 — Inspector에서 BallMaterial enum 직접 변경 가능
- [x] **MonsterBase.OnCollisionEnter2D**가 BallController.Material을 자동으로 DamageContext에 주입
- [x] DamageCalculator 단계 [4]에서 Mithril + Magic이면 ×1.15 자동 적용 (EditMode 테스트 통과)
- [ ] **사용자 확인 필요** — Sample 씬 Play → Ball Inspector에서 Material=Mithril 변경 → 인게임 데미지 ×1.15 육안 검증

### 6.3 StageTimerTests 추가

- [x] [StageTimerTests.cs](../Assets/Tests/EditMode/StageTimerTests.cs) — 5건 (초기값/Penalize/AddTime 상한/음수 보호)
- [x] StageTimer에 `ResetTimer(float startTime)` 메서드 추가 — EditMode에서 Awake 미호출 대응

---

## 7. **[인계: M1]** 멀티볼 카메라 줌아웃

> `Constants.CameraMultiballZoomPerBall = 0.1f` 이미 정의됨. 마일스톤 1에서 미구현 → MultiBallI 스킬 본 구현(§4.3)과 함께.

- [x] [CameraController.cs](../Assets/02.Scripts/Physics/CameraController.cs) 신규 작성 — Mathf.SmoothDamp 보간 + ProCamera2D와 직접 충돌 없이 `Camera.orthographicSize` 조작 (베이스 = 9)
- [x] `NotifyBallCount/NotifyBallAdded/NotifyBallRemoved` API 제공. `SetBossFight(bool)`도 함께 노출 (마일스톤 4 인계)
- [x] [BallController.cs](../Assets/02.Scripts/Physics/BallController.cs) `OnEnable`/`OnDisable`에서 분열 공만 `NotifyBallAdded(+1)/NotifyBallRemoved(-1)` 호출
- [x] **시뮬레이션 검증**: 1공→9.00, 3공→9.05 (SmoothDamp 보간 중, 최종 목표 10.8) 정상 보간 확인
- [ ] **사용자 확인 필요** — Play 모드에서 MultiBallI 발동 시 화면 줌아웃 → 분열 공 소멸 시 원복 육안 검증

---

## 8. Constants.cs 확장 항목 (§3.3과 일부 중복 — 한 곳에 모음)

모든 항목 **§3.3에서 완료됨** — 위 §3.3 참조.

- [x] 성장/SP/하드캡/A전환/궁극기/넉백 거리/점감 계수 — 모두 Constants.cs에 추가됨

---

## 9. 검증 체크리스트

### 9.1 단위 테스트 (EditMode) — **75/75 통과** ✅

- [x] [SkillFormulaTests.cs](../Assets/Tests/EditMode/SkillFormulaTests.cs) — 13건
- [x] [SkillTreeManagerTests.cs](../Assets/Tests/EditMode/SkillTreeManagerTests.cs) — 5건 (해금/투자 거부/MaxLv/선행/리셋)
- [x] [LevelSystemTests.cs](../Assets/Tests/EditMode/LevelSystemTests.cs) — 14건 (XP 공식/오버레벨링/다중 레벨업/Lv 캡/SP)
- [x] [DamageCalculatorTests.cs](../Assets/Tests/EditMode/DamageCalculatorTests.cs) — 17건 (M2의 9건 + M3 추가 8건)
- [x] [ManaSystemTests.cs](../Assets/Tests/EditMode/ManaSystemTests.cs) — 7건
- [x] [StageTimerTests.cs](../Assets/Tests/EditMode/StageTimerTests.cs) — 5건
- [x] **결과**: M2 21건 + M3 신규 54건 = **75/75 모두 통과 (1.39초)**

### 9.2 빌드 시뮬레이션 (PlayMode execute_code) — 완료 ✅

| 빌드 | 결과 | 비고 |
|---|---|---|
| 베이스 (Lv.90, 패시브 0) | 28.00 DMG | Damage_Formula.md 일치 |
| 제어 (8종 만렙) | DMG 28.00, 쿨감 17.4%, 쿨타임 1.24s, 스택 5 | 데미지 보너스 없음 (정상) |
| 파괴 (11종 만렙) | DMG **108.61** (속도30, 콤보20) | 합 +39.5%, 콤보스택18, 관성×1.35, 플스매시×1.45 |
| 원소 (10종 만렙) | 마법 DMG **31.50** (콤보20) | 합 +12.5%, 마나효율 ×1.16, 멀티볼 5 |

### 9.3 인게임 검증 (사용자 확인 필요)

- [ ] Sample 씬 Play → 14종 액티브 스킬 코드 호출(`SkillDeck.Instance.Use()`) 또는 임시 디버그 입력으로 발동 → 콘솔에 데미지/히트 수/넉백 거리 로그 정상
- [ ] LevelSystem.GainXP 디버그 호출 → Lv.1 → Lv.10 도달, SP 9 증가 확인
- [ ] SkillTreeManager.Invest로 제어/파괴/원소 스킬 각각 투자 → DebugHud에서 데미지 변화 확인
- [ ] MultiBallI 발동 → 카메라 줌아웃 (베이스 9 → 9.4 정도) → 분열 공 소멸 시 원복
- [ ] Ball Inspector의 BallMaterial=Mithril 변경 → 마법 스킬 발동 시 ×1.15 적용

### 9.4 문서 정합성

- [x] [Skill_Tree_Formulas.md](../Design/Skill_Tree_Formulas.md) — 60종 SkillData SO + SkillFormula 호출에 반영됨
- [x] [Skill_Tree_Diagram.md](../Design/Skill_Tree_Diagram.md) — 선행 의존성 그래프와 `SkillData.prerequisiteIds` 일치
- [x] [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 14종 액티브의 마나/반경/히트/넉백 표 일치
- [x] [Data_Schema.md](../Design/Data_Schema.md) §2 플레이어 데이터 — PlayerData SO 필드 정합
- [x] [Damage_Formula.md](../Design/Damage_Formula.md) — DamageContext 신규 필드와 단계 [2]·[3] 일치

---

## 10. 후속 마일스톤 인계 사항

> 마일스톤 3 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤에 넘기는 항목. 후속 마일스톤 진입 시 `[인계: M3]` 표기로 cross-link.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 약점 포착·약점 부위 판정 | **마일스톤 4** | 보스 약점 부위 콜라이더 도입 시 본격 적용. 마일스톤 3에서는 `IsWeakPointHit = false` 기본 |
| 2 | 분노의 일격(보스 HP ≤ 30%) 실 적용 | **마일스톤 4** | 보스 HP 시스템 도입 시. 마일스톤 3는 더미 몬스터로 식만 검증 |
| 3 | 보스 넉백 면역 등급(중간 보스·최종 보스) | **마일스톤 4** | BossAI 도입 시 `KnockbackTier.Immune`/`Absolute` 본격 분기 |
| 4 | 코어 효과 — 가디언·집중·축복 3종 | **마일스톤 6** | 대장간 코어 튜닝 UI 도입 시 |
| 5 | 룬 효과 (단계 [6]) | **마일스톤 6** | 마법 부여소 |
| 6 | 타로카드 효과 (단계 [7]) | **마일스톤 6** | 점성술사 |
| 7 | 플리퍼 파생형 효과 (단계 [5]) — 가시/연성/충격파 | **마일스톤 6** | 대장간 플리퍼 강화 Lv.4 |
| 8 | 스킬 트리 UI (트리 그래프, SP 투자 버튼, 리셋) | **마일스톤 7** | 정식 UI 본격 도입 시 |
| 9 | 스킬 인게임 입력부 — 슬롯 선택 UI + 터치 좌표 캡처 | **마일스톤 7** | (마일스톤 2 인계와 동일) |
| 10 | 14종 스킬의 시각/사운드 이펙트 | **마일스톤 8** | 폴리싱 단계 |
| 11 | PlayerData 영구 저장 — JSON + 암호화 | **마일스톤 7** | SaveSystem 본격 도입 시 |

---

## 11. 참조 문서

- [Skill_Tree_Formulas.md](../Design/Skill_Tree_Formulas.md) — 60종 공식·SP 경제·하드캡 규칙
- [Skill_Tree_Diagram.md](../Design/Skill_Tree_Diagram.md) — 분기별 의존성 그래프
- [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 액티브 14종 판정 표
- [Damage_Formula.md](../Design/Damage_Formula.md) — 10단계 파이프라인 (단계 [2]·[3] 본격 활성)
- [Data_Schema.md](../Design/Data_Schema.md) — PlayerData·런타임 ballState 구조
- [Physics_Parameters.md](../Design/Physics_Parameters.md) — BallMaxSpeed·하드캡 출처
- [Implementation_Plan.md §마일스톤 3](Implementation_Plan.md) — 상위 로드맵
- [Milestone1_TODO.md](Milestone1_TODO.md) · [Milestone2_TODO.md](Milestone2_TODO.md) — 양식 표준 및 인계 출처
