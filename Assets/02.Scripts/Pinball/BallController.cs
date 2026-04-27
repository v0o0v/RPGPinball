using UnityEngine;

namespace RPGPinball.Pinball
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BallController : MonoBehaviour
    {
        private Rigidbody2D rb;
        [SerializeField] private float maxSpeed = 100f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            // Limit max speed
            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }
    }
}
