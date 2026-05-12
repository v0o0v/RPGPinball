# RPG 핀볼 Unity 구현 계획서

> 13개 설계 문서를 기반으로 한 전체 구현 로드맵입니다.
> 총 **8개 마일스톤**, 예상 기간 약 16~20주.

---

## User Review Required

> [!IMPORTANT]
> 아래 구현 계획은 **1인 개발 기준**으로 작성되었습니다. 팀 규모에 따라 병렬 작업이 가능하므로 기간이 단축될 수 있습니다.

> [!WARNING]
> 아트 에셋(스프라이트, 이펙트, 사운드)은 이 계획에서 **플레이스홀더**로 처리합니다. 실제 아트 파이프라인은 별도 논의가 필요합니다.

## 확정 사항

| 항목            | 결정                                                                   |
| --------------- | ---------------------------------------------------------------------- |
| **Unity 버전**  | 6000.4.0f1 (Unity 6)                                                   |
| **추가 에셋**   | DOTween (트윈 애니메이션), UniTask (비동기 처리), ProCamera2D (카메라) |
| **CI/CD**       | 불필요 (수동 빌드)                                                     |
| **아트 리소스** | 플레이스홀더로 시작 → 이후 교체                                        |
| **데이터 보안** | 세이브 데이터 암호화 + 메모리 값 난독화 (안티치트)                     |

## 기술 스택

| 카테고리    | 기술                                            |
| ----------- | ----------------------------------------------- |
| 엔진        | Unity 6 (6000.4.0f1)                            |
| 물리        | Rigidbody2D + Collider2D (Continuous Detection) |
| 비동기      | UniTask (씬 로딩, 네트워크, 연출 시퀀스)        |
| 트윈        | DOTween (UI 애니메이션, 이펙트, 카메라 연출)    |
| 카메라      | ProCamera2D (공 추적, 줌, 바운딩)               |
| 저장        | AES-256 암호화 JSON + HMAC-SHA256 무결성 검증   |
| 메모리 보안 | 난독화 래퍼 타입 (SafeInt, SafeFloat)           |
| 플랫폼      | Android (세로 화면)                             |

---

## 프로젝트 아키텍처 개요

```
Assets/
├── 01.Scenes/          ← Village, ActMap, Stage, Title
├── 02.Scripts/
│   ├── Core/           ← GameManager, SaveSystem, EventBus
│   ├── Security/       ← SafeTypes, Encryption, IntegrityCheck
│   ├── Physics/        ← Ball, Flipper, Collision, DeadZone
│   ├── Combat/         ← DamageCalculator, Combo, Mana, Skill
│   ├── Enemy/          ← Monster, Boss, Elite, AI, Projectile
│   ├── Stage/          ← ProceduralGen, Segment, Gimmick, Wave
│   ├── Village/        ← Forge, Enchanter, Tavern, Astrologer, Training
│   ├── UI/             ← HUD, SkillDeck, Popup, Navigation
│   ├── Data/           ← ScriptableObjects, Enums, Constants
│   └── Meta/           ← Achievement, Quest, Login, Economy
├── 03.Sprites/
├── 04.Data/            ← SO 에셋 인스턴스
├── 05.Prefabs/
└── Resources/
```

---

## 마일스톤 1: 코어 핀볼 프로토타입 (2주)

> **목표**: 공이 튕기고 플리퍼가 소환되는 기본 핀볼 루프 완성

### Core 시스템

#### [NEW] `02.Scripts/Core/GameManager.cs`

- 싱글턴 게임 매니저. 씬 전환(UniTask 비동기 로딩), 게임 상태(Playing/Paused/Result) 관리
- `Time.timeScale` 제어 (일시정지, 타임 딜레이전)

#### [NEW] `02.Scripts/Core/EventBus.cs`

- 옵저버 패턴 이벤트 시스템: `OnBallHit`, `OnComboChange`, `OnManaChange`, `OnTimePenalty` 등

#### [NEW] `02.Scripts/Core/Constants.cs`

- `Physics_Parameters.md` 기반 모든 물리 상수 정의

### 🔒 보안 시스템 (안티치트)

#### [NEW] `02.Scripts/Security/SafeInt.cs`

- int 래퍼. 내부에서 XOR 난독화 + 랜덤 키 저장. 메모리 스캐너(GameGuardian 등) 방지
- `Value` 프로퍼티로 접근 시 자동 복호화/암호화

