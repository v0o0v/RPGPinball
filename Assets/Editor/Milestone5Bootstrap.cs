#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.EditorTools
{
    /// <summary>
    /// 마일스톤 5: 절차 생성에 필요한 핵심 SO 에셋들을 일괄 생성.
    /// Resources/ 하위에 두어 Resources.LoadAll 로 풀이 자동 채워짐.
    /// 메뉴: RPG Pinball > Bootstrap > Generate Milestone 5 Data
    /// </summary>
    public static class Milestone5Bootstrap
    {
        private const string RES_ROOT = "Assets/Resources";
        private const string MODIFIERS_DIR = RES_ROOT + "/Modifiers";
        private const string MUTATIONS_DIR = RES_ROOT + "/Mutations";
        private const string ARENAS_DIR = RES_ROOT + "/Arenas";
        private const string GIMMICKS_DIR = RES_ROOT + "/Gimmicks";
        private const string SEGMENTS_DIR = RES_ROOT + "/Segments";
        private const string MONSTERS_DIR = RES_ROOT + "/Monsters";

        [MenuItem("RPG Pinball/Bootstrap/Refresh Gimmick Effects (Backfill)")]
        public static void RefreshGimmickEffects()
        {
            int updated = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:GimmickData", new[] { GIMMICKS_DIR }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<GimmickData>(path);
                if (so == null) continue;
                ApplyCategoryDefaults(so);
                EditorUtility.SetDirty(so);
                updated++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Milestone5Bootstrap] {updated}건 기믹 SO 효과 슬롯 보강 완료.");
        }

        [MenuItem("RPG Pinball/Bootstrap/Generate Milestone 5 Data")]
        public static void Run()
        {
            EnsureDir(MODIFIERS_DIR);
            EnsureDir(MUTATIONS_DIR);
            EnsureDir(ARENAS_DIR);
            EnsureDir(GIMMICKS_DIR);
            EnsureDir(SEGMENTS_DIR);
            EnsureDir(MONSTERS_DIR);

            GenerateModifiers();
            GenerateMutations();
            GenerateArenas();
            GenerateGimmicks();
            GenerateSegments();
            GenerateMonsters();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Milestone5Bootstrap] 모든 SO 에셋 생성 완료. Resources/ 하위에서 확인.");
        }

        // ── Modifier 18종 ───────────────────────────────────────
        private static void GenerateModifiers()
        {
            CreateModifier("Modifier_BlitzMatch", ModifierId.BlitzMatch, "쾌속전", 3, m =>
            {
                m.timeLimitDeltaSeconds = -30f;
                m.monsterHpMultiplier = 0.8f;
                m.goldRewardDelta = 0.2f;
            });
            CreateModifier("Modifier_IronFortress", ModifierId.IronFortress, "철벽 요새", 3, m =>
            {
                m.monsterDefMultiplier = 1.5f;
                m.xpRewardDelta = 0.3f;
            });
            CreateModifier("Modifier_ReapersTouch", ModifierId.ReapersTouch, "사신의 손길", 4, m =>
            {
                m.deadzonePenaltyMultiplier = 2f;
                m.eliteSpawnMultiplier = 2f;
                m.runeDropChanceDelta = 0.15f;
            });
            CreateModifier("Modifier_FranticPitch", ModifierId.FranticPitch, "광란의 피치", 3, m =>
            {
                m.ballBaseSpeedMultiplier = 1.5f;
                m.flipperCooldownDelta = 0.3f;
                m.comboBonusDelta = 0.2f;
            });
            CreateModifier("Modifier_ChaosGravity", ModifierId.ChaosGravity, "혼돈의 중력", 5, m =>
            {
                m.gravityChaosIntervalSeconds = 5f;
                m.manaCrystalDelta = 0.5f;
            });
            CreateModifier("Modifier_GoldRush", ModifierId.GoldRush, "황금 열풍", 2, m =>
            {
                m.goldDropMultiplier = 3f;
                m.monsterHpMultiplier = 1.3f;
            });
            CreateModifier("Modifier_ExposedWeakness", ModifierId.ExposedWeakness, "약점 노출", 1, m =>
            {
                m.criticalChanceDelta = 0.2f;
            });
            CreateModifier("Modifier_EternalFrost", ModifierId.EternalFrost, "영겁의 서리", 4, m =>
            {
                m.ballFreezeTriggerSeconds = 2f;
                m.coreShardChanceDelta = 0.1f;
            });
            CreateModifier("Modifier_GamblersNight", ModifierId.GamblersNight, "도박사의 밤", 5, m =>
            {
                m.gimbalRandomBuffChance = 0.5f;
                m.tarotShardDelta = 1f;
            });
            CreateModifier("Modifier_ManaStorm", ModifierId.ManaStorm, "마력 폭풍", 3, m =>
            {
                m.manaChargeMultiplier = 2f;
                m.skillDamageDelta = 0.3f;
                m.skillCostMultiplier = 1.5f;
                m.spRewardDelta = 1;
            });

            // 봄 테마
            CreateModifier("Modifier_PollenAllergy", ModifierId.PollenAllergy, "꽃가루 알레르기", 3, m =>
            {
                m.flipperFailChance = 0.3f;
            }, ActId.Act1_Spring);
            CreateModifier("Modifier_BlossomForest", ModifierId.BlossomForest, "만개의 숲", 2, m =>
            {
                m.vegetationGrowthEnabled = true;
            }, ActId.Act1_Spring);

            // 여름 테마
            CreateModifier("Modifier_TideCycle", ModifierId.TideCycle, "밀물과 썰물", 3, m =>
            {
                m.tideCycleSeconds = 30f;
            }, ActId.Act2_Summer);
            CreateModifier("Modifier_PirateCurse", ModifierId.PirateCurse, "해적의 저주", 2, m =>
            {
                m.goldDropMultiplier = 0.5f;
            }, ActId.Act2_Summer);

            // 가을 테마
            CreateModifier("Modifier_MachineMalfunction", ModifierId.MachineMalfunction, "기계 오작동", 3, m =>
            {
                m.rampGimmickToggleEnabled = true;
            }, ActId.Act3_Autumn);
            CreateModifier("Modifier_GhostsJoke", ModifierId.GhostsJoke, "유령의 농담", 4, m =>
            {
                m.phaseShiftIntervalSeconds = 5f;
            }, ActId.Act3_Autumn);

            // 겨울 테마
            CreateModifier("Modifier_Blizzard", ModifierId.Blizzard, "블리자드", 4, m =>
            {
                m.visionReductionPercent = 0.4f;
            }, ActId.Act4_Winter);
            CreateModifier("Modifier_TimeWarp", ModifierId.TimeWarp, "시간 왜곡", 5, m =>
            {
                m.timeWarpEnabled = true;
            }, ActId.Act4_Winter);
        }

        private static void CreateModifier(string fileName, ModifierId id, string ko, int tier,
            System.Action<StageModifierData> setup, ActId theme = ActId.None)
        {
            var path = $"{MODIFIERS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<StageModifierData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<StageModifierData>();
            so.modifierId = id;
            so.displayNameKo = ko;
            so.tier = tier;
            so.themeOwner = theme;
            so.monsterHpMultiplier = 1f;
            so.monsterDefMultiplier = 1f;
            so.eliteSpawnMultiplier = 1f;
            so.ballBaseSpeedMultiplier = 1f;
            so.goldDropMultiplier = 1f;
            so.manaChargeMultiplier = 1f;
            so.skillCostMultiplier = 1f;
            so.deadzonePenaltyMultiplier = 1f;
            setup?.Invoke(so);
            AssetDatabase.CreateAsset(so, path);
        }

        // ── Mutation 5종 ────────────────────────────────────────
        private static void GenerateMutations()
        {
            CreateMutation("Mutation_ThemeErosion", MutationId.ThemeErosion, "테마 침식", m =>
            {
                m.allowPrologue = false;
                m.crossThemeGimmickCount = 2;
            });
            CreateMutation("Mutation_MirrorWorld", MutationId.MirrorWorld, "거울 세계", m =>
            {
                m.mirrorLayoutHorizontal = true;
            });
            CreateMutation("Mutation_Miniature", MutationId.Miniature, "미니어처", m =>
            {
                m.allowPrologue = false;
                m.allowDevelopment = false;
                m.playfieldScaleMultiplier = 0.6f;
                m.wallElasticityMultiplier = 1.2f;
            });
            CreateMutation("Mutation_TimeRush", MutationId.TimeRush, "타임 러시", m =>
            {
                m.forceTimeLimit = true;
                m.forcedTimeLimitSeconds = 60f;
                m.trMonsterHpMultiplier = 0.5f;
                m.trRewardMultiplier = 3f;
            });
            CreateMutation("Mutation_BossRush", MutationId.BossRush, "보스 러시", m =>
            {
                m.allowPrologue = false;
                m.allowDevelopment = false;
                m.recurringBossHpRatio = 0.5f;
            });
        }

        private static void CreateMutation(string fileName, MutationId id, string ko, System.Action<MutationData> setup)
        {
            var path = $"{MUTATIONS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<MutationData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<MutationData>();
            so.mutationId = id;
            so.displayNameKo = ko;
            so.allowPrologue = true;
            so.allowDevelopment = true;
            so.allowClimax = true;
            so.goldMultiplier = 2f;
            so.rareRuneChanceDelta = 0.15f;
            so.iconKind = "⚠️";
            setup?.Invoke(so);
            AssetDatabase.CreateAsset(so, path);
        }

        // ── Arena 4종 ───────────────────────────────────────────
        private static void GenerateArenas()
        {
            CreateArena("Arena_StormElemental", EliteId.StormElemental, ActId.Act1_Spring, "봄 숲");
            CreateArena("Arena_AbyssalLeviathan", EliteId.AbyssalLeviathan, ActId.Act2_Summer, "심해");
            CreateArena("Arena_GoldenGoblinKing", EliteId.GoldenGoblinKing, ActId.Act3_Autumn, "가을 미로");
            CreateArena("Arena_FrostSentinel", EliteId.FrostSentinel, ActId.Act4_Winter, "겨울 성벽");
        }

        private static void CreateArena(string fileName, EliteId id, ActId act, string desc)
        {
            var path = $"{ARENAS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ArenaLayoutData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<ArenaLayoutData>();
            so.eliteId = id;
            so.themeOwner = act;
            so.descriptionKo = desc;
            AssetDatabase.CreateAsset(so, path);
        }

        // ── Gimmick 대표 18종 (충돌 6쌍 + 시너지 시드) ──────────
        private static void GenerateGimmicks()
        {
            // 공통 베이스 8종
            CreateG("Gimmick_HiddenBumper", GimmickId.HiddenBumper, "히든 범퍼", GimmickCategory.Reward, -10, g =>
            {
                g.impulseN = 20f; g.manaDelta = 15; g.goldDelta = 30; g.xpDelta = 50;
                g.triggerOnceOnly = true;
            });
            CreateG("Gimmick_AccelRail", GimmickId.AccelRail, "가속 레일", GimmickCategory.Environment, 10);
            CreateG("Gimmick_JumpPad", GimmickId.JumpPad, "점프 패드", GimmickCategory.Reward, -12, g =>
            {
                g.impulseN = 30f; g.buffDurationSeconds = 1.5f;
                g.cooldownSeconds = 3f;
            });
            CreateG("Gimmick_Wormhole", GimmickId.Wormhole, "블랙홀 웜홀", GimmickCategory.Environment, 15, g =>
            {
                g.cooldownSeconds = 0.5f;
            });
            CreateG("Gimmick_TimeOrb", GimmickId.TimeOrb, "시간 구슬", GimmickCategory.Reward, -15, g =>
            {
                g.timePenaltySeconds = 5f; // 회복
                g.triggerOnceOnly = true;
            });
            CreateG("Gimmick_ManaShrine", GimmickId.ManaShrine, "마나 제단", GimmickCategory.Reward, -8, g =>
            {
                g.manaDelta = 20;
                g.triggerOnceOnly = true;
            });
            // 충돌 규칙 적용
            CreateG("Gimmick_HeavyBall", GimmickId.HeavyBall, "무거워진 공", GimmickCategory.Debuff, 25, g =>
            {
                g.conflictingIds = new[] { GimmickId.GravityReversal };
            });
            CreateG("Gimmick_InversionMirror", GimmickId.InversionMirror, "반전 거울", GimmickCategory.Debuff, 30, g =>
            {
                g.conflictingIds = new[] { GimmickId.CursedFlipper };
            });
            CreateG("Gimmick_DarkVeil", GimmickId.DarkVeil, "어둠의 장막", GimmickCategory.Debuff, 35, g =>
            {
                g.conflictingIds = new[] { GimmickId.BlindSpot };
            });

            // 봄 — 충돌 + 시너지 + 풍부한 풀
            CreateG("Gimmick_PoisonPool", GimmickId.PoisonPool, "독장판", GimmickCategory.Trial, 30, g =>
            {
                g.synergyIds = new[] { GimmickId.PurifyingFlame };
            }, ActId.Act1_Spring);
            CreateG("Gimmick_PurifyingFlame", GimmickId.PurifyingFlame, "정화의 불꽃", GimmickCategory.Reward, -10, g =>
            {
                g.synergyIds = new[] { GimmickId.PoisonPool };
            }, ActId.Act1_Spring);
            CreateG("Gimmick_PollenSilence", GimmickId.PollenSilence, "플리퍼 침묵", GimmickCategory.Trial, 35, g =>
            {
                g.conflictingIds = new[] { GimmickId.FreezingFloor, GimmickId.InputLagCurse };
                g.triggerKind = GimmickTriggerKind.Periodic;
                g.triggerIntervalSeconds = 10f;
                g.debuffDurationSeconds = 3f;
            }, ActId.Act1_Spring);
            CreateG("Gimmick_FairyBlessing", GimmickId.FairyBlessing, "요정의 축복", GimmickCategory.Buff, -8, null, ActId.Act1_Spring);
            CreateG("Gimmick_StickyWeb", GimmickId.StickyWeb, "끈끈이 거미줄", GimmickCategory.Trial, 25, null, ActId.Act1_Spring);
            CreateG("Gimmick_WindTunnel", GimmickId.WindTunnel, "바람 구멍", GimmickCategory.Environment, 15, null, ActId.Act1_Spring);
            CreateG("Gimmick_SeedOfGrowth", GimmickId.SeedOfGrowth, "성장의 씨앗", GimmickCategory.Reward, -10, null, ActId.Act1_Spring);
            CreateG("Gimmick_SpikePit", GimmickId.SpikePit, "가시 구덩이", GimmickCategory.Trial, 30, null, ActId.Act1_Spring);
            CreateG("Gimmick_AngelWings", GimmickId.AngelWings, "천사의 날개", GimmickCategory.Buff, -10, null, ActId.Act1_Spring);
            CreateG("Gimmick_GhostBumper", GimmickId.GhostBumper, "유령 범퍼", GimmickCategory.Environment, 15, null, ActId.Act1_Spring);

            // 여름
            CreateG("Gimmick_Whirlpool", GimmickId.Whirlpool, "소용돌이", GimmickCategory.Environment, 20, null, ActId.Act2_Summer);
            CreateG("Gimmick_NetTrap", GimmickId.NetTrap, "투망 함정", GimmickCategory.Trial, 30, null, ActId.Act2_Summer);
            CreateG("Gimmick_Drawbridge", GimmickId.Drawbridge, "널판지 도개교", GimmickCategory.Environment, 18, null, ActId.Act2_Summer);
            CreateG("Gimmick_ReverseEscalator", GimmickId.ReverseEscalator, "해류 에스컬레이터", GimmickCategory.Trial, 30, null, ActId.Act2_Summer);
            CreateG("Gimmick_SpringWall", GimmickId.SpringWall, "해파리 벽", GimmickCategory.Environment, 12, null, ActId.Act2_Summer);
            CreateG("Gimmick_IronSkin", GimmickId.IronSkin, "게껍질 버프", GimmickCategory.Buff, -8, null, ActId.Act2_Summer);
            CreateG("Gimmick_MirrorShield", GimmickId.MirrorShield, "진주 반사판", GimmickCategory.Reward, -10, null, ActId.Act2_Summer);
            CreateG("Gimmick_Earthquake", GimmickId.Earthquake, "해일/지진", GimmickCategory.Trial, 35, null, ActId.Act2_Summer);
            CreateG("Gimmick_ManaLeech", GimmickId.ManaLeech, "마나 거머리", GimmickCategory.Debuff, 25, null, ActId.Act2_Summer);
            CreateG("Gimmick_Oasis", GimmickId.Oasis, "오아시스 생명수", GimmickCategory.Reward, -12, null, ActId.Act2_Summer);

            // 가을 — 충돌
            CreateG("Gimmick_GravityReversal", GimmickId.GravityReversal, "중력 역전", GimmickCategory.Trial, 40, g =>
            {
                g.conflictingIds = new[] { GimmickId.IcePatch, GimmickId.HeavyBall };
            }, ActId.Act3_Autumn);
            CreateG("Gimmick_BlindSpot", GimmickId.BlindSpot, "짙은 안개", GimmickCategory.Trial, 35, g =>
            {
                g.conflictingIds = new[] { GimmickId.DarkVeil };
            }, ActId.Act3_Autumn);
            CreateG("Gimmick_CursedFlipper", GimmickId.CursedFlipper, "투명 플리퍼", GimmickCategory.Trial, 40, g =>
            {
                g.conflictingIds = new[] { GimmickId.InversionMirror };
            }, ActId.Act3_Autumn);
            CreateG("Gimmick_GearRotary", GimmickId.GearRotary, "톱니 회전 교차로", GimmickCategory.Environment, 18, null, ActId.Act3_Autumn);
            CreateG("Gimmick_LaserFence", GimmickId.LaserFence, "레이저 펜스", GimmickCategory.Trial, 32, null, ActId.Act3_Autumn);
            CreateG("Gimmick_ManaReactor", GimmickId.ManaReactor, "마력 융합로", GimmickCategory.Reward, -10, null, ActId.Act3_Autumn);
            CreateG("Gimmick_SkillBook", GimmickId.SkillBook, "마도서", GimmickCategory.Buff, -8, null, ActId.Act3_Autumn);
            CreateG("Gimmick_MiniBlackhole", GimmickId.MiniBlackhole, "소형 블랙홀", GimmickCategory.Environment, 20, null, ActId.Act3_Autumn);
            CreateG("Gimmick_Blackmarket", GimmickId.Blackmarket, "암시장 상인", GimmickCategory.Reward, -15, null, ActId.Act3_Autumn);

            // 겨울 — 충돌
            CreateG("Gimmick_IcePatch", GimmickId.IcePatch, "얼음 바닥", GimmickCategory.Environment, 20, g =>
            {
                g.conflictingIds = new[] { GimmickId.GravityReversal };
            }, ActId.Act4_Winter);
            CreateG("Gimmick_FreezingFloor", GimmickId.FreezingFloor, "빙결 바닥", GimmickCategory.Trial, 35, g =>
            {
                g.conflictingIds = new[] { GimmickId.PollenSilence };
            }, ActId.Act4_Winter);
            CreateG("Gimmick_InputLagCurse", GimmickId.InputLagCurse, "동상 래그", GimmickCategory.Trial, 35, g =>
            {
                g.conflictingIds = new[] { GimmickId.PollenSilence };
            }, ActId.Act4_Winter);
            CreateG("Gimmick_ReflectingPrism", GimmickId.ReflectingPrism, "반사 프리즘", GimmickCategory.Environment, 18, null, ActId.Act4_Winter);
            CreateG("Gimmick_CrystalPillar", GimmickId.CrystalPillar, "얼음 수정", GimmickCategory.Reward, -10, null, ActId.Act4_Winter);
            CreateG("Gimmick_GoldenEgg", GimmickId.GoldenEgg, "황금 얼음 조각", GimmickCategory.Reward, -15, null, ActId.Act4_Winter);
            CreateG("Gimmick_Hourglass", GimmickId.Hourglass, "시간의 모래시계", GimmickCategory.Reward, -12, null, ActId.Act4_Winter);
            CreateG("Gimmick_Aurora", GimmickId.Aurora, "오로라 버프", GimmickCategory.Buff, -10, null, ActId.Act4_Winter);
            CreateG("Gimmick_HolyRelic", GimmickId.HolyRelic, "성기사의 쉴드", GimmickCategory.Buff, -8, null, ActId.Act4_Winter);
        }

        private static void CreateG(string fileName, GimmickId id, string ko, GimmickCategory cat, int budgetCost,
            System.Action<GimmickData> setup = null, ActId theme = ActId.None)
        {
            var path = $"{GIMMICKS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<GimmickData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<GimmickData>();
            so.gimmickId = id;
            so.displayNameKo = ko;
            so.category = cat;
            so.budgetCost = budgetCost;
            so.themeOwner = theme;
            so.placement = GimmickPlacement.MidSegment;
            setup?.Invoke(so);
            ApplyCategoryDefaults(so);
            AssetDatabase.CreateAsset(so, path);
        }

        /// <summary>
        /// 카테고리 기반 기본 효과 슬롯을 자동 채움 — setup에서 명시되지 않은 슬롯만 보강.
        /// PlaceholderGimmick(M5 본 구현, 2026-05-14)이 발행할 의미 있는 효과를 보장.
        /// </summary>
        private static void ApplyCategoryDefaults(GimmickData so)
        {
            bool hasAnyEffect = so.impulseN > 0f || so.manaDelta != 0 || so.timePenaltySeconds != 0f
                                || so.absoluteHpDamage != 0 || so.damagePercent != 0f
                                || so.goldDelta != 0 || so.xpDelta != 0;
            if (hasAnyEffect) return; // 명시된 효과 우선

            switch (so.category)
            {
                case GimmickCategory.Reward:
                    so.timePenaltySeconds = 5f;   // 시간 +5초 (회복)
                    so.manaDelta = 10;
                    so.triggerOnceOnly = true;
                    break;
                case GimmickCategory.Buff:
                    so.manaDelta = 15;
                    so.buffDurationSeconds = 5f;
                    so.triggerOnceOnly = true;
                    break;
                case GimmickCategory.Trial:
                    so.timePenaltySeconds = -8f;  // 시간 -8초 (페널티)
                    if (so.respectsResistance == false) so.respectsResistance = true;
                    so.cooldownSeconds = 2f;
                    break;
                case GimmickCategory.Debuff:
                    so.timePenaltySeconds = -5f;
                    so.debuffDurationSeconds = 4f;
                    so.cooldownSeconds = 3f;
                    break;
                case GimmickCategory.Environment:
                    so.impulseN = 8f;             // 약한 반발/유도
                    so.cooldownSeconds = 1.5f;
                    break;
            }
        }

        // ── Segment 정식 풀 ────────────────────────────────────
        // 설계 문서 `Procedural_Stage_Gen.md` §3 기준:
        //   상단 풀: 테마당 8~12종 → 테마당 3종 + 공통 2종 = 14종 (Top)
        //   중단 풀: 테마당 15~20종 → 테마당 15종 + 공통 4종 = 64종 (Middle)
        //   하단 풀: 공통 고정 1종 (Bottom)
        // 총 79종 + 기존 1 (Bottom_Common 중복 회피)
        private static void GenerateSegments()
        {
            // 하단: 공통 1종 (플리퍼 영역, 닫힌 바닥)
            CreateSeg("Segment_Bottom_Common", SegmentSlot.Bottom, ActId.None, 6f, 5f, 9f);

            // 상단: 보스/관문 슬롯이 비교적 단순. 짧은 높이.
            // 공통 2종
            CreateSeg("Segment_Top_Common_A", SegmentSlot.Top, ActId.None, 4f, 3f, 7f,
                anchors: new[] { new Vector2(-2, 0), new Vector2(2, 0) });
            CreateSeg("Segment_Top_Common_B", SegmentSlot.Top, ActId.None, 4f, 3f, 7f,
                anchors: new[] { new Vector2(0, 0) });
            // 테마별 상단 3종 × 4테마 = 12종
            var topPatterns = new (string label, Vector2[] anchors)[]
            {
                ("Tri",   new[] { new Vector2(-2.5f, -0.5f), new Vector2(0, 1f), new Vector2(2.5f, -0.5f) }),
                ("Wide",  new[] { new Vector2(-3, 0), new Vector2(0, 0), new Vector2(3, 0) }),
                ("Pair",  new[] { new Vector2(-1.5f, 0.5f), new Vector2(1.5f, -0.5f) }),
            };
            for (int act = 1; act <= 4; act++)
            {
                var actId = (ActId)act;
                foreach (var p in topPatterns)
                    CreateSeg($"Segment_Top_{actId}_{p.label}", SegmentSlot.Top, actId, 4f, 3f, 7f, anchors: p.anchors);
            }

            // 중단: 15종 패턴 × 4테마 = 60종 + 공통 fallback 4종
            // 패턴은 anchors 형태와 높이 프로파일을 함께 정의.
            // height: 권장 높이, heightMin~Max: 허용 범위. mid 슬롯 합계가 target(OrthoSize×6)을 맞추도록 빌더에서 분배.
            var midPatterns = new (string label, Vector2[] anchors, float height, float min, float max)[]
            {
                // 작은 세그먼트 (height 6~10, 시그마 패턴)
                ("A_HPair",   new[] { new Vector2(-2, 0), new Vector2(2, 0) },                                                      8f, 6f, 12f),
                ("B_Diag",    new[] { new Vector2(-1.5f, 1), new Vector2(1.5f, -1), new Vector2(0, 0) },                            8f, 6f, 12f),
                ("E_Center",  new[] { new Vector2(0, 0) },                                                                          6f, 4f, 10f),
                ("Q_Tri",     new[] { new Vector2(-2.5f, 1), new Vector2(2.5f, 1), new Vector2(0, -1) },                            8f, 6f, 12f),
                ("R_InvTri",  new[] { new Vector2(-2.5f, -1), new Vector2(2.5f, -1), new Vector2(0, 1) },                           8f, 6f, 12f),
                // 표준 세그먼트 (height 10~16)
                ("C_VStack4", new[] { new Vector2(0, 2f), new Vector2(0, 0.7f), new Vector2(0, -0.7f), new Vector2(0, -2f) },       12f, 10f, 16f),
                ("D_V",       new[] { new Vector2(-2.5f, -1.5f), new Vector2(0, 1.5f), new Vector2(2.5f, -1.5f) },                  10f, 8f, 14f),
                ("F_Mirror4", new[] { new Vector2(-2.5f, 1.5f), new Vector2(2.5f, 1.5f), new Vector2(-2.5f, -1.5f), new Vector2(2.5f, -1.5f) }, 12f, 10f, 16f),
                ("H_ZigZag",  new[] { new Vector2(-2.5f, 2f), new Vector2(2.5f, 0.7f), new Vector2(-2.5f, -0.7f), new Vector2(2.5f, -2f) },     14f, 10f, 18f),
                ("I_Cross",   new[] { new Vector2(0, 2f), new Vector2(-2.5f, 0), new Vector2(2.5f, 0), new Vector2(0, -2f), new Vector2(0, 0) }, 12f, 10f, 16f),
                ("J_XShape",  new[] { new Vector2(-2.5f, 2f), new Vector2(2.5f, 2f), new Vector2(0, 0), new Vector2(-2.5f, -2f), new Vector2(2.5f, -2f) }, 12f, 10f, 16f),
                // 큰 세그먼트 (height 16~22)
                ("K_DiagLR",  new[] { new Vector2(-3, 2.5f), new Vector2(-1, 1f), new Vector2(1, -1f), new Vector2(3, -2.5f) },     18f, 14f, 22f),
                ("L_DiagRL",  new[] { new Vector2(3, 2.5f), new Vector2(1, 1f), new Vector2(-1, -1f), new Vector2(-3, -2.5f) },     18f, 14f, 22f),
                ("M_LSide",   new[] { new Vector2(-3, 2f), new Vector2(-3, 0), new Vector2(-3, -2f), new Vector2(2, 0) },           16f, 12f, 20f),
                ("N_RSide",   new[] { new Vector2(3, 2f), new Vector2(3, 0), new Vector2(3, -2f), new Vector2(-2, 0) },             16f, 12f, 20f),
            };

            for (int act = 1; act <= 4; act++)
            {
                var actId = (ActId)act;
                foreach (var p in midPatterns)
                    CreateSeg($"Segment_Mid_{actId}_{p.label}", SegmentSlot.Middle, actId, p.height, p.min, p.max, anchors: p.anchors);
            }
            // 공통 중단 fallback 4종 (테마 풀 부족 / 모디파이어 충돌 회피)
            CreateSeg("Segment_Mid_Common_A", SegmentSlot.Middle, ActId.None, 8f, 6f, 12f, anchors: midPatterns[0].anchors);
            CreateSeg("Segment_Mid_Common_B", SegmentSlot.Middle, ActId.None, 10f, 8f, 14f, anchors: midPatterns[6].anchors);
            CreateSeg("Segment_Mid_Common_C", SegmentSlot.Middle, ActId.None, 12f, 10f, 16f, anchors: midPatterns[9].anchors);
            CreateSeg("Segment_Mid_Common_D", SegmentSlot.Middle, ActId.None, 16f, 12f, 20f, anchors: midPatterns[13].anchors);
        }

        private static void CreateSeg(string fileName, SegmentSlot slot, ActId theme, float height, float min, float max, Vector2[] anchors = null)
        {
            var path = $"{SEGMENTS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<SegmentData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<SegmentData>();
            so.segmentId = fileName;
            so.slot = slot;
            so.theme = theme;
            so.width = Constants.SegPlayfieldWidth; // 런타임은 Constants 만 참조하지만 SO 일관성 유지
            so.height = height;
            so.heightMin = min;
            so.heightMax = max;
            so.gimmickSlotAnchors = anchors ?? new[] { Vector2.zero };
            AssetDatabase.CreateAsset(so, path);
        }

        // ── Monster 최소 풀 (테마당 Small 1 + Medium 1) ─────────
        private static void GenerateMonsters()
        {
            for (int act = 1; act <= 4; act++)
            {
                var actId = (ActId)act;
                CreateMon($"Monster_{actId}_Small", actId, MonsterSizeCategory.Small, 100, 5, 12);
                CreateMon($"Monster_{actId}_Medium", actId, MonsterSizeCategory.Medium, 200, 8, 24);
            }
        }

        private static void CreateMon(string fileName, ActId theme, MonsterSizeCategory cat, int hp, int atk, int xp)
        {
            var path = $"{MONSTERS_DIR}/{fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<MonsterData>(path) != null) return;
            var so = ScriptableObject.CreateInstance<MonsterData>();
            so.id = (int)theme * 100 + (int)cat;
            so.displayName = fileName;
            so.level = 1;
            so.maxHp = hp;
            so.xpReward = xp;
            so.themeOwner = theme;
            so.sizeCategory = cat;
            AssetDatabase.CreateAsset(so, path);
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
