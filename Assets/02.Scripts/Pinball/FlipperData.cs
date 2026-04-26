using UnityEngine;

namespace RPGPinball.Pinball
{
    [CreateAssetMenu(fileName = "New Flipper Data", menuName = "RPG Pinball/Flipper Data")]
    public class FlipperData : ScriptableObject
    {
        [Header("Physics Settings")]
        [Tooltip("Rigidbody2D 질량 (공보다 무거워야 함)")]
        public float mass = 20f;

        [Header("Motor Settings")]
        [Tooltip("플리퍼 타격 속도 (왼쪽은 음수, 오른쪽은 양수 추천)")]
        public float hitForce = -7000f;
        [Tooltip("모터 최대 힘 (공의 무게를 밀어낼 힘, 10000 이상 추천)")]
        public float maxMotorTorque = 10000f;

        [Header("Angle Limits")]
        public float lowerAngle = -10f;
        public float upperAngle = 45f;
    }
}