#### [NEW] `02.Scripts/Security/SafeFloat.cs`

- float 래퍼. SafeInt와 동일한 XOR 난독화 방식

#### [NEW] `02.Scripts/Security/SafeLong.cs`

- long 래퍼. 골드, XP 등 큰 수치용

#### [NEW] `02.Scripts/Security/IntegrityChecker.cs`

- 런타임 핵심 수치(HP, 골드, 레벨, SP, 마나, 타이머) 주기적 체크섬 검증
- 체크섬 불일치 시 → 경고 로그 + 강제 값 복원

#### [NEW] `02.Scripts/Security/SaveEncryption.cs`

- **AES-256-CBC** 암호화로 세이브 JSON 암호화
- **HMAC-SHA256** 서명으로 파일 변조 감지
- 디바이스 고유 키(SystemInfo.deviceUniqueIdentifier) + 앱 내장 Salt 조합
- 변조 감지 시 → 클라우드 백업에서 복원 또는 세이브 초기화

### 물리 시스템

#### [NEW] `02.Scripts/Physics/BallController.cs`

- Rigidbody2D + CircleCollider2D. 재질별 PhysicsMaterial2D 교체
- 최대/최소 속도 클램핑 (2.0~40.0 U/s)
- Continuous Collision Detection 적용
- 낙사 판정 → 리스폰 처리

#### [NEW] `02.Scripts/Physics/FlipperController.cs`

- 터치 입력 → 플리퍼 즉시 생성 (소환 애니메이션 0.08초)
- 스윙 구간(0~0.15초) vs 정적 구간(0.15~0.5초) 반발력 차등
- 쿨타임 시스템 (기본 1.5초, 하한 0.5초)
- 소환 불가 영역 체크 (보스 영역, 데드존, 벽)
- 블로킹 판정 (탄막 접촉 → 소멸 + 쿨타임 -0.3초)

#### [NEW] `02.Scripts/Physics/DeadZone.cs`

- Trigger2D로 낙사 판정. 시간 페널티(-10초) 이벤트 발행

#### [NEW] `02.Scripts/Physics/PlayfieldBuilder.cs`

- 9.0×12.0 Unit 플레이필드 생성, 벽/범퍼 배치

### 카메라

#### [MODIFY] ProCamera2D 설정

- 공 추적 (smoothTime 0.15초), 수직 오프셋 +2.0U
- 보스전 줌아웃 ×1.2, 멀티볼 줌아웃

### 검증

- 공 발사 → 벽 반사 → 플리퍼 타격 → 낙사/리스폰 루프 확인
- 4종 재질 PhysicsMaterial2D 프리셋 생성 후 교체 테스트

### 마일스톤 1 → 후속 인계 사항

> 마일스톤 1 회고형 검증([Milestone1_TODO.md](Milestone1_TODO.md))에서 식별된, 후속 마일스톤에 넘긴 항목.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 멀티볼 카메라 줌아웃 (+0.1/공) — `Constants.CameraMultiballZoomPerBall` | **마일스톤 3** | `MultiBallI` 스킬 실 구현(공 추가) 시점에 ProCamera2D 동적 줌 로직 추가 |
| 2 | 보스전 카메라 줌아웃 ×1.2 — `Constants.CameraBossZoom` | **마일스톤 4** | 보스 등장 트리거 도입 시 ProCamera2D `UpdateScreenSize` 호출로 동적 적용 |
| 3 | Flipper 탄막 블로킹용 트리거 자식 콜라이더 | **마일스톤 4** | 현재 BoxCollider2D `isTrigger=false`로 인해 `OnTriggerEnter2D(Projectile)` 분기 호출 불가. 탄막 도입 시 자식 트리거 콜라이더 분리 필요 |
| 4 | DeadZone 보스전 -20초 분기 — `Constants.BossDeadzonePenalty` | **마일스톤 4** | 보스 컨텍스트 도입 시 DeadZone에 분기 추가 (마일스톤 2 인계 표와 중복) |
| 5 | Camera Orthographic Size 재조정 (현재 임시값 9) | **마일스톤 7** | Android 세로 화면 비율(9:16~9:19.5)에서 플레이필드 전체가 한눈에 보이도록 조정 |
| 6 | SaveEncryption `AppSalt` 외부 주입 메커니즘 | **마일스톤 7** | 현재 하드코딩. 실 배포 빌드 파이프라인/서버에서 주입하는 구조로 전환 |

