using System.Collections.Generic;
using RPGPinball.Data;

namespace RPGPinball.Stage.Generation
{
    /// <summary>
    /// 전통 핀볼 정석 4구역. 스테이지 세로를 3등분(상/중/하 각 ~18U) 한 화면에 맵핑.
    /// TopLane = top 세그먼트 + middle[2] 상단 / BumperCluster = middle[2] / MidPlay = middle[1] / SlingshotZone = middle[0].
    /// </summary>
    public enum PinballZone
    {
        TopLane,        // 상단 롤오버 레인 (이동·텔레포트류)
        BumperCluster,  // 중상단 범퍼 삼각형
        MidPlay,        // 중간 램프·드롭 타겟·보상
        SlingshotZone   // 하단 슬링샷·인레인·함정
    }

    /// <summary>
    /// 기믹 ID → PinballZone 정적 매핑.
    /// 명시 매핑 우선, 없으면 GimmickCategory fallback.
    /// SegmentLayoutBuilder 가 middle 세그먼트를 정확히 3개 만든다고 가정:
    ///   middle[0] = SlingshotZone, middle[1] = MidPlay, middle[2] = BumperCluster, top = TopLane.
    /// </summary>
    public static class PinballZoneMap
    {
        // 인덱스 컨벤션: top=0, middle[0..N-1] = 1..N, bottom = N+1 (StageRuntimeBuilder.ResolveSegment).
        public const int SegmentIndexTop = 0;
        public const int SegmentIndexSlingshot = 1; // middle[0]
        public const int SegmentIndexMidPlay = 2;   // middle[1]
        public const int SegmentIndexBumper = 3;    // middle[2]

        private static readonly Dictionary<GimmickId, PinballZone> Explicit = new()
        {
            // TopLane — 롤오버·이동·텔레포트
            { GimmickId.AccelRail,        PinballZone.TopLane },
            { GimmickId.Wormhole,         PinballZone.TopLane },
            { GimmickId.TeleportPanel,    PinballZone.TopLane },
            { GimmickId.OneWayGate,       PinballZone.TopLane },
            { GimmickId.WindTunnel,       PinballZone.TopLane },
            { GimmickId.Drawbridge,       PinballZone.TopLane },
            { GimmickId.ReverseEscalator, PinballZone.TopLane },
            { GimmickId.GearRotary,       PinballZone.TopLane },
            { GimmickId.Rotary,           PinballZone.TopLane },

            // BumperCluster — 반발·범퍼
            { GimmickId.HiddenBumper,     PinballZone.BumperCluster },
            { GimmickId.HpBumper,         PinballZone.BumperCluster },
            { GimmickId.GhostBumper,      PinballZone.BumperCluster },
            { GimmickId.CasinoBumper,     PinballZone.BumperCluster },
            { GimmickId.SpringWall,       PinballZone.BumperCluster },
            { GimmickId.ManaShrine,       PinballZone.BumperCluster },

            // MidPlay — 보상·드롭 타겟·스피너
            { GimmickId.TimeOrb,          PinballZone.MidPlay },
            { GimmickId.SkillBook,        PinballZone.MidPlay },
            { GimmickId.DiceRoller,       PinballZone.MidPlay },
            { GimmickId.ManaReactor,      PinballZone.MidPlay },
            { GimmickId.FairyBlessing,    PinballZone.MidPlay },
            { GimmickId.AngelWings,       PinballZone.MidPlay },
            { GimmickId.MirrorShield,     PinballZone.MidPlay },
            { GimmickId.ReflectingPrism,  PinballZone.MidPlay },
            { GimmickId.CrystalPillar,    PinballZone.MidPlay },
            { GimmickId.GoldenEgg,        PinballZone.MidPlay },
            { GimmickId.Hourglass,        PinballZone.MidPlay },
            { GimmickId.Aurora,           PinballZone.MidPlay },
            { GimmickId.HolyRelic,        PinballZone.MidPlay },
            { GimmickId.SeedOfGrowth,     PinballZone.MidPlay },
            { GimmickId.PurifyingFlame,   PinballZone.MidPlay },
            { GimmickId.IronSkin,         PinballZone.MidPlay },
            { GimmickId.Oasis,            PinballZone.MidPlay },
            { GimmickId.RainbowStar,      PinballZone.MidPlay },
            { GimmickId.GiantPotion,      PinballZone.MidPlay },
            { GimmickId.JumpPad,          PinballZone.MidPlay },

            // SlingshotZone — 슬링샷·함정·디버프
            { GimmickId.SpikePit,         PinballZone.SlingshotZone },
            { GimmickId.PoisonPool,       PinballZone.SlingshotZone },
            { GimmickId.NetTrap,          PinballZone.SlingshotZone },
            { GimmickId.LaserFence,       PinballZone.SlingshotZone },
            { GimmickId.StickyWeb,        PinballZone.SlingshotZone },
            { GimmickId.IceSpike,         PinballZone.SlingshotZone },
            { GimmickId.FreezingFloor,    PinballZone.SlingshotZone },
            { GimmickId.IcePatch,         PinballZone.SlingshotZone },
            { GimmickId.HallucinationMushroom, PinballZone.SlingshotZone },
            { GimmickId.CrumblingFloor,   PinballZone.SlingshotZone },
            { GimmickId.Earthquake,       PinballZone.SlingshotZone },
            { GimmickId.MagneticMine,     PinballZone.SlingshotZone },
            { GimmickId.ClockworkTrap,    PinballZone.SlingshotZone },
            { GimmickId.GravityReversal,  PinballZone.SlingshotZone },
        };

