# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

**RPG 핀볼 (가칭)** — Unity 6 (6000.4.0f1)로 개발 중인 Android(세로 화면) RPG + 로그라이크 + 핀볼 액션 게임. 터치로 플리퍼를 즉석 소환해 보스를 토벌하는 타임어택 구조.

현재 상태: **사용자 작성 코드 0줄**. `Assets/02.Scripts/` 폴더가 비어 있음(서드파티 ProCamera2D만 존재). 과거 git log에는 BallController/FlipperController/GameManager/ScoreDisplay 등 일부 프로토타입 커밋이 있으나 현재 워킹트리에는 남아 있지 않음 — 어느 시점에 정리됨. 따라서 마일스톤 1(코어 핀볼 프로토타입)을 **백지 상태에서** 시작한다고 보면 됨. 8개 마일스톤 전체가 미구현 상태이며, 모든 시스템(코어/보안/물리/전투/스킬 트리/보스 AI/절차적 생성/마을 시설)은 설계 문서에만 존재.

## 핵심 디렉토리 구조

```
Assets/
├── 01.Scenes/              현재 Sample.unity 하나만 존재 (Title/Village/ActMap/Stage는 미생성)
├── 02.Scripts/             ⚠️ 빈 폴더. 마일스톤 1부터 새로 작성 필요
│                            계획상 하위 폴더: Core/ Security/ Physics/ Combat/ Enemy/
│                                              Stage/ Village/ UI/ Data/ Meta/
├── 03.Sprites/, 03.Textures/, 04.Data/, 05.Prefabs/, 06.Materials/
├── 50. External Assets/    ProCamera2D, kenney_toy-brick-pack 등 서드파티 임포트
├── Plugins/Demigiant/      DOTween + DOTween Pro (Asset Store 임포트, UPM 아님)
├── Editor/, Resources/, Settings/, TextMesh Pro/, UI Toolkit/
Design/                     14개 한국어 게임 설계 문서 (소스 오브 트루스)
Implementation/             Implementation_Plan.md — 8개 마일스톤 로드맵
GeneratedAssets/             gitignore됨 (AI 생성 임시 에셋용)
```

## 코드 아키텍처 (계획)

> 아직 코드가 없으므로 아래는 **`Implementation/Implementation_Plan.md` 기반 약속**임. 첫 파일 작성 시 이 규약을 따를 것.

### 네임스페이스 규약

`Assets/02.Scripts/<폴더>/` ↔ `RPGPinball.<폴더>` 1:1 대응을 사용 예정.
- `RPGPinball.Core` — `GameManager`, `EventBus`, `Constants`
- `RPGPinball.Security` — `SafeInt`, `SafeFloat`, `SafeLong`, `IntegrityChecker`, `SaveEncryption`
- `RPGPinball.Physics` — `BallController`, `FlipperController`, `DeadZone`, `PlayfieldBuilder`
- `RPGPinball.Combat` / `RPGPinball.Enemy` / `RPGPinball.Stage` / `RPGPinball.Village` / `RPGPinball.UI` / `RPGPinball.Data` / `RPGPinball.Meta` — 각 마일스톤에서 추가

마일스톤 1 파일 목록은 [Implementation_Plan.md](Implementation/Implementation_Plan.md) 라인 65~146 참조.

### 통신 패턴 (계획)

- **GameManager 싱글턴 + DontDestroyOnLoad**: 글로벌 상태(점수/타이머/씬 전환) 보유.
- **EventBus (옵저버 패턴)**: `OnBallHit`, `OnComboChange`, `OnManaChange`, `OnTimePenalty` 등 글로벌 이벤트는 모두 EventBus 경유. 컴포넌트 직접 참조 최소화.
- **태그 기반 충돌**: 공은 Unity 태그 `"Ball"` 사용 예정. `collision.gameObject.CompareTag("Ball")` 패턴.
- **ScriptableObject 데이터**: 플리퍼·코어·재질·스킬·기믹 파라미터는 SO로 분리. `CreateAssetMenu` 경로는 `RPG Pinball/...`로 통일.

### 물리 / 입력 (계획)

- 2D 물리만 사용: `Rigidbody2D`, `Collider2D`, `HingeJoint2D` 모터로 플리퍼 구동(Continuous Detection).
- 모든 물리 상수의 최종 출처는 `Design/Physics_Parameters.md` — 매직 넘버 추가 시 이 문서와 일치 여부 반드시 확인. 공 속도 클램핑(2.0~40.0 U/s), 플리퍼 타이밍(0.08/0.5/0.12s), 쿨타임(1.5s, 하한 0.5s) 등.
- **Unity InputSystem** 사용 (`com.unity.inputsystem` 1.19.0). `InputAction`을 `[SerializeField]`로 인스펙터 노출하는 패턴. 레거시 `Input.GetKey` 사용 금지.

### 카메라 (계획)

`ProCamera2D` 에셋(서드파티) 사용 예정. 메인 카메라에 ProCamera2D 컴포넌트를 부착하고 공을 타깃으로 설정. smoothTime 0.15초, 수직 오프셋 +2.0U, 보스전 줌아웃 ×1.2.

## 설계 문서가 곧 사양서

`Design/`의 한국어 문서들이 구현보다 훨씬 앞서 있으며 **소스 오브 트루스**임. 시스템 작업 시 이 문서를 먼저 읽을 것:

| 시스템 | 참조 문서 |
| --- | --- |
| 전체 기획 | `Design/Game_Design_Spec.md` |
| 데미지 10단계 공식 | `Design/Damage_Formula.md` |
| 물리 상수 (속도, 쿨타임, 페널티) | `Design/Physics_Parameters.md` |
| 세이브 데이터 스키마 | `Design/Data_Schema.md` |
| UI / 씬 흐름 | `Design/UI_Flow.md` |
| 60종 스킬 트리 / 공식 | `Design/Skill_Tree_Diagram.md`, `Design/Skill_Tree_Formulas.md` |
| 액티브 스킬 판정 (범위·히트·넉백) | `Design/Active_Skill_Judgment.md` |
| 보스 패턴 12종 | `Design/Boss_Patterns.md` |
| 엘리트 4종 (현상금) | `Design/Elite_Bounty_Spec.md` |
| 80종 기믹 (수치 포함) | `Design/Gimmick_List.md` |
| 38장 타로카드 | `Design/Tarot_Card_List.md` |
| 절차적 스테이지 생성 | `Design/Procedural_Stage_Gen.md` |
| 프로젝트 관리 규칙 (문서·이미지·MCP) | `Design/rules.md` |
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
- **코드 0줄 상태**: 마일스톤 1부터 실제로 처음 작성한다는 전제로 진행. 프로토타입 작업도 처음부터 `Implementation_Plan.md`의 폴더·네임스페이스 규약을 따를 것 (Core/Pinball 같은 임시 이름 금지 → Physics/, Combat/ 등 계획상 이름 사용).
- **보안 시스템 미구현**: 계획서의 `SafeInt`/`SafeFloat`/AES-256 저장 등은 아직 없음. 점수/HP/골드 같은 민감 수치를 추가할 때 일반 `int`로 두면 추후 `SafeInt`로 일괄 교체 대상이 됨을 인지.
- **씬 의존성**: 현재 유일한 씬 `Sample.unity`는 프로토타입 검증용. 새 시스템 검증은 가급적 Sample을 복제해 진행하고, 본격 씬(`Title`, `Village`, `ActMap`, `Stage`)은 마일스톤 7에서 정식 생성될 예정.