---

## 마일스톤 2: 전투 시스템 (2~3주)

> **목표**: 데미지 파이프라인, 콤보, 마나, 스킬 덱 완성

### 데미지 시스템

#### [NEW] `02.Scripts/Combat/DamageCalculator.cs`

- `Damage_Formula.md` 10단계 파이프라인 구현
- 합연산 → 곱연산 → 코어 → 재질 → 플리퍼 → 룬 → 타로 → 크리티컬 → DEF/MRes → 최종

#### [NEW] `02.Scripts/Combat/ComboSystem.cs`

- 몬스터/보스 타격 시 +1, 3초 내 미타격 시 리셋, 낙사 시 즉시 0
- 10/30콤보 마나 획득 배율 (1.5배/2.0배)
- UI 연동 (중앙 큰 숫자, 10/30/50/100콤보 이펙트)

#### [NEW] `02.Scripts/Combat/ManaSystem.cs`

- 최대치 100 고정. 충전 효율 = 레벨 기반
- 충전원: 벽 충돌(+3), 몬스터(+8), 보스(+15) × 충전 효율%

### 스킬 시스템

#### [NEW] `02.Scripts/Combat/SkillDeck.cs`

- 하단 4칸 스킬 UI. 선택 → 터치 → 발동 (딜레이 0.3초)
- 궁극기 1개 제한, 동일 스킬 중복 불가

#### [NEW] `02.Scripts/Combat/ActiveSkillBase.cs`

- 모든 액티브 스킬의 추상 베이스 클래스
- `Active_Skill_Judgment.md` 기반 판정 범위/히트/넉백 파라미터

#### [NEW] `02.Scripts/Combat/Skills/` (16개 스킬 스크립트)

- 제어 6종, 파괴 4종, 원소 6종 액티브/A전환 스킬 각각 구현

#### [NEW] `02.Scripts/Combat/KnockbackSystem.cs`

- 넉백 면역 등급(없음/저항/면역/절대면역), 궁극기 관통 처리

### 적 시스템

#### [NEW] `02.Scripts/Enemy/MonsterBase.cs`

- HP, DEF, MRes, 히트박스. Trigger2D로 공 타격 감지
- 처치 시 XP/골드/아이템 드랍 이벤트

#### [NEW] `02.Scripts/Enemy/ProjectileBase.cs`

- 보스/몬스터 탄막. 소형(0.15U)/대형(0.4U)/특수(0.6U)
- 데드존 관통 시 시간 페널티, 플리퍼 블로킹 처리

### 타이머

#### [NEW] `02.Scripts/Combat/StageTimer.cs`

- 기본 180초 카운트다운. 시간 회복 상한 60초/스테이지 추적
- 낙사 페널티(-10/-20초), 탄막 관통(-5초) 적용

### 검증

- 더미 몬스터 배치 → 공 타격 → 데미지 계산 → 콤보 → 마나 충전 → 스킬 발동 루프
- `Damage_Formula.md` 시뮬레이션 수치와 실제 수치 비교

### 마일스톤 2 → 후속 인계 사항

