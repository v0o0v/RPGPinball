using System;
using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Meta
{
    [System.Serializable]
    public class QuestInstance
    {
        public string questId;
        public QuestKind kind;
        public int progress;
        public int target;
        public bool claimed;
        public DateTimeOffset expiresAt;

        public bool Completed => progress >= target;
    }

    /// <summary>
    /// 일일·주간·현상금 의뢰 풀 관리. UTC+9 자정 갱신 + IClock 주입으로 테스트 가능.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private QuestData[] dailyPool;
        [SerializeField] private QuestData[] weeklyPool;
        [SerializeField] private QuestData[] bountyPool;

        [SerializeField] private List<QuestInstance> dailyQuests = new();
        [SerializeField] private QuestInstance weeklyQuest;
        [SerializeField] private List<QuestInstance> bountyTargets = new();

        private IClock clock = SystemClock.Instance;
        private System.Random rng = new System.Random();

        public IClock Clock => clock;
        public IReadOnlyList<QuestInstance> DailyQuests => dailyQuests;
        public QuestInstance WeeklyQuest => weeklyQuest;
        public IReadOnlyList<QuestInstance> BountyTargets => bountyTargets;

        public void SetClock(IClock clock) { this.clock = clock ?? SystemClock.Instance; }
        public void SetSeed(int seed) { rng = new System.Random(seed); }

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
            SubscribeAll();
        }

        private void SubscribeAll()
        {
            if (subscribed) return;
            EventBus.Subscribe<OnStageCleared>(HandleStageClear);
            EventBus.Subscribe<OnBossDefeated>(HandleBossDefeated);
            EventBus.Subscribe<OnEliteDefeated>(HandleEliteDefeated);
            EventBus.Subscribe<RPGPinball.Stage.Gimmicks.OnGimmickActivated>(HandleGimmick);
            EventBus.Subscribe<OnFlipperSummoned>(HandleFlipperSummoned);
            EventBus.Subscribe<OnBallForceReset>(HandleForceReset);
            subscribed = true;
        }
        private void UnsubscribeAll()
        {
            if (!subscribed) return;
            EventBus.Unsubscribe<OnStageCleared>(HandleStageClear);
            EventBus.Unsubscribe<OnBossDefeated>(HandleBossDefeated);
            EventBus.Unsubscribe<OnEliteDefeated>(HandleEliteDefeated);
            EventBus.Unsubscribe<RPGPinball.Stage.Gimmicks.OnGimmickActivated>(HandleGimmick);
            EventBus.Unsubscribe<OnFlipperSummoned>(HandleFlipperSummoned);
            EventBus.Unsubscribe<OnBallForceReset>(HandleForceReset);
            subscribed = false;
        }

        private void OnEnable() { SubscribeAll(); }
        private void OnDisable() { UnsubscribeAll(); if (Instance == this) Instance = null; }
        private void OnDestroy() { UnsubscribeAll(); if (Instance == this) Instance = null; }

        public static void ResetInstance() { Instance = null; }

        public void InitializeForTest()
        {
            if (Instance == null) Instance = this;
            SubscribeAll();
        }

        // ── 갱신 시각 계산 (UTC+9 자정 / 매주 월요일) ────────

        private DateTimeOffset NextDailyRefreshKst(DateTimeOffset nowUtc)
        {
            // KST = UTC+9. 다음 00:00 KST
            var nowKst = nowUtc.ToOffset(TimeSpan.FromHours(Constants.SeedKstOffsetHours));
            var tomorrow = nowKst.Date.AddDays(1);
            return new DateTimeOffset(tomorrow, TimeSpan.FromHours(Constants.SeedKstOffsetHours));
        }

        private DateTimeOffset NextWeeklyRefreshKst(DateTimeOffset nowUtc)
        {
            var nowKst = nowUtc.ToOffset(TimeSpan.FromHours(Constants.SeedKstOffsetHours));
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)nowKst.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            var monday = nowKst.Date.AddDays(daysUntilMonday);
            return new DateTimeOffset(monday, TimeSpan.FromHours(Constants.SeedKstOffsetHours));
        }

        // ── 갱신 로직 ──────────────────────────────────────

        public void RefreshDailyIfExpired()
        {
            var now = clock.UtcNow;
            bool needsRefresh = dailyQuests.Count == 0;
            foreach (var q in dailyQuests) if (now > q.expiresAt) { needsRefresh = true; break; }
            if (!needsRefresh) return;
            RollDaily(now);
        }

        public void RefreshWeeklyIfExpired()
        {
            var now = clock.UtcNow;
            if (weeklyQuest == null || now > weeklyQuest.expiresAt) RollWeekly(now);
        }

        public void RefreshBountyIfExpired()
        {
            var now = clock.UtcNow;
            bool needsRefresh = bountyTargets.Count == 0;
            foreach (var q in bountyTargets) if (now > q.expiresAt) { needsRefresh = true; break; }
            if (!needsRefresh) return;
            RollBounty(now);
        }

        private void RollDaily(DateTimeOffset now)
        {
            dailyQuests.Clear();
            if (dailyPool == null || dailyPool.Length == 0) return;
            var expiry = NextDailyRefreshKst(now);

            var pool = new List<QuestData>(dailyPool);
            int picks = Mathf.Min(Constants.DailyQuestSlotCount, pool.Count);
            var ids = new List<string>(picks);
            for (int i = 0; i < picks; i++)
            {
                int idx = rng.Next(0, pool.Count);
                var pick = pool[idx];
                pool.RemoveAt(idx);
                dailyQuests.Add(new QuestInstance
                {
                    questId = pick.questId,
                    kind = QuestKind.Daily,
                    target = pick.targetValue,
                    progress = 0,
                    claimed = false,
                    expiresAt = expiry
                });
                ids.Add(pick.questId);
            }
            EventBus.Publish(new OnDailyQuestRolled { QuestIds = ids.ToArray() });
        }

        private void RollWeekly(DateTimeOffset now)
        {
            weeklyQuest = null;
            if (weeklyPool == null || weeklyPool.Length == 0) return;
            var expiry = NextWeeklyRefreshKst(now);
            var pick = weeklyPool[rng.Next(0, weeklyPool.Length)];
            weeklyQuest = new QuestInstance
            {
                questId = pick.questId,
                kind = QuestKind.Weekly,
                target = pick.targetValue,
                expiresAt = expiry
            };
        }

        private void RollBounty(DateTimeOffset now)
        {
            bountyTargets.Clear();
            if (bountyPool == null || bountyPool.Length == 0) return;
            var expiry = NextWeeklyRefreshKst(now);
            int picks = Mathf.Min(Constants.BountySlotCount, bountyPool.Length);
            for (int i = 0; i < picks; i++)
            {
                var pick = bountyPool[i];
                bountyTargets.Add(new QuestInstance
                {
                    questId = pick.questId,
                    kind = QuestKind.Bounty,
                    target = pick.targetValue > 0 ? pick.targetValue : 1,
                    expiresAt = expiry
                });
            }
        }

        // ── 진행도/청구 ────────────────────────────────────

        public void ReportProgress(QuestObjectiveKind kind, int delta, string optionalArg = null)
        {
            for (int i = 0; i < dailyQuests.Count; i++) ApplyTo(dailyQuests[i], kind, delta, optionalArg);
            if (weeklyQuest != null) ApplyTo(weeklyQuest, kind, delta, optionalArg);
            for (int i = 0; i < bountyTargets.Count; i++) ApplyTo(bountyTargets[i], kind, delta, optionalArg);
        }

        private void ApplyTo(QuestInstance qi, QuestObjectiveKind kind, int delta, string optionalArg)
        {
            var data = LookupData(qi.questId);
            if (data == null || data.objective != kind) return;
            qi.progress = Mathf.Min(qi.progress + delta, qi.target);
            EventBus.Publish(new OnQuestProgress { QuestId = qi.questId, CurrentProgress = qi.progress, Target = qi.target });
            if (qi.Completed && !qi.claimed)
            {
                EventBus.Publish(new OnQuestCompleted { QuestId = qi.questId, Kind = qi.kind });
            }
        }

        public QuestData LookupData(string questId)
        {
            foreach (var d in dailyPool ?? Array.Empty<QuestData>()) if (d != null && d.questId == questId) return d;
            foreach (var d in weeklyPool ?? Array.Empty<QuestData>()) if (d != null && d.questId == questId) return d;
            foreach (var d in bountyPool ?? Array.Empty<QuestData>()) if (d != null && d.questId == questId) return d;
            return null;
        }

        public bool ClaimReward(QuestInstance qi)
        {
            if (qi == null || !qi.Completed || qi.claimed) return false;
            var data = LookupData(qi.questId);
            if (data == null) return false;

            var econ = EconomyManager.Instance;
            if (econ != null)
            {
                if (data.goldReward > 0) econ.Add(CurrencyId.Gold, data.goldReward, "QuestClaim");
                if (data.manaCrystalReward > 0) econ.Add(CurrencyId.ManaCrystal, data.manaCrystalReward, "QuestClaim");
                if (data.bossSoulReward > 0) econ.Add(CurrencyId.BossSoul, data.bossSoulReward, "QuestClaim");
                if (data.blueprintFragmentReward > 0) econ.Add(CurrencyId.BlueprintFragment, data.blueprintFragmentReward, "QuestClaim");
                if (data.respecScrollReward) econ.Add(CurrencyId.RespecScroll, 1, "QuestClaim");
            }
            qi.claimed = true;
            return true;
        }

        // ── 이벤트 핸들러 ──────────────────────────────────

        private void HandleStageClear(OnStageCleared e) { /* TimePenaltyZero/ActFullClear 등은 별도 처리 — 이 마일스톤은 골격 */ }
        private void HandleBossDefeated(OnBossDefeated e) { ReportProgress(QuestObjectiveKind.BossNoWeakpointHit, 1); }
        private void HandleEliteDefeated(OnEliteDefeated e) { ReportProgress(QuestObjectiveKind.EliteDefeat, 1); }
        private void HandleGimmick(RPGPinball.Stage.Gimmicks.OnGimmickActivated e) { ReportProgress(QuestObjectiveKind.GimmickExperience, 1); }
        private void HandleFlipperSummoned(OnFlipperSummoned e) { ReportProgress(QuestObjectiveKind.FlipperSummonLimit, 1); }
        private void HandleForceReset(OnBallForceReset e) { /* NoForcedReset 위반 처리는 별도 — 이 마일스톤은 골격 */ }
    }
}