        public static PinballZone Resolve(GimmickData data)
        {
            if (data == null) return PinballZone.MidPlay;
            if (Explicit.TryGetValue(data.gimmickId, out var z)) return z;
            // 카테고리 fallback
            switch (data.category)
            {
                case GimmickCategory.Reward:      return PinballZone.MidPlay;
                case GimmickCategory.Buff:        return PinballZone.MidPlay;
                case GimmickCategory.Environment: return PinballZone.TopLane;
                case GimmickCategory.Trial:       return PinballZone.SlingshotZone;
                case GimmickCategory.Debuff:      return PinballZone.SlingshotZone;
                default:                          return PinballZone.MidPlay;
            }
        }

        /// <summary>
        /// PinballZone → StageBlueprint.GimmickPlacementEntry.segmentIndex.
        /// middle 이 3개 미만이면 가용 인덱스로 클램프.
        /// </summary>
        public static int SegmentIndexFor(PinballZone zone, int middleCount)
        {
            if (middleCount <= 0) return SegmentIndexTop;
            switch (zone)
            {
                case PinballZone.TopLane:
                    return SegmentIndexTop;
                case PinballZone.BumperCluster:
                    // middle 상단 = middle[middleCount-1]
                    return UnityEngine.Mathf.Min(middleCount, SegmentIndexBumper);
                case PinballZone.MidPlay:
                    // middle 중간
                    return middleCount >= 3 ? SegmentIndexMidPlay : 1 + middleCount / 2;
                case PinballZone.SlingshotZone:
                    // middle 하단 = middle[0]
                    return SegmentIndexSlingshot;
                default:
                    return SegmentIndexMidPlay;
            }
        }

        /// <summary>각 zone 의 최소 필수 기믹 수 (좌우 페어 단위).</summary>
        public static int MinRequiredCount(PinballZone zone)
        {
            switch (zone)
            {
                case PinballZone.TopLane:       return 2;   // 좌·우 페어
                case PinballZone.BumperCluster: return 4;   // 좌·우 페어 + 중앙 페어 = 4 (혹은 좌·우·중3)
                case PinballZone.MidPlay:       return 4;   // 좌·우 페어 × 2
                case PinballZone.SlingshotZone: return 2;   // 좌·우 페어
                default: return 2;
            }
        }

        /// <summary>각 zone 의 격자 셀 수용량 (cols=4 × rows). 비율 기반 분배에 사용.</summary>
        public static int MaxCapacity(PinballZone zone)
        {
            // 2026-05-14 격자 분포 기준 (cols=4 × rows≈6 for mid, top 은 좁아 4 cells):
            switch (zone)
            {
                case PinballZone.TopLane:       return 6;   // 좁은 top 세그먼트
                case PinballZone.BumperCluster: return 24;
                case PinballZone.MidPlay:       return 24;
                case PinballZone.SlingshotZone: return 24;
                default: return 16;
            }
        }

        /// <summary>오버플로 시 흘려보낼 인접 zone.</summary>
        public static PinballZone OverflowTarget(PinballZone zone)
        {
            switch (zone)
            {
                case PinballZone.TopLane:       return PinballZone.BumperCluster;
                case PinballZone.BumperCluster: return PinballZone.MidPlay;
                case PinballZone.MidPlay:       return PinballZone.SlingshotZone;
                case PinballZone.SlingshotZone: return PinballZone.MidPlay;
                default: return PinballZone.MidPlay;
            }
        }
    }
}