> 마일스톤 2 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤에 넘긴 항목 목록. 후속 마일스톤 섹션에 `[인계: M2]` 표기로 cross-link.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 스킬 14종 본 구현 (제어 5, 파괴 3, 원소 6) — 현재 스텁(Debug.Log만) | **마일스톤 3** | 스킬 트리·SP·레벨링과 함께 구현해야 효과 식 검증이 가능 |
| 2 | `DamageContext` 누락 필드 — 공 속도, 공 재질 enum, 콤보 수, 타격 직후 플래그 | **마일스톤 3** | 스킬 트리 패시브("관성 돌파 I 속도 비례 배율", "플리퍼 스매시" 등) 단계 [2]를 채울 때 같이 추가 |
| 3 | `DamageCalculator` 단계 [3] 코어 효과 — 가속/분열/크로노 코어 분기 | **마일스톤 3** | 코어 데이터 SO·대장간 시설과 함께 |
| 4 | `ManaSystem.ChargeEfficiency` 실제 적용 — 레벨/룬 기반 효율 갱신 | **마일스톤 3** | LevelSystem · 강화 시스템과 연계 |
| 5 | 미스릴 공 재질 ×1.15 **실 플레이 검증** | **마일스톤 3** | 단위 테스트 통과, 인게임 검증은 공 재질 교체 UI(대장간) 필요 |
| 6 | `ManaSystemTests.cs`, `StageTimerTests.cs` EditMode 테스트 추가 | **마일스톤 3** | 단위 테스트 가능하나 시간 우선순위로 보류 |
| 7 | 스킬 인게임 입력부 — 슬롯 선택 UI + 터치 좌표 캡처 | **마일스톤 7** | 정식 InGameHUD 작성 시 함께 구현. 현재는 `SkillDeck.Instance.Use()` 코드 호출로만 발동 가능 |
| 8 | `FlipperController` ↔ `SkillDeck` 입력 충돌 차단 플래그 | **마일스톤 7** | 위 7과 함께 |
| 9 | `OnComboMilestone(10/30/50/100)` 이벤트 훅 + 콤보 이펙트 연출 | **마일스톤 7** | 이펙트/사운드 시스템 도입 시 |
| 10 | `DamageCalculator` 단계 [5] 플리퍼 파생형 효과 (가시/연성/충격파) | **마일스톤 6** | 대장간 플리퍼 강화 시 |
| 11 | `DamageCalculator` 단계 [6] 룬 효과 | **마일스톤 6** | 마법 부여소 시 |
| 12 | `DamageCalculator` 단계 [7] 타로카드 효과 | **마일스톤 6** | 점성술사 시 |
| 13 | 보스전 낙사 -20초 페널티 분기 (`Constants.BossDeadzonePenalty` 정의됨) | **마일스톤 4** | 보스 컨텍스트 도입 시 DeadZone에 분기 추가 |
| 14 | `ProjectileBase` 풀링 전환 | **마일스톤 4** | 보스 탄막 양 증가 시점에 |
| 15 | `ProjectileBase` 공 접촉 시 강제 감속/넉백 | **마일스톤 4** | 보스 패턴 구현 시 |
| 16 | `DebugHud` → 정식 `InGameHUD` 교체 | **마일스톤 7** | UI 본격 구현 시 |
| 17 | "원소 폭주" 스킬 ID와 `Skill_Tree_Diagram.md` 정합 재확인 (현재 ID 35, Tier 6 궁극기로 설정) | **마일스톤 3** | 스킬 트리 정식 정의 시 검증 |

---

## 마일스톤 3: 스킬 트리 & 성장 시스템 (2주)

> **목표**: 60종 스킬 트리, 레벨링, SP 시스템 완성
>
> **[인계: M2]** 마일스톤 2에서 넘긴 항목 — 스킬 14종 본 구현, DamageContext 필드 보강(공 속도·재질·콤보·타격직후), DamageCalculator 단계 [3] 코어 효과, ManaSystem.ChargeEfficiency 적용, 미스릴 ×1.15 실 플레이 검증, ManaSystemTests/StageTimerTests, "원소 폭주" 스킬 트리 정합 재확인. (상세는 §마일스톤 2 → 후속 인계 사항 표)
>
> **[인계: M1]** 멀티볼 카메라 줌아웃 (+0.1/공, `Constants.CameraMultiballZoomPerBall`) — `MultiBallI` 스킬 실 구현 시점에 ProCamera2D 동적 줌 로직 추가.

### 데이터 구조

#### [NEW] `02.Scripts/Data/SkillData.cs` (ScriptableObject)

- 60종 스킬 정의: ID, 이름, Tier, MaxLv, 유형(P/A/A전환), 선행 조건, 공식 파라미터

#### [NEW] `02.Scripts/Data/PlayerData.cs` (ScriptableObject)

- `Data_Schema.md` 기반 플레이어 런타임 데이터 구조

### 스킬 트리 로직

#### [NEW] `02.Scripts/Combat/SkillTreeManager.cs`

- 3분기(제어/파괴/원소) × Tier 1~6 관리
- 해금 조건 검증 (선행 Lv.1+), SP 투자/회수
- 패시브 효과 합산 → DamageCalculator에 공급
- 합연산/곱연산/점감 연산/하드캡 규칙 적용

### 레벨 시스템

#### [NEW] `02.Scripts/Meta/LevelSystem.cs`

- `RequiredXP = 80 + (Lv * 12) + (Lv² * 0.5)` 공식
- 오버레벨링 페널티 (5레벨 초과 50%, 10레벨 초과 20%)
- 레벨업 시 SP +1, 스탯 자동 증가

### 검증

