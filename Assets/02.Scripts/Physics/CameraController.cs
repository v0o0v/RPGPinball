using UnityEngine;
using RPGPinball.Core;
using Com.LuisPedroFonseca.ProCamera2D;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 카메라 동적 줌 컨트롤러. 멀티볼 발생 시 +0.1/공 줌아웃.
    /// 보스전 줌아웃 ×1.2는 마일스톤 4에서 활용.
    /// ProCamera2D와 같은 카메라에 부착 시 충돌 회피를 위해 변경이 필요할 때만 사이즈 조작.
    /// 사이즈 변경이 끝나면 ProCamera2D에 다시 위임.
    /// 스테이지 경계 클램프는 ProCamera2D 갱신 이후에 적용되도록 ScriptExecutionOrder 를 큰 값으로 지정.
    /// </summary>
    [DefaultExecutionOrder(10000)]
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

        private ProCamera2D proCamera;

        private float targetSize;
        private float currentVelocity;
        // ProCamera2D와의 충돌 회피: 사이즈 조작이 실제 필요한 경우에만 활성
        private bool sizeControlActive;

        // ── 스테이지 경계 (FitToStageBounds 로 설정) ──────────
        private bool stageBoundsActive;
        private float stageMinY, stageMaxY;
        private float stageCenterX;

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

            proCamera = GetComponent<ProCamera2D>();
            if (proCamera == null && targetCamera != null) proCamera = targetCamera.GetComponent<ProCamera2D>();

            ApplyStageHudSafeArea();
        }

        /// <summary>
        /// 메인 카메라의 viewport rect 를 하단 HUD(마나바+스킬슬롯) 위로 올려 핀볼판이 HUD 와 겹치지 않게 함.
        /// 비율은 1080×1920 기준. 다른 해상도는 normalized 라 자동 적용됨.
        /// </summary>
        private void ApplyStageHudSafeArea()
        {
            if (targetCamera == null) return;
            float bottom = (float)Constants.StageCameraBottomHudPx / Constants.UIReferenceHeight;
            targetCamera.rect = new Rect(0f, bottom, 1f, 1f - bottom);
        }

        private void OnEnable()
        {
            // 도메인 리로드/씬 재진입 시 Instance 가 null 인 채 유지되는 케이스 보정.
            if (Instance == null) Instance = this;
            EventBus.Subscribe<OnBallCountChanged>(HandleBallCountChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnBallCountChanged>(HandleBallCountChanged);
            // Instance 해제는 OnDestroy 로 늦춤 (OnDisable 시점에는 다른 컴포넌트가 여전히 참조할 수 있음).
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            // 1) 줌 스무딩 (필요 시)
            if (sizeControlActive)
            {
                float current = targetCamera.orthographicSize;
                float newSize = Mathf.SmoothDamp(current, targetSize, ref currentVelocity, smoothTime);
                targetCamera.orthographicSize = newSize;

                if (Mathf.Abs(newSize - targetSize) < sizeEpsilon)
                {
                    targetCamera.orthographicSize = targetSize;
                    currentVelocity = 0f;
                    if (Mathf.Abs(targetSize - baseOrthographicSize) < sizeEpsilon)
                        sizeControlActive = false;
                }
            }

            // 2) 스테이지 경계 클램프 (ProCamera2D 갱신 이후 후처리)
            if (stageBoundsActive)
            {
                var p = targetCamera.transform.position;
                p.x = stageCenterX;

                float halfH = targetCamera.orthographicSize;
                if (stageMaxY - stageMinY <= halfH * 2f)
                {
                    // 스테이지가 화면보다 짧으면 중앙 고정
                    p.y = (stageMinY + stageMaxY) * 0.5f;
                }
                else
                {
                    float minCamY = stageMinY + halfH;
                    float maxCamY = stageMaxY - halfH;
                    p.y = Mathf.Clamp(p.y, minCamY, maxCamY);
                }
                targetCamera.transform.position = p;
            }
        }

        public void SetBaseSize(float size)
        {
            baseOrthographicSize = size;
            RecalculateTargetSize();
        }

        /// <summary>
        /// 카메라 OrthoSize를 스테이지 폭(worldWidth)에 정확히 맞춤. Camera.aspect 기반.
        /// 세로 화면(모바일)에서 좌·우 외벽이 화면 가장자리에 닿도록 한다.
        /// 호출 직후 카메라 X를 centerX로 고정(좌우 추적 비활성과 짝).
        /// stageBottomY/stageTopY 를 전달하면 상·하 외벽 너머가 보이지 않도록 LateUpdate 에서 Y 클램프.
        /// </summary>
        public void FitToStageBounds(float worldWidth, float centerX, float stageBottomY, float stageTopY)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || worldWidth <= 0f) return;

            float aspect = targetCamera.aspect;
            if (aspect <= 0f) return;

            // 가로 반폭 = OrthoSize × aspect → OrthoSize = worldWidth/2 / aspect
            float requiredOrthoSize = (worldWidth * 0.5f) / aspect;
            baseOrthographicSize = requiredOrthoSize;
            targetCamera.orthographicSize = requiredOrthoSize;
            targetSize = requiredOrthoSize;
            sizeControlActive = false;

            stageCenterX = centerX;
            // 카메라 X 고정 (Y/Z는 유지)
            var p = targetCamera.transform.position;
            p.x = centerX;
            targetCamera.transform.position = p;

            if (stageTopY > stageBottomY)
            {
                stageMinY = stageBottomY;
                stageMaxY = stageTopY;
                stageBoundsActive = true;
            }
        }

        /// <summary>레거시 호환 — 폭만 맞춤 (상·하 경계 클램프 없음).</summary>
        public void FitToStageWidth(float worldWidth, float centerX = 0f)
            => FitToStageBounds(worldWidth, centerX, 0f, 0f);

        /// <summary>
        /// ProCamera2D 추적 타깃 등록. BallController.OnEnable 등에서 호출.
        /// 중복 등록은 ProCamera2D 내부에서 무시.
        /// 기본 influenceH=0 → 좌우 추적 비활성(카메라 X 고정), 상하만 추적.
        /// </summary>
        public void RegisterFollow(Transform target, float influenceH = 0f, float influenceV = 1f)
        {
            if (target == null) return;
            if (proCamera == null) proCamera = GetComponent<ProCamera2D>() ?? (targetCamera != null ? targetCamera.GetComponent<ProCamera2D>() : null);
            if (proCamera == null) return;
            // ProCamera2D 의 AddCameraTarget 은 중복 등록을 자체적으로 방지하지 않으므로 사전 검사
            for (int i = 0; i < proCamera.CameraTargets.Count; i++)
            {
                if (proCamera.CameraTargets[i] != null && proCamera.CameraTargets[i].TargetTransform == target) return;
            }
            proCamera.AddCameraTarget(target, influenceH, influenceV);
        }

        public void UnregisterFollow(Transform target)
        {
            if (target == null || proCamera == null) return;
            for (int i = proCamera.CameraTargets.Count - 1; i >= 0; i--)
            {
                if (proCamera.CameraTargets[i] != null && proCamera.CameraTargets[i].TargetTransform == target)
                {
                    proCamera.RemoveCameraTarget(target);
                    break;
                }
            }
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

        // ── 마일스톤 5 모디파이어 스텁 ────────────────────────
        private float visionReductionPercent;
        /// <summary>현재 시야 감소율(0~1). 모디파이어 dispatcher가 조회.</summary>
        public float VisionReductionPercent => visionReductionPercent;

        /// <summary>
        /// 블리자드 모디파이어 등에서 호출. 마일스톤 5는 값 보관만 — 실제 화면 가장자리 어두워짐 셰이더는 마일스톤 8 인계.
        /// </summary>
        public void SetVisionReduction(float percent)
        {
            visionReductionPercent = Mathf.Clamp01(percent);
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
