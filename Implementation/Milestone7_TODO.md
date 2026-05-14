# 마일스톤 7 TODO — UI/UX & 세이브/로드

> **목표**: `Design/UI_Flow.md` 기반 **전체 씬 흐름**(스플래시 → Title → Village → ActMap → Stage → Result)을 완성하고, 인게임 HUD·액트맵·팝업·결과 화면을 정식 구현하며, **AES-256-CBC 암호화 + HMAC-SHA256 무결성 검증 + Google Play Games 클라우드 동기화**가 적용된 `SaveSystem`을 도입한다. `Data_Schema.md` §1~§10 전 영역을 직렬화하며, `OnApplicationPause` 자동 일시정지와 인게임 물리 상태 직렬화(§11 런타임 데이터)도 함께 마무리한다.
>
> **기간**: 2주 (누적 18주)
>
> **상위 문서**: [Implementation_Plan.md §마일스톤 7](Implementation_Plan.md)
>
> **검증 기준**:
> - **전체 씬 흐름 1회 완주**: Splash → Title(터치) → Village → ActMap(노드 30개) → Stage(인게임) → Result → Village/ActMap 분기 모두 정상 (`SceneManager.LoadSceneAsync` UniTask로 비동기 로드, 로딩 페이드 0.3s)
> - **InGameHUD 12종 위젯 모두 갱신**: ⏸ 일시정지·⏱ 타이머·💰 골드·⭐ 등급·콤보 카운터·마나 게이지·스킬 4슬롯·소모품 2슬롯·HP(보스)·결정 카운터·타로 3슬롯·이벤트 토스트
> - **사용자 입력 → 핸들러 → 다운스트림 이벤트 체인 통합 검증** (Skill/Pause/Quit/Continue 5건 이상 — `feedback_e2e_input_path_test.md` 회고)
> - **SaveSystem 무결성**: AES-256-CBC 라운드트립 + HMAC-SHA256 변조 감지(1바이트 변조 시 LoadResult.Tampered) + 디바이스 고유 키 + AppSalt 외부 주입 + 클라우드 백업/복원 (Google Play Games Sign-In 옵션)
> - **PauseManager 백그라운드 복귀**: `OnApplicationPause(true)` → 공/플리퍼/탄막/보스/타이머/콤보 timer 모두 직렬화 → `OnApplicationPause(false)` 복귀 시 정확히 이어서 진행 (5초 백그라운드 + 30초 백그라운드 + 5분 백그라운드 시나리오 3건)
> - **이어하기 시스템**: 시간 초과 → 광고 시청 모킹 → +30초 + 마나 100 + 콤보 0 + 스킬 쿨타임 0 + 보스 HP 유지 + `continueCount` 1 증가 + 등급 상한 C로 클램프
> - **카메라 ortho 최종 확정**: Title/Result ortho=5.625, Village ortho=10, ActMap ortho=10, Stage 동적(`FitToStageBounds`) — `Resolution_Spec.md §3.1`과 1:1 일치
> - **InGameHUD Safe Area 적용**: 상단 60px / 하단 200px 패딩 (Notch/Punch Hole 디바이스 대응은 M8 인계, M7은 고정 값으로 시작)
> - **EditMode 단위 테스트** 신규 60건+ (누적 245건+ 통과)
> - **§13.2 PlayMode 시나리오** 통합 검증 — Title 진입 → ActMap 노드 선택 → Stage 진입 → 클리어 → Result → ActMap 복귀(자동 저장) + 일시정지/재개 (5건 이상)
> - **세이브 데이터 호환**: M6 PlayerPrefs 어댑터 키 → M7 SaveSystem JSON 마이그레이션 (1회성 변환기 통과)
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 미착수 또는 사용자 직접 확인 필요
>
> **작성일**: 2026-05-15 (사전 계획)
>
> **상태**: **사전 계획** (착수 전)

---

## 0. 선행 조건 (마일스톤 1~6 산출물 재사용)

마일스톤 7에서는 **확장·교체·정식화**만 하고 마일스톤 1~6에서 정착된 시그니처는 변경하지 않는다.

