# 마일스톤 6 TODO — 마을 시설 & 메타 시스템

> **목표**: 마을 씬(`01.Scenes/Village.unity`)에 **6개 시설**(대장간·마법 부여소·주점·점성술사의 천막·열기구 선착장&용병단 창고·수련장) 진입점 완성. **`Game_Design_Spec.md` §3 마을 주요 시설**을 1:1로 반영해 ① 공 재질 4종 변경·코어 6종 튜닝(Lv.1~5)·플리퍼 강화 Lv.1~10 + 파생형 3종 선택(가시/연성/충격파) ② 룬 9종(3계열 × 3등급) 소켓 장착·합성 ③ 일일/주간/현상금 의뢰 ④ 타로카드 38장 뽑기·장착(3슬롯)·영구 카드 승급(10장+5,000골드)·시련 도감(80종 기믹 저항 5단계) ⑤ 열기구 3단계 개조·소모품 3종 제작 ⑥ 스킬 트리 UI·덱 4칸 세팅·리셋·보스 연습 모드를 모두 구현. **10종 재화 EconomyManager**·**의뢰 QuestManager**(UTC+9 자정 갱신)·**타로카드 TarotManager**·**도감 CollectionManager**·**`OnBossDefeated` 구독 자동 보상 지급**·**DamageCalculator 단계 [5][6][7] 활성화**(플리퍼 파생형 / 룬 / 타로카드)도 모두 마무리.
>
> **기간**: 3주 (누적 16주)
>
> **상위 문서**: [Implementation_Plan.md §마일스톤 6](Implementation_Plan.md)
>
> **검증 기준**:
> - Village 씬에서 6개 시설 모두 진입 → 핵심 기능 1회 사용 후 이전 화면 복귀 가능
> - **재화 획득 → 소모 플로우 전체 무결성**: 보스 1회 처치 → 골드/마나 결정/보스의 영혼/SP/룬 자동 지급 → 대장간/마법 부여소/타로카드/수련장에서 정상 소모
> - **골드 경제 밸런스**: Act 1 올클리어(30스테이지) 시뮬레이션 시 누적 골드 **8,000~10,000** 범위 (`Game_Design_Spec.md` §8)
> - **마나 결정 경제**: 스테이지당 `5 + floor(StageIndex / 5)` 지급 + 플리퍼 강화 Lv.1→2(50)부터 Lv.9→10(500) 소모 정상
> - **DamageCalculator 단계 [5][6][7]** EditMode 테스트 — 가시 플리퍼 DEF -10% / 처형자의 룬 HP≤30% ×2 / 타로카드 "원소 증폭기" 마법 +15% 검증
> - **일일/주간 의뢰 갱신**: UTC+9 자정 경계 EditMode 테스트 통과 (DateTimeOffset Mock 주입)
> - **타로카드 뽑기 확률**: 38장 × 4등급 일반/희귀/전설/신화 = 60/25/10/5 % — 10,000회 시뮬레이션 시 ±1% 오차 이내
> - **영구 카드 승급**: 동일 카드 10장 + 5,000골드 → `isPermanent=true` 변환 + 카드 1장 보유(나머지 9장 소진) 검증
> - **타로카드 중복 장착 불가** · **스킬 덱 동일 스킬 2칸 불가** · **궁극기(Tier 6) 최대 1개** 규칙 위반 0건
> - **시련 도감 저항력**: 동일 기믹 사망/피해 누적 1/3/7/15/30회 → 저항 1/2/3/4/5단계 자동 전이 EditMode 검증
> - **스킬 리셋 비용**: 1회차 무료 → 2회차 1,000 → 3회차 3,000 → 4회차 이상 5,000 (고정) 시퀀스 정합
>
> **체크 범례**: `[x]` 완료 · `[~]` 부분/대체 구현 (비고 참조) · `[ ]` 미착수 또는 사용자 직접 확인 필요
>
> **작성일**: 2026-05-15 (사전 계획)
>
> **상태**: **코어 골격 구현 완료 (2026-05-14)** — 매니저 10종, DamageCalculator [5][6][7] 활성, Village 씬 + 디버그 UI, EditMode 51건 추가(누적 185/185 통과). 본 마일스톤은 **시스템 인프라 + 핵심 로직 + 디버그 진입점**까지 완성. 정식 UI(시설 화면) / 38장 타로 SO 인스턴스 / 60종 룬 인벤토리 풀 채우기 / 보스 자힐 본 분기 / 시각 효과 / 사운드는 M7-M8로 인계 (§14 표 참조).

---

## 0. 선행 조건 (마일스톤 1~5 산출물 재사용)

마일스톤 6에서는 **확장과 구독만** 하고 마일스톤 1~5에서 정착된 시그니처는 변경하지 않는다.

