using System;
using NUnit.Framework;
using RPGPinball.Meta;

namespace RPGPinball.Tests.EditMode
{
    public class MockClockTests
    {
        [Test]
        public void MockClock_AdvanceMovesTimeForward()
        {
            var initial = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
            var clock = new MockClock(initial);
            Assert.AreEqual(initial, clock.UtcNow);
            clock.Advance(TimeSpan.FromHours(25));
            Assert.AreEqual(initial.AddHours(25), clock.UtcNow);
        }

        [Test]
        public void SystemClock_ReturnsCurrentTime()
        {
            var t1 = SystemClock.Instance.UtcNow;
            System.Threading.Thread.Sleep(10);
            var t2 = SystemClock.Instance.UtcNow;
            Assert.That(t2, Is.GreaterThanOrEqualTo(t1));
        }
    }
}
