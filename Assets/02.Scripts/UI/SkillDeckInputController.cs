using UnityEngine;
using Cysharp.Threading.Tasks;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Physics;

namespace RPGPinball.UI
{
    /// <summary>
    /// HUD 스킬 슬롯 입력 처리. 2단계 입력:
    /// 1) 슬롯 버튼 터치 → selectedSlot 활성 + FlipperController.InputBlocked=true + 표적 지정 오버레이 표시
    /// 2) 화면(플레이필드) 터치 → worldPos 계산 → SkillDeck.Use(slot, worldPos) + InputBlocked=false
    /// 즉발 스킬은 슬롯 터치 즉시 발동.
    /// </summary>
    public class SkillDeckInputController : MonoBehaviour
    {
        public static SkillDeckInputController Instance { get; private set; }

        [SerializeField] private Camera worldCamera;
        public int SelectedSlot { get; private set; } = -1;
        public bool AwaitingTarget => SelectedSlot >= 0;

        public System.Action<int> OnSlotSelected;
        public System.Action<int> OnSlotCancelled;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>HUD 스킬 슬롯 버튼 → 슬롯 선택. 즉발 스킬은 즉시 발동.</summary>
        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Constants.SkillDeckSize) return;
            if (SkillDeck.Instance == null) return;
            var skill = SkillDeck.Instance.GetSkill(slotIndex);
            if (skill == null) return;

            // 즉발 (표적 불필요) — 본 마일스톤은 isUltimate 여부 무관, 항상 표적 지정 필요로 가정.
            // requiresTarget 플래그를 SkillData 에 추가하면 즉발 분기 가능 — 본 마일스톤은 모두 표적 지정.
            SelectedSlot = slotIndex;
            FlipperController.InputBlocked = true;
            OnSlotSelected?.Invoke(slotIndex);
            EventBus.Publish(new OnSkillSlotSelected { SlotIndex = slotIndex, RequiresTarget = true });
        }

        public void CancelSelection()
        {
            if (SelectedSlot < 0) return;
            int prev = SelectedSlot;
            SelectedSlot = -1;
            FlipperController.InputBlocked = false;
            OnSlotCancelled?.Invoke(prev);
            EventBus.Publish(new OnSkillSlotCancelled { SlotIndex = prev });
        }

        /// <summary>플레이필드 클릭 시 호출(InGameHUD 또는 PlayfieldClickRelay 가 위임). worldPos 또는 screenPos 둘 다 받음.</summary>
        public void ConsumeTargetTouch(Vector2 screenPosition)
        {
            if (SelectedSlot < 0) return;
            if (SkillDeck.Instance == null) { CancelSelection(); return; }

            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) { CancelSelection(); return; }
            var world = cam.ScreenToWorldPoint(screenPosition);
            int slot = SelectedSlot;
            SelectedSlot = -1;
            FlipperController.InputBlocked = false;
            SkillDeck.Instance.Use(slot, new Vector2(world.x, world.y)).Forget();
        }
    }
}
