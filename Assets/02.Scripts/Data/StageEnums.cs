using System;

namespace RPGPinball.Data
{
    // 액트(계절). 권장 레벨 / 난이도 배율 / 테마 풀의 기준.
    public enum ActId
    {
        None = 0,
        Act1_Spring = 1,
        Act2_Summer = 2,
        Act3_Autumn = 3,
        Act4_Winter = 4
    }

    // 스테이지 진행 난이도 구간 (Procedural_Stage_Gen.md §3·§5).
    public enum DifficultyBand
    {
        Prologue,     // 1~9 서막
        Development,  // 11~19 전개
        Climax        // 21~29 클라이맥스
    }

    // 세그먼트 슬롯 (Procedural_Stage_Gen.md §2).
    public enum SegmentSlot
    {
        Top,     // 보스/엘리트 배치 영역
        Middle,  // 기믹/레일/함정 배치 영역
        Bottom   // 플리퍼 + 낙사 영역 (고정)
    }

    // 기믹 분류 (예산 부호 결정).
    public enum GimmickCategory
    {
        Buff,
        Debuff,
        Trial,
        Reward,
        Environment
    }

    // 기믹 배치 가능 영역 (flags).
    [Flags]
    public enum GimmickPlacement
    {
        None = 0,
        TopSegment = 1 << 0,
        MidSegment = 1 << 1,
        BotSegment = 1 << 2,
        GlobalModifier = 1 << 3,
        BossRoom = 1 << 4,
        AnySegment = TopSegment | MidSegment | BotSegment
    }

    // 80종 기믹 ID. Gimmick_List.md 번호 일치.
    public enum GimmickId
    {
        None = 0,

        // ── 공통 베이스 20종 (1~20) ──
        HiddenBumper = 1,
        HpBumper = 2,
        Wormhole = 3,
        AccelRail = 4,
        JumpPad = 5,
        OneWayGate = 6,
        ExplosiveBarrel = 7,
        TeleportPanel = 8,
        Rotary = 9,
        MagneticBlock = 10,
        TimeOrb = 11,
        GiantPotion = 12,
        RainbowStar = 13,
        DiceRoller = 14,
        ManaShrine = 15,
        InversionMirror = 16,
        DarkVeil = 17,
        HeavyBall = 18,
        RouletteOfFate = 19,
        ThiefGoblin = 20,

        // ── 봄 전용 15종 (21~35) ──
        StickyWeb = 21,
        PoisonPool = 22,
        WindTunnel = 23,
        SeedOfGrowth = 24,
        FairyBlessing = 25,
        HallucinationMushroom = 26,
        CrumblingFloor = 27,
        SpikePit = 28,
        AngelWings = 29,
        PurifyingFlame = 30,
        BerserkPotion = 31,
        GiantSlime = 32,
        GaleWarning = 33,
        PollenSilence = 34,
        GhostBumper = 35,

        // ── 여름 전용 15종 (36~50) ──
        Whirlpool = 36,
        NetTrap = 37,
        Drawbridge = 38,
        TimeBomb = 39,
        ReverseEscalator = 40,
        SpringWall = 41,
        IronSkin = 42,
        MirrorShield = 43,
        Earthquake = 44,
        ManaLeech = 45,
        EnrageOrb = 46,
        WhiteNoise = 47,
        EmpStorm = 48,
        Oasis = 49,
        MagneticMine = 50,

        // ── 가을 전용 15종 (51~65) ──
        GearRotary = 51,
        LaserFence = 52,
        MissilePod = 53,
        ManaReactor = 54,
        SkillBook = 55,
        ClockworkTrap = 56,
        FakeFlipper = 57,
        BlindSpot = 58,
        GravityReversal = 59,
        CursedFlipper = 60,
        MiniBlackhole = 61,
        Doppelganger = 62,
        BloodPact = 63,
        CasinoBumper = 64,
        Blackmarket = 65,

        // ── 겨울 전용 15종 (66~80) ──
        IcePatch = 66,
        FreezingFloor = 67,
        ShatteredMirror = 68,
        ReflectingPrism = 69,
        CrystalPillar = 70,
        GoldenEgg = 71,
        IceWall = 72,
        Hourglass = 73,
        InputLagCurse = 74,
        MeteorShower = 75,
        AbsoluteZero = 76,
        Avalanche = 77,
        IceSpike = 78,
        Aurora = 79,
        HolyRelic = 80
    }

    // 기믹 트리거 분류.
    public enum GimmickTriggerKind
    {
        BallContact,
        Periodic,
        StageEnter,
        ExternalEvent,
        BossHpThreshold
    }

    // 웨이브 구성 패턴 3종 (Procedural_Stage_Gen.md §5).
    public enum WaveCompositionPattern
    {
        MassRush,        // 물량 러시
        EliteMinority,   // 정예 소수
        BossEscort       // 보스 호위 (엘리트 1 + 졸개)
    }

    // 노드 유형.
    public enum NodeKind
    {
        NormalBattle,
        EliteBattle,
        Boss,
        Event,
        Rest,
        Hidden,
        Tutorial
    }

    // 이벤트 노드 4종.
    public enum EventNodeKind
    {
        SuspiciousTraveler,
        TreasureRoom,
        MysticAltar,
        WanderersGamble
    }

    // 스테이지 모디파이어(특성) 18종.
    public enum ModifierId
    {
        None = 0,

        // 공통 10종
        BlitzMatch = 1,        // 쾌속전
        IronFortress = 2,      // 철벽 요새
        ReapersTouch = 3,      // 사신의 손길
        FranticPitch = 4,      // 광란의 피치
        ChaosGravity = 5,      // 혼돈의 중력
        GoldRush = 6,          // 황금 열풍
        ExposedWeakness = 7,   // 약점 노출
        EternalFrost = 8,      // 영겁의 서리
        GamblersNight = 9,     // 도박사의 밤
        ManaStorm = 10,        // 마력 폭풍

        // 봄 2종
        PollenAllergy = 21,    // 꽃가루 알레르기
        BlossomForest = 22,    // 만개의 숲

        // 여름 2종
        TideCycle = 36,        // 밀물과 썰물
        PirateCurse = 37,      // 해적의 저주

        // 가을 2종
        MachineMalfunction = 51, // 기계 오작동
        GhostsJoke = 52,         // 유령의 농담

        // 겨울 2종
        Blizzard = 66,         // 블리자드
        TimeWarp = 67          // 시간 왜곡
    }

    // 돌연변이 스테이지 5종.
    public enum MutationId
    {
        None = 0,
        ThemeErosion = 1,  // 테마 침식 (전개 이후)
        MirrorWorld = 2,   // 거울 세계
        Miniature = 3,     // 미니어처 (클라이맥스만)
        TimeRush = 4,      // 타임 러시
        BossRush = 5       // 보스 러시 (클라이맥스만)
    }
}
