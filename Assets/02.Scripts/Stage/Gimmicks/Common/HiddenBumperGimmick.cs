using RPGPinball.Physics;
using RPGPinball.Stage.Gimmicks.Bases;

namespace RPGPinball.Stage.Gimmicks.Common
{
    /// <summary>
    /// 1. 히든 범퍼 — XP +50, 마나 +15, 골드 +30, 강한 반발 임펄스 20N, 1회성.
    /// SO에서 모든 수치 로드.
    /// </summary>
    public class HiddenBumperGimmick : BumperGimmickBase
    {
        // 동작은 BumperGimmickBase.HandleBallContact 로 충분.
        // 추가 보상 지급(XP/골드)은 M6 EconomyManager 도입 후 분기.
    }
}
