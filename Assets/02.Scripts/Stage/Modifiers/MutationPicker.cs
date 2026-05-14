using System.Collections.Generic;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// 5% 확률로 돌연변이 1종 추첨. band 필터로 일부는 클라이맥스 한정.
    /// </summary>
    public static class MutationPicker
    {
        /// <summary>
        /// 5% 확률 판정 후 적격 후보 중 1종 반환. None = 미발생.
        /// </summary>
        public static MutationId TryPick(DifficultyBand band, DeterministicRng rng, MutationPool pool)
        {
            if (!rng.RollChance(Core.Constants.ProcMutationChance)) return MutationId.None;

            var candidates = new List<MutationData>();
            foreach (var m in pool.All)
            {
                if (m == null) continue;
                if (band == DifficultyBand.Prologue && !m.allowPrologue) continue;
                if (band == DifficultyBand.Development && !m.allowDevelopment) continue;
                if (band == DifficultyBand.Climax && !m.allowClimax) continue;
                candidates.Add(m);
            }
            if (candidates.Count == 0) return MutationId.None;
            return candidates[rng.NextInt(0, candidates.Count)].mutationId;
        }
    }
}