- Lv.1 → Lv.100 시뮬레이션, SP 144개 배분 테스트
- 3가지 빌드(제어/파괴/원소) 스킬 투자 후 DPS 검증

---

## 마일스톤 4: 보스 & 엘리트 AI (3주)

> **목표**: 12종 보스 + 4종 엘리트 몬스터 AI 구현
>
> **[인계: M2]** 보스전 낙사 -20초 분기(`DeadZone`), `ProjectileBase` 풀링 전환, 탄막 공 접촉 시 강제 감속/넉백.
>
> **[인계: M1]** 보스전 카메라 줌아웃 ×1.2(`Constants.CameraBossZoom`), Flipper 탄막 블로킹용 트리거 자식 콜라이더 분리.

### 보스 AI 프레임워크

#### [NEW] `02.Scripts/Enemy/BossAI/BossStateMachine.cs`

- 공통 행동 사이클: Idle → Telegraph → Execute → Recovery
- 페이즈 전환 (HP 기반), 분노 모드 (HP 30%)

#### [NEW] `02.Scripts/Enemy/BossAI/` (12개 보스 스크립트)

- `Boss_Patterns.md` 기반 각 보스별 패턴 구현
- Act 1: 식충식물, 타락한 요정, 세계수 수호정령
- Act 2: 무장 게, 유령 해적선장, 크라켄
- Act 3: 미치광이 발명가, 호박머리 유령, 태엽장치 드래곤
- Act 4: 서리거인, 시계탑 파수꾼, 겨울 여왕

#### [NEW] `02.Scripts/Enemy/BossAI/BulletPatterns/`

- 부채꼴, 나선형, 직선 연사, 회전, 유도 등 탄막 패턴 라이브러리

### 엘리트 AI

#### [NEW] `02.Scripts/Enemy/EliteAI/` (4개)

- `Elite_Bounty_Spec.md` 기반
- 폭풍의 정령, 심해 리바이어선, 황금 고블린 왕, 서리 파수꾼

### 검증

- Act 1 보스 3종 → 풀 플레이 테스트
- 겨울 여왕 Phase 3 DPS 레이스 밸런스 검증 (초당 311+ 확인)

---

## 마일스톤 5: 절차적 스테이지 생성 & 기믹 (3주)

> **목표**: 80종 기믹, 세그먼트 조합, 난이도 예산 시스템 완성

### 절차 생성 엔진

#### [NEW] `02.Scripts/Stage/ProceduralStageGenerator.cs`

- `Procedural_Stage_Gen.md` 10단계 생성 플로우 구현
- 시드 기반 재현 (`PlayerUID + 날짜 해시`)
- 난이도 예산 공식: `(100 + Stage×20) × ActMult ± 10%`

#### [NEW] `02.Scripts/Stage/SegmentPool.cs`

- 상단/중단/하단 세그먼트 프리셋 관리
- 중단 2~4개 조합, 연결 통로 보장

#### [NEW] `02.Scripts/Stage/DifficultyBudget.cs`

- 예산 소비 테이블, 기믹 가중치, 충돌 방지 규칙 6종, 시너지 조합 4종

### 기믹 시스템

#### [NEW] `02.Scripts/Stage/Gimmicks/GimmickBase.cs`

- 80종 기믹 추상 베이스. 버프/디버프/시련 분류

#### [NEW] `02.Scripts/Stage/Gimmicks/` (80개 기믹 스크립트)

- 공통 20종 + 봄 15종 + 여름 15종 + 가을 15종 + 겨울 15종

### 몬스터 웨이브

#### [NEW] `02.Scripts/Stage/WaveManager.cs`

- 웨이브 수 = `floor(Stage/5) + 1`
- 3패턴 (물량 러시/정예 소수/보스 호위) 랜덤 선택

### 스테이지 특성 (Modifier)

#### [NEW] `02.Scripts/Stage/StageModifier.cs`

- 공통 10종 + 테마 전용 8종 글로벌 특성
- 돌연변이 스테이지 5종 (5% 확률)

### 고정 이정표

#### [NEW] `02.Scripts/Stage/MilestoneManager.cs`

- 스테이지 10/20/30 보스 고정, 5/15/25 휴식·이벤트
- 이벤트 노드 4종 (여행자/보물방/제단/도박)

### 검증

- Act 1 전체 30스테이지 절차 생성 → 플레이 테스트
- 동일 시드 재현성 확인
- 충돌 방지 규칙 위반 0건 확인

---

