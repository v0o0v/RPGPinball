using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Village;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 타로 뽑기 확률 검증 (Common 60 / Rare 25 / Legendary 10 / Mythic 5 %).
    /// </summary>
    public class TarotProbabilityTests
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
            econHost = new GameObject("TestEcon");
            econ = econHost.AddComponent<EconomyManager>();
            pd = ScriptableObject.CreateInstance<PlayerData>();
            pd.gold = int.MaxValue / 2;
            pd.bossSoul = int.MaxValue / 2;
            econ.Initialize(pd, null);

            astrHost = new GameObject("TestAstr");
            astr = astrHost.AddComponent<AstrologerManager>();
            astr.InitializeForTest();

            // 등급별 1장씩 카드 풀 SO 생성
            var catalog = new System.Collections.Generic.List<TarotCardData>();
            catalog.Add(MakeCard(TarotCardId.C01_MoonlightSpring, TarotGrade.Common));
            catalog.Add(MakeCard(TarotCardId.R01_PhoenixWing, TarotGrade.Rare));
            catalog.Add(MakeCard(TarotCardId.L01_TwinStar, TarotGrade.Legendary));
            catalog.Add(MakeCard(TarotCardId.M01, TarotGrade.Mythic));
            typeof(AstrologerManager).GetField("cardCatalog",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(astr, catalog.ToArray());
        }

        private TarotCardData MakeCard(TarotCardId id, TarotGrade grade)
        {
            var c = ScriptableObject.CreateInstance<TarotCardData>();
            c.cardId = id;
            c.grade = grade;
            return c;
        }

        [TearDown]
        public void TearDown()
        {
            if (econHost != null) Object.DestroyImmediate(econHost);
            if (astrHost != null) Object.DestroyImmediate(astrHost);
            if (pd != null) Object.DestroyImmediate(pd);
        }

        [Test]
        public void Pull_GoldCostDeducted()
        {
            long before = econ.GetBalance(CurrencyId.Gold);
            var inst = astr.Pull(false);
            Assert.IsNotNull(inst);
            Assert.AreEqual(before - Constants.TarotPullGoldCost, econ.GetBalance(CurrencyId.Gold));
        }

        [Test]
        public void Pull_BossSoulCostDeducted_WhenSelected()
        {
            long before = econ.GetBalance(CurrencyId.BossSoul);
            var inst = astr.Pull(true);
            Assert.IsNotNull(inst);
            Assert.AreEqual(before - Constants.TarotPullBossSoulCost, econ.GetBalance(CurrencyId.BossSoul));
        }

        [Test]
        public void Pull_GradeDistribution_WithinTolerance_10kRolls()
        {
            astr.SetSeed(42);
            int common = 0, rare = 0, legend = 0, mythic = 0;
            const int N = 10000;
            for (int i = 0; i < N; i++)
            {
                var inst = astr.Pull(false);
                Assert.IsNotNull(inst);
                switch (inst.grade)
                {
                    case TarotGrade.Common: common++; break;
                    case TarotGrade.Rare: rare++; break;
                    case TarotGrade.Legendary: legend++; break;
                    case TarotGrade.Mythic: mythic++; break;
                }
            }
            // ±2% 허용 (60/25/10/5 → ±200/±200/±200/±200 정도 폭)
            Assert.That(common / (float)N, Is.EqualTo(0.60f).Within(0.02f));
            Assert.That(rare / (float)N, Is.EqualTo(0.25f).Within(0.02f));
            Assert.That(legend / (float)N, Is.EqualTo(0.10f).Within(0.02f));
            Assert.That(mythic / (float)N, Is.EqualTo(0.05f).Within(0.02f));
        }
    }
}
