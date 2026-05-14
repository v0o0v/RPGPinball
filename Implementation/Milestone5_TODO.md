# 마일스톤 5 TODO — 절차적 스테이지 생성 & 기믹

> **목표**: `Procedural_Stage_Gen.md` 10단계 생성 플로우를 따라 **시드 기반 절차 스테이지 생성기** 완성. 상단/중단/하단 **세그먼트 풀**(테마 4종 × 8/15/1) 조합, **난이도 예산** 공식(`BaseBudget × ActMultiplier ± 10%`) 구현, **80종 기믹**(공통 20 + 봄/여름/가을/겨울 각 15) ScriptableObject + 런타임 분기 작성, **몬스터 웨이브 3패턴**(물량 러시 / 정예 소수 / 보스 호위) + 엘리트 스폰 확률, **스테이지 특성** 공통 10 + 테마 8 + **돌연변이 5종**, **고정 이정표**(5/15/25 휴식·이벤트, 10/20/30 보스), **엘리트 전용 투기장 4종**(M4 인계). 충돌 방지 규칙 6종 / 시너지 가중치 4종 / 테마 비율 40% 보장.
>
> **기간**: 3주 (누적 13주)
>
> **상위 문서**: [Implementation_Plan.md §마일스톤 5](Implementation_Plan.md)
>
> **검증 기준**:
> - 동일 시드 → **동일 레이아웃·기믹·몬스터 배치 재현성 100%** (자동 테스트)
> - Act 1 전체 30스테이지 절차 생성 → 풀 플레이 클리어 가능
> - **충돌 방지 규칙 6종 위반 0건** (시드 1000회 fuzz 테스트)
> - **테마 비율 40% 보장** (각 스테이지의 기믹 중 해당 액트 전용 기믹 비율 ≥ 40%)
> - 난이도 예산 공식 EditMode 테스트 통과 (Act 1 스테이지 1=108~132, 스테이지 29=612~748)
> - 일일 도전 시드(`날짜 해시`) — 동일 날짜·플레이어 무관 동일 결과
> - **스테이지 세로 총합 = 카메라 시야 세로 × 3** (`orthographicSize × 2 × 3`, ±0.5 Unit 오차 이내). 카메라 OrthoSize 변경 시(M1 #5 인계 — M7) 빌더가 자동 추종
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 미착수 또는 사용자 직접 확인 필요
>
> **작성일**: 2026-05-13 (사전 계획) · **갱신일**: 2026-05-13 (1차 구현 완료)
>
> **상태**: 핵심 시스템·SO 스키마 7종·데이터 풀 6종(Modifier 18/Mutation 5/Arena 4/Gimmick 22/Segment 11/Monster 8 SO)·시드 RNG·세그먼트 빌더(세로 합 강제)·기믹 셀렉터·웨이브 빌더·모디파이어·돌연변이·고정 이정표·ProceduralStageGenerator 10단계 파이프라인 완료. EditMode 140/140 → 데드존 제거로 134/134 통과 (DeadZoneBossFightTests 6건 삭제). 기믹 80종 중 6종만 본 구현(나머지는 PlaceholderGimmick으로 동작), 세그먼트/기믹 프리팹 비주얼은 마일스톤 8 인계. 인게임 절차 생성 호출 검증 완료.
>
> **2026-05-13 데드존 메커닉 제거 결정** — 외벽 닫힌 통으로 공이 사라지지 않음. 자연 낙사 페널티 시스템 삭제, 보스/탄막/기믹이 직접 발행하는 시간 페널티만 유지. BallController.CheckFallDead 제거 / DeadZone.cs deprecated / Constants.DeadzonePenalty·BossDeadzonePenalty 삭제 (BossForcedTimePenalty=-15f 신설) / FrostGiant·WinterQueen 강제 낙사 → 공 reset + 시간 페널티 / DeadZone GameObject 비활성. **ComboSystem 콤보 reset은 3초 무타격으로만 유지** (OnBallDead 구독 해제).

---

## 0. 선행 조건 (마일스톤 1·2·3·4 산출물 재사용)

마일스톤 5에서는 **확장과 호출만** 하고 기존 시그니처는 변경하지 않는다.

| 자산 | 재사용 포인트 |
|---|---|
| [Physics/PlayfieldBuilder.cs](../Assets/02.Scripts/Physics/PlayfieldBuilder.cs) | 9.0×12.0 Unit 플레이필드 빌드. **세그먼트 시스템이 이 위에 합쳐지므로** `BuildPlayfield(SegmentLayout)` 오버로드 추가. 기존 `Build()`는 검증용으로 유지 |
| [Physics/DeadZone.cs](../Assets/02.Scripts/Physics/DeadZone.cs) | 하단 세그먼트 고정 위치. 세그먼트 빌드 시 그대로 사용 |
| [Enemy/MonsterBase.cs](../Assets/02.Scripts/Enemy/MonsterBase.cs) | `WaveSpawner`가 `MonsterData` 기반으로 인스턴스화. Act 배율·StageIndex 비례 HP/ATK 적용은 `WaveSpawner`가 SO 기본값에 곱해 주입 |
| [Enemy/EliteAI/EliteBase.cs](../Assets/02.Scripts/Enemy/EliteAI/EliteBase.cs) | 보스 호위/엘리트 노드/돌연변이 보스 러시에서 그대로 사용. **엘리트 4종 투기장**은 EliteBase 그대로 + 고정 레이아웃 SO만 추가 |
| [Enemy/BossAI/BossBase.cs](../Assets/02.Scripts/Enemy/BossAI/BossBase.cs) | 10/20/30 고정 이정표에서 BossData 그대로 로드. **돌연변이 "보스 러시"** 는 HP 50% 상태로 BossBase 인스턴스화 |
| [Combat/StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs) | `AddTime` API 그대로. 휴식 노드(+20초), 이벤트 노드(신비한 제단 -30초), 시간 구슬 기믹에서 호출. **상한 60초/스테이지** 유지 |
| [Combat/BossFightContext.cs](../Assets/02.Scripts/Combat/BossFightContext.cs) | 10/20/30 보스 노드에서 Enter/Exit. 엘리트 투기장에서도 Enter (DeadZone -20초 분기 활성) |
| [Combat/ManaSystem.cs](../Assets/02.Scripts/Combat/ManaSystem.cs) | 휴식 노드 "마나 충전" 50%, 기믹 효과(예: 마나 결정)에서 호출 |
| [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) | 이벤트 추가: `OnStageGenerated`, `OnStageStart`, `OnStageClear`, `OnWaveSpawned`, `OnWaveCleared`, `OnGimmickActivated`, `OnGimmickDespawned`, `OnNodeEntered`(휴식/이벤트/엘리트/보스), `OnModifierApplied`, `OnMutationTriggered` |
| [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) | **§절차생성 / §세그먼트 / §기믹 / §웨이브 / §모디파이어 / §시드** 6개 섹션 신규 추가 |
| [Core/GameManager.cs](../Assets/02.Scripts/Core/GameManager.cs) | 씬 전환에 `LoadStage(StageRuntime)` 진입점 추가. ActMap → Stage 흐름은 M7에서 정식화, 마일스톤 5는 임시 진입점만 |
| [Data/MonsterData.cs](../Assets/02.Scripts/Data/MonsterData.cs) | 일반 몬스터 SO. **테마별 비주얼 풀 4종 × 5마리 = 20종 SO 신규 생성** 필요 (봄: 슬라임/독버섯/요정 전사/거미/식충식물 졸개 등) |
| [Data/EliteData.cs](../Assets/02.Scripts/Data/EliteData.cs) | M4에서 정의된 4종 그대로. 투기장 진입 조건만 별도 SO에서 정의 |

---

## 1. 데이터 정의 (ScriptableObject) — **가장 먼저**

> 코드 작성 전에 기믹 80종 + 세그먼트 풀 + 특성 + 돌연변이 + 투기장 슬롯을 먼저 정의. `Gimmick_List.md` · `Procedural_Stage_Gen.md` 표를 1:1로 반영해 추후 밸런스 수정 시 SO Inspector만 만지면 되도록 한다.

### 1.1 enum 추가 — [StageEnums.cs](../Assets/02.Scripts/Data/StageEnums.cs) 신규

- [x] `ActId` enum (Act1=봄, Act2=여름, Act3=가을, Act4=겨울)
- [x] `DifficultyBand` enum (Prologue=서막 1~9, Development=전개 11~19, Climax=클라이맥스 21~29)
- [x] `SegmentSlot` enum (Top, Middle, Bottom)
- [x] `GimmickCategory` enum (Buff, Debuff, Trial, Reward, Environment)
- [x] `GimmickPlacement` flags (TopSeg=1, MidSeg=2, BotSeg=4, GlobalModifier=8, BossRoom=16)
- [x] `GimmickId` enum 80종 (공통 1~20 / 봄 21~35 / 여름 36~50 / 가을 51~65 / 겨울 66~80)
- [x] `GimmickTriggerKind` enum (BallContact, Periodic, StageEnter, ExternalEvent, BossHpThreshold)
- [x] `WaveCompositionPattern` enum (MassRush, EliteMinority, BossEscort)
- [x] `NodeKind` enum (NormalBattle, EliteBattle, Boss, Event, Rest, Hidden, Tutorial)
- [x] `EventNodeKind` enum (SuspiciousTraveler, TreasureRoom, MysticAltar, WanderersGamble)
- [x] `ModifierId` enum 18종 (공통 10 + 봄 2 + 여름 2 + 가을 2 + 겨울 2)
- [x] `MutationId` enum 5종 (ThemeErosion, MirrorWorld, Miniature, TimeRush, BossRush)

### 1.2 [Data/GimmickData.cs](../Assets/02.Scripts/Data/GimmickData.cs) — `ScriptableObject`

- [x] `CreateAssetMenu("RPG Pinball/Stage/Gimmick")` + 모든 필드(식별/테마/판정/트리거/효과/메타/충돌·시너지/프리팹/설명) 구현 완료. 판정 슬롯은 `size`(Vector2)+`isCircularShape`(bool)로 단순화 (M8 폴리싱 시 세분화).

### 1.3 [Data/SegmentData.cs](../Assets/02.Scripts/Data/SegmentData.cs) — `ScriptableObject`

- [ ] `CreateAssetMenu("RPG Pinball/Stage/Segment")`
- [ ] 필드: `segmentId`(string), `slot`(SegmentSlot), `theme`(ActId? — null이면 공통), `prefab`(GameObject)
- [ ] 구조: `gimmickSlotAnchors`(Transform[] — 2~4개), `connectionPorts`(Vector2[] — 인접 세그먼트 연결 통로), `width`(=9.0 Unit 고정), `height`(중단 기본 3.5 Unit, 변형 허용 — **단, 누적 합계 = 카메라 시야 세로 × 3 강제. §3.2 빌더 참조**), `heightMin`/`heightMax`(허용 변형 범위, 빌더가 미세 조정용으로 사용), `wallPrefabs`(GameObject[])
- [ ] 분류 태그: `placementTags`(string[] — "DeadEnd"/"Open"/"RailHeavy" 등 — 상위 생성기가 검색 필터로 사용)
- [ ] 메타: `descriptionKo`

### 1.4 [Data/StageModifierData.cs](../Assets/02.Scripts/Data/StageModifierData.cs)

- [ ] `CreateAssetMenu("RPG Pinball/Stage/Modifier")`
- [ ] 필드: `modifierId`(ModifierId), `displayNameKo`, `themeOwner`(ActId? — null이면 공통), `tier`(int 1~5 — 별점), `descriptionKo`
- [ ] 효과 파라미터 슬롯 (모든 특성 통합):
  - `timeLimitDeltaSeconds`(쾌속전: -30), `monsterHpMultiplier`(쾌속전: 0.8, 황금 열풍: 1.3), `monsterDefMultiplier`(철벽 요새: 1.5), `deadzonePenaltyMultiplier`(사신의 손길: 2.0)
  - `eliteSpawnMultiplier`(사신의 손길: 2.0), `ballBaseSpeedMultiplier`(광란의 피치: 1.5), `flipperCooldownDelta`(광란의 피치: +0.3)
  - `gravityChaosIntervalSeconds`(혼돈의 중력: 5), `goldDropMultiplier`(황금 열풍: 3.0, 해적의 저주: 0.5), `criticalChanceDelta`(약점 노출: +0.2)
  - `ballFreezeTriggerSeconds`(영겁의 서리: 2.0), `gimbalRandomBuffChance`(도박사의 밤: 0.5)
  - `manaChargeMultiplier`(마력 폭풍: 2.0), `skillDamageDelta`(마력 폭풍: 0.3), `skillCostMultiplier`(마력 폭풍: 1.5)
  - `flipperFailChance`(꽃가루 알레르기: 0.3), `vegetationGrowthEnabled`(만개의 숲), `tideCycleSeconds`(밀물과 썰물: 30)
  - `rampGimmickToggleEnabled`(기계 오작동), `phaseShiftIntervalSeconds`(유령의 농담: 5)
  - `visionReductionPercent`(블리자드: 0.4), `timeWarpEnabled`(시간 왜곡 — 0.8~1.5 변동)
- [ ] 보상 보정: `goldRewardDelta`, `xpRewardDelta`, `runeDropChanceDelta`, `manaCrystalDelta`, `tarotShardDelta`, `coreShardChanceDelta`, `spRewardDelta`, `comboBonusDelta`
- [ ] 18종 SO 생성 (`04.Data/Modifiers/Modifier_*.asset`)

### 1.5 [Data/MutationData.cs](../Assets/02.Scripts/Data/MutationData.cs)

- [ ] `CreateAssetMenu("RPG Pinball/Stage/Mutation")`
- [ ] 필드: `mutationId`(MutationId), `displayNameKo`, `requiredDifficultyBand`(DifficultyBand flags — 일부는 클라이맥스만), `descriptionKo`
- [ ] 효과 파라미터:
  - **테마 침식**: `crossThemeGimmickCount`(1~2), `crossThemeSourceActs`(ActId[] — 다른 액트 기믹 풀)
  - **거울 세계**: `mirrorLayoutHorizontal`(true)
  - **미니어처**: `playfieldScaleMultiplier`(0.6), `wallElasticityMultiplier`(1.2)
  - **타임 러시**: `forceTimeLimit`(60), `monsterHpMultiplier`(0.5), `rewardMultiplier`(3.0)
  - **보스 러시**: `recurringBossHpRatio`(0.5), `bossPoolForAct`(BossId[])
- [ ] 공통 보상 보정: `goldMultiplier`(2.0), `rareRuneChanceDelta`(0.15), `iconKind`(string — "⚠️" 노드맵 표시)
- [ ] 5종 SO 생성 (`04.Data/Mutations/Mutation_*.asset`)

### 1.6 [Data/StageBlueprint.cs](../Assets/02.Scripts/Data/StageBlueprint.cs)

> 절차 생성 결과를 직렬화 가능한 형태로 표현 (시드 재현·일일 도전·세이브 양쪽 활용).

- [ ] `[Serializable]` 클래스 (SO 아님 — 런타임 생성 결과)
- [ ] 필드: `seed`(ulong), `actId`, `stageIndex`(1~30), `nodeKind`, `band`, `finalBudget`(int), `recommendedLevel`(int), `timeLimitSeconds`(180 기본)
- [ ] 레이아웃: `topSegmentId`(string), `middleSegmentIds`(string[]), `bottomSegmentId`(string)
- [ ] 기믹 배치: `gimmickPlacements`(struct{ GimmickId id, int segmentIndex, int slotIndex }[])
- [ ] 웨이브: `waves`(struct{ WaveCompositionPattern pattern, string[] monsterIds, bool hasElite }[])
- [ ] 특성·돌연변이: `modifierIds`(ModifierId[] — 0~2개), `mutationId`(MutationId? — 5% 확률, 명시적으로 nullable)

### 1.7 [Data/ArenaLayoutData.cs](../Assets/02.Scripts/Data/ArenaLayoutData.cs) — **[인계: M4 #1]** 엘리트 전용 투기장

- [ ] `CreateAssetMenu("RPG Pinball/Stage/Elite Arena")`
- [ ] 필드: `eliteId`(EliteId), `themeOwner`(ActId), `topSegmentId`/`middleSegmentIds`/`bottomSegmentId`, `lockedGimmickIds`(GimmickId[] — 투기장 고정 기믹), `forbiddenGimmickIds`(GimmickId[] — 투기장에서 금지)
- [ ] 4종 SO 생성:
  - `Arena_StormElemental.asset` — 봄 숲 (`themeOwner=Act1`)
  - `Arena_AbyssalLeviathan.asset` — 심해 (`themeOwner=Act2`)
  - `Arena_GoldenGoblinKing.asset` — 가을 미로 (`themeOwner=Act3`)
  - `Arena_FrostSentinel.asset` — 겨울 성벽 (`themeOwner=Act4`)

### 1.8 80종 GimmickData SO 생성

> `Gimmick_List.md` 표 1:1 반영. 모든 수치(반경/임펄스/지속시간/쿨다운/예산)는 인스펙터에서 그대로 조정 가능하도록 노출.

- [ ] **공통 베이스 20종** (`04.Data/Gimmicks/Common/`)
  - 1. 히든 범퍼 / 2. HP 몬스터 범퍼 / 3. 블랙홀 웜홀 / 4. 가속 레일 / 5. 점프 패드 / 6. 일방통행 게이트 / 7. 폭발성 드럼통 / 8. 텔레포트 패널 / 9. 회전 교차로 / 10. 카지노 범퍼
  - 11. 시간 구슬 / 12. 자성 블록 / 13. 슬링샷 / 14. 댄싱 범퍼 / 15. 미스트 존 / 16. 도펠갱어 / 17. 미러 월 / 18. 보너스 슬롯 / 19. 컨베이어 / 20. 디스라이트 봉인 (`Gimmick_List.md` 본문 확인 후 매핑)
- [ ] **봄 전용 15종** (`04.Data/Gimmicks/Spring/`) — 21~35: 정화의 불꽃 · 광란의 물약 · 거대 슬라임(보스전) · 강풍 경보(모디파이어 후보) · 플리퍼 침묵 · 유령 범퍼 · 외 9종
- [ ] **여름 전용 15종** (`04.Data/Gimmicks/Summer/`) — 36~50: 소용돌이 · 투망 함정 · 널판지 도개교 · 해적 폭탄(보스전) · 해류 에스컬레이터 · 외 10종
- [ ] **가을 전용 15종** (`04.Data/Gimmicks/Autumn/`) — 51~65: `Gimmick_List.md` 본문 매핑
- [ ] **겨울 전용 15종** (`04.Data/Gimmicks/Winter/`) — 66~80
- [ ] 각 SO에 `conflictingIds`/`synergyIds` 채워 충돌 방지·시너지 규칙을 데이터 단에서 자동 적용 (코드 분기 최소화)

### 1.9 일반 몬스터 SO 20종 (테마별 5종 × 4)

- [ ] `04.Data/Monsters/Spring/` — 슬라임 · 독버섯 · 요정 전사 · 거미 · 식충식물 졸개
- [ ] `04.Data/Monsters/Summer/` — 해적 졸병 · 해파리 · 게 전사 · 상어 · 문어
- [ ] `04.Data/Monsters/Autumn/` — 톱니 골렘 · 유령 · 가면 광대 · 기계 인형 · 호박 워리어
- [ ] `04.Data/Monsters/Winter/` — 얼음 골렘 · 눈사람 병사 · 서리 정령 · 늑대 · 빙하 기사
- [ ] 기본 스탯은 `Game_Design_Spec.md` §9 (BaseHP=100, BaseATK=5, BaseDEF=0%, BaseXP=12) — Act 배율·StageIndex 비례 곱셈은 `WaveSpawner`가 런타임 주입

### 1.10 세그먼트 프리팹 풀

- [ ] **상단 세그먼트**: 테마별 8~12종 × 4테마 = 32~48 프리팹 + SegmentData
- [ ] **중단 세그먼트**: 테마별 15~20종 × 4테마 = 60~80 프리팹 + SegmentData
- [ ] **하단 세그먼트**: 고정 1종 (플리퍼 존 + 낙사 라인) — 기존 [PlayfieldBuilder.cs](../Assets/02.Scripts/Physics/PlayfieldBuilder.cs)에서 마이그레이션
- [ ] 1차 구현은 **테마당 상단 2종 + 중단 4종 + 하단 1종 = 7종 × 4 = 28종 최소 풀**로 시작. 잔여 풀은 마일스톤 8 폴리싱에서 확장

---

## 2. **시드 기반 결정론적 RNG**

> 같은 시드 → 같은 스테이지 보장. `System.Random`은 알고리즘이 .NET 버전 의존이므로 자체 PRNG 구현.

### 2.1 [Stage/Generation/DeterministicRng.cs](../Assets/02.Scripts/Stage/Generation/DeterministicRng.cs)

- [ ] `xoshiro256**` 또는 `pcg32` 기반 자체 PRNG (UnityEngine.Random 사용 금지 — 전역 상태 공유로 인한 시드 누출 위험)
- [ ] API: `NextInt(min, max)`, `NextFloat(min, max)`, `NextDouble()`, `Pick<T>(IList<T>)`, `Shuffle<T>(IList<T>)`, `WeightedPick<T>(IList<(T, float)>)`, `RollChance(float p)`
- [ ] 시드 합성: `static ulong CombineSeed(ulong baseSeed, params int[] salts)` — XorShift 기반 결정론적 mix

### 2.2 [Stage/Generation/StageSeedFactory.cs](../Assets/02.Scripts/Stage/Generation/StageSeedFactory.cs)

- [ ] `BuildSeed(string playerUid, DateTime nowUtc9, int actId, int stageIndex)` — 최초 진입용
- [ ] `BuildSeedForRetry(ulong baseSeed, int retryCount)` — 재도전 시 다른 레이아웃
- [ ] `BuildDailyChallengeSeed(DateTime nowUtc9, int actId, int stageIndex)` — 플레이어 무관 동일 결과 (주점 일일 의뢰 연동)
- [ ] 자정(UTC+9) 갱신 로직 — `DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9))`

### 2.3 검증

- [ ] [SeedReproducibilityTests.cs](../Assets/Tests/EditMode/SeedReproducibilityTests.cs) — 동일 시드 1000회 생성 → 모든 출력 동일 (StageBlueprint deep equality 비교)
- [ ] 일일 도전 시드는 **다른 PlayerUID로도 동일** — 별도 테스트 케이스

---

## 3. 세그먼트 시스템

### 3.1 [Stage/Segments/SegmentPool.cs](../Assets/02.Scripts/Stage/Segments/SegmentPool.cs)

- [ ] 싱글톤. `Resources.LoadAll<SegmentData>("Segments")` 또는 Addressables 키로 풀 로드
- [ ] API:
  - `PickTopSegment(ActId, DeterministicRng) → SegmentData`
  - `PickMiddleSegments(ActId, DeterministicRng, int count) → SegmentData[]` (count=2~4, band 기반)
  - `GetBottomSegment() → SegmentData` (고정 1종)
- [ ] 가중치 규칙: 직전 스테이지에서 등장한 세그먼트는 가중치 ×0.5 (중복 방지). 직전 스테이지 ID는 `PlayerData`에 휘발성 캐시

### 3.2 [Stage/Segments/SegmentLayoutBuilder.cs](../Assets/02.Scripts/Stage/Segments/SegmentLayoutBuilder.cs)

- [ ] `BuildLayout(StageBlueprint) → SegmentLayout` — 상단(고정 위치) + 중단(스택, 2~4개 수직 누적) + 하단(고정 위치)
- [ ] **스테이지 세로 총합 = 카메라 시야 세로 × 3** 강제 — 목표 세로 `TargetStageHeight = Camera.main.orthographicSize × 2 × Constants.SegStageVerticalScreenCount`(=3.0). 빌더는 `(TopHeight + Σ MiddleHeight + BottomHeight)`가 `TargetStageHeight ±0.5 Unit` 오차 이내가 되도록 조정
- [ ] **세로 합 맞춤 우선순위** (충돌 해결): ① 중단 세그먼트의 `heightMin`/`heightMax` 범위 내 height 변형 ② band 허용 범위 내에서 중단 개수 ±1 (서막 2 ↔ 3 / 클라이맥스 3 ↔ 4) ③ 다른 SegmentData로 재추첨 (가중치 ×0.5 페널티). 3차 시도까지 실패 시 디버그 로그 + Fallback 세그먼트 적용
- [ ] 빌드 결과 `SegmentLayout.totalHeight` 필드에 실제 세로 합을 기록해 카메라/플레이필드/물리 경계가 참조
- [ ] 연결 통로 검증: 인접 중단 세그먼트의 `connectionPorts` 중 1개 이상이 ±0.5U 이내 정렬되어야 함. 미정렬 시 빌더가 회전 또는 swap 시도
- [ ] Dead End 검증: 출구가 0인 세그먼트는 보상 기믹이 동시 배치된 경우에만 허용. 그 외 경우 빌더가 다른 세그먼트로 재추첨

### 3.3 [Stage/Segments/SegmentRuntime.cs](../Assets/02.Scripts/Stage/Segments/SegmentRuntime.cs)

- [ ] 세그먼트 프리팹 인스턴스의 런타임 컨테이너. `gimmickSlotAnchors`/현재 활성 기믹/연결 통로 노출
- [ ] OnDestroy 시 자식 기믹/몬스터/탄막 일괄 정리 (씬 전환 안전성)

### 3.4 난이도 구간별 세그먼트 수 결정

- [ ] 서막(1~9): 중단 2개 / 전개(11~19): 3개 / 클라이맥스(21~29): 3~4개 (난이도 예산 잔여분에 따라 +1)
- [ ] 위 기본값은 **세로 총합 제약(§3.2)을 만족하도록 빌더가 ±1 범위에서 추가 조정 가능**. 예: 카메라 OrthoSize=9 → 목표 세로 54U → 상단 4U + 하단 6U + 중단 4×11U 또는 중단 3×14.7U 등으로 맞춤
- [ ] [SegmentLayoutTests.cs](../Assets/Tests/EditMode/SegmentLayoutTests.cs) — 각 band에서 중단 수 정상 결정 + 연결 통로 검증 + **세로 총합 = 화면 세로 × 3 (±0.5U)** 통과

---

## 4. 난이도 예산 시스템

### 4.1 [Stage/Generation/DifficultyBudget.cs](../Assets/02.Scripts/Stage/Generation/DifficultyBudget.cs)

- [ ] 공식 구현: `BaseBudget = 100 + (StageIndex * 20)`, `ActMultiplier` 테이블(Act1=1.0, Act2=1.8, Act3=2.5, Act4=3.5), `FinalBudget = BaseBudget × ActMultiplier × (1 + Rng.NextFloat(-0.1f, 0.1f))`
- [ ] 권장 레벨 공식: `RecommendedLevel = ActStartLevel + floor(StageIndex * (ActEndLevel - ActStartLevel) / 30)`
- [ ] `ActLevelRange` 테이블 (Act1=1~25, Act2=25~50, Act3=50~72, Act4=72~90)

### 4.2 예산 소비 엔진 [Stage/Generation/BudgetConsumer.cs](../Assets/02.Scripts/Stage/Generation/BudgetConsumer.cs)

- [ ] `TryConsume(int cost) → bool` — 잔여 예산 ≥ cost일 때만 true 반환
- [ ] `RefundReward(int reward)` — 보상 기믹(예산 음수)으로 환급 (예: 히든 범퍼 -10)
- [ ] **핵심 밸런스 규칙**:
  - 각 스테이지 기믹 중 **최소 1개는 버프/보상** 강제 (`RewardOrBuffRequired` 플래그로 보장)
  - **시련 기믹 3개 이상 중첩 시** 보상 기믹 1개 자동 추가 (구제 규칙)
  - 남은 예산 음수 → 추가 배치 중단

### 4.3 검증

- [ ] [DifficultyBudgetTests.cs](../Assets/Tests/EditMode/DifficultyBudgetTests.cs)
  - Act1·Stage1: 108~132
  - Act1·Stage9: 252~308
  - Act1·Stage29: 612~748
  - Act4·Stage1: 378~462
  - Act4·Stage29: 2,142~2,618
  - 모두 100회 시뮬레이션해 분포 검증 (max/min/mean)

---

## 5. 절차 생성 엔진 — `Procedural_Stage_Gen.md` §10 10단계 플로우

### 5.1 [Stage/Generation/ProceduralStageGenerator.cs](../Assets/02.Scripts/Stage/Generation/ProceduralStageGenerator.cs)

- [ ] 메인 진입점: `Generate(int actId, int stageIndex, ulong seed) → StageBlueprint`
- [ ] 10단계 파이프라인 그대로 구현:
  1. **고정 이정표 검사** — `MilestoneManager.IsMilestone(stage)`면 보스/휴식·이벤트 분기로 라우팅, 종료
  2. **시드 → DeterministicRng 인스턴스화**
  3. **난이도 예산 계산** (Budget + 권장 레벨)
  4. **세그먼트 조합** (Top + Middle × N + Bottom)
  5. **기믹 풀에서 추출** (예산 소비 + 충돌 방지 + 테마 비율 40% + 시너지 가중치)
  6. **몬스터 웨이브 생성** (WaveCount + 패턴 + 엘리트 확률)
  7. **돌연변이 판정** (5% 확률, band 조건 필터)
  8. **스테이지 특성 적용** (band 기반 0~2개)
  9. **튜토리얼 분기** (Act1 1~3은 고정 콘텐츠 강제 주입 — M8 TutorialManager가 받음)
  10. **StageBlueprint 직렬화 반환**
- [ ] 각 단계는 `private` 메서드로 분리. 단위 테스트 용이성을 위해 외부 인터페이스로 의존성 주입(`IGimmickPool`/`ISegmentPool`/`IMonsterPool`)

### 5.2 [Stage/Generation/GimmickSelector.cs](../Assets/02.Scripts/Stage/Generation/GimmickSelector.cs)

- [ ] 기믹 수 결정 (스테이지 전체 합계): 서막 1~2 / 전개 3~4 / 클라이맥스 4~5
- [ ] 가중치 적용: 액트 전용 기믹 ×2, 직전 스테이지 등장 기믹 ×0.5
- [ ] 충돌 방지 규칙 6종 (`GimmickData.conflictingIds`로 자동 적용) — 위반 시 재추첨
- [ ] 시너지 조합 4종 (`GimmickData.synergyIds`로 가중치 ×1.5)
- [ ] **테마 비율 보장**: 추첨 후 액트 전용 비율이 40% 미만이면 테마 기믹 강제 1~2개 추가 (예산 무시 또는 다른 기믹 1개 제거)

### 5.3 [Stage/Generation/StageRuntimeBuilder.cs](../Assets/02.Scripts/Stage/Generation/StageRuntimeBuilder.cs)

- [ ] `Build(StageBlueprint) → StageRuntime` (Scene 단위 인스턴스화)
- [ ] 세그먼트 프리팹 인스턴스화 → 기믹 프리팹을 슬롯 앵커에 배치 → 웨이브 매니저 초기화 → 모디파이어 적용 → 카메라 바인딩
- [ ] `OnDestroy`에서 모든 인스턴스 정리 (`SegmentRuntime` 위임)

### 5.4 검증

- [ ] [ProceduralStageGenerationTests.cs](../Assets/Tests/EditMode/ProceduralStageGenerationTests.cs)
  - 시드 1000회 fuzz — **충돌 방지 규칙 위반 0건**
  - 테마 비율 40% 위반 0건
  - 보상/버프 1개 이상 포함 0건 위반
  - 시련 3개 중첩 시 보상 자동 추가 검증
  - StageBlueprint 직렬화 → 역직렬화 → 동등성 통과

---

## 6. 기믹 시스템

### 6.1 [Stage/Gimmicks/GimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/GimmickBase.cs)

- [ ] 추상 MonoBehaviour. `GimmickData data` SerializeField + `Initialize(GimmickData, SegmentRuntime)` API
- [ ] 공통 라이프사이클: `OnSpawn(seed)`, `OnDespawn()`, `OnBallEnter(BallController)`, `OnPeriodicTick()`, `OnExternalTrigger(string eventId)`
- [ ] `EventBus.PublishGimmickActivated/Despawned` 자동 호출
- [ ] 저항력 감쇠 헬퍼: `GetEffectiveDurationSeconds()` — `Data_Schema.md §7` 도감 저항력 0~40% 반영
- [ ] 블로킹 처리 추상: `bool TryBlock(BallController/FlipperController)`

### 6.2 카테고리별 베이스 클래스

- [ ] [Gimmicks/Bases/BumperGimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/Bases/BumperGimmickBase.cs) — 임펄스 발생 공통 (히든 범퍼 / HP 범퍼 / 카지노 범퍼 / 유령 범퍼)
- [ ] [Gimmicks/Bases/TeleportGimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/Bases/TeleportGimmickBase.cs) — 위치 이동 + 속도 보존 공통 (블랙홀 웜홀 / 텔레포트 패널)
- [ ] [Gimmicks/Bases/RailGimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/Bases/RailGimmickBase.cs) — 강제 가속 + 방향 정렬 공통 (가속 레일 / 해류 에스컬레이터 / 컨베이어)
- [ ] [Gimmicks/Bases/AreaEffectGimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/Bases/AreaEffectGimmickBase.cs) — 영역 진입 시 디버프/버프 공통 (미스트 존 / 소용돌이 / 빙결 오라)
- [ ] [Gimmicks/Bases/PeriodicGimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/Bases/PeriodicGimmickBase.cs) — 주기 트리거 공통 (꽃가루 침묵 / 강풍 경보 / 도개교 / 유령 범퍼 / 댄싱 범퍼)

### 6.3 80종 GimmickComponent 구현

> 카테고리별 베이스를 상속해 차이점만 채움. 각 컴포넌트는 `GimmickData` SO에서 모든 수치를 읽어와 매직 넘버 0 유지.

#### 6.3.1 공통 베이스 20종 — [Gimmicks/Common/](../Assets/02.Scripts/Stage/Gimmicks/Common/)

- [ ] 히든 범퍼 / HP 몬스터 범퍼 / 블랙홀 웜홀 / 가속 레일 / 점프 패드 / 일방통행 게이트 / 폭발성 드럼통 / 텔레포트 패널 / 회전 교차로 / 카지노 범퍼
- [ ] 시간 구슬 / 자성 블록 / 슬링샷 / 댄싱 범퍼 / 미스트 존 / 도펠갱어 / 미러 월 / 보너스 슬롯 / 컨베이어 / 디스라이트 봉인

#### 6.3.2 봄 전용 15종 — [Gimmicks/Spring/](../Assets/02.Scripts/Stage/Gimmicks/Spring/)

- [ ] 정화의 불꽃 / 광란의 물약 / 거대 슬라임(보스 룸 한정) / 강풍 경보 / 플리퍼 침묵(꽃가루 알레르기) / 유령 범퍼 / 외 9종

#### 6.3.3 여름 전용 15종 — [Gimmicks/Summer/](../Assets/02.Scripts/Stage/Gimmicks/Summer/)

- [ ] 소용돌이 / 투망 함정 / 널판지 도개교 / 해적 폭탄(보스 룸 한정) / 해류 에스컬레이터 / 외 10종

#### 6.3.4 가을 전용 15종 — [Gimmicks/Autumn/](../Assets/02.Scripts/Stage/Gimmicks/Autumn/)

- [ ] 톱니 트랩 / 시계 폭탄 / 호박 함정 / 가면 회전판 / 외 11종 (`Gimmick_List.md` 본문 매핑)

#### 6.3.5 겨울 전용 15종 — [Gimmicks/Winter/](../Assets/02.Scripts/Stage/Gimmicks/Winter/)

- [ ] 빙결 바닥 / 블리자드 영역 / 고드름 함정 / 서리 늪 / 시간 정지 결정 / 외 10종

### 6.4 충돌 방지 규칙 자동 검증

- [ ] `GimmickData.conflictingIds`는 6쌍 자동 양방향 등록:
  - 중력 역전 ↔ 얼음 바닥
  - 어둠의 장막 ↔ 짙은 안개
  - 플리퍼 침묵 ↔ 빙결 바닥
  - 무거워진 공 ↔ 중력 역전
  - 반전 거울 ↔ 투명 플리퍼
  - 동상 래그 ↔ 플리퍼 침묵
- [ ] [GimmickConflictTests.cs](../Assets/Tests/EditMode/GimmickConflictTests.cs) — fuzz 1000 시드 위반 0건

### 6.5 후속 인계

- 모든 기믹의 **시각 이펙트(VFX)·사운드**는 마일스톤 8 폴리싱으로 인계. 마일스톤 5는 **로직과 콜라이더, 디버그 색상**까지만.
- 도감 저항력 시스템 본격 구현(점성술사 시설)은 **마일스톤 6** 인계. 마일스톤 5는 `GimmickBase.GetEffectiveDurationSeconds()` API와 SO `respectsResistance` 플래그까지만 정의하고, 저항 수치는 0(미적용)으로 통과.

---

## 7. 몬스터 웨이브 시스템

### 7.1 [Stage/Wave/WaveManager.cs](../Assets/02.Scripts/Stage/Wave/WaveManager.cs)

- [ ] 싱글톤 또는 StageRuntime 내부. `Initialize(StageBlueprint)`로 웨이브 큐 적재
- [ ] `WaveCount = floor(StageIndex / 5) + 1` (결과 1~7)
- [ ] 웨이브 시작/종료 이벤트: `EventBus.OnWaveSpawned`, `OnWaveCleared`
- [ ] 모든 몬스터 처치 시 다음 웨이브 즉시 진행

### 7.2 [Stage/Wave/WaveSpawner.cs](../Assets/02.Scripts/Stage/Wave/WaveSpawner.cs)

- [ ] 패턴별 스폰:
  - **물량 러시**: 소형 몬스터 8~12마리, HP ×0.5
  - **정예 소수**: 중형 몬스터 3~5마리, HP ×1.5
  - **보스 호위**: 엘리트 1 + 소형 4~6
- [ ] 엘리트 스폰 확률: `min(0.5, 0.05 * StageIndex)` — 트리거 시 일반 몬스터 수 50% 감소
- [ ] HP/ATK 동적 스케일링:
  - `MonsterHP = BaseHP × ActMultiplier × (1 + StageIndex × 0.05)`
  - `MonsterATK = BaseATK × ActMultiplier × (1 + StageIndex × 0.03)`
- [ ] 테마 비주얼 풀 보장 — `themeOwner == currentActId` 또는 공통 풀에서만 선택

### 7.3 [Stage/Wave/WaveCompositionPicker.cs](../Assets/02.Scripts/Stage/Wave/WaveCompositionPicker.cs)

- [ ] 3패턴 가중치 추첨 — band별 기본 33/33/33, 클라이맥스에서 "보스 호위" 가중치 ×1.5
- [ ] 직전 웨이브 패턴 가중치 ×0.5 (단조로움 방지)

### 7.4 검증

- [ ] [WaveCompositionTests.cs](../Assets/Tests/EditMode/WaveCompositionTests.cs) — 패턴별 몬스터 수 정상, 엘리트 확률 0/0.05/0.5 경계값
- [ ] [WaveScalingTests.cs](../Assets/Tests/EditMode/WaveScalingTests.cs) — Act 1 Stage 1 HP = 100×1.0×1.05 = 105 등 다 케이스 통과

---

## 8. 스테이지 특성(Modifier) + 돌연변이

### 8.1 [Stage/Modifiers/ModifierApplier.cs](../Assets/02.Scripts/Stage/Modifiers/ModifierApplier.cs)

- [ ] StageRuntime 인스턴스화 시 `StageBlueprint.modifierIds` 순회하며 효과 적용
- [ ] band별 개수 결정:
  - 서막(1~9): 0개 (학습 구간 보호)
  - 전개(11~19): 50% 확률로 1개
  - 클라이맥스(21~29): 100% 확률로 1개, 30% 확률로 추가 1개
- [ ] 각 모디파이어가 만지는 대상별 dispatcher:
  - 시간/페널티 → `StageTimer`
  - 몬스터 스탯 → `WaveSpawner` 곱셈 인자 주입
  - 공 속도/마찰 → `BallController`
  - 플리퍼 쿨다운 → `FlipperController`
  - 시야 → `CameraController.SetVisionReduction(percent)` (M4 인계 — 새 API 추가)
  - 기믹 토글 → `SegmentRuntime.SetGimmicksActive(bool)`
- [ ] **테마 비율 보장**: 해당 액트 테마 특성은 가중치 ×2

### 8.2 [Stage/Modifiers/MutationApplier.cs](../Assets/02.Scripts/Stage/Modifiers/MutationApplier.cs)

- [ ] 5% 확률 판정 → 해당되면 5종 중 1종 추첨 (band 조건 필터)
  - **테마 침식**: 전개 구간 이후 — 다른 액트의 GimmickPool에서 1~2개 추가 추첨
  - **거울 세계**: 어디서든 — `SegmentLayout.HorizontalMirror()` 변환
  - **미니어처**: 클라이맥스만 — Playfield Scale 0.6, 카메라 줌 보정
  - **타임 러시**: 어디서든 — `StageTimer.ForceLimit(60)`, 몬스터 HP ×0.5
  - **보스 러시**: 클라이맥스만 — 해당 액트의 중간보스(10, 20) BossId 풀에서 1체를 HP 50% 상태로 추가 스폰
- [ ] 노드맵에 ⚠️ 아이콘 표시는 **마일스톤 7 ActMapUI** 인계 — 마일스톤 5는 `StageBlueprint.mutationId` 노출까지

### 8.3 검증

- [ ] [ModifierApplicationTests.cs](../Assets/Tests/EditMode/ModifierApplicationTests.cs) — 18종 모디파이어 각 1회 적용 후 대상 객체 상태 변경 확인
- [ ] [MutationTriggerTests.cs](../Assets/Tests/EditMode/MutationTriggerTests.cs) — 5% 확률, band 조건 필터, 5종 모두 적어도 1회 적중하는 fuzz 100k 시드

---

## 9. 고정 이정표 (Milestone) 시스템

### 9.1 [Stage/MilestoneManager.cs](../Assets/02.Scripts/Stage/MilestoneManager.cs)

- [ ] `IsMilestone(stageIndex) → NodeKind?` — 5/15/25=휴식 또는 이벤트 / 10/20=중간 보스 / 30=최종 보스
- [ ] 휴식/이벤트 70/30 분기 — Act1 1~3은 강제 튜토리얼(M8 인계)
- [ ] 보스 노드: `GetMilestoneBossId(actId, stageIndex)` — 10/20/30 각각 액트별 정해진 BossId 반환

### 9.2 [Stage/Nodes/RestNode.cs](../Assets/02.Scripts/Stage/Nodes/RestNode.cs)

- [ ] 3종 효과 메뉴 UI: 시간 회복 +20초 / 마나 충전 50% / 소매품 구매
  - UI는 [M7] 인계, 마일스톤 5는 API 호출까지: `StageTimer.AddBonusTime(20)` 등
- [ ] 시간 회복은 **상한 60초/스테이지 누적 한도에 포함되지 않음** 명시 (`AddTime`이 아닌 신규 `AddBonusTime` 분기 추가)

### 9.3 [Stage/Nodes/EventNode.cs](../Assets/02.Scripts/Stage/Nodes/EventNode.cs)

- [ ] 4종 이벤트 분기:
  - **수상한 여행자**: 2택 1 — "다음 스테이지 몬스터 HP -20%" vs "다음 스테이지 보상 2배" (StageBlueprint에 임시 플래그 저장)
  - **보물 방**: 보물상자 3개 — 골드/코어 조각/타로카드 랜덤 (코어 조각·타로카드는 M6 인계, 마일스톤 5는 골드만 즉시 지급)
  - **신비한 제단**: 시간 -30초 ↔ SP +1 (LevelSystem.AwardBonusSP(1) 호출)
  - **방랑자의 도박**: 카지노 범퍼 풀 화면 진입 — 대박 버프 or 디버프
- [ ] 모든 이벤트는 EventBus.OnNodeEntered 발행, 결과는 StageBlueprint.eventOutcomeId에 기록

### 9.4 [Stage/Nodes/BossNode.cs](../Assets/02.Scripts/Stage/Nodes/BossNode.cs)

- [ ] BossData 로드 → BossBase 인스턴스화 → BossFightContext.Enter
- [ ] 보스 전용 고정 레이아웃은 마일스톤 4에서 이미 보스가 자체 배치하므로 마일스톤 5는 호출만

### 9.5 노드 유형 비율 검증

- [ ] [NodeRatioTests.cs](../Assets/Tests/EditMode/NodeRatioTests.cs) — 30노드 시뮬레이션 후 비율 검증 (일반 70% / 엘리트 10% / 보스 10% / 이벤트 10% / 휴식 5% / 히든 5%, ±5%p 허용)
- [ ] 히든 노드 변환: 절차 생성 노드 중 5% 확률로 NodeKind=Hidden 변환 (열기구 3단계 시 +15% 확률은 M6 인계)

---

## 10. **[인계: M4 #1]** 엘리트 전용 투기장 4종

> 절차 생성이 아닌 **고정 레이아웃 SO** 정의. M4에서 엘리트 본체는 완성 → 마일스톤 5는 입장 컨테이너만.

### 10.1 [Stage/Nodes/EliteArenaNode.cs](../Assets/02.Scripts/Stage/Nodes/EliteArenaNode.cs)

- [ ] `ArenaLayoutData` 로드 → 고정 세그먼트 인스턴스화 → 잠금 기믹 강제 배치 → 금지 기믹 제외
- [ ] EliteBase 인스턴스화 → BossFightContext.Enter (DeadZone -20초 분기 자동 활성)
- [ ] 엘리트 격파 보상은 M4에서 OnBossDefeated 이벤트만 발행 — 실지급은 M6 EconomyManager 인계

### 10.2 4종 투기장 SegmentData·ArenaLayoutData 세팅

- [ ] **봄 숲** — `Arena_StormElemental` — 활엽수 가지 배경, 중단 세그먼트 폭풍 잔상 친화적 (장애물 적음 + 가속 레일 1)
- [ ] **심해** — `Arena_AbyssalLeviathan` — 산호 기둥 + 잠수 사각지대 + 소용돌이 2개 잠금 배치
- [ ] **가을 미로** — `Arena_GoldenGoblinKing` — 미로형 격자 + 황금 폭탄 함정 + 보물 상자 시뮬레이션 노드
- [ ] **겨울 성벽** — `Arena_FrostSentinel` — 정면 방패 무적 분기 활용 가능한 좁은 통로 + 빙결 바닥 잠금

### 10.3 입장 조건 검증

- [ ] **해당 액트 최종 보스 처치 여부 검증**은 마일스톤 6 주점 시스템 인계 — 마일스톤 5는 `EliteArenaNode.CanEnter(playerData)` API 스텁만 (`return true` 기본값)

---

## 11. EventBus / Constants 확장

### 11.1 [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) 추가 이벤트

- [ ] `OnStageGenerated(StageBlueprint)`, `OnStageStart(StageBlueprint)`, `OnStageClear(StageBlueprint, ClearGrade)`
- [ ] `OnWaveSpawned(int waveIndex, MonsterBase[])`, `OnWaveCleared(int waveIndex)`
- [ ] `OnGimmickActivated(GimmickId, Vector2 pos)`, `OnGimmickDespawned(GimmickId)`
- [ ] `OnNodeEntered(NodeKind, int stageIndex)`
- [ ] `OnModifierApplied(ModifierId[])`, `OnMutationTriggered(MutationId)`
- [ ] `OnTimeBonusAdded(float seconds)` — 휴식 노드 +20초가 상한 외 누적임을 구독자(StageTimer)가 분기

### 11.2 [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) 신규 섹션

- [ ] **§절차생성**: `ProcMutationChance=0.05f`, `ProcThemeRatioMin=0.4f`, `ProcRewardOrBuffMin=1`, `ProcTrialOverloadThreshold=3`
- [ ] **§세그먼트**: `SegPlayfieldWidth=9.0f`(가로 고정), `SegStageVerticalScreenCount=3.0f`(**한 스테이지 세로 = 카메라 시야 세로 × 이 값**), `SegMiddleHeightDefault=3.5f`, `SegHeightTolerance=0.5f`(세로 합 허용 오차 Unit), `SegTopHeightDefault=4.0f`, `SegBottomHeightDefault=6.0f`(플리퍼 + 낙사 라인). 런타임에서 `TargetStageHeight = Camera.main.orthographicSize × 2 × SegStageVerticalScreenCount`로 계산 (기존 단일 화면 12.0 Unit 상수는 폐기 또는 Bottom 세그먼트 전용으로 한정)
- [ ] **§기믹**: `GimmickDuplicateWeightDecay=0.5f`, `GimmickThemeWeightBonus=2.0f`, `GimmickSynergyWeightBonus=1.5f`
- [ ] **§웨이브**: `WaveCountBase=1`, `WaveCountPer5Stages=1`, `WaveCountMax=7`, `EliteSpawnRatePerStage=0.05f`, `EliteSpawnRateMax=0.5f`, `MassRushMinCount=8`, `MassRushMaxCount=12`, `EliteMinorityMinCount=3`, `EliteMinorityMaxCount=5`
- [ ] **§모디파이어**: `ModifierProbDevelopment=0.5f`, `ModifierProbClimaxExtra=0.3f`
- [ ] **§시드**: `SeedSaltActiKey=17`, `SeedSaltStageKey=53`, `SeedSaltRetryKey=131`, `SeedSaltDailyKey=2027`
- [ ] **§휴식·이벤트**: `RestNodeBonusTime=20f`, `MysticAltarTimeCost=30f`, `MysticAltarSpReward=1`, `EventRestRatio=0.7f`
- [ ] **§돌연변이**: `MutationGoldMultiplier=2.0f`, `MutationRareRuneDelta=0.15f`, `MutationMiniaturePlayfieldScale=0.6f`, `MutationBossRushHpRatio=0.5f`
- [ ] **§노드 비율**: `NodeRatioNormal=0.7f`, `NodeRatioElite=0.1f`, `NodeRatioEvent=0.1f`, `NodeRatioRest=0.05f`, `NodeRatioHidden=0.05f`

---

## 12. 검증 체크리스트

### 12.1 단위 테스트 (EditMode) — 목표 신규 25건 추가 (누적 124+건)

- [ ] [SeedReproducibilityTests.cs](../Assets/Tests/EditMode/SeedReproducibilityTests.cs) — 동일 시드 1000회 동일 결과 (3건)
- [ ] [DifficultyBudgetTests.cs](../Assets/Tests/EditMode/DifficultyBudgetTests.cs) — Act 1·4 경계 + 분포 (5건)
- [ ] [SegmentLayoutTests.cs](../Assets/Tests/EditMode/SegmentLayoutTests.cs) — band별 중단 수 + 연결 통로 + Dead End + **세로 총합 = 화면 세로 × 3 (±0.5U) — OrthoSize 6/9/12 3케이스** (6건)
- [ ] [GimmickConflictTests.cs](../Assets/Tests/EditMode/GimmickConflictTests.cs) — 6쌍 충돌 방지 (6건)
- [ ] [WaveCompositionTests.cs](../Assets/Tests/EditMode/WaveCompositionTests.cs) — 3패턴 + 엘리트 확률 (4건)
- [ ] [WaveScalingTests.cs](../Assets/Tests/EditMode/WaveScalingTests.cs) — Act·Stage 스케일링 (3건)
- [ ] [ProceduralStageGenerationTests.cs](../Assets/Tests/EditMode/ProceduralStageGenerationTests.cs) — 통합 fuzz (3건)
- [ ] [ModifierApplicationTests.cs](../Assets/Tests/EditMode/ModifierApplicationTests.cs) — 18종 적용 검증 (2건)
- [ ] [MutationTriggerTests.cs](../Assets/Tests/EditMode/MutationTriggerTests.cs) — 5종 적중 + band 필터 (2건)
- [ ] [NodeRatioTests.cs](../Assets/Tests/EditMode/NodeRatioTests.cs) — 30노드 비율 (1건)

> M3 75건 + M4 24건 = M5 직전 99건 → M5 신규 32건 합쳐 **목표 131건 통과**

### 12.2 PlayMode 자동 시뮬레이션

| 시나리오 | 기대 결과 |
|---|---|
| 동일 시드 → StageBlueprint deep equality | True |
| 일일 도전 시드 (다른 PlayerUID, 동일 날짜) → 동일 결과 | True |
| Act 1 전체 30스테이지 자동 생성 → 모든 스테이지에 보상/버프 1개 이상 포함 | True |
| 시드 1000회 fuzz → 충돌 방지 6규칙 위반 | 0건 |
| 시드 1000회 fuzz → 테마 비율 40% 미만 | 0건 |
| 시드 100k fuzz → 돌연변이 5종 모두 적중 | True |
| Act 1 Stage 1 예산 분포 (1000회) | 108~132 범위 95%+ |
| 시드 1000회 빌드 → 스테이지 세로 총합이 `OrthoSize × 6 ±0.5U` 범위 위반 | 0건 |
| 카메라 OrthoSize 6 / 9 / 12 변경 후 재빌드 → 새 목표 세로 자동 추종 | True |

### 12.3 인게임 검증 (사용자 확인 필요)

- [ ] **Sample 씬 복제 → ProcStage_Test.unity** 생성 (테스트용)
- [ ] Inspector에서 ActId/StageIndex 지정 → Generate → 인게임 풀 플레이
- [ ] Act 1 전체 30스테이지 절차 생성 → 풀 플레이 클리어 가능
- [ ] 휴식 노드(5/15/25) 진입 → 3종 효과 메뉴 동작
- [ ] 이벤트 노드 진입 → 4종 이벤트 각각 동작 (특히 신비한 제단 SP +1 적용 확인)
- [ ] 돌연변이 5종 → 시드 강제 주입 모드로 각각 1회 발동 확인
- [ ] 엘리트 4종 투기장 → 각 액트의 노드에서 입장 → 격파 가능
- [ ] 동일 시드 재진입(재도전 X) → 동일 레이아웃 인스턴스화

### 12.4 문서 정합성

- [ ] [Procedural_Stage_Gen.md](../Design/Procedural_Stage_Gen.md) — 10단계 플로우 / 6충돌 규칙 / 4시너지 / 5돌연변이 → 코드 1:1 반영
- [ ] [Gimmick_List.md](../Design/Gimmick_List.md) — 80종 스펙 표 → GimmickData SO 1:1 반영
- [ ] [Game_Design_Spec.md §9](../Design/Game_Design_Spec.md) — 몬스터 기본 스탯 → WaveSpawner 계산 일치
- [ ] [Data_Schema.md §7](../Design/Data_Schema.md) — 도감 저항력 0~40% → GimmickBase 헬퍼 일치 (실 적용은 M6)
- [ ] [Physics_Parameters.md](../Design/Physics_Parameters.md) — 기믹 임펄스/속도/시간 페널티 일치
- [ ] [Implementation_Plan.md §마일스톤 5](Implementation_Plan.md) — M4 인계 항목(엘리트 투기장) 처리 표기

---

## 13. 후속 마일스톤 인계 사항

> 마일스톤 5 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤에 넘기는 항목. 후속 마일스톤 진입 시 `[인계: M5]` 표기로 cross-link하며, [Implementation_Plan.md](Implementation_Plan.md)의 해당 마일스톤 헤더에도 동일 표기를 명시한다.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 기믹 80종 **VFX·사운드** (텔레그래프 파티클, 폭발 이펙트, 환경음) | **마일스톤 8** | 폴리싱 단계. 마일스톤 5는 로직·콜라이더·디버그 색상까지만 |
| 2 | **도감 저항력 시스템 본격 구현** — 점성술사 시설 도입 시 0~40% 누적 적용 | **마일스톤 6** | 점성술사(AstrologerManager) 도입 시. M5는 `GimmickBase.GetEffectiveDurationSeconds()` 헬퍼와 SO `respectsResistance` 플래그 정의까지 |
| 3 | 노드맵 ⚠️ 돌연변이 아이콘 표시 + 진입 전 인지 UI | **마일스톤 7** | ActMapUI 본격 구현 시. M5는 `StageBlueprint.mutationId` 노출까지 |
| 4 | 휴식 노드 **3종 효과 메뉴 UI** + 소매품 구매 (용병단 창고 소모품) | **마일스톤 7 / 마일스톤 6** | UI는 M7, 소모품 인벤토리는 M6 MercenaryManager |
| 5 | 이벤트 노드 **보물 방** 보상 — 코어 조각·타로카드 (M5는 골드만 즉시 지급) | **마일스톤 6** | EconomyManager + TarotManager 도입 시 |
| 6 | 일일 도전 **랭킹 시스템** — 동일 시드 전체 유저 타임어택 경쟁 | **마일스톤 6** | 주점 의뢰 시스템(TavernManager) + 서버 백엔드. M5는 결정론적 시드만 |
| 7 | 히든 노드 출현 확률 +15% — 열기구 3단계 개조 | **마일스톤 6** | BalloonManager 도입 시. M5는 기본 5% 고정 |
| 8 | 엘리트 투기장 **입장 조건 검증** — 해당 액트 최종 보스 처치 여부 | **마일스톤 6** | TavernManager + PlayerData.clearedBossIds 도입 시 |
| 9 | 모디파이어 **시야 감소(블리자드)** 셰이더 — 화면 가장자리 어두워짐 | **마일스톤 8** | M5는 `CameraController.SetVisionReduction(0.4f)` API 스텁까지 |
| 10 | 모디파이어 **시간 왜곡** — 타이머 속도 변동 시각화 (UI 진동) | **마일스톤 7** | StageTimer.SetTimeScaleMultiplier API는 M5에서 정의, UI 표현은 M7 |
| 11 | 스테이지 모디파이어가 적용된 상태에서의 **밸런스 풀 테스트** (Lv.90 빌드 3종) | **마일스톤 8** | 최종 밸런싱 단계 |
| 12 | 절차 생성 결과의 **세이브/로드** (재진입 시 같은 stage 인스턴스 복원) | **마일스톤 7** | SaveSystem 도입 시. M5는 StageBlueprint 직렬화 가능 형태만 |
| 13 | 절차 생성 **시각화 디버그 툴** (에디터 윈도우에서 시드 입력 → 미리보기) | **별도 트랙** | 1인 개발 우선순위 낮음. 필요 시 마일스톤 8에 합류 |
| 14 | 일반 몬스터 20종 정식 비주얼·스프라이트·애니메이션 | **별도 아트 트랙** | `Implementation_Plan.md §확정 사항`의 플레이스홀더 방침 |
| 15 | 세그먼트 풀 확장 (테마당 상단 8~12 / 중단 15~20 정식 수량) | **마일스톤 8** | M5는 테마당 상단 2 + 중단 4 + 하단 1 = 28종 최소 풀로 출발 |

---

## 14. 참조 문서

- [Procedural_Stage_Gen.md](../Design/Procedural_Stage_Gen.md) — 10단계 플로우 / 세그먼트 / 예산 / 충돌 규칙 / 시너지 / 모디파이어 / 돌연변이 / 시드
- [Gimmick_List.md](../Design/Gimmick_List.md) — 80종 기믹 스펙 표 (공통 20 + 봄 15 + 여름 15 + 가을 15 + 겨울 15)
- [Game_Design_Spec.md](../Design/Game_Design_Spec.md) — §9 몬스터 기본 스탯, §3 점성술사·열기구, §6 노드 유형 비율
- [Data_Schema.md](../Design/Data_Schema.md) — §7 도감 저항력, §11 ballState
- [Physics_Parameters.md](../Design/Physics_Parameters.md) — 기믹 임펄스·시간 페널티 상수
- [Active_Skill_Judgment.md](../Design/Active_Skill_Judgment.md) — 기믹 스펙 표 양식 표준 (Gimmick_List.md가 차용)
- [Elite_Bounty_Spec.md](../Design/Elite_Bounty_Spec.md) — 4종 엘리트 투기장 설정
- [Boss_Patterns.md](../Design/Boss_Patterns.md) — 보스 러시 돌연변이 풀(중간보스 10, 20 BossId)
- [Implementation_Plan.md §마일스톤 5](Implementation_Plan.md) — 상위 로드맵
- [Milestone1_TODO.md](Milestone1_TODO.md) · [Milestone2_TODO.md](Milestone2_TODO.md) · [Milestone3_TODO.md](Milestone3_TODO.md) · [Milestone4_TODO.md](Milestone4_TODO.md) — 양식 표준 및 인계 출처
