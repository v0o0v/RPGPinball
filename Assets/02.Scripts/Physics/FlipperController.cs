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

        // 정적 소환 제한 영역은 데드존 제거(2026-05-13) 이후 폐기.
        // 보스 영역 차단은 OnFlipperSpawnBlocked 이벤트(areaBlocks)로 동적 관리.

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

        // ── 마일스톤 7: UI 입력 충돌 차단 (SkillDeck 표적 지정 등) ─────
        /// <summary>true 면 터치 입력으로 인한 플리퍼 소환 무시. SkillDeckInputController 가 토글.</summary>
        public static bool InputBlocked { get; set; }

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
            // M7: UI 입력 차단 (SkillDeck 표적 지정 / 일시정지 등)
            if (InputBlocked) return;

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

            // 공의 위치 기준 좌/우 결정: 공의 왼쪽 터치 → 왼쪽 플리퍼(반시계 스윙), 오른쪽 터치 → 오른쪽 플리퍼(시계 스윙).
            // 멀티볼이면 가장 가까운 공 기준. 공이 없으면 화면 중심 fallback.
            var flipper = go.GetComponent<Flipper>();
            if (flipper != null) flipper.InitializeSwing(IsLeftOfNearestBall(pos));

            active.Add(new FlipperInstance(go));
            cooldown = ComputeCooldown();
            EventBus.Publish(new OnFlipperSpawned { Position = pos });
        }

        private static bool IsLeftOfNearestBall(Vector2 spawnPos)
        {
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            if (balls == null || balls.Length == 0) return spawnPos.x < 0f;

            float bestSqr = float.MaxValue;
            float bestBallX = 0f;
            for (int i = 0; i < balls.Length; i++)
            {
                if (balls[i] == null) continue;
                var bp = (Vector2)balls[i].transform.position;
                float sqr = (bp - spawnPos).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; bestBallX = bp.x; }
            }
            return spawnPos.x < bestBallX;
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
        // 데드존 제거(2026-05-13) 이후 정적 Y 제약 폐기. 보스 영역 차단은
        // OnFlipperSpawnBlocked → areaBlocks 로 동적 관리(OnTouchPerformed 에서 검사).

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
