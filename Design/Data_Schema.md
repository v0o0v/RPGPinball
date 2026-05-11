# RPG 핀볼 데이터 스키마 정의서

Unity 구현 시 저장/로드에 사용되는 데이터 구조를 정의합니다.
모든 데이터는 JSON 직렬화 형태로 로컬 저장 + Google Play Games 클라우드 동기화됩니다.

---

## 1. 최상위 세이브 데이터

```json
{
  "version": "1.0.0",
  "lastSaveTime": "2026-05-10T01:30:00+09:00",
  "player": { ... },
  "inventory": { ... },
  "skillTree": { ... },
  "stageProgress": { ... },
  "village": { ... },
  "collection": { ... },
  "quests": { ... },
  "settings": { ... },
  "statistics": { ... }
}
```

---

## 2. 플레이어 데이터 (Player)

```json
{
  "playerUID": "string (고유 식별자)",
  "playerName": "string",
  "level": 1,
  "currentXP": 0,
  "totalSP": 0,
  "usedSP": 0,
  "gold": 0,
  "manaCrystal": 0,
  "bossSoul": 0,
  "respecScrollCount": 0,
  "resetCount": 0,
  "adContinueUsedToday": 0,
  "lastLoginDate": "2026-05-10",
  "totalPlayTimeSec": 0
}
```

### 필드 설명

| 필드 | 타입 | 설명 |
|---|---|---|
| level | int (1~100) | 현재 플레이어 레벨 |
| currentXP | int | 현재 레벨에서의 누적 경험치 |
| totalSP | int | 총 획득한 스킬 포인트 |
| usedSP | int | 현재 투자된 스킬 포인트 (리셋 시 0으로) |
| gold | int | 보유 골드 |
| manaCrystal | int | 보유 마나 결정 |
| bossSoul | int | 보유 보스의 영혼 |
| resetCount | int | 스킬 리셋 누적 횟수 (비용 계산용) |
| adContinueUsedToday | int (0~3) | 오늘 사용한 이어하기 횟수 |

---

## 3. 인벤토리 데이터 (Inventory)

```json
{
  "equippedBallMaterial": "wood",
  "unlockedMaterials": ["wood"],
  "mainCore": null,
  "subCores": [null, null],
  "flipperType": "basic",
  "flipperLevel": 1,
  "equippedTarotCards": [null, null, null],
  "equippedConsumables": [null, null],
  "equippedSkillDeck": [null, null, null, null],
  "cores": [ ... ],
  "runes": [ ... ],
  "tarotCards": [ ... ],
  "consumables": [ ... ],
  "specialMaterials": { ... }
}
```

### 코어 아이템 구조

```json
{
  "coreId": "acceleration_core",
  "coreLevel": 1,
  "fragments": 2
}
```

### 룬 아이템 구조

```json
{
  "runeId": "spread_shape",
  "runeGrade": "normal",
  "equippedOnSkill": null
}
```

### 타로카드 구조

```json
{
  "cardId": "mana_start_20",
  "cardGrade": "normal",
  "isPermanent": false,
  "duplicateCount": 3
}
```

### 특수 소재 구조

```json
{
  "ironOre": 5,
  "mithrilFragment": 0,
  "volcanicAsh": 0,
  "springFlowerExtract": 12,
  "pirateCoin": 8,
  "cogwheel": 3,
  "blueprintFragment": 1
}
```

---

## 4. 스킬 트리 데이터 (SkillTree)

```json
{
  "control": {
    "flipperLight1": 3,
    "flipperLight2": 0,
    "elasticBoost": 2,
    "fastDraw": 1,
    "longFlipper": 0,
    "magneticField": 0,
    ...
  },
  "destruction": {
    "steelBall1": 5,
    "steelBall2": 3,
    "charge": 0,
    ...
  },
  "element": {
    "elementAffinity1": 4,
    "elementAffinity2": 2,
    "manaCharge": 3,
    ...
  }
}
```

> 각 스킬의 값은 현재 투자된 레벨(0 = 미해금, 1~MaxLv = 해금 및 레벨). 스킬 ID는 구현 시 enum으로 관리.

---

## 5. 스테이지 진행 데이터 (StageProgress)

```json
{
  "currentAct": 1,
  "maxUnlockedAct": 1,
  "acts": {
    "1": {
      "stages": {
        "1": { "cleared": true, "bestGrade": "S", "bestTimeSec": 65.2 },
        "2": { "cleared": true, "bestGrade": "A", "bestTimeSec": 120.5 },
        "3": { "cleared": false, "bestGrade": null, "bestTimeSec": null },
        ...
      },
      "bossesDefeated": ["giant_venus_flytrap"],
      "hiddenNodesRevealed": [7],
      "eventHistory": [
        { "stageIndex": 5, "eventType": "treasure_room", "reward": "gold_500" },
        { "stageIndex": 15, "eventType": "mystic_altar", "reward": "sp_1" }
      ]
    },
    "2": { ... },
    "3": { ... },
    "4": { ... }
  }
}
```

---

## 6. 마을 시설 데이터 (Village)

```json
{
  "forge": {
    "materialRecipes": ["wood", "steel"],
    "flipperVariant": "basic",
    "flipperUpgradeLevel": 3
  },
  "enchanter": {
    "runeSlots": { ... }
  },
  "tavern": {
    "dailyQuestsAccepted": [...],
    "weeklyQuestAccepted": null,
    "bountyTargets": [...]
  },
  "astrologer": {
    "tarotPullCount": 15,
    "permanentCardsOwned": ["mana_start_20"]
  },
  "balloon": {
    "upgradeLevel": 1
  },
  "trainingGround": {
    "practiceModeBossesUnlocked": ["giant_venus_flytrap"]
  }
}
```

