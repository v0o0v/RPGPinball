using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 난이도 예산 / 권장 레벨 / Band / Act 배율 정적 데이터.
    /// Procedural_Stage_Gen.md §3, §5.
    /// </summary>
    public static class DifficultyBudget
    {
        public static float GetActMultiplier(ActId actId)
        {
            switch (actId)
            {
                case ActId.Act1_Spring: return 1.0f;
                case ActId.Act2_Summer: return 1.8f;
                case ActId.Act3_Autumn: return 2.5f;
                case ActId.Act4_Winter: return 3.5f;
                default: return 1.0f;
            }
        }

        /// <summary>액트별 권장 레벨 범위 (시작~끝). Act 1=1~25 / Act 4=72~90.</summary>
        public static (int start, int end) GetActLevelRange(ActId actId)
        {
            switch (actId)
            {
                case ActId.Act1_Spring: return (1, 25);
                case ActId.Act2_Summer: return (25, 50);
                case ActId.Act3_Autumn: return (50, 72);
                case ActId.Act4_Winter: return (72, 90);
                default: return (1, 1);
            }
        }

        public static int ComputeBaseBudget(int stageIndex)
        {
            return Constants.ProcBudgetBase + stageIndex * Constants.ProcBudgetPerStage;
        }

        /// <summary>
        /// 최종 예산 = BaseBudget × ActMultiplier × (1 + Rng[-10%, +10%]).
        /// </summary>
        public static int ComputeFinalBudget(int stageIndex, ActId actId, DeterministicRng rng)
        {
            int baseBudget = ComputeBaseBudget(stageIndex);
            float mul = GetActMultiplier(actId);
            float variance = rng.NextFloat(Constants.ProcBudgetVarianceMin, Constants.ProcBudgetVarianceMax);
            return Mathf.RoundToInt(baseBudget * mul * (1f + variance));
        }

        /// <summary>
        /// 스테이지 권장 레벨 = ActStart + floor(stageIndex × (ActEnd - ActStart) / 30).
        /// </summary>
        public static int ComputeRecommendedLevel(int stageIndex, ActId actId)
        {
            var (start, end) = GetActLevelRange(actId);
            return start + Mathf.FloorToInt(stageIndex * (end - start) / 30f);
        }

        public static DifficultyBand BandFor(int stageIndex)
        {
            if (stageIndex <= 9) return DifficultyBand.Prologue;
            if (stageIndex <= 19) return DifficultyBand.Development;
            return DifficultyBand.Climax;
        }

        /// <summary>난이도 구간별 기믹 수 범위 (스테이지 전체 합계).
        /// 화면 9×16U=144 sq U 의 50% (≈72 sq U) 를 기믹으로 채우는 목표(반지름 0.4 → ~30개/화면)
        /// + 스테이지 ≈ 3.4화면 → 스테이지 전체 80~110개 기준(2026-05-14 재설계).
        /// 좌우 대칭 페어로 배치되므로 결과 수는 항상 짝수에 가까움.</summary>
        public static (int min, int max) GimmickCountRange(DifficultyBand band)
        {
            switch (band)
            {
                case DifficultyBand.Prologue: return (60, 80);
                case DifficultyBand.Development: return (80, 100);
                case DifficultyBand.Climax: return (100, 130);
                default: return (60, 80);
            }
        }

        /// <summary>모디파이어 개수 결정 — 서막 0 / 전개 50%로 1개 / 클라이맥스 1개+30%로 추가 1개.</summary>
        public static int ResolveModifierCount(DifficultyBand band, DeterministicRng rng)
        {
            switch (band)
            {
                case DifficultyBand.Prologue: return 0;
                case DifficultyBand.Development: return rng.RollChance(0.5f) ? 1 : 0;
                case DifficultyBand.Climax: return rng.RollChance(0.3f) ? 2 : 1;
                default: return 0;
            }
        }
    }
}
