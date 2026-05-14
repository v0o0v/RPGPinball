using System;
using System.Collections.Generic;

namespace RPGPinball.Core
{
    /// <summary>
    /// 글로벌 이벤트 버스. 컴포넌트 직접 참조 없이 이벤트를 주고받기 위한 옵저버 패턴 구현체.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (handlers.TryGetValue(type, out var existing))
                handlers[type] = Delegate.Combine(existing, handler);
            else
                handlers[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (handlers.TryGetValue(type, out var existing))
            {
                var updated = Delegate.Remove(existing, handler);
                if (updated == null)
                    handlers.Remove(type);
                else
                    handlers[type] = updated;
            }
        }

        public static void Publish<T>(T evt)
        {
            if (handlers.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler)?.Invoke(evt);
        }

        public static void Clear()
        {
            handlers.Clear();
        }
    }

    // ── 이벤트 데이터 구조체 ──────────────────────────────────

    public struct OnBallHit
    {
        public float Speed;
        public string TargetTag;
    }

    public struct OnComboChange
    {
        public int Combo;
    }

    public struct OnManaChange
    {
        public float Current;
        public float Max;
    }

    public struct OnTimePenalty
    {
        public float Delta;
    }

    /// <summary>
    /// 공이 강제 리셋되어 일시 비활성화됨. 보스 강제 reset 패턴(빙결 거인 등)에서 발행.
    /// 데드존 제거(2026-05-13) 이후 자연 낙사는 없으며, 이 이벤트는 명시적 보스 메커닉 전용.
    /// </summary>
    public struct OnBallReset
    {
        public int BallIndex;
    }

    /// <summary>리셋 후 공이 재배치되어 다시 활성화됨.</summary>
    public struct OnBallSpawned
    {
        public int BallIndex;
    }

    public struct OnFlipperSpawned
    {
        public UnityEngine.Vector2 Position;
    }

    public struct OnFlipperBlocked
    {
        public float CooldownReduction;
    }

    public struct OnGameStateChanged
    {
        public GameState Previous;
        public GameState Current;
    }

    public enum GameState
    {
        Playing,
        Paused,
        Result
    }

    // ── 마일스톤 2 이벤트 ─────────────────────────────────────

    public struct OnDamageDealt
    {
        public UnityEngine.GameObject Target;
        public float Damage;
        public bool IsCritical;
        public bool IsMagic;
    }

    public struct OnMonsterKilled
    {
        public UnityEngine.GameObject Monster;
        public int XpReward;
        public int GoldReward;
        public bool IsBoss;
    }

    public struct OnSkillCast
    {
        public int SkillId;
        public UnityEngine.Vector2 Position;
    }

    public struct OnTimerChanged
    {
        public float Remaining;
        public float Total;
    }

    public struct OnProjectilePenalty
    {
        public float Delta;
    }

    // ── 마일스톤 3 이벤트 ─────────────────────────────────────

    public struct OnLevelUp
    {
        public int PreviousLevel;
        public int NewLevel;
    }

    public struct OnXPGained
    {
        public int Amount;
        public int CurrentXP;
        public int RequiredXP;
    }

    public struct OnSkillPointGained
    {
        public int Delta;
        public int TotalSP;
        public string Reason; // "LevelUp" / "BossKill" / "ActClear"
    }

    public struct OnSkillInvested
    {
        public int SkillId;
        public int NewLevel;
    }

    public struct OnSkillReset
    {
        public int RefundedSP;
    }

    public struct OnBallCountChanged
    {
        public int Count;
    }

    // ── 마일스톤 4 이벤트 (Boss/Elite) ─────────────────────────

    public struct OnBossSpawned
    {
        public UnityEngine.GameObject Boss;
        public Data.BossId BossId;
    }

    public struct OnBossPhaseChanged
    {
        public UnityEngine.GameObject Boss;
        public Data.BossPhase Phase;
    }

    public struct OnBossEnraged
    {
        public UnityEngine.GameObject Boss;
    }

    public struct OnBossDefeated
    {
        public UnityEngine.GameObject Boss;
        public Data.BossId BossId;
        public int XpReward;
        public int GoldReward;
        public int BossSoul;
        public int ManaCrystal;
        public int SpReward;
    }

    public struct OnEliteFlee
    {
        public UnityEngine.GameObject Elite;
        public Data.EliteId EliteId;
        public string Reason; // "FirstHitTimeout" / "FleeTimer"
    }

    public struct OnLeviathanSubmerge
    {
        public UnityEngine.GameObject Elite;
        public bool Submerging; // true: 잠수 시작, false: 부상
    }

    /// <summary>
    /// 플리퍼 소환 차단. area=null이면 전 영역.
    /// 꽃가루 침묵(전체), 무장 게 집게 강타(영역), 시계탑 시간 정지 등에서 사용.
    /// </summary>
    public struct OnFlipperSpawnBlocked
    {
        public float Duration;
        public UnityEngine.Rect? Area;
    }

    /// <summary>
    /// 공 속도 강제 조작 요청. 시계탑 가속/감속, 빙결 동결 등에서 사용.
    /// </summary>
    public struct OnTimeScaleRequest
    {
        public float BallSpeedMultiplier;
        public float Duration;
    }

    // ── 마일스톤 5 이벤트 (Stage / Generation) ─────────────────

    public struct OnStageGenerated
    {
        public Data.ActId ActId;
        public int StageIndex;
        public ulong Seed;
        public Data.NodeKind NodeKind;
        public Data.MutationId MutationId;
    }

    public struct OnStageStart
    {
        public Data.ActId ActId;
        public int StageIndex;
    }

    public struct OnStageClear
    {
        public Data.ActId ActId;
        public int StageIndex;
        public string Grade; // "S"/"A"/"B"/"C" (M8 GradeSystem 도입 시 enum화)
        public float ClearTimeSeconds;
    }

    public struct OnNodeEntered
    {
        public Data.NodeKind NodeKind;
        public int StageIndex;
    }

    public struct OnModifierApplied
    {
        public Data.ModifierId[] ModifierIds;
    }

    public struct OnMutationTriggered
    {
        public Data.MutationId MutationId;
    }

    /// <summary>휴식 노드 등 상한 외 시간 누적용. StageTimer 가 분기 구독.</summary>
    public struct OnTimeBonusAdded
    {
        public float Seconds;
        public string Source; // "RestNode" / "Gimmick" 등
    }

    // ── 마일스톤 6 이벤트 (Village / Meta) ─────────────────────

    /// <summary>범용 통화 변경. EconomyManager 가 모든 차감/지급 시 발행.</summary>
    public struct OnCurrencyChanged
    {
        public Data.CurrencyId CurrencyId;
        public long Delta;
        public long NewBalance;
        public string Reason;
    }

    public struct OnForgeBallChanged
    {
        public Data.BallMaterialId MaterialId;
    }

    public struct OnFlipperUpgraded
    {
        public int NewLevel;
    }

    public struct OnFlipperVariantSelected
    {
        public Data.FlipperVariantId VariantId;
    }

    public struct OnRuneEquipped
    {
        public Data.RuneId RuneId;
        public Data.RuneGrade Grade;
        public string EquippedOnSkillId;
    }

    public struct OnRuneUnequipped
    {
        public Data.RuneId RuneId;
        public Data.RuneGrade Grade;
        public string EquippedOnSkillId;
    }

    public struct OnRuneFused
    {
        public Data.RuneId RuneId;
        public Data.RuneGrade FromGrade;
        public Data.RuneGrade ToGrade;
    }

    public struct OnTarotPulled
    {
        public Data.TarotCardId[] CardIds;
        public Data.TarotGrade[] Grades;
    }

    public struct OnTarotEquipped
    {
        public Data.TarotCardId CardId;
        public int SlotIndex;
    }

    public struct OnTarotUnequipped
    {
        public Data.TarotCardId CardId;
        public int SlotIndex;
    }

    public struct OnTarotPermanentUpgraded
    {
        public Data.TarotCardId CardId;
    }

    public struct OnGimmickResistanceLevelUp
    {
        public Data.GimmickId GimmickId;
        public int NewResistLevel;
    }

    public struct OnDailyQuestRolled
    {
        public string[] QuestIds;
    }

    public struct OnQuestProgress
    {
        public string QuestId;
        public int CurrentProgress;
        public int Target;
    }

    public struct OnQuestCompleted
    {
        public string QuestId;
        public Data.QuestKind Kind;
    }

    public struct OnBountyAccepted
    {
        public string BountyId;
        public Data.EliteId EliteId;
    }

    public struct OnConsumableCrafted
    {
        public Data.ConsumableId ConsumableId;
    }

    public struct OnConsumableUsed
    {
        public Data.ConsumableId ConsumableId;
    }

    public struct OnBalloonUpgraded
    {
        public int NewLevel;
        public Data.BalloonUpgradeId UpgradeId;
    }

    public struct OnSkillDeckEquipped
    {
        public int SlotIndex;
        public int SkillId;
    }

    /// <summary>몬스터/보스가 회복했음을 알림. M6 도감/UI 카운트용.</summary>
    public struct OnMonsterHealed
    {
        public UnityEngine.GameObject Monster;
        public int Amount;
        public Data.HealSource Source;
    }

    /// <summary>이벤트 노드(보물방/제단/도박/여행자)에서 발행한 보상.</summary>
    public struct OnNodeReward
    {
        public Data.NodeKind NodeKind;
        public Data.EventNodeRewardKind RewardKind;
        public int Amount;
    }

    /// <summary>엘리트 처치 보상 트리거. EconomyManager 가 구독해 고유 코어 조각 등 자동 지급.</summary>
    public struct OnEliteDefeated
    {
        public UnityEngine.GameObject Elite;
        public Data.EliteId EliteId;
        public bool IsBountyActive;
    }

    /// <summary>스테이지 클리어. M5의 OnStageClear 를 보강하는 보상 계산 페이로드. (기존 OnStageClear 는 그대로 유지)</summary>
    public struct OnStageCleared
    {
        public Data.ActId ActId;
        public int StageIndex;
        public string Grade;
    }

    public struct OnFlipperSummoned
    {
        public UnityEngine.Vector2 Position;
    }

    public struct OnBallForceReset
    {
        public Data.GimmickId? Cause;
    }
}
