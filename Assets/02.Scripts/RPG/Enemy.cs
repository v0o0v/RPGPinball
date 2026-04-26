using UnityEngine;

namespace RPGPinball.RPG
{
    public class Enemy : MonoBehaviour
    {
        public int maxHP = 50;
        private int currentHP;
        public int expReward = 10;

        private void Awake()
        {
            currentHP = maxHP;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                // TODO: Get actual damage from DamageSystem or PlayerStats
                int damageToTake = 10; 
                TakeDamage(damageToTake);
            }
        }

        public void TakeDamage(int damage)
        {
            currentHP -= damage;
            if (currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Enemy died, give exp.");
            Destroy(gameObject);
        }
    }
}
