using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.Enemy
{
    /// <summary>
    /// 적/보스 탄막. 마일스톤 2에서는 단순 Instantiate/Destroy. 마일스톤 4에서 풀링으로 전환.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class ProjectileBase : MonoBehaviour
    {
        [SerializeField] private ProjectileData data;
        [SerializeField] private float lifetime = 6f;

        private Rigidbody2D rb;
        private float age;

        public ProjectileData Data => data;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            gameObject.tag = Constants.TagProjectile;
        }

        public void Launch(Vector2 dir)
        {
            if (data == null) { Debug.LogError($"[ProjectileBase] ProjectileData 누락: {name}"); return; }
            rb.linearVelocity = dir.normalized * data.speed;
            age = 0f;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            HandleHit(col.gameObject);
        }

        private void HandleHit(GameObject target)
        {
            if (target.CompareTag(Constants.TagDeadZone))
            {
                EventBus.Publish(new OnProjectilePenalty { Delta = data.deadZonePenalty });
                Destroy(gameObject);
                return;
            }

            if (target.CompareTag(Constants.TagFlipper))
            {
                if (data.blockableByFlipper)
                {
                    EventBus.Publish(new OnFlipperBlocked { CooldownReduction = Constants.FlipperBlockCooldownBonus });
                    Destroy(gameObject);
                }
                return;
            }
        }
    }
}
