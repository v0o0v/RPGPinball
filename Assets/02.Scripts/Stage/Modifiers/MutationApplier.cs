using UnityEngine;
using RPGPinball.Data;

namespace RPGPinball.Stage.Modifiers
{
    /// <summary>
    /// 돌연변이 효과를 StageBlueprint / Scene에 적용.
    /// 일부 효과는 StageBlueprint 단계에서 반영(예: 타임 러시 → timeLimit 강제),
    /// 일부는 Scene 빌드 시점에서 반영(예: 거울 세계 → 레이아웃 수평 반전).
    /// </summary>
    public static class MutationApplier
    {
        /// <summary>
        /// 블루프린트에 적용. 타임 러시·테마 침식 등 비주얼 외 데이터 변경.
        /// </summary>
        public static void ApplyToBlueprint(StageBlueprint blueprint, MutationPool pool)
        {
            if (blueprint == null || blueprint.mutationId == MutationId.None) return;
            var data = pool.Get(blueprint.mutationId);
            if (data == null) return;

            switch (blueprint.mutationId)
            {
                case MutationId.TimeRush:
                    if (data.forceTimeLimit) blueprint.timeLimitSeconds = data.forcedTimeLimitSeconds;
                    break;
                case MutationId.ThemeErosion:
                case MutationId.MirrorWorld:
                case MutationId.Miniature:
                case MutationId.BossRush:
                    // 후속 단계(StageRuntimeBuilder)에서 처리
                    break;
            }
        }

        /// <summary>
        /// Scene 빌드 시점 적용. 거울 세계의 좌우 반전, 미니어처의 플레이필드 축소 등.
        /// 마일스톤 5는 데이터 단까지만 적용 — 시각 효과는 마일스톤 8 인계.
        /// </summary>
        public static void ApplyToScene(StageBlueprint blueprint, Transform stageRoot, MutationPool pool)
        {
            if (blueprint == null || blueprint.mutationId == MutationId.None || stageRoot == null) return;
            var data = pool.Get(blueprint.mutationId);
            if (data == null) return;

            switch (blueprint.mutationId)
            {
                case MutationId.MirrorWorld:
                    if (data.mirrorLayoutHorizontal)
                    {
                        var s = stageRoot.localScale;
                        s.x = -Mathf.Abs(s.x);
                        stageRoot.localScale = s;
                    }
                    break;
                case MutationId.Miniature:
                    if (data.playfieldScaleMultiplier > 0f && Mathf.Abs(data.playfieldScaleMultiplier - 1f) > 0.001f)
                    {
                        stageRoot.localScale *= data.playfieldScaleMultiplier;
                    }
                    break;
            }
        }
    }
}
