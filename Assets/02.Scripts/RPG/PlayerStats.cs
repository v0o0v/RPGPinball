using UnityEngine;

namespace RPGPinball.RPG
{
    public class PlayerStats : MonoBehaviour
    {
        public int maxHP = 100;
        public int currentHP;
        public int baseDamage = 10;
        public int level = 1;
        public int currentExp = 0;

        private void Awake()
        {
            currentHP = maxHP;
        }

        public void TakeDamage(int damage)
        {
            currentHP -= damage;
            if (currentHP <= 0)
            {
                Debug.Log("Player Game Over");
            }
        }

        public void GainExp(int exp)
        {
            currentExp += exp;
            // Level up logic...
        }
    }
}
