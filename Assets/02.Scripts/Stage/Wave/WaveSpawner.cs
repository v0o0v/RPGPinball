using System.Collections.Generic;
using UnityEngine;
using RPGPinball.Data;
using RPGPinball.Enemy;

namespace RPGPinball.Stage.Wave
{
    /// <summary>
    /// 웨이브 단위 몬스터 인스턴스화.
    /// HP/ATK 동적 스케일링은 SpawnedMonster의 Initialize 시 SO 원본 값에 곱해 주입.
    /// </summary>
    public class WaveSpawner
    {
        private readonly Transform parent;
        private readonly ActId actId;
        private readonly int stageIndex;
        private readonly StageModifierData modifierComposite;

        public WaveSpawner(Transform parent, ActId actId, int stageIndex, StageModifierData modifierComposite = null)
        {
            this.parent = parent;
            this.actId = actId;
            this.stageIndex = stageIndex;
            this.modifierComposite = modifierComposite;
        }

        public List<GameObject> SpawnWave(StageBlueprint.WaveEntry wave)
        {
            var spawned = new List<GameObject>();
            float actMul = DifficultyBudget.GetActMultiplier(actId);
            float hpFactor = actMul * (1f + stageIndex * 0.05f);
            float atkFactor = actMul * (1f + stageIndex * 0.03f);

            // 모디파이어 보정
            if (modifierComposite != null)
            {
                hpFactor *= modifierComposite.monsterHpMultiplier;
            }

            for (int i = 0; i < wave.monsterIds.Length; i++)
            {
                var data = MonsterPool.Instance.FindByAssetName(wave.monsterIds[i]);
                if (data == null) continue;

                GameObject go;
                if (data.prefab != null)
                {
                    go = Object.Instantiate(data.prefab, parent != null ? parent : null);
                }
                else
                {
                    go = new GameObject($"Monster_{data.name}_{i}");
                    if (parent != null) go.transform.SetParent(parent, false);
                }

                // 런타임 스탯 주입 — MonsterBase가 있으면 스케일링된 복제 SO 주입.
                var mb = go.GetComponent<MonsterBase>();
                if (mb != null)
                {
                    var scaledData = ScriptableObject.Instantiate(data); // 원본 SO 손상 방지
                    scaledData.maxHp = Mathf.RoundToInt(data.maxHp * hpFactor);
                    scaledData.xpReward = Mathf.RoundToInt(data.xpReward * Mathf.Max(1f, atkFactor));
                    mb.InjectData(scaledData);
                }
                spawned.Add(go);
            }
            return spawned;
        }

        private static class DifficultyBudget
        {
            public static float GetActMultiplier(ActId actId) => RPGPinball.Stage.Generation.DifficultyBudget.GetActMultiplier(actId);
        }
    }
}
