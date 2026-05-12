using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Physics;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 멀티볼 I (원소 · A · Tier 4). 본체 위치에서 분열 공 1개 추가 생성.
    /// 분열 공 데미지 = 본체 × 0.8 (분열 코어로 완화 가능).
    /// 하드캡: MultiBallHardCap (5). 카메라 줌아웃 자동 적용.
    /// 지속: 5.0 + Lv × 0.3s
    /// </summary>
    public class MultiBallI : ActiveSkillBase
    {
        [SerializeField] private GameObject ballPrefab; // Inspector에서 설정

        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 5f + level * 0.3f;

            var existing = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            int currentCount = existing != null ? existing.Length : 0;
            int maxAllowed = SkillTreeManager.Instance != null
                ? SkillTreeManager.Instance.GetMaxMultiBallCount()
                : 2;
            // 하드캡
            maxAllowed = Mathf.Min(maxAllowed, Constants.MultiBallHardCap);

            if (currentCount >= maxAllowed)
            {
                Debug.LogWarning($"[MultiBallI] 하드캡 도달 ({currentCount}/{maxAllowed}) — 분열 거부");
                return;
            }

            // 본체 공 위치 기준 0.5u 반경 무작위 위치
            BallController source = existing != null && existing.Length > 0 ? existing[0] : null;
            Vector2 basePos = source != null ? (Vector2)source.transform.position : targetPos;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Vector2 spawnPos = basePos + offset;

            GameObject prefab = ballPrefab;
            if (prefab == null && source != null) prefab = source.gameObject; // 본체 복제 폴백
            if (prefab == null)
            {
                Debug.LogWarning("[MultiBallI] ballPrefab이 설정되지 않음. Inspector에 Ball.prefab을 할당하세요.");
                return;
            }

            var splitGO = Instantiate(prefab, spawnPos, Quaternion.identity);
            var splitBall = splitGO.GetComponent<BallController>();
            if (splitBall != null)
            {
                splitBall.IsSplitBall = true;
            }

            Debug.Log($"[MultiBallI] 분열 발동 Lv.{level} | 활성 공 {currentCount + 1}/{maxAllowed} | 지속 {duration:F1}s");

            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { /* 취소 정상 */ }
            finally
            {
                if (splitGO != null) Destroy(splitGO);
            }
        }
    }
}
