using UnityEngine;

namespace RPGPinball.Pinball
{
    public class DeadZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Ball"))
            {
                Debug.Log("Ball entered dead zone. Lose life/HP.");
                // TODO: Trigger GameManager life loss event
                // TODO: Respawn ball
            }
        }
    }
}
