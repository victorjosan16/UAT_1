using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Butoanele de reclame opționale din dreapta ecranului:
    /// „BANI x2” pentru un timp și „BONUS” (o sumă proporțională cu stratul), fiecare cu pauză între folosiri.
    /// </summary>
    public class RewardedOffersHud : MonoBehaviour
    {
        [SerializeField] private float boostSeconds = 90f;
        [SerializeField] private float bonusCooldown = 180f;
        [SerializeField] private long bonusPerLayer = 150;

        private Button _boostButton;
        private Button _bonusButton;
        private Text _boostLabel;
        private Text _bonusLabel;
        private float _bonusReadyAt;
        private float _refresh;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _boostButton = UiFactory.CreateButton("BoostAd", transform, "", theme.AdColor, OnBoostClicked, 30);
            UiFactory.Place((RectTransform)_boostButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-24f, 140f), new Vector2(230f, 130f));
            _boostLabel = UiFactory.GetButtonLabel(_boostButton);

            _bonusButton = UiFactory.CreateButton("BonusAd", transform, "", theme.AdColor, OnBonusClicked, 30);
            UiFactory.Place((RectTransform)_bonusButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-24f, -10f), new Vector2(230f, 130f));
            _bonusLabel = UiFactory.GetButtonLabel(_bonusButton);
        }

        private void Update()
        {
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.25f;

            MoneyManager money = MoneyManager.Instance;
            bool boostActive = money != null && money.BoostTimeLeft > 0f;
            _boostButton.interactable = !boostActive;
            _boostLabel.text = boostActive ? $"x2 activ\n{Mathf.CeilToInt(money.BoostTimeLeft)}s" : $"BANI x2\n{Mathf.RoundToInt(boostSeconds)}s (reclamă)";

            float wait = _bonusReadyAt - Time.unscaledTime;
            _bonusButton.interactable = wait <= 0f;
            _bonusLabel.text = wait > 0f ? $"BONUS\n{Mathf.CeilToInt(wait)}s" : $"BONUS\n+{NumberFormat.Money(BonusAmount)} (reclamă)";
        }

        private long BonusAmount
        {
            get
            {
                int layer = LayerGenerator.Instance != null ? LayerGenerator.Instance.CurrentLayerIndex : 0;
                return bonusPerLayer * (layer + 1);
            }
        }

        private void OnBoostClicked()
        {
            if (AdService.Instance == null) return;
            AdService.Instance.ShowRewarded("boost_x2", rewarded =>
            {
                if (rewarded && MoneyManager.Instance != null) MoneyManager.Instance.StartBoost(boostSeconds);
            });
        }

        private void OnBonusClicked()
        {
            if (AdService.Instance == null) return;
            long amount = BonusAmount;
            AdService.Instance.ShowRewarded("bonus_cash", rewarded =>
            {
                if (!rewarded || MoneyManager.Instance == null) return;
                _bonusReadyAt = Time.unscaledTime + bonusCooldown;
                MoneyManager.Instance.Add(amount);
                Vector3 position = PlayerRoot.Instance != null ? PlayerRoot.Instance.Position : Vector3.zero;
                GameEvents.RaiseMoneyEarned(amount, position, true);
            });
        }
    }
}
