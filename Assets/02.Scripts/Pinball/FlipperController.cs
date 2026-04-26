using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGPinball.Pinball
{
    [RequireComponent(typeof(HingeJoint2D), typeof(Rigidbody2D))]
    public class FlipperController : MonoBehaviour
    {
        [SerializeField] private InputAction flipperAction;
        [SerializeField] private FlipperData flipperData;
        
        private HingeJoint2D hinge;
        private JointMotor2D motor;
        private Rigidbody2D rb;

        private void Awake()
        {
            hinge = GetComponent<HingeJoint2D>();
            rb = GetComponent<Rigidbody2D>();
            motor = hinge.motor;

            if (flipperData != null)
            {
                ApplyFlipperData();
            }
        }

        private void ApplyFlipperData()
        {
            // 질량 적용
            rb.mass = flipperData.mass;

            // 각도 제한 적용
            JointAngleLimits2D limits = hinge.limits;
            limits.min = flipperData.lowerAngle;
            limits.max = flipperData.upperAngle;
            hinge.limits = limits;
            hinge.useLimits = true;

            // 모터 파워 적용
            motor.maxMotorTorque = flipperData.maxMotorTorque;
            hinge.motor = motor;
        }

        private void OnEnable()
        {
            flipperAction.Enable();
            flipperAction.started += OnFlipperPressed;
            flipperAction.canceled += OnFlipperReleased;
        }

        private void OnDisable()
        {
            flipperAction.Disable();
            flipperAction.started -= OnFlipperPressed;
            flipperAction.canceled -= OnFlipperReleased;
        }

        private void OnFlipperPressed(InputAction.CallbackContext context)
        {
            float force = flipperData != null ? flipperData.hitForce : 10000f;
            motor.motorSpeed = -force;
            hinge.motor = motor;
            hinge.useMotor = true;
        }

        private void OnFlipperReleased(InputAction.CallbackContext context)
        {
            float force = flipperData != null ? flipperData.hitForce : 10000f;
            motor.motorSpeed = force;
            hinge.motor = motor;
            hinge.useMotor = true;
        }
    }
}
