using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Stage.Wave
{
    /// <summary>
    /// 웨이브 큐를 진행하며 모든 몬스터 처치 시 다음 웨이브로 자동 전환.
    /// 절차 생성 결과 (StageBlueprint.waves) 를 입력으로 받음.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        private readonly Queue<StageBlueprint.WaveEntry> queue = new();
        private readonly List<GameObject> currentMonsters = new();
        private WaveSpawner spawner;
        private int currentWaveIndex;
        private int totalWaves;

        public bool IsAllCleared => totalWaves > 0 && queue.Count == 0 && currentMonsters.TrueForAll(go => go == null);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(StageBlueprint blueprint, Transform spawnParent, StageModifierData modifierComposite = null)
        {
            queue.Clear();
            currentMonsters.Clear();
            currentWaveIndex = 0;

            if (blueprint == null) return;
            for (int i = 0; i < blueprint.waves.Count; i++) queue.Enqueue(blueprint.waves[i]);
            totalWaves = queue.Count;
            spawner = new WaveSpawner(spawnParent, blueprint.actId, blueprint.stageIndex, modifierComposite);
        }

        public void StartNextWave()
        {
            if (queue.Count == 0) return;
            currentWaveIndex++;
            var wave = queue.Dequeue();
            currentMonsters.Clear();
            currentMonsters.AddRange(spawner.SpawnWave(wave));
            EventBus.Publish(new OnWaveSpawned
            {
                WaveIndex = currentWaveIndex,
                MonsterCount = currentMonsters.Count,
                Pattern = wave.pattern,
                HasElite = wave.hasElite
            });
        }

        private void Update()
        {
            if (currentMonsters.Count == 0) return;
            // 살아있는 몬스터 카운트
            int alive = 0;
            for (int i = 0; i < currentMonsters.Count; i++)
                if (currentMonsters[i] != null) alive++;

            if (alive == 0)
            {
                EventBus.Publish(new OnWaveCleared { WaveIndex = currentWaveIndex });
                currentMonsters.Clear();
                if (queue.Count > 0) StartNextWave();
            }
        }
    }

    // ── 웨이브 이벤트 ────────────────────────────────────────────
    public struct OnWaveSpawned
    {
        public int WaveIndex;
        public int MonsterCount;
        public WaveCompositionPattern Pattern;
        public bool HasElite;
    }

    public struct OnWaveCleared
    {
        public int WaveIndex;
    }
}
