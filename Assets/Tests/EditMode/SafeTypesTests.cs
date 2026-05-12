using NUnit.Framework;
using RPGPinball.Security;

namespace RPGPinball.Tests.EditMode
{
    public class SafeTypesTests
    {
        [Test]
        public void SafeInt_RoundTrip()
        {
            var s = SafeInt.Create(42);
            Assert.AreEqual(42, s.Value);
            s.Value = -100;
            Assert.AreEqual(-100, s.Value);
        }

        [Test]
        public void SafeInt_ArithmeticOperators()
        {
            var s = SafeInt.Create(10);
            var r1 = s + 5;
            Assert.AreEqual(15, r1.Value);
            var r2 = s - 3;
            Assert.AreEqual(7, r2.Value);
            var r3 = s * 2;
            Assert.AreEqual(20, r3.Value);
        }

        [Test]
        public void SafeFloat_RoundTrip()
        {
            var s = SafeFloat.Create(3.14f);
            Assert.AreEqual(3.14f, s.Value, 0.0001f);
        }

        [Test]
        public void SafeLong_RoundTrip()
        {
            var s = SafeLong.Create(9_999_999_999L);
            Assert.AreEqual(9_999_999_999L, s.Value);
        }
    }
}
