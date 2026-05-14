using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RPGPinball.Combat;
using RPGPinball.Core;
using RPGPinball.Data;

namespace RPGPinball.UI
{
    /// <summary>
    /// Stage 씬 인게임 HUD — 위젯 12종 (DebugHud → 정식 교체).
    /// 코드 기반 UI 생성. Awake 에서 모든 위젯 자동 빌드.
    /// </summary>
    public class InGameHUD : MonoBehaviour
    {
        public static InGameHUD Instance { get; private set; }

        [Header("외부 참조")]
        [SerializeField] private RPGPinball.Enemy.BossAI.BossBase boundBoss;
        private SkillDeckInputController inputController;

        // 위젯 캐시
        private Text timerText, comboText, goldText, gradeText, manaText, bossHpText;
        private Image manaFill, bossHpFill;
        private Image[] skillSlotIcons;
        private Image[] skillCooldownOverlays;
        private GameObject pauseButton, bossHpBar, comboBig;
        private CanvasGroup targetOverlay;

        // 상태
        private int currentMaxCombo;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildAll();
        }

        private void Start()
        {
            // 덱이 비어있으면 디버그 4종 자동 장착 + 아이콘 표시
            EnsureDebugDeckIfEmpty();
            RefreshAllSkillSlotIcons();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnTimerChanged>(OnTimer);
            EventBus.Subscribe<OnManaChange>(OnMana);
            EventBus.Subscribe<OnComboChange>(OnCombo);
            EventBus.Subscribe<OnComboMilestone>(OnComboMilestone);
            EventBus.Subscribe<OnCurrencyChanged>(OnCurrency);
            EventBus.Subscribe<OnBossSpawned>(OnBossSpawned);
            EventBus.Subscribe<OnBossDefeated>(OnBossDefeated);
            EventBus.Subscribe<OnSkillDeckEquipped>(OnDeckEquipped);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnTimerChanged>(OnTimer);
            EventBus.Unsubscribe<OnManaChange>(OnMana);
            EventBus.Unsubscribe<OnComboChange>(OnCombo);
            EventBus.Unsubscribe<OnComboMilestone>(OnComboMilestone);
            EventBus.Unsubscribe<OnCurrencyChanged>(OnCurrency);
            EventBus.Unsubscribe<OnBossSpawned>(OnBossSpawned);
            EventBus.Unsubscribe<OnBossDefeated>(OnBossDefeated);
            EventBus.Unsubscribe<OnSkillDeckEquipped>(OnDeckEquipped);
        }

        private void OnDeckEquipped(OnSkillDeckEquipped e) => RefreshSkillSlotIcon(e.SlotIndex);

        private void RefreshAllSkillSlotIcons()
        {
            if (skillSlotIcons == null) return;
            for (int i = 0; i < skillSlotIcons.Length; i++) RefreshSkillSlotIcon(i);
        }

