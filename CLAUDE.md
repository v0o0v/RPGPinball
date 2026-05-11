# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

**RPG 핀볼 (가칭)** — Unity 6 (6000.4.0f1)로 개발 중인 Android(세로 화면) RPG + 로그라이크 + 핀볼 액션 게임. 터치로 플리퍼를 즉석 소환해 보스를 토벌하는 타임어택 구조.

현재 상태: **마일스톤 1(코어 핀볼 프로토타입)** 초기 구현 단계. `Implementation/Implementation_Plan.md`의 8개 마일스톤 중 거의 첫 마일스톤만 부분적으로 구현되어 있음. 대부분의 시스템(보안/저장, 전투 파이프라인, 스킬 트리, 보스 AI, 절차적 생성, 마을 시설)은 **아직 미구현**이며 설계 문서에만 존재.

## 핵심 디렉토리 구조

```
Assets/
├── 01.Scenes/              현재 Sample.unity 하나만 존재 (Title/Village/ActMap/Stage는 미생성)
├── 02.Scripts/             게임 코드. 현재 Core / Pinball / RPG 3개 폴더만 존재
├── 03.Sprites/, 03.Textures/, 04.Data/, 05.Prefabs/, 06.Materials/
├── 50. External Assets/    ProCamera2D, kenney_toy-brick-pack 등 서드파티 임포트
├── Plugins/Demigiant/      DOTween + DOTween Pro (Asset Store 임포트, UPM 아님)
├── Editor/, Resources/, Settings/, TextMesh Pro/, UI Toolkit/
Design/                     13개 한국어 게임 설계 문서 (소스 오브 트루스)
Implementation/             Implementation_Plan.md — 8개 마일스톤 로드맵
GeneratedAssets/             gitignore됨 (AI 생성 임시 에셋용)
```

## 코드 아키텍처

### 네임스페이스 규약

`Assets/02.Scripts/<폴더>/` ↔ `RPGPinball.<폴더>` 네임스페이스가 1:1 대응. 새 스크립트 작성 시 이 규약을 따를 것.
- `RPGPinball.Core` — [GameManager](Assets/02.Scripts/Core/GameManager.cs), [CameraController](Assets/02.Scripts/Core/CameraController.cs), [ScoreDisplay](Assets/02.Scripts/Core/ScoreDisplay.cs)
- `RPGPinball.Pinball` — [BallController](Assets/02.Scripts/Pinball/BallController.cs), [FlipperController](Assets/02.Scripts/Pinball/FlipperController.cs), [FlipperData](Assets/02.Scripts/Pinball/FlipperData.cs), [Bumper](Assets/02.Scripts/Pinball/Bumper.cs), [DeadZone](Assets/02.Scripts/Pinball/DeadZone.cs)
- `RPGPinball.RPG` — [PlayerStats](Assets/02.Scripts/RPG/PlayerStats.cs), [Enemy](Assets/02.Scripts/RPG/Enemy.cs), [DamageSystem](Assets/02.Scripts/RPG/DamageSystem.cs)

`Implementation_Plan.md`는 추후 `Security/`, `Combat/`, `Enemy/`, `Stage/`, `Village/`, `UI/`, `Data/`, `Meta/` 폴더를 추가하도록 명시 — 새 시스템 작업 시 계획서의 명명을 따를 것.

### 통신 패턴

- **GameManager 싱글턴**: `GameManager.Instance` + `DontDestroyOnLoad`. 점수 등 글로벌 상태와 `Action<int> OnScoreChanged` 같은 C# 이벤트 노출. 다른 컴포넌트는 이 이벤트를 구독해 UI 갱신. ([Bumper.cs:24-27](Assets/02.Scripts/Pinball/Bumper.cs:24)에서 점수 가산, [ScoreDisplay.cs](Assets/02.Scripts/Core/ScoreDisplay.cs)에서 구독 예시)
- 계획서상 **EventBus**(옵저버 패턴)가 추후 추가될 예정. 새 글로벌 이벤트는 EventBus가 생기면 그쪽으로 이전 가능하도록 작성.
- **태그 기반 충돌**: 공은 Unity 태그 `"Ball"`로 식별. 충돌 처리 시 `collision.gameObject.CompareTag("Ball")` 패턴 사용.
- **ScriptableObject 데이터**: 플리퍼 등 파라미터는 SO로 분리 ([FlipperData.cs](Assets/02.Scripts/Pinball/FlipperData.cs), `CreateAssetMenu` 경로 = `RPG Pinball/...`). 새 데이터 정의 시 동일한 메뉴 루트 유지.

### 물리 / 입력

