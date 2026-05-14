using System.Collections.Generic;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// band 기반 모디파이어 추첨. 액트 테마 모디파이어는 가중치 ×2.
    /// </summary>
    public static class ModifierPicker
    {
        public static List<ModifierId> Pick(ActId actId, DifficultyBand band, DeterministicRng rng, ModifierPool pool)
        {
            int targetCount = DifficultyBudget.ResolveModifierCount(band, rng);
            var result = new List<ModifierId>();
            if (targetCount <= 0) return result;

            var candidates = new List<(StageModifierData item, float weight)>();
            foreach (var m in pool.All)
            {
                if (m == null || m.modifierId == ModifierId.None) continue;
                float w = 1f;
                if (m.themeOwner == actId && actId != ActId.None) w *= 2f;
                candidates.Add((m, w));
            }
            if (candidates.Count == 0) return result;

            var used = new HashSet<ModifierId>();
            int safety = targetCount * 4;
            while (result.Count < targetCount && safety-- > 0)
            {
                var live = new List<(StageModifierData, float)>();
                foreach (var c in candidates)
                    if (!used.Contains(c.item.modifierId)) live.Add(c);
                if (live.Count == 0) break;

                var picked = rng.WeightedPick(live);
                if (picked == null) break;
                used.Add(picked.modifierId);
                result.Add(picked.modifierId);
            }
            return result;
        }
    }
}
