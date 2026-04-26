using UnityEngine;

namespace RPGPinball.Pinball
{
    public class Bumper : MonoBehaviour
    {
        [SerializeField] private float bounceForce = 10f;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (ballRb != null)
                {
                    Vector2 bounceDirection = collision.GetContact(0).normal * -1;
                    ballRb.AddForce(bounceDirection * bounceForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}
