using UnityEngine;

namespace RPGPinball.Physics
{
    /// <summary>
    /// [DEPRECATED 2026-05-13] 데드존 개념 제거. 스테이지 외벽이 닫힌 통이므로 공은 절대 떨어지지 않음.
    /// 빈 컴포넌트로 남겨두어 기존 씬·프리팹의 참조 깨짐을 방지. 신규 씬에는 부착하지 말 것.
    /// 트리거 동작이 필요하면 BossFightContext / 보스 패턴에서 EventBus.OnTimePenalty 를 직접 발행.
    /// </summary>
    [System.Obsolete("DeadZone 개념은 마일스톤 5에서 제거됨. 외벽 닫힌 통으로 대체.")]
    public class DeadZone : MonoBehaviour
    {
        // 의도적으로 동작 없음. 씬 호환성 유지용 placeholder.
    }
}