---

## 7. 도감 및 수집 데이터 (Collection)

```json
{
  "gimmickEncounters": {
    "hidden_bumper": { "deathCount": 0, "encounterCount": 5, "resistLevel": 2 },
    "sticky_web": { "deathCount": 3, "encounterCount": 8, "resistLevel": 3 },
    ...
  },
  "totalGimmicksExperienced": 42,
  "achievements": {
    "first_step": { "completed": true, "claimedReward": true },
    "combo_master": { "completed": false, "progress": 38 },
    ...
  },
  "titles": ["봄의 전사"],
  "equippedTitle": "봄의 전사",
  "ballSkins": ["default", "lightning"],
  "equippedBallSkin": "default",
  "flipperEffects": ["default"],
  "equippedFlipperEffect": "default"
}
```

### 기믹 저항력 레벨 계산

```
resistLevel은 deathCount 기반 자동 계산:
  0회 → Level 1 (저항력 0%)
  3회 → Level 2 (저항력 10%)
  7회 → Level 3 (저항력 20%)
 15회 → Level 4 (저항력 30%)
 30회 → Level 5 (저항력 40%)
```

---

## 8. 의뢰 데이터 (Quests)

```json
{
  "dailyQuests": [
    {
      "questId": "flipper_limit_5",
      "description": "플리퍼 5번만 소환하여 클리어",
      "progress": 0,
      "target": 1,
      "completed": false,
      "expiresAt": "2026-05-11T00:00:00+09:00"
    }
  ],
  "weeklyQuest": { ... },
  "bountyBoard": [
    {
      "eliteId": "crystal_golem",
      "actLocation": 3,
      "defeated": false,
      "expiresAt": "2026-05-17T00:00:00+09:00"
    }
  ]
}
```

---

## 9. 설정 데이터 (Settings)

```json
{
  "bgmVolume": 0.8,
  "sfxVolume": 1.0,
  "hapticEnabled": true,
  "graphicsQuality": "high",
  "fpsLimit": 60,
  "showGimmickTips": true,
  "language": "ko",
  "notificationEnabled": true,
  "cloudSaveEnabled": true,
  "accessibility": {
    "colorBlindMode": "none",
    "touchSensitivity": 1.0,
    "screenShakeIntensity": 1.0,
    "flashEffectReduction": false,
    "largeUIMode": false
  }
}
```

### 접근성 옵션 설명

| 필드 | 값 | 설명 |
|---|---|---|
| colorBlindMode | "none" / "protanopia" / "deuteranopia" / "tritanopia" | 색맹 모드 (적록/녹색/청색 색맹 대응) |
| touchSensitivity | 0.5 ~ 2.0 | 터치 감도 배율 |
| screenShakeIntensity | 0.0 ~ 1.0 | 화면 흔들림 강도 (0 = 완전 비활성화) |
| flashEffectReduction | boolean | 강렬한 섬광 효과 감소 |
| largeUIMode | boolean | 터치 버튼/텍스트 확대 모드 |

---

## 10. 통계 데이터 (Statistics)

```json
{
  "totalStagesCleared": 45,
  "totalBossesDefeated": 4,
  "totalDeaths": 128,
  "highestCombo": 87,
  "totalGoldEarned": 125000,
  "totalPlayTimeSec": 36000,
  "fastestBossKillSec": { "giant_venus_flytrap": 62.3 },
  "totalFlipperSpawns": 5420,
  "totalBlockingSuccess": 890
}
```

---

## 11. 인게임 런타임 데이터 (저장 대상 아님)

스테이지 플레이 중에만 메모리에 존재하며, 일시정지/이어하기 시 직렬화가 필요한 데이터입니다.

```json
{
  "stageId": "act1_stage12",
  "seed": 20260510,
  "remainingTimeSec": 142.5,
  "timeRecoveredThisStage": 15.0,
  "manaGauge": 75,
  "comboCount": 32,
  "ballState": {
    "position": [4.5, 8.2],
    "velocity": [3.1, -12.5],
    "angularVelocity": 45.0,
    "currentMaterial": "wood",
    "activeTransformation": "fireball",
    "transformRemainingTime": 8.5
  },
  "multiBalls": [ ... ],
  "bossState": {
    "currentHP": 3500,
    "maxHP": 12000,
    "currentPhase": 2,
    "activeBuffs": [],
    "activeDebuffs": ["armor_crash"]
  },
  "monstersAlive": [ ... ],
  "activatedGimmicks": [ ... ],
  "skillCooldowns": [0.0, 2.5, 0.0, 8.0],
  "consumablesRemaining": [1, 0],
  "droppedItems": [ ... ],
  "stageGrade": "A",
  "continueCount": 0
}
```

---

## 12. 참조 문서

| 항목 | 참조 문서 |
|---|---|
| 재화 종류 및 소모처 | `Game_Design_Spec.md` 섹션 8 |
| 스킬 트리 전체 목록 | `Skill_Tree_Formulas.md` |
| 기믹 저항력 시스템 | `Game_Design_Spec.md` 섹션 3 (점성술사) |
| 스테이지 절차 생성 시드 | `Procedural_Stage_Gen.md` 섹션 9 |
| 물리 엔진 파라미터 | `Physics_Parameters.md` |
