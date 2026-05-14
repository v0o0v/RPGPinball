# 해상도 사양

> **작성일**: 2026-05-15 (최초 픽스)
> **상태**: 적용 완료 (Player Settings + Game View 프리셋 + Village 씬 카메라 조정)

## 1. 픽스된 해상도

| 항목 | 값 |
|---|---|
| **기준 해상도** | **1080 × 1920** (가로 × 세로) |
| **종횡비** | **9 : 16** (= 0.5625) |
| **화면 방향** | **Portrait (세로형 고정)** |
| **회전 허용** | Portrait **만**. PortraitUpsideDown / Landscape 모두 차단 |
| **타겟 플랫폼** | Android (ARMv7 + ARM64) |
| **풀스크린** | `Android.startInFullscreen = true` |
| **안전 영역** | `renderOutsideSafeArea = false` (노치/펀치홀 영역 회피) |

## 2. Unity 적용 위치

### 2.1 Player Settings
- `defaultScreenWidth = 1080`
- `defaultScreenHeight = 1920`
- `defaultIsFullScreen = true`
- `defaultInterfaceOrientation = UIOrientation.Portrait`
- `allowedAutorotateToPortrait = true`
- `allowedAutorotateToPortraitUpsideDown = false`
- `allowedAutorotateToLandscapeLeft = false`
- `allowedAutorotateToLandscapeRight = false`
- `useAnimatedAutorotation = false`

### 2.2 Game View 프리셋
- 프리셋명: **`RPG Pinball 1080x1920`**
- 그룹: Android
- 타입: FixedResolution
- 추가 위치: `Game View → 좌측 상단 해상도 드롭다운`

## 3. 카메라 좌표계 (월드 단위 U)

세로형 9:16 비율에서 Orthographic 카메라가 화면에 보여주는 월드 영역:

| `orthographicSize` | 화면 높이(U) | 화면 너비(U) |
|---|---|---|
| 5.0 | 10.0 | 5.625 |
| 6.5 | 13.0 | 7.3125 |
| **9.0** | **18.0** | **10.125** |
| **10.0** | **20.0** | **11.25** |
| 12.0 | 24.0 | 13.5 |

> 공식: `screenWidthU = orthographicSize × 2 × (9/16)` / `screenHeightU = orthographicSize × 2`

### 3.1 씬별 카메라 권장 ortho

| 씬 | `orthographicSize` | 화면 너비 | 화면 높이 | 비고 |
|---|---|---|---|---|
| **Village** | **10.0** | 11.25U | 20U | 6개 시설(2열 × 3행) 그리드. 시설 X=±2.5U, Y=±5U |
| **Stage (M5)** | 동적 | 핀볼판 너비 `SegPlayfieldWidth=16.9U` 기준 | 공 위치 추적 | `CameraController.FitToStageBounds` 사용 |
| **Title/Result** | 5.625 | 6.328U | 11.25U | UI 캔버스 위주 |

## 4. UI Canvas 설정

### 4.1 Screen Space - Overlay 캔버스
- **Reference Resolution**: 1080 × 1920
- **Match**: 0 (Width) — 너비 기준으로 스케일링
- **UI Scale Mode**: Scale With Screen Size

### 4.2 Screen Space - Camera 캔버스
- Canvas Plane Distance: 카메라 Far Clip 의 1/2
- Reference Resolution / Match는 위와 동일

### 4.3 Safe Area 권장
- 상단 60px (스코어/통화 HUD)
- 하단 200px (플리퍼 컨트롤·스킬 덱 4슬롯)

## 5. Village 씬 레이아웃 (1080×1920 적용)

```
세로형 화면 (1080×1920, ortho=10 → 11.25 × 20 U)

      X = -2.5            X = +2.5
   ┌─────────────┬─────────────┐
   │ ┌─────────┐ │ ┌─────────┐ │
Y=+5  Forge      │   Enchanter │
   │ │  (red)  │ │ │ (blue)  │ │
   │ └─────────┘ │ └─────────┘ │
   │             │             │
Y= 0  Tavern     │   Astrologer│
   │   (gray)    │   (purple)  │
   │             │             │
Y=-5  Balloon    │   Training  │
   │  (cart)     │  (bridge)   │
   │             │             │
   └─────────────┴─────────────┘
```

- 시설 크기: localScale (2, 2, 1)
- 시설 간격: X 5U / Y 5U
- Ground 타일: 12U × 22U 타일링

## 6. Stage 핀볼판 비율 (M5 기존 값)

- `SegPlayfieldWidth = 16.9U` (가로)
- `SegStageVerticalScreenCount = 3.0` (한 스크린에 들어가는 세로 segment 개수)
- `SegMiddleHeightDefault = 3.5U`

> Stage 카메라는 핀볼판 너비 16.9U를 가로에 맞춰야 하므로:
> `orthoSize = (16.9/2) / (9/16) ≈ 15.02`
> 또는 `FitToStageBounds`로 동적 맞춤. M8 폴리싱에서 정식화.

## 7. 인계 사항

| # | 항목 | 인계 대상 |
|---|---|---|
| 1 | Title / Result / ActMap 씬 카메라 ortho 최종 확정 | M7 |
| 2 | UI Canvas Reference Resolution 1080×1920로 일괄 적용 (M6 디버그 IMGUI는 OnGUI 좌표계 사용) | M7 |
| 3 | Safe Area (Notch/Punch Hole) 자동 패딩 컴포넌트 | M8 |
| 4 | 태블릿 비율(예: 3:4) 대응 — `Camera.aspect` 동적 처리 | M8 (옵션) |
| 5 | iOS 빌드 시 동일 비율 확인 (현재는 Android만) | M8+ |
