using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Tests.EditMode
{
    public class PlayerPrefsSaveAdapterTests
    {
        [System.Serializable]
        public class TestPayload
        {
            public int gold;
            public string name;
            public int[] items;
        }

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("RPGPinball.Test.Payload");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("RPGPinball.Test.Payload");
        }

        [Test]
        public void Save_Then_Load_RoundTripsCorrectly()
        {
            var p = new TestPayload { gold = 5000, name = "테스트", items = new[] { 1, 2, 3 } };
            PlayerPrefsSaveAdapter.Save("RPGPinball.Test.Payload", p);
            var loaded = PlayerPrefsSaveAdapter.Load<TestPayload>("RPGPinball.Test.Payload");
            Assert.IsNotNull(loaded);
            Assert.AreEqual(5000, loaded.gold);
            Assert.AreEqual("테스트", loaded.name);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, loaded.items);
        }

        [Test]
        public void Load_MissingKey_ReturnsNull()
        {
            var loaded = PlayerPrefsSaveAdapter.Load<TestPayload>("RPGPinball.Test.DoesNotExist");
            Assert.IsNull(loaded);
        }

        [Test]
        public void Delete_RemovesKey()
        {
            var p = new TestPayload { gold = 10, name = "x", items = new int[0] };
            PlayerPrefsSaveAdapter.Save("RPGPinball.Test.Payload", p);
            Assert.IsTrue(PlayerPrefsSaveAdapter.Has("RPGPinball.Test.Payload"));
            PlayerPrefsSaveAdapter.Delete("RPGPinball.Test.Payload");
            Assert.IsFalse(PlayerPrefsSaveAdapter.Has("RPGPinball.Test.Payload"));
        }

        [Test]
        public void Save_NullData_DoesNothing()
        {
            PlayerPrefsSaveAdapter.Save<TestPayload>("RPGPinball.Test.Null", null);
            Assert.IsFalse(PlayerPrefsSaveAdapter.Has("RPGPinball.Test.Null"));
        }
    }
}
