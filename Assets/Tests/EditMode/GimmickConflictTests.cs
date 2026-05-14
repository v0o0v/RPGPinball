using NUnit.Framework;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Stage.Generation;

namespace RPGPinball.Tests.EditMode
{
    /// <summary>
    /// 마일스톤 5: 충돌 방지 규칙 6쌍 양방향 검증.
    /// Procedural_Stage_Gen.md §4.
    /// </summary>
    [TestFixture]
    public class GimmickConflictTests
    {
        private GimmickData MakeGimmick(GimmickId id, params GimmickId[] conflicting)
        {
            var g = ScriptableObject.CreateInstance<GimmickData>();
            g.gimmickId = id;
            g.conflictingIds = conflicting;
            g.placement = GimmickPlacement.MidSegment;
            return g;
        }

        [Test]
        public void HasConflict_OneSide_DetectsBothDirections()
        {
            var a = MakeGimmick(GimmickId.GravityReversal, GimmickId.IcePatch);
            var b = MakeGimmick(GimmickId.IcePatch);
            // A→B 명시, B→A 미명시. 양방향이어야 충돌.
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
            Assert.IsTrue(GimmickPool.HasConflict(b, a));
        }

        [Test]
        public void HasConflict_NoLink_ReturnsFalse()
        {
            var a = MakeGimmick(GimmickId.HiddenBumper);
            var b = MakeGimmick(GimmickId.JumpPad);
            Assert.IsFalse(GimmickPool.HasConflict(a, b));
        }

        [Test]
        public void Conflict_DarkVeilVsBlindSpot()
        {
            var a = MakeGimmick(GimmickId.DarkVeil, GimmickId.BlindSpot);
            var b = MakeGimmick(GimmickId.BlindSpot);
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
        }

        [Test]
        public void Conflict_PollenSilenceVsFreezingFloor()
        {
            var a = MakeGimmick(GimmickId.PollenSilence, GimmickId.FreezingFloor);
            var b = MakeGimmick(GimmickId.FreezingFloor);
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
        }

        [Test]
        public void Conflict_HeavyBallVsGravityReversal()
        {
            var a = MakeGimmick(GimmickId.HeavyBall, GimmickId.GravityReversal);
            var b = MakeGimmick(GimmickId.GravityReversal);
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
        }

        [Test]
        public void Conflict_InversionMirrorVsCursedFlipper()
        {
            var a = MakeGimmick(GimmickId.InversionMirror, GimmickId.CursedFlipper);
            var b = MakeGimmick(GimmickId.CursedFlipper);
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
        }

        [Test]
        public void Conflict_InputLagCurseVsPollenSilence()
        {
            var a = MakeGimmick(GimmickId.InputLagCurse, GimmickId.PollenSilence);
            var b = MakeGimmick(GimmickId.PollenSilence);
            Assert.IsTrue(GimmickPool.HasConflict(a, b));
        }
    }
}
