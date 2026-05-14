using System;

namespace RPGPinball.Meta
{
    /// <summary>
    /// 시계 추상화. 테스트에서 시간을 Mock 으로 주입 가능.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public static readonly SystemClock Instance = new SystemClock();
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    /// <summary>테스트용 Mock 시계.</summary>
    public sealed class MockClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; }
        public MockClock(DateTimeOffset initial) { UtcNow = initial; }
        public void Advance(TimeSpan delta) { UtcNow = UtcNow.Add(delta); }
    }
}
