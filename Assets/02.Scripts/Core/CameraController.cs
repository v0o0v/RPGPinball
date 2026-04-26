using Com.LuisPedroFonseca.ProCamera2D;
using UnityEngine;

namespace RPGPinball.Core
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform ballTransform;
        
        private ProCamera2D proCamera;

        private void Start()
        {
            // Check if ProCamera2D exists in the scene before accessing Instance
            if (FindObjectOfType<ProCamera2D>() != null)
            {
                proCamera = ProCamera2D.Instance;
                if (ballTransform != null) proCamera.AddCameraTarget(ballTransform);
            }
            else
            {
                Debug.LogWarning("ProCamera2D component not found in the scene! Please add 'ProCamera2D' component to your Main Camera.");
            }
        }
    }
}
