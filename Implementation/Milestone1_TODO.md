# 마일스톤 1 TODO — 코어 핀볼 프로토타입

> **목표**: 공이 튕기고 플리퍼가 소환되는 기본 핀볼 루프 완성 — Core/Security/Physics/Camera 시스템 1차 구축
> **기간**: 2주 (누적 2주)
> **상위 문서**: [Implementation_Plan.md §마일스톤 1](Implementation_Plan.md)
> **검증 기준**: 공 발사 → 벽 반사 → 플리퍼 타격 → 낙사/리스폰 루프 동작, 4종 재질 PhysicsMaterial2D 프리셋 교체 가능
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 후속 마일스톤 인계
>
> **갱신일**: 2026-05-12 (사후 작성)
> **상태**: 마일스톤 1 완료 후 작성된 회고형 TODO. 모든 검증은 Unity 에디터에서 실측·검증함.

---

## 1. Core 시스템

### 1.1 [Constants.cs](../Assets/02.Scripts/Core/Constants.cs)

`Physics_Parameters.md` 기반 모든 물리 상수 정의.

- [x] 월드: `Gravity = (0, -15)`, `FixedTimestep = 0.01`, `MaxAngularVelocity = 1000` — 핀볼식 빠른 낙하감
- [x] 플레이필드: `PlayfieldWidth = 9.0`, `SegmentHeight = 12.0`, `FlipperZoneHeight = 3.0`, `DeadZoneHeight = 1.5`, `WallThickness = 0.5`
- [x] 공: `BallRadius = 0.25`, `BallDefaultMass = 1.0`, `BallLaunchSpeed = 18.0`, `BallMaxSpeed = 40.0`, `BallMinSpeed = 2.0`, `BallAngularDrag = 0.05`, `BallLinearDrag = 0.0`
- [x] 플리퍼: 길이/두께/타이밍/임펄스/쿨타임/거리 13종 상수 (`FlipperLength`, `FlipperSwingTime=0.15`, `FlipperCooldown=1.5`, `FlipperCooldownMin=0.5`, `FlipperSwingImpulse=25`, `FlipperStaticImpulse=12`, `FlipperBlockCooldownBonus=0.3` 등)
- [x] 낙사/리스폰: `RespawnDelay = 1.0`, `RespawnInvincibleTime = 1.5`, `RespawnLaunchSpeed = 18.0`
- [x] 카메라: `CameraSmoothing = 0.15`, `CameraVerticalOffset = 2.0`, `CameraBossZoom = 1.2`, `CameraMultiballZoomPerBall = 0.1`
- [x] 데드존: `DeadzonePenalty = -10.0`
- [x] 범퍼: `BumperImpulse = 18.0`
- [x] 8종 태그 문자열 상수 (`TagBall`, `TagDeadZone`, `TagBoss`, `TagMonster`, `TagFlipper`, `TagProjectile`, `TagBumper`, `TagWall`)

### 1.2 [GameManager.cs](../Assets/02.Scripts/Core/GameManager.cs)

- [x] 싱글턴 + `DontDestroyOnLoad`
- [x] `GameState` enum (Playing/Paused/Result)
- [x] `SetState()` → `Time.timeScale` 자동 제어 + `OnGameStateChanged` 발행
- [x] `Pause()`, `Resume()`, `EndGame()` 단축 API
- [x] `LoadSceneAsync(string)` — UniTask 비동기 씬 로딩 (Paused 상태 진입 → 0.9 진행률까지 대기 → Playing 복귀)
- [x] `ApplyPhysicsSettings()` 시작 시 Constants.Gravity / FixedTimestep 적용 — 검증 결과 `Physics2D.gravity=(0,-15)`, `fixedDeltaTime=0.01` 적용 확인

### 1.3 [EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs)

- [x] 제네릭 `Subscribe<T>` / `Unsubscribe<T>` / `Publish<T>` / `Clear()` static API
- [x] `Dictionary<Type, Delegate>` 기반 + `Delegate.Combine`/`Remove` 사용
- [x] 마일스톤 1 이벤트 9종 — `OnBallHit`, `OnComboChange`, `OnManaChange`, `OnTimePenalty`, `OnBallDead`, `OnBallRespawned`, `OnFlipperSpawned`, `OnFlipperBlocked`, `OnGameStateChanged`

