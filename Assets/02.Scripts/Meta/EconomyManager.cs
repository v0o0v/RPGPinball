using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Security;

namespace RPGPinball.Meta
{
    /// <summary>
    /// 10종 통화 단일 출처. 모든 차감/지급은 본 매니저를 거치며 OnCurrencyChanged 발행.
    /// 마일스톤 4의 OnBossDefeated/OnEliteDefeated 를 자동 구독해 보상 지급.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [SerializeField] private EconomyConfig config;
        [SerializeField] private PlayerData playerData;

        private readonly Dictionary<CurrencyId, SafeLong> balances = new();
        private bool suppressRewards; // 보스 연습 모드에서 보상 지급 차단

        public EconomyConfig Config => config;
        public PlayerData Player => playerData;

        public bool IsSuppressed => suppressRewards;
        public void Suppress(bool on) { suppressRewards = on; }

        private bool subscribed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying)
            {
                if (transform.parent != null) transform.SetParent(null, true);
                DontDestroyOnLoad(gameObject);
            }
            ReloadFromPlayerData();
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            if (subscribed) return;
            EventBus.Subscribe<OnBossDefeated>(HandleBossDefeated);
            EventBus.Subscribe<OnEliteDefeated>(HandleEliteDefeated);
            EventBus.Subscribe<OnStageCleared>(HandleStageCleared);
            EventBus.Subscribe<OnNodeReward>(HandleNodeReward);
            subscribed = true;
        }

        private void UnsubscribeAll()
        {
            if (!subscribed) return;
            EventBus.Unsubscribe<OnBossDefeated>(HandleBossDefeated);
            EventBus.Unsubscribe<OnEliteDefeated>(HandleEliteDefeated);
            EventBus.Unsubscribe<OnStageCleared>(HandleStageCleared);
            EventBus.Unsubscribe<OnNodeReward>(HandleNodeReward);
            subscribed = false;
        }

        /// <summary>테스트 / 외부 주입 후 잔액 캐시 재초기화.</summary>
        public void ReloadFromPlayerData()
        {
            balances.Clear();
            if (playerData != null)
            {
                balances[CurrencyId.Gold] = SafeLong.Create(playerData.gold);
                balances[CurrencyId.ManaCrystal] = SafeLong.Create(playerData.manaCrystal);
                balances[CurrencyId.BossSoul] = SafeLong.Create(playerData.bossSoul);
                balances[CurrencyId.RespecScroll] = SafeLong.Create(playerData.respecScrollCount);
            }
        }

        /// <summary>테스트 / 외부 코드용 SO 주입. EditMode 에서 Awake 가 호출되지 않을 때도 Singleton + Subscribe 처리.</summary>
        public void Initialize(PlayerData player, EconomyConfig cfg)
        {
            playerData = player;
            config = cfg;
            if (Instance == null) Instance = this;
            ReloadFromPlayerData();
            SubscribeAll();
        }

        private void OnEnable() { SubscribeAll(); }

        private void OnDisable()
        {
            UnsubscribeAll();
            if (Instance == this) Instance = null;
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
            if (Instance == this) Instance = null;
        }

        /// <summary>테스트 / 씬 재로드용 — Instance 강제 null 처리.</summary>
        public static void ResetInstance() { Instance = null; }

        // ── 잔액 조작 ─────────────────────────────────────────

        public long GetBalance(CurrencyId id)
        {
            if (id == CurrencyId.None) return 0;
            return balances.TryGetValue(id, out var v) ? v.Value : 0;
        }

        public bool Has(CurrencyId id, long amount) => GetBalance(id) >= amount;

        public void Add(CurrencyId id, long amount, string reason = null)
        {
            if (id == CurrencyId.None || amount <= 0) return;
            long cur = GetBalance(id);
            long newBal = cur + amount;
            balances[id] = SafeLong.Create(newBal);
            SyncToPlayerData(id, newBal);
            EventBus.Publish(new OnCurrencyChanged
            {
                CurrencyId = id,
                Delta = amount,
                NewBalance = newBal,
                Reason = reason ?? "Add"
            });
        }

        public bool Spend(CurrencyId id, long amount, string reason = null)
        {
            if (id == CurrencyId.None || amount <= 0) return false;
            long cur = GetBalance(id);
            if (cur < amount) return false;
            long newBal = cur - amount;
            balances[id] = SafeLong.Create(newBal);
            SyncToPlayerData(id, newBal);
            EventBus.Publish(new OnCurrencyChanged
            {
                CurrencyId = id,
                Delta = -amount,
                NewBalance = newBal,
                Reason = reason ?? "Spend"
            });
            return true;
        }

        private void SyncToPlayerData(CurrencyId id, long newBal)
        {
            if (playerData == null) return;
            switch (id)
            {
                case CurrencyId.Gold: playerData.gold = (int)Mathf.Clamp(newBal, 0, int.MaxValue); break;
                case CurrencyId.ManaCrystal: playerData.manaCrystal = (int)Mathf.Clamp(newBal, 0, int.MaxValue); break;
                case CurrencyId.BossSoul: playerData.bossSoul = (int)Mathf.Clamp(newBal, 0, int.MaxValue); break;
                case CurrencyId.RespecScroll: playerData.respecScrollCount = (int)Mathf.Clamp(newBal, 0, int.MaxValue); break;
            }
        }

        // ── 이벤트 구독 ───────────────────────────────────────

        private void HandleBossDefeated(OnBossDefeated e)
        {
            if (suppressRewards) return;

            // 패키지로 들어온 페이로드(이미 BossData에서 산정된 값)를 우선 사용.
            if (e.GoldReward > 0) Add(CurrencyId.Gold, e.GoldReward, "Boss");
            if (e.ManaCrystal > 0) Add(CurrencyId.ManaCrystal, e.ManaCrystal, "Boss");
            if (e.BossSoul > 0) Add(CurrencyId.BossSoul, e.BossSoul, "Boss");

            // 페이로드가 비어 있으면 EconomyConfig 공식 사용 (안전망)
            if (e.GoldReward == 0 && config != null)
            {
                bool isFinal = IsFinalBoss(e.BossId);
                ActId act = GetActFromBossId(e.BossId);
                int gold = config.CalculateBossGold(act, isFinal);
                Add(CurrencyId.Gold, gold, "BossFallback");
                Add(CurrencyId.BossSoul, isFinal ? config.bossSoulFinal : config.bossSoulNormal, "BossFallback");
            }
        }

        private void HandleEliteDefeated(OnEliteDefeated e)
        {
            if (suppressRewards) return;
            if (config == null) return;

            int gold = UnityEngine.Random.Range(config.eliteGoldMin, config.eliteGoldMax + 1);
            Add(CurrencyId.Gold, gold, "Elite");
            // 고유 코어 조각 3개 (CoreFragment 통합 카운트로 처리)
            Add(CurrencyId.CoreFragment, config.eliteCoreFragmentReward, "Elite");

            if (e.IsBountyActive)
            {
                // 현상금 의뢰 활성 시: 전설 룬 1 + 보스 영혼 5 (지급은 별도 시스템 — 본 매니저는 보스 영혼만)
                Add(CurrencyId.BossSoul, 5, "EliteBounty");
            }
        }

        private void HandleStageCleared(OnStageCleared e)
        {
            if (suppressRewards) return;
            if (config == null) return;

            int gold = config.CalculateStageGold(e.StageIndex);
            int mana = config.CalculateStageManaCrystal(e.StageIndex);
            float gradeBonus = config.GetGradeBonus(e.Grade);
            Add(CurrencyId.Gold, Mathf.RoundToInt(gold * gradeBonus), $"Stage{e.StageIndex}");
            Add(CurrencyId.ManaCrystal, Mathf.RoundToInt(mana * gradeBonus), $"Stage{e.StageIndex}");
        }

        private void HandleNodeReward(OnNodeReward e)
        {
            if (suppressRewards) return;
            switch (e.RewardKind)
            {
                case EventNodeRewardKind.Gold: Add(CurrencyId.Gold, e.Amount, "NodeReward"); break;
                case EventNodeRewardKind.ManaCrystal: Add(CurrencyId.ManaCrystal, e.Amount, "NodeReward"); break;
                case EventNodeRewardKind.BossSoul: Add(CurrencyId.BossSoul, e.Amount, "NodeReward"); break;
                case EventNodeRewardKind.SkillPoint: Add(CurrencyId.SkillPoint, e.Amount, "NodeReward"); break;
                case EventNodeRewardKind.BlueprintFragment: Add(CurrencyId.BlueprintFragment, e.Amount, "NodeReward"); break;
                case EventNodeRewardKind.SpecialOre: /* Act 기반 — 별도 분기 필요 */ break;
                // Rune/Tarot 는 EnchanterManager/AstrologerManager 가 별도 처리
                default: break;
            }
        }

        private static bool IsFinalBoss(BossId id)
        {
            switch (id)
            {
                case BossId.Act1_WorldTreeSpirit:
                case BossId.Act2_Kraken:
                case BossId.Act3_ClockworkDragon:
                case BossId.Act4_WinterQueen:
                    return true;
                default:
                    return false;
            }
        }

        private static ActId GetActFromBossId(BossId id)
        {
            int code = (int)id;
            if (code >= 110 && code <= 199) return ActId.Act1_Spring;
            if (code >= 210 && code <= 299) return ActId.Act2_Summer;
            if (code >= 310 && code <= 399) return ActId.Act3_Autumn;
            if (code >= 410 && code <= 499) return ActId.Act4_Winter;
            return ActId.None;
        }
    }
}
