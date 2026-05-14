namespace RPGPinball.Stage.Gimmicks.Bases
{
    /// <summary>
    /// 주기 트리거 공통 (꽃가루 침묵 / 강풍 경보 / 도개교 / 댄싱 범퍼).
    /// GimmickData.triggerIntervalSeconds 주기로 HandlePeriodicTick 호출.
    /// </summary>
    public abstract class PeriodicGimmickBase : GimmickBase
    {
        // GimmickBase.Update가 Periodic 트리거 분기를 이미 처리하므로 추가 코드 불필요.
        // 자식 클래스는 HandlePeriodicTick() 오버라이드해 동작 정의.
    }
}