## 마일스톤 6: 마을 시설 & 메타 시스템 (3주)

> **목표**: 6개 마을 시설 + 재화 + 의뢰 + 도감 + 타로카드
>
> **[인계: M2]** `DamageCalculator` 단계 [5] 플리퍼 파생형 효과(대장간), 단계 [6] 룬 효과(마법 부여소), 단계 [7] 타로카드 효과(점성술사).

### 마을 씬

#### [NEW] `01.Scenes/Village.unity`

- 하단 네비게이션 바 6탭 UI

### 대장간

#### [NEW] `02.Scripts/Village/ForgeManager.cs`

- 재질 변경 (4종), 코어 튜닝 (6종, Lv.1~5), 플리퍼 강화 (Lv.1~10)
- 플리퍼 파생형 선택 (Lv.4에서 가시/연성/충격파)

### 마법 부여소

#### [NEW] `02.Scripts/Village/EnchanterManager.cs`

- 룬 소켓 시스템 (9종 룬, 1~2칸)
- 룬 합성 (3개 → 상위 등급)

### 주점

#### [NEW] `02.Scripts/Village/TavernManager.cs`

- 일일 의뢰 3개, 주간 의뢰 1개, 현상금 사냥 3마리

### 점성술사

#### [NEW] `02.Scripts/Village/AstrologerManager.cs`

- 시련 도감 (80종 기믹, 저항력 5단계)
- 타로카드 뽑기 (38장, 4등급), 장착 (3슬롯)
- 영구 카드 승급 (10장 + 5,000골드)

### 기타 시설

#### [NEW] `02.Scripts/Village/BalloonManager.cs` — 열기구 3단계 개조

#### [NEW] `02.Scripts/Village/MercenaryManager.cs` — 소모품 제작 3종

#### [NEW] `02.Scripts/Village/TrainingManager.cs` — 스킬 트리 UI, 덱 세팅, 리셋, 보스 연습

### 재화 시스템

#### [NEW] `02.Scripts/Meta/EconomyManager.cs`

- 10종 재화 관리 (골드, 마나 결정, 보스의 영혼, 코어 조각, 특수 광석 등)

### 의뢰 시스템

#### [NEW] `02.Scripts/Meta/QuestManager.cs`

- 자정(UTC+9) 갱신, 주간 갱신

### 검증

- 재화 획득 → 소모 플로우 전체 테스트
- 골드 경제 밸런스 (Act 1 올클리어 시 8,000~10,000골드 검증)

---

## 마일스톤 7: UI/UX & 세이브/로드 (2주)

> **목표**: `UI_Flow.md` 기반 전체 화면 흐름 + 세이브 시스템
>
> **[인계: M2]** 스킬 인게임 입력부(슬롯 선택 UI + 터치 좌표 캡처), `FlipperController` ↔ `SkillDeck` 입력 충돌 차단 플래그, `OnComboMilestone(10/30/50/100)` 이벤트 훅 + 콤보 이펙트, `DebugHud` → 정식 `InGameHUD` 교체.
>
> **[인계: M1]** Camera Orthographic Size 재조정(현재 임시값 9 → Android 세로 비율 맞춤), `SaveEncryption.AppSalt` 외부 주입 메커니즘 (현재 하드코딩).

### 씬 구조

#### [NEW] `01.Scenes/Title.unity` — 타이틀 화면

#### [NEW] `01.Scenes/ActMap.unity` — 액트맵 (노드 맵)

#### [MODIFY] `01.Scenes/Stage1.unity` → 인게임 스테이지 템플릿

### 인게임 HUD

#### [NEW] `02.Scripts/UI/InGameHUD.cs`

- 상단: 일시정지/타이머/골드/등급
- 중앙: 콤보 카운터 + 이펙트
- 하단: 마나 게이지, 스킬 덱 4칸, 소모품 2칸

### 액트맵 UI

#### [NEW] `02.Scripts/UI/ActMapUI.cs`

- 노드 맵 30개, 분기/히든 노드, 등급 아이콘 표시
- 출격 준비 화면 (타로카드/소모품 교체)

### 팝업 시스템

#### [NEW] `02.Scripts/UI/PopupManager.cs`

- 확인/알림/보상/가이드/설정 5종 팝업

### 결과 화면

#### [NEW] `02.Scripts/UI/ResultScreen.cs`

- 클리어: 등급(S/A/B)/시간/콤보/보상 표시
- 실패: 이어하기(광고, 1일 3회)/재도전

