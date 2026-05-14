using UnityEngine;
using UnityEngine.UI;
using RPGPinball.Core;
using RPGPinball.Data;
using RPGPinball.Meta;

namespace RPGPinball.UI
{
    /// <summary>
    /// 마일스톤 6 임시 통화 HUD — 좌측 상단 Gold/ManaCrystal/BossSoul 3종.
    /// M7 정식 HUD 도입 시 교체.
    /// </summary>
    public class CurrencyHud : MonoBehaviour
    {
        [SerializeField] private CurrencyIconRegistry registry;
        [SerializeField] private Text goldText;
        [SerializeField] private Text manaCrystalText;
        [SerializeField] private Text bossSoulText;
        [SerializeField] private Image goldIcon;
        [SerializeField] private Image manaCrystalIcon;
        [SerializeField] private Image bossSoulIcon;

        private void OnEnable()
        {
            EventBus.Subscribe<OnCurrencyChanged>(HandleChange);
            ApplyIcons();
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnCurrencyChanged>(HandleChange);
        }

        private void ApplyIcons()
        {
            if (registry == null) return;
            SetIcon(goldIcon, CurrencyId.Gold);
            SetIcon(manaCrystalIcon, CurrencyId.ManaCrystal);
            SetIcon(bossSoulIcon, CurrencyId.BossSoul);
        }

        private void SetIcon(Image img, CurrencyId id)
        {
            if (img == null) return;
            if (registry.TryGetIcon(id, out var sprite, out var tint))
            {
                img.sprite = sprite;
                img.color = tint;
            }
        }

        private void HandleChange(OnCurrencyChanged e)
        {
            switch (e.CurrencyId)
            {
                case CurrencyId.Gold: SetText(goldText, e.NewBalance); break;
                case CurrencyId.ManaCrystal: SetText(manaCrystalText, e.NewBalance); break;
                case CurrencyId.BossSoul: SetText(bossSoulText, e.NewBalance); break;
            }
        }

        private void RefreshAll()
        {
            if (EconomyManager.Instance == null) return;
            SetText(goldText, EconomyManager.Instance.GetBalance(CurrencyId.Gold));
            SetText(manaCrystalText, EconomyManager.Instance.GetBalance(CurrencyId.ManaCrystal));
            SetText(bossSoulText, EconomyManager.Instance.GetBalance(CurrencyId.BossSoul));
        }

        private static void SetText(Text t, long value)
        {
            if (t != null) t.text = value.ToString();
        }
    }
}
