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
}
