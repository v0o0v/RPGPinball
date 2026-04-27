using UnityEngine;

namespace RPGPinball.Pinball
{
    public class Bumper : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 10f;
        [SerializeField] private int scoreValue = 100;
        
        private Vector3 initialScale;
        private float animationDuration = 0.1f;
        private float scaleMultiplier = 1.2f;

        private void Awake()
        {
            initialScale = transform.localScale;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                // Score
                if (RPGPinball.Core.GameManager.Instance != null)
                {
                    RPGPinball.Core.GameManager.Instance.AddScore(scoreValue);
                }

                // Physics
                Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    Vector2 bounceDirection = collision.GetContact(0).normal * -1;
                    ballRb.AddForce(bounceDirection * bounceForce, ForceMode2D.Impulse);
                }

                // Feedback
                StopAllCoroutines();
                StartCoroutine(PulseAnimation());
            }
        }

        private System.Collections.IEnumerator PulseAnimation()
        {
            transform.localScale = initialScale * scaleMultiplier;
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale * scaleMultiplier, initialScale, elapsed / animationDuration);
                yield return null;
            }
            transform.localScale = initialScale;
        }
    }
}
