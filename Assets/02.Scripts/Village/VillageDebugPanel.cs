using UnityEngine;
using Cysharp.Threading.Tasks;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.Village
{
    /// <summary>
    /// Village 씬 디버그 IMGUI 패널. 6개 시설 클릭 → 해당 시설 API 호출 + 잔액 로그.
    /// 본격 UI는 M7 인계.
    /// </summary>
    public class VillageDebugPanel : MonoBehaviour
    {
        private VillageFacilityId? activeFacility;
        private string statusLog = "";
        private Vector2 scroll;

        private void OnEnable()
        {
            VillageFacilityEntry.OnFacilityClicked += HandleClick;
        }

        private void OnDisable()
        {
            VillageFacilityEntry.OnFacilityClicked -= HandleClick;
        }

        private void HandleClick(VillageFacilityEntry entry)
        {
            activeFacility = entry.Facility;
            Log($"시설 진입: {entry.DisplayName}");
        }

        private void Log(string msg)
        {
            statusLog = $"[{System.DateTime.Now:HH:mm:ss}] {msg}\n" + statusLog;
            if (statusLog.Length > 4000) statusLog = statusLog.Substring(0, 4000);
            Debug.Log($"[VillageDebug] {msg}");
        }

        // 1080x1920 고해상도 IMGUI 폰트 크기 일괄 (기준 12pt → 28~32pt)
        private const int FontSizeLabel = 28;
        private const int FontSizeButton = 30;
        private const int FontSizeBox = 28;
        private const int FontSizeTextArea = 24;
        private static bool styleApplied;

        private static void ApplyLargeFontStyle()
        {
            if (styleApplied) return;
            // GUI.skin은 매 프레임 초기화될 수 있어 항상 강제 적용
            GUI.skin.label.fontSize = FontSizeLabel;
            GUI.skin.button.fontSize = FontSizeButton;
            GUI.skin.box.fontSize = FontSizeBox;
            GUI.skin.textArea.fontSize = FontSizeTextArea;
            GUI.skin.textField.fontSize = FontSizeTextArea;
            GUI.skin.toggle.fontSize = FontSizeLabel;
            // 박스 여백도 키움
            GUI.skin.box.padding = new RectOffset(12, 12, 12, 12);
            GUI.skin.button.padding = new RectOffset(16, 16, 10, 10);
            GUI.skin.label.padding = new RectOffset(4, 4, 6, 6);
        }

        private void OnGUI()
        {
            ApplyLargeFontStyle();

            // 통화 HUD
            DrawCurrencyHud();
            // 시설 패널
            if (activeFacility.HasValue)
            {
                DrawFacilityPanel(activeFacility.Value);
            }
            // 로그
            DrawLog();
        }

        private void DrawCurrencyHud()
        {
            var econ = EconomyManager.Instance;
            // 1080×1920 세로형 화면: 통화 HUD 영역 폭/높이 확장 (큰 폰트 대응)
            GUILayout.BeginArea(new Rect(20, 20, 1040, 260), GUI.skin.box);
            if (econ == null) GUILayout.Label("EconomyManager 미초기화");
            else
            {
                GUILayout.Label($"Gold: {econ.GetBalance(CurrencyId.Gold)}  ManaCrystal: {econ.GetBalance(CurrencyId.ManaCrystal)}  BossSoul: {econ.GetBalance(CurrencyId.BossSoul)}");
                GUILayout.Label($"CoreFragment: {econ.GetBalance(CurrencyId.CoreFragment)}  RespecScroll: {econ.GetBalance(CurrencyId.RespecScroll)}");
                if (GUILayout.Button("디버그: +1,000골드 / +50마나 결정 / +5 보스영혼"))
                {
                    econ.Add(CurrencyId.Gold, 1000, "Debug");
                    econ.Add(CurrencyId.ManaCrystal, 50, "Debug");
                    econ.Add(CurrencyId.BossSoul, 5, "Debug");
                }
            }
            GUILayout.EndArea();

            // ── 화면 우상단: ActMap 진입 (출항) 글로벌 버튼 ──
            const int btnW = 360, btnH = 140;
            GUILayout.BeginArea(new Rect(Screen.width - btnW - 20, 20, btnW, btnH), GUI.skin.box);
            if (GUILayout.Button("🎈 출항 (ActMap)", GUILayout.ExpandHeight(true)))
            {
                Log("출항 → ActMap 씬 전환");
                if (GameManager.Instance != null) GameManager.Instance.LoadActMap().Forget();
            }
            GUILayout.EndArea();
        }

        private void DrawFacilityPanel(VillageFacilityId fid)
        {
            // 1080×1920: 화면 중앙 영역, 시설 패널 폭 1000 / 높이 800
            GUILayout.BeginArea(new Rect(40, 300, 1000, 800), GUI.skin.box);
            GUILayout.Label($"== {fid} ==");
            switch (fid)
            {
                case VillageFacilityId.Forge: DrawForge(); break;
                case VillageFacilityId.Enchanter: DrawEnchanter(); break;
                case VillageFacilityId.Astrologer: DrawAstrologer(); break;
                case VillageFacilityId.Tavern: DrawTavern(); break;
                case VillageFacilityId.BalloonDock: DrawBalloon(); break;
                case VillageFacilityId.TrainingGround: DrawTraining(); break;
            }
            if (GUILayout.Button("닫기")) activeFacility = null;
            GUILayout.EndArea();
        }

        private void DrawForge()
        {
            var f = ForgeManager.Instance;
            if (f == null) { GUILayout.Label("ForgeManager 미초기화"); return; }
            GUILayout.Label($"재질: {f.CurrentMaterial} / 플리퍼 Lv.{f.FlipperUpgradeLevel} / 파생형: {f.CurrentVariant}");

            if (GUILayout.Button("플리퍼 강화"))
            {
                bool ok = f.UpgradeFlipper();
                Log($"플리퍼 강화 → {(ok ? "성공" : "실패")} / Lv.{f.FlipperUpgradeLevel}");
            }
            if (GUILayout.Button("가시 플리퍼 선택"))
            {
                bool ok = f.SelectFlipperVariant(FlipperVariantId.Spike);
                Log($"가시 파생형 → {(ok ? "성공" : "실패")}");
            }
            if (GUILayout.Button("강철 재질 제작"))
            {
                bool ok = f.CraftMaterial(BallMaterialId.Steel);
                Log($"강철 제작 → {(ok ? "성공" : "실패")}");
            }
            if (GUILayout.Button("강철 재질 장착"))
            {
                bool ok = f.EquipMaterial(BallMaterialId.Steel);
                Log($"강철 장착 → {(ok ? "성공" : "실패")}");
            }
        }

        private void DrawEnchanter()
        {
            var e = EnchanterManager.Instance;
            if (e == null) { GUILayout.Label("EnchanterManager 미초기화"); return; }
            GUILayout.Label($"인벤토리: {e.Inventory.Count}개 룬");
            if (GUILayout.Button("Normal 처형자 룬 추가 (디버그)"))
            {
                e.AddRune(RuneId.Executioner, RuneGrade.Normal);
                Log("처형자 룬(Normal) 추가");
            }
            if (GUILayout.Button("Normal 처형자 3개 → Rare 합성"))
            {
                e.AddRune(RuneId.Executioner, RuneGrade.Normal);
                e.AddRune(RuneId.Executioner, RuneGrade.Normal);
                e.AddRune(RuneId.Executioner, RuneGrade.Normal);
                bool ok = e.FuseRune(RuneId.Executioner, RuneGrade.Normal);
                Log($"합성 → {(ok ? "성공" : "실패")}");
            }
        }

        private void DrawAstrologer()
        {
            var a = AstrologerManager.Instance;
            if (a == null) { GUILayout.Label("AstrologerManager 미초기화"); return; }
            GUILayout.Label($"인벤토리: {a.Inventory.Count}장");
            if (GUILayout.Button("타로 뽑기 (500골드)"))
            {
                var inst = a.Pull(false);
                Log(inst != null ? $"타로 획득: {inst.id} ({inst.grade})" : "뽑기 실패");
            }
        }

        private void DrawTavern()
        {
            var t = TavernManager.Instance;
            if (t == null) { GUILayout.Label("TavernManager 미초기화"); return; }
            GUILayout.Label($"일일 의뢰: {t.GetActiveDailyQuests().Count}개");
            GUILayout.Label($"현상금: {t.GetBountyBoard().Count}개");
            if (GUILayout.Button("일일 의뢰 갱신"))
            {
                QuestManager.Instance?.RefreshDailyIfExpired();
                Log("일일 의뢰 갱신");
            }
        }

        private void DrawBalloon()
        {
            var b = BalloonManager.Instance;
            if (b == null) { GUILayout.Label("BalloonManager 미초기화"); return; }
            GUILayout.Label($"열기구 Lv.{b.CurrentUpgradeLevel} / 시작 마나 +{b.StartingManaBonus}");
            if (GUILayout.Button("🎈 출항 (ActMap 진입)"))
            {
                Log("열기구 출항 → ActMap 씬 전환");
                if (GameManager.Instance != null) GameManager.Instance.LoadActMap().Forget();
            }
            if (GUILayout.Button("열기구 다음 단계 개조"))
            {
                bool ok = b.Upgrade();
                Log($"열기구 개조 → {(ok ? "성공" : "실패")} Lv.{b.CurrentUpgradeLevel}");
            }

            var m = MercenaryManager.Instance;
            if (m == null) return;
            if (GUILayout.Button("긴급 방패 제작"))
            {
                bool ok = m.Craft(ConsumableId.EmergencyShield);
                Log($"긴급 방패 제작 → {(ok ? "성공" : "실패")}");
            }
        }

        private void DrawTraining()
        {
            var t = TrainingManager.Instance;
            if (t == null) { GUILayout.Label("TrainingManager 미초기화"); return; }
            GUILayout.Label($"리셋 비용: {t.GetResetCost()}골드");
            if (GUILayout.Button("스킬 전체 리셋"))
            {
                bool ok = t.ResetAllSkills(false);
                Log($"리셋 → {(ok ? "성공" : "실패")}");
            }
        }

        private void DrawLog()
        {
            // 1080×1920 세로형: 로그 패널은 하단에 가로 풀폭으로 배치 (우측 사이드바보다 가시성 좋음)
            int logHeight = 500;
            GUILayout.BeginArea(new Rect(20, Screen.height - logHeight - 20, Screen.width - 40, logHeight), GUI.skin.box);
            GUILayout.Label("== 로그 ==");
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.TextArea(statusLog, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
