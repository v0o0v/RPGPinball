using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Physics;

namespace RPGPinball.Enemy.BossAI
{
    /// <summary>
    /// 모든 보스의 공통 부모. MonsterBase 상속하여 페이즈/약점/분노 모드 추가.
    /// Boss_Patterns.md의 행동 사이클(Idle→Telegraph→Execute→Recovery) 프레임워크.
    /// 자식 클래스는 BuildPatterns()에서 IBossPattern 인스턴스를 등록.
    /// </summary>
    public class BossBase : MonsterBase
    {
        [Header("약점 콜라이더 프리팹 (선택). 없으면 코드로 생성")]
        [SerializeField] protected GameObject weakPointHitboxPrefab;

        protected BossStateMachine stateMachine;
        protected BossPhase currentPhase = BossPhase.P1;
        protected bool isEnraged;
        protected readonly List<WeakPointHitbox> weakPointInstances = new();

        public BossData BossData => Data as BossData;
        public BossPhase CurrentPhase => currentPhase;
        public bool IsEnraged => isEnraged;
        public BossStateMachine StateMachine => stateMachine;

        protected override void Awake()
        {
            base.Awake();
            stateMachine = gameObject.AddComponent<BossStateMachine>();
        }

        protected virtual void Start()
        {
            var bd = BossData;
            // 약점 콜라이더 생성
            if (bd != null && bd.weakPoints != null)
            {
                for (int i = 0; i < bd.weakPoints.Length; i++)
                {
                    SpawnWeakPoint(bd.weakPoints[i]);
                }
            }
            // 패턴 빌드 → 상태머신 시작
            var patterns = BuildPatterns();
            stateMachine.Configure(this, bd, patterns);
            stateMachine.Begin();
        }

        protected virtual void OnEnable()
        {
            var bd = BossData;
            BossFightContext.Enter(this, bd != null ? bd.bossId : BossId.None);
            if (CameraController.Instance != null)
                CameraController.Instance.SetBossFight(true);
        }

        protected virtual void OnDisable()
        {
            // 의도된 비활성 (Die 후 호출) 시 컨텍스트 정리
            if (BossFightContext.CurrentBoss == this)
            {
                BossFightContext.Exit();
                if (CameraController.Instance != null)
                    CameraController.Instance.SetBossFight(false);
            }
        }

        /// <summary>자식 보스가 오버라이드하여 패턴 인스턴스 배열을 반환.</summary>
        protected virtual IBossPattern[] BuildPatterns()
        {
            return System.Array.Empty<IBossPattern>();
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead) return;

            // 페이즈 전환 검사
            float hpRatio = CurrentHpRatio;
            var bd = BossData;
            if (bd != null)
            {
                if (bd.hasPhase3 && currentPhase != BossPhase.P3 && hpRatio <= bd.phase3HpRatio)
                {
                    EnterPhase(BossPhase.P3);
                }
                else if (bd.hasPhase2 && currentPhase == BossPhase.P1 && hpRatio <= bd.phase2HpRatio)
                {
                    EnterPhase(BossPhase.P2);
                }

                // 분노 모드
                if (!isEnraged && hpRatio <= bd.enragedHpRatio)
                {
                    EnterEnraged();
                }
            }
        }

        protected virtual void EnterPhase(BossPhase phase)
        {
            currentPhase = phase;
            EventBus.Publish(new OnBossPhaseChanged { Boss = gameObject, Phase = phase });
            // 약점 콜라이더 활성/비활성 갱신
            RefreshWeakPointActivation();
        }

        protected virtual void EnterEnraged()
        {
            isEnraged = true;
            EventBus.Publish(new OnBossEnraged { Boss = gameObject });
        }

        protected void RefreshWeakPointActivation()
        {
            for (int i = 0; i < weakPointInstances.Count; i++)
            {
                var wp = weakPointInstances[i];
                if (wp == null) continue;
                bool active = IsWeakPointActive(wp.Spec);
                if (wp.gameObject.activeSelf != active) wp.gameObject.SetActive(active);
            }
        }

        public bool IsWeakPointActive(WeakPointSpec spec)
        {
            // P1 ≤ P2 ≤ P3 순서이므로 현재 페이즈 >= activeFromPhase
            return (int)currentPhase >= (int)spec.activeFromPhase;
        }

        protected virtual void SpawnWeakPoint(WeakPointSpec spec)
        {
            var go = new GameObject($"WeakPoint_{spec.label}");
            go.transform.SetParent(transform, false);
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = Mathf.Max(0.05f, spec.radius);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var hit = go.AddComponent<WeakPointHitbox>();
            hit.Initialize(this, spec);
            weakPointInstances.Add(hit);
            // 시작 시 활성/비활성 결정
            go.SetActive(IsWeakPointActive(spec));
        }

        /// <summary>현재 페이즈의 DEF (BossData.phaseDefenseRatios 기반).</summary>
        public virtual int GetEffectiveDefense()
        {
            var bd = BossData;
            if (bd == null) return Data != null ? Data.defense : 0;
            int idx = (int)currentPhase;
            if (bd.phaseDefenseRatios == null || bd.phaseDefenseRatios.Length == 0)
                return bd.defense;
            if (idx >= bd.phaseDefenseRatios.Length) idx = bd.phaseDefenseRatios.Length - 1;
            return Mathf.RoundToInt(bd.phaseDefenseRatios[idx] * 100f);
        }

        protected override void Die()
        {
            if (IsDead) return;
            var bd = BossData;

            int sp = bd != null ? bd.rewardTable.spReward : 0;
            int gold = bd != null ? bd.rewardTable.bonusGold : 0;
            int xp = bd != null ? bd.rewardTable.bonusXp : 0;
            int soul = bd != null ? bd.rewardTable.bossSoul : 0;
            int mana = bd != null ? bd.rewardTable.manaCrystal : 0;

            EventBus.Publish(new OnBossDefeated
            {
                Boss = gameObject,
                BossId = bd != null ? bd.bossId : BossId.None,
                XpReward = xp,
                GoldReward = gold,
                BossSoul = soul,
                ManaCrystal = mana,
                SpReward = sp
            });

            // SP 보상
            if (sp > 0 && LevelSystem.Instance != null) LevelSystem.Instance.AwardBossSP();

            // 상태머신 정지
            if (stateMachine != null) stateMachine.Stop();

            // base.Die()가 IsDead=true, OnMonsterKilled 발행, XP 지급, SetActive(false)까지 처리.
            // 보스전 컨텍스트는 OnDisable이 자동 정리.
            base.Die();
        }
    }
}
