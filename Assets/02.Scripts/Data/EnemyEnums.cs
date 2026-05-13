namespace RPGPinball.Data
{
    /// <summary>
    /// 12종 보스 ID. Boss_Patterns.md 일치.
    /// </summary>
    public enum BossId
    {
        None = 0,

        // ───── 🌸 Act 1: 봄 ─────
        Act1_FleshPlant = 110,
        Act1_FallenFairy = 120,
        Act1_WorldTreeSpirit = 130,

        // ───── 🌊 Act 2: 여름 ─────
        Act2_ArmoredCrab = 210,
        Act2_PirateGhost = 220,
        Act2_Kraken = 230,

        // ───── 🍁 Act 3: 가을 ─────
        Act3_MadInventor = 310,
        Act3_PumpkinGhost = 320,
        Act3_ClockworkDragon = 330,

        // ───── ❄️ Act 4: 겨울 ─────
        Act4_FrostGiant = 410,
        Act4_ClockTowerSentinel = 420,
        Act4_WinterQueen = 430
    }

    /// <summary>
    /// 4종 엘리트 ID. Elite_Bounty_Spec.md 일치.
    /// </summary>
    public enum EliteId
    {
        None = 0,
        StormElemental = 1,       // 봄
        AbyssalLeviathan = 2,     // 여름
        GoldenGoblinKing = 3,     // 가을
        FrostSentinel = 4         // 겨울
    }

    /// <summary>
    /// 보스 페이즈 식별자. 단일 페이즈 보스는 P1만 사용.
    /// </summary>
    public enum BossPhase
    {
        P1,
        P2,
        P3
    }

    /// <summary>
    /// 보스 행동 사이클 상태. Boss_Patterns.md §공통 규칙.
    /// </summary>
    public enum BossActionState
    {
        Idle,
        Telegraph,
        Execute,
        Recovery
    }

    /// <summary>
    /// 탄막 패턴 식별자. BulletEmitter가 dispatch에 사용.
    /// </summary>
    public enum BulletPatternId
    {
        FanShot,             // 부채꼴
        Spiral,              // 본체 회전 나선형
        StraightBurst,       // 직선 연사
        RotatingRay,         // 회전 광선/탄막
        Homing,              // 공 추적 유도
        Concentric,          // 동심원 충격파
        Radial,              // 360° 방사
        Reverse              // 시간 역행
    }

    /// <summary>
    /// 탄막 크기 분류. ProjectileData의 size enum과 의미적으로 일치하나 외부 식별용.
    /// </summary>
    public enum BulletSize
    {
        Small,    // 0.15U
        Large,    // 0.4U
        Special   // 0.6U
    }
}
