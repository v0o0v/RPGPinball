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
            flipperAction.performed += OnFlipperPressed;
            flipperAction.canceled += OnFlipperReleased;
        }

        private void OnDisable()
        {
            flipperAction.Disable();
            flipperAction.performed -= OnFlipperPressed;
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ball"))
            {
                float targetHitSpeed = flipperData != null ? -flipperData.hitForce : -10000f;
                
                // If motor is currently trying to flip UP
                if (hinge.useMotor && Mathf.Approximately(motor.motorSpeed, targetHitSpeed))
                {
                    Rigidbody2D ballRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (ballRb != null && flipperData != null)
                    {
                        // Apply extra impulse in the direction the flipper is facing (usually 'up' for a flipper sprite)
                        Vector2 hitDir = transform.up;
                        ballRb.AddForce(hitDir * flipperData.kickForce, ForceMode2D.Impulse);
                        
                        // Also slightly increase the ball's velocity based on how far it is from the pivot
                        // to simulate the tip of the flipper moving faster.
                        float distanceToPivot = Vector2.Distance(collision.GetContact(0).point, transform.position);
                        ballRb.AddForce(hitDir * distanceToPivot * (flipperData.kickForce * 0.5f), ForceMode2D.Impulse);
                    }
                }
            }
        }
        }
        }
