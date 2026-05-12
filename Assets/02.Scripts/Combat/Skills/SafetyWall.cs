using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 세이프티 월 (제어 · A · Tier 4). 좌·우 벽 + 바닥에 임시 트램펄린 소환.
    /// 공 접촉 시 보스 방향(위쪽)으로 강제 반사. 데미지 없음.
    /// 지속: 2.0 + Lv × 0.2s
    /// </summary>
    public class SafetyWall : ActiveSkillBase
    {
        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float duration = data != null ? data.GetDuration(level) : 2f + level * 0.2f;

            var holder = new GameObject("SafetyWall_Temp");
            holder.transform.position = Vector3.zero;
            // 바닥 트램펄린
            CreateTrampoline(holder.transform, new Vector2(0, -5.5f), new Vector2(Constants.PlayfieldWidth, 0.5f));
            // 좌측 벽
            CreateTrampoline(holder.transform, new Vector2(-4.5f, -3.5f), new Vector2(0.5f, 3f));
            // 우측 벽
            CreateTrampoline(holder.transform, new Vector2(4.5f, -3.5f), new Vector2(0.5f, 3f));

            Debug.Log($"[SafetyWall] 발동 @ Lv.{level}, 지속 {duration:F1}s");

            try
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), cancellationToken: ct);
            }
            catch (System.OperationCanceledException) { /* 취소 정상 */ }
            finally
            {
                if (holder != null) Destroy(holder);
            }
        }

        private void CreateTrampoline(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Trampoline");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = false;

            // 반사 핸들러
            var reflector = go.AddComponent<SafetyWallReflector>();
        }
    }

    /// <summary>세이프티 월에 부착되어 공 접촉 시 위로 반사하는 컴포넌트.</summary>
    public class SafetyWallReflector : MonoBehaviour
    {
        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!col.gameObject.CompareTag(Constants.TagBall)) return;
            var rb = col.rigidbody;
            if (rb == null) return;
            // 위쪽으로 강제 반사 (30N 임펄스)
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(Vector2.up * 30f, ForceMode2D.Impulse);
        }
    }
}