### 세이브/로드

#### [NEW] `02.Scripts/Core/SaveSystem.cs`

- `Data_Schema.md` 기반 JSON 직렬화
- **SaveEncryption** 연동: AES-256 암호화 저장 + HMAC 무결성 검증
- 로컬 저장 + Google Play Games 클라우드 동기화 (클라우드는 변조 방지 원본)
- 자동 저장 (클리어/시설 이용/장비 변경)
- 로드 시 HMAC 검증 실패 → 클라우드 백업 자동 복원

### 일시정지

#### [NEW] `02.Scripts/Core/PauseManager.cs`

- `OnApplicationPause` 자동 일시정지, 물리 상태 직렬화

### 검증

- Title → Village → ActMap → Stage → Result 전체 흐름 테스트
- 세이브/로드 무결성 검증

---

## 마일스톤 8: 폴리싱 & 밸런스 (2주)

> **목표**: 튜토리얼, 업적, 로그인 보상, BM, 최종 밸런싱

### 온보딩

#### [NEW] `02.Scripts/Meta/TutorialManager.cs`

- Act 1 스테이지 1~3 강제 튜토리얼
- 마을 첫 방문 NPC 안내, 신규 기믹 팁 팝업

### 업적

#### [NEW] `02.Scripts/Meta/AchievementManager.cs`

- 전투 6종, 수집 4종, 도전 5종 업적 + 보상

### 로그인 보상

#### [NEW] `02.Scripts/Meta/DailyLoginManager.cs`

- 7일 주기, 자정 갱신, 연속 보너스

### 클리어 등급

#### [NEW] `02.Scripts/Meta/GradeSystem.cs`

- S(60%+)/A(30%+)/B(클리어)/C(이어하기) 등급 판정
- XP/골드 보너스 적용

### 수익화

#### [NEW] `02.Scripts/Meta/AdManager.cs`

- 이어하기 광고(1일 3회), 여행 상인 일일 무료 카드

#### [NEW] `02.Scripts/Meta/IAPManager.cs`

- 월간 패스, 시즌 패스 (전투력 직결 항목 절대 불가)

### 접근성

- 색맹 모드, 터치 감도, 화면 흔들림, 섬광 감소, 대형 UI

### 사운드

- 마을/액트맵/전투 BGM 슬롯, 재질별 충돌음, 스킬 속성별 효과음

### 최종 밸런스 검증

- 3빌드(파괴/제어/원소) × 겨울 여왕 DPS 검증
- 골드 경제 Act 1~4 시뮬레이션
- SP 배분 전략 4종 실전 테스트

---

## Verification Plan

### Automated Tests

- **단위 테스트**: DamageCalculator, ComboSystem, ManaSystem, LevelSystem 공식 검증
- **통합 테스트**: 절차적 생성 시드 재현성, 충돌 방지 규칙 위반 검사
- **밸런스 시뮬레이터**: Lv.90 3빌드 vs 겨울 여왕 DPS ≥ 311/초 자동 검증

### Manual Verification

- Act 1 풀 플레이 (마일스톤 5 이후)
- Act 1~4 전체 플레이 (마일스톤 7 이후)
- Android 실기기 터치 반응성 테스트
- 일시정지 → 백그라운드 → 복귀 시 상태 보존 확인

---

## 마일스톤별 일정 요약

| #   | 마일스톤             | 기간  | 누적 | 핵심 산출물           |
| --- | -------------------- | ----- | ---- | --------------------- |
| 1   | 코어 핀볼 프로토타입 | 2주   | 2주  | 공+플리퍼+물리 루프   |
| 2   | 전투 시스템          | 2~3주 | 5주  | 데미지+콤보+마나+스킬 |
| 3   | 스킬 트리 & 성장     | 2주   | 7주  | 60종 스킬+레벨+SP     |
| 4   | 보스 & 엘리트 AI     | 3주   | 10주 | 12보스+4엘리트        |
| 5   | 절차적 생성 & 기믹   | 3주   | 13주 | 80기믹+세그먼트+특성  |
| 6   | 마을 시설 & 메타     | 3주   | 16주 | 6시설+재화+의뢰+타로  |
| 7   | UI/UX & 세이브       | 2주   | 18주 | 전체 화면 흐름+저장   |
| 8   | 폴리싱 & 밸런스      | 2주   | 20주 | 튜토리얼+업적+BM+QA   |
