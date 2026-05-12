namespace RPGPinball.Data
{
    public enum SkillCategory
    {
        Control,
        Destruction,
        Element
    }

    public enum SkillType
    {
        Passive,
        Active,
        ActiveSwitch
    }

    public enum SkillShape
    {
        Circle,
        Rectangle,
        Sector,
        Line,
        Global
    }

    public enum DamageType
    {
        Physical,
        Magic
    }

    public enum KnockbackTier
    {
        None,
        Resist,
        Immune,
        Absolute
    }

    /// <summary>
    /// 분기 enum. SkillCategory와 동일한 의미이나 SkillTreeManager에서 가독성을 위해 사용.
    /// </summary>
    public enum SkillBranch
    {
        Control,
        Destruction,
        Element
    }

    /// <summary>
    /// 공 재질 enum. Data_Schema.md §11 ballState.currentMaterial 일치.
    /// </summary>
    public enum BallMaterial
    {
        Wood,
        Steel,
        Mithril,
        Volcanic
    }

    /// <summary>
    /// A전환 스킬 변신 상태. Data_Schema.md §11 ballState.activeTransformation 일치.
    /// </summary>
    public enum BallTransformation
    {
        None,
        Fireball,
        IceForm,
        Thunderball,
        GiantBall
    }

    /// <summary>
    /// 코어 종류. Game_Design_Spec.md §3 대장간 코어 목록 기반.
    /// 마일스톤 3에서는 Acceleration/Split/Chrono만 효과 활성, 나머지는 데이터 자리만 잡음.
    /// </summary>
    public enum CoreId
    {
        None,
        Acceleration,   // 공 속도 비례 추가 데미지
        Split,          // 분열 공 데미지 페널티 완화
        Chrono,         // 처치 시 시간 회복
        Guardian,       // 방어/저항 (마일스톤 6)
        Focus,          // 크리티컬 (마일스톤 6)
        Blessing        // 마나/효율 (마일스톤 6)
    }

    public enum CoreGrade
    {
        Normal,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// 60종 스킬 ID enum. Skill_Tree_Diagram.md / Skill_Tree_Formulas.md 일치.
    /// 분기별 1~20번을 (분기 * 100 + 번호)로 매핑하여 향후 확장성 확보.
    /// 마일스톤 2에서 사용된 ID(제어 10~15·파괴 20~23·원소 30~36)는 SO 인스턴스의 'id' 필드 값.
    /// 본 enum은 정식 코드 참조용. SkillData.id를 (int)SkillId.X로 캐스팅하여 동기화.
    /// </summary>
    public enum SkillId
    {
        None = 0,

        // ───── 🟢 제어 (Control) 100번대 ─────
        // Tier 1
        ControlFlipperLight1 = 101,
        ControlFlipperLight2 = 102,
        ControlElasticBoost = 103,
        // Tier 2
        ControlFastDraw = 104,
        ControlLongFlipper = 105,
        ControlMagneticField = 106,
        // Tier 3
        ControlQuickRecovery = 107,
        ControlEnergyShield = 108,
        ControlEnhancedMagneticField = 109,
        ControlWideAngle = 110,
        // Tier 4
        ControlTripleDraw = 111,
        ControlSafetyWall = 112,
        ControlTrampolineExcel = 113,
        ControlJustGuard = 114,
        // Tier 5
        ControlPerfectDefense = 115,
        ControlTimeAura = 116,
        ControlReboundShield = 117,
        ControlMassRecall = 118,
        // Tier 6 (궁극기)
        ControlTimeDilation = 119,
        ControlZoneOfControl = 120,

        // ───── 🔴 파괴 (Destruction) 200번대 ─────
        // Tier 1
        DestSteelBall1 = 201,
        DestSteelBall2 = 202,
        DestCharge = 203,
        // Tier 2
        DestInertiaBreak1 = 204,
        DestInertiaBreak2 = 205,
        DestDestructionInstinct = 206,
        // Tier 3
        DestComboStrike = 207,
        DestWeakPoint = 208,
        DestHeavyBlow = 209,
        DestTimeBreaker = 210,
        // Tier 4
        DestHyperCombo = 211,
        DestSonicBoom = 212,
        DestGiantBall = 213,
        DestFuryStrike = 214,
        // Tier 5
        DestSpeedThrill = 215,
        DestArmorCrash = 216,
        DestFlipperSmash = 217,
        DestHeavyAccelerator = 218,
        // Tier 6 (궁극기)
        DestMeteorStrike = 219,
        DestZeroBlade = 220,

        // ───── 🔵 원소 (Element) 300번대 ─────
        // Tier 1
        ElemAffinity1 = 301,
        ElemAffinity2 = 302,
        ElemManaCharge = 303,
        // Tier 2
        ElemFireballI = 304,
        ElemFireballII = 305,
        ElemSpark = 306,
        // Tier 3
        ElemIceFormI = 307,
        ElemIceFormII = 308,
        ElemChainLightning1 = 309,
        ElemFlameTrail = 310,
        // Tier 4
        ElemDualElement = 311,
        ElemChainLightning2 = 312,
        ElemMultiBallI = 313,
        ElemFrostExplosion = 314,
        // Tier 5
        ElemMultiBallII = 315,
        ElemElementalFusion = 316,
        ElemThunderbolt = 317,
        ElemVortex = 318,
        // Tier 6 (궁극기)
        ElemElementalRampage = 319,
        ElemArmageddon = 320
    }
}