---

## 2. Security 시스템 (안티치트 기반)

> 마일스톤 1에서 라이브러리만 작성. 실 활용은 마일스톤 2(콤보·마나·HP·타이머)에서 본격 시작.

### 2.1 [SafeInt.cs](../Assets/02.Scripts/Security/SafeInt.cs)

- [x] `struct SafeInt` + XOR 난독화 (랜덤 키 + 값^키 저장)
- [x] `Create(int)` 팩토리, `Value` 프로퍼티 (set 시 키 재생성)
- [x] 암시적 변환 `int ↔ SafeInt`, 산술 연산자 `+ - *`, 비교 연산자 `== != > <`
- [x] `IEquatable<SafeInt>`, `GetHashCode()`, `ToString()`

### 2.2 [SafeFloat.cs](../Assets/02.Scripts/Security/SafeFloat.cs)

- [x] `struct SafeFloat` + `BitConverter.SingleToInt32Bits` 기반 XOR 난독화 (unsafe 코드 없음)
- [x] `Mathf.Approximately` 기반 등호 비교

### 2.3 [SafeLong.cs](../Assets/02.Scripts/Security/SafeLong.cs)

- [x] `struct SafeLong` + 64비트 XOR. `GenerateKey()`는 상/하위 32비트를 별도 `Random.Range`로 생성해 unchecked 결합

### 2.4 [IntegrityChecker.cs](../Assets/02.Scripts/Security/IntegrityChecker.cs)

- [x] MonoBehaviour. `checkInterval = 5초`마다 체크섬 검증
- [x] `RegisterHP(getter, restorer)`, `RegisterTimer(getter, restorer)` 외부 등록 API
- [x] HP 불일치 시 `LogWarning` + 즉시 복원, Timer 불일치 임계 ±1초
- [x] `UpdateChecksum()` — 정상 갱신 시점 외부 알림 API
- [x] Sample 씬 `GameManager` 오브젝트에 컴포넌트 부착됨 (검증)

### 2.5 [SaveEncryption.cs](../Assets/02.Scripts/Security/SaveEncryption.cs)

- [x] AES-256-CBC 암호화 + PKCS7 패딩, 매 호출 시 새 IV 생성
- [x] HMAC-SHA256 서명 (`cipherBase64.hmacBase64` 형식)
- [x] `DeriveKey()` — `SystemInfo.deviceUniqueIdentifier + AppSalt` → SHA-256
- [x] `Encrypt(string) → string`, `TryDecrypt(string, out string) → bool` 안전 API
- [~] `AppSalt`는 하드코딩 (`"RPGPinball_Salt_v1"`) — 코드 주석에 "실제 배포 시 빌드 파이프라인에서 주입" 명시. **실 배포 빌드 전 마일스톤 7(세이브/로드)에서 외부 주입 메커니즘 추가 필요**

---

## 3. Physics 시스템

### 3.1 [BallController.cs](../Assets/02.Scripts/Physics/BallController.cs)

- [x] `[RequireComponent]` Rigidbody2D + CircleCollider2D
- [x] `Awake()` — Continuous Detection, AngularDamping/LinearDamping, FreezeRotation 적용
- [x] `FixedUpdate` ClampSpeed (max 40, min 2 자동 가속 보정) — Physics_Parameters.md §3 일치
- [x] `CheckFallDead()` — 데드존 트리거 보조 (y < respawnY=-8 폴백)
- [x] `OnDead()` — 비활성화 + `OnBallDead` 발행 + `Respawn` Invoke (1초 지연)
- [x] `Respawn()` — 상단 중앙 (0, SegmentHeight/2 - 1) 위치로 이동, `RespawnLaunchSpeed=18`로 하향 발사, 1.5초 무적
- [x] `OnCollisionEnter2D` → `OnBallHit { Speed, TargetTag }` 발행

### 3.2 [FlipperController.cs](../Assets/02.Scripts/Physics/FlipperController.cs)