        private void RefreshSkillSlotIcon(int slot)
        {
            if (skillSlotIcons == null || slot < 0 || slot >= skillSlotIcons.Length) return;
            var img = skillSlotIcons[slot];
            if (img == null) return;
            var deck = RPGPinball.Combat.SkillDeck.Instance;
            var data = deck != null ? deck.GetSkill(slot) : null;
            if (data != null && data.icon != null)
            {
                img.sprite = data.icon;
                img.color = Color.white;
                img.enabled = true;
            }
            else
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0.25f);
            }
        }

        /// <summary>덱이 모두 비어 있으면 카테고리별 4종 디버그 스킬 자동 장착. M3 인계 사항 — UI 시각화용.</summary>
        private void EnsureDebugDeckIfEmpty()
        {
            var deck = RPGPinball.Combat.SkillDeck.Instance;
            if (deck == null) return;
            bool empty = true;
            for (int i = 0; i < Constants.SkillDeckSize; i++) if (deck.GetSkill(i) != null) { empty = false; break; }
            if (!empty) return;

#if UNITY_EDITOR
            // 4슬롯 분포: Destruction / Element / Control / Ultimate 1
            var allGuids = UnityEditor.AssetDatabase.FindAssets("t:SkillData");
            RPGPinball.Data.SkillData destruction = null, element = null, control = null, ultimate = null;
            foreach (var g in allGuids)
            {
                var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var sd = UnityEditor.AssetDatabase.LoadAssetAtPath<RPGPinball.Data.SkillData>(p);
                if (sd == null || sd.type == RPGPinball.Data.SkillType.Passive) continue;
                if (sd.isUltimate)
                {
                    if (ultimate == null) ultimate = sd;
                    continue; // 궁극기는 Slot3 전용 — branch 분기로 넘기지 않음
                }
                if (sd.branch == RPGPinball.Data.SkillBranch.Destruction && destruction == null) destruction = sd;
                else if (sd.branch == RPGPinball.Data.SkillBranch.Element && element == null) element = sd;
                else if (sd.branch == RPGPinball.Data.SkillBranch.Control && control == null) control = sd;
                if (destruction != null && element != null && control != null && ultimate != null) break;
            }
            if (destruction != null) deck.Equip(0, destruction, 1);
            if (element != null) deck.Equip(1, element, 1);
            if (control != null) deck.Equip(2, control, 1);
            if (ultimate != null) deck.Equip(3, ultimate, 1);
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            UpdateGrade();
            UpdateBossHp();
        }

        // ── 빌드 ─────────────────────────────────────────────

        private void BuildAll()
        {
            // 위젯 컨테이너는 이미 Canvas 자식이라 가정 (씬에 Canvas 부착)
            // 12종 위젯 — 모두 RectTransform 기반.
            BuildTopBar();
            BuildComboCenter();
            BuildBossHpBar();
            BuildBottomBar();
            BuildTargetOverlay();

            inputController = GetComponentInChildren<SkillDeckInputController>();
            if (inputController == null)
            {
                var go = new GameObject("SkillDeckInputController");
                go.transform.SetParent(transform, false);
                inputController = go.AddComponent<SkillDeckInputController>();
            }
            inputController.OnSlotSelected += _ => SetTargetOverlay(true);
            inputController.OnSlotCancelled += _ => SetTargetOverlay(false);
        }

        private void BuildTopBar()
        {
            var bar = NewRect("TopBar", transform);
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = new Vector2(1, 1);
            bar.pivot = new Vector2(0.5f, 1);
            bar.sizeDelta = new Vector2(0, 140);
            bar.anchoredPosition = new Vector2(0, -Constants.UISafeAreaTopPx);

            // ⏸ Pause Button (좌측)
            pauseButton = NewButton(bar, "PauseBtn", "⏸", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(40, -70), new Vector2(120, 120), new Color(0.7f, 0.7f, 0.7f, 1f), OnPauseClick);

            // ⏱ Timer (중앙)
            timerText = NewText(bar, "Timer", "180.0", 64, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(500, 100), TextAnchor.MiddleCenter);
            timerText.color = Color.white;

            // 💰 Gold (우측 상단 첫줄)
            goldText = NewText(bar, "Gold", "0G", 40, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40, -50), new Vector2(360, 60), TextAnchor.MiddleRight);
            goldText.color = new Color(1f, 0.85f, 0.2f, 1f);

            // ⭐ Grade (우측 상단 둘째줄)
            gradeText = NewText(bar, "Grade", "B", 56, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40, -110), new Vector2(360, 70), TextAnchor.MiddleRight);
            gradeText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        private void BuildComboCenter()
        {
            comboBig = new GameObject("ComboCenter", typeof(RectTransform));
            comboBig.transform.SetParent(transform, false);
            var rt = comboBig.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.7f);
            rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.sizeDelta = new Vector2(900, 200);
            rt.anchoredPosition = Vector2.zero;
            comboText = comboBig.AddComponent<Text>();
            comboText.text = "";
            comboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            comboText.fontSize = 100;
            comboText.color = Color.white;
            comboText.alignment = TextAnchor.MiddleCenter;
            var outline = comboBig.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);
        }

        private void BuildBossHpBar()
        {
            bossHpBar = new GameObject("BossHpBar", typeof(RectTransform));
            bossHpBar.transform.SetParent(transform, false);
            var rt = bossHpBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -180);
            rt.sizeDelta = new Vector2(900, 40);
            var bg = bossHpBar.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(bossHpBar.transform, false);
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(1, 1);
            frt.offsetMin = new Vector2(4, 4);
            frt.offsetMax = new Vector2(-4, -4);
            bossHpFill = fillGo.AddComponent<Image>();
            bossHpFill.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            bossHpFill.type = Image.Type.Filled;
            bossHpFill.fillMethod = Image.FillMethod.Horizontal;
            bossHpFill.fillAmount = 1f;

            bossHpText = NewText(bossHpBar.transform, "Label", "BOSS", 28, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 40), TextAnchor.MiddleCenter);
            bossHpText.color = Color.white;
            bossHpBar.SetActive(false);
        }

        private void BuildBottomBar()
        {
            var bar = NewRect("BottomBar", transform);
            bar.anchorMin = new Vector2(0, 0);
            bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.sizeDelta = new Vector2(0, Constants.StageCameraBottomHudPx); // 320px = 카메라 viewport bottom 과 정확히 일치
            bar.anchoredPosition = Vector2.zero;
            // 어두운 패널 배경 — 카메라 viewport 외 검은 영역을 자연스럽게 가림
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(0.06f, 0.06f, 0.10f, 1f);
            barBg.raycastTarget = false;

            // 마나 게이지
            var manaBg = NewRect("ManaBg", bar);
            manaBg.anchorMin = new Vector2(0.5f, 1);
            manaBg.anchorMax = new Vector2(0.5f, 1);
            manaBg.pivot = new Vector2(0.5f, 1);
            manaBg.anchoredPosition = new Vector2(0, -20);
            manaBg.sizeDelta = new Vector2(900, 50);
            var manaBgImg = manaBg.gameObject.AddComponent<Image>();
            manaBgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            var manaFillGo = new GameObject("ManaFill", typeof(RectTransform));
            manaFillGo.transform.SetParent(manaBg, false);
            var mfrt = manaFillGo.GetComponent<RectTransform>();
            mfrt.anchorMin = Vector2.zero;
            mfrt.anchorMax = Vector2.one;
            mfrt.offsetMin = new Vector2(4, 4);
            mfrt.offsetMax = new Vector2(-4, -4);
            manaFill = manaFillGo.AddComponent<Image>();
            manaFill.color = new Color(0.20f, 0.50f, 0.95f, 1f);
            manaFill.type = Image.Type.Filled;
            manaFill.fillMethod = Image.FillMethod.Horizontal;
            manaFill.fillAmount = 0f;
            manaText = NewText(manaBg, "Label", "0 / 100", 28, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 50), TextAnchor.MiddleCenter);
            manaText.color = Color.white;

            // 스킬 슬롯 4개
            skillSlotIcons = new Image[Constants.SkillDeckSize];
            skillCooldownOverlays = new Image[Constants.SkillDeckSize];
            float slotW = 160f;
            float spacing = 30f;
            float total = slotW * Constants.SkillDeckSize + spacing * (Constants.SkillDeckSize - 1);
            float startX = -total * 0.5f + slotW * 0.5f;
            for (int i = 0; i < Constants.SkillDeckSize; i++)
            {
                int slot = i;
                var slotGo = NewRect($"SkillSlot{i + 1}", bar);
                slotGo.anchorMin = new Vector2(0.5f, 0);
                slotGo.anchorMax = new Vector2(0.5f, 0);
                slotGo.pivot = new Vector2(0.5f, 0);
                slotGo.anchoredPosition = new Vector2(startX + i * (slotW + spacing), 30);
                slotGo.sizeDelta = new Vector2(slotW, slotW);
                var bg = slotGo.gameObject.AddComponent<Image>();
                bg.color = new Color(0.20f, 0.30f, 0.50f, 0.85f);
                var btn = slotGo.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSkillSlotClick(slot));

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(slotGo, false);
                var irt = iconGo.GetComponent<RectTransform>();
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(10, 10);
                irt.offsetMax = new Vector2(-10, -10);
                skillSlotIcons[i] = iconGo.AddComponent<Image>();
                skillSlotIcons[i].color = new Color(1f, 1f, 1f, 0.4f);

                var overlayGo = new GameObject("CooldownOverlay", typeof(RectTransform));
                overlayGo.transform.SetParent(slotGo, false);
                var ort = overlayGo.GetComponent<RectTransform>();
                ort.anchorMin = Vector2.zero;
                ort.anchorMax = Vector2.one;
                ort.offsetMin = Vector2.zero;
                ort.offsetMax = Vector2.zero;
                skillCooldownOverlays[i] = overlayGo.AddComponent<Image>();
                skillCooldownOverlays[i].color = new Color(0, 0, 0, 0.6f);
                skillCooldownOverlays[i].type = Image.Type.Filled;
                skillCooldownOverlays[i].fillMethod = Image.FillMethod.Radial360;
                skillCooldownOverlays[i].fillAmount = 0f;

                var labelText = NewText(slotGo, "SlotNum", (i + 1).ToString(), 32, new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(40, 40), TextAnchor.UpperLeft);
                labelText.color = Color.white;
            }
        }

        private void BuildTargetOverlay()
        {
            var go = new GameObject("TargetOverlay", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.10f);
            img.raycastTarget = true;
            targetOverlay = go.AddComponent<CanvasGroup>();
            targetOverlay.alpha = 0f;
            targetOverlay.blocksRaycasts = false;

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(OnTargetOverlayClick);

            var label = NewText(go.transform, "Hint", "🎯 표적 지정", 56, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 100), TextAnchor.MiddleCenter);
            label.color = Color.white;
        }

        private void SetTargetOverlay(bool on)
        {
            if (targetOverlay == null) return;
            targetOverlay.alpha = on ? 1f : 0f;
            targetOverlay.blocksRaycasts = on;
        }

        private void OnTargetOverlayClick()
        {
            if (inputController == null || !inputController.AwaitingTarget) return;
            // 화면 중앙 임시 사용 — 실제 터치 좌표는 InputSystem 또는 Pointer 캡처 필요
            inputController.ConsumeTargetTouch(Input.mousePosition);
            SetTargetOverlay(false);
        }

        // ── 이벤트 핸들러 ────────────────────────────────────

        private void OnTimer(OnTimerChanged e)
        {
            if (timerText == null) return;
            timerText.text = $"{e.Remaining:F1}";
            timerText.color = e.Remaining < 30f ? new Color(1f, 0.3f, 0.3f, 1f)
                : e.Remaining < 60f ? new Color(1f, 0.85f, 0.2f, 1f)
                : Color.white;
        }

        private void OnMana(OnManaChange e)
        {
            if (manaFill == null) return;
            float ratio = e.Max > 0 ? Mathf.Clamp01(e.Current / e.Max) : 0f;
            manaFill.fillAmount = ratio;
            if (manaText != null) manaText.text = $"{(int)e.Current} / {(int)e.Max}";
        }

        private void OnCombo(OnComboChange e)
        {
            if (e.Combo > currentMaxCombo) currentMaxCombo = e.Combo;
            if (comboText == null) return;
            comboText.text = e.Combo >= 2 ? $"{e.Combo} COMBO!" : "";
        }

        private void OnComboMilestone(OnComboMilestone e)
        {
            if (comboBig == null) return;
            comboBig.transform.DOKill();
            comboBig.transform.localScale = Vector3.one;
            comboBig.transform.DOPunchScale(Vector3.one * (Constants.UIComboMilestonePunchScale - 1f), Constants.UIComboMilestonePunchSec, 8, 0.5f);
            if (comboText != null) comboText.color = e.Milestone >= 100 ? Color.magenta : e.Milestone >= 50 ? Color.yellow : e.Milestone >= 30 ? Color.cyan : Color.white;
        }

        private void OnCurrency(OnCurrencyChanged e)
        {
            if (goldText == null) return;
            if (e.CurrencyId == CurrencyId.Gold)
                goldText.text = $"{e.NewBalance}G";
        }

        private void OnBossSpawned(OnBossSpawned e)
        {
            boundBoss = e.Boss != null ? e.Boss.GetComponent<RPGPinball.Enemy.BossAI.BossBase>() : null;
            if (bossHpBar != null) bossHpBar.SetActive(boundBoss != null);
            if (bossHpText != null && boundBoss != null) bossHpText.text = $"BOSS — {e.BossId}";
        }

        private void OnBossDefeated(OnBossDefeated e)
        {
            if (bossHpBar != null) bossHpBar.SetActive(false);
            boundBoss = null;
        }

        private void UpdateBossHp()
        {
            if (boundBoss == null || bossHpFill == null) return;
            bossHpFill.fillAmount = Mathf.Clamp01(boundBoss.CurrentHpRatio);
        }

        private void UpdateGrade()
        {
            if (gradeText == null || StageTimer.Instance == null) return;
            float ratio = StageTimer.Instance.Total > 0
                ? StageTimer.Instance.Remaining / StageTimer.Instance.Total
                : 0f;
            string g = ratio >= 0.6f ? "S" : ratio >= 0.3f ? "A" : ratio > 0 ? "B" : "C";
            gradeText.text = g;
            gradeText.color = g switch
            {
                "S" => new Color(1f, 0.4f, 0.8f),
                "A" => new Color(1f, 0.85f, 0.2f),
                "B" => new Color(0.85f, 0.85f, 0.85f),
                _ => new Color(0.7f, 0.5f, 0.3f)
            };
        }

        // ── 입력 ─────────────────────────────────────────────

        private void OnPauseClick()
        {
            if (PauseManager.Instance == null) return;
            if (PauseManager.Instance.IsPaused)
                PauseManager.Instance.Resume(PauseReason.UserRequest);
            else
                PauseManager.Instance.Pause(PauseReason.UserRequest);
            // PauseMenuUI 가 OnApplicationPaused 구독으로 표시
        }

        private void OnSkillSlotClick(int slotIndex)
        {
            inputController?.SelectSlot(slotIndex);
        }

        public int MaxCombo => currentMaxCombo;

        // ── UI 빌더 유틸 ─────────────────────────────────────

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Text NewText(Transform parent, string name, string text, int size, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            return t;
        }

        private static GameObject NewButton(Transform parent, string name, string label, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sz, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sz;
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = labelGo.AddComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = (int)(sz.y * 0.5f);
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }
    }
}
