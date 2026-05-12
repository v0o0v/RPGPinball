using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RPGPinball.Combat.Skills
{
    /// <summary>
    /// 마그네틱 필드 (제어 · A · Tier 2).
    /// 터치 지점 원형 자기장. 공을 인력으로 끌어당김. 데미지 없음.
    /// </summary>
    public class MagneticField : ActiveSkillBase
    {
        [SerializeField] private float duration = 2.0f;
        [SerializeField] private float pullForce = 12f;

        public override async UniTask Execute(Vector2 targetPos, CancellationToken ct)
        {
            float radius = data != null ? data.radius : 3.0f;
            float t = 0f;
            Debug.Log($"[MagneticField] 발동 @ {targetPos}, 반경 {radius}, 지속 {duration}s");

            while (t < duration && !ct.IsCancellationRequested)
            {
                var hits = Physics2D.OverlapCircleAll(targetPos, radius);
                foreach (var h in hits)
                {
                    if (!h.CompareTag(Core.Constants.TagBall)) continue;
                    var rb = h.attachedRigidbody;
                    if (rb == null) continue;
                    Vector2 dir = targetPos - rb.position;
                    rb.AddForce(dir.normalized * pullForce, ForceMode2D.Force);
                }
                t += Time.deltaTime;
                await UniTask.Yield(ct);
            }
        }
    }
}
