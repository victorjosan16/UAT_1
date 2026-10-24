using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Butoanele de reclame opționale (dreapta jos, rotunde, cu iconiță „play”):
    /// „BANI x2” pentru un timp și „BONUS” (o sumă proporțională cu stratul), fiecare cu pauză între folosiri.
    /// </summary>
    public class RewardedOffersHud : MonoBehaviour
    {
        [SerializeField] private float boostSeconds = 90f;
        [SerializeField] private float bonusCooldown = 180f;
        [SerializeField] private long bonusPerLayer = 150;

        private Button _boostButton;
        private Button _bonusButton;
        private UiLabel _boostLabel;
        private UiLabel _bonusLabel;
        private CanvasGroup _boostGroup;
        private CanvasGroup _bonusGroup;
        private float _bonusReadyAt;
        private float _refresh;
        private float _time;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _boostButton = CreateOffer("BoostAd", new Vector2(-34f, 470f), theme.AdColor, OnBoostClicked, out _boostLabel, out _boostGroup);
            _bonusButton = CreateOffer("BonusAd", new Vector2(-34f, 260f), theme.AccentColor, OnBonusClicked, out _bonusLabel, out _bonusGroup);
        }

        private Button CreateOffer(string name, Vector2 position, Color color, System.Action onClick, out UiLabel label, out CanvasGroup group)
        {
            RectTransform root = UiFactory.CreateRect(name, transform);
            UiFactory.Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), position, new Vector2(150f, 196f));
            group = root.gameObject.AddComponent<CanvasGroup>();

            Button button = UiFactory.CreateIconButton("Button", root, ProceduralSprites.Play, color, onClick, 0.42f);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(140f, 140f));

            Image tag = UiFactory.CreatePanel("Tag", root, UiRoot.Theme.PanelColor);
            UiFactory.Place((RectTransform)tag.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(150f, 56f));
            label = UiFactory.CreateText("Label", tag.transform, "", 26);
            UiFactory.Stretch(label.RectTransform, 4f);
            return button;
        }

        private void Update()
        {
            // Butoanele disponibile „respiră”, ca să atragă atenția fără să deranjeze.
            _time += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(_time * 3f) * 0.035f;
            if (_boostButton.interactable) _boostButton.transform.localScale = Vector3.one * pulse;
            if (_bonusButton.interactable) _bonusButton.transform.localScale = Vector3.one * pulse;

            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.25f;

            MoneyManager money = MoneyManager.Instance;
            bool boostActive = money != null && money.BoostTimeLeft > 0f;
            _boostButton.interactable = !boostActive;
            _boostGroup.alpha = boostActive ? 0.55f : 1f;
            _boostLabel.Text = boostActive ? $"{Mathf.CeilToInt(money.BoostTimeLeft)}s" : "BANI x2";

            float wait = _bonusReadyAt - Time.unscaledTime;
            _bonusButton.interactable = wait <= 0f;
            _bonusGroup.alpha = wait > 0f ? 0.55f : 1f;
            _bonusLabel.Text = wait > 0f ? $"{Mathf.CeilToInt(wait)}s" : "+" + NumberFormat.Money(BonusAmount);
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
