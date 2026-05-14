using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// 주점 시설 진입점. QuestManager 를 감싸 UI 호출을 단순화.
    /// 현상금 입장 조건 (해당 액트 최종 보스 처치 여부) 검증.
    /// </summary>
    public class TavernManager : MonoBehaviour
    {
        public static TavernManager Instance { get; private set; }

        [SerializeField] private List<BossId> defeatedBosses = new();

        public IReadOnlyList<BossId> DefeatedBosses => defeatedBosses;

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
            EventBus.Subscribe<OnBossDefeated>(HandleBossDefeated);
            subscribed = true;
        }
        private void UnsubscribeAll()
        {
            if (!subscribed) return;
            EventBus.Unsubscribe<OnBossDefeated>(HandleBossDefeated);
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

        private void HandleBossDefeated(OnBossDefeated e)
        {
            if (e.BossId != BossId.None && !defeatedBosses.Contains(e.BossId))
                defeatedBosses.Add(e.BossId);
        }

        public IReadOnlyList<QuestInstance> GetActiveDailyQuests()
        {
            return QuestManager.Instance != null ? QuestManager.Instance.DailyQuests : new List<QuestInstance>();
        }

        public QuestInstance GetActiveWeeklyQuest()
        {
            return QuestManager.Instance != null ? QuestManager.Instance.WeeklyQuest : null;
        }

        public IReadOnlyList<QuestInstance> GetBountyBoard()
        {
            return QuestManager.Instance != null ? QuestManager.Instance.BountyTargets : new List<QuestInstance>();
        }

        /// <summary>현상금 입장 시도. 액트 최종 보스 처치 여부 검증 후 ArenaLayout 로드.</summary>
        public bool EnterBountyStage(QuestInstance bounty)
        {
            if (bounty == null || bounty.kind != QuestKind.Bounty) return false;
            var data = QuestManager.Instance?.LookupData(bounty.questId);
            if (data == null) return false;
            if (data.requiredActBossDefeated != BossId.None && !defeatedBosses.Contains(data.requiredActBossDefeated))
                return false;
            EventBus.Publish(new OnBountyAccepted { BountyId = bounty.questId, EliteId = data.bountyTargetEliteId });
            // 실제 씬 로드는 M7 인계 — 본 마일스톤은 이벤트 발행까지
            return true;
        }
    }
}
