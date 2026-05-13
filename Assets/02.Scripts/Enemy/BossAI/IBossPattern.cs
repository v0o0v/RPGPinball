using System.Threading;
using Cysharp.Threading.Tasks;

namespace RPGPinball.Enemy.BossAI
{
    /// <summary>
    /// 보스 패턴 인터페이스. 각 보스의 패턴 1개당 하나의 구현체.
    /// BossStateMachine이 Telegraph/Execute/Recovery 사이클 안에서 Execute를 호출.
    /// </summary>
    public interface IBossPattern
    {
        string Id { get; }
        UniTask Execute(BossBase boss, CancellationToken ct);
    }
}