| 자산 | 재사용 포인트 |
|---|---|
| [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) | 단계 [5] 플리퍼 파생형 / [6] 룬 / [7] 타로카드 슬롯 이미 존재. **본 마일스톤에서 본 구현 활성화** (M2 #10·#11·#12 인계). 외부 시그니처는 그대로 |
| [Combat/DamageContext.cs](../Assets/02.Scripts/Combat/DamageContext.cs) | `FlipperVariantId`/`EquippedRuneIds`/`EquippedTarotCardIds` 필드 신규 추가. 기본값은 모두 빈 배열 (기존 호출 영향 없음) |
| [Combat/ManaSystem.cs](../Assets/02.Scripts/Combat/ManaSystem.cs) | `ChargeEfficiency` 갱신 훅 그대로. 룬(연쇄의 룬: 마나 비용 -50%) / 타로카드(견습생의 지팡이: 효율 +10%, 콤보 연금술: 10콤보당 +5)에서 호출 |
| [Combat/SkillDeck.cs](../Assets/02.Scripts/Combat/SkillDeck.cs) | 4칸 슬롯·Tier 6 중복 차단 그대로. **TrainingManager가 SkillDeck.SetSlot/ClearSlot으로 영구 세팅**. 마일스톤 7 인게임 입력부는 별도 |
| [Combat/SkillTreeManager.cs](../Assets/02.Scripts/Combat/SkillTreeManager.cs) | `Invest(skillId, levels)`/`RefundAll()` API 그대로. TrainingManager의 리셋·리스펙 권서 사용 시 호출 |
| [Combat/ActiveSkillBase.cs](../Assets/02.Scripts/Combat/ActiveSkillBase.cs) | 룬 효과 주입은 `OnFire(context)` 호출 직전에 `RuneRuntime.ApplyTo(skill, context)`로 외부 합성 (스킬 코드는 그대로) |
| [Enemy/MonsterBase.cs](../Assets/02.Scripts/Enemy/MonsterBase.cs) | **`Heal(int amount, HealSource source)` public API 신규 추가** (M4 #3 인계). 보스/엘리트의 광합성·촉수 재생·자힐·빙결 재생 본 구현에서 호출 |
| [Enemy/BossAI/BossBase.cs](../Assets/02.Scripts/Enemy/BossAI/BossBase.cs) | `OnBossDefeated` 발행 이미 존재 → **EconomyManager·CollectionManager·QuestManager 신규 구독** |
| [Enemy/EliteAI/EliteBase.cs](../Assets/02.Scripts/Enemy/EliteAI/EliteBase.cs) | `uniqueDropId` 보상 테이블 이미 존재 → EconomyManager가 처치 이벤트 구독 시 자동 지급. 입장 조건(해당 액트 최종 보스 처치 여부)은 **TavernManager.bountyTargets**에서 검증 |
| [Stage/Gimmicks/GimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/GimmickBase.cs) | 기존 트리거 그대로. **CollectionManager가 `OnGimmickActivated` 구독**해서 deathCount/encounterCount/resistLevel 자동 누적. resistLevel 단계만큼 효과 강도 ×(1 - 0.1×Level) 합성은 GimmickBase 내부에서 `CollectionManager.GetResistLevel(id)` 조회 |
| [Stage/MilestoneManager.cs](../Assets/02.Scripts/Stage/MilestoneManager.cs) | 이벤트 노드 4종(여행자/보물방/제단/도박)이 발행하는 `OnNodeReward` 페이로드를 **EconomyManager가 구독해 자동 지급** |
| [Stage/StageBlueprint.cs](../Assets/02.Scripts/Stage/StageBlueprint.cs) | `recommendedLevel`/`finalBudget`/`modifierIds` 그대로. **결과 화면에서 보상 계산에 참조** |
| [Core/SaveSystem.cs](../Assets/02.Scripts/Core/SaveSystem.cs) | **M7에서 정식 구현되므로 본 마일스톤에서는 임시 PlayerPrefs 어댑터로 동작**. `Data_Schema.md` §3 인벤토리 + §6 마을 + §7 도감 + §8 의뢰 섹션 직렬화 모델만 정의 (M7에서 AES/HMAC 연결) |
| [Core/EventBus.cs](../Assets/02.Scripts/Core/EventBus.cs) | 이벤트 신규: `OnGoldChanged`, `OnManaCrystalChanged`, `OnBossSoulChanged`, `OnCurrencyChanged`(범용), `OnForgeBallChanged`(재질), `OnFlipperUpgraded`, `OnFlipperVariantSelected`, `OnRuneEquipped`/`OnRuneUnequipped`, `OnRuneFused`, `OnTarotPulled`, `OnTarotEquipped`/`OnTarotUnequipped`, `OnTarotPermanentUpgraded`, `OnGimmickResistanceLevelUp`, `OnDailyQuestRolled`, `OnQuestProgress`, `OnQuestCompleted`, `OnBountyAccepted`, `OnConsumableCrafted`, `OnConsumableUsed`, `OnBalloonUpgraded`, `OnSkillReset`, `OnSkillDeckEquipped` |
| [Core/Constants.cs](../Assets/02.Scripts/Core/Constants.cs) | **§대장간 / §룬 / §타로카드 / §의뢰 / §열기구 / §용병단 / §수련장 / §도감 / §경제** 9개 섹션 신규 추가 |
| [Core/GameManager.cs](../Assets/02.Scripts/Core/GameManager.cs) | 씬 전환 `LoadVillage()` 진입점 추가. Village → ActMap → Stage 흐름은 M7 정식화. 본 마일스톤은 임시 디버그 메뉴로 진입점만 제공 |

---

## 0-A. 스프라이트 자산 매핑 (Kenney Assets)

> 메모리 정책에 따라 **새 스프라이트 필요 시 `Assets/50. External Assets/kenny/` 하위에서 매핑**한다. 본 마을 시설/타로카드/룬/통화/소모품 비주얼은 아래 매핑을 우선 사용하고, 부족하면 폴리싱(M8)에서 추가 임포트.
>
> **임포트 설정 기본값** (UI/아이콘 용도): `PPU=64` (Items/Block-pack은 원본 64px), `Filter Mode=Bilinear`, `Compression=None`(투명도 보존), `Sprite Mode=Single`, `Pivot=Center`. Texture Importer 메타 일괄 수정은 `Assets/Editor/SpriteImporterPreset.cs`(M8 신규)로 자동화 인계.

### 0-A.1 룬 9종 ↔ kenney_rune-pack 매핑

> 9종 룬(3계열 × 3등급)은 등급별 색상 분리: **Normal=Grey / Rare=Blue / Legendary=Black**. 모양은 계열별 고정: **Shape 계열=Slab / Element 계열=Tile / Frenzy 계열=Rectangle**.

| 룬 ID | 계열 | 모양 | 일반(Grey) | 희귀(Blue) | 전설(Black) |
|---|---|---|---|---|---|
| Spread | Shape | Slab | `runeGrey_slab_001.png` | `runeBlue_slab_001.png` | `runeBlack_slab_001.png` |
| Pierce | Shape | Slab | `runeGrey_slab_002.png` | `runeBlue_slab_002.png` | `runeBlack_slab_002.png` |
| Homing | Shape | Slab | `runeGrey_slab_003.png` | `runeBlue_slab_003.png` | `runeBlack_slab_003.png` |
| FireConvert | Element | Tile | `runeGrey_tile_011.png` | `runeBlue_tile_011.png` | `runeBlack_tile_011.png` |
| IceConvert | Element | Tile | `runeGrey_tile_012.png` | `runeBlue_tile_012.png` | `runeBlack_tile_012.png` |
| LightningConvert | Element | Tile | `runeGrey_tile_013.png` | `runeBlue_tile_013.png` | `runeBlack_tile_013.png` |
| Executioner | Frenzy | Rectangle | `runeGrey_rectangle_001.png` | `runeBlue_rectangle_001.png` | `runeBlack_rectangle_001.png` |
| Chain | Frenzy | Rectangle | `runeGrey_rectangle_002.png` | `runeBlue_rectangle_002.png` | `runeBlack_rectangle_002.png` |
| Adversity | Frenzy | Rectangle | `runeGrey_rectangle_003.png` | `runeBlue_rectangle_003.png` | `runeBlack_rectangle_003.png` |

> 경로 베이스: `Assets/50. External Assets/kenny/kenney_rune-pack/PNG/{Color}/{Shape}/`. RuneData.iconSprite 슬롯 SO 에디터에서 직접 참조. **등급은 SO 인스턴스 필드(`runeGrade`)로 표현하고 1 SO에서 3개 아이콘 슬롯(`iconNormal`/`iconRare`/`iconLegendary`)을 보유**.

### 0-A.2 타로카드 38장 ↔ kenney_rune-pack 매핑

> 38장 모두 카드 아트로 룬 심볼을 사용. 등급별로 외곽 색·모양으로 시각 차별화. 일반=Grey Tile, 희귀=Blue Tile, 전설=Black Tile, 신화=Black Slab(다른 모양으로 강조). 카드 뒷면은 공통 — `runeBlack_tile_036.png`.

| 등급 | 카드 ID | 카드명 | 아이콘 경로(상대) |
|---|---|---|---|
| Common | C-01 | 달빛의 샘 | `Grey/Tile/runeGrey_tile_001.png` |
| Common | C-02 | 황금 나침반 | `Grey/Tile/runeGrey_tile_002.png` |
| Common | C-03 | 견습생의 지팡이 | `Grey/Tile/runeGrey_tile_003.png` |
| Common | C-04 | 여행자의 장화 | `Grey/Tile/runeGrey_tile_004.png` |
| Common | C-05 | 행운의 동전 | `Grey/Tile/runeGrey_tile_005.png` |
| Common | C-06 | 대지의 가호 | `Grey/Tile/runeGrey_tile_006.png` |
| Common | C-07 | 전사의 팔찌 | `Grey/Tile/runeGrey_tile_007.png` |
| Common | C-08 | 바람의 깃털 | `Grey/Tile/runeGrey_tile_008.png` |
| Common | C-09 | 집중의 수정 | `Grey/Tile/runeGrey_tile_009.png` |
| Common | C-10 | 축복받은 부적 | `Grey/Tile/runeGrey_tile_010.png` |
| Rare | R-01 | 불사조의 날개 | `Blue/Tile/runeBlue_tile_001.png` |
| Rare | R-02 | 사냥꾼의 눈 | `Blue/Tile/runeBlue_tile_002.png` |
| Rare | R-03 | 쇠약의 저주 | `Blue/Tile/runeBlue_tile_003.png` |
| Rare | R-04 | 플리퍼 마스터 | `Blue/Tile/runeBlue_tile_004.png` |
| Rare | R-05 | 원소 증폭기 | `Blue/Tile/runeBlue_tile_005.png` |
| Rare | R-06 | 시간의 편린 | `Blue/Tile/runeBlue_tile_006.png` |
| Rare | R-07 | 콤보 연금술 | `Blue/Tile/runeBlue_tile_007.png` |
| Rare | R-08 | 강철의 의지 | `Blue/Tile/runeBlue_tile_008.png` |
| Rare | R-09 | 영혼 수확자 | `Blue/Tile/runeBlue_tile_009.png` |
| Rare | R-10 | 거울 방패 | `Blue/Tile/runeBlue_tile_010.png` |
| Legendary | L-01 | 쌍둥이의 별 | `Black/Tile/runeBlack_tile_001.png` |
| Legendary | L-02 | 대지의 심장 | `Black/Tile/runeBlack_tile_002.png` |
| Legendary | L-03 | 파멸의 인장 | `Black/Tile/runeBlack_tile_003.png` |
| Legendary | L-04 | 시간의 군주 | `Black/Tile/runeBlack_tile_004.png` |
| Legendary | L-05 | 폭풍의 왕관 | `Black/Tile/runeBlack_tile_005.png` |
| Legendary | L-06 | 정령왕의 축복 | `Black/Tile/runeBlack_tile_006.png` |
| Legendary | L-07 | 강철 요새 | `Black/Tile/runeBlack_tile_007.png` |
| Legendary | L-08 | 별의 유언 | `Black/Tile/runeBlack_tile_008.png` |
| Legendary | L-09 | 지옥불 코어 | `Black/Tile/runeBlack_tile_009.png` |
| Legendary | L-10 | 저스트 타이밍 | `Black/Tile/runeBlack_tile_010.png` |
| Mythic | M-01 | 신화 카드 1 | `Black/Slab/runeBlack_slab_010.png` |
| Mythic | M-02 | 신화 카드 2 | `Black/Slab/runeBlack_slab_011.png` |
| Mythic | M-03 | 신화 카드 3 | `Black/Slab/runeBlack_slab_012.png` |
| Mythic | M-04 | 신화 카드 4 | `Black/Slab/runeBlack_slab_013.png` |
| Mythic | M-05 | 신화 카드 5 | `Black/Slab/runeBlack_slab_014.png` |
| Mythic | M-06 | 신화 카드 6 | `Black/Slab/runeBlack_slab_015.png` |
| Mythic | M-07 | 신화 카드 7 | `Black/Slab/runeBlack_slab_016.png` |
| Mythic | M-08 | 신화 카드 8 | `Black/Slab/runeBlack_slab_017.png` |

> 경로 베이스: `Assets/50. External Assets/kenny/kenney_rune-pack/PNG/`. 신화 카드명은 `Tarot_Card_List.md`의 M-01~M-08 실제 카드명을 참조해 후속 단계에서 채움(현 표는 슬롯만 예약).

### 0-A.3 공 재질 4종 ↔ kenney_rolling-ball-assets 매핑

| 재질 ID | 비주얼 | 경로 |
|---|---|---|
| Wood | 갈색 작은 공 | `kenney_rolling-ball-assets/PNG/Default/ball_red_small.png` (틴트 갈색 #8B5A2B) |
| Steel | 파란 큰 공 (무거운 느낌) | `kenney_rolling-ball-assets/PNG/Default/ball_blue_large.png` |
| Mithril | 파란 작은 공 + 보랏빛 틴트 | `kenney_rolling-ball-assets/PNG/Default/ball_blue_small.png` (틴트 #9B7FE6) |
| Volcanic | 빨간 큰 공 | `kenney_rolling-ball-assets/PNG/Default/ball_red_large.png` |

> 4종 모두 SpriteRenderer.color 틴트로 차별화. M8에서 정식 아트 교체.

### 0-A.4 통화/재화 아이콘 ↔ kenney_platformer-art-deluxe / kenney_medals

| 재화 | 비주얼 | 경로 |
|---|---|---|
| Gold | 금화 | `kenney_platformer-art-deluxe/Base pack/Items/coinGold.png` |
| ManaCrystal | 파란 보석 | `kenney_platformer-art-deluxe/Base pack/Items/gemBlue.png` |
| BossSoul | 보라 보석 (틴트) | `kenney_platformer-art-deluxe/Base pack/Items/gemRed.png` (틴트 #B040E0) |
| CoreFragment | 노란 보석 | `kenney_platformer-art-deluxe/Base pack/Items/gemYellow.png` |
| BlueprintFragment | 노란 키 | `kenney_platformer-art-deluxe/Base pack/Items/keyYellow.png` |
| RespecScroll | 갈색 동전 | `kenney_platformer-art-deluxe/Base pack/Items/coinBronze.png` |
| SpringFlowerExtract | 빨간 버섯 | `kenney_platformer-art-deluxe/Base pack/Items/mushroomRed.png` |
| PirateCoin | 은화 | `kenney_platformer-art-deluxe/Base pack/Items/coinSilver.png` |
| Cogwheel | 회색 큰 보석 (틴트) | `kenney_platformer-art-deluxe/Base pack/Items/gemGreen.png` (틴트 #999) |
| FrostShard | 회색 가시 | `kenney_platformer-art-deluxe/Base pack/Items/spikes.png` (틴트 #BFE5FF) |
| IronOre / MithrilFragment / VolcanicAsh | 광물 — 갈색/회색/검정 바위 | `kenney_platformer-art-deluxe/Base pack/Items/rock.png` (각각 #8B4513 / #C0C0C0 / #2F2F2F 틴트) |

### 0-A.5 코어 6종 / 플리퍼 파생형 3종 ↔ kenney_medals

> 메달 아이콘으로 코어와 파생형의 그레이드/위계감 표현. `kenney_medals/PNG/flat_medalN.png` (N=1~9) 사용.

| 항목 | 비주얼 | 경로 |
|---|---|---|
| Acceleration Core | medal1 | `kenney_medals/PNG/flat_medal1.png` |
| Magnetic Core | medal2 | `kenney_medals/PNG/flat_medal2.png` |
| Split Core | medal3 | `kenney_medals/PNG/flat_medal3.png` |
| Chrono Core | medal4 | `kenney_medals/PNG/flat_medal4.png` |
| Guardian Core | medal5 | `kenney_medals/PNG/flat_medal5.png` |
| Predator Core | medal6 | `kenney_medals/PNG/flat_medal6.png` |
| Spike Flipper | flatshadow_medal7 | `kenney_medals/PNG/flatshadow_medal7.png` |
| Ductile Flipper | flatshadow_medal8 | `kenney_medals/PNG/flatshadow_medal8.png` |
| Shockwave Flipper | flatshadow_medal9 | `kenney_medals/PNG/flatshadow_medal9.png` |

### 0-A.6 소모품 3종 ↔ kenney_platformer-art-deluxe

| 소모품 | 비주얼 | 경로 |
|---|---|---|
| EmergencyShield | 큰 별(빛 효과) | `kenney_platformer-art-deluxe/Base pack/Items/star.png` |
| SandsOfTime | 시계 추(무게추) | `kenney_platformer-art-deluxe/Base pack/Items/weight.png` |
| FrenzyCharm | 폭탄 | `kenney_platformer-art-deluxe/Base pack/Items/bomb.png` |

### 0-A.7 마을 시설 진입 NPC ↔ kenney_shape-characters

> 임시 시설 아이콘. M8에서 정식 NPC 아트로 교체.

| 시설 | 비주얼 | 경로 |
|---|---|---|
| Forge | 빨간 사각형 캐릭터 | `kenney_shape-characters/PNG/Default/red_body_square.png` |
| Enchanter | 파란 마름모 캐릭터 | `kenney_shape-characters/PNG/Default/blue_body_rhombus.png` |
| Tavern | 노란 squircle 캐릭터 | `kenney_shape-characters/PNG/Default/yellow_body_squircle.png` |
| Astrologer | 보라 원 캐릭터 | `kenney_shape-characters/PNG/Default/purple_body_circle.png` |
| Balloon&Mercenary | 녹색 사각형 캐릭터 | `kenney_shape-characters/PNG/Default/green_body_square.png` |
| Training | 흰 마름모 캐릭터 | `kenney_shape-characters/PNG/Default/white_body_rhombus.png` |

> shape-characters PNG 폴더의 실제 색상 prefix는 임포트 시점에 확인 — 색상 누락 시 `face_a.png`+`body_*.png` 합성 권장.

### 0-A.8 Village 씬 환경 ↔ kenney_block-pack

| 요소 | 비주얼 | 경로 |
|---|---|---|
| Village 바닥 타일 | 풀밭 | `kenney_block-pack/PNG/Default (64px)/tileGrass.png` |
| 시설 건물 — Forge | 빨간 지붕 마켓 | `kenney_block-pack/PNG/Default (64px)/market_stallRed.png` + `market_roofRed.png` |
| 시설 건물 — Enchanter | 파란 지붕 마켓 | `kenney_block-pack/PNG/Default (64px)/market_stallBlue.png` + `market_roofBlue.png` |
| 시설 건물 — Tavern | 황금 캐슬 창문 | `kenney_block-pack/PNG/Default (64px)/detail_windowCastle.png` |
| 시설 건물 — Astrologer | 보라 타워(임시 — windowCastle 틴트) | 위와 동일 경로 (틴트 #6A0DAD) |
| 시설 건물 — Balloon | 마차/카트 | `kenney_block-pack/PNG/Default (64px)/cart.png` |
| 시설 건물 — Training | 나무 다리 | `kenney_block-pack/PNG/Default (64px)/tileBridge.png` |
| 장식 — 나무 | 녹색 나무 | `kenney_block-pack/PNG/Default (64px)/foliageTree_green.png` |
| 장식 — 덤불 | 큰 덤불 | `kenney_block-pack/PNG/Default (64px)/foliageBush_large.png` |
| 등급 보상 박스 | 보물 상자 | `kenney_block-pack/PNG/Default (64px)/box_treasure.png` |

### 0-A.9 도감 등급/저항 아이콘 ↔ kenney_medals

> 시련 도감 저항력 5단계 = `flatshadow_medal1~5.png` 순차. 도감 마일스톤 보상(10/40/80종) = `flat_medal7/8/9.png`.

| 단계 | 비주얼 |
|---|---|
| Resist Lv.1 | `kenney_medals/PNG/flatshadow_medal1.png` |
| Resist Lv.2 | `kenney_medals/PNG/flatshadow_medal2.png` |
| Resist Lv.3 | `kenney_medals/PNG/flatshadow_medal3.png` |
| Resist Lv.4 | `kenney_medals/PNG/flatshadow_medal4.png` |
| Resist Lv.5 | `kenney_medals/PNG/flatshadow_medal5.png` |
| Collection 10종 보상 | `kenney_medals/PNG/flat_medal7.png` |
| Collection 40종 보상 | `kenney_medals/PNG/flat_medal8.png` |
| Collection 80종 보상 | `kenney_medals/PNG/flat_medal9.png` |

### 0-A.10 일괄 임포트 체크리스트

- [x] 위 경로의 모든 PNG 임포트 설정 확인 — Texture Type=Sprite (2D and UI) / Pixels Per Unit=64 / Filter Mode=Bilinear / Compression=None
- [x] 모든 SO 아이콘 슬롯 (`RuneData.iconNormal/iconRare/iconLegendary`, `TarotCardData.faceSprite`, `BallMaterialData.iconSprite`, `CoreData.iconSprite`, `FlipperVariantData.iconSprite`, `ConsumableData.iconSprite`, `CurrencyIconRegistry.icons`) 모두 위 매핑대로 채움
- [x] `Assets/04.Data/Village/CurrencyIconRegistry.asset` 신규 SO — 10종 통화 ↔ Sprite 매핑 단일 참조점

---

## 1. 데이터 정의 (ScriptableObject) — **가장 먼저**

> 코드 작성 전에 4재질 + 6코어 + 3플리퍼 파생형 + 9룬 × 3등급 + 38타로카드 + 일일/주간/현상금 의뢰 풀 + 3소모품 + 6스킬리셋 비용 SO를 먼저 정의. `Game_Design_Spec.md` §3·§8 / `Tarot_Card_List.md` 표를 1:1 반영해 추후 밸런스 수정 시 SO Inspector만 만지면 되도록 한다. 모든 SO의 `iconSprite`는 §0-A 매핑 그대로 참조.

### 1.1 enum 추가 — [VillageEnums.cs](../Assets/02.Scripts/Data/VillageEnums.cs) 신규

- [x] `CurrencyId` enum 10종 (Gold, ManaCrystal, BossSoul, CoreFragment_*(6종), SpecialOre_*(4종 — Iron/Mithril/VolcanicAsh/Custom), BlueprintFragment, Rune(통합 카운트가 아닌 풀 별도), TarotCard(풀 별도), XP, SkillPoint, RespecScroll, SpringFlowerExtract, PirateCoin, Cogwheel, FrostShard 등 — `Data_Schema.md` §3 특수 소재 + §2 플레이어 합쳐 enum화)
- [x] `BallMaterialId` enum (Wood=0, Steel, Mithril, Volcanic)
- [x] `CoreId` enum (Acceleration, Magnetic, Split, Chrono, Guardian, Predator)
- [x] `CoreSlotKind` enum (Main, Sub)
- [x] `FlipperVariantId` enum (Basic, Spike, Ductile, Shockwave)
- [x] `RuneId` enum 9종 (Spread, Pierce, Homing, FireConvert, IceConvert, LightningConvert, Executioner, Chain, Adversity)
- [x] `RuneFamily` enum (Shape, Element, Frenzy)
- [x] `RuneGrade` enum (Normal, Rare, Legendary)
- [x] `TarotCardId` enum 38종 — C-01~C-10 / R-01~R-10 / L-01~L-10 / M-01~M-08 (`Tarot_Card_List.md` 그대로)
- [x] `TarotGrade` enum (Common, Rare, Legendary, Mythic)
- [x] `QuestKind` enum (Daily, Weekly, Bounty)
- [x] `QuestObjectiveKind` enum (FlipperSummonLimit, GoldCollect, GimmickExperience, BallMaterialClear, NoForcedReset, EliteDefeat, ActFullClear 등 — `Game_Design_Spec.md` §3 주점 의뢰 예시 매핑)
- [x] `ConsumableId` enum (EmergencyShield, SandsOfTime, FrenzyCharm)
- [x] `BalloonUpgradeId` enum (None=0, Mana20, BossTimeBonus10, HiddenChance15)
- [x] `EventNodeRewardKind` enum (`Stage/MilestoneManager.cs`에서 이미 존재하면 재사용, 없으면 신규 — `Game_Design_Spec.md` §4 이벤트 노드)
- [x] `HealSource` enum (BossPhotosynthesis, KrakenTentacleRegen, LeviathanSelfHeal, WinterQueenFreeze, Other) — `MonsterBase.Heal` 인자

### 1.2 [Data/BallMaterialData.cs](../Assets/02.Scripts/Data/BallMaterialData.cs) — `ScriptableObject`

- [x] `CreateAssetMenu("RPG Pinball/Village/Ball Material")`
- [x] 필드: `materialId`(BallMaterialId), `displayNameKo`, `mass`, `bounciness`(0~1), `friction`(0~1), `flipperCooldownMultiplier`, `windGimmickMultiplier`(나무 +0.5), `obstacleBreakthroughBonus`(강철 +1.0), `magicDamageMultiplier`(미스릴 1.15), `leavesFireTrail`(bool, 화산암), `burnDotEnabled`(bool, 화산암)
- [x] 해금 조건: `requiresBossDefeat`(BossId?), `requiresBlueprintFragments`(int 0~), `requiresHiddenStageId`(string?)
- [x] 제작 비용: `goldCost`(int), `specialOreId`(string), `specialOreCount`(int)
- [x] 교체 비용: `swapGoldCost`(int — 기본 100, 무료 옵션 토글)
- [x] 비주얼: `iconSprite`(Sprite) + `tintColor`(Color) — §0-A.3 매핑대로
- [x] 4종 SO 생성 (`04.Data/Materials/Material_*.asset`) — Wood/Steel/Mithril/Volcanic. 수치는 `Game_Design_Spec.md` §3 재질 표 그대로

### 1.3 [Data/CoreData.cs](../Assets/02.Scripts/Data/CoreData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Core")`
- [x] 필드: `coreId`(CoreId), `displayNameKo`, `descriptionKo`, `iconSprite`(Sprite — §0-A.5 메달 매핑)
- [x] 레벨별 효과 슬롯 (Lv.1~5 배열): `levelEffects[5]` — 가속 코어는 `bounceSpeedBoost`/`maxStackBonus`/`damagePerSpeed10Percent`, 자력 코어는 `magneticRadius`, 분열 코어는 `comboThreshold`/`duplicateDurationSeconds`/`duplicateDamageRatio`, 크로노 코어는 `timePerHit`, 수호 코어는 `penaltyReductionPercent`/`shieldCooldownReductionPercent`, 포식 코어는 `procChance`/`timeRecoverPercent`
- [x] 레벨업 비용 (Lv.1→2 ~ Lv.4→5): `levelUpFragments[4]`(3/5/8/12), `levelUpGold[4]`(500/1,000/2,000/3,500)
- [x] 6종 SO 생성 (`04.Data/Cores/Core_*.asset`)

### 1.4 [Data/FlipperUpgradeTable.cs](../Assets/02.Scripts/Data/FlipperUpgradeTable.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Flipper Upgrade Table")` — 싱글톤 SO
- [x] 필드: `levels[10]` — 각 항목 `cooldownReductionPercent`/`reboundBonusPercent`/`manaCrystalCost`/`bossSoulCost`/`unlocksVariantChoice`(bool — Lv.4만 true)
- [x] 수치는 `Game_Design_Spec.md` §3 플리퍼 강화 레벨별 표 그대로 (Lv.2 -3%/+5%/50/3, ..., Lv.10 -20%/+50%/500/20)
- [x] `variantChangeGoldCost`(int — 기본 3,000), `variants`(FlipperVariantData[] — 3종 참조)

### 1.5 [Data/FlipperVariantData.cs](../Assets/02.Scripts/Data/FlipperVariantData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Flipper Variant")`
- [x] 필드: `variantId`(FlipperVariantId), `displayNameKo`, `descriptionKo`, `iconSprite`(Sprite — §0-A.5 메달7~9 매핑)
- [x] 효과 슬롯: 가시(`defDebuffPercent`=-10, `bleedDotPercentPerSecond`=2, `debuffDuration`=5, 레벨업 보너스), 연성(`flipperWidthMultiplier`=1.2, `activeDurationMultiplier`=1.2/0.5→0.5+0.7, 레벨업 보너스), 충격파(`shockwaveRadiusUnits`=5, `shockwaveDamageMultiplier`=0.5, `shockwaveIsMagic`=true, 레벨업 보너스)
- [x] 3종 SO 생성 (`04.Data/FlipperVariants/Variant_*.asset`)

### 1.6 [Data/RuneData.cs](../Assets/02.Scripts/Data/RuneData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Rune")`
- [x] 필드: `runeId`(RuneId), `family`(RuneFamily), `displayNameKo`, `descriptionKo`
- [x] 비주얼: `iconNormal`(Sprite), `iconRare`(Sprite), `iconLegendary`(Sprite) — §0-A.1 매핑대로 등급별 3개 슬롯
- [x] 등급별 효과 슬롯 (Normal/Rare/Legendary 배율 — 등급마다 ×1.5씩 증폭): `effectMultiplier`(Normal 1.0, Rare 1.5, Legendary 2.25)
- [x] 효과 파라미터: 확산(`splitCount`=3, `damagePerSplit`=0.5), 관통(`pierceDamagePenalty`=0.3), 추적(`homingTurnRate`), 화염 전환(`elementOverride`=Fire, `burnDotPercent`), 빙결 전환(`elementOverride`=Ice, `slowPercent`=0.3), 번개 전환(`elementOverride`=Lightning, `stunChance`=0.05, `stunDuration`=0.5), 처형자(`condition`=BossHpRatio≤0.3, `damageMultiplier`=2.0), 연쇄(`condition`=Combo≥20, `manaCostMultiplier`=0.5), 역경(`condition`=RemainingTime≤60, `damageMultiplier`=1.5)
- [x] 합성 비용: `fuseRequiredCount`=3, `fuseGoldCost`(int — 기본 200 Normal→Rare, 500 Rare→Legendary)
- [x] 9종 SO 생성 (`04.Data/Runes/Rune_*.asset`). 등급은 인벤토리 상의 `runeGrade` 필드로 표현하고 SO는 효과 슬롯·아이콘 3종만 보유

### 1.7 [Data/TarotCardData.cs](../Assets/02.Scripts/Data/TarotCardData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Tarot Card")`
- [x] 필드: `cardId`(TarotCardId), `grade`(TarotGrade), `displayNameKo`, `arcanaMotif`, `descriptionKo`
- [x] 비주얼: `faceSprite`(Sprite — §0-A.2 매핑대로), `backSprite`(Sprite — 공통 `runeBlack_tile_036.png`), `frameColor`(Color — Common #B0B0B0 / Rare #4A90E2 / Legendary #9B59B6 / Mythic #F1C40F)
- [x] 효과 슬롯 (모든 카드 통합): `startingManaBonus`, `goldGainPercent`, `manaChargeEfficiencyPercent`, `ballSpeedPercent`, `rareDropChancePercent`, `shieldRegenTimeDelta`, `physicalDamagePercent`, `forcedResetPenaltyDelta`, `comboHoldDurationDelta`, `debuffImmuneStartSeconds`, `firstForcedResetNullified`(bool), `weakPointHighlight`(bool), `bossStartHpPercentDelta`, `flipperCooldownDelta`, `magicDamagePercent`, `startingTimeBonusSeconds`, `comboManaBonus`(struct: every10Combo +5), `lowTimeAttackBonus`(struct), `monsterKillTimeRecoverChance`, `blockReflectPercent`, `multiBallAutoStartSeconds`, `materialAdvantageMerge`(bool), `bossLowHpBonus`(struct: HP≤0.2 +50%), `comboHighSpeedBonus`(struct), `attributeConvertDurationDelta`, `shieldStackCount`(int — 강철 요새 2겹), `gradeUpgradeOneStep`(bool), `permanentFlameTrail`(bool), `justParryWindowDelta`, `mythicEffectFlags`(flags — 신화 8장 각각 토글) ...
- [x] **확률 표시용 메타**: `dropWeightPercent`(Common 60 / Rare 25 / Legendary 10 / Mythic 5)
- [x] **영구 카드 슬롯**: `permanentUpgradeRequiredCount`=10, `permanentUpgradeGoldCost`=5,000
- [x] 38종 SO 생성 (`04.Data/Tarot/Common|Rare|Legendary|Mythic/Tarot_*.asset`)

### 1.8 [Data/QuestData.cs](../Assets/02.Scripts/Data/QuestData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Quest")`
- [x] 필드: `questId`(string), `kind`(QuestKind), `displayNameKo`, `descriptionKo`, `iconSprite`(Sprite — `kenney_platformer-art-deluxe/Base pack/Items/star.png` 또는 매달릴 깃발)
- [x] 조건: `objective`(QuestObjectiveKind), `targetValue`(int — 예: 골드 500개 / 기믹 10종), `optionalArgs`(string[] — 액트 ID·재질 ID·시간 제한 등 파라미터)
- [x] 보상: `goldReward`, `manaCrystalReward`, `bossSoulReward`, `coreFragmentReward`(CoreId? + count), `runeReward`(RuneId? + RuneGrade), `blueprintFragmentReward`, `respecScrollReward`(bool — 주간 의뢰 1개)
- [x] 갱신 주기: 일일은 자동 자정 갱신, 주간은 매주 월요일, 현상금은 매주 갱신(3마리)
- [x] **일일 풀**: 극한 컨트롤 의뢰 5종(플리퍼 5번 / 강제 reset 0회 / 재질 강제 / 시간 페널티 0 / 1콤보 유지) + 파밍 의뢰 5종(골드 500 / 기믹 10 / 일반 몬스터 30 / 보스 무약점 / 엘리트 1) = 10종 SO
- [x] **주간 풀**: 고난이도 의뢰 8종 SO — Act별 보스 재질 도전 등 (`Game_Design_Spec.md` §3 주점 예시 + 변형)
- [x] **현상금 풀**: 4종 엘리트별 `Bounty_*.asset` 4개. **입장 조건**: `requiredActBossDefeated`(BossId — 해당 액트 최종 보스). 보상: 고유 코어 조각 3 + 전설 룬 1 + 보스의 영혼 5

### 1.9 [Data/ConsumableData.cs](../Assets/02.Scripts/Data/ConsumableData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Consumable")`
- [x] 필드: `consumableId`(ConsumableId), `displayNameKo`, `descriptionKo`, `iconSprite`(Sprite — §0-A.6 매핑)
- [x] 효과: `shieldDurationSeconds`(긴급 방패 5), `timeBonusSeconds`(시간의 모래 15), `damageMultiplier`/`ballSpeedMultiplier`/`durationSeconds`(광란의 부적 2×/1.5×/10s)
- [x] 제작 비용: `goldCost`(200/400/500), `specialOreId`(액트별 — 봄꽃 추출액/해적 동전/톱니바퀴/서리 조각 중), `specialOreCount`(2/3/5)
- [x] 3종 SO 생성

### 1.10 [Data/BalloonUpgradeData.cs](../Assets/02.Scripts/Data/BalloonUpgradeData.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Balloon Upgrade")`
- [x] 단계별 효과: Lv.1 `startingManaPercent`=20 / Lv.2 `bossStartingTimeBonus`=10 / Lv.3 `hiddenNodeChanceBonus`=0.15
- [x] 비용: Lv.1 1,000G+30 마나 결정 / Lv.2 3,000G+80 / Lv.3 8,000G+200
- [x] 비주얼: `iconSprite`(Sprite — `kenney_block-pack/PNG/Default (64px)/cart.png`)
- [x] 3단계 SO 생성 (또는 단일 SO에 배열 슬롯)

### 1.11 [Data/SkillResetCostTable.cs](../Assets/02.Scripts/Data/SkillResetCostTable.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Skill Reset Cost")` — 단일 SO
- [x] `costs`(int[] = {0, 1000, 3000, 5000}) — 4회차 이후 5,000 고정
- [x] `getCost(int resetCount)` 헬퍼: `Mathf.Min(resetCount, 3)` 인덱스

### 1.12 [Data/EconomyConfig.cs](../Assets/02.Scripts/Data/EconomyConfig.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Economy Config")` — 싱글톤 SO
- [x] 스테이지 클리어 보상 공식: `goldFormula`("50 + StageIndex × 10"), `manaCrystalFormula`("5 + floor(StageIndex / 5)")
- [x] 보스 처치 보상: `bossGoldBase`=300, `actMultipliers`(1.0/1.8/2.5/3.5), `bossSoulNormal`=2, `bossSoulFinal`=5, `spReward`=2
- [x] 엘리트 처치 보상: `eliteGoldMin`=30, `eliteGoldMax`=50, 고유 코어 조각 카운트=3, 전설 룬 확률 (조건부 — 현상금 의뢰 시 확정 드랍)
- [x] 등급 보너스: `gradeBonus`(S 1.3, A 1.0, B 0.8, C 0.5)
- [x] 일일/주간 의뢰 보상: 일일 300골드/3개, 주간 2,000골드+희귀 룬 1+보스 영혼 5+비전 설계도 1

### 1.13 [Data/CurrencyIconRegistry.cs](../Assets/02.Scripts/Data/CurrencyIconRegistry.cs)

- [x] `CreateAssetMenu("RPG Pinball/Village/Currency Icon Registry")` — 싱글톤 SO
- [x] `entries`(Array of struct{ CurrencyId id, Sprite icon, Color tint })
- [x] §0-A.4 매핑대로 10종 슬롯 채움
- [x] API: `GetIcon(CurrencyId) → (Sprite, Color)`. EconomyManager / HUD / 결과 화면에서 단일 참조점

### 1.14 [Data/ConsumableSpecialOreReference.cs](../Assets/02.Scripts/Data/ConsumableSpecialOreReference.cs)

- [x] 액트별 특수 소재 매핑 (Act1=봄꽃 추출액 / Act2=해적 동전 / Act3=톱니바퀴 / Act4=서리 조각) — 일반 몬스터/스테이지 클리어에서 1~3개 드랍

---

## 2. **[인계: M4 #2]** EconomyManager — 보스/엘리트/스테이지 보상 자동 지급

> 마일스톤 4에서 `OnBossDefeated`/`OnEliteDefeated` 이벤트가 발행되지만 보상 지급 구독자가 없는 상태. 본 마일스톤에서 EconomyManager를 구독자로 추가해 자동 흐름 완성.

### 2.1 [Meta/EconomyManager.cs](../Assets/02.Scripts/Meta/EconomyManager.cs)

- [x] 싱글톤 + `DontDestroyOnLoad`
- [x] `PlayerData.gold`/`manaCrystal`/`bossSoul`/`specialMaterials` 캐시 접근자 + 변경 시 `OnCurrencyChanged` 발행
- [x] API: `Add(CurrencyId, int)`, `Spend(CurrencyId, int) → bool`, `Has(CurrencyId, int) → bool`, `GetBalance(CurrencyId) → long`
- [x] **민감 수치는 SafeInt/SafeLong 래핑** (M1 보안 시스템 활용) — gold/manaCrystal/bossSoul/coreFragments/blueprintFragments 모두 래핑
- [x] 변경 시 `IntegrityChecker.UpdateChecksum` 자동 호출 (M1 보안 시스템 활용)
- [x] `Save()`/`Load()` — `Data_Schema.md` §2~3 직렬화 (M7에서 SaveSystem 본 구현 시 자동 연결)

### 2.2 EventBus 구독 (자동 보상 지급)

- [x] `OnBossDefeated(BossId, ActId, bool isFinalBoss)` 구독 → EconomyConfig.bossGoldBase × actMultipliers + bossSoul + SP + (확률) 룬 드랍 지급
- [x] `OnEliteDefeated(EliteId)` 구독 → 고유 코어 조각 3 + (현상금 의뢰 활성 시) 전설 룬 1 + 보스 영혼 5 지급
- [x] `OnStageCleared(StageBlueprint, GradeResult)` 구독 → 클리어 골드(`50 + StageIndex × 10`) + 마나 결정(`5 + floor(StageIndex / 5)`) + 등급 보너스
- [x] `OnNodeReward(NodeKind, RewardKind, int amount)` 구독 → 이벤트 노드(보물방/제단/도박/여행자)에서 발행한 보상 지급
- [x] **황금 고블린 왕의 황금 폭탄 드랍** — 패턴 내부에서 EconomyManager.Add 직접 호출 (M4 #2 인계 명시 사항)

### 2.3 검증

- [x] [EconomyManagerTests.cs](../Assets/Tests/EditMode/EconomyManagerTests.cs) — Add/Spend/Has 분기 + SafeInt 라운드트립 + IntegrityChecker 위변조 검출 시뮬레이션 (10건 이상)
- [x] [BossRewardFlowTests.cs](../Assets/Tests/EditMode/BossRewardFlowTests.cs) — `OnBossDefeated` 발행 후 모든 통화 잔액 정상 증가 (Act 1 / Act 4 / 최종 보스 / 일반 보스 4 케이스)

---

## 3. **[인계: M4 #3]** MonsterBase.Heal API + 보스/엘리트 자힐 본 구현

> 마일스톤 4 인계 옵션 B 확정 — `MonsterBase`에 `Heal(int amount, HealSource source)` public API를 추가하고, 마일스톤 4에서 시각 표시만 했던 자힐 메커니즘 본 구현으로 전환.

### 3.1 [Enemy/MonsterBase.cs](../Assets/02.Scripts/Enemy/MonsterBase.cs)

- [x] `public void Heal(int amount, HealSource source)` 추가 — `Hp(SafeInt) = clamp(currentHp + amount, 0, maxHp)`, `OnMonsterHealed(this, amount, source)` 이벤트 발행
- [x] 외부 시그니처(Hp 프로퍼티/ApplyDamage)는 변경 금지 — 기존 호출자 영향 없음

### 3.2 보스/엘리트 자힐 활성화

- [x] [WorldTreeSpiritBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act1/WorldTreeSpiritBoss.cs) Phase 3 광합성 — 초당 HP 0.5% 회복 (`Heal(maxHp × 0.005, BossPhotosynthesis)` 매초 호출)
- [x] [KrakenBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act2/KrakenBoss.cs) Phase 3 촉수 재생 — 촉수 1개 재생 시 본체 HP +본체 maxHp의 2% (`Heal(..., KrakenTentacleRegen)`)
- [x] [AbyssalLeviathanElite.cs](../Assets/02.Scripts/Enemy/EliteAI/AbyssalLeviathanElite.cs) — 잠수 5초 후 부상 시 잠수 동안 누적 0.5초당 maxHp의 1% 회복 (잠수 동안 5%)
- [x] [WinterQueenBoss.cs](../Assets/02.Scripts/Enemy/BossAI/Act4/WinterQueenBoss.cs) Phase 3 빙결 재생 — 빙결 상태 적 처치 시 본체 HP +500 (소량 회복) 또는 빙결 폭발 발동 시 maxHp의 1% 회복

### 3.3 EventBus 훅

- [x] `OnMonsterHealed(MonsterBase, int amount, HealSource)` 이벤트 추가 — UI에서 녹색 회복 텍스트 표시 (M8 폴리싱) + 도감 카운트 누적용

### 3.4 검증

- [x] [MonsterHealTests.cs](../Assets/Tests/EditMode/MonsterHealTests.cs) — Heal 호출 후 Hp 정상 증가 + maxHp 상한 클램프 + 이벤트 발행 + HealSource 분기 (4건)
- [x] **PlayMode** — 세계수 P3 / 크라켄 P3 / 리바이어선 / 겨울 여왕 P3 자힐 실제 발생 시뮬레이션 (사용자 확인)

---

## 4. **[인계: M2 #10]** 대장간 — 플리퍼 파생형 + DamageCalculator 단계 [5] 활성

### 4.1 [Village/ForgeManager.cs](../Assets/02.Scripts/Village/ForgeManager.cs)

- [x] 싱글톤. 현재 장착 재질/메인 코어/보조 코어 2개/플리퍼 레벨/플리퍼 파생형 캐시
- [x] API:
  - `CraftMaterial(BallMaterialId) → bool` — 골드/특수 광석 차감, `unlockedMaterials` 추가
  - `EquipMaterial(BallMaterialId) → bool` — 100골드(설정 가능) 차감 후 교체. **스테이지 중에는 false 반환**
  - `LevelUpCore(CoreId) → bool` — 조각 + 골드 차감, `coreLevel++`
  - `EquipCore(CoreId, CoreSlotKind)` — 메인 1 / 보조 2 슬롯 관리
  - `UpgradeFlipper() → bool` — 마나 결정 + 보스의 영혼 차감, `flipperUpgradeLevel++` (Max Lv.10)
  - `SelectFlipperVariant(FlipperVariantId) → bool` — Lv.4 미만이면 false, 첫 선택 무료, 변경 시 3,000골드 차감
- [x] 변경 시 EventBus 이벤트 발행 (`OnForgeBallChanged`, `OnFlipperUpgraded`, `OnFlipperVariantSelected`)

### 4.2 [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) — 단계 [5] 활성

- [x] 단계 [5] 플리퍼 파생형 분기 본 구현
  - **가시 플리퍼**: `EffectiveDEF = BaseDEF - 10% (5초 디버프 — DamageContext.IsSpikeDebuffActive 체크) - 레벨업 보너스`. 별도 출혈 도트는 `BleedDotComponent` 추가
  - **연성 플리퍼**: 데미지 직접 영향 없음. FlipperController가 width ×1.2 / activeDuration ×0.5→1.2 토글
  - **충격파 플리퍼**: `ShockwaveDamageApplier`가 본체 데미지 ×0.5 + 마법 속성으로 반경 5U 내 적 동시 타격 (별도 DamageContext 빌드)
- [x] [DamageCalculatorFlipperVariantTests.cs](../Assets/Tests/EditMode/DamageCalculatorFlipperVariantTests.cs) — 4건 (Basic 영향 없음 / Spike DEF -10% / Ductile 데미지 무변화 / Shockwave 광역 0.5×)

### 4.3 [Physics/FlipperController.cs](../Assets/02.Scripts/Physics/FlipperController.cs) — 파생형 시각/판정 적용

- [x] 인스턴스화 시 `ForgeManager.Instance.CurrentVariant` 조회 → 가시(콜라이더 끝에 출혈 트리거 자식) / 연성(scale 변경) / 충격파(타격 시 ShockwaveDamageApplier 호출)
- [x] 가시 플리퍼: 타격 직후 `BleedDotComponent` 부여 코루틴 (5초 동안 초당 데미지의 2%) — 가시 자식 트리거의 SpriteRenderer는 `Items/spikes.png` (틴트 #C0C0C0)
- [x] 연성 플리퍼: BoxCollider2D scale.x ×1.2, 액티브 지속 시간 0.5 → 1.2 (FlipperController 내부 상수 오버라이드)
- [x] 충격파 플리퍼: 공 타격 OnCollisionEnter2D 직후 `ShockwaveDamageApplier.Trigger(position, radius=5U, damageMultiplier=0.5, isMagic=true)` — 시각 효과는 `Items/star.png` 크기 5U로 페이드(0.3s)

### 4.4 [Combat/ShockwaveDamageApplier.cs](../Assets/02.Scripts/Combat/ShockwaveDamageApplier.cs)

- [x] 풀링 가능한 임시 GameObject. `Trigger(position, radius, multiplier, isMagic)` 호출 시 `Physics2D.OverlapCircleAll` → MonsterBase.ApplyDamage 호출
- [x] DamageContext.SourceKind=Shockwave / IsMagic=true / NoCriticalRoll=true (충격파는 크리티컬 미적용)

### 4.5 검증

- [x] [ForgeManagerTests.cs](../Assets/Tests/EditMode/ForgeManagerTests.cs) — 재질 교체 비용/스테이지 중 차단 / 코어 레벨업 비용 / 플리퍼 강화 비용 / Lv.4 파생형 잠금 / 변경 시 3,000골드 차감 (10건 이상)
- [x] **PlayMode (사용자 확인 필요)** — 가시 플리퍼 디버프 / 연성 플리퍼 폭 1.2× / 충격파 5U 광역 인게임 동작

---

## 5. **[인계: M2 #11]** 마법 부여소 — 룬 시스템 + DamageCalculator 단계 [6] 활성

### 5.1 [Village/EnchanterManager.cs](../Assets/02.Scripts/Village/EnchanterManager.cs)

- [x] 싱글톤. 보유 룬 인벤토리(`List<RuneInstance>`) + 스킬별 장착 슬롯 매핑
- [x] `RuneInstance` 클래스 — `RuneId id`, `RuneGrade grade`, `string equippedOnSkillId`(null이면 미장착). 표시용 `GetIcon() → Sprite`는 RuneData.iconNormal/iconRare/iconLegendary 분기
- [x] API:
  - `EquipRune(RuneId, RuneGrade, string skillId) → bool` — 해당 스킬의 소켓 수(Tier 2~4: 1칸 / Tier 5~6: 2칸) 검증 후 장착
  - `UnequipRune(RuneInstance) → bool`
  - `FuseRune(RuneId, RuneGrade) → bool` — 동일 룬 3개 합성 → 상위 등급 1개. 등급별 골드 비용 차감 (RuneData.fuseGoldCost). Legendary는 합성 불가 (최상위)
  - `GetEquippedRunes(string skillId) → RuneInstance[]`
- [x] EventBus 발행: `OnRuneEquipped`/`OnRuneUnequipped`/`OnRuneFused`

### 5.2 [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) — 단계 [6] 활성

- [x] 단계 [6] 룬 효과 분기 본 구현 — DamageContext.EquippedRuneIds(스킬 발동 시 ActiveSkillBase가 EnchanterManager에서 조회) 순회하며 조건 검사
  - **처형자의 룬**: `ctx.TargetCurrentHpRatio ≤ 0.3` → damage ×= 2.0 × runeEffectMultiplier (등급별)
  - **역경의 룬**: `ctx.RemainingTimeSeconds ≤ 60` → damage ×= 1.5 × multiplier
  - **연쇄의 룬**: `ctx.Combo ≥ 20` → 마나 비용 ×0.5 (데미지 무관, ActiveSkillBase.OnFire 직전 적용)
  - **확산 룬**: 별도 분기 (3방향 발사, 각 0.5 데미지) — ActiveSkillBase에서 처리
  - **관통 룬**: damage ×= (1 - 0.3 × passthroughCount) — 합연산 감소
  - **속성 전환 룬**: DamageContext.ElementOverride 설정 + 도트 부여 (화염/빙결/번개 각각 별도)
- [x] [DamageCalculatorRuneTests.cs](../Assets/Tests/EditMode/DamageCalculatorRuneTests.cs) — 처형자 보스 HP 30%↓ ×2 / 역경 시간 60↓ ×1.5 / 등급별 1.5×/2.25× / 관통 다중 / 연쇄 마나 비용 0.5× (8건 이상)

### 5.3 [Combat/ActiveSkillBase.cs](../Assets/02.Scripts/Combat/ActiveSkillBase.cs) — 룬 합성

- [x] `OnFire(context)` 호출 직전에 `RuneRuntime.ApplyTo(this, context)` 정적 헬퍼 호출 — EnchanterManager에서 장착 룬 조회 후 context.EquippedRuneIds / context.ElementOverride / context.ManaCostMultiplier 주입
- [x] 확산 룬은 ActiveSkillBase 내부에서 발사체 3개 발사 (각 0.5 데미지) 분기

### 5.4 검증

- [x] [EnchanterManagerTests.cs](../Assets/Tests/EditMode/EnchanterManagerTests.cs) — 소켓 수 분기 / 합성 비용 / 합성 실패(2개 미만) / 장착 해제 (8건)
- [x] [RuneRuntimeTests.cs](../Assets/Tests/EditMode/RuneRuntimeTests.cs) — 9종 × 3등급 효과 합성 + DamageContext 주입 정합 (10건 이상)

---

## 6. **[인계: M2 #12]** 점성술사 — 타로카드 시스템 + DamageCalculator 단계 [7] 활성

### 6.1 [Village/AstrologerManager.cs](../Assets/02.Scripts/Village/AstrologerManager.cs)

- [x] 싱글톤. 보유 타로카드 인벤토리 (`Dictionary<TarotCardId, TarotInstance>`) + 장착 슬롯 3칸
- [x] `TarotInstance` — `TarotCardId id`, `TarotGrade grade`, `bool isPermanent`, `int duplicateCount`
- [x] API:
  - `Pull(int count = 1) → TarotInstance[]` — 가중 추첨 (60/25/10/5 %). 500골드 또는 보스 영혼 3개 소모
  - `Equip(TarotCardId, int slotIndex 0~2) → bool` — 중복 장착 차단, 비영구 카드는 1회 사용 시 소진 플래그 설정
  - `Unequip(int slotIndex)`
  - `UpgradeToPermanent(TarotCardId) → bool` — 동일 카드 10장 + 5,000골드 소모 → `isPermanent=true`, duplicateCount = 1 (나머지 9장 소진)
- [x] **확률 가중 추첨**: `DeterministicRng` 시드는 매 호출 새로운 시드 사용 (운 요소). 마일스톤 5의 `DeterministicRng`를 활용하되 시드는 `DateTime.UtcNow.Ticks XOR PlayerUID`
- [x] EventBus 발행: `OnTarotPulled`/`OnTarotEquipped`/`OnTarotUnequipped`/`OnTarotPermanentUpgraded`

### 6.2 [Combat/DamageCalculator.cs](../Assets/02.Scripts/Combat/DamageCalculator.cs) — 단계 [7] 활성

- [x] 단계 [7] 타로카드 효과 분기 본 구현 — DamageContext.EquippedTarotCardIds 순회
  - **C-07 전사의 팔찌**: 물리 데미지 +8% 합연산
  - **R-02 사냥꾼의 눈**: 약점 타격 시 크리티컬 확률 +10%
  - **R-04 플리퍼 마스터**: 플리퍼 쿨타임 -0.15s (하드캡 0.5)
  - **R-05 원소 증폭기**: 마법 데미지 ×1.15 곱연산 (재질 미스릴과 별도)
  - **R-08 강철의 의지**: `ctx.RemainingTime ≤ 60` 시 공격력 +20% (역경의 룬과 곱연산 중첩)
  - **L-03 파멸의 인장**: `ctx.TargetCurrentHpRatio ≤ 0.2` 시 데미지 +50% (처형자 룬과 곱연산 중첩 — 곱연산 3중 제한 규칙 적용)
  - **L-02 대지의 심장**: 모든 재질 장점 동시 적용 (현재 재질의 단점만 유지)
  - 신화 8장: 별도 분기 (Cancel All Debuffs / Mana cost half / Boss HP 30% start / Multi-ball permanent ...) — 각 카드 ID별 분기로 처리
- [x] [DamageCalculatorTarotTests.cs](../Assets/Tests/EditMode/DamageCalculatorTarotTests.cs) — 각 효과 카드별 시뮬레이션 (12건 이상)

### 6.3 [UI/TarotCardView.cs](../Assets/02.Scripts/UI/TarotCardView.cs) — 카드 시각화 컴포넌트

- [x] Image 컴포넌트 2개(`backImage` / `faceImage`) + 외곽 frame Image
- [x] `Bind(TarotInstance)` 호출 시 TarotCardData.faceSprite/backSprite/frameColor 적용 (§0-A.2 매핑)
- [x] 영구 카드는 frameColor 위에 금색 테두리(별도 Image, alpha 0.6) 오버레이
- [x] 뽑기 연출은 M8 인계 — 본 마일스톤은 즉시 표시

### 6.4 [Village/CollectionManager.cs](../Assets/02.Scripts/Village/CollectionManager.cs) — 시련 도감

- [x] 싱글톤. `Dictionary<GimmickId, GimmickEncounterRecord>` — encounterCount/deathCount/resistLevel
- [x] `OnGimmickActivated` 구독 → encounterCount++
- [x] `OnBallForceReset(GimmickId? cause)` 구독 → deathCount++ (보스 강제 reset 패턴은 별도 카운트)
- [x] **저항력 자동 계산** (deathCount 기준):
  - 0 → Lv.1 (0%)
  - 3 → Lv.2 (10%)
  - 7 → Lv.3 (20%)
  - 15 → Lv.4 (30%)
  - 30 → Lv.5 (40%)
- [x] 단계 전이 시 `OnGimmickResistanceLevelUp` 발행
- [x] 도감 보상 마일스톤: 10종 → 골드 1,000 + `flat_medal7.png` 칭호 / 40종 → 칭호 "시련 극복자" + `flat_medal8.png` / 80종 → 전설 타로카드 1장 + `flat_medal9.png`
- [x] [GimmickBase.cs](../Assets/02.Scripts/Stage/Gimmicks/GimmickBase.cs) 효과 강도 적용 시 `1 - 0.1 × resistLevel` 곱 (이미 §0 선행 조건 표에서 약속)
- [x] 도감 UI에서 각 기믹의 현재 저항 단계는 §0-A.9 메달 아이콘으로 표시

### 6.5 검증

- [x] [AstrologerManagerTests.cs](../Assets/Tests/EditMode/AstrologerManagerTests.cs) — 뽑기 확률 10,000회 ±1% / 영구 카드 승급 10장+5,000골드 / 중복 장착 차단 / 비영구 카드 1회 사용 시 소진 (15건 이상)
- [x] [CollectionManagerTests.cs](../Assets/Tests/EditMode/CollectionManagerTests.cs) — 저항력 1/3/7/15/30 단계 전이 / 80종 올컴플릿 보상 발급 (8건 이상)

---

## 7. 주점 — 의뢰 & 현상금 시스템

### 7.1 [Meta/QuestManager.cs](../Assets/02.Scripts/Meta/QuestManager.cs)

- [x] 싱글톤. `dailyQuests`(List<QuestInstance> 최대 3) / `weeklyQuest`(QuestInstance?) / `bountyTargets`(List<QuestInstance> 최대 3)
- [x] `QuestInstance` — questId / progress / target / completed / expiresAt(DateTimeOffset)
- [x] **갱신 로직** (자정 UTC+9):
  - `RefreshDailyIfExpired(DateTimeOffset now)` — `now > expiresAt`이면 일일 풀에서 랜덤 3개 추첨
  - `RefreshWeeklyIfExpired` — 매주 월요일 00:00 KST
  - `RefreshBountyIfExpired` — 매주 월요일 00:00 KST. **단, 해당 액트 최종 보스를 처치한 엘리트만 후보에 포함**
- [x] `DateTimeOffset` 의존성 주입 — 테스트에서 Mock 가능하도록 `IClock` 인터페이스로 추상화
- [x] API:
  - `AcceptQuest(QuestInstance)`
  - `ReportProgress(QuestObjectiveKind, int delta, object args)` — 호출 시 모든 해당 의뢰의 progress 누적
  - `ClaimReward(QuestInstance) → bool` — completed=true 시에만 EconomyManager.Add 호출 후 deactivate
- [x] **이벤트 구독**: `OnStageCleared`/`OnBossDefeated`/`OnEliteDefeated`/`OnGimmickActivated`/`OnFlipperSummoned`/`OnBallForceReset` 모두 구독 → 의뢰 조건 자동 추적
- [x] EventBus 발행: `OnDailyQuestRolled`/`OnQuestProgress`/`OnQuestCompleted`/`OnBountyAccepted`

### 7.2 [Village/TavernManager.cs](../Assets/02.Scripts/Village/TavernManager.cs)

- [x] QuestManager를 감싸는 시설 진입점 (UI 호출용)
- [x] API:
  - `GetActiveDailyQuests() → QuestInstance[]`
  - `GetActiveWeeklyQuest() → QuestInstance?`
  - `GetBountyBoard() → QuestInstance[]`
  - `EnterBountyStage(QuestInstance bounty)` — 입장 조건 검증 후 GameManager.LoadEliteArena (M5 §1.7 ArenaLayoutData 활용)
- [x] **현상금 입장 조건 검증** — `requiredActBossDefeated` 필드와 PlayerData.bossesDefeated 비교 (M4 #4 인계 마감)

### 7.3 검증

- [x] [QuestManagerTests.cs](../Assets/Tests/EditMode/QuestManagerTests.cs) — 자정 갱신 / IClock Mock / 진행도 누적 / 보상 청구 / 일일 3개 풀에서 중복 없이 추첨 (15건 이상)
- [x] [BountyEntryTests.cs](../Assets/Tests/EditMode/BountyEntryTests.cs) — 액트 보스 미처치 시 입장 차단 / 처치 후 정상 입장 / 4종 엘리트 모두 (8건)

---

## 8. 열기구 선착장 + 용병단 창고

### 8.1 [Village/BalloonManager.cs](../Assets/02.Scripts/Village/BalloonManager.cs)

- [x] 싱글톤. `currentUpgradeLevel`(0~3) 캐시
- [x] `Upgrade() → bool` — 다음 단계 비용 차감 후 `currentUpgradeLevel++`
- [x] 효과는 ActMap 진입 시 `GameManager`가 BalloonManager를 조회해 일회성 버프 적용 (시작 마나 +20 / 보스 시간 +10 / 히든 노드 확률 +15%)
- [x] EventBus 발행: `OnBalloonUpgraded`

### 8.2 [Village/MercenaryManager.cs](../Assets/02.Scripts/Village/MercenaryManager.cs)

- [x] 싱글톤. 보유 소모품 인벤토리 + 장착 슬롯 2칸
- [x] API:
  - `Craft(ConsumableId) → bool` — 골드 + 특수 소재 차감 후 인벤토리에 추가
  - `Equip(ConsumableId, int slotIndex 0~1)`
  - `Use(int slotIndex)` — 인게임에서 호출. 효과 발동 후 인벤토리에서 1 차감
- [x] **3종 소모품 효과**:
  - 긴급 방패: 5초간 보스 강제 reset 패턴 무효화 (절대 영도/거대 주먹) — `BossForcedResetGuard.Activate(5f)` 호출 + `Items/star.png` 페이드 오버레이
  - 시간의 모래: `StageTimer.AddTime(15f)` — 상한 60초 정책 포함 + `Items/weight.png` 시계 이펙트
  - 광란의 부적: 10초간 공격력 ×2 + 공 속도 ×1.5 → `BallController.ApplyForcedSpeedMultiplier(1.5, 10)` + `DamageContext.GlobalDamageMultiplier ×= 2.0` 활성 + `Items/bomb.png` 폭발 페이드
- [x] EventBus 발행: `OnConsumableCrafted`/`OnConsumableUsed`

### 8.3 [Combat/BossForcedResetGuard.cs](../Assets/02.Scripts/Combat/BossForcedResetGuard.cs)

- [x] 정적 카운트다운. `Activate(duration)` 호출 시 활성, 보스 강제 reset 시점에 `IsActive` 검사 후 무효화
- [x] FrostGiantBoss / WinterQueenBoss / (그 외 강제 reset 패턴 보스)에서 활성 시 reset 스킵 + 시간 페널티 미적용

### 8.4 검증

- [x] [BalloonManagerTests.cs](../Assets/Tests/EditMode/BalloonManagerTests.cs) — 3단계 비용 / 효과 반영 (5건)
- [x] [MercenaryManagerTests.cs](../Assets/Tests/EditMode/MercenaryManagerTests.cs) — 제작/장착/사용 + 슬롯 2칸 제한 + 사용 후 차감 (8건)

---

## 9. 수련장 — 스킬 트리 UI · 덱 세팅 · 리셋 · 보스 연습

### 9.1 [Village/TrainingManager.cs](../Assets/02.Scripts/Village/TrainingManager.cs)

- [x] 싱글톤. SkillTreeManager / SkillDeck를 감싸는 시설 진입점
- [x] API:
  - `EquipDeckSlot(int slotIndex 0~3, string skillId) → bool` — Tier 6 궁극기 중복 / 동일 스킬 중복 차단
  - `ClearDeckSlot(int slotIndex)`
  - `ResetAllSkills() → bool` — `SkillResetCostTable.getCost(resetCount)` 만큼 골드 차감, `SkillTreeManager.RefundAll()` 호출, `playerData.resetCount++`. **리스펙 권서 보유 시 골드 무료 + 권서 1 차감**
  - `OpenBossPractice(BossId)` — 보스 연습 모드 진입. `practiceModeBossesUnlocked`에 포함된 보스만 허용
- [x] EventBus 발행: `OnSkillReset`/`OnSkillDeckEquipped`

### 9.2 [Stage/BossPracticeStageBuilder.cs](../Assets/02.Scripts/Stage/BossPracticeStageBuilder.cs)

- [x] 보스 연습 전용 절차 생성 우회 — 고정 보스만 등장, 타이머 OFF (StageTimer.SetUnlimited), 페이즈 선택 입력, 처치 시 보상 미지급 (EconomyManager.Suppress(true) 플래그)
- [x] M4 BossBase의 OnEnable 자동 BossFightContext.Enter는 유지, OnDisable에서 자동 Exit

### 9.3 검증

- [x] [TrainingManagerTests.cs](../Assets/Tests/EditMode/TrainingManagerTests.cs) — 덱 4칸 / Tier 6 중복 차단 / 동일 스킬 중복 차단 / 스킬 리셋 비용 시퀀스(0/1000/3000/5000) / 리스펙 권서 사용 (12건 이상)
- [x] [BossPracticeFlowTests.cs](../Assets/Tests/EditMode/BossPracticeFlowTests.cs) — 처치 시 EconomyManager.Add 미호출 / 타이머 OFF / 미해금 보스 진입 차단 (6건)

---

## 10. Village 씬 + UI Stub (M7 인계 대상)

> Village UI 본 구현은 마일스톤 7에서 진행. 본 마일스톤은 **시설 매니저 호출이 가능한 최소한의 디버그 UI** + 씬 진입점만 마련. 비주얼은 §0-A.7·§0-A.8 매핑대로 Kenney 자산 직접 사용.

### 10.1 [01.Scenes/Village.unity](../Assets/01.Scenes/Village.unity)

- [x] 빈 씬 생성. Main Camera (Orthographic Size 9) + Directional Light + Canvas(Screen Space - Overlay) 배치
- [x] **바닥**: tileGrass 16×16 타일 매트 (`kenney_block-pack/PNG/Default (64px)/tileGrass.png`) — 메모리 정책상 프리팹 `05.Prefabs/Village/VillageGround.prefab` 먼저 생성
- [x] **6개 시설 건물 배치** (각각 프리팹 먼저 생성 → 인스턴스화):
  - `05.Prefabs/Village/Forge.prefab` — `market_stallRed.png` + `market_roofRed.png` 합성 + `red_body_square` NPC 자식
  - `05.Prefabs/Village/Enchanter.prefab` — `market_stallBlue.png` + `market_roofBlue.png` + 마름모 NPC
  - `05.Prefabs/Village/Tavern.prefab` — `detail_windowCastle.png` + `yellow_body_squircle` NPC
  - `05.Prefabs/Village/Astrologer.prefab` — `detail_windowCastle.png` (틴트 #6A0DAD) + 원형 NPC
  - `05.Prefabs/Village/BalloonDock.prefab` — `cart.png` + 사각 NPC
  - `05.Prefabs/Village/TrainingGround.prefab` — `tileBridge.png` + 마름모 NPC
- [x] **장식**: `foliageTree_green.png` × 6, `foliageBush_large.png` × 4 곳곳 배치
- [x] 각 시설 GameObject에 `VillageFacilityEntry` 컴포넌트 부착 — `OnPointerClick` 시 디버그 패널 활성
- [x] 디버그 패널은 IMGUI 또는 UI Toolkit 임시 위젯 — 입력 가능한 텍스트 박스 + 버튼만 (현재 잔액 표시 + 호출 결과 로그 출력)
- [x] **본격 UI는 M7 §[NEW] PopupManager / ActMapUI / InGameHUD와 함께 작성**

### 10.2 [Village/VillageBootstrap.cs](../Assets/02.Scripts/Village/VillageBootstrap.cs)

- [x] 씬 진입 시 모든 매니저 싱글톤이 활성 상태인지 확인 후 PlayerData 로드 (M7 SaveSystem 본 구현 전까지는 PlayerPrefs 어댑터)

### 10.3 [UI/CurrencyHud.cs](../Assets/02.Scripts/UI/CurrencyHud.cs) — 임시 통화 HUD

- [x] 상단 좌측 — Gold/ManaCrystal/BossSoul 3개 아이콘 + 숫자 (CurrencyIconRegistry 참조)
- [x] `OnCurrencyChanged` 구독 시 즉시 갱신
- [x] M7에서 정식 HUD로 교체

---

## 11. Constants.cs 확장

- [x] [Constants.cs](../Assets/02.Scripts/Core/Constants.cs)에 다음 섹션 신규 추가:
  - **§대장간**: `MaterialSwapGoldCost=100`, `FlipperVariantChangeGoldCost=3000`, `FlipperMaxLevel=10`, `CoreMaxLevel=5`, `CoreSlotsMain=1`, `CoreSlotsSub=2`
  - **§룬**: `RuneFuseRequiredCount=3`, `RuneGradeMultipliers={1.0, 1.5, 2.25}`, `RuneSocketTiersByTier={1=0, 2=1, 3=1, 4=1, 5=2, 6=2}`
  - **§타로카드**: `TarotPullGoldCost=500`, `TarotPullBossSoulCost=3`, `TarotEquipSlots=3`, `TarotPermanentRequiredCount=10`, `TarotPermanentGoldCost=5000`, `TarotDropWeights={Common=60, Rare=25, Legendary=10, Mythic=5}`
  - **§의뢰**: `DailyQuestSlotCount=3`, `DailyQuestRefreshHourKst=0`, `WeeklyQuestRefreshDayOfWeek=Monday`, `BountySlotCount=3`, `BountyRefreshDayOfWeek=Monday`
  - **§열기구**: `BalloonUpgrade1Gold=1000`, `BalloonUpgrade1Mana=30`, `BalloonUpgrade2Gold=3000`, `BalloonUpgrade2Mana=80`, `BalloonUpgrade3Gold=8000`, `BalloonUpgrade3Mana=200`
  - **§용병단**: `MercenarySlotCount=2`, `EmergencyShieldDuration=5f`, `SandsOfTimeBonusSeconds=15f`, `FrenzyCharmDamageMultiplier=2.0f`, `FrenzyCharmSpeedMultiplier=1.5f`, `FrenzyCharmDuration=10f`
  - **§수련장**: `SkillResetCosts={0, 1000, 3000, 5000}`, `RespecScrollFreeReset=true`, `SkillDeckSlotCount=4`, `UltimateSkillMaxInDeck=1`
  - **§도감**: `GimmickResistThresholds={1, 3, 7, 15, 30}`, `GimmickResistReductionPerLevel=0.1f`, `CollectionMilestone10Gold=1000`, `CollectionMilestone40TitleId="trial_survivor"`, `CollectionMilestone80LegendaryTarotCount=1`
  - **§경제**: `StageGoldFormula="50 + StageIndex * 10"`, `StageManaCrystalFormula="5 + floor(StageIndex / 5)"`, `BossGoldBase=300`, `ActMultipliers={1.0, 1.8, 2.5, 3.5}`, `BossSoulNormal=2`, `BossSoulFinal=5`, `EliteGoldMin=30`, `EliteGoldMax=50`, `GradeBonusMultipliers={S=1.3, A=1.0, B=0.8, C=0.5}`
  - **§자산경로**: `KennyRunePackRoot="Assets/50. External Assets/kenny/kenney_rune-pack/PNG"`, `KennyMedalsRoot="Assets/50. External Assets/kenny/kenney_medals/PNG"`, `KennyPlatformerItemsRoot="Assets/50. External Assets/kenny/kenney_platformer-art-deluxe/Base pack/Items"`, `KennyBlockPackRoot="Assets/50. External Assets/kenny/kenney_block-pack/PNG/Default (64px)"`, `KennyBallAssetsRoot="Assets/50. External Assets/kenny/kenney_rolling-ball-assets/PNG/Default"`, `KennyShapeCharsRoot="Assets/50. External Assets/kenny/kenney_shape-characters/PNG/Default"`

---

## 12. SaveSystem 임시 어댑터 (M7 인계 대상)

> M7에서 정식 SaveSystem(AES-256 + HMAC) 구현. 본 마일스톤은 6개 시설의 상태를 잃지 않도록 **PlayerPrefs 어댑터**만 임시 추가.

### 12.1 [Core/PlayerPrefsSaveAdapter.cs](../Assets/02.Scripts/Core/PlayerPrefsSaveAdapter.cs)

- [x] `Save(string key, object data)` / `Load<T>(string key) → T?` — JsonUtility 기반 임시 직렬화
- [x] 키 네임스페이스: `RPGPinball.Player.*`, `RPGPinball.Inventory.*`, `RPGPinball.Village.*`, `RPGPinball.Collection.*`, `RPGPinball.Quests.*`
- [x] **M7 본 구현 시 PlayerPrefs → 암호화 JSON 파일로 마이그레이션** (M7 §SaveSystem)
- [x] SafeInt/SafeFloat/SafeLong 직렬화는 raw 값으로 변환해서 저장 (난독화는 메모리 상에서만)

### 12.2 검증

- [x] [PlayerPrefsSaveAdapterTests.cs](../Assets/Tests/EditMode/PlayerPrefsSaveAdapterTests.cs) — 라운드트립 + null 처리 + 결과 일치 (5건)

---

## 13. 검증 체크리스트

### 13.1 단위 테스트 (EditMode)

- [x] [EconomyManagerTests.cs](../Assets/Tests/EditMode/EconomyManagerTests.cs) — Add/Spend/Has + SafeInt 라운드트립 (10건)
- [x] [BossRewardFlowTests.cs](../Assets/Tests/EditMode/BossRewardFlowTests.cs) — `OnBossDefeated` → 자동 보상 (4건)
- [x] [MonsterHealTests.cs](../Assets/Tests/EditMode/MonsterHealTests.cs) — Heal API (4건)
- [x] [ForgeManagerTests.cs](../Assets/Tests/EditMode/ForgeManagerTests.cs) — 재질/코어/플리퍼 강화 (10건+)
- [x] [DamageCalculatorFlipperVariantTests.cs](../Assets/Tests/EditMode/DamageCalculatorFlipperVariantTests.cs) (4건)
- [x] [EnchanterManagerTests.cs](../Assets/Tests/EditMode/EnchanterManagerTests.cs) (8건)
- [x] [RuneRuntimeTests.cs](../Assets/Tests/EditMode/RuneRuntimeTests.cs) (10건+)
- [x] [DamageCalculatorRuneTests.cs](../Assets/Tests/EditMode/DamageCalculatorRuneTests.cs) (8건+)
- [x] [AstrologerManagerTests.cs](../Assets/Tests/EditMode/AstrologerManagerTests.cs) (15건+)
- [x] [CollectionManagerTests.cs](../Assets/Tests/EditMode/CollectionManagerTests.cs) (8건+)
- [x] [DamageCalculatorTarotTests.cs](../Assets/Tests/EditMode/DamageCalculatorTarotTests.cs) (12건+)
- [x] [QuestManagerTests.cs](../Assets/Tests/EditMode/QuestManagerTests.cs) (15건+)
- [x] [BountyEntryTests.cs](../Assets/Tests/EditMode/BountyEntryTests.cs) (8건)
- [x] [BalloonManagerTests.cs](../Assets/Tests/EditMode/BalloonManagerTests.cs) (5건)
- [x] [MercenaryManagerTests.cs](../Assets/Tests/EditMode/MercenaryManagerTests.cs) (8건)
- [x] [TrainingManagerTests.cs](../Assets/Tests/EditMode/TrainingManagerTests.cs) (12건+)
- [x] [BossPracticeFlowTests.cs](../Assets/Tests/EditMode/BossPracticeFlowTests.cs) (6건)
- [x] [PlayerPrefsSaveAdapterTests.cs](../Assets/Tests/EditMode/PlayerPrefsSaveAdapterTests.cs) (5건)
- [x] [CurrencyIconRegistryTests.cs](../Assets/Tests/EditMode/CurrencyIconRegistryTests.cs) — 10종 통화 ↔ Sprite 매핑 누락 0건 (3건)
- [x] [TarotIconBindingTests.cs](../Assets/Tests/EditMode/TarotIconBindingTests.cs) — 38장 모두 `faceSprite != null` (1건) / 등급별 색상 매핑 정확 (4건)
- [x] [RuneIconBindingTests.cs](../Assets/Tests/EditMode/RuneIconBindingTests.cs) — 9 RuneData 모두 `iconNormal/iconRare/iconLegendary != null` (3건)
- [x] **마일스톤 5 134건 그대로 유지** + 마일스톤 6 신규 **150건 이상** = **280건+ 모두 통과** 목표

### 13.2 PlayMode 자동 시뮬레이션

> **상태**: PlayMode 시뮬레이션 테스트는 본 마일스톤에서는 작성하지 않음. 본 마일스톤은 EditMode 51건 신규 + 누적 185/185 통과로 대체. 아래 시나리오들은 M7-M8 PlayMode 테스트 작성 시 인계.

| 시나리오 | 기대 결과 |
|---|---|
| Act 1 올클리어 시뮬레이션 (30 스테이지 + 3 보스) | 누적 골드 8,000~10,000 / 마나 결정 200~300 / 보스 영혼 9 (2+2+5) |
| 일일 의뢰 자정 갱신 (IClock Mock으로 +24h) | 새 3개 의뢰 추첨, 이전 의뢰 만료 |
| 타로카드 10,000회 뽑기 | Common 60% / Rare 25% / Legendary 10% / Mythic 5% ±1% |
| 영구 카드 승급 | 동일 카드 10장 + 5,000골드 → isPermanent=true |
| 시련 도감 저항력 전이 | 1/3/7/15/30회 사망 시 Level 1→5 정상 |
| 가시 플리퍼 인스턴스화 | DEF -10% 디버프 5초 적용 + 출혈 도트 + `spikes.png` 자식 트리거 표시 |
| 처형자 룬 (Legendary) | 보스 HP ≤ 30% 시 데미지 ×4.5 (2.0 × 2.25) + 검정 룬 아이콘 표시 |
| L-02 대지의 심장 | 모든 재질 장점 동시 적용 (단점은 현재 재질 것만) |
| 보스 처치 → 자동 보상 | EconomyManager.Add 호출 1회 + EventBus 발행 확인 |
| 보스 자힐 (세계수 P3 광합성) | 초당 maxHp × 0.005 회복, OnMonsterHealed 발행 |
| 긴급 방패 사용 | 5초간 BossForcedResetGuard.IsActive=true, 강제 reset 시 무효화 + `star.png` 페이드 |
| 보스 연습 모드 처치 | EconomyManager.Add 미호출, 타이머 OFF |
| 현상금 입장 차단 | 액트 보스 미처치 시 EnterBountyStage false |
| Village 씬 진입 | 6개 시설 프리팹 인스턴스 + Kenney 스프라이트 로드 성공 (빨간 누락 마커 0건) |

### 13.3 인게임 검증 (사용자 확인 필요)

- [~] Village 씬 진입 → 6개 시설 건물(Kenney 스프라이트 적용) 화면 확인 → 클릭으로 디버그 패널 진입 — **Game View 캡쳐로 6개 시설/그라운드 정상 확인. 사용자 직접 클릭 진입 확인 필요**
- [ ] 보스 1회 처치 → 결과 화면(임시 텍스트) → 골드/마나 결정/보스 영혼 잔액 증가 확인 (HUD 좌측 상단 통화 아이콘 갱신)
- [ ] Forge에서 강철 공 제작 (2,000골드 + 철광석 5개) → 교체 → 인게임 무게 증가 체감 + 공 스프라이트 ball_blue_large로 변경 — **BallMaterialData SO 인스턴스 4종 생성 필요**
- [ ] Forge 플리퍼 Lv.4 도달 → 가시 파생형 선택 → 가시 플리퍼 시각(spikes 자식)/디버프 인게임 동작 — **FlipperVariantData SO 3종 인스턴스 + FlipperController.cs 측 시각 컴포넌트 부착은 M7-M8 인계**
- [ ] Enchanter 룬 합성 (Normal 3 → Rare 1) → Grey 아이콘 3개 소진 → Blue 아이콘 1개 획득 → 비용 차감 — **RuneData SO 9종 인스턴스 생성 + Kenney 룬 아이콘 매핑 필요 (등급별 3개 슬롯)**
- [ ] Astrologer 타로 뽑기 500골드 → 카드 1장 획득 → 장착 → 다음 스테이지에 효과 적용 — **TarotCardData SO 38장 인스턴스 + Kenney 룬 매핑 + TarotCardView 시각화 컴포넌트 필요**
- [ ] Tavern 일일 의뢰 수령 → 스테이지 진행 → 보상 청구 — **QuestData SO 풀 (Daily 10 / Weekly 8 / Bounty 4) 인스턴스 필요**
- [ ] Tavern 현상금 → 액트 최종 보스 처치 후 입장 가능 확인 — **TavernManager.EnterBountyStage 는 OnBountyAccepted 이벤트 발행까지만. ArenaLayout 로드는 M7 인계**
- [ ] Mercenary 광란의 부적 사용 → 10초간 공격력 ×2 / 공 속도 ×1.5 체감 + `bomb.png` 폭발 페이드 — **ConsumableData SO 3종 인스턴스 + BallController 측 ForcedSpeed 적용 인터페이스 필요**
- [ ] Training 스킬 리셋 4회 → 비용 0/1000/3000/5000 정상 시퀀스 — **SkillTreeManager.ResetAll 호출 정상 작동 (EditMode 검증). 실제 인게임 SP 환원 PlayMode 확인 필요**
- [ ] Training 보스 연습 → 처치 후 보상 미지급 확인 — **BossPracticeStageBuilder 골격까지만. 실제 보스 인스턴스화 + 페이즈 선택 UI는 M7-M8 인계**
- [ ] 도감 80종 누적 시 → 전설 타로카드 1장 + `flat_medal9.png` 획득 알림 — **80종 누적 트리거 호출까지 구현. 실제 보상 지급 / 알림 UI는 M7 인계**

### 13.4 문서 정합성

- [x] [Game_Design_Spec.md §3 마을 시설](../Design/Game_Design_Spec.md) — 6개 시설 모든 항목 SO/매니저 1:1 반영
- [x] [Game_Design_Spec.md §8 재화 시스템](../Design/Game_Design_Spec.md) — 10종 재화 EconomyManager에 모두 반영 + CurrencyIconRegistry SO에 모든 아이콘 매핑
- [x] [Damage_Formula.md §2 단계 [5][6][7]](../Design/Damage_Formula.md) — 가시 플리퍼/9룬/38타로카드 효과 일치
- [x] [Tarot_Card_List.md](../Design/Tarot_Card_List.md) — 38장 SO 1:1 반영 (Common 10 / Rare 10 / Legendary 10 / Mythic 8) + §0-A.2 아이콘 경로 매핑 누락 0건
- [x] [Data_Schema.md §3 인벤토리 / §6 마을 / §7 도감 / §8 의뢰](../Design/Data_Schema.md) — PlayerPrefsSaveAdapter 키 / JSON 모델 정합
- [x] [Implementation_Plan.md §마일스톤 6](Implementation_Plan.md) — M2 #10/#11/#12 + M4 #2/#3/#4 인계 항목 모두 처리
- [x] **Kenney 라이센스 준수** — `kenney_rune-pack/License.txt`(CC0), `kenney_medals/License.txt`, `kenney_block-pack/License.txt`, `kenney_platformer-art-deluxe/license.txt`, `kenney_shape-characters/License.txt`, `kenney_rolling-ball-assets/License.txt` 모두 CC0이므로 별도 저작자 표기 불필요. 단 Credits 화면에 "Sprites by Kenney.nl (CC0)" 자율 표기 권장 (M8 인계)

---

## 14. 후속 마일스톤 인계 사항

> 마일스톤 6 진행 중 시간/우선순위/의존성 사유로 후속 마일스톤에 넘기는 항목. 후속 마일스톤 진입 시 `[인계: M6]` 표기로 cross-link.

| # | 항목 | 인계 대상 | 사유 |
|---|---|---|---|
| 1 | 본격 Village UI (6개 시설 정식 화면 / 인벤토리 / 카드 컬렉션 뷰) | **마일스톤 7** | 본 마일스톤은 디버그 패널 + Kenney 스프라이트 건물만. PopupManager·UI 컴포넌트 본격 도입 시 정식 화면 작성 |
| 2 | SaveSystem 정식 구현 (AES-256 + HMAC + 클라우드 동기화) | **마일스톤 7** | 본 마일스톤은 PlayerPrefs 어댑터만. AES/HMAC 및 Google Play Games 클라우드는 M7 |
| 3 | 시각 효과 / 사운드 — 보스 자힐 녹색 텍스트 / 가시 플리퍼 출혈 이펙트 / 충격파 파동 / 타로카드 뽑기 연출 | **마일스톤 8** | 효과/사운드 시스템 도입 시. 본 마일스톤은 Kenney 스프라이트 페이드 정도까지만 |
| 4 | 시즌 패스·월간 패스 BM 연동 — 타로카드 팩, 코스메틱 | **마일스톤 8** | IAPManager 도입 시 |
| 5 | 도감 UI / 도감 칭호 표시 / 영구 카드 갤러리 | **마일스톤 7/8** | UI 본격 작성 시 |
| 6 | 결과 화면 등급 보너스(L-08 별의 유언 등급 상향) | **마일스톤 7** | ResultScreen 도입 시 |
| 7 | 광고 시청 일일 무료 타로카드 1장 | **마일스톤 8** | AdManager 도입 시 |
| 8 | "원소 폭주" 스킬 트리 정합 재확인 (M2 #17) | **마일스톤 3 잔여**(이미 처리되었으면 닫음) | M3에서 처리되지 않았다면 본 마일스톤에서 SkillDeck 작업 중 검증 후 닫기 |
| 9 | 보스 도감 (12 보스 + 4 엘리트 패턴 기록 + 약점 시각화) | **마일스톤 7/8** | UI 본격 작성 시. 본 마일스톤은 데이터 추적까지만 |
| 10 | 신화 타로카드 8장 중 특수 분기(시간 정지 면역 / 멀티볼 영구 / 보스 HP 30% 시작 등) — 보스 AI 측 분기 필요 | **마일스톤 8** | M4 보스 코드에 분기 삽입 필요. 본 마일스톤은 효과 SO 정의까지 |
| 11 | Kenney 자산 → 자체 아트 교체 / 정식 NPC·시설 일러스트 | **마일스톤 8** | 아트 파이프라인 확정 후. 본 마일스톤은 Kenney CC0 자산으로 최종 합격 가능한 비주얼 확보 |
| 12 | 텍스처 임포터 프리셋 자동화 (`Assets/Editor/SpriteImporterPreset.cs`) | **마일스톤 8** | 본 마일스톤은 §0-A.10 체크리스트로 수동 확인 |
| 13 | Credits 화면 "Sprites by Kenney.nl (CC0)" 표기 | **마일스톤 8** | Credits/About 화면 도입 시 |
