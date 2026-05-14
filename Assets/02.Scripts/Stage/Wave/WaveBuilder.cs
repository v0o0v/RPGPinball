using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Stage.Wave
{
    /// <summary>
    /// StageBlueprint.waves 채우기 빌더.
    /// WaveCount = floor(StageIndex/5)+1 (1~7).
    /// 패턴별 몬스터 수·종류 결정 + 엘리트 스폰 확률 적용.
    /// </summary>
    public static class WaveBuilder
    {
        public static List<StageBlueprint.WaveEntry> Build(ActId actId, int stageIndex, DeterministicRng rng, MonsterPool pool)
        {
            int waveCount = Mathf.Clamp((stageIndex / 5) + 1, 1, 7);
            float eliteChance = Mathf.Min(0.5f, 0.05f * stageIndex);

            var result = new List<StageBlueprint.WaveEntry>(waveCount);
            DifficultyBand band = DifficultyBudget.BandFor(stageIndex);
            WaveCompositionPattern? previous = null;

            for (int i = 0; i < waveCount; i++)
            {
                var pattern = WaveCompositionPicker.Pick(band, previous, rng);
                previous = pattern;
                bool hasElite = pattern == WaveCompositionPattern.BossEscort || rng.RollChance(eliteChance);

                var monsterIds = ResolveMonsters(actId, pattern, hasElite, rng, pool);
                result.Add(new StageBlueprint.WaveEntry
                {
                    pattern = pattern,
                    monsterIds = monsterIds,
                    hasElite = hasElite
                });
            }

            return result;
        }

        private static string[] ResolveMonsters(ActId actId, WaveCompositionPattern pattern, bool hasElite, DeterministicRng rng, MonsterPool pool)
        {
            int small, medium;
            switch (pattern)
            {
                case WaveCompositionPattern.MassRush:
                    small = rng.NextInt(8, 13); // 8~12
                    medium = 0;
                    break;
                case WaveCompositionPattern.EliteMinority:
                    small = 0;
                    medium = rng.NextInt(3, 6); // 3~5
                    break;
                case WaveCompositionPattern.BossEscort:
                    small = rng.NextInt(4, 7); // 4~6
                    medium = 0;
                    break;
                default:
                    small = 4; medium = 0; break;
            }

            // 엘리트 출현 시 일반 몬스터 50% 감소 (BossEscort 제외)
            if (hasElite && pattern != WaveCompositionPattern.BossEscort)
            {
                small = Mathf.Max(0, small / 2);
                medium = Mathf.Max(0, medium / 2);
            }

            var list = new List<string>(small + medium);
            AppendByCategory(list, actId, MonsterSizeCategory.Small, small, rng, pool);
            AppendByCategory(list, actId, MonsterSizeCategory.Medium, medium, rng, pool);
            return list.ToArray();
        }

        private static void AppendByCategory(List<string> list, ActId actId, MonsterSizeCategory cat, int count, DeterministicRng rng, MonsterPool pool)
        {
            if (count <= 0) return;
            var candidates = pool.Filter(actId, cat);
            if (candidates.Count == 0)
            {
                // 풀이 비어 있어도 ID는 자리표시자로 기록 (블루프린트 결정론 유지)
                for (int i = 0; i < count; i++) list.Add($"_missing_{cat}_{i}");
                return;
            }
            for (int i = 0; i < count; i++)
            {
                var picked = candidates[rng.NextInt(0, candidates.Count)];
                list.Add(picked.name);
            }
        }
    }
}
