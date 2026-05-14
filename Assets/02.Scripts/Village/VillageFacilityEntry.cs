using UnityEngine;
using UnityEngine.EventSystems;
using RPGPinball.Core;

namespace RPGPinball.Village
{
    public enum VillageFacilityId
    {
        Forge,
        Enchanter,
        Tavern,
        Astrologer,
        BalloonDock,
        TrainingGround,
    }

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class VillageFacilityEntry : MonoBehaviour
    {
        [SerializeField] private VillageFacilityId facility;
        [SerializeField] private string displayNameKo = "시설";

        public VillageFacilityId Facility => facility;
        public string DisplayName => displayNameKo;

        public static event System.Action<VillageFacilityEntry> OnFacilityClicked;

        private void OnMouseDown()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            Debug.Log($"[Village] 시설 진입: {displayNameKo} ({facility})");
            OnFacilityClicked?.Invoke(this);
        }

        private void Reset()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
            }
        }
    }
}
