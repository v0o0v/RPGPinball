using System.Collections.Generic;
using RPGPinball.Data;

namespace RPGPinball.Stage.Segments
{
    /// <summary>
    /// 세그먼트 조합 결과 컨테이너 (런타임 휘발성).
    /// StageBlueprint에는 ID만 직렬화되지만 빌더는 이 구조체를 통해 동작.
    /// </summary>
    public class SegmentLayout
    {
        public SegmentData top;
        public SegmentData bottom;
        public readonly List<SegmentData> middles = new();
        /// <summary>중단 세그먼트의 실제 적용 높이(빌더 변형 후).</summary>
        public readonly List<float> middleHeights = new();
        public float topHeight;
        public float bottomHeight;
        /// <summary>= top + Σ middle + bottom.</summary>
        public float totalHeight;
        /// <summary>= 카메라 시야 세로 × Constants.SegStageVerticalScreenCount.</summary>
        public float targetStageHeight;

        public bool IsHeightWithinTolerance(float toleranceUnit)
        {
            return UnityEngine.Mathf.Abs(totalHeight - targetStageHeight) <= toleranceUnit;
        }
    }
}
