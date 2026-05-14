namespace RPGPinball.Data
{
    /// <summary>
    /// 마일스톤 6: 마을 시설 / 메타 시스템 관련 enum 통합 정의.
    /// Data_Schema.md §3 + Game_Design_Spec.md §8 통화 시스템을 1:1 반영.
    /// </summary>

    // ── §재화 (10종 기본 + 특수 광석 4종) ───────────────────────
    public enum CurrencyId
    {
        None = 0,
        Gold = 1,
        ManaCrystal = 2,
        BossSoul = 3,
        CoreFragment = 4,        // 6코어 공통 — 세부는 CoreId로 분리
        BlueprintFragment = 5,   // 비전 설계도
        RespecScroll = 6,        // 리스펙 권서
        XP = 7,
        SkillPoint = 8,

        // 특수 광석 (Act별)
        SpringFlowerExtract = 20,  // Act 1
        PirateCoin = 21,           // Act 2
        Cogwheel = 22,             // Act 3
        FrostShard = 23,           // Act 4

        // 일반 광석
        IronOre = 30,
        MithrilFragment = 31,
        VolcanicAsh = 32
    }

    // ── §공 재질 ────────────────────────────────────────────
    // BallMaterial enum은 SkillEnums.cs에 이미 정의됨 (Wood/Steel/Mithril/Volcanic)

    public enum BallMaterialId
    {
        Wood = 0,
        Steel = 1,
        Mithril = 2,
        Volcanic = 3
    }

    // ── §코어 슬롯 ───────────────────────────────────────────
    public enum CoreSlotKind
    {
        Main = 0,
        Sub = 1
    }

    // ── §플리퍼 파생형 ───────────────────────────────────────
    public enum FlipperVariantId
    {
        Basic = 0,
        Spike = 1,       // 가시 — DEF -10% 디버프 + 출혈
        Ductile = 2,     // 연성 — 폭 ×1.2 / 액티브 지속 ×0.5→1.2 토글
        Shockwave = 3    // 충격파 — 반경 5U 마법 광역 0.5×
    }

    // ── §룬 ─────────────────────────────────────────────────
    public enum RuneId
    {
        None = 0,
        // Shape 계열
        Spread = 1,      // 확산 — 3방향 발사, 각 0.5
        Pierce = 2,      // 관통 — 다중 적 통과 (감쇠 0.3)
        Homing = 3,      // 추적 — 회전 속도

        // Element 계열
        FireConvert = 4,
        IceConvert = 5,
        LightningConvert = 6,

        // Frenzy 계열
        Executioner = 7, // HP ≤ 30% 시 ×2
        Chain = 8,       // 콤보 ≥ 20 시 마나 비용 ×0.5
        Adversity = 9    // 잔여 시간 ≤ 60 시 ×1.5
    }

    public enum RuneFamily
    {
        Shape = 0,
        Element = 1,
        Frenzy = 2
    }

    public enum RuneGrade
    {
        Normal = 0,    // ×1.0
        Rare = 1,      // ×1.5
        Legendary = 2  // ×2.25
    }

    public enum ElementOverride
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Lightning = 3
    }

    // ── §타로카드 ────────────────────────────────────────────
    public enum TarotGrade
    {
        Common = 0,
        Rare = 1,
        Legendary = 2,
        Mythic = 3
    }

    public enum TarotCardId
    {
        None = 0,
        // Common 10종
        C01_MoonlightSpring = 101,
        C02_GoldenCompass = 102,
        C03_ApprenticeStaff = 103,
        C04_TravelerBoots = 104,
        C05_LuckyCoin = 105,
        C06_EarthBlessing = 106,
        C07_WarriorBracelet = 107,
        C08_WindFeather = 108,
        C09_FocusCrystal = 109,
        C10_BlessedAmulet = 110,
        // Rare 10종
        R01_PhoenixWing = 201,
        R02_HunterEye = 202,
        R03_WeaknessCurse = 203,
        R04_FlipperMaster = 204,
        R05_ElementalAmplifier = 205,
        R06_TimeFragment = 206,
        R07_ComboAlchemy = 207,
        R08_SteelWill = 208,
        R09_SoulHarvester = 209,
        R10_MirrorShield = 210,
        // Legendary 10종
        L01_TwinStar = 301,
        L02_EarthHeart = 302,
        L03_DoomSeal = 303,
        L04_TimeLord = 304,
        L05_StormCrown = 305,
        L06_ElementKingBlessing = 306,
        L07_SteelFortress = 307,
        L08_StarWill = 308,
        L09_InfernoCore = 309,
        L10_JustTiming = 310,
        // Mythic 8종
        M01 = 401,
        M02 = 402,
        M03 = 403,
        M04 = 404,
        M05 = 405,
        M06 = 406,
        M07 = 407,
        M08 = 408
    }

    // ── §의뢰 ───────────────────────────────────────────────
    public enum QuestKind
    {
        Daily = 0,
        Weekly = 1,
        Bounty = 2
    }

    public enum QuestObjectiveKind
    {
        None = 0,
        FlipperSummonLimit = 1,      // 플리퍼 N번 이내 클리어
        GoldCollect = 2,             // 골드 N개 획득
        GimmickExperience = 3,       // 기믹 N종 경험
        BallMaterialClear = 4,       // 지정 재질로 클리어
        NoForcedReset = 5,           // 강제 reset 0회
        EliteDefeat = 6,             // 엘리트 N마리 처치
        ActFullClear = 7,            // 액트 올클리어
        NormalMonsterKill = 8,       // 일반 몬스터 N마리
        BossNoWeakpointHit = 9,      // 약점 미타격 보스 처치
        ComboHold = 10,              // 단일 콤보 유지
        TimePenaltyZero = 11         // 시간 페널티 0회
    }

    // ── §소모품 ────────────────────────────────────────────
    public enum ConsumableId
    {
        None = 0,
        EmergencyShield = 1,
        SandsOfTime = 2,
        FrenzyCharm = 3
    }

    // ── §열기구 ────────────────────────────────────────────
    public enum BalloonUpgradeId
    {
        None = 0,
        Mana20 = 1,
        BossTimeBonus10 = 2,
        HiddenChance15 = 3
    }

    // ── §회복 출처 (MonsterBase.Heal source) ────────────────
    public enum HealSource
    {
        None = 0,
        BossPhotosynthesis = 1,
        KrakenTentacleRegen = 2,
        LeviathanSelfHeal = 3,
        WinterQueenFreeze = 4,
        Other = 99
    }

    // ── §이벤트 노드 보상 종류 ──────────────────────────────
    public enum EventNodeRewardKind
    {
        None = 0,
        Gold = 1,
        ManaCrystal = 2,
        BossSoul = 3,
        SkillPoint = 4,
        RuneNormal = 5,
        RuneRare = 6,
        TarotCommon = 7,
        TarotRare = 8,
        BlueprintFragment = 9,
        SpecialOre = 10
    }
}
