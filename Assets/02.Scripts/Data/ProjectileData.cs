using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 적/보스 탄막 정의. 마일스톤 2에서는 더미 탄막 1~2종, 마일스톤 4에서 풀링 기반으로 전환.
    /// </summary>
    [CreateAssetMenu(menuName = "RPG Pinball/Projectile", fileName = "ProjectileData")]
    public class ProjectileData : ScriptableObject
    {
        public enum ProjectileSize
        {
            Small,   // 0.15U
            Large,   // 0.4U
            Special  // 0.6U
        }

        [Header("식별")]
        public int id;
        public string displayName;

        [Header("물리")]
        public ProjectileSize size = ProjectileSize.Small;
        public float speed = Core.Constants.ProjectileDefaultSpeed;

        [Header("판정")]
        public bool blockableByFlipper = true;
        public float deadZonePenalty = Core.Constants.ProjectilePenetratePenalty;
        public bool homing;
    }
}