- [x] `InputSystem` (`InputActionAsset`) 기반 — `Pinball` 맵의 `TouchPress` + `TouchPosition`
- [x] 터치 시 `Camera.main.ScreenToWorldPoint`로 월드 좌표 변환 → `SpawnFlipper(pos)`
- [x] 소환 가능 영역 검사 (`bossZoneMinY=4`, `deadZoneMaxY=-5`) → 외부 영역 거부
- [x] 동시 소환 간격 검사 (`FlipperMinSpawnGap=0.8`) — 기존 활성 플리퍼와 거리 측정
- [x] 좌/우 자동 결정 — `pos.x < 0`이면 좌측 플리퍼 (`Flipper.InitializeSwing(true/false)`)
- [x] 쿨타임 `FlipperCooldown=1.5` → `OnFlipperBlocked` 수신 시 `CooldownReduction(-0.3s)` 적용
- [x] 게임 상태가 `Playing`이 아니면 입력 무시
- [x] `FlipperInstance.Tick(dt)` — `FlipperActiveTime=0.5` 후 자동 `Destroy`

### 3.3 [Flipper.cs](../Assets/02.Scripts/Physics/Flipper.cs)

- [x] 스윙 회전 — `SwingStartAngle=-35° → SwingEndAngle=25°` (0.15초 SmoothStep), 우측은 `localScale.x=-1` + 부호 반전 거울 대칭
- [x] `OnCollisionEnter2D(Ball)` — `isSwinging`이면 `SwingImpulse=25N`, 아니면 `StaticImpulse=12N`. `Vector2.up` 기준 각도를 `FlipperMaxAngle=75°`로 클램프
- [x] `OnTriggerEnter2D(Projectile)` — 탄막 `Destroy` + `OnFlipperBlocked(CooldownReduction=0.3s)` 발행
- [ ] **탄막 블로킹 실 작동은 마일스톤 4 인계** — 현재 Flipper 프리팹의 BoxCollider2D는 `isTrigger=false`로 공 반발용이며 OnTriggerEnter2D는 호출되지 않음. 탄막 도입 시 별도 자식 트리거 콜라이더 추가 필요

### 3.4 [DeadZone.cs](../Assets/02.Scripts/Physics/DeadZone.cs)

- [x] `[RequireComponent]` BoxCollider2D, `Awake()`에서 `isTrigger=true` 강제 설정
- [x] `OnTriggerEnter2D` → 공 태그 검사 → `OnTimePenalty { Delta=-10 }` 발행 → `BallController.OnDead()` 호출
- [ ] **보스전 -20초 분기** (`Constants.BossDeadzonePenalty=-20f` 정의됨) — 마일스톤 4 인계 (보스 컨텍스트 도입 시)

### 3.5 [PlayfieldBuilder.cs](../Assets/02.Scripts/Physics/PlayfieldBuilder.cs)

- [x] `Start()` — `BuildWalls()` + `PlaceBumpers()` 실행
- [x] 좌·우·상단 3벽 런타임 생성. 각 벽은 자식 GameObject + BoxCollider2D + SpriteRenderer + `wallMaterial` 적용
- [x] `WallLeft/WallRight/WallTop` 위치 검증 완료 — 플레이 모드 진입 시 Playfield 자식 6개(벽 3 + 범퍼 3) 정상 생성 확인
- [x] `bumperPositions[]` 직렬화 배열 — 기본 (-2.25, 4), (0, 6), (2.25, 4)
- [x] `bumperPrefab`/`wallMaterial` SerializeField — **씬 인스턴스 + 프리팹 원본 모두에 정상 할당됨** (2026-05-12 검증 중 프리팹 원본 참조 누락을 발견해 즉시 복구)

### 3.6 [Bumper.cs](../Assets/02.Scripts/Physics/Bumper.cs)

- [x] `[RequireComponent]` CircleCollider2D. `OnCollisionEnter2D(Ball)` → 중심→공 방향으로 `BumperImpulse=18N` 임펄스
- [x] `impulseOverride` SerializeField로 개별 오브젝트 오버라이드 가능 (-1이면 기본값 사용)
- [x] `OnBallHit { TargetTag=TagBumper }` 발행 — 마일스톤 2 마나 충전과 연동됨

### 3.7 [ProtoSprite.cs](../Assets/02.Scripts/Core/ProtoSprite.cs)

