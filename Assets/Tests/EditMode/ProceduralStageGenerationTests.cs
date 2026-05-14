using NUnit.Framework;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;
using RPGPinball.Stage.Modifiers;
using RPGPinball.Stage.Segments;
using RPGPinball.Stage.Wave;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 절차 생성 통합 검증.
    /// 동일 시드 재현성 + 보상/버프 강제 + 충돌 0건.
    /// </summary>
    [TestFixture]
    public class ProceduralStageGenerationTests
    {
        [SetUp]
        public void SetUp()
        {
            SegmentPool.Instance.OverrideForTest(MakeSegmentPool());
            GimmickPool.Instance.OverrideForTest(MakeGimmickPool());
            MonsterPool.Instance.OverrideForTest(MakeMonsterPool());
            ModifierPool.Instance.OverrideForTest(new StageModifierData[0]);
            MutationPool.Instance.OverrideForTest(new MutationData[0]);
            ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
        }

        private SegmentData[] MakeSegmentPool()
        {
            var top = ScriptableObject.CreateInstance<SegmentData>();
            top.segmentId = "top_test"; top.slot = SegmentSlot.Top;
            top.height = Constants.SegTopHeightDefault; top.heightMin = 3f; top.heightMax = 6f;
            var bot = ScriptableObject.CreateInstance<SegmentData>();
            bot.segmentId = "bot_test"; bot.slot = SegmentSlot.Bottom;
            bot.height = Constants.SegBottomHeightDefault; bot.heightMin = 5f; bot.heightMax = 8f;

            var mids = new System.Collections.Generic.List<SegmentData>();
            for (int i = 0; i < 6; i++)
            {
                var m = ScriptableObject.CreateInstance<SegmentData>();
                m.segmentId = $"mid_test_{i}"; m.slot = SegmentSlot.Middle;
                m.height = 4f; m.heightMin = 3f; m.heightMax = 12f;
                m.gimmickSlotAnchors = new[] { new Vector2(-2, 0), new Vector2(2, 0) };
                mids.Add(m);
            }
            var all = new System.Collections.Generic.List<SegmentData> { top, bot };
            all.AddRange(mids);
            return all.ToArray();
        }

        private GimmickData[] MakeGimmickPool()
        {
            var list = new System.Collections.Generic.List<GimmickData>();
            // 보상 1
            list.Add(MakeG(GimmickId.HiddenBumper, GimmickCategory.Reward, -10));
            // 시련 다수
            list.Add(MakeG(GimmickId.PoisonPool, GimmickCategory.Trial, 30));
            list.Add(MakeG(GimmickId.SpikePit, GimmickCategory.Trial, 30));
            list.Add(MakeG(GimmickId.NetTrap, GimmickCategory.Trial, 30));
            // 환경
            list.Add(MakeG(GimmickId.Wormhole, GimmickCategory.Environment, 15));
            list.Add(MakeG(GimmickId.AccelRail, GimmickCategory.Environment, 10));
            // 봄 전용
            list.Add(MakeG(GimmickId.SeedOfGrowth, GimmickCategory.Buff, -5, ActId.Act1_Spring));
            list.Add(MakeG(GimmickId.FairyBlessing, GimmickCategory.Buff, -8, ActId.Act1_Spring));
            return list.ToArray();
        }

        private GimmickData MakeG(GimmickId id, GimmickCategory cat, int cost, ActId theme = ActId.None)
        {
            var g = ScriptableObject.CreateInstance<GimmickData>();
            g.gimmickId = id; g.category = cat; g.budgetCost = cost;
            g.themeOwner = theme; g.placement = GimmickPlacement.MidSegment;
            return g;
        }

        private MonsterData[] MakeMonsterPool()
        {
            var m = ScriptableObject.CreateInstance<MonsterData>();
            m.maxHp = 100; m.xpReward = 12;
            m.sizeCategory = MonsterSizeCategory.Small;
            return new[] { m };
        }

        [Test]
        public void Generate_SameSeed_ProducesSameBlueprint()
        {
            ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
            var a = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 7, 12345UL);
            ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
            var b = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 7, 12345UL);

            Assert.AreEqual(a.seed, b.seed);
            Assert.AreEqual(a.finalBudget, b.finalBudget);
            Assert.AreEqual(a.recommendedLevel, b.recommendedLevel);
            Assert.AreEqual(a.middleSegmentIds.Count, b.middleSegmentIds.Count);
            Assert.AreEqual(a.gimmickPlacements.Count, b.gimmickPlacements.Count);
            Assert.AreEqual(a.waves.Count, b.waves.Count);
        }

        [Test]
        public void Generate_Stage10_IsBossNode()
        {
            var bp = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 10, 999UL);
            Assert.AreEqual(NodeKind.Boss, bp.nodeKind);
        }

        [Test]
        public void Generate_Stage5_IsRestOrEvent()
        {
            var bp = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 5, 999UL);
            Assert.That(bp.nodeKind == NodeKind.Rest || bp.nodeKind == NodeKind.Event);
        }

        [Test]
        public void Generate_NormalStage_HasRewardOrBuff()
        {
            // 100 시드 모두에서 보상/버프 1개 이상 강제
            for (int i = 0; i < 30; i++)
            {
                ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
                var bp = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 7, (ulong)(2000 + i));
                int rewards = 0;
                foreach (var p in bp.gimmickPlacements)
                {
                    var data = GimmickPool.Instance.Get(p.id);
                    if (data == null) continue;
                    if (data.category == GimmickCategory.Reward || data.category == GimmickCategory.Buff) rewards++;
                }
                Assert.GreaterOrEqual(rewards, Constants.ProcRewardOrBuffMin,
                    $"seed={2000 + i}: 보상/버프 부족 ({rewards}). 배치={bp.gimmickPlacements.Count}");
            }
        }

        [Test]
        public void Generate_NoConflictingGimmicks()
        {
            for (int i = 0; i < 30; i++)
            {
                ProceduralStageGenerator.PreviousStageGimmickIds.Clear();
                var bp = ProceduralStageGenerator.Generate(ActId.Act1_Spring, 15, (ulong)(3000 + i));
                for (int a = 0; a < bp.gimmickPlacements.Count; a++)
                {
                    for (int b = a + 1; b < bp.gimmickPlacements.Count; b++)
                    {
                        var ga = GimmickPool.Instance.Get(bp.gimmickPlacements[a].id);
                        var gb = GimmickPool.Instance.Get(bp.gimmickPlacements[b].id);
                        Assert.IsFalse(GimmickPool.HasConflict(ga, gb),
                            $"seed={3000 + i}: 충돌 발견 {ga?.gimmickId} ↔ {gb?.gimmickId}");
                    }
                }
            }
        }
    }
}
