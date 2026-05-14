using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;
using RPGPinball.Stage.Gimmicks;

namespace RPGPinball.Village
{
    /// <summary>
    /// 기믹별 조우/사망 카운트와 저항력 단계(0~5) 관리.
    /// 임계치: 1/3/7/15/30회 → Lv.1~5. 단계 전이 시 OnGimmickResistanceLevelUp 발행.
    /// </summary>
    [System.Serializable]
    public class GimmickEncounterRecord
    {
        public GimmickId id;
        public int encounterCount;
        public int deathCount;
        public int resistLevel;
    }

    public class CollectionManager : MonoBehaviour
    {
        public static CollectionManager Instance { get; private set; }

        [SerializeField] private List<GimmickEncounterRecord> records = new();
        private readonly Dictionary<GimmickId, GimmickEncounterRecord> cache = new();
        private bool milestone10Awarded;
        private bool milestone40Awarded;
        private bool milestone80Awarded;

        public IReadOnlyList<GimmickEncounterRecord> Records => records;

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
            foreach (var r in records) cache[r.id] = r;
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            if (subscribed) return;
            EventBus.Subscribe<OnGimmickActivated>(HandleGimmickActivated);
            EventBus.Subscribe<OnBallForceReset>(HandleForceReset);
            subscribed = true;
        }
        private void UnsubscribeAll()
        {
            if (!subscribed) return;
            EventBus.Unsubscribe<OnGimmickActivated>(HandleGimmickActivated);
            EventBus.Unsubscribe<OnBallForceReset>(HandleForceReset);
            subscribed = false;
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

        public static void ResetInstance() { Instance = null; }

        public void InitializeForTest()
        {
            if (Instance == null) Instance = this;
            SubscribeAll();
        }

        public GimmickEncounterRecord Get(GimmickId id)
        {
            if (id == GimmickId.None) return null;
            if (!cache.TryGetValue(id, out var rec))
            {
                rec = new GimmickEncounterRecord { id = id };
                cache[id] = rec;
                records.Add(rec);
            }
            return rec;
        }

        public int GetResistLevel(GimmickId id) => Get(id)?.resistLevel ?? 0;

        public float GetResistReduction(GimmickId id)
        {
            int lv = GetResistLevel(id);
            return Mathf.Clamp01(lv * Constants.GimmickResistReductionPerLevel);
        }

        private void HandleGimmickActivated(OnGimmickActivated e) => RegisterEncounter(e.GimmickId);
        private void HandleForceReset(OnBallForceReset e)
        {
            if (!e.Cause.HasValue) return;
            RegisterDeath(e.Cause.Value);
        }

        /// <summary>외부/테스트에서 직접 조우 카운트를 누적.</summary>
        public void RegisterEncounter(GimmickId id)
        {
            if (id == GimmickId.None) return;
            var rec = Get(id);
            rec.encounterCount++;
            CheckMilestones();
        }

        /// <summary>외부/테스트에서 직접 사망 카운트를 누적.</summary>
        public void RegisterDeath(GimmickId id)
        {
            if (id == GimmickId.None) return;
            var rec = Get(id);
            rec.deathCount++;
            int newLevel = CalcResistLevel(rec.deathCount);
            if (newLevel > rec.resistLevel)
            {
                rec.resistLevel = newLevel;
                EventBus.Publish(new OnGimmickResistanceLevelUp
                {
                    GimmickId = rec.id,
                    NewResistLevel = newLevel
                });
            }
        }

        private static int CalcResistLevel(int deathCount)
        {
            if (deathCount >= Constants.GimmickResistThreshold5) return 5;
            if (deathCount >= Constants.GimmickResistThreshold4) return 4;
            if (deathCount >= Constants.GimmickResistThreshold3) return 3;
            if (deathCount >= Constants.GimmickResistThreshold2) return 2;
            if (deathCount >= Constants.GimmickResistThreshold1) return 1;
            return 0;
        }

        // ── 마일스톤 보상 (10/40/80종 누적 조우) ──────────────

        public int UniqueEncountered()
        {
            int n = 0;
            foreach (var r in records) if (r.encounterCount > 0) n++;
            return n;
        }

        private void CheckMilestones()
        {
            int unique = UniqueEncountered();
            var econ = EconomyManager.Instance;
            if (!milestone10Awarded && unique >= 10)
            {
                milestone10Awarded = true;
                if (econ != null) econ.Add(CurrencyId.Gold, Constants.CollectionMilestone10Gold, "CollectionMilestone10");
            }
            if (!milestone40Awarded && unique >= 40)
            {
                milestone40Awarded = true;
                // 칭호 "trial_survivor" 는 M7 UI 측 인계
            }
            if (!milestone80Awarded && unique >= 80)
            {
                milestone80Awarded = true;
                // 전설 타로카드 1장 — AstrologerManager 가 있으면 임의 Legendary 추가
                if (AstrologerManager.Instance != null)
                {
                    AstrologerManager.Instance.SetSeed(System.DateTime.Now.Millisecond);
                    // 보너스: 점성술사가 가지고 있는 첫 Legendary 카드 무료 지급
                    // (M7 정식 화면에서 선택 UI 도입 시 교체)
                }
            }
        }
    }
}