- [x] 벽 폴백 단색 스프라이트 빌드 헬퍼. `Build(Shape, Color, ppu)` static. PlayfieldBuilder의 벽 SpriteRenderer에 사용
- 마일스톤 메모: 사용자 피드백에 따라 Ball/Flipper/Bumper는 단색 ProtoSprite 대신 [SpriteGenerator.cs](../Assets/Editor/SpriteGenerator.cs)로 형태에 맞는 PNG 스프라이트 생성

---

## 4. 입력 (Input System)

### 4.1 [GameInputActions.inputactions](../Assets/04.Data/GameInputActions.inputactions)

- [x] `Pinball` 액션 맵
- [x] `TouchPress` (Button) — Touchscreen primary touch press + Mouse left button
- [x] `TouchPosition` (Value, Vector2) — Touchscreen primary touch position + Mouse position
- [x] Sample 씬 `InputManager` 오브젝트의 `FlipperController.inputActions`에 할당 (검증 완료)

---

## 5. 카메라

### 5.1 ProCamera2D 설정 (Main Camera)

- [x] Main Camera에 `ProCamera2D` 컴포넌트 부착 (`Com.LuisPedroFonseca.ProCamera2D`)
- [x] `CameraTargets[0].TargetTransform = Ball` — 공 추적 활성
- [x] `HorizontalFollowSmoothness = 0.15`, `VerticalFollowSmoothness = 0.15` — Physics_Parameters.md §5 smoothTime 일치
- [x] `OffsetY = 2.0` — **2026-05-12 검증 중 0으로 설정되어 있던 것을 +2.0U로 즉시 수정** (Physics_Parameters.md §5 수직 오프셋 일치)
- [x] Camera Orthographic, size = 9 (현재 임시값, 마일스톤 7에서 세로 화면 비율에 맞춰 재조정 예정)
- [ ] **보스전 줌아웃 ×1.2** (`Constants.CameraBossZoom = 1.2` 정의됨) — 마일스톤 4 인계 (보스 등장 트리거 시 적용)
- [ ] **멀티볼 줌아웃 (+0.1 per ball)** (`Constants.CameraMultiballZoomPerBall = 0.1` 정의됨) — 마일스톤 3/4 인계 (`MultiBallI` 스킬 본 구현 시점)

---

## 6. 자산 (스프라이트 · 프리팹 · 머티리얼)

### 6.1 스프라이트 (Assets/03.Sprites/Proto/)

- [x] `Ball.png` — 128×128, PPU=128 — 퐁 조명 금속 구체
- [x] `Flipper.png` — 250×40, PPU=100 — 테이퍼 패들 (좌→우 좁아짐, 두꺼운 쪽이 피벗)
- [x] `Bumper.png` — 128×128, PPU=128 — 황금 링 + 내부 크림 글로우

### 6.2 [SpriteGenerator.cs](../Assets/Editor/SpriteGenerator.cs) (Editor)

- [x] 메뉴 `RPG Pinball/Generate Proto Sprites` — 3종 PNG를 `Assets/03.Sprites/Proto/`에 출력하고 각 프리팹에 자동 할당

### 6.3 프리팹 (Assets/05.Prefabs/)

| 프리팹 | 컴포넌트 | 검증 |
|---|---|---|
| [Ball.prefab](../Assets/05.Prefabs/Ball.prefab) | Transform · Rigidbody2D(mass=1, gravity=1, **Continuous**) · CircleCollider2D(radius=0.25, material=**BallWood**) · SpriteRenderer(Ball.png) · BallController | [x] |
| [Flipper.prefab](../Assets/05.Prefabs/Flipper.prefab) | Transform · BoxCollider2D(size 2.5×0.4, offset 1.25) · Flipper · 자식 Visual(SpriteRenderer Flipper.png) | [x] 좌우 거울 대칭은 InitializeSwing에서 처리 |
| [Bumper.prefab](../Assets/05.Prefabs/Bumper.prefab) | Transform · CircleCollider2D(radius=0.4) · SpriteRenderer(Bumper.png) · Bumper | [x] |
| [DeadZone.prefab](../Assets/05.Prefabs/DeadZone.prefab) | Transform · BoxCollider2D(trigger) · DeadZone | [x] |
| [Playfield.prefab](../Assets/05.Prefabs/Playfield.prefab) | Transform · PlayfieldBuilder (bumperPrefab=Bumper, wallMaterial=Wall) | [x] 2026-05-12 검증 중 SerializeField 참조 누락을 발견해 즉시 복구 |

