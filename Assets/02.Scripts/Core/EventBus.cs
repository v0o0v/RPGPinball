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

    public struct OnBallDead
    {
        public int BallIndex;
    }

    public struct OnBallRespawned
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
}
