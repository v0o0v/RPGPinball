using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using RPGPinball.Core;
using DG.Tweening;

namespace RPGPinball.UI
{
    /// <summary>
    /// Title 씬 컨트롤러. 화면 어디든 터치 → 세이브 유무에 따라 Village 또는 신규 게임 분기.
    /// 좌하단 [설정] / 우하단 [크레딧] 버튼.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private CanvasGroup touchToStartLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button touchToStartButton; // 전체 영역 fullscreen 투명 버튼

        private Tween pulseTween;

        private void OnEnable()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCredits);
            if (touchToStartButton != null)
                touchToStartButton.onClick.AddListener(OnStart);

            StartTouchPulse();
        }

        private void OnDisable()
        {
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCredits);
            if (touchToStartButton != null) touchToStartButton.onClick.RemoveListener(OnStart);
            KillPulseTween();
        }

        private void OnDestroy()
        {
            KillPulseTween();
        }

        private void KillPulseTween()
        {
            if (pulseTween != null)
            {
                if (pulseTween.IsActive()) pulseTween.Kill();
                pulseTween = null;
            }
        }

        private void StartTouchPulse()
        {
            if (touchToStartLabel == null) return;
            touchToStartLabel.alpha = 0.5f;
            // CanvasGroup.DOFade 는 DOTween Pro 별도 모듈 — alpha 보간을 직접 트윈으로 처리.
            // SetTarget 등록 + setter null 가드로 씬 전환 후 객체 파괴된 뒤 setter 호출되는 race 방지.
            pulseTween = DOTween.To(
                    () => touchToStartLabel != null ? touchToStartLabel.alpha : 0f,
                    v => { if (touchToStartLabel != null) touchToStartLabel.alpha = v; },
                    1f, 0.75f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetTarget(touchToStartLabel);
        }

        public void OnStart()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasSave())
            {
                GameManager.Instance.LoadVillage().Forget();
            }
            else
            {
                GameManager.Instance.LoadTutorial().Forget();
            }
        }

        public void OnSettings()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.OpenSettings();
        }

        public void OnCredits()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.OpenAlert("Sprites by Kenney.nl (CC0)\nBGM/SFX: TBD\nDeveloped with Unity 6", 5f);
        }
    }
}