- 2D 물리만 사용: `Rigidbody2D`, `Collider2D`, `HingeJoint2D` 모터로 플리퍼 구동.
- 공 최대 속도 클램핑은 [BallController.FixedUpdate](Assets/02.Scripts/Pinball/BallController.cs:16)에서. 모든 물리 상수의 최종 출처는 `Design/Physics_Parameters.md` — 매직 넘버를 추가할 때 이 문서와 일치하는지 확인.
- **Unity InputSystem** 사용 (`com.unity.inputsystem` 1.19.0). `InputAction`을 `[SerializeField]`로 인스펙터 노출하는 패턴 ([FlipperController.cs:9](Assets/02.Scripts/Pinball/FlipperController.cs:9)). 레거시 `Input.GetKey` 사용 금지.

### 카메라

`ProCamera2D` 에셋(서드파티) 사용. [CameraController](Assets/02.Scripts/Core/CameraController.cs)는 씬에 ProCamera2D가 없으면 경고만 남기고 종료 — Sample 씬 외 작업 시 ProCamera2D 컴포넌트가 메인 카메라에 붙어 있는지 확인.

## 설계 문서가 곧 사양서

`Design/`의 한국어 문서들이 구현보다 훨씬 앞서 있으며 **소스 오브 트루스**임. 시스템 작업 시 이 문서를 먼저 읽을 것:

| 시스템 | 참조 문서 |
| --- | --- |
| 전체 기획 | `Design/Game_Design_Spec.md` |
| 데미지 10단계 공식 | `Design/Damage_Formula.md` |
| 물리 상수 (속도, 쿨타임, 페널티) | `Design/Physics_Parameters.md` |
| 세이브 데이터 스키마 | `Design/Data_Schema.md` |
| 60종 스킬 트리 / 공식 | `Design/Skill_Tree_Diagram.md`, `Design/Skill_Tree_Formulas.md` |
| 보스 패턴 12종 | `Design/Boss_Patterns.md` |
| 80종 기믹 | `Design/Gimmick_List.md` |
| 절차적 스테이지 생성 | `Design/Procedural_Stage_Gen.md` |
| 마일스톤별 구현 순서 / 파일 명세 | `Implementation/Implementation_Plan.md` |

## 빌드 / 실행 / 테스트

Unity 프로젝트이며 별도 CLI 빌드 스크립트 없음:

- **에디터 실행**: Unity Hub에서 6000.4.0f1로 프로젝트를 열고 `Assets/01.Scenes/Sample.unity` 재생.
- **빌드**: Unity 에디터 `File → Build Profiles → Android` 사용. CI/CD 없음(`Implementation_Plan.md`에서 수동 빌드로 명시).
- **테스트**: `com.unity.test-framework` 1.6.0이 매니페스트에 있으나 현재 작성된 테스트 코드 없음. 추가 시 Unity Test Runner(Window → General → Test Runner)에서 EditMode/PlayMode 실행.
- **`.csproj` / `.sln`**: Unity가 자동 생성하므로 `.gitignore`되어 있음(`*.csproj`, `*.sln`). 워킹트리에 보이더라도 커밋하지 말 것.

## 의존성 메모

- **`Packages/manifest.json`** (UPM): Unity 2D 풀세트, Universal RP 17.4.0, Input System 1.19.0, Unity Purchasing(IAP), Visual Scripting, AI Inference/Assistant. **UniTask**는 `com.cysharp.unitask` Git URL로 등록(`Cysharp/UniTask` 마스터).
- **`Assets/Plugins/Demigiant/`**: **DOTween + DOTween Pro** (Asset Store 직접 임포트). UPM 패키지가 아니므로 매니페스트에는 없음. 트윈 코드는 `using DG.Tweening;`으로 사용.
- **`Assets/50. External Assets/`**: ProCamera2D, kenney_toy-brick-pack. UPM 외부 임포트, 편집 금지.

## 작업 시 주의

- **문서 작성 언어**: 새/수정되는 모든 마크다운/주석 문서는 한국어. 식별자(클래스/메서드/변수)는 영문 유지.
- **보안 시스템 미구현**: 계획서의 `SafeInt`/`SafeFloat`/AES-256 저장 등은 아직 없음. 점수/HP/골드 같은 민감 수치를 추가할 때 일반 `int`로 두면 추후 `SafeInt`로 일괄 교체 대상이 됨을 인지.
- **씬 의존성**: 현재 유일한 씬 `Sample.unity`는 프로토타입 검증용. 새 시스템 검증은 가급적 Sample을 복제해 진행하고, 본격 씬(`Title`, `Village`, `ActMap`, `Stage`)은 마일스톤 7에서 정식 생성될 예정.
