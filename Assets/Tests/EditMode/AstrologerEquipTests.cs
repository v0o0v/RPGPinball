using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    public class AstrologerEquipTests
    {
        private GameObject econHost;
        private GameObject astrHost;
        private EconomyManager econ;
        private AstrologerManager astr;
        private PlayerData pd;

        [SetUp]
        public void SetUp()
        {
            TestSingletonReset.ClearAllManagers();
            econHost = new GameObject("Econ");
            econ = econHost.AddComponent<EconomyManager>();
            pd = ScriptableObject.CreateInstance<PlayerData>();
            pd.gold = 1_000_000;
            econ.Initialize(pd, null);

            astrHost = new GameObject("Astr");
            astr = astrHost.AddComponent<AstrologerManager>();
            astr.InitializeForTest();
            var cat = new TarotCardData[]
            {
                MakeCard(TarotCardId.C01_MoonlightSpring, TarotGrade.Common),
                MakeCard(TarotCardId.C02_GoldenCompass, TarotGrade.Common),
            };
            typeof(AstrologerManager).GetField("cardCatalog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(astr, cat);
        }

        private TarotCardData MakeCard(TarotCardId id, TarotGrade grade)
        {
            var c = ScriptableObject.CreateInstance<TarotCardData>();
            c.cardId = id; c.grade = grade;
            return c;
        }

        [TearDown]
        public void TearDown()
        {
            if (econHost != null) Object.DestroyImmediate(econHost);
            if (astrHost != null) Object.DestroyImmediate(astrHost);
            if (pd != null) Object.DestroyImmediate(pd);
        }

        private void GiveCard(TarotCardId id, TarotGrade grade, int count = 1)
        {
            var f = typeof(AstrologerManager).GetField("inventory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = f.GetValue(astr) as System.Collections.Generic.List<TarotInstance>;
            for (int i = 0; i < count; i++)
            {
                // 동일 카드 누적 표현 — 동일 비영구 인스턴스가 있으면 count++
                bool found = false;
                foreach (var t in list)
                {
                    if (t.id == id && !t.isPermanent) { t.duplicateCount++; found = true; break; }
                }
                if (!found) list.Add(new TarotInstance(id, grade));
            }
        }

        [Test]
        public void Equip_DuplicateBlocked()
        {
            GiveCard(TarotCardId.C01_MoonlightSpring, TarotGrade.Common);
            GiveCard(TarotCardId.C02_GoldenCompass, TarotGrade.Common);
            Assert.IsTrue(astr.Equip(TarotCardId.C01_MoonlightSpring, 0));
            // 같은 카드 다른 슬롯 불가
            Assert.IsFalse(astr.Equip(TarotCardId.C01_MoonlightSpring, 1));
            // 다른 카드 다른 슬롯 가능
            Assert.IsTrue(astr.Equip(TarotCardId.C02_GoldenCompass, 1));
        }

        [Test]
        public void Equip_NoOwnership_Fails()
        {
            // 인벤토리에 없는 카드는 장착 불가
            Assert.IsFalse(astr.Equip(TarotCardId.C01_MoonlightSpring, 0));
        }

        [Test]
        public void UpgradeToPermanent_RequiresTenCopies_AndFiveThousandGold()
        {
            GiveCard(TarotCardId.C01_MoonlightSpring, TarotGrade.Common, count: 10);
            long goldBefore = econ.GetBalance(CurrencyId.Gold);

            Assert.IsTrue(astr.UpgradeToPermanent(TarotCardId.C01_MoonlightSpring));
            Assert.AreEqual(goldBefore - Constants.TarotPermanentGoldCost, econ.GetBalance(CurrencyId.Gold));

            var inst = astr.Find(TarotCardId.C01_MoonlightSpring);
            Assert.IsNotNull(inst);
            Assert.IsTrue(inst.isPermanent);
        }

        [Test]
        public void UpgradeToPermanent_NotEnoughCopies_Fails()
        {
            GiveCard(TarotCardId.C01_MoonlightSpring, TarotGrade.Common, count: 9);
            long goldBefore = econ.GetBalance(CurrencyId.Gold);
            Assert.IsFalse(astr.UpgradeToPermanent(TarotCardId.C01_MoonlightSpring));
            Assert.AreEqual(goldBefore, econ.GetBalance(CurrencyId.Gold));
        }
    }
}
