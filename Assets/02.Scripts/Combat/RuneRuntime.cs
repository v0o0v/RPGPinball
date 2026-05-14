using System.Collections.Generic;
using RPGPinball.Data;
using RPGPinball.Village;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 액티브 스킬 실행 직전 룬 효과를 DamageContext 에 합성하는 정적 헬퍼.
    /// ActiveSkillBase.OnFire 호출 직전에 호출.
    /// </summary>
    public static class RuneRuntime
    {
        /// <summary>
        /// 지정 스킬에 장착된 룬을 DamageContext 에 주입.
        /// EquippedRuneIds / EquippedRuneGrades / ElementOverride / ManaCostMultiplier 채움.
        /// </summary>
        public static void ApplyTo(string skillId, ref DamageContext ctx)
        {
            if (EnchanterManager.Instance == null) return;
            var runes = EnchanterManager.Instance.GetEquippedRunes(skillId);
            if (runes == null || runes.Count == 0) return;

            var ids = new List<RuneId>(runes.Count);
            var grades = new List<RuneGrade>(runes.Count);
            float manaMul = ctx.ManaCostMultiplier <= 0f ? 1f : ctx.ManaCostMultiplier;

            foreach (var r in runes)
            {
                ids.Add(r.id);
                grades.Add(r.grade);

                switch (r.id)
                {
                    case RuneId.FireConvert:
                        ctx.ElementOverride = ElementOverride.Fire;
                        ctx.DamageType = DamageType.Magic;
                        break;
                    case RuneId.IceConvert:
                        ctx.ElementOverride = ElementOverride.Ice;
                        ctx.DamageType = DamageType.Magic;
                        break;
                    case RuneId.LightningConvert:
                        ctx.ElementOverride = ElementOverride.Lightning;
                        ctx.DamageType = DamageType.Magic;
                        break;
                    case RuneId.Chain:
                        // 콤보 ≥ 20 시 마나 비용 ×0.5
                        if (ctx.ComboCount >= 20) manaMul *= 0.5f;
                        break;
                    default: break;
                }
            }

            ctx.EquippedRuneIds = ids.ToArray();
            ctx.EquippedRuneGrades = grades.ToArray();
            ctx.ManaCostMultiplier = manaMul;
        }

        /// <summary>장착된 모든 룬을 ID/Grade 배열로 노출. UI/디버그용.</summary>
        public static (RuneId[] ids, RuneGrade[] grades) Snapshot(string skillId)
        {
            var emptyId = System.Array.Empty<RuneId>();
            var emptyGrade = System.Array.Empty<RuneGrade>();
            if (EnchanterManager.Instance == null) return (emptyId, emptyGrade);
            var runes = EnchanterManager.Instance.GetEquippedRunes(skillId);
            if (runes == null || runes.Count == 0) return (emptyId, emptyGrade);
            var ids = new RuneId[runes.Count];
            var grades = new RuneGrade[runes.Count];
            for (int i = 0; i < runes.Count; i++)
            {
                ids[i] = runes[i].id;
                grades[i] = runes[i].grade;
            }
            return (ids, grades);
        }
    }
}
