using UnityEngine;
using UnityEngine.UI;
using RPGPinball.Core;

namespace RPGPinball.UI
{
    /// <summary>
    /// 마일스톤 2 검증용 HUD. 마나/콤보/타이머/마지막 데미지를 텍스트로 표시.
    /// 정식 HUD는 마일스톤 7에서 교체.
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private Text manaText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text damageText;

        private float lastDamage;
        private bool lastCrit;

        private void OnEnable()
        {
            EventBus.Subscribe<OnManaChange>(HandleMana);
            EventBus.Subscribe<OnComboChange>(HandleCombo);
            EventBus.Subscribe<OnTimerChanged>(HandleTimer);
            EventBus.Subscribe<OnDamageDealt>(HandleDamage);
            EventBus.Subscribe<OnMonsterKilled>(HandleKill);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnManaChange>(HandleMana);
            EventBus.Unsubscribe<OnComboChange>(HandleCombo);
            EventBus.Unsubscribe<OnTimerChanged>(HandleTimer);
            EventBus.Unsubscribe<OnDamageDealt>(HandleDamage);
            EventBus.Unsubscribe<OnMonsterKilled>(HandleKill);
        }

        private void HandleMana(OnManaChange e)
        {
            if (manaText != null) manaText.text = $"MANA  {e.Current:0} / {e.Max:0}";
        }

        private void HandleCombo(OnComboChange e)
        {
            if (comboText != null) comboText.text = $"COMBO  {e.Combo}";
        }

        private void HandleTimer(OnTimerChanged e)
        {
            if (timerText != null) timerText.text = $"TIME  {e.Remaining:0.0}s";
        }

        private void HandleDamage(OnDamageDealt e)
        {
            lastDamage = e.Damage;
            lastCrit = e.IsCritical;
            if (damageText != null)
                damageText.text = $"DMG {lastDamage:0.0}{(lastCrit ? " CRIT!" : "")}";
        }

        private void HandleKill(OnMonsterKilled e)
        {
            Debug.Log($"[DebugHud] Monster killed (XP +{e.XpReward}, Gold +{e.GoldReward}, Boss={e.IsBoss})");
        }
    }
}
