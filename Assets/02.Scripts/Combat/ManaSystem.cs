using UnityEngine;
using RPGPinball.Core;
using RPGPinball.Security;

namespace RPGPinball.Combat
{
    /// <summary>
    /// 마나 충전/소비 관리. 벽/몬스터/보스 충돌 시 충전, 콤보 배율 적용.
    /// 0~100 클램프. 마일스톤 6의 강화 시스템에서 ChargeEfficiency 갱신.
    /// </summary>
    public class ManaSystem : MonoBehaviour
    {
        public static ManaSystem Instance { get; private set; }

        [SerializeField] private float chargeEfficiency = 1.0f;

        private SafeInt mana;

        public int Mana => mana.Value;
        public int Max => Constants.ManaMax;
        public float ChargeEfficiency
        {
            get => chargeEfficiency;
            set => chargeEfficiency = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mana = SafeInt.Create(0);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnBallHit>(HandleBallHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnBallHit>(HandleBallHit);
            if (Instance == this) Instance = null;
        }

        /// <summary>충전. 콤보 배율은 호출 측에서 적용해도 되지만 기본은 ComboSystem 인스턴스를 참조.</summary>
        public void Charge(int baseAmount)
        {
            float comboMult = ComboSystem.Instance != null ? ComboSystem.Instance.ManaMultiplier : 1f;
            int delta = Mathf.RoundToInt(baseAmount * chargeEfficiency * comboMult);
            SetMana(Mathf.Clamp(mana.Value + delta, 0, Constants.ManaMax));
        }

        /// <summary>스킬 발동 시 마나 차감. 부족하면 false.</summary>
        public bool TrySpend(int cost)
        {
            if (mana.Value < cost) return false;
            SetMana(mana.Value - cost);
            return true;
        }

        public void SetManaDirect(int value)
        {
            SetMana(Mathf.Clamp(value, 0, Constants.ManaMax));
        }

        private void SetMana(int v)
        {
            mana = SafeInt.Create(v);
            EventBus.Publish(new OnManaChange { Current = v, Max = Constants.ManaMax });
        }

        // ── 이벤트 핸들러 ─────────────────────────────────────

        private void HandleBallHit(OnBallHit e)
        {
            switch (e.TargetTag)
            {
                case Constants.TagWall:
                case Constants.TagBumper:
                    Charge(Constants.ManaPerWall);
                    break;
                case Constants.TagMonster:
                    Charge(Constants.ManaPerMonster);
                    break;
                case Constants.TagBoss:
                    Charge(Constants.ManaPerBoss);
                    break;
            }
        }
    }
}
