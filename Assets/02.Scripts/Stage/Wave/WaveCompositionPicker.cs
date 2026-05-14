using System.Collections.Generic;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Wave
{
    /// <summary>
    /// 웨이브 패턴 가중치 추첨. 직전 웨이브 패턴은 가중치 ×0.5.
    /// 클라이맥스에서 "보스 호위" 가중치 ×1.5.
    /// </summary>
    public static class WaveCompositionPicker
    {
        public static WaveCompositionPattern Pick(DifficultyBand band, WaveCompositionPattern? previous, DeterministicRng rng)
        {
            var entries = new List<(WaveCompositionPattern item, float weight)>(3)
            {
                (WaveCompositionPattern.MassRush, 1f),
                (WaveCompositionPattern.EliteMinority, 1f),
                (WaveCompositionPattern.BossEscort, 1f)
            };

            // 클라이맥스에서 보스 호위 가중치 ×1.5
            if (band == DifficultyBand.Climax)
            {
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].item == WaveCompositionPattern.BossEscort)
                        entries[i] = (entries[i].item, entries[i].weight * 1.5f);
            }

            // 직전 웨이브 패턴 ×0.5
            if (previous.HasValue)
            {
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].item == previous.Value)
                        entries[i] = (entries[i].item, entries[i].weight * 0.5f);
            }

            return rng.WeightedPick(entries);
        }
    }
}
