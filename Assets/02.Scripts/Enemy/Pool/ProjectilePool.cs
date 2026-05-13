using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPGPinball.Data;

namespace RPGPinball.Enemy.Pool
{
    /// <summary>
    /// ProjectileBase 풀링. 마일스톤 4 보스 탄막 양이 많아 GC 압박 회피용.
    /// 동일 ProjectileData 단위로 Stack 풀 유지. prewarm 후 Spawn/Despawn 라운드트립.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        [Header("프리팹 (탄막 사이즈별)")]
        [SerializeField] private GameObject smallProjectilePrefab;
        [SerializeField] private GameObject largeProjectilePrefab;
        [SerializeField] private GameObject specialProjectilePrefab;

        [Header("프리워밍 토글")]
        [SerializeField] private bool prewarmOnAwake = true;

        private readonly Dictionary<ProjectileData, Stack<ProjectileBase>> pools = new();
        private readonly Dictionary<ProjectileData, GameObject> prefabsForData = new();
        // 검증용 카운터 (EditMode 테스트가 인스턴스 개수 검사)
        public int TotalInstantiateCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                Instance = null;
            }
        }

        /// <summary>
        /// 풀에서 탄막 인스턴스를 꺼내 활성화 + Launch 호출.
        /// 풀이 비어 있으면 새 인스턴스 생성 (Instantiate).
        /// </summary>
        public ProjectileBase Spawn(ProjectileData data, Vector2 pos, Vector2 dir)
        {
            if (data == null) { Debug.LogError("[ProjectilePool] data == null"); return null; }

            var prefab = ResolvePrefab(data);
            if (prefab == null)
            {
                Debug.LogError($"[ProjectilePool] {data.size} 크기 탄막 프리팹이 등록되지 않음", this);
                return null;
            }

            if (!pools.TryGetValue(data, out var stack))
            {
                stack = new Stack<ProjectileBase>();
                pools[data] = stack;
                prefabsForData[data] = prefab;
            }

            ProjectileBase pj;
            if (stack.Count > 0)
            {
                pj = stack.Pop();
                pj.transform.position = pos;
                pj.transform.rotation = Quaternion.identity;
                pj.gameObject.SetActive(true);
            }
            else
            {
                var go = Instantiate(prefab, pos, Quaternion.identity);
                pj = go.GetComponent<ProjectileBase>();
                if (pj == null)
                {
                    Debug.LogError($"[ProjectilePool] 프리팹에 ProjectileBase 컴포넌트가 없음: {prefab.name}");
                    Destroy(go);
                    return null;
                }
                pj.AssignData(data);
                TotalInstantiateCount++;
            }

            pj.OnSpawn();
            pj.Launch(dir);
            return pj;
        }

        /// <summary>풀로 반환. 비활성화 후 스택에 푸시.</summary>
        public void Despawn(ProjectileBase pj)
        {
            if (pj == null) return;
            pj.OnDespawn();
            pj.gameObject.SetActive(false);
            pj.transform.SetParent(transform, false);

            if (pj.Data != null && pools.TryGetValue(pj.Data, out var stack))
            {
                stack.Push(pj);
            }
            else
            {
                // 데이터가 없는 비정상 케이스 — 안전하게 파괴
                Destroy(pj.gameObject);
            }
        }

        /// <summary>탄막 데이터별로 N개를 미리 인스턴스화 (씬 진입 시 1회 권장).</summary>
        public void Prewarm(ProjectileData data, int count)
        {
            if (data == null || count <= 0) return;
            var prefab = ResolvePrefab(data);
            if (prefab == null) return;

            if (!pools.TryGetValue(data, out var stack))
            {
                stack = new Stack<ProjectileBase>();
                pools[data] = stack;
                prefabsForData[data] = prefab;
            }
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                var pj = go.GetComponent<ProjectileBase>();
                if (pj != null)
                {
                    pj.AssignData(data);
                    pj.gameObject.SetActive(false);
                    stack.Push(pj);
                    TotalInstantiateCount++;
                }
            }
        }

        private GameObject ResolvePrefab(ProjectileData data)
        {
            switch (data.size)
            {
                case ProjectileData.ProjectileSize.Small: return smallProjectilePrefab;
                case ProjectileData.ProjectileSize.Large: return largeProjectilePrefab;
                case ProjectileData.ProjectileSize.Special: return specialProjectilePrefab;
                default: return smallProjectilePrefab;
            }
        }

        private void OnSceneUnloaded(Scene s)
        {
            // 씬 전환 시 풀 비우기 (보스 전용 탄막이 다음 씬으로 새지 않도록)
            foreach (var kv in pools)
            {
                while (kv.Value.Count > 0)
                {
                    var pj = kv.Value.Pop();
                    if (pj != null) Destroy(pj.gameObject);
                }
            }
            pools.Clear();
            prefabsForData.Clear();
            TotalInstantiateCount = 0;
        }

        // ── EditMode 테스트용 헬퍼 ─────────────────────────────

        public int GetPoolSize(ProjectileData data)
        {
            return pools.TryGetValue(data, out var stack) ? stack.Count : 0;
        }
    }
}
