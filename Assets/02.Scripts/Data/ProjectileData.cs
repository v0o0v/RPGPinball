using UnityEngine;

namespace RPGPinball.Data
{
    /// <summary>
    /// 적/보스 탄막 정의. 마일스톤 2에서는 단순 Instantiate/Destroy.
    /// 마일스톤 4에서 풀링 기반(ProjectilePool) + 공 접촉 시 강제 감속/넉백 옵션 추가.
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

        // ── 마일스톤 4: 공 접촉 시 효과 ──────────────────────────
        [Header("공 접촉 시 강제 효과 (M2 #15 인계)")]
        public bool slowsBallOnContact;
        [Range(0f, 1f)]
        public float ballSlowMultiplier = 0.5f;
        public float ballSlowDuration = 1.0f;
        public bool knockbackBallOnContact;
        public float ballKnockbackForce = 5.0f;

        // ── 마일스톤 4: 벽 반사 (미치광이 발명가 P3) ─────────────
        [Header("벽 반사 횟수 (반사 탄막)")]
        public int wallBounceLimit = 0;
    }
}