| 자산 | 재사용 포인트 |
|---|---|
| [Core/GameManager.cs](../Assets/02.Scripts/Core/GameManager.cs) | `LoadVillage()` 임시 진입점 → **`LoadTitle()`/`LoadVillage()`/`LoadActMap()`/`LoadStage(StageBlueprint)`/`LoadResult(StageResult)` 5종 정식화**. UniTask `WhenAll`로 페이드 + 비동기 로드 병렬화 |
| [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) | M1~M6 이벤트 그대로. **이벤트 신규**: `OnSceneLoadStart`, `OnSceneLoadComplete`, `OnApplicationPaused`, `OnApplicationResumed`, `OnContinueRequested`, `OnContinueGranted`, `OnSaveStarted`, `OnSaveCompleted`, `OnSaveFailed`, `OnLoadStarted`, `OnLoadCompleted`, `OnCloudSyncStarted`, `OnCloudSyncCompleted`, `OnPopupOpened`, `OnPopupClosed`, `OnComboMilestone(int milestone)` (M2 #9 인계) |
| [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) | **§UI / §Scene / §Save / §Pause / §Continue / §Camera 6개 섹션 신규 추가** (§11 참조) |
| [Combat/FlipperController.cs](../Assets/02.Scripts/Physics/FlipperController.cs) | `InputBlocked` 플래그 신규 (M2 #8 인계). SkillDeck UI 터치 중에는 플리퍼 소환 차단. PauseManager가 `Time.timeScale=0` 적용 시 자동 무시 |
| [Combat/SkillDeck.cs](../Assets/02.Scripts/Combat/SkillDeck.cs) | 코드 호출(`SkillDeck.Instance.Use(slotIndex, worldPos)`)에 더해 **터치 좌표 캡처 UI** 신규 도입 (M2 #7 인계). InGameHUD가 슬롯 버튼 + 표적 좌표 입력 모드 핸들링 |
| [Combat/ComboSystem.cs](../Assets/02.Scripts/Combat/ComboSystem.cs) | 10/30/50/100 콤보 도달 시 `OnComboMilestone` 발행 추가 (M2 #9 인계). 효과 트리거는 InGameHUD가 구독 → DOTween Scale Punch + 색상 펄스 |
| [Combat/StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs) | `AddTime(seconds, source)` API 그대로. `ContinueRestoreTime(30)` 신규 (이어하기 +30초). M8 광고 연동 |
| [Stage/StageRunner.cs / StageBuilder.cs](../Assets/02.Scripts/Stage/) | M5 절차 생성 그대로. `BuildStage(StageBlueprint)` 시그니처 유지. **Stage 씬 진입 시 StageBlueprint를 GameManager에서 SerializableObject로 전달** |
| [Enemy/BossBase.cs](../Assets/02.Scripts/Enemy/BossAI/BossBase.cs) | `currentHP`/`maxHP`/`currentPhase`/`activeBuffs`/`activeDebuffs` 게터 그대로. PauseManager가 `IPersistableState` 구현 객체 수집 시 활용 |
| [Village/*Manager.cs](../Assets/02.Scripts/Village/) | 10개 매니저 그대로. 본 마일스톤은 **시설별 정식 UI 화면** 도입(M6 #1 인계). 시설 진입은 디버그 패널 → `PopupManager.OpenFacility(facility)`로 교체 |
| [Meta/EconomyManager.cs](../Assets/02.Scripts/Meta/EconomyManager.cs) | 10종 통화 그대로. **결과 화면이 `BatchAdd(rewards)` 1회 호출로 통화 일괄 지급**. 등급 보너스(S +50%, A +20%, C -30%)는 `GradeSystem`(M8)이 사전 적용 |
| [Meta/QuestManager.cs](../Assets/02.Scripts/Meta/QuestManager.cs) | UTC+9 자정 갱신 그대로. **타이틀 진입 시 `RefreshIfExpired()` 자동 호출**(IClock 주입) + Tavern UI에서 갱신 가능 |
| [Security/SaveEncryption.cs](../Assets/02.Scripts/Security/SaveEncryption.cs) | **AES-256-CBC + HMAC-SHA256 골격 그대로**. `AppSalt`를 하드코딩 → **`ISaltProvider` 인터페이스 주입**(M1 #6 인계). M7 빌드 파이프라인에서 `BuildTimeSaltProvider`, 런타임에선 `RuntimeSaltProvider` 사용 |
| [Security/Safe*.cs](../Assets/02.Scripts/Security/) | SafeInt/SafeFloat/SafeLong 그대로. SaveSystem 직렬화 시 `Value`로 복호화하여 평문 저장 (저장 후 즉시 재난독화) |
| [Security/IntegrityChecker.cs](../Assets/02.Scripts/Security/IntegrityChecker.cs) | 그대로. 로드 직후 1회 + 매 30초 주기적 체크섬 검증 |

---

## 0-A. 스프라이트 자산 매핑 (Kenney Assets 추가)

> M6에서 `kenney_rune-pack` / `kenney_block-pack` / `kenney_medals` 등을 적용. M7은 **UI 위젯**(버튼/패널/슬라이더/팝업 배경)은 `kenney_pixel-ui-pack`을, **액트맵 월드맵**(노드/경로/지형 타일)은 `kenney_map-pack`을 사용한다. 결과 화면 등급 메달은 M6의 `kenney_medals` 재사용.
>
> **임포트 설정 기본값**:
> - `kenney_pixel-ui-pack` 9-Slice: `PPU=32`, `Filter Mode=Point (no filter)`(픽셀 아트 유지), `Compression=None`, `Sprite Mode=Single`, `Mesh Type=Full Rect`, `Border = 6/6/6/6 px`(Sprite Editor에서 수동 설정 — Colored/Outline 16×16 기준), `Border = 8/8/8/8 px`(Ancient 24×24 기준)
> - `kenney_pixel-ui-pack` Spritesheet: `Sprite Mode=Multiple`로 슬라이스 후 위와 동일
> - `kenney_map-pack` PNG 타일: `PPU=64`, `Filter Mode=Point`, `Compression=None`, `Sprite Mode=Single`, `Pivot=Center` (Tilemap 셀 1U=1Tile)

### 0-A.1 UI 위젯 ↔ `kenney_pixel-ui-pack` 매핑

> 픽셀 아트 9-Slice 패널 + 픽셀 화살표/아이콘. 9-Slice는 `9-Slice/{Colored|Ancient|Outline}/` 폴더에 색상별 PNG로 분리되어 있고, 작은 아이콘/슬라이더 부품은 `Spritesheet/UIpackSheet_transparent.png` 한 장에 모여 있어 Sprite Editor에서 Slice → Grid by Cell Size 16×16(컬러/아웃라인) 또는 24×24(고대풍)로 추출한다. 부족하면 M8에서 자체 아트 교체.

| 위젯 | 용도 | Kenney 경로 |
|---|---|---|
| 버튼 (기본) | [확인]/[취소]/[닫기]/일반 액션 | `kenney_pixel-ui-pack/9-Slice/Colored/blue.png` (+ `blue_pressed.png` for Pressed State) |
| 버튼 (강조) | [출격]/[시작]/[클리어]/위험 알림 | `kenney_pixel-ui-pack/9-Slice/Colored/red.png` (+ `red_pressed.png`) |
| 버튼 (긍정/성공) | [보상 받기]/[수령] | `kenney_pixel-ui-pack/9-Slice/Colored/green.png` (+ `green_pressed.png`) |
| 버튼 (보조) | [다시 보지 않기] 체크박스 외곽 | `kenney_pixel-ui-pack/9-Slice/Colored/grey.png` (+ `grey_pressed.png`) |
| 버튼 (보너스/주의) | 일일 보상/리셋 경고 | `kenney_pixel-ui-pack/9-Slice/Colored/yellow.png` (+ `yellow_pressed.png`) |
| 패널 배경 (일반) | PopupManager 5종 팝업 외곽 9-Slice | `kenney_pixel-ui-pack/9-Slice/Ancient/tan.png` + `tan_inlay.png` (내부 음각) |
| 패널 배경 (다크) | 일시정지 메뉴/설정 팝업 외곽 | `kenney_pixel-ui-pack/9-Slice/Ancient/brown.png` + `brown_inlay.png` |
| 패널 배경 (밝음) | RewardPopup / 클리어 결과 외곽 | `kenney_pixel-ui-pack/9-Slice/Ancient/white.png` + `white_inlay.png` |
| 패널 (외곽선만) | 인벤토리 슬롯 / 스킬 슬롯 보더 | `kenney_pixel-ui-pack/9-Slice/Outline/yellow.png` (선택 시), `Outline/blue.png` (일반), `Outline/green.png` (장착) |
| 슬라이더 트랙 / 마나 게이지 / 보스 HP / 진행 바 | 가로 게이지 9-Slice | `kenney_pixel-ui-pack/9-Slice/Outline/blue.png` (트랙) + 내부 채움은 `Colored/blue.png` 또는 `Colored/green.png` |
| 슬라이더 핸들 / 체크박스 ✓ / ✕ 아이콘 / 작은 아이콘 | Spritesheet 슬라이스 | `kenney_pixel-ui-pack/Spritesheet/UIpackSheet_transparent.png` (Sprite Editor: Grid by Cell Size 16×16 후 사용) |
| 인벤토리 배경 컨테이너 | 인벤토리 그리드 외곽 | `kenney_pixel-ui-pack/9-Slice/list.png` |
| 우주풍 컨테이너 | (옵션) Title 로딩 / SciFi 알림 | `kenney_pixel-ui-pack/9-Slice/space.png` + `space_inlay.png` |
| 방향 화살표 (좌/우/상/하, 4색) | 액트 탭 전환 / 페이지 네비게이션 / 뒤로가기 | `Spritesheet/UIpackSheet_transparent.png` 우측 화살표 4×4 그리드 (Yellow/Green/Orange/Blue × ↑↓←→) |

> **9-Slice Border 설정 작업** — Unity Sprite Editor에서 Border 직접 입력 필요. **`Assets/Editor/PixelUIBorderImporter.cs` 신규** (M7 작업 항목): `AssetPostprocessor.OnPreprocessTexture` 훅으로 `kenney_pixel-ui-pack/9-Slice/Colored/*.png`는 `Border=6,6,6,6`, `9-Slice/Ancient/*.png`는 `Border=8,8,8,8`, `9-Slice/Outline/*.png`는 `Border=4,4,4,4`로 자동 적용.

### 0-A.2 액트맵 월드맵 ↔ `kenney_map-pack` 매핑

> ActMap은 **Unity 2D Tilemap**(Tilemap + TilemapRenderer) 위에 노드 마커를 얹는 구조. `kenney_map-pack/PNG/` 188장의 64×64 픽셀 타일을 시즌(액트)별로 분류해 매핑한다. **타일 ID 분류는 `kenney_map-pack/Sample.png` 미리보기 + `mapTile_XXX.png` 인덱스 조사로 본 마일스톤 작업 중 1회 확정 후 [Map_Tile_Index.md](../Design/Map_Tile_Index.md)에 기록**(신규 문서). 본 표는 1차 후보 매핑이며, 작업 중 시각 검증 후 확정.

| 액트 (테마) | 지형 / 경로 / 노드 타일 후보 (`mapTile_XXX.png`) |
|---|---|
| **Act 1 — 봄 (숲/초원)** | 풀밭 `001~012` / 흙길 `065~076` / 나무 `121~128` / 호수 `040~048` / 다리 `113~116` / 봄 보스 영역 (식충식물) `145~148` |
| **Act 2 — 여름 (심해/해안)** | 모래 `013~024` / 바위 `077~088` / 야자수 `129~132` / 바다 `049~056` / 부두 `117~120` / 여름 보스 영역 (크라켄) `149~152` |
| **Act 3 — 가을 (기계/유적)** | 황토 `025~036` / 자갈 `089~100` / 단풍나무 `133~136` / 강 `057~064` / 톱니다리 `121~124` / 가을 보스 영역 (드래곤) `153~156` |
| **Act 4 — 겨울 (빙판/성벽)** | 눈 `037~048` / 얼음 `101~112` / 침엽수 `137~140` / 빙하 `065~072` / 성벽 다리 `125~128` / 겨울 보스 영역 (겨울 여왕) `157~160` |

> 위 번호 범위는 1차 추정. **`Assets/Editor/MapTilePreviewer.cs` 신규** (M7 작업 항목): EditorWindow로 188장 썸네일 그리드 표시 + 액트별 카테고리 토글 → 확정된 매핑을 `MapTilePalette.asset`(SO)에 직렬화.

#### 0-A.2.1 노드 마커 (액트맵 위에 얹는 6종 마커)

> 노드 마커는 지형 타일과 별도. `kenney_map-pack/PNG/`에는 `mapTile_161~188.png` 영역에 깃발/돌탑/표지판/문(door) 류의 **랜드마크 타일**이 있어 노드 종류별로 매핑.

| 노드 유형 | 미클리어 (1차 후보) | 클리어(B+) 오버레이 | 비고 |
|---|---|---|---|
| 일반 전투 | `mapTile_161.png` (깃발) | `kenney_medals/PNG/Flat/flat_medal3.png` | A=금 / B=은 |
| 엘리트 전투 | `mapTile_165.png` (붉은 깃발 / 검은 표지) | `flat_medal5.png` | 어둠 적색 |
| 보스 전투 | `mapTile_171.png` (성문 / 큰 구조물) | `flat_medal9.png` (Best) | 10/20/30 고정 |
| 휴식 노드 | `mapTile_175.png` (텐트 / 모닥불) | — | 5/15/25 |
| 이벤트 노드 | `mapTile_177~180.png` (상자/제단/표지판/주사위) | `flat_medal1.png` | 4종 (여행자/보물/제단/도박) |
| 히든 노드 | `mapTile_185.png` (덮인 표지 — 닫힘) → `mapTile_186.png` (열림) | — | 해금 후 표시 |

> 노드 마커 타일 ID 확정도 **§0-A.2 `MapTilePreviewer.cs`로 1회 시각 검증 후 [Map_Tile_Index.md](../Design/Map_Tile_Index.md) §노드 마커 표에 기록**.

#### 0-A.2.2 노드 간 연결선

- Line Renderer (M7 §6.1) — 미클리어=회색 (`#808080`) / 클리어=금 (`#FFD700`)
- 또는 **`mapTile_073~076.png` (흙길 4방향)** + `mapTile_109~112.png` (포장도로 4방향)을 Tilemap 위에 깔아 연결선 표현 (시각적으로 더 풍부). 본 마일스톤은 **Line Renderer 우선**, Tilemap 경로는 M8 폴리싱에서 시도

### 0-A.3 결과 화면 등급 메달 ↔ `kenney_medals` 매핑 (M6 재사용)

| 등급 | 메달 경로 | 색상 |
|---|---|---|
| S | `kenney_medals/PNG/Flat/flat_medal9.png` | 무지개 |
| A | `kenney_medals/PNG/Flat/flat_medal5.png` | 금 |
| B | `kenney_medals/PNG/Flat/flat_medal3.png` | 은 |
| C | `kenney_medals/PNG/Flat/flat_medal1.png` | 동 |

### 0-A.4 Title 배경

> Title 씬은 ActMap과 다른 톤 — `kenney_map-pack` 일부 타일을 4×6 등으로 타일링한 풀스크린 배경 또는 단색 배경 + 픽셀 이펙트로 구성. 본 마일스톤은 임시 매핑까지만, M8에서 자체 아트 교체.

| 화면 | 배경 구성 |
|---|---|
| Title (스플래시 포함) | `kenney_map-pack/PNG/mapTile_001.png` (풀밭 베이스 타일)을 화면 전체에 Tilemap 타일링 + DOTween Color Lerp(파스텔 톤 천천히 변화) + 픽셀 별 파티클(M8) |

> Title 일러스트(주인공 캐릭터, 게임 로고 이미지)는 **자체 아트 / 외주 / Asset Store 별도 임포트**가 필요하므로 **M8 인계** (§13 #14).
>
> **부족 자산 식별 시 M8 인계**: 마을 NPC 인물 일러스트, 액트별 보스 초상화, 스킬 아이콘 60종(현재 [Image_Assets_Inventory.md](../Design/Image_Assets_Inventory.md) 참조).

---

## 1. 데이터 정의 (SaveSystem JSON 모델 + UI ScriptableObject)

### 1.1 SaveSystem 모델 (`02.Scripts/Data/SaveData.cs` 신규)

> `Data_Schema.md` §1~§10에 1:1 대응하는 직렬화 클래스. M6의 `PlayerPrefsSaveAdapter`가 키별로 저장하던 항목을 **단일 JSON으로 통합**한다.

- [ ] `SaveData` (최상위) — `version=2.0.0` (M6=1.0.0 → 마이그레이션), `lastSaveTime`, `player`, `inventory`, `skillTree`, `stageProgress`, `village`, `collection`, `quests`, `settings`, `statistics`
- [ ] `PlayerSaveData` — 13개 필드 (§2)
- [ ] `InventorySaveData` — 5개 장착 슬롯 + 5개 인벤토리 리스트 (§3)
- [ ] `SkillTreeSaveData` — 3 분기 dictionary<string, int>
- [ ] `StageProgressSaveData` — `Dictionary<int, ActProgress>` + `bossesDefeated[]` + `hiddenNodesRevealed[]` + `eventHistory[]`
- [ ] `VillageSaveData` — 6 시설 nested struct
- [ ] `CollectionSaveData` — `Dictionary<string, GimmickEncounter>` + achievements + titles + skins
- [ ] `QuestsSaveData` — daily[3] + weekly + bountyBoard[3]
- [ ] `SettingsSaveData` — BGM/SFX/Haptic/Graphics/Accessibility 5종
- [ ] `StatisticsSaveData` — 9개 누적 카운터

### 1.2 인게임 런타임 직렬화 모델 (§11 — PauseManager용)

> `OnApplicationPause(true)` 시점에 메모리 스냅샷. 저장 대상은 아니지만 백그라운드 복귀 후 정확한 재개를 위해 직렬화.

- [ ] `RuntimeStageSnapshot` — `Data_Schema.md §11` 19개 필드 (`stageId`, `seed`, `remainingTimeSec`, `manaGauge`, `comboCount`, `ballState`, `multiBalls`, `bossState`, `monstersAlive`, `activatedGimmicks`, `skillCooldowns`, `consumablesRemaining`, `droppedItems`, `stageGrade`, `continueCount` 등)
- [ ] `IPersistableState` 인터페이스 — 모든 인게임 객체가 구현 (Ball/Flipper/Boss/Monster/Gimmick/Projectile)
- [ ] `RuntimeStageSerializer` — `Find<IPersistableState>()` 전체 수집 → JSON 직렬화

### 1.3 UI ScriptableObject 데이터

- [ ] `UIThemeData.cs` (SO) — 컬러 팔레트(Primary/Secondary/Success/Warning/Danger), 폰트 사이즈 6단계, 9-Slice 마진. 모든 위젯이 참조 → M8에서 다크 모드 추가 시 1곳만 수정
- [ ] `ActMapNodeData.cs` (SO) — 노드 유형 6종 × 미클리어/클리어 아이콘 + 클릭음 + 강조 색
- [ ] `PopupTemplateData.cs` (SO) — 5종 팝업 템플릿(확인/알림/보상/가이드/설정) 프리팹 + 기본 버튼 셋
- [ ] `ContinueAdData.cs` (SO) — 광고 ID(AdMob/UnityAds) 플레이스홀더 + 일일 한도 3회

---

## 2. **[인계: M1 #6]** SaveEncryption.AppSalt 외부 주입

> 현재 `SaveEncryption.cs`의 `AppSalt`는 하드코딩. 빌드 파이프라인 또는 서버에서 주입하는 구조로 전환.

- [ ] `02.Scripts/Security/ISaltProvider.cs` (인터페이스) — `byte[] GetAppSalt()` + `string GetSaltVersion()` (롤링용)
- [ ] `02.Scripts/Security/BuildTimeSaltProvider.cs` — `[InitializeOnLoadMethod]`로 빌드 시점에 `BuildSettings`에서 솔트 주입. 에디터 모드는 디버그 솔트(`"debug_salt_v1"`)
- [ ] `02.Scripts/Security/RuntimeSaltProvider.cs` — 빌드된 APK에서 `StreamingAssets/salt.bin` (난독화 처리) 또는 Firebase Remote Config 조회. 본 마일스톤은 **`StreamingAssets/salt.bin` 방식**으로 첫 구현
- [ ] `02.Scripts/Security/SaveEncryption.cs` 수정 — 생성자에 `ISaltProvider` 주입 + 솔트 버전을 헤더에 기록(`SALTV:v1\nDATA:...`)
- [ ] `Assets/Editor/SaltBuildProcessor.cs` — Android 빌드 직전에 32바이트 랜덤 솔트 생성 → `StreamingAssets/salt.bin` 기록 + `BuildSettings.SaltVersion` 갱신
- [ ] `02.Scripts/Security/SaveMigration.cs` — 솔트 버전 변경 시 구 데이터 → 신 데이터 1회성 변환 (구 솔트는 마이그레이션 후 폐기)

### 2.1 검증

- [ ] `SaltProviderTests.cs` — BuildTimeSaltProvider / RuntimeSaltProvider 모두 32바이트 + 0이 아닌 솔트 반환 (4건)
- [ ] `SaveEncryptionSaltVersionTests.cs` — 솔트 v1 → v2 마이그레이션 시 데이터 무손실 (3건)

---

## 3. SaveSystem — AES-256-CBC + HMAC-SHA256 + 클라우드 동기화

> M6는 `PlayerPrefsSaveAdapter` 임시 어댑터. M7은 **정식 SaveSystem**으로 교체. M6 어댑터 키 → M7 JSON 마이그레이션 변환기 포함.

### 3.1 코어 SaveSystem

- [ ] `02.Scripts/Core/SaveSystem.cs` (싱글턴) — 3개 슬롯(local/cloud/temp) 관리. `SaveResult Save(SaveData)`, `LoadResult Load()`, `bool HasSave()`, `Delete()` API
- [ ] `02.Scripts/Core/SaveResult.cs`, `LoadResult.cs` — enum: `Success`, `Tampered`, `Corrupted`, `VersionMismatch`, `CloudConflict`, `IOError`
- [ ] **암호화 파이프라인**:
    1. `SaveData` → `JsonUtility.ToJson` (들여쓰기 없음)
    2. AES-256-CBC 암호화 (`SaveEncryption.Encrypt`)
    3. HMAC-SHA256 서명 (`SaveEncryption.Sign`)
    4. `Application.persistentDataPath/save.dat`에 `[16바이트 IV][N바이트 암호문][32바이트 HMAC]` 형식으로 저장
- [ ] **복호화 파이프라인**:
    1. 파일 읽기 + IV/암호문/HMAC 분리
    2. HMAC 검증 (실패 → `LoadResult.Tampered`, 클라우드 자동 복원 시도)
    3. AES-256-CBC 복호화
    4. JSON 역직렬화 + 버전 확인 (`version != "2.0.0"` → 마이그레이션)

### 3.2 자동 저장 트리거

- [ ] EventBus 구독 — `OnStageCleared`, `OnFacilityUsed`, `OnEquipmentChanged`, `OnBossDefeated`, `OnLevelUp` 5종에서 자동 저장 호출
- [ ] **저장 빈도 제한**: 마지막 저장 이후 5초 미만이면 큐에 적재 → 5초 후 단일 저장(불필요한 디스크 I/O 방지)
- [ ] **저장 중 충돌 방지**: `SemaphoreSlim(1,1)`로 동시 저장 호출 직렬화 (씬 전환 + 자동 저장 동시 발생 케이스)

### 3.3 Google Play Games 클라우드 동기화

- [ ] `02.Scripts/Core/CloudSaveAdapter.cs` — Google Play Games Services Saved Games API 래핑
- [ ] **로그인 흐름**: Title 진입 시 `PlayGamesPlatform.Authenticate()` 시도. 실패 시 로컬만 사용 (Settings에서 재시도 가능)
- [ ] **클라우드 충돌 해결**: 로컬 vs 클라우드 `lastSaveTime` 비교 → 최신 자동 선택. **5분 이상 차이 시** Conflict 팝업 표시(M8에서 본 구현, 본 마일스톤은 자동 최신 선택)
- [ ] **변조 감지 시 자동 복원**: `LoadResult.Tampered` 발생 → 클라우드 최신본 다운로드 → 로컬 덮어쓰기 → 사용자에게 알림 팝업
- [ ] **본 마일스톤 범위**: 인증 + 업로드 + 다운로드 + 자동 충돌 해결까지. **충돌 해결 UI / Sign-In Conflict 흐름은 M8 인계**

### 3.4 M6 → M7 마이그레이션

- [ ] `02.Scripts/Core/SaveMigrationV1ToV2.cs` — PlayerPrefs 키 → `SaveData` JSON 변환 1회 실행. M6 키 목록:
    - `Economy.Gold`, `Economy.ManaCrystal`, `Economy.BossSoul`, ...(10종)
    - `Forge.Material`, `Forge.MainCore`, `Forge.SubCores`, `Forge.FlipperVariant`, `Forge.FlipperLevel`
    - `Enchanter.RuneSlots`, `Astrologer.TarotPullCount`, `Astrologer.PermanentCards`, ...
- [ ] Title 진입 시 `PlayerPrefs.HasKey("Economy.Gold")` && `!File.Exists(save.dat)` → 마이그레이션 실행 → 성공 시 PlayerPrefs 키 삭제
- [ ] **마이그레이션 실패 시**: PlayerPrefs 키 유지 + 사용자에게 "구버전 데이터 변환 실패" 안내 + 로그 송신

### 3.5 검증

- [ ] `SaveSystemRoundTripTests.cs` — Save → Load 후 모든 필드 일치 (10건+)
- [ ] `SaveSystemTamperingTests.cs` — 암호문 1바이트 변조 / IV 변조 / HMAC 변조 → `LoadResult.Tampered` (3건)
- [ ] `SaveSystemVersionMigrationTests.cs` — v1.0.0 → v2.0.0 마이그레이션 + 잘못된 버전 거부 (4건)
- [ ] `SaveAutoTriggerTests.cs` — 5종 이벤트 발생 시 단일 호출로 합쳐짐 (`OnStageCleared` 연속 3회 → 5초 후 1회만 저장) (3건)
- [ ] `SaveMigrationV1ToV2Tests.cs` — M6 PlayerPrefs 데이터 → SaveData 변환 무손실 + PlayerPrefs 키 삭제 (5건)
- [ ] **클라우드 동기화는 EditMode 단위 테스트 불가** — PlayMode 시나리오로 인계 (§12.2)

---

## 4. PauseManager — 일시정지 + 백그라운드 복귀

> `Game_Design_Spec.md §UI_Flow §5` 모바일 필수 요구사항. 백그라운드 → 복귀 시 정확한 재개.

- [ ] `02.Scripts/Core/PauseManager.cs` (싱글턴) — `Pause(reason)`, `Resume()`, `IsPaused` API. `reason` enum: `UserRequest`, `ApplicationBackground`, `SystemNotification`, `IncomingCall`
- [ ] `Time.timeScale = 0` 적용 (StageTimer/ComboSystem은 `Time.timeScale` 의존, 자동 정지)
- [ ] **`OnApplicationPause(true)` 자동 처리**:
    1. `Pause(reason: ApplicationBackground)` 호출
    2. `RuntimeStageSerializer.Snapshot()` → `Application.persistentDataPath/runtime.snapshot` 임시 저장 (암호화 불필요, 앱 강제 종료 시 복원용)
    3. `SaveSystem.Save()` 자동 호출
- [ ] **`OnApplicationPause(false)` 복귀**: 자동 `Resume()` 호출. 단 `IsPaused == true && reason == UserRequest`이면 사용자 입력 대기
- [ ] **앱 강제 종료 복원**: 다음 실행 시 `runtime.snapshot` 존재 시 → 사용자에게 "이어서 시작" 확인 → `RuntimeStageSerializer.Restore()` 호출
- [ ] **인게임 객체 직렬화**: Ball/Flipper/Boss/Monster/Gimmick/Projectile 모두 `IPersistableState` 구현 → `Snapshot()` / `Restore()` 메서드 추가
- [ ] **시간 처리**: 일시정지 중 `Time.unscaledTime` 사용한 UI 애니메이션만 동작. 게임 로직(타이머/콤보 timer)은 완전 정지

### 4.1 검증

- [ ] `PauseManagerTests.cs` — Pause/Resume 시 Time.timeScale 토글 + 중첩 호출 안전 (5건)
- [ ] `RuntimeStageSnapshotRoundTripTests.cs` — Snapshot → Restore 후 공 위치/속도/회전 정확 일치 (오차 < 0.001) (4건)
- [ ] `OnApplicationPauseTests.cs` (PlayMode) — PauseManager가 `OnApplicationPause(true)` 호출 시 자동 저장 + 스냅샷 생성 (3건)

---

## 5. Title 씬

> 첫 진입 화면. 스플래시 → Title → 마을 또는 신규 게임 분기.

### 5.1 Splash + Title 씬 구성

- [ ] `01.Scenes/Title.unity` (신규)
- [ ] 카메라 ortho=5.625 (`Resolution_Spec.md §3.1`) — Title/Result 공용
- [ ] 배경 Tilemap — `kenney_map-pack/PNG/mapTile_001.png`(풀밭 베이스) 풀스크린 타일링 + DOTween Color Lerp(파스텔 톤 천천히 변화, 8s loop) + 페이드 인 0.5s
- [ ] 로고 텍스트 — "RPG Pinball" + 부제 (한국어/영어 토글). 1080×400 영역 중앙 상단
- [ ] **터치하여 시작** 텍스트 — DOTween Alpha 펄스(0.5→1.0→0.5, 1.5s loop)
- [ ] 좌하단 [설정] / 우하단 [크레딧] 버튼 — 9-Slice 배경 `kenney_pixel-ui-pack/9-Slice/Colored/grey.png` + 내부 아이콘은 `Spritesheet/UIpackSheet_transparent.png` 슬라이스에서 톱니바퀴/책 아이콘

### 5.2 TitleScreenController

- [ ] `02.Scripts/UI/TitleScreenController.cs`
- [ ] Awake 시 `SaveSystem.HasSave()` 체크
- [ ] 화면 터치(어디든) → 세이브 있음 → `GameManager.LoadVillage()`, 없음 → `GameManager.LoadTutorial()` (Act 1 Stage 1 강제 진입, M8 튜토리얼과 연계)
- [ ] [설정] 터치 → `PopupManager.OpenSettings()`
- [ ] [크레딧] 터치 → `PopupManager.OpenCredits()` — Kenney CC0 표기 포함

### 5.3 검증

- [ ] `TitleScreenControllerTests.cs` (EditMode) — `SaveSystem.HasSave()` 모킹 후 분기 (3건)
- [ ] PlayMode 시나리오 (§12.2): Title 진입 → 터치 → Village 전환 / 신규 게임 진입

---

## 6. ActMap 씬 + ActMapUI

> 액트(봄/여름/가을/겨울) 4종 + 30개 노드 맵. 분기/히든 노드.

### 6.1 씬 구성

- [ ] `01.Scenes/ActMap.unity` (신규)
- [ ] 카메라 ortho=10 (Village와 동일)
- [ ] **배경 Tilemap** — `kenney_map-pack` 타일로 액트별 지형(§0-A.2 표)을 절차적으로 배치. **`02.Scripts/UI/ActMapTilemapBuilder.cs` 신규** — `MapTilePalette.asset`(SO) 참조해 액트별 30 노드 좌표 주변에 지형 타일 자동 배치 (해당 액트 외 영역은 흐릿하게 / 잠금)
- [ ] Tilemap 그리드 셀 크기 — 1U × 1U (PPU=64, 64×64 픽셀 = 1U)
- [ ] 액트 탭 4종 (상단) — 봄/여름/가을/겨울. 미해금 액트는 잠금 아이콘 (`kenney_pixel-ui-pack` 자물쇠 슬라이스, 또는 `kenney_map-pack/PNG/mapTile_185.png` 임시 활용)
- [ ] 노드 30개 그리드 — 5열 × 6행 (X=±4.5U, Y=-9~+9U 분포). 보스 10/20/30은 중앙 정렬, 보너스 분기/히든은 좌우로. 각 노드는 §0-A.2.1 표의 노드 마커 타일을 Sprite Renderer로 표시
- [ ] 노드 간 연결선 — Line Renderer로 그라데이션 (미클리어=회색, 클리어=금) — Tilemap 경로 타일 표현은 M8 폴리싱 인계

### 6.2 ActMapUI

- [ ] `02.Scripts/UI/ActMapUI.cs`
- [ ] `SaveData.stageProgress` 조회 → 노드별 클리어 상태 + 최고 등급 메달 표시
- [ ] 노드 터치 → `NodeInfoPopup` (스테이지 번호 + 유형 + 권장 레벨 + 적용 스테이지 특성 + 보상 미리보기)
- [ ] [출격 준비] 버튼 → `PrepScreen` (장착 타로 3슬롯 / 소모품 2슬롯 / 스킬 덱 4슬롯 / 재질·코어·플리퍼 요약)
- [ ] [출격] 버튼 → `GameManager.LoadStage(blueprint)` — `ProceduralStageGenerator.Generate(seed)` 호출 후 결과를 전달
- [ ] [뒤로가기] → `GameManager.LoadVillage()`
- [ ] **히든 노드 해금 조건**: `StageProgress.hiddenNodesRevealed[stageIndex]` 체크. 미해금 시 `?` 아이콘만 표시. 해금 트리거는 `OnEventNodeCompleted`(M5)에서 구독

### 6.3 검증

- [ ] `ActMapUITests.cs` (EditMode) — 30 노드 그리드 좌표 정확 + 클리어 상태 매핑 (8건)
- [ ] `NodeInfoPopupTests.cs` — 노드별 정보 표시 데이터 일치 (5건)
- [ ] PlayMode 시나리오: ActMap 진입 → 노드 선택 → 출격 준비 → 출격 → Stage 전환

---

## 7. Stage 씬 + InGameHUD

> **[인계: M2 #16]** DebugHud → 정식 InGameHUD 교체. **[인계: M2 #7]** 스킬 인게임 입력부. **[인계: M2 #8]** Flipper ↔ SkillDeck 입력 충돌 차단. **[인계: M2 #9]** OnComboMilestone 이펙트.

### 7.1 씬 구성

- [ ] `01.Scenes/Stage.unity` (M5 Sample.unity → 정식 템플릿화 + 이름 변경)
- [ ] 카메라 — `CameraController.FitToStageBounds` (M5 그대로). Stage Blueprint 진입 시 동적 ortho 계산
- [ ] HUD Canvas (Screen Space - Overlay, Ref 1080×1920, Match=0) 신규
- [ ] Pause Canvas (Screen Space - Overlay, sortOrder=10) 신규 — 일시정지 시 활성화

### 7.2 InGameHUD 위젯 12종

- [ ] `02.Scripts/UI/InGameHUD.cs`
- [ ] **상단 HUD** (Safe Area 60px 상단 패딩):
    - [ ] ⏸ 일시정지 버튼 (좌측) — 9-Slice `kenney_pixel-ui-pack/9-Slice/Colored/grey.png` + 일시정지 ‖ 아이콘 (Spritesheet 슬라이스). 터치 → `PauseManager.Pause(UserRequest)`
    - [ ] ⏱ 타이머 (중앙) — DOTween Color Lerp (60s 미만 시 황색, 30s 미만 시 적색)
    - [ ] 💰 골드 카운터 (우측 상단) — `OnGoldChanged` 구독
    - [ ] ⭐ 현재 예상 등급 (우측) — `StageTimer.RemainingTimeRatio` 기반 실시간 S/A/B/C
- [ ] **중앙 콤보 카운터**:
    - [ ] "{N} COMBO!" 텍스트 (DOTween Scale Punch 1.2× 0.3s)
    - [ ] `OnComboMilestone(10/30/50/100)` 구독 → 마일스톤별 색상 펄스 + 사운드(M8 인계). 본 마일스톤은 DOTween Scale 효과만
- [ ] **하단 HUD** (Safe Area 200px 하단 패딩):
    - [ ] 마나 게이지 — Slider (`OnManaChange` 구독), 100/100 표시
    - [ ] 스킬 덱 4슬롯 — Image 4개 + 쿨타임 오버레이(원형 채우기) + 슬롯 번호. 터치 → `SkillDeckInputController.SelectSlot(i)`
    - [ ] 소모품 2슬롯 — Image 2개 + 남은 횟수. 터치 → `ConsumableSlot.Use()`
- [ ] **부가 위젯**:
    - [ ] 보스 HP 바 (상단, 보스전 한정) — `BossBase.HPRatio` 바인딩 + 페이즈 마커
    - [ ] 마나 결정 / 보스 영혼 / 코어 조각 등 통화 토스트(우상단, 3초 페이드)

### 7.3 SkillDeck 인게임 입력부 (M2 #7 인계)

- [ ] `02.Scripts/UI/SkillDeckInputController.cs`
- [ ] **2단계 입력**:
    1. 슬롯 버튼 터치 → `selectedSlot = i` + `FlipperController.InputBlocked = true` + 화면에 "표적 지정" 오버레이 표시 (반투명 흰색 + 십자선)
    2. 화면 터치 → `worldPos = Camera.ScreenToWorldPoint(touch)` + `SkillDeck.Instance.Use(selectedSlot, worldPos)` 호출 + `InputBlocked = false`
- [ ] **취소 입력**: 슬롯 선택 후 화면 외 영역(상단 HUD/하단 HUD) 터치 → 취소 + `InputBlocked = false`
- [ ] **즉발 스킬** (표적 불필요): 슬롯 터치 즉시 `Use(i, ballPos)` 호출 → 입력 차단 없음
- [ ] **궁극기 1개 제한**: 4슬롯 중 Tier 6은 1개만. SkillDeck.SetSlot이 이미 검증(M3)
- [ ] **`FlipperController.InputBlocked`** 신규 플래그 (M2 #8 인계):
    - 슬롯 선택 + 표적 지정 중에는 플리퍼 소환 입력 무시
    - PauseManager 활성 시에도 무시
    - 검증: 슬롯 선택 → 플레이필드 터치 → `FlipperController.Spawn` 호출 0회

### 7.4 OnComboMilestone 이벤트 훅 (M2 #9 인계)

- [ ] `ComboSystem.cs` 수정 — `OnComboMilestone` 발행 추가 (10/30/50/100 도달 시 1회)
- [ ] InGameHUD 구독 → 콤보 텍스트에 DOTween Scale Punch + 색상 펄스 + (M8) 효과음
- [ ] EditMode 검증: ComboSystem.AddCombo()로 10/30/50/100 도달 시 OnComboMilestone(N) 호출 1회

### 7.5 일시정지 화면

- [ ] `PauseMenuUI.cs` — Pause Canvas에 배치, `PauseManager.OnPaused` 구독으로 활성/비활성
- [ ] [계속하기] / [설정] / [포기]
- [ ] [포기] → ConfirmQuitPopup → 예 → `GameManager.LoadVillage()` + 진행 데이터 폐기 (스테이지 미클리어로 처리)

### 7.6 이어하기 UI

- [ ] `02.Scripts/UI/ContinueAdPopup.cs`
- [ ] `StageTimer.OnTimeOut` 구독 → 자동 표시
- [ ] [광고 시청] 버튼 → `AdManager.ShowRewardedAd(callback)` (M8 본 구현, 본 마일스톤은 5초 모킹 + 즉시 콜백)
- [ ] 콜백 시 `StageTimer.ContinueRestoreTime(30)`, `ManaSystem.SetMana(100)`, `ComboSystem.Reset()`, `SkillDeck.ResetAllCooldowns()`, `BallController.RespawnAtSafePoint()` + `RuntimeStageSnapshot.continueCount++` + `stageGrade = "C"` 클램프
- [ ] [포기] → ResultScreen (실패)
- [ ] **일일 한도 3회** — `SaveData.player.adContinueUsedToday >= 3` 시 [광고 시청] 비활성

### 7.7 검증

- [ ] `InGameHUDTests.cs` (EditMode) — 위젯 12종 모두 GameObject 활성/Image 할당 확인 (12건)
- [ ] `SkillDeckInputControllerTests.cs` — 슬롯 선택 → 표적 지정 → Use 호출 + 취소 분기 (8건)
- [ ] `FlipperInputBlockedTests.cs` — 입력 차단 중 Spawn 호출 0회 (3건)
- [ ] `ComboMilestoneTests.cs` — 10/30/50/100 도달 시 OnComboMilestone(N) 호출 1회, 비-마일스톤(11/12 등)에서는 호출 0회 (5건)
- [ ] `ContinuePopupTests.cs` — 광고 시청 콜백 후 5종 복원 + 일일 한도 (5건)
- [ ] **🚨 입력 경로 통합 검증** (`feedback_e2e_input_path_test.md`):
    - [ ] HUD 일시정지 버튼: `Physics2D.OverlapPoint` + `GraphicRaycaster` → PointerEventData → onClick 발화 → PauseManager.Pause 호출 (1건)
    - [ ] 스킬 슬롯 1: 같은 경로로 SkillDeckInputController.SelectSlot(0) 호출 (1건)
    - [ ] 플레이필드 터치 (슬롯 선택 후): `Input.GetTouch(0)` → SkillDeck.Use(0, worldPos) 발화 (1건)

---

## 8. PopupManager — 5종 팝업 템플릿

> `UI_Flow.md §7` 5종 팝업: 확인 / 알림 / 보상 / 가이드 / 설정.

### 8.1 PopupManager 코어

- [ ] `02.Scripts/UI/PopupManager.cs` (싱글턴) — `OpenConfirm(title, message, onConfirm, onCancel)`, `OpenAlert(message, autoClose=3)`, `OpenReward(rewards[])`, `OpenGuide(guideId, message, hideForeverOption=true)`, `OpenSettings()` 5종 API
- [ ] DontDestroyOnLoad — 씬 전환 후에도 유지
- [ ] **스택 관리**: 동시에 여러 팝업 열림 허용 (사운드 설정 중 알림 팝업 등) — sortOrder로 표시 우선순위
- [ ] DOTween Scale 0→1 + Alpha 0→1 (0.25s OutBack)

### 8.2 5종 팝업 프리팹

- [ ] `05.Prefabs/UI/ConfirmPopup.prefab` — 제목 + 메시지 + [확인]/[취소]. 재화 소모 행동 (강화/리셋/구매)에 사용
- [ ] `05.Prefabs/UI/AlertPopup.prefab` — 메시지 + 자동 닫힘 3초 (레벨업/해금/업적)
- [ ] `05.Prefabs/UI/RewardPopup.prefab` — 보상 아이콘 그리드 + 수량 (의뢰 완료/도감 달성/타로 획득) + DOTween Sequence(아이콘 1개씩 0.15s 간격 페이드인)
- [ ] `05.Prefabs/UI/GuidePopup.prefab` — 일러스트 + 가이드 텍스트 + [확인] + [다시 보지 않기] 체크박스 (신규 기믹/시설 첫 방문)
- [ ] `05.Prefabs/UI/SettingsPopup.prefab` — BGM/SFX 슬라이더 + 그래픽 품질 드롭다운 + Haptic 토글 + 접근성 5종 + 계정 연동 + 클라우드 동기화 토글

### 8.3 SettingsPopup 상세

- [ ] BGM/SFX 슬라이더 → `AudioMixer` 볼륨 즉시 적용 + `SaveData.settings.bgmVolume` 즉시 저장
- [ ] 그래픽 품질 — Low/Medium/High/Ultra. `QualitySettings.SetQualityLevel` 호출
- [ ] Haptic — `Vibration.Vibrate(50)` (M8 본 구현, 본 마일스톤은 토글만)
- [ ] 접근성 5종 (`Data_Schema.md §9`): colorBlindMode / touchSensitivity / screenShakeIntensity / flashEffectReduction / largeUIMode — 본 마일스톤은 토글/슬라이더 UI만, 실제 적용은 M8
- [ ] 계정 연동 — Google Play Games 로그인/로그아웃 (`CloudSaveAdapter` 호출)
- [ ] 클라우드 동기화 토글 — `SaveData.settings.cloudSaveEnabled` 갱신

### 8.4 검증

- [ ] `PopupManagerTests.cs` — 5종 API 호출 시 GameObject 인스턴스화 + 콜백 (10건)
- [ ] `PopupStackOrderTests.cs` — 다중 팝업 시 sortOrder 정확 (3건)
- [ ] `SettingsPersistenceTests.cs` — 슬라이더 조작 후 SaveData에 즉시 반영 (5건)

---

## 9. ResultScreen — 클리어/실패 결과 화면

> 등급(S/A/B/C) 판정 + 보상 표시 + 다음 분기.

### 9.1 결과 화면 구성

- [ ] `02.Scripts/UI/ResultScreen.cs`
- [ ] 카메라 ortho=5.625 (Title과 공용 카메라 설정)
- [ ] **클리어 화면**:
    - [ ] 등급 메달 (S/A/B/C) — Kenney `flat_medal*.png` + DOTween Scale 0→1.5→1 (0.5s, 회전 360°)
    - [ ] 등급 텍스트 (S=무지개 그라데이션 / A=금 / B=은 / C=동)
    - [ ] 클리어 시간 "120.5초 / 180초" (남은 시간 비율 막대 추가)
    - [ ] 최대 콤보
    - [ ] 획득 XP / 골드 (등급 보너스 표시 — S=+50%, A=+20%, C=-30%)
    - [ ] 획득 아이템 그리드 (룬/코어 조각/특수 광석 — DOTween Sequence 페이드인)
    - [ ] 등급 보너스 적용 — **GradeSystem.ApplyBonus(rewards, grade)** (M8 본 구현, 본 마일스톤은 단순 배율만)
    - [ ] [다음 스테이지] / [액트맵] / [마을]
- [ ] **실패 화면**:
    - [ ] "시간 초과" 텍스트 (적색 강조)
    - [ ] 획득 XP/골드 30% (부분 보상)
    - [ ] [재도전] (시드 변경 후 `GameManager.LoadStage(newBlueprint)`) / [액트맵]
- [ ] **`GradeSystem` 기본 호출** (M8 본 구현):
    - S: remainingTimeRatio ≥ 0.6, A: ≥ 0.3, B: 0~0.3, C: continueCount > 0 (강제 C)

### 9.2 자동 저장 트리거

- [ ] ResultScreen 진입 직후 `EventBus.Publish(new OnStageCleared(...))` → `SaveSystem.Save()` 자동 호출
- [ ] `StageProgress.acts[N].stages[M].bestGrade`를 최고 등급으로 갱신 (S > A > B > C 비교)
- [ ] `Statistics.totalStagesCleared++`, `highestCombo` 갱신, `fastestBossKillSec` 갱신 (보스전인 경우)

### 9.3 검증

- [ ] `ResultScreenTests.cs` (EditMode) — 등급 매핑 4종 + 보상 배율 (8건)
- [ ] `StageProgressUpdateTests.cs` — 클리어 후 bestGrade 갱신 + bestTimeSec 단축 시만 갱신 (5건)
- [ ] PlayMode 시나리오: Stage 클리어 → ResultScreen → [액트맵] → ActMap 노드 메달 갱신 확인

---

## 10. **[인계: M1 #5]** Stage 카메라 Orthographic Size 정식화

> 2026-05-15 해상도 픽스로 Village ortho=10 / Title-Result ortho=5.625는 확정. **Stage 씬 카메라만 동적 조정 정식화 필요**.

- [ ] `02.Scripts/Stage/StageCameraController.cs` 또는 기존 `CameraController` 확장 (M5)
- [ ] StageBlueprint 진입 시 `FitToStageBounds(SegPlayfieldWidth=16.9, screenAspect=9/16)`
- [ ] 공식: `orthoSize = (SegPlayfieldWidth / 2) / (9/16) ≈ 15.02`
- [ ] **공 추적 모드**: 공이 카메라 시야 상단 30% / 하단 30% 진입 시 카메라 따라감 (ProCamera2D Vertical/Horizontal Smooth)
- [ ] **보스전 줌아웃 ×1.2** — `OnBossSpawned` 구독 (M1 #2 인계, M4에서 이미 처리 여부 확인 후 닫기)
- [ ] **멀티볼 줌아웃 +0.1/공** — `OnMultiBallAdded` 구독 (M1 #1 인계, M3에서 이미 처리 여부 확인 후 닫기)
- [ ] Title/Result/Village/ActMap 카메라는 `Resolution_Spec.md §3.1`과 일치하는지 일괄 점검 (Game View 캡쳐로 확인)

### 10.1 검증

- [ ] `StageCameraFitTests.cs` — orthoSize 15.02 ±0.1 계산 + 9:16 비율 가정 (3건)
- [ ] PlayMode: Title ortho=5.625 / Village ortho=10 / ActMap ortho=10 / Stage 동적 (Game View 4 캡쳐 비교)

---

## 11. Constants.cs 확장

> M7 신규 상수 6개 섹션.

- [ ] **§UI 섹션**:
    - `UIPopupFadeInSec = 0.25f`
    - `UIPopupFadeOutSec = 0.20f`
    - `UIAlertAutoCloseSec = 3.0f`
    - `UIComboMilestonePunchScale = 1.2f`
    - `UIComboMilestonePunchSec = 0.3f`
    - `UISafeAreaTopPx = 60`
    - `UISafeAreaBottomPx = 200`
- [ ] **§Scene 섹션**:
    - `SceneNameTitle = "Title"`
    - `SceneNameVillage = "Village"`
    - `SceneNameActMap = "ActMap"`
    - `SceneNameStage = "Stage"`
    - `SceneNameResult = "Result"` (또는 Stage 내 Canvas 활성화로 처리)
    - `SceneFadeInSec = 0.3f` / `SceneFadeOutSec = 0.3f`
- [ ] **§Save 섹션**:
    - `SaveVersion = "2.0.0"`
    - `SaveFileName = "save.dat"`
    - `RuntimeSnapshotFileName = "runtime.snapshot"`
    - `SaveAutoIntervalSec = 5.0f`
    - `CloudSyncTimeoutSec = 10.0f`
- [ ] **§Pause 섹션**:
    - `PauseAllowBackgroundAutoSave = true`
- [ ] **§Continue 섹션**:
    - `ContinueTimeBonusSec = 30.0f`
    - `ContinueDailyLimit = 3`
    - `ContinueManaRestore = 100`
    - `ContinueGradeClamp = "C"` (string)
- [ ] **§Camera 섹션** (Resolution_Spec.md §3.1 반영):
    - `CameraTitleOrtho = 5.625f`
    - `CameraResultOrtho = 5.625f`
    - `CameraVillageOrtho = 10.0f`
    - `CameraActMapOrtho = 10.0f`
    - `CameraStageOrthoBase = 15.02f` (16.9/2 / (9/16))

---

## 12. 검증 체크리스트

### 12.1 단위 테스트 (EditMode) — 신규 60건 이상 (누적 245건+)

- [ ] `SaveSystemRoundTripTests.cs` (10건+)
- [ ] `SaveSystemTamperingTests.cs` (3건)
- [ ] `SaveSystemVersionMigrationTests.cs` (4건)
- [ ] `SaveAutoTriggerTests.cs` (3건)
- [ ] `SaveMigrationV1ToV2Tests.cs` (5건)
- [ ] `SaltProviderTests.cs` (4건)
- [ ] `SaveEncryptionSaltVersionTests.cs` (3건)
- [ ] `PauseManagerTests.cs` (5건)
- [ ] `RuntimeStageSnapshotRoundTripTests.cs` (4건)
- [ ] `TitleScreenControllerTests.cs` (3건)
- [ ] `ActMapUITests.cs` (8건)
- [ ] `NodeInfoPopupTests.cs` (5건)
- [ ] `InGameHUDTests.cs` (12건)
- [ ] `SkillDeckInputControllerTests.cs` (8건)
- [ ] `FlipperInputBlockedTests.cs` (3건)
- [ ] `ComboMilestoneTests.cs` (5건)
- [ ] `ContinuePopupTests.cs` (5건)
- [ ] `PopupManagerTests.cs` (10건)
- [ ] `PopupStackOrderTests.cs` (3건)
- [ ] `SettingsPersistenceTests.cs` (5건)
- [ ] `ResultScreenTests.cs` (8건)
- [ ] `StageProgressUpdateTests.cs` (5건)
- [ ] `StageCameraFitTests.cs` (3건)
- [ ] `PixelUIBorderImporterTests.cs` — `kenney_pixel-ui-pack/9-Slice/` 하위 모든 PNG의 Border 값이 의도와 일치 (Colored=6/6/6/6, Ancient=8/8/8/8, Outline=4/4/4/4) (3건)
- [ ] `MapTilePaletteTests.cs` — `MapTilePalette.asset`이 4 액트 × {지형/경로/노드 마커 6종} 슬롯을 모두 채우고 있고 빈 슬롯 0건 (4건)
- [ ] `ActMapTilemapBuilderTests.cs` — Act 1~4 각각의 Tilemap 빌드 결과 셀 수 / 노드 마커 30개 좌표 정확 (4건)
- [ ] **마일스톤 6 누적 185건 유지** + 마일스톤 7 신규 130건 = **315건+ 모두 통과** 목표

### 12.2 PlayMode 자동 시뮬레이션 (5건 이상)

| 시나리오 | 기대 결과 |
|---|---|
| Title 진입 → 터치 → Village 전환 | `SaveSystem.HasSave()` 분기 정확, Village 씬 활성 카메라 ortho=10 |
| ActMap 진입 → 노드 선택 → 출격 → Stage 진입 | `GameManager.LoadStage` 호출, StageBlueprint 정상 전달, 카메라 동적 ortho |
| Stage 클리어 → ResultScreen → ActMap 복귀 | `SaveSystem.Save` 1회 자동 호출, `StageProgress.bestGrade` 갱신, ActMap 노드 메달 갱신 |
| Stage 일시정지 → 5초 → 재개 | Time.timeScale 토글, 공 위치/속도 정확 유지, 타이머 정확 이어짐 |
| Stage `OnApplicationPause(true)` → 30초 백그라운드 → 복귀 | `runtime.snapshot` 생성, 공/플리퍼/보스 상태 직렬화, 복귀 시 정확 복원 |
| Stage 시간 초과 → 광고 모킹 → 이어하기 | +30초 / 마나 100 / 콤보 0 / 쿨타임 0 / 보스 HP 유지 / continueCount=1 / 등급 C 클램프 |
| 클라우드 저장 시뮬레이션 (모킹) | 로컬 저장 후 클라우드 업로드 1회, 충돌 발생 시 최신 자동 선택 |
| 변조 감지 시 자동 복원 | save.dat 1바이트 변조 → 로드 시 LoadResult.Tampered → CloudSaveAdapter.Download 호출 → 로컬 덮어쓰기 |
| HUD 입력 통합 검증 | 일시정지 버튼 → PauseManager.Pause / 스킬 슬롯 1 → SelectSlot(0) / 플레이필드 터치 → SkillDeck.Use(0, worldPos) |
| PopupManager 5종 | 5종 팝업 모두 인스턴스화 + 닫기 + 콜백 정상 |

### 12.3 인게임 검증 (사용자 확인 필요)

- [ ] Title 씬 진입 → "터치하여 시작" 펄스 → 화면 터치 → Village 페이드 전환 0.3s
- [ ] Village 씬에서 [출항] (BalloonManager) → ActMap 전환 → 노드 30개 그리드 + 메달 표시
- [ ] ActMap 노드 터치 → NodeInfoPopup → [출격] → Stage 진입 → 인게임 HUD 12종 위젯 모두 표시
- [ ] 인게임 콤보 10/30/50/100 도달 → 콤보 카운터 Scale Punch + 색상 펄스 확인
- [ ] 인게임 스킬 슬롯 1 터치 → 표적 지정 오버레이 표시 → 플레이필드 터치 → 스킬 발동 + 플리퍼 차단 확인
- [ ] 일시정지 버튼 터치 → 일시정지 메뉴 표시 → [계속하기] → 정확 재개
- [ ] 일시정지 메뉴 → [포기] → ConfirmQuitPopup → 예 → Village 복귀
- [ ] 시간 초과 → 이어하기 팝업 → [광고 시청](모킹) → +30초 / 마나 100 / 콤보 0 확인
- [ ] 시간 초과 → 이어하기 팝업 → [포기] → ResultScreen 실패 → [재도전] → 시드 변경 후 재시작
- [ ] 스테이지 클리어 → ResultScreen → 등급 메달(S/A/B/C) DOTween 연출 → [액트맵] → 액트맵 노드 메달 갱신
- [ ] 백그라운드 전환 (홈 버튼) → 30초 대기 → 복귀 → 게임 정확 재개 (공 위치/속도/콤보/타이머)
- [ ] 앱 강제 종료 → 재실행 → "이어서 시작" 확인 → 백그라운드 직전 상태 복원
- [ ] 설정 팝업 → BGM 슬라이더 → 즉시 음량 변경 + 종료 후 재진입 시 유지
- [ ] Google Play Games 로그인 → 다른 기기 로그인 시 진행 상황 동기화 (실기기 검증)

### 12.4 문서 정합성

- [ ] [UI_Flow.md §1~§7](../Design/UI_Flow.md) — 전체 씬 흐름 / 마을 시설 진입 / 액트맵 / 인게임 HUD / 결과 화면 / 팝업 시스템 1:1 반영
- [ ] [Data_Schema.md §1~§11](../Design/Data_Schema.md) — 모든 필드 SaveData/RuntimeStageSnapshot에 매핑
- [ ] [Game_Design_Spec.md §10 메타 게임 시스템](../Design/Game_Design_Spec.md) — 자동 저장 트리거 + 클라우드 저장 + 이어하기 시스템 일치
- [ ] [Resolution_Spec.md §3.1 씬별 카메라 권장 ortho](../Design/Resolution_Spec.md) — Title/Result=5.625, Village=10, ActMap=10, Stage 동적 일치
- [ ] [Resolution_Spec.md §4 UI Canvas 설정](../Design/Resolution_Spec.md) — Reference 1080×1920, Match=0, Safe Area 상단 60px/하단 200px 일치
- [ ] [Implementation_Plan.md §마일스톤 7](Implementation_Plan.md) — M1 #5/#6 + M2 #7/#8/#9/#16 인계 항목 모두 처리
- [ ] **Kenney 라이센스 준수** — `kenney_pixel-ui-pack/License.txt`, `kenney_map-pack/License.txt`, `kenney_medals/License.txt` 모두 CC0 확인 + Credits 화면에 "Sprites by Kenney.nl (CC0)" 표기 + [Map_Tile_Index.md](../Design/Map_Tile_Index.md) 신규 작성 (§0-A.2 액트별 타일 ID 확정 결과)

---

## 13. 후속 마일스톤 인계 사항

> 마일스톤 7 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤(M8 폴리싱)에 넘기는 항목. 후속 마일스톤 진입 시 `[인계: M7]` 표기로 cross-link.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 튜토리얼 (Act 1 Stage 1~3 강제) | **마일스톤 8** | `TutorialManager.cs` 본 구현. M7은 Title → 신규 게임 시 단순 Stage 진입 (튜토리얼 플래그 미적용). 강제 가이드 팝업 시퀀스는 M8 |
| 2 | 결과 화면 등급 보너스 자동 적용 (GradeSystem) | **마일스톤 8** | M7은 단순 배율 적용 (S +50%, A +20%, C -30%). 정식 GradeSystem + XP/골드 자동 합산은 M8 (`GradeSystem.cs` 신규) |
| 3 | 광고 시청 본 구현 (`AdManager.cs`) | **마일스톤 8** | M7은 5초 모킹. AdMob/UnityAds SDK 통합 + 리워드 광고 + 일일 무료 타로카드(여행 상인)는 M8 |
| 4 | 클라우드 충돌 해결 UI | **마일스톤 8** | M7은 자동 최신 선택. 사용자 선택 UI(로컬 vs 클라우드 미리보기)는 M8 |
| 5 | 접근성 5종 본 적용 | **마일스톤 8** | M7은 토글/슬라이더 UI만. colorBlindMode 셰이더 / largeUIMode UI 스케일링 / screenShakeIntensity 카메라 셰이크 본 구현은 M8 |
| 6 | Haptic 본 구현 (`Vibration.Vibrate`) | **마일스톤 8** | M7은 토글만. iOS/Android Native Plugin은 M8 |
| 7 | 일일 로그인 보상 (`DailyLoginManager.cs`) | **마일스톤 8** | 7일 주기, 자정 갱신, 연속 보너스. M7은 SaveData.player.lastLoginDate만 갱신 |
| 8 | 업적 시스템 (`AchievementManager.cs`) | **마일스톤 8** | 전투 6 + 수집 4 + 도전 5 업적. M7은 SaveData.collection.achievements 필드만 정의 |
| 9 | IAP 시스템 (`IAPManager.cs`) | **마일스톤 8** | 월간/시즌 패스 + Unity Purchasing(IAP) 통합 |
| 10 | 사운드 시스템 (BGM/SFX 슬롯 + AudioMixer) | **마일스톤 8** | M7은 SettingsPopup 볼륨 슬라이더만 + AudioMixer 골격. 마을/액트맵/전투 BGM + 재질별 충돌음 + 스킬 속성별 효과음 본 구현은 M8 |
| 11 | 콤보 마일스톤 사운드 / 시각 효과(별 파티클 등) | **마일스톤 8** | M7은 DOTween Scale Punch만 |
| 12 | 보스 도감 UI (12 보스 + 4 엘리트 패턴 기록 + 약점 시각화) | **마일스톤 8** | [인계: M6 #9] M6에서 데이터 추적까지만. UI 본 구현 |
| 13 | 영구 카드 갤러리 / 도감 칭호 표시 | **마일스톤 8** | [인계: M6 #5] |
| 14 | 자체 아트 교체 (NPC 일러스트 / 보스 초상화 / 스킬 아이콘 60종 / Title 로고·캐릭터 일러스트) | **마일스톤 8** | M7은 `kenney_pixel-ui-pack` + `kenney_map-pack` CC0 자산 임시 매핑까지. 아트 파이프라인 확정 후 |
| 21 | ActMap 노드 간 경로 Tilemap 표현 (`mapTile_073~076`, `109~112` 4방향 흙길/포장도로) | **마일스톤 8** | M7은 Line Renderer로 회색/금 그라데이션만. 시각 풍부도 향상은 폴리싱 |
| 15 | Safe Area 자동 패딩 (Notch/Punch Hole 디바이스) | **마일스톤 8** | [인계: Resolution_Spec.md #3] M7은 고정 60px/200px |
| 16 | 태블릿 비율(3:4 등) 대응 — Camera.aspect 동적 처리 | **마일스톤 8 (옵션)** | [인계: Resolution_Spec.md #4] M7은 9:16 고정 |
| 17 | iOS 빌드 검증 — 동일 비율 / Apple Sign-In / iCloud | **마일스톤 8+** | [인계: Resolution_Spec.md #5] M7은 Android만. iOS는 별도 |
| 18 | Sign-In Conflict 흐름 (Google Play Games 다중 계정) | **마일스톤 8** | M7은 자동 최신 선택까지 |
| 19 | Texture Importer 프리셋 자동화 (`SpriteImporterPreset.cs`) | **마일스톤 8** | [인계: M6 #12] M7은 §0-A.1 체크리스트로 수동 확인 |
| 20 | 신화 타로카드 8장 특수 분기(시간 정지 면역 등) | **마일스톤 8** | [인계: M6 #10] M4 보스 코드 분기 삽입 필요 |

---

## 14. 완료 보고 (2026-05-15 구현)

> **상태**: **코어 골격 완료** — 4개 신규 씬 + AES/HMAC SaveSystem + PauseManager + 12종 HUD + 5종 팝업 + 등급 결과 화면 + 30 노드 ActMap.

### 14.1 구현된 산출물

**Core (8 파일 신규/수정)**
- [x] [Constants.cs](../Assets/02.Scripts/Core/Constants.cs) — `§UI`/`§Scene`/`§Save`/`§Pause`/`§Continue`/`§Camera`/`§Kenney M7`/`§ActMap 그리드` 6개 + 콤보 마일스톤 임계값 신규
- [x] [EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) — `OnSceneLoadStart`/`OnSceneLoadComplete`/`OnApplicationPaused`/`OnApplicationResumed`/`OnContinueRequested`/`OnContinueGranted`/`OnSaveStarted`/`OnSaveCompleted`/`OnSaveFailed`/`OnLoadStarted`/`OnLoadCompleted`/`OnCloudSyncStarted`/`OnCloudSyncCompleted`/`OnPopupOpened`/`OnPopupClosed`/`OnComboMilestone`/`OnSkillSlotSelected`/`OnSkillSlotCancelled` 18개 신규
- [x] [GameManager.cs](../Assets/02.Scripts/Core/GameManager.cs) — `LoadTitle`/`LoadVillage`/`LoadActMap`/`LoadStage(blueprint)`/`LoadResult(ctx)` 5종 정식화 + UniTask 페이드 + `PendingStageBlueprint`/`LastStageResult` 전달
- [x] [SaveResult.cs](../Assets/02.Scripts/Core/SaveResult.cs) — `SaveResult`/`LoadResult`/`CloudSyncResult` enum 3종
- [x] [SaveSystem.cs](../Assets/02.Scripts/Core/SaveSystem.cs) — AES-256-CBC + HMAC-SHA256 라운드트립, 5초 인터벌 RequestSave, SemaphoreSlim 동시성, M1V2 마이그레이션, `ResetForTest`/`NowProvider` 주입
- [x] [SaveMigrationV1ToV2.cs](../Assets/02.Scripts/Core/SaveMigrationV1ToV2.cs) — M6 PlayerPrefs 키 11종 → SaveData JSON 1회성 변환
- [x] [CloudSaveAdapter.cs](../Assets/02.Scripts/Core/CloudSaveAdapter.cs) — GPG 스텁(mock remote). 자동 충돌 해결(`ResolveConflict`)
- [x] [SaveAutoTrigger.cs](../Assets/02.Scripts/Core/SaveAutoTrigger.cs) — `OnStageCleared`/`OnBossDefeated`/`OnLevelUp`/`OnCurrencyChanged`/`OnForgeBallChanged`/`OnFlipperUpgraded` 자동 구독 → `RequestSave` (5초 인터벌)
- [x] [PauseManager.cs](../Assets/02.Scripts/Core/PauseManager.cs) — reason 스택 기반 중첩 안전, `Time.timeScale=0`, `OnApplicationPause(true)` → 자동 저장 + 스냅샷
- [x] [RuntimeStageSerializer.cs](../Assets/02.Scripts/Core/RuntimeStageSerializer.cs) — `IPersistableState` 수집 + ball/timer/mana 캡쳐/복원
- [x] [IPersistableState.cs](../Assets/02.Scripts/Core/IPersistableState.cs) — 직렬화 인터페이스
- [x] [Bootstrap.cs](../Assets/02.Scripts/Core/Bootstrap.cs) — `[RuntimeInitializeOnLoadMethod]` 자동 부팅 시 SaveSystem/GameManager/PauseManager/SceneFader/SaveAutoTrigger/PopupManager/PauseMenuUI/ContinueAdPopup EnsureInstance

**Security (4 파일)**
- [x] [ISaltProvider.cs](../Assets/02.Scripts/Security/ISaltProvider.cs) — `byte[] GetAppSalt()` / `string GetSaltVersion()`
- [x] [DebugSaltProvider.cs](../Assets/02.Scripts/Security/DebugSaltProvider.cs) — 에디터/테스트 디버그 솔트 (SHA256 32바이트)
- [x] [RuntimeSaltProvider.cs](../Assets/02.Scripts/Security/RuntimeSaltProvider.cs) — `StreamingAssets/salt.bin` 로드 + `BuildBlob`/`TryParse` (BuildTimeSaltProvider 는 M8 인계)
- [x] [SaveEncryption.cs](../Assets/02.Scripts/Security/SaveEncryption.cs) — `ISaltProvider` 인스턴스 주입 + `EncryptToBytes`/`TryDecryptFromBytes` 바이너리 + 정적 호환 API + Constant-time HMAC 비교 + `DecryptResult` enum

**Data (5 파일)**
- [x] [SaveData.cs](../Assets/02.Scripts/Data/SaveData.cs) — Data_Schema.md §1~§11 매핑. `PlayerSaveData`/`InventorySaveData`/`SkillTreeSaveData`/`StageProgressSaveData`/`VillageSaveData`/`CollectionSaveData`/`QuestsSaveData`/`SettingsSaveData`/`StatisticsSaveData`/`RuntimeStageSnapshot`/`BallSnapshot`/`BossSnapshot` 등
- [x] [UIThemeData.cs](../Assets/02.Scripts/Data/UIThemeData.cs) — primary/secondary/success/warning/danger + 폰트 6단계 + 9-Slice 마진
- [x] [ActMapNodeData.cs](../Assets/02.Scripts/Data/ActMapNodeData.cs) — 노드 유형별 아이콘 슬롯
- [x] [PopupTemplateData.cs](../Assets/02.Scripts/Data/PopupTemplateData.cs) — 5종 프리팹 슬롯 (현재 코드 빌드 fallback 사용)
- [x] [ContinueAdData.cs](../Assets/02.Scripts/Data/ContinueAdData.cs) — AdMob 테스트 ID + 일일 한도
- [x] [MapTilePalette.cs](../Assets/02.Scripts/Data/MapTilePalette.cs) — Act 4종 × {지형/경로/노드 마커 6종}

**UI (10 파일)**
- [x] [SceneFader.cs](../Assets/02.Scripts/UI/SceneFader.cs) — DontDestroyOnLoad 글로벌 페이드 (UniTask)
- [x] [TitleScreenController.cs](../Assets/02.Scripts/UI/TitleScreenController.cs) — 터치 → SaveSystem.HasSave 분기
- [x] [PopupManager.cs](../Assets/02.Scripts/UI/PopupManager.cs) — 5종 API (Confirm/Alert/Reward/Guide/Settings) + DOTween Scale 등장 + sortOrder 스택
- [x] [InGameHUD.cs](../Assets/02.Scripts/UI/InGameHUD.cs) — 12종 위젯 (Pause/Timer/Gold/Grade/Combo/Mana/Skill x4/Boss HP/Target Overlay 등) 코드 빌드
- [x] [SkillDeckInputController.cs](../Assets/02.Scripts/UI/SkillDeckInputController.cs) — 2단계 입력(슬롯 선택 → 표적 터치) + `FlipperController.InputBlocked` 토글
- [x] [PauseMenuUI.cs](../Assets/02.Scripts/UI/PauseMenuUI.cs) — `OnApplicationPaused` 구독, [계속하기]/[설정]/[포기]
- [x] [ContinueAdPopup.cs](../Assets/02.Scripts/UI/ContinueAdPopup.cs) — 5초 광고 모킹 + 마나 100/타이머 +30s/콤보 0 복원 + 일일 한도
- [x] [ResultScreen.cs](../Assets/02.Scripts/UI/ResultScreen.cs) — 등급 메달 DOTween (Scale+Rotate) + `ActProgress.Apply` 확장 + 자동 저장 트리거
- [x] [ActMapUI.cs](../Assets/02.Scripts/UI/ActMapUI.cs) — 30 노드 (5×6) 격자 + 액트 탭 4종 + 뒤로 가기 + `MapTilePalette` 매핑

**Physics/Combat (3 파일 수정)**
- [x] [FlipperController.cs](../Assets/02.Scripts/Physics/FlipperController.cs) — `static bool InputBlocked` 플래그 추가 + OnTouchPerformed 분기
- [x] [ComboSystem.cs](../Assets/02.Scripts/Combat/ComboSystem.cs) — 10/30/50/100 도달 시 `OnComboMilestone` 발행
- [x] [StageTimer.cs](../Assets/02.Scripts/Combat/StageTimer.cs) — `ContinueRestoreTime(seconds)` API 추가

**씬 (4개 신규 + 1 복제)**
- [x] [Title.unity](../Assets/01.Scenes/Title.unity) — ortho=5.625, TitleScreenController + 4 UI ref 모두 SerializedObject 검증 통과
- [x] [ActMap.unity](../Assets/01.Scenes/ActMap.unity) — ortho=10, ActMapUI + 30 노드(일반 14/엘리트 4/보스 3/휴식 3/이벤트 4/히든 2) 자동 빌드 확인
- [x] [Stage.unity](../Assets/01.Scenes/Stage.unity) — Sample.unity 복제 + InGameHUD Canvas 부착 (DebugHud 미존재)
- [x] [Result.unity](../Assets/01.Scenes/Result.unity) — ortho=5.625, ResultScreen + StageResultContext 동작 검증 (등급 S 표시 / 콤보 / XP+Gold 정상)
- [x] **Build Settings 등록**: Title/Village/ActMap/Stage/Result 5개 순서

### 14.2 테스트

- [x] **EditMode 228/228 PASS** (이전 185 + M7 신규 43건)
  - [x] [SaveSystemM7Tests.cs](../Assets/Tests/EditMode/SaveSystemM7Tests.cs) — RoundTrip(2) / NestedList / HasSave / Delete / Tampered(HMAC/IV/Cipher 3종) / VersionDefault / DebugSaltProvider(2) / RuntimeSaltProvider blob(2) / RequestSave 인터벌(3) — 16건
  - [x] [PauseAndComboM7Tests.cs](../Assets/Tests/EditMode/PauseAndComboM7Tests.cs) — Pause(5) / ComboMilestone(4) / InputBlocked(3) — 12건
  - [x] [M7DataAndMigrationTests.cs](../Assets/Tests/EditMode/M7DataAndMigrationTests.cs) — Migration(5) / RuntimeSnapshot(2) / ActProgress(3) / MapTilePalette(2) / Constants(2) / PopupManager(1) — 15건

### 14.3 PlayMode 회석 검증 (4 씬)

- [x] Title — Canvas 5(TitleCanvas/PauseMenuUI/ContinueAdPopup/SceneFader/PopupManager) + Text 5("RPG Pinball"/"마일스톤 7"/"터치하여 시작"/"⚙ 설정"/"📜 크레딧") 모두 활성
- [x] ActMap — currentAct=Act1_Spring, 30 노드 분포 정확
- [x] Result — 등급 S 표시 / 클리어 시간/콤보/XP/Gold 텍스트 / 메달 색상 무지개 / 카메라 ortho 5.625
- [x] Stage — InGameHUD 1개 + 5 Canvas 활성 / ComboSystem RegisterHit ×10 → OnComboMilestone(10) 1회 발행 확인 / PauseManager.Pause(User)+Pause(Popup)+Resume(User) 후 PopupOpen 활성 유지 정상

### 14.4 사용자가 직접 결정·확인할 항목

> 자동 검증으로 닫지 못한 항목.

**[A] 실기 시각/입력 확인** (Game View 캡쳐 도구가 Screen Space Overlay UI 캡쳐 불가, PlayMode 컴포넌트 검증으로만 통과)
- [ ] Title 씬 진입 → "터치하여 시작" 알파 펄스(0.5↔1.0) 시각 확인
- [ ] Title 화면 어디든 터치 → Village 또는 Tutorial(Stage) 전환 0.3s 페이드
- [ ] ActMap 30 노드 클릭 시 `OpenConfirm` 팝업 → [확인] → Stage 진입
- [ ] InGameHUD 콤보 10/30/50/100 도달 시 색상 펄스 + Scale Punch 확인
- [ ] 스킬 슬롯 1 터치 → 표적 지정 오버레이 표시 → 플레이필드 터치 → 스킬 발동
- [ ] 일시정지 ⏸ → [계속하기]/[설정]/[포기] 메뉴 표시
- [ ] 시간 초과 → ContinueAdPopup → [광고 시청] 5초 모킹 → +30초/마나 100/콤보 0/등급 C 클램프

**[B] M8 인계 필요 작업**
- [ ] BuildTimeSaltProvider + `Assets/Editor/SaltBuildProcessor.cs` — Android 빌드 직전 32바이트 랜덤 솔트 생성
- [ ] Google Play Games SDK 실제 통합 (`CloudSaveAdapter` 는 mock)
- [ ] Tilemap 기반 ActMap 지형 자동 배치 (`ActMapTilemapBuilder.cs` + `MapTilePreviewer` EditorWindow)
- [ ] 9-Slice Pixel UI 임포터 자동화 (`Assets/Editor/PixelUIBorderImporter.cs`)
- [ ] 정식 GradeSystem (M7은 등급 메달 표시 + remainingTimeRatio 단순 매핑까지)
- [ ] AdMob/UnityAds 본 통합 (현재 5초 모킹)
- [ ] Notch/Punch Hole Safe Area 동적 패딩 (M7은 60/200 고정)
- [ ] 9-Slice 패널/버튼 배경 — 현재 Kenney 자산 매핑 미적용 (단색 Image fallback 사용 중)
- [ ] `Map_Tile_Index.md` 신규 + `MapTilePalette` 인스턴스 채우기 (현재 SO 클래스만 존재)
- [ ] DOTween Pro CanvasGroup.DOFade 가 본 Asset Store 임포트에 누락 — `DOTween.To` 알파 보간으로 우회 중. 정식 모듈 임포트 권장

**[C] M7 핵심 디자인 결정 필요**
- [ ] `Sample.unity` vs `Stage.unity` 둘 다 존재 — Stage 가 복제본. Sample 폐기 결정 후 git 정리 필요
- [ ] `ProcStage_Test.unity` 도 잔존 — M5 디버그용. 폐기 또는 유지 결정
- [ ] Title 씬 배경: 현재 단색 (네이비). Kenney `mapTile_001.png` 타일링 또는 자체 아트 결정
- [ ] Title 로고 텍스트 → 자체 로고 이미지 교체 시점 (M8 인계 명시)
- [ ] 게임 첫 진입(신규 게임) 시 `LoadTutorial()` 호출됨 — 본 마일스톤은 빈 Stage 진입. 튜토리얼 강제 가이드 시퀀스 도입 시점 (M8 §13 #1)
- [ ] 일시정지 메뉴 → [설정] 진입 — 본 마일스톤은 `OpenSettings()` 가 골격(텍스트 안내)만. 정식 UI 슬라이더/드롭다운 도입 시점

### 14.5 파일 변경 요약 (커밋 직전 점검용)

```
신규: 21개 cs + 4개 .unity + (이번 세션 만든 4 SO 비포함; 인스턴스 미생성)
수정: Constants/EventBus/GameManager/SaveEncryption + ComboSystem/StageTimer/FlipperController
복제: Sample.unity → Stage.unity
빌드 설정: 5씬 정식 등록 (Title/Village/ActMap/Stage/Result)
```
