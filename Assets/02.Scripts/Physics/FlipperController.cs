using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RPGPinball.Combat;
using RPGPinball.Core;

namespace RPGPinball.Physics
{
    /// <summary>
    /// 터치 입력을 받아 플리퍼를 소환·유지·소멸시킨다.
    /// 쿨타임, 소환 불가 영역, 블로킹 보너스를 처리한다.
    /// 마일스톤 3: SkillTreeManager의 쿨감 패시브 적용 + 존 오브 컨트롤 오버라이드.
    /// </summary>
    public class FlipperController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private GameObject flipperPrefab;
        [SerializeField] private InputActionAsset inputActions;

        [Header("소환 제한 영역")]
        [SerializeField] private float bossZoneMinY = 4f;
        [SerializeField] private float deadZoneMaxY = -5f;

        private InputAction touchPressAction;
        private InputAction touchPositionAction;
        private float cooldown;
        private float cooldownOverride = -1f; // 0 이상이면 오버라이드 사용 (존 오브 컨트롤)
        private bool unlimitedStack;
        private readonly List<FlipperInstance> active = new();

        // ── 마일스톤 4: 소환 차단 (꽃가루 침묵 / 집게 강타 / 시간 정지) ──
        // areaBlocked[i] == 영역 한정 차단, fullBlockedUntil == 전체 차단 종료 시각
        private float fullBlockedUntil;
        private readonly List<(Rect area, float endTime)> areaBlocks = new();

        private void Awake()
        {
            var map = inputActions.FindActionMap("Pinball", throwIfNotFound: true);
            touchPressAction = map.FindAction("TouchPress", throwIfNotFound: true);
            touchPositionAction = map.FindAction("TouchPosition", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            touchPressAction.Enable();
            touchPositionAction.Enable();
            touchPressAction.performed += OnTouchPerformed;
            EventBus.Subscribe<OnFlipperBlocked>(OnBlocked);
            EventBus.Subscribe<OnFlipperSpawnBlocked>(OnSpawnBlocked);
        }

        private void OnDisable()
        {
            touchPressAction.performed -= OnTouchPerformed;
            touchPressAction.Disable();
            touchPositionAction.Disable();
            EventBus.Unsubscribe<OnFlipperBlocked>(OnBlocked);
            EventBus.Unsubscribe<OnFlipperSpawnBlocked>(OnSpawnBlocked);
        }

        private void Update()
        {
            if (cooldown > 0f)
                cooldown -= Time.deltaTime;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].Tick(Time.deltaTime);
                if (active[i].IsDone)
                    active.RemoveAt(i);
            }
        }

        // ── 터치 입력 처리 ─────────────────────────────────────

        private void OnTouchPerformed(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                return;
            if (cooldown > 0f) return;

            // 전체 차단
            if (Time.time < fullBlockedUntil) return;

            var screenPos = touchPositionAction.ReadValue<Vector2>();
            var worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            var spawnPos = new Vector2(worldPos.x, worldPos.y);

            // 영역 차단 (집게 강타, 보스 발생 영역 등)
            PurgeExpiredAreaBlocks();
            for (int i = 0; i < areaBlocks.Count; i++)
            {
                if (areaBlocks[i].area.Contains(spawnPos)) return;
            }

            if (!IsSpawnAllowed(spawnPos)) return;
            if (IsTooCloseToExisting(spawnPos)) return;

            SpawnFlipper(spawnPos);
        }

        private void OnSpawnBlocked(OnFlipperSpawnBlocked evt)
        {
            float endTime = Time.time + Mathf.Max(0f, evt.Duration);
            if (evt.Area.HasValue)
            {
                areaBlocks.Add((evt.Area.Value, endTime));
            }
            else
            {
                fullBlockedUntil = Mathf.Max(fullBlockedUntil, endTime);
            }
        }

        private void PurgeExpiredAreaBlocks()
        {
            for (int i = areaBlocks.Count - 1; i >= 0; i--)
            {
                if (Time.time >= areaBlocks[i].endTime) areaBlocks.RemoveAt(i);
            }
        }

        private void SpawnFlipper(Vector2 pos)
        {
            var go = Instantiate(flipperPrefab, pos, Quaternion.identity);

            // 화면 중심 기준 좌/우 결정 → 스윙 방향 자동 세팅
            var flipper = go.GetComponent<Flipper>();
            if (flipper != null) flipper.InitializeSwing(pos.x < 0f);

            active.Add(new FlipperInstance(go));
            cooldown = ComputeCooldown();
            EventBus.Publish(new OnFlipperSpawned { Position = pos });
        }

        private float ComputeCooldown()
        {
            // 존 오브 컨트롤 오버라이드 (0 가능)
            if (cooldownOverride >= 0f) return cooldownOverride;

            // SkillTreeManager 쿨감 패시브 + 하드캡 0.5초
            float mult = SkillTreeManager.Instance != null
                ? SkillTreeManager.Instance.GetFlipperCooldownMultiplier()
                : 1f;
            float computed = Constants.FlipperCooldown * mult;
            return SkillFormula.HardCapMin(computed, Constants.FlipperCooldownMin);
        }

        /// <summary>존 오브 컨트롤용. seconds = 0 이면 쿨타임 없음, -1이면 오버라이드 해제.</summary>
        public void OverrideCooldown(float seconds)
        {
            cooldownOverride = seconds;
            if (seconds <= 0f) cooldown = 0f;
        }

        /// <summary>존 오브 컨트롤용. 스택 무한 해제.</summary>
        public void SetUnlimitedStack(bool active) { unlimitedStack = active; }

        // ── 소환 가능 여부 ─────────────────────────────────────

        private bool IsSpawnAllowed(Vector2 pos)
        {
            if (pos.y >= bossZoneMinY) return false;
            if (pos.y <= deadZoneMaxY) return false;
            return true;
        }

        private bool IsTooCloseToExisting(Vector2 pos)
        {
            foreach (var inst in active)
            {
                if (inst.IsAlive && Vector2.Distance(inst.Position, pos) < Constants.FlipperMinSpawnGap)
                    return true;
            }
            return false;
        }

        // ── 블로킹 보너스 ──────────────────────────────────────

        private void OnBlocked(OnFlipperBlocked evt)
        {
            cooldown = Mathf.Max(0f, cooldown - evt.CooldownReduction);
        }

        // ── 내부 플리퍼 인스턴스 ──────────────────────────────

        private class FlipperInstance
        {
            private readonly GameObject go;
            private float elapsed;

            public bool IsAlive => go != null && go.activeSelf;
            public bool IsDone => go == null;
            public Vector2 Position => go != null ? (Vector2)go.transform.position : Vector2.zero;

            public FlipperInstance(GameObject flipper) { go = flipper; }

            public void Tick(float dt)
            {
                if (go == null) return;
                elapsed += dt;
                if (elapsed >= Constants.FlipperActiveTime)
                    Object.Destroy(go);
            }
        }
    }
}
