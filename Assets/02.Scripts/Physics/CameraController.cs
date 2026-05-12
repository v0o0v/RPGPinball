using UnityEngine;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 카메라 동적 줌 컨트롤러. 멀티볼 발생 시 +0.1/공 줌아웃.
    /// 보스전 줌아웃 ×1.2는 마일스톤 4에서 활용.
    /// ProCamera2D와 같은 카메라에 부착 시 충돌 회피를 위해 변경이 필요할 때만 사이즈 조작.
    /// 사이즈 변경이 끝나면 ProCamera2D에 다시 위임.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("카메라 참조 (없으면 Camera.main 사용)")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float baseOrthographicSize = 9f;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float sizeEpsilon = 0.01f; // 이 값 이하 차이는 무시

        [Header("디버그")]
        [SerializeField] private int activeBallCount = 1;
        [SerializeField] private bool inBossFight;

        private float targetSize;
        private float currentVelocity;
        // ProCamera2D와의 충돌 회피: 사이즈 조작이 실제 필요한 경우에만 활성
        private bool sizeControlActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null && baseOrthographicSize <= 0f)
            {
                baseOrthographicSize = targetCamera.orthographicSize;
            }
            targetSize = baseOrthographicSize;
            sizeControlActive = false;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnBallCountChanged>(HandleBallCountChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnBallCountChanged>(HandleBallCountChanged);
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            // 사이즈 변경이 필요 없으면 ProCamera2D에 양보 (transform/size 모두 건드리지 않음)
            if (!sizeControlActive || targetCamera == null) return;

            float current = targetCamera.orthographicSize;
            float newSize = Mathf.SmoothDamp(current, targetSize, ref currentVelocity, smoothTime);
            targetCamera.orthographicSize = newSize;

            // 목표 사이즈에 충분히 가까우면 사이즈 제어 종료 → ProCamera2D 위임
            if (Mathf.Abs(newSize - targetSize) < sizeEpsilon)
            {
                targetCamera.orthographicSize = targetSize;
                currentVelocity = 0f;
                // 베이스로 복귀했으면 완전 양보
                if (Mathf.Abs(targetSize - baseOrthographicSize) < sizeEpsilon)
                {
                    sizeControlActive = false;
                }
            }
        }

        public void SetBaseSize(float size)
        {
            baseOrthographicSize = size;
            RecalculateTargetSize();
        }

        public void NotifyBallCount(int count)
        {
            int prev = activeBallCount;
            activeBallCount = Mathf.Max(1, count);
            if (prev != activeBallCount) RecalculateTargetSize();
        }

        public void NotifyBallAdded(int delta = 1)
        {
            activeBallCount = Mathf.Max(1, activeBallCount + delta);
            RecalculateTargetSize();
        }

        public void NotifyBallRemoved(int delta = 1)
        {
            activeBallCount = Mathf.Max(1, activeBallCount - delta);
            RecalculateTargetSize();
        }

        public void SetBossFight(bool active)
        {
            if (inBossFight == active) return;
            inBossFight = active;
            RecalculateTargetSize();
        }

        private void RecalculateTargetSize()
        {
            // 베이스 × (1 + (n-1) × 0.1) × (보스전이면 1.2)
            float multiBallMult = 1f + (activeBallCount - 1) * Constants.CameraMultiballZoomPerBall;
            float bossMult = inBossFight ? Constants.CameraBossZoom : 1f;
            targetSize = baseOrthographicSize * multiBallMult * bossMult;

            // 베이스와 차이가 있으면 사이즈 제어 활성
            if (Mathf.Abs(targetSize - baseOrthographicSize) >= sizeEpsilon
                || (targetCamera != null && Mathf.Abs(targetCamera.orthographicSize - targetSize) >= sizeEpsilon))
            {
                sizeControlActive = true;
            }
        }

        // ── 이벤트 핸들러 ────────────────────────────────────

        private void HandleBallCountChanged(OnBallCountChanged e) => NotifyBallCount(e.Count);
    }
}