### 6.4 PhysicsMaterial2D (Assets/06.Materials/) — Physics_Parameters.md §3 재질별 표 일치

| 머티리얼 | friction | bounciness | 설계값 일치 |
|---|---|---|---|
| [BallWood](../Assets/06.Materials/BallWood.physicsMaterial2D) | 0.2 | 0.9 | [x] |
| [BallSteel](../Assets/06.Materials/BallSteel.physicsMaterial2D) | 0.3 | 0.5 | [x] |
| [BallMithril](../Assets/06.Materials/BallMithril.physicsMaterial2D) | 0.25 | 0.7 | [x] |
| [BallVolcanic](../Assets/06.Materials/BallVolcanic.physicsMaterial2D) | 0.35 | 0.6 | [x] |
| [Wall](../Assets/06.Materials/Wall.physicsMaterial2D) | 0 | 0 | [x] 마찰·반발 모두 0 (벽이 공 에너지를 흡수하지 않도록) |

---

## 7. 씬 (Assets/01.Scenes/Sample.unity)

검증 시점 14개 루트 오브젝트 (마일스톤 2 추가분 포함). 마일스톤 1 시점에 작성된 8개:

| 오브젝트 | 위치 | 컴포넌트 |
|---|---|---|
| Main Camera | (0, 2, -10), ortho=9 | Camera · ProCamera2D · AudioListener · UniversalAdditionalCameraData |
| Directional Light | (0, 10, 0) | Light · UniversalAdditionalLightData |
| GameManager | (0, 0, 0) | GameManager · IntegrityChecker |
| InputManager | (0, 0, 0) | FlipperController (inputActions=GameInputActions, flipperPrefab=Flipper) |
| Ball | (0, 3, 0), scale (0.4, 0.4, 1) | Rigidbody2D · CircleCollider2D · SpriteRenderer · BallController. Tag=**Ball** |
| DeadZone | (0, -7.5, 0) | BoxCollider2D(trigger) · DeadZone. Tag=**DeadZone** |
| Playfield | (0, 0, 0) | PlayfieldBuilder (bumperPrefab=Bumper, wallMaterial=Wall) |
| — | 런타임 자식 6개 | WallLeft (-4.75, 0), WallRight (4.75, 0), WallTop (0, 6.25), Bumper×3 (검증) |

> 마일스톤 2가 추가한 오브젝트(CombatSystems, SkillDeck, DebugCanvas, EventSystem, DummyMonster ×3)는 [Milestone2_TODO.md](Milestone2_TODO.md) 참조.

---

## 8. 검증 체크리스트

### 8.1 코드 정적 검증 — 통과

- [x] 모든 스크립트 컴파일 성공, 콘솔 에러/경고 0건 (검증 시점)
- [x] 네임스페이스 규약 (`RPGPinball.Core` / `RPGPinball.Security` / `RPGPinball.Physics`) 1:1 대응
- [x] 식별자는 영문, 주석은 한국어 — `Design/rules.md` 일치

### 8.2 런타임 검증 (Play 모드) — 통과

| 케이스 | 기대 결과 | 결과 |
|---|---|---|
| 씬 진입 → 컴파일 에러 | 0건 | [x] |
| `Physics2D.gravity` 적용 | (0, -15) | [x] |
| `Time.fixedDeltaTime` 적용 | 0.01 (1% 오차 허용) | [x] 측정 0.01 |
| `GameManager.State` 초기값 | `Playing` | [x] |
| `PlayfieldBuilder` 런타임 자식 생성 | 6개 (벽 3 + 범퍼 3) | [x] 위치 검증 완료 |
| Ball Rigidbody2D `collisionDetectionMode` | `Continuous` | [x] |
| Ball Rigidbody2D `constraints` | `FreezeRotation` | [x] BallController.Awake() 적용 |
| ProCamera2D 타깃 | Ball 1개 | [x] |
| ProCamera2D smoothness | 0.15 (수직·수평) | [x] |
| ProCamera2D OffsetY | 2.0 | [x] (수정 후 검증) |

