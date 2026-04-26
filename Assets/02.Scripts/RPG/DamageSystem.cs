using UnityEngine;

namespace RPGPinball.RPG
{
    public static class DamageSystem
    {
        // Utility method to calculate damage based on ball velocity and player stats
        public static int CalculateDamage(PlayerStats stats, Rigidbody2D ballRb)
        {
            if (stats == null || ballRb == null) return 0;
            
            float velocityMagnitude = ballRb.linearVelocity.magnitude;
            // Example formula: baseDamage + (velocity * multiplier)
            int calculatedDamage = stats.baseDamage + Mathf.RoundToInt(velocityMagnitude * 0.5f);
            
            return calculatedDamage;
        }
    }
}