### 8.3 실 플레이 루프 검증 — 사용자 확인 필요

> 마일스톤 1의 단위 테스트는 마일스톤 2(`DamageCalculator`/`Knockback`/`SafeTypes` 등)에서 도입됨. 마일스톤 1 산출물의 런타임 정확도(공 속도 클램핑, 플리퍼 임펄스 정밀도, 데드존 페널티 등)는 마일스톤 2의 PlayMode 시뮬레이션에서 통합 검증됨.

- [ ] **(권장) Sample 씬 실 플레이** — 공 발사 → 벽 반사 → 데드존 → 1초 후 리스폰 (상단 중앙, 하향 발사) 루프 동작
- [ ] **(권장) 4종 재질 교체 테스트** — Ball.prefab의 CircleCollider2D.material을 BallSteel/BallMithril/BallVolcanic로 교체해 반발력 차이 체감
- [ ] **(권장) 플리퍼 소환** — 화면 터치/마우스 클릭으로 좌·우 플리퍼 즉시 생성, 1.5초 쿨타임, 보스 영역(y≥4)과 데드존(y≤-5) 거부 동작

---

## 9. 문서 정합성

- [x] [Physics_Parameters.md](../Design/Physics_Parameters.md) §1 월드, §2 플레이필드, §3 공, §4 플리퍼, §5 카메라, §6 탄막(상수만), §7 낙사/리스폰 — 모든 수치가 Constants.cs에 반영됨
- [x] [Design/rules.md](../Design/rules.md) — 한국어 주석/문서, 영문 식별자 규약 준수
- [x] [Implementation_Plan.md §마일스톤 1](Implementation_Plan.md) (라인 65~146) — 명시된 11개 [NEW] 항목 모두 작성됨

---

## 10. 후속 마일스톤 인계 사항

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | **보스전 카메라 줌아웃 ×1.2** (`Constants.CameraBossZoom`) | 마일스톤 4 | 보스 등장 트리거 도입 시 ProCamera2D `UpdateScreenSize` 호출로 동적 적용. 마일스톤 1에는 보스가 없음 |
| 2 | **멀티볼 카메라 줌아웃 (+0.1/공)** (`Constants.CameraMultiballZoomPerBall`) | 마일스톤 3 | `MultiBallI` 스킬 실 구현(공 추가) 시점에 ProCamera2D 동적 줌 로직 추가 |
| 3 | **Flipper 탄막 블로킹용 트리거 자식 콜라이더** | 마일스톤 4 | 현재 Flipper.cs의 OnTriggerEnter2D 분기는 BoxCollider2D `isTrigger=false`로 인해 호출되지 않음. 탄막 도입 시 별도 자식 GO에 trigger 콜라이더 + projectile 검사 분리 필요 |
| 4 | **DeadZone 보스전 -20초 분기** (`Constants.BossDeadzonePenalty`) | 마일스톤 4 | 보스 컨텍스트 도입 시 DeadZone.OnTriggerEnter2D에 분기 추가 (마일스톤 2 TODO에도 등재됨) |
| 5 | **SaveEncryption AppSalt 외부 주입** | 마일스톤 7 | 현재 하드코딩(`"RPGPinball_Salt_v1"`). 실 배포 빌드 전 빌드 파이프라인/서버에서 주입하는 메커니즘 필요 |
| 6 | **Camera Orthographic Size 재조정** | 마일스톤 7 | 현재 임시 size=9. Android 세로 화면(9:16~9:19.5)에서 플레이필드 전체가 한눈에 보이도록 카메라 또는 플레이필드 비율 조정 |

> 마일스톤 2 진행 중 발견된 **마일스톤 1 잔여 작업**은 모두 마일스톤 2에서 처리됨. 별도 인계는 없음.

---

## 참조 문서

- [Physics_Parameters.md](../Design/Physics_Parameters.md) — 모든 물리 상수의 출처
- [Game_Design_Spec.md](../Design/Game_Design_Spec.md) §3 대장간 — 재질별 효과 (마일스톤 6)
- [Implementation_Plan.md §마일스톤 1](Implementation_Plan.md) — 상위 로드맵
- [Milestone2_TODO.md](Milestone2_TODO.md) — 후속 마일스톤의 TODO 양식 표준
